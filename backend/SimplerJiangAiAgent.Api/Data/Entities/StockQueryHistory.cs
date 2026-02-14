namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class StockQueryHistory
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal TurnoverRate { get; set; }
    public decimal PeRatio { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Speed { get; set; }
    public DateTime UpdatedAt { get; set; }
}
