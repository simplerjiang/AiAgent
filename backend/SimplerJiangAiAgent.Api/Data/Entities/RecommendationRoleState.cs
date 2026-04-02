using System.Text.Json.Serialization;

namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum RecommendRoleStatus
{
    Pending,
    Running,
    Completed,
    Degraded,
    Failed,
    Skipped
}

public sealed class RecommendationRoleState
{
    public long Id { get; set; }
    public long StageId { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public int RunIndex { get; set; }
    public RecommendRoleStatus Status { get; set; }
    public string? ToolPolicyClass { get; set; }
    public string? InputRefsJson { get; set; }
    public string? OutputRefsJson { get; set; }
    public string? OutputContentJson { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? LlmTraceId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [JsonIgnore]
    public RecommendationStageSnapshot Stage { get; set; } = null!;
}
