namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum TradingPlanEventType
{
    Triggered = 1,
    Invalidated = 2,
    VolumeDivergenceWarning = 3
}

public enum TradingPlanEventSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}

public sealed class TradingPlanEvent
{
    public long Id { get; set; }
    public long PlanId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public TradingPlanEventType EventType { get; set; }
    public TradingPlanEventSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal? SnapshotPrice { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime OccurredAt { get; set; }

    public TradingPlan? Plan { get; set; }
}