using System;
using System.Collections.Generic;

namespace FullofGraceMoneyManager.Desktop.Models;

public sealed class DebtLiabilityProfile
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public string LiabilityName { get; set; } = string.Empty;
    public string LenderInstitution { get; set; } = string.Empty;
    public string LiabilityType { get; set; } = "Mortgage";

    public string Name
    {
        get => LiabilityName;
        set => LiabilityName = value ?? string.Empty;
    }

    public string Institution
    {
        get => LenderInstitution;
        set => LenderInstitution = value ?? string.Empty;
    }

    public string CategoryType
    {
        get => LiabilityType;
        set => LiabilityType = value ?? "Mortgage";
    }

    public decimal CurrentBalance { get; set; }
    public decimal InterestRateApr { get; set; }
    public decimal MinimumMonthlyPayment { get; set; }
    public decimal EscrowAndTaxesMonthly { get; set; }

    public decimal EscrowMonthly
    {
        get => EscrowAndTaxesMonthly;
        set => EscrowAndTaxesMonthly = value;
    }

    public decimal ExtraPrincipalMonthly { get; set; }
    public decimal OriginalPrincipal { get; set; }
    public int PayoffPriorityOrder { get; set; }
    public string Notes { get; set; } = string.Empty;

    public decimal MonthlyInterestCharge => Math.Round(CurrentBalance * (InterestRateApr / 100m / 12m), 2);
    public decimal TotalMonthlyPayment => MinimumMonthlyPayment + ExtraPrincipalMonthly + EscrowAndTaxesMonthly;
    public decimal PrincipalPaidProgressPercent => OriginalPrincipal > 0m
        ? Math.Clamp(Math.Round((OriginalPrincipal - CurrentBalance) / OriginalPrincipal * 100m, 1), 0m, 100m)
        : 0m;

    public int PayoffMonthsBaseline { get; set; }
    public int PayoffMonthsAccelerated { get; set; }
    public int MonthsSaved => Math.Max(0, PayoffMonthsBaseline - PayoffMonthsAccelerated);
    public decimal TotalInterestBaseline { get; set; }
    public decimal TotalInterestAccelerated { get; set; }
    public decimal TotalInterestSaved => Math.Max(0m, TotalInterestBaseline - TotalInterestAccelerated);
    public string ProjectedPayoffDate { get; set; } = string.Empty;
}

public sealed class AmortizationScheduleEntry
{
    public int MonthIndex { get; set; }
    public string MonthDate { get; set; } = string.Empty;
    public decimal StartingBalance { get; set; }
    public decimal ScheduledMinimumPayment { get; set; }
    public decimal ExtraPrincipalPayment { get; set; }
    public decimal InterestPaid { get; set; }
    public decimal PrincipalPaid { get; set; }
    public decimal EndingBalance { get; set; }
    public decimal CumulativeInterestPaid { get; set; }
}

public sealed class DebtPayoffWorkspaceSummary
{
    public string StrategyMode { get; set; } = "Avalanche";
    public string ActiveStrategy => StrategyMode;
    public decimal TotalDebtPrincipal { get; set; }
    public decimal WeightedAverageApr { get; set; }
    public decimal TotalMinimumMonthlyPayment { get; set; }
    public decimal TotalExtraPrincipalMonthly { get; set; }
    public decimal TotalAcceleratedMonthlyPayment => TotalMinimumMonthlyPayment + TotalExtraPrincipalMonthly;
    public decimal BaselineTotalInterest { get; set; }
    public decimal AcceleratedTotalInterest { get; set; }
    public decimal TotalInterestSaved => Math.Max(0m, BaselineTotalInterest - AcceleratedTotalInterest);
    public int BaselineMonthsToDebtFree { get; set; }
    public int AcceleratedMonthsToDebtFree { get; set; }
    public int MonthsSaved => Math.Max(0, BaselineMonthsToDebtFree - AcceleratedMonthsToDebtFree);
    public string ProjectedDebtFreeDate { get; set; } = string.Empty;
    public List<DebtLiabilityProfile> Liabilities { get; set; } = new();
    public List<AmortizationScheduleEntry> AmortizationSchedule { get; set; } = new();
}
