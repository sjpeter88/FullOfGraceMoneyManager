namespace FullofGraceMoneyManager.Desktop.Models;

public class ImportedStatementItem
{
    public string TempId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string? ExternalFitId { get; set; }
    public string TransactionDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string RawPayee { get; set; } = string.Empty;
    public string NormalizedPayee { get; set; } = string.Empty;
    public string? RawMemo { get; set; }
    public string? FinalMemo { get; set; }
    public string? ReferenceNumber { get; set; }
    public decimal Amount { get; set; }

    // Rule Engine Output
    public int? SuggestedCategoryId { get; set; }
    public string SuggestedCategoryName { get; set; } = "Uncategorized";
    public string SuggestedStatus { get; set; } = TransactionStatus.Cleared;
    public int? AppliedRuleId { get; set; }
    public string? AppliedRuleName { get; set; }

    // Multi-Pass Deduplication Output
    public string DuplicateMatchType { get; set; } = "None"; // None, ExactFitId, FuzzyMatch
    public int? MatchedTransactionId { get; set; }
    public int MatchConfidenceScore { get; set; }
    public string? MatchReason { get; set; }
    public bool SelectedForImport { get; set; } = true;

    public bool IsDuplicate => !string.Equals(DuplicateMatchType, "None", StringComparison.OrdinalIgnoreCase);
}

public class StatementImportPreviewResult
{
    public string SourceFileName { get; set; } = string.Empty;
    public string DetectedFormat { get; set; } = "OFX";
    public int TargetAccountId { get; set; } = 1;
    public int TotalParsedCount => Items.Count;
    public int NewItemsCount => Items.Count(i => !i.IsDuplicate);
    public int DuplicateItemsCount => Items.Count(i => i.IsDuplicate);
    public int RulesMatchedCount => Items.Count(i => i.AppliedRuleId.HasValue);
    public List<ImportedStatementItem> Items { get; set; } = new();
}
