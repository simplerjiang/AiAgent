namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class NewsSourceHealthDaily
{
    public long Id { get; set; }
    public long SourceId { get; set; }
    public DateTime HealthDate { get; set; }
    public decimal ParseSuccessRate { get; set; }
    public decimal TimestampCoverage { get; set; }
    public decimal DuplicateRate { get; set; }
    public int FreshnessLagMinutes { get; set; }
    public int ErrorCount { get; set; }
    public string SuggestedStatus { get; set; } = NewsSourceStatus.Pending;
    public string? SuggestionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public NewsSourceRegistry? Source { get; set; }
}