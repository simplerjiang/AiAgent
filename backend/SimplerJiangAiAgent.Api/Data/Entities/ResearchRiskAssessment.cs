namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum RiskAnalystTier { Aggressive, Neutral, Conservative }

public sealed class ResearchRiskAssessment
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public long TurnId { get; set; }
    public long StageId { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public RiskAnalystTier Tier { get; set; }
    public int RoundIndex { get; set; }

    /// <summary>JSON array of risk limits recommended by this analyst.</summary>
    public string? RiskLimitsJson { get; set; }

    /// <summary>JSON array of invalidation conditions that would cancel the thesis.</summary>
    public string? InvalidationsJson { get; set; }

    /// <summary>Assessment of the trader proposal — accept/reject/modify.</summary>
    public string? ProposalAssessment { get; set; }

    /// <summary>Free-form analysis content.</summary>
    public string? AnalysisContent { get; set; }

    /// <summary>ID of a prior artifact (debate message, proposal) being responded to.</summary>
    public long? ResponseToArtifactId { get; set; }

    public string? LlmTraceId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ResearchSession Session { get; set; } = null!;
    public ResearchStageSnapshot Stage { get; set; } = null!;
}
