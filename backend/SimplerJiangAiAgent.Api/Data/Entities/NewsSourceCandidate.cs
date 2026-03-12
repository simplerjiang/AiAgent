namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class NewsSourceCandidate
{
    public long Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string HomepageUrl { get; set; } = string.Empty;
    public string ProposedTier { get; set; } = NewsSourceTier.Fallback;
    public string Status { get; set; } = NewsSourceStatus.Pending;
    public string DiscoveryReason { get; set; } = string.Empty;
    public string FetchStrategy { get; set; } = "html";
    public decimal? VerificationScore { get; set; }
    public decimal? ParseSuccessRate { get; set; }
    public decimal? TimestampCoverage { get; set; }
    public int? FreshnessLagMinutes { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
}