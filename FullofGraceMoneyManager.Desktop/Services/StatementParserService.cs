using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FullofGraceMoneyManager.Desktop.Models;

namespace FullofGraceMoneyManager.Desktop.Services;

public class StatementParserService
{
    public (string DetectedFormat, List<ImportedStatementItem> Items) ParseStatementContent(
        string fileName,
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return ("CSV", new List<ImportedStatementItem>());
        }

        string trimmed = content.TrimStart();
        string ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

        if (ext is ".ofx" or ".qfx" ||
            trimmed.Contains("<OFX", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("OFXHEADER", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("<STMTTRN>", StringComparison.OrdinalIgnoreCase))
        {
            return ("OFX/QFX", ParseOfx(content));
        }

        if (ext == ".qif" || trimmed.StartsWith("!Type:", StringComparison.OrdinalIgnoreCase))
        {
            return ("QIF", ParseQif(content));
        }

        return ("CSV", ParseCsv(content));
    }

    public List<ImportedStatementItem> ParseOfx(string ofxContent)
    {
        var items = new List<ImportedStatementItem>();
        var blockSplit = Regex.Split(ofxContent, @"<STMTTRN>", RegexOptions.IgnoreCase);

        for (int i = 1; i < blockSplit.Length; i++)
        {
            string block = blockSplit[i];
            int endIdx = block.IndexOf("</STMTTRN>", StringComparison.OrdinalIgnoreCase);
            if (endIdx >= 0)
            {
                block = block[..endIdx];
            }

            string dtPosted = ExtractOfxTag(block, "DTPOSTED");
            string trnAmt = ExtractOfxTag(block, "TRNAMT");
            string fitId = ExtractOfxTag(block, "FITID");
            string checkNum = ExtractOfxTag(block, "CHECKNUM");
            string name = ExtractOfxTag(block, "NAME");
            string memo = ExtractOfxTag(block, "MEMO");

            if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(memo))
            {
                name = memo;
            }

            if (!decimal.TryParse(trnAmt, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
            {
                continue;
            }

            items.Add(new ImportedStatementItem
            {
                ExternalFitId = string.IsNullOrWhiteSpace(fitId) ? null : fitId.Trim(),
                TransactionDate = NormalizeOfxDate(dtPosted),
                RawPayee = string.IsNullOrWhiteSpace(name) ? "OFX Bank Entry" : name.Trim(),
                NormalizedPayee = string.IsNullOrWhiteSpace(name) ? "OFX Bank Entry" : name.Trim(),
                RawMemo = string.IsNullOrWhiteSpace(memo) ? null : memo.Trim(),
                FinalMemo = string.IsNullOrWhiteSpace(memo) ? null : memo.Trim(),
                ReferenceNumber = !string.IsNullOrWhiteSpace(checkNum)
                    ? checkNum.Trim()
                    : (!string.IsNullOrWhiteSpace(fitId) ? fitId.Trim() : "OFX"),
                Amount = amount,
                SuggestedStatus = TransactionStatus.Cleared
            });
        }

        return items;
    }

    public List<ImportedStatementItem> ParseQif(string qifContent)
    {
        var items = new List<ImportedStatementItem>();
        var lines = qifContent.Split(["\r\n", "\n"], StringSplitOptions.None);

        string? dateStr = null;
        decimal? amount = null;
        string? payee = null;
        string? memo = null;
        string? refNum = null;
        string status = TransactionStatus.Uncleared;

        void FlushRecord()
        {
            if (amount.HasValue && (!string.IsNullOrWhiteSpace(payee) || !string.IsNullOrWhiteSpace(memo)))
            {
                string rawP = !string.IsNullOrWhiteSpace(payee) ? payee.Trim() : (memo?.Trim() ?? "QIF Entry");
                items.Add(new ImportedStatementItem
                {
                    ExternalFitId = refNum,
                    TransactionDate = NormalizeFlexibleDate(dateStr),
                    RawPayee = rawP,
                    NormalizedPayee = rawP,
                    RawMemo = memo?.Trim(),
                    FinalMemo = memo?.Trim(),
                    ReferenceNumber = string.IsNullOrWhiteSpace(refNum) ? "QIF" : refNum.Trim(),
                    Amount = amount.Value,
                    SuggestedStatus = status
                });
            }

            dateStr = null;
            amount = null;
            payee = null;
            memo = null;
            refNum = null;
            status = TransactionStatus.Uncleared;
        }

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('!'))
            {
                continue;
            }

            if (line == "^")
            {
                FlushRecord();
                continue;
            }

            char code = char.ToUpperInvariant(line[0]);
            string value = line[1..].Trim();

            switch (code)
            {
                case 'D':
                    dateStr = value;
                    break;
                case 'T':
                case 'U':
                    string cleanAmt = value.Replace(",", "").Replace("$", "");
                    if (decimal.TryParse(cleanAmt, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedAmt))
                    {
                        amount = parsedAmt;
                    }
                    break;
                case 'P':
                    payee = value;
                    break;
                case 'M':
                    memo = value;
                    break;
                case 'N':
                    refNum = value;
                    break;
                case 'C':
                    status = value is "*" or "c" or "C"
                        ? TransactionStatus.Cleared
                        : (value is "X" or "R" ? TransactionStatus.Reconciled : TransactionStatus.Uncleared);
                    break;
            }
        }

        FlushRecord();
        return items;
    }

