namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum TraderProposalStatus { Draft, Active, Superseded, Withdrawn }

public sealed class ResearchTraderProposal
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public long TurnId { get; set; }
    public long StageId { get; set; }
    public int Version { get; set; }
    public TraderProposalStatus Status { get; set; }

    public string? Direction { get; set; }
    public string? EntryPlanJson { get; set; }
    public string? ExitPlanJson { get; set; }
    public string? PositionSizingJson { get; set; }
    public string? Rationale { get; set; }

    /// <summary>Points to the newer proposal that supersedes this one.</summary>
    public long? SupersededByProposalId { get; set; }

    public string? LlmTraceId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ResearchSession Session { get; set; } = null!;
    public ResearchStageSnapshot Stage { get; set; } = null!;
}
