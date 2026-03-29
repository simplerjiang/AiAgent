namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class StockPosition
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int QuantityLots { get; set; }
    public decimal AverageCostPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
}