    public List<ImportedStatementItem> ParseCsv(string csvContent)
    {
        var items = new List<ImportedStatementItem>();
        var lines = csvContent
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0)
        {
            return items;
        }

        var headerCols = SplitCsvLine(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToList();
        bool hasHeader = headerCols.Any(h =>
            h.Contains("date") || h.Contains("payee") || h.Contains("description") || h.Contains("amount"));

        int dateIdx = FindColumnIndex(headerCols, ["date", "posting date", "transaction date"], 0);
        int payeeIdx = FindColumnIndex(headerCols, ["payee", "description", "name", "merchant"], 1);
        int amountIdx = FindColumnIndex(headerCols, ["amount", "transaction amount", "value"], 2);
        int debitIdx = FindColumnIndex(headerCols, ["debit", "withdrawal", "payment"], -1);
        int creditIdx = FindColumnIndex(headerCols, ["credit", "deposit"], -1);
        int memoIdx = FindColumnIndex(headerCols, ["memo", "notes", "details"], 3);
        int refIdx = FindColumnIndex(headerCols, ["reference", "ref", "check", "fitid", "id"], 4);

        int startRow = hasHeader ? 1 : 0;
        for (int i = startRow; i < lines.Count; i++)
        {
            var cols = SplitCsvLine(lines[i]);
            if (cols.Count < 2)
            {
                continue;
            }

            string rawDate = GetCol(cols, dateIdx);
            string rawPayee = GetCol(cols, payeeIdx);
            string rawMemo = GetCol(cols, memoIdx);
            string rawRef = GetCol(cols, refIdx);

            decimal amount = 0m;
            bool parsedAmount = false;

            if (debitIdx >= 0 || creditIdx >= 0)
            {
                string debStr = GetCol(cols, debitIdx).Replace("$", "").Replace(",", "").Trim();
                string credStr = GetCol(cols, creditIdx).Replace("$", "").Replace(",", "").Trim();
                if (decimal.TryParse(debStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal debVal) && debVal != 0)
                {
                    amount = -Math.Abs(debVal);
                    parsedAmount = true;
                }
                else if (decimal.TryParse(credStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal credVal) && credVal != 0)
                {
                    amount = Math.Abs(credVal);
                    parsedAmount = true;
                }
            }

            if (!parsedAmount)
            {
                string amtStr = GetCol(cols, amountIdx).Replace("$", "").Replace(",", "").Trim();
                if (decimal.TryParse(amtStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
                {
                    amount = val;
                    parsedAmount = true;
                }
            }

            if (!parsedAmount || string.IsNullOrWhiteSpace(rawPayee))
            {
                continue;
            }

            items.Add(new ImportedStatementItem
            {
                ExternalFitId = string.IsNullOrWhiteSpace(rawRef) ? null : rawRef,
                TransactionDate = NormalizeFlexibleDate(rawDate),
                RawPayee = rawPayee.Trim(),
                NormalizedPayee = rawPayee.Trim(),
                RawMemo = string.IsNullOrWhiteSpace(rawMemo) ? null : rawMemo.Trim(),
                FinalMemo = string.IsNullOrWhiteSpace(rawMemo) ? null : rawMemo.Trim(),
                ReferenceNumber = string.IsNullOrWhiteSpace(rawRef) ? "CSV" : rawRef.Trim(),
                Amount = amount,
                SuggestedStatus = TransactionStatus.Cleared
            });
        }

        return items;
    }

    public static (string FileName, string Content) GetSampleStatementPayload(string format)
    {
        return format.ToUpperInvariant() switch
        {
            "QIF" => ("chase_checking_nov2026.qif", """
                !Type:Bank
                D11/13/2026
                T-138.45
                PWHOLEFDS MKT #10248 SEATTLE
                MPOS DEBIT WHOLE FOODS MARKET
                NPOS-334
                C*
                ^
                D11/14/2026
                T-18.75
                PSQ *BLUE BOTTLE COFFEE #04
                MMorning espresso & pastry
                NPOS-910
                C*
                ^
                D11/14/2026
                T-64.20
                PAMZN MKTP US*849201 WA
                MUSB-C Dock & Reference cable
                NACH-541
                C*
                ^
                D11/15/2026
                T-52.40
                PSHELL OIL 574429810
                MPremium unleaded fuel
                NPOS-914
                ^
                """),

            "CSV" => ("statement_export_nov2026.csv", """
                Date,Payee,Amount,Memo,Reference
                2026-11-13,"WHOLEFDS MKT #10248",-138.45,"Fresh produce, poultry & bakery",POS-334
                2026-11-14,"SONIC.NET*FIBER GIGABIT",-79.99,"November gigabit fiber service",ACH-992
                2026-11-14,"SQ *BLUE BOTTLE COFFEE",-22.50,"Client coffee catchup",POS-918
                2026-11-15,"TRADER JOE'S #142",-88.90,"Weekly pantry & dairy restock",POS-922
                2026-11-15,"GRACE SOVEREIGN*PAYROLL DD",3650.00,"Mid-month net salary direct deposit",DD-955
                """),

            _ => ("chase_private_client_nov2026.ofx", """
                OFXHEADER:100
                DATA:OFXSGML
                VERSION:102
                <OFX>
                  <BANKMSGSRSV1>
                    <STMTTRNRS>
                      <STMTRS>
                        <BANKTRANLIST>
                          <STMTTRN>
                            <TRNTYPE>DEBIT
                            <DTPOSTED>20261113120000
                            <TRNAMT>-138.45
                            <FITID>POS-334
                            <NAME>WHOLEFDS MKT #10248
                            <MEMO>Fresh produce, poultry & bakery
                          </STMTTRN>
                          <STMTTRN>
                            <TRNTYPE>DEBIT
                            <DTPOSTED>20261114120000
                            <TRNAMT>-79.99
                            <FITID>ACH-992
                            <NAME>SONIC.NET*FIBER TELECOM
                            <MEMO>November gigabit fiber service
                          </STMTTRN>
                          <STMTTRN>
                            <TRNTYPE>DEBIT
                            <DTPOSTED>20261114153000
                            <TRNAMT>-19.50
                            <FITID>OFX-20261114-01
                            <NAME>SQ *BLUE BOTTLE COFFEE
                            <MEMO>Afternoon roast & cortado
                          </STMTTRN>
                          <STMTTRN>
                            <TRNTYPE>DEBIT
                            <DTPOSTED>20261115091500
                            <TRNAMT>-114.20
                            <FITID>OFX-20261115-02
                            <NAME>COSTCO WHSE #0482
                            <MEMO>Household bulk stockup
                          </STMTTRN>
                          <STMTTRN>
                            <TRNTYPE>DEBIT
                            <DTPOSTED>20261115114500
                            <TRNAMT>-49.99
                            <FITID>OFX-20261115-03
                            <NAME>AMZN MKTP US*99104
                            <MEMO>Clean Architecture book & stand
                          </STMTTRN>
                          <STMTTRN>
                            <TRNTYPE>CREDIT
                            <DTPOSTED>20261115080000
                            <TRNAMT>3650.00
                            <FITID>OFX-20261115-04
                            <NAME>GRACE SOVEREIGN*PAYROLL
                            <MEMO>Bi-weekly net salary direct deposit
                          </STMTTRN>
                        </BANKTRANLIST>
                      </STMTRS>
                    </STMTTRNRS>
                  </BANKMSGSRSV1>
                </OFX>
                """)
        };
    }

    private static string ExtractOfxTag(string block, string tagName)
    {
        var match = Regex.Match(
            block,
            $"<{tagName}>([^<\\r\\n]+)",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static string NormalizeOfxDate(string raw)
    {
        string digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length >= 8)
        {
            string y = digits[..4];
            string m = digits[4..6];
            string d = digits[6..8];
            return $"{y}-{m}-{d}";
        }
        return DateTime.Today.ToString("yyyy-MM-dd");
    }

    private static string NormalizeFlexibleDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        string cleaned = raw.Trim().Replace("'", "/");
        if (DateTime.TryParse(cleaned, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
            DateTime.TryParse(cleaned, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
        {
            return dt.ToString("yyyy-MM-dd");
        }

        return DateTime.Today.ToString("yyyy-MM-dd");
    }

    private static int FindColumnIndex(List<string> headers, string[] candidates, int fallback)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            foreach (string c in candidates)
            {
                if (headers[i].Equals(c, StringComparison.OrdinalIgnoreCase) ||
                    headers[i].Contains(c, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }
        return fallback;
    }

    private static string GetCol(List<string> cols, int index)
    {
        return index >= 0 && index < cols.Count ? cols[index] : string.Empty;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        result.Add(current.ToString().Trim());
        return result;
    }
}
