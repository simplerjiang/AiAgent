using System.Text.Json.Serialization;

namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum RecommendTurnStatus
{
    Draft,
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum RecommendContinuationMode
{
    NewSession,
    PartialRerun,
    FullRerun,
    WorkbenchHandoff,
    DirectAnswer
}

public sealed class RecommendationTurn
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public int TurnIndex { get; set; }
    public string UserPrompt { get; set; } = string.Empty;
    public RecommendTurnStatus Status { get; set; }
    public RecommendContinuationMode ContinuationMode { get; set; }
    public string? RoutingDecision { get; set; }
    public string? RoutingReasoning { get; set; }
    public decimal? RoutingConfidence { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [JsonIgnore]
    public RecommendationSession Session { get; set; } = null!;
    public ICollection<RecommendationStageSnapshot> StageSnapshots { get; set; } = new List<RecommendationStageSnapshot>();
    public ICollection<RecommendationFeedItem> FeedItems { get; set; } = new List<RecommendationFeedItem>();
}
