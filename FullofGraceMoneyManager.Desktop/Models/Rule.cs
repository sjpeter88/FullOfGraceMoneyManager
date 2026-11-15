namespace FullofGraceMoneyManager.Desktop.Models;

public class CategorizationRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;

    // IF Conditions
    public string MatchField { get; set; } = "Payee"; // Payee, Memo, PayeeOrMemo
    public string MatchOperator { get; set; } = "Contains"; // Contains, StartsWith, Equals, Regex
    public string MatchPattern { get; set; } = string.Empty;
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int? TargetAccountId { get; set; }

    // THEN Actions
    public string? RenamePayeeTo { get; set; }
    public int? AssignCategoryId { get; set; }
    public string AssignCategoryName { get; set; } = "Uncategorized";
    public string? SetStatus { get; set; }
    public string? AppendMemo { get; set; }
}
