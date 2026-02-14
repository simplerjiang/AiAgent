namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class MinuteLinePointEntity
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeSpan Time { get; set; }
    public decimal Price { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Volume { get; set; }
}
