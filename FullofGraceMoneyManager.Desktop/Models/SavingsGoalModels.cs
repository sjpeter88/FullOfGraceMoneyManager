namespace FullofGraceMoneyManager.Desktop.Models;

/// <summary>
/// Represents a dedicated savings goal or sinking fund tracked in SQLite (Phase 4B).
/// </summary>
public class SavingsGoal
{
    public int Id { get; set; }
    public int AccountId { get; set; } = 2;
    public string AccountName { get; set; } = "High-Yield Savings ••9014";
    public string Name { get; set; } = string.Empty;
    public string CategoryGroup { get; set; } = "Emergency & Reserves";
    public string IconGlyph { get; set; } = "🛡️";
    public string ColorHex { get; set; } = "#10B981";
    public decimal TargetAmount { get; set; }
    public decimal CurrentSavedAmount { get; set; }
    public decimal MonthlyContributionTarget { get; set; }
    public string TargetDate { get; set; } = "2027-06-01";
    public int PriorityOrder { get; set; } = 10;
    public string Notes { get; set; } = string.Empty;

    public decimal RemainingAmount => Math.Max(0m, TargetAmount - CurrentSavedAmount);

    public decimal ProgressPercent => TargetAmount > 0m
        ? Math.Min(100m, Math.Max(0m, Math.Round((CurrentSavedAmount / TargetAmount) * 100m, 1)))
        : 100m;

    public int MonthsRemaining { get; set; }
    public decimal RequiredMonthlyToHitTargetDate { get; set; }
    public string ProjectedCompletionDate { get; set; } = string.Empty;
    public string StatusBadge { get; set; } = "On Track";
}

/// <summary>
/// Aggregate summary for the Savings Goals &amp; Sinking Funds Planner workspace (Phase 4B).
/// </summary>
public class SavingsGoalsWorkspaceSummary
{
    public decimal HighYieldSavingsBalance { get; set; }
    public decimal TotalTargetAmount { get; set; }
    public decimal TotalCurrentSaved { get; set; }
    public decimal TotalRemainingToFund => Math.Max(0m, TotalTargetAmount - TotalCurrentSaved);
    public decimal OverallFundedPercent => TotalTargetAmount > 0m
        ? Math.Round((TotalCurrentSaved / TotalTargetAmount) * 100m, 1)
        : 0m;
    public decimal UnallocatedSavingsBuffer => Math.Max(0m, HighYieldSavingsBalance - TotalCurrentSaved);
    public decimal TotalMonthlyContributionPlan { get; set; }
    public decimal TotalRequiredMonthlyForDeadlines { get; set; }
    public int FullyFundedCount { get; set; }
    public int OnTrackCount { get; set; }
    public List<SavingsGoal> Goals { get; set; } = new();
}
