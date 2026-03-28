namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum ResearchSessionStatus
{
    Idle,
    Running,
    Degraded,
    Blocked,
    Completed,
    Failed,
    Closed
}

public sealed class ResearchSession
{
    public long Id { get; set; }
    public string SessionKey { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ResearchSessionStatus Status { get; set; }
    public long? ActiveTurnId { get; set; }
    public string? ActiveStage { get; set; }
    public string? LastUserIntent { get; set; }
    public string? DegradedFlagsJson { get; set; }
    public string? LatestRating { get; set; }
    public string? LatestDecisionHeadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ResearchTurn> Turns { get; set; } = new List<ResearchTurn>();
    public ICollection<ResearchReportSnapshot> Reports { get; set; } = new List<ResearchReportSnapshot>();
    public ICollection<ResearchDecisionSnapshot> Decisions { get; set; } = new List<ResearchDecisionSnapshot>();
}
