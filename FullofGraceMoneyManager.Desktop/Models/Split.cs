namespace FullofGraceMoneyManager.Desktop.Models;

public class Split
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = "Uncategorized";
    public string? Memo { get; set; }
    public decimal Amount { get; set; }
    public int? TransferAccountId { get; set; }
}
