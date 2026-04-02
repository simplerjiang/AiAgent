namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum RecommendSessionStatus
{
    Idle,
    Running,
    Degraded,
    Completed,
    Failed,
    Closed,
    TimedOut
}

public sealed class RecommendationSession
{
    public long Id { get; set; }
    public string SessionKey { get; set; } = string.Empty;
    public RecommendSessionStatus Status { get; set; }
    public long? ActiveTurnId { get; set; }
    public string? LastUserIntent { get; set; }
    public string? MarketSentiment { get; set; }
    public string? TopSectorsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<RecommendationTurn> Turns { get; set; } = new List<RecommendationTurn>();
}
