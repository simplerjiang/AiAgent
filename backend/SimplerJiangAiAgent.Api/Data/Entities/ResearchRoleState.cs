namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum ResearchRoleStatus
{
    Pending,
    Running,
    Completed,
    Degraded,
    Blocked,
    Failed,
    Skipped
}

public sealed class ResearchRoleState
{
    public long Id { get; set; }
    public long StageId { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public int RunIndex { get; set; }
    public ResearchRoleStatus Status { get; set; }
    public string? ToolPolicyClass { get; set; }
    public string? InputRefsJson { get; set; }
    public string? OutputRefsJson { get; set; }
    public string? OutputContentJson { get; set; }
    public string? DegradedFlagsJson { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? LlmTraceId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ResearchStageSnapshot Stage { get; set; } = null!;
}
