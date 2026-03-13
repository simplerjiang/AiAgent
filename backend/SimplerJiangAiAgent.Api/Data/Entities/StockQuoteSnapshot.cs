namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class StockQuoteSnapshot
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal PeRatio { get; set; }
    public decimal FloatMarketCap { get; set; }
    public decimal VolumeRatio { get; set; }
    public int? ShareholderCount { get; set; }
    public string? SectorName { get; set; }
    public DateTime Timestamp { get; set; }
}
