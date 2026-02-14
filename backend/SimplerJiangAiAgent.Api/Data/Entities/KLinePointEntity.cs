namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class KLinePointEntity
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Interval { get; set; } = "day";
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Volume { get; set; }
}
