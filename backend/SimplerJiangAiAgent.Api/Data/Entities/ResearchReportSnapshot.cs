namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class ResearchReportSnapshot
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public long TurnId { get; set; }
    public long? TriggeredByStageId { get; set; }
    public int VersionIndex { get; set; }
    public bool IsFinal { get; set; }
    public string? ReportBlocksJson { get; set; }
    public DateTime CreatedAt { get; set; }

    public ResearchSession Session { get; set; } = null!;
}
