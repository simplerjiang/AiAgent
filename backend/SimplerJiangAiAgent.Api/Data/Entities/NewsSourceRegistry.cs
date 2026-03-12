namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class NewsSourceRegistry
{
    public long Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Tier { get; set; } = NewsSourceTier.Fallback;
    public string Status { get; set; } = NewsSourceStatus.Pending;
    public string FetchStrategy { get; set; } = "html";
    public string ParserVersion { get; set; } = "v1";
    public decimal? QualityScore { get; set; }
    public decimal? ParseSuccessRate { get; set; }
    public decimal? TimestampCoverage { get; set; }
    public int? FreshnessLagMinutes { get; set; }
    public int ConsecutiveFailures { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public string? LastStatusReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<NewsSourceHealthDaily> HealthDailies { get; set; } = new();
}

public static class NewsSourceTier
{
    public const string Authoritative = "authoritative";
    public const string Preferred = "preferred";
    public const string Fallback = "fallback";
    public const string Blocked = "blocked";
}

public static class NewsSourceStatus
{
    public const string Pending = "pending";
    public const string Active = "active";
    public const string Quarantine = "quarantine";
    public const string Rejected = "rejected";
}