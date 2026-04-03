namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class StockPosition
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int QuantityLots { get; set; }
    public decimal AverageCostPrice { get; set; }
    public decimal TotalCost { get; set; }
    public decimal? LatestPrice { get; set; }
    public decimal? MarketValue { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public decimal? UnrealizedReturnRate { get; set; }
    public decimal? PositionRatio { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
}
