namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class NewsSourceVerificationRun
{
    public long Id { get; set; }
    public string? TraceId { get; set; }
    public long? SourceId { get; set; }
    public long? CandidateId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int HttpStatusCode { get; set; }
    public decimal ParseSuccessRate { get; set; }
    public decimal TimestampCoverage { get; set; }
    public decimal DuplicateRate { get; set; }
    public decimal ContentDepth { get; set; }
    public decimal CrossSourceAgreement { get; set; }
    public int FreshnessLagMinutes { get; set; }
    public decimal VerificationScore { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}