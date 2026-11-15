namespace FullofGraceMoneyManager.Desktop.Models;

/// <summary>
/// Represents a scheduled recurring bill, subscription, or paycheck used for
/// reminders, 1-click ledger posting, and 30-day cash-flow forecasting.
/// </summary>
public class RecurringSchedule
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = "Uncategorized";
    public string Frequency { get; set; } = "Monthly"; // Weekly, Bi-Weekly, Monthly, Quarterly, Annual
    public string NextDueDate { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsAutoPay { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Memo { get; set; }

    public int DaysUntilDue { get; set; }

    public string DueStatus => DaysUntilDue < 0
        ? "Overdue"
        : DaysUntilDue <= 7
            ? "DueSoon"
            : "Upcoming";

    public decimal MonthlyEquivalentAmount => Frequency switch
    {
        "Weekly" => Math.Round(Amount * 52m / 12m, 2),
        "Bi-Weekly" => Math.Round(Amount * 26m / 12m, 2),
        "Quarterly" => Math.Round(Amount / 3m, 2),
        "Annual" => Math.Round(Amount / 12m, 2),
        _ => Amount
    };
}

/// <summary>
/// Represents a single chronological step in the 30-day running cash-flow projection.
/// </summary>
public class CashFlowProjectionPoint
{
    public int ScheduleId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public decimal DeltaAmount { get; set; }
    public decimal ProjectedCheckingBalance { get; set; }
    public decimal ProjectedLiquidBalance { get; set; }
}

/// <summary>
/// Complete 30-day recurring bill & cash-flow forecast summary.
/// </summary>
public class RecurringCashFlowSummary
{
    public decimal CurrentCheckingBalance { get; set; }
    public decimal CurrentLiquidBalance { get; set; }
    public decimal Next30DaysInflows { get; set; }
    public decimal Next30DaysOutflows { get; set; }
    public decimal Next30DaysNetCashFlow => Next30DaysInflows - Next30DaysOutflows;
    public decimal Projected30DayCheckingBalance { get; set; }
    public decimal LowestProjectedCheckingBalance { get; set; }
    public decimal MonthlyRecurringBillsTotal { get; set; }
    public int DueWithin7DaysCount { get; set; }

    public List<RecurringSchedule> Schedules { get; set; } = new();
    public List<CashFlowProjectionPoint> ProjectionTimeline { get; set; } = new();
}
