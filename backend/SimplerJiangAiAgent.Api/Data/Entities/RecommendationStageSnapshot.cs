using System.Text.Json.Serialization;

namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum RecommendStageType
{
    MarketScan,
    SectorDebate,
    StockPicking,
    StockDebate,
    FinalDecision
}

public enum RecommendStageStatus
{
    Pending,
    Running,
    Completed,
    Degraded,
    Failed,
    Skipped
}

public enum RecommendStageExecutionMode
{
    Sequential,
    Parallel,
    Debate
}

public sealed class RecommendationStageSnapshot
{
    public long Id { get; set; }
    public long TurnId { get; set; }
    public RecommendStageType StageType { get; set; }
    public int StageRunIndex { get; set; }
    public RecommendStageExecutionMode ExecutionMode { get; set; }
    public RecommendStageStatus Status { get; set; }
    public string? ActiveRoleIdsJson { get; set; }
    public string? Summary { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [JsonIgnore]
    public RecommendationTurn Turn { get; set; } = null!;
    public ICollection<RecommendationRoleState> RoleStates { get; set; } = new List<RecommendationRoleState>();
}
