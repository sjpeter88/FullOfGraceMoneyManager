namespace FullofGraceMoneyManager.Desktop.Models;

/// <summary>
/// Represents an individual security position (ETF, mutual fund, stock, bond, or money market)
/// inside an investment or retirement account.
/// </summary>
public class InvestmentHolding
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string TickerSymbol { get; set; } = string.Empty;
    public string SecurityName { get; set; } = string.Empty;
    public string AssetClass { get; set; } = "US Equity";
    public decimal Shares { get; set; }
    public decimal AverageCostPerShare { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DayChangePercent { get; set; }
    public decimal DividendYieldPercent { get; set; }
    public decimal ExpenseRatioPercent { get; set; } = 0.03m;
    public string LastUpdated { get; set; } = string.Empty;

    public decimal CostBasis => Math.Round(Shares * AverageCostPerShare, 2);
    public decimal MarketValue => Math.Round(Shares * CurrentPrice, 2);
    public decimal UnrealizedGainLoss => MarketValue - CostBasis;

    public decimal UnrealizedGainLossPercent => CostBasis > 0m
        ? Math.Round(UnrealizedGainLoss / CostBasis * 100m, 2)
        : 0m;

    public decimal DayChangeDollar => Math.Round(MarketValue * (DayChangePercent / 100m), 2);
    public decimal EstimatedAnnualIncome => Math.Round(MarketValue * (DividendYieldPercent / 100m), 2);
    public decimal PortfolioWeightPercent { get; set; }
}

/// <summary>
/// Represents an asset class slice in the portfolio allocation & target rebalancing model.
/// </summary>
public class AssetAllocationItem
{
    public string AssetClass { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#38BDF8";
    public decimal MarketValue { get; set; }
    public decimal WeightPercent { get; set; }
    public decimal TargetWeightPercent { get; set; }
    public decimal DriftPercent => Math.Round(WeightPercent - TargetWeightPercent, 1);
}

/// <summary>
/// Complete investment portfolio snapshot across all or selected investment accounts.
/// </summary>
public class InvestmentPortfolioSummary
{
    public decimal TotalMarketValue { get; set; }
    public decimal TotalCostBasis { get; set; }
    public decimal TotalUnrealizedGainLoss => TotalMarketValue - TotalCostBasis;

    public decimal TotalUnrealizedGainLossPercent => TotalCostBasis > 0m
        ? Math.Round(TotalUnrealizedGainLoss / TotalCostBasis * 100m, 2)
        : 0m;

    public decimal TotalDayChangeDollar { get; set; }
    public decimal TotalDayChangePercent { get; set; }
    public decimal EstimatedAnnualDividends { get; set; }
    public decimal WeightedDividendYieldPercent { get; set; }
    public decimal WeightedExpenseRatioPercent { get; set; }

    public List<InvestmentHolding> Holdings { get; set; } = new();
    public List<AssetAllocationItem> AssetAllocation { get; set; } = new();
}
