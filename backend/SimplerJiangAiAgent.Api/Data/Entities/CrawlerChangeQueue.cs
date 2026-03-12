namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class CrawlerChangeQueue
{
    public long Id { get; set; }
    public string? TraceId { get; set; }
    public long SourceId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Status { get; set; } = CrawlerChangeStatus.Pending;
    public string TriggerReason { get; set; } = string.Empty;
    public string? ProposedFilesJson { get; set; }
    public string? ProposedPatchJson { get; set; }
    public string? ProposedPatchSummary { get; set; }
    public string? ProposedTestCommand { get; set; }
    public string? ProposedReplayCommand { get; set; }
    public string? ValidationNote { get; set; }
    public string? DeploymentBackupJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public static class CrawlerChangeStatus
{
    public const string Pending = "pending";
    public const string Generated = "generated";
    public const string Validated = "validated";
    public const string Rejected = "rejected";
    public const string Deployed = "deployed";
    public const string RolledBack = "rolled_back";
}