namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum ResearchStageType
{
    CompanyOverviewPreflight,
    AnalystTeam,
    ResearchDebate,
    TraderProposal,
    RiskDebate,
    PortfolioDecision
}

public enum ResearchStageStatus
{
    Pending,
    Running,
    Completed,
    Degraded,
    Blocked,
    Failed,
    Skipped
}

public enum ResearchStageExecutionMode
{
    Sequential,
    Parallel,
    Debate
}

public sealed class ResearchStageSnapshot
{
    public long Id { get; set; }
    public long TurnId { get; set; }
    public ResearchStageType StageType { get; set; }
    public int StageRunIndex { get; set; }
    public ResearchStageExecutionMode ExecutionMode { get; set; }
    public ResearchStageStatus Status { get; set; }
    public string? ActiveRoleIdsJson { get; set; }
    public string? Summary { get; set; }
    public string? DegradedFlagsJson { get; set; }
    public string? StopReason { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ResearchTurn Turn { get; set; } = null!;
    public ICollection<ResearchRoleState> RoleStates { get; set; } = new List<ResearchRoleState>();
}
