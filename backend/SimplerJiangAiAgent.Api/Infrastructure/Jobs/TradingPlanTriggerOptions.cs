namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public sealed class TradingPlanTriggerOptions
{
    public const string SectionName = "TradingPlanTrigger";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 30;
    public int MaxPlansPerPass { get; set; } = 200;
    public int DivergenceLookbackMinutes { get; set; } = 30;
}