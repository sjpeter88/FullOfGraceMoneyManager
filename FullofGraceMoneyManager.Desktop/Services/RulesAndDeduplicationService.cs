using System.Globalization;
using System.Text.RegularExpressions;
using FullofGraceMoneyManager.Desktop.Models;

namespace FullofGraceMoneyManager.Desktop.Services;

public class RulesAndDeduplicationService
{
    /// <summary>
    /// Cleans noisy bank payee strings, evaluates IF/THEN categorization rules,
    /// and runs multi-pass exact + fuzzy deduplication against existing account transactions.
    /// </summary>
    public StatementImportPreviewResult ProcessImportBatch(
        string sourceFileName,
        string detectedFormat,
        int targetAccountId,
        List<ImportedStatementItem> rawItems,
        List<CategorizationRule> rules,
        List<Category> categories,
        List<Transaction> existingTransactions)
    {
        var categoriesById = categories.ToDictionary(c => c.Id);
        var activeRules = rules
            .Where(r => r.IsEnabled && (!r.TargetAccountId.HasValue || r.TargetAccountId.Value == targetAccountId))
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Id)
            .ToList();

        foreach (var item in rawItems)
        {
            // Step 1: Baseline Payee Normalization (strip POS noise, store numbers, card masks)
            item.NormalizedPayee = NormalizeBankPayeeNoise(item.RawPayee);

            // Step 2: Evaluate IF/THEN Categorization Rules
            foreach (var rule in activeRules)
            {
                if (DoesRuleMatch(rule, item))
                {
                    item.AppliedRuleId = rule.Id;
                    item.AppliedRuleName = rule.Name;

                    if (!string.IsNullOrWhiteSpace(rule.RenamePayeeTo))
                    {
                        item.NormalizedPayee = rule.RenamePayeeTo.Trim();
                    }

                    if (rule.AssignCategoryId.HasValue &&
                        categoriesById.TryGetValue(rule.AssignCategoryId.Value, out var matchedCat))
                    {
                        item.SuggestedCategoryId = matchedCat.Id;
                        item.SuggestedCategoryName = matchedCat.FullPath;
                    }

                    if (!string.IsNullOrWhiteSpace(rule.SetStatus))
                    {
                        item.SuggestedStatus = rule.SetStatus;
                    }

                    if (!string.IsNullOrWhiteSpace(rule.AppendMemo))
                    {
                        item.FinalMemo = string.IsNullOrWhiteSpace(item.RawMemo)
                            ? rule.AppendMemo
                            : $"{item.RawMemo} • {rule.AppendMemo}";
                    }

                    break; // Highest-priority matching rule wins
                }
            }

            // Step 3: Multi-Pass Deduplication against existing register entries
            EvaluateDeduplication(item, existingTransactions);
        }

