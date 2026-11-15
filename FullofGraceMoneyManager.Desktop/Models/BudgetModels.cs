namespace FullofGraceMoneyManager.Desktop.Models;

public class BudgetEnvelopeItem
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ParentGroupName { get; set; } = "General";
    public string ColorHex { get; set; } = "#38BDF8";
    public string MonthKey { get; set; } = "2026-11";
    public decimal MonthlyTarget { get; set; }
    public decimal RolloverFromPriorMonth { get; set; }
    public decimal AssignedAmount { get; set; }
    public decimal ActivityAmount { get; set; }

    public decimal TotalBudgeted => RolloverFromPriorMonth + AssignedAmount;
    public decimal AvailableAmount => RolloverFromPriorMonth + AssignedAmount - ActivityAmount;
    public decimal UnderfundedAmount => MonthlyTarget > AssignedAmount ? MonthlyTarget - AssignedAmount : 0m;

    public decimal FundingProgressPercent => MonthlyTarget > 0
        ? Math.Min(100m, Math.Round(AssignedAmount / MonthlyTarget * 100m, 1))
        : 100m;

    public decimal SpendingProgressPercent => TotalBudgeted > 0
        ? Math.Min(100m, Math.Round(ActivityAmount / TotalBudgeted * 100m, 1))
        : (ActivityAmount > 0 ? 100m : 0m);

    public string StatusState => AvailableAmount < 0
        ? "Overspent"
        : (UnderfundedAmount > 0 ? "Underfunded" : "Funded");
}

public class MonthlyBudgetSummary
{
    public string MonthKey { get; set; } = "2026-11";
    public string MonthDisplayTitle { get; set; } = "November 2026";
    public decimal TotalInflowPool { get; set; }
    public decimal TotalAssigned { get; set; }
    public decimal ReadyToAssign => TotalInflowPool - TotalAssigned;
    public decimal TotalActivitySpent { get; set; }
    public decimal TotalAvailableInEnvelopes { get; set; }
    public decimal TotalUnderfunded { get; set; }
    public int AgeOfMoneyDays { get; set; } = 38;
    public List<BudgetEnvelopeItem> Envelopes { get; set; } = new();
}
