namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum ReviewType { Daily = 1, Weekly = 2, Monthly = 3, Custom = 4 }

public sealed class TradeReview
{
    public long Id { get; set; }
    public ReviewType ReviewType { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TradeCount { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal WinRate { get; set; }
    public decimal ComplianceRate { get; set; }
    public string ReviewContent { get; set; } = string.Empty;
    public string? ContextSummaryJson { get; set; }
    public string? LlmTraceId { get; set; }
    public DateTime CreatedAt { get; set; }
}
