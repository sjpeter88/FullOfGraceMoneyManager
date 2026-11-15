namespace FullofGraceMoneyManager.Desktop.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string CategoryType { get; set; } = "Expense"; // Income, Expense, Transfer
    public string ColorHex { get; set; } = "#38BDF8";
    public string Icon { get; set; } = "Tag";
    public decimal MonthlyBudget { get; set; }

    public string FullPath => string.IsNullOrWhiteSpace(ParentName)
        ? Name
        : $"{ParentName}:{Name}";
}