        return new StatementImportPreviewResult
        {
            SourceFileName = sourceFileName,
            DetectedFormat = detectedFormat,
            TargetAccountId = targetAccountId,
            Items = rawItems
        };
    }

    public static bool DoesRuleMatch(CategorizationRule rule, ImportedStatementItem item)
    {
        return DoesRuleMatchValues(
            rule,
            item.RawPayee,
            item.NormalizedPayee,
            item.RawMemo,
            Math.Abs(item.Amount));
    }

    public static bool DoesRuleMatchTransaction(CategorizationRule rule, Transaction tx)
    {
        if (rule.TargetAccountId.HasValue && rule.TargetAccountId.Value != tx.AccountId)
        {
            return false;
        }

        return DoesRuleMatchValues(
            rule,
            tx.PayeeName,
            tx.PayeeName,
            tx.Memo,
            Math.Abs(tx.Amount));
    }

    private static bool DoesRuleMatchValues(
        CategorizationRule rule,
        string rawPayee,
        string normalizedPayee,
        string? memo,
        decimal absAmount)
    {
        if (string.IsNullOrWhiteSpace(rule.MatchPattern))
        {
            return false;
        }

        if (rule.MinAmount.HasValue && absAmount < rule.MinAmount.Value)
        {
            return false;
        }

        if (rule.MaxAmount.HasValue && absAmount > rule.MaxAmount.Value)
        {
            return false;
        }

        string candidateText = rule.MatchField switch
        {
            "Memo" => memo ?? string.Empty,
            "PayeeOrMemo" => $"{rawPayee} {normalizedPayee} {memo}",
            _ => $"{rawPayee} {normalizedPayee}"
        };

        string pattern = rule.MatchPattern.Trim();

        return rule.MatchOperator switch
        {
            "StartsWith" => candidateText.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) ||
                            rawPayee.StartsWith(pattern, StringComparison.OrdinalIgnoreCase),
            "Equals" => string.Equals(rawPayee, pattern, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(normalizedPayee, pattern, StringComparison.OrdinalIgnoreCase),
            "Regex" => SafeRegexIsMatch(candidateText, pattern),
            _ => candidateText.Contains(pattern, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool SafeRegexIsMatch(string input, string pattern)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void EvaluateDeduplication(ImportedStatementItem item, List<Transaction> existingTransactions)
    {
        // Pass 1: Exact FITID / Reference Number + Exact Amount Match
        if (!string.IsNullOrWhiteSpace(item.ExternalFitId))
        {
            var exactFit = existingTransactions.FirstOrDefault(t =>
                !string.IsNullOrWhiteSpace(t.ReferenceNumber) &&
                string.Equals(t.ReferenceNumber, item.ExternalFitId, StringComparison.OrdinalIgnoreCase) &&
                t.Amount == item.Amount);

            if (exactFit != null)
            {
                item.DuplicateMatchType = "ExactFitId";
                item.MatchedTransactionId = exactFit.Id;
                item.MatchConfidenceScore = 100;
                item.MatchReason = $"Exact FITID/Ref match (#{exactFit.Id} '{exactFit.PayeeName}' on {exactFit.TransactionDate})";
                item.SelectedForImport = false;
                return;
            }
        }

        // Pass 2: Fuzzy Match — Date proximity (±3 days) + Exact Amount + Normalized Payee similarity
        DateTime.TryParse(item.TransactionDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var itemDate);

        Transaction? bestCandidate = null;
        int bestScore = 0;
        int bestDayDelta = 999;

        foreach (var existing in existingTransactions)
        {
            if (existing.Amount != item.Amount)
            {
                continue;
            }

            if (!DateTime.TryParse(existing.TransactionDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exDate))
            {
                continue;
            }

            int dayDelta = Math.Abs((itemDate - exDate).Days);
            if (dayDelta > 3)
            {
                continue;
            }

            double nameSim = ComputePayeeSimilarity(item.NormalizedPayee, item.RawPayee, existing.PayeeName);
            int dateScore = dayDelta == 0 ? 45 : (dayDelta == 1 ? 38 : (dayDelta == 2 ? 30 : 22));
            int totalScore = Math.Min(99, dateScore + (int)Math.Round(nameSim * 55));

            if (totalScore > bestScore)
            {
                bestScore = totalScore;
                bestCandidate = existing;
                bestDayDelta = dayDelta;
            }
        }

        if (bestCandidate != null && bestScore >= 65)
        {
            item.DuplicateMatchType = "FuzzyMatch";
            item.MatchedTransactionId = bestCandidate.Id;
            item.MatchConfidenceScore = bestScore;
            string dayText = bestDayDelta == 0 ? "same day" : $"±{bestDayDelta}d";
            item.MatchReason = $"Fuzzy match ({bestScore}%): #{bestCandidate.Id} '{bestCandidate.PayeeName}' ({dayText}, exact ${Math.Abs(item.Amount):N2})";
            item.SelectedForImport = false;
        }
    }

    public static string NormalizeBankPayeeNoise(string rawPayee)
    {
        if (string.IsNullOrWhiteSpace(rawPayee))
        {
            return "Unknown Payee";
        }

        string cleaned = rawPayee.Trim();

        // Remove common card processor prefixes
        cleaned = Regex.Replace(cleaned, @"^(POS\s+DEBIT|POS\s+PURCHASE|ACH\s+WEB|TST\*|SQ\s*\*|SP\s*\*|PAYPAL\s*\*)\s*", "", RegexOptions.IgnoreCase);

        // Remove store / branch identifiers like #10248 or *849201
        cleaned = Regex.Replace(cleaned, @"([#*]\d{2,10}|\b\d{5,12}\b)", "", RegexOptions.IgnoreCase);

        // Collapse multiple spaces
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim(' ', '-', '*', '/');

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return rawPayee.Trim();
        }

        // Convert ALL CAPS bank strings into clean Title Case while preserving short acronyms
        if (cleaned == cleaned.ToUpperInvariant() && cleaned.Length > 4)
        {
            cleaned = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
        }

        return cleaned;
    }

    private static double ComputePayeeSimilarity(string normalizedImport, string rawImport, string existingPayee)
    {
        string a = normalizedImport.ToLowerInvariant();
        string aRaw = rawImport.ToLowerInvariant();
        string b = existingPayee.ToLowerInvariant();

        if (a == b || aRaw == b || a.Contains(b) || b.Contains(a))
        {
            return 1.0;
        }

        var tokensA = Regex.Split($"{a} {aRaw}", @"[^a-z0-9]+").Where(t => t.Length >= 3).ToHashSet();
        var tokensB = Regex.Split(b, @"[^a-z0-9]+").Where(t => t.Length >= 3).ToHashSet();

        if (tokensA.Count == 0 || tokensB.Count == 0)
        {
            return 0.35;
        }

        int intersection = tokensA.Count(t => tokensB.Any(tb => tb.StartsWith(t[..Math.Min(4, t.Length)]) || t.StartsWith(tb[..Math.Min(4, tb.Length)])));
        return Math.Min(1.0, (double)intersection / Math.Min(tokensA.Count, tokensB.Count));
    }
}
