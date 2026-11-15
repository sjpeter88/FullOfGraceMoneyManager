namespace FullofGraceMoneyManager.Desktop.Models;

public class ReconciliationSessionCommitRequest
{
    public int AccountId { get; set; }
    public string StatementDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public decimal StatementEndingBalance { get; set; }
    public List<int> ReconciledTransactionIds { get; set; } = new();
    public List<int> UncheckedTransactionIds { get; set; } = new();
    public bool CreateAdjustmentIfNeeded { get; set; }
    public decimal AdjustmentAmount { get; set; }
}

public class ReconciliationHistoryItem
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string StatementDate { get; set; } = string.Empty;
    public decimal StatementEndingBalance { get; set; }
    public int ReconciledCount { get; set; }
    public string CompletedAt { get; set; } = string.Empty;
}
