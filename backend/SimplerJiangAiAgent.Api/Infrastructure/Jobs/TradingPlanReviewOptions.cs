namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public sealed class TradingPlanReviewOptions
{
    public const string SectionName = "TradingPlanReview";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 45;
    public int LookbackMinutes { get; set; } = 120;
    public int MaxPlansPerPass { get; set; } = 80;
    public int MaxNewsPerSymbol { get; set; } = 8;
    public int ThreatConfidenceThreshold { get; set; } = 70;
    public string LlmProvider { get; set; } = "active";
    public string LlmModel { get; set; } = "gemini-3.1-flash-lite-preview-thinking-high";
}