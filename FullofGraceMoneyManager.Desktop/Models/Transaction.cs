namespace FullofGraceMoneyManager.Desktop.Models;

public static class TransactionStatus
{
    public const string Uncleared = "Uncleared";
    public const string Cleared = "Cleared";
    public const string Reconciled = "Reconciled";

    public static string NextStatus(string current) => current switch
    {
        Uncleared => Cleared,
        Cleared => Reconciled,
        Reconciled => Uncleared,
        _ => Cleared
    };
}

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string TransactionDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string? ReferenceNumber { get; set; }
    public int? PayeeId { get; set; }
    public string PayeeName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = "Uncategorized";
    public string? Memo { get; set; }

    /// <summary>
    /// Comma-separated #tags for Phase 4C multi-criteria ledger filtering (e.g. "#TaxDeductible,#Ministry").
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Optional receipt or invoice document reference (e.g. "RCPT-2026-108.PDF" or "INV-412.PDF").
    /// </summary>
    public string? ReceiptRef { get; set; }

    /// <summary>
    /// Optional external bank FITID for statement deduplication and CSV export.
    /// </summary>
    public string? ExternalFitId { get; set; }

    public List<string> TagList => string.IsNullOrWhiteSpace(Tags)
        ? new List<string>()
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .Select(t => t.StartsWith('#') ? t : "#" + t)
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .ToList();

    /// <summary>
    /// Signed amount: positive for deposits/inflows, negative for payments/outflows.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Uncleared, Cleared, or Reconciled.
    /// </summary>
    public string Status { get; set; } = TransactionStatus.Uncleared;

    public bool IsSplit { get; set; }
    public List<Split> Splits { get; set; } = new();

    /// <summary>
    /// Positive payment value if Amount &lt; 0, else 0.
    /// </summary>
    public decimal Payment => Amount < 0 ? Math.Abs(Amount) : 0m;

    /// <summary>
    /// Positive deposit value if Amount &gt; 0, else 0.
    /// </summary>
    public decimal Deposit => Amount > 0 ? Amount : 0m;

    public decimal? PaymentAmount => Amount < 0 ? Math.Abs(Amount) : null;
    public decimal? DepositAmount => Amount > 0 ? Amount : null;

    /// <summary>
    /// Computed chronologically in the ledger register view.
    /// </summary>
    public decimal RunningBalance { get; set; }
}
