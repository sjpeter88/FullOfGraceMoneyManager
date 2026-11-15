using System;
using System.Collections.Generic;

namespace FullofGraceMoneyManager.Desktop.Models;

public sealed class CategoryFinancialBreakdownItem
{
    public string GroupName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#38BDF8";
    public decimal Amount { get; set; }
    public decimal SharePercent { get; set; }
    public int TransactionCount { get; set; }
}

public sealed class PayeeBreakdownItem
{
    public string PayeeName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public int TransactionCount { get; set; }
    public decimal SharePercent { get; set; }
}

public sealed class CharitableStewardshipItem
{
    public int TransactionId { get; set; }
    public string TransactionDate { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class NetWorthTrendPoint
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }
}

public sealed class ExecutiveReportSummary
{
    public string ReportPeriodLabel { get; set; } = "November 2026 • Executive P&L & Stewardship Audit";
    public decimal GrossIncome { get; set; }
    public decimal TotalOperatingExpenses { get; set; }
    public decimal TotalDebtServiceAndTransfers { get; set; }
    public decimal NetOperatingSurplus { get; set; }
    public decimal SavingsRatePercent { get; set; }
    public decimal CharitableGivingTotal { get; set; }
    public decimal CharitableGivingPercentOfIncome { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal CurrentNetWorth { get; set; }
    public decimal DebtToAssetRatioPercent { get; set; }
    public List<CategoryFinancialBreakdownItem> IncomeByCategory { get; set; } = new();
    public List<CategoryFinancialBreakdownItem> ExpenseByCategory { get; set; } = new();
    public List<PayeeBreakdownItem> TopPayees { get; set; } = new();
    public List<CharitableStewardshipItem> StewardshipTaxReceipts { get; set; } = new();
    public List<NetWorthTrendPoint> NetWorthHistory { get; set; } = new();
}
