namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class MarketIndexSnapshot
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public DateTime Timestamp { get; set; }
}
