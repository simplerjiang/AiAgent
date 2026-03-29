namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum ReportBlockType
{
    CompanyOverview,
    Market,
    Social,
    News,
    Fundamentals,
    Shareholder,
    Product,
    ResearchDebate,
    TraderProposal,
    RiskReview,
    PortfolioDecision
}

public enum ReportBlockStatus
{
    Pending,
    Complete,
    Degraded,
    Failed,
    Skipped
}

public enum NextActionType
{
    ViewDailyChart,
    ViewMinuteChart,
    ViewEvidence,
    ViewLocalFacts,
    DraftTradingPlan,
    FollowUpDispute
}

public sealed class ResearchReportBlock
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public long TurnId { get; set; }
    public ResearchTurn? Turn { get; set; }
    public ReportBlockType BlockType { get; set; }
    public int VersionIndex { get; set; }

    /// <summary>Block-level concise headline.</summary>
    public string? Headline { get; set; }

    /// <summary>Block-level narrative summary.</summary>
    public string? Summary { get; set; }

    /// <summary>JSON array of key points extracted from stage output.</summary>
    public string? KeyPointsJson { get; set; }

    /// <summary>JSON array of evidence references supporting the block.</summary>
    public string? EvidenceRefsJson { get; set; }

    /// <summary>JSON array of counter-evidence or contrarian data.</summary>
    public string? CounterEvidenceRefsJson { get; set; }

    /// <summary>JSON array of unresolved disagreements or disputes.</summary>
    public string? DisagreementsJson { get; set; }

    /// <summary>JSON array of risk limits relevant to the block.</summary>
    public string? RiskLimitsJson { get; set; }

    /// <summary>JSON array of invalidation conditions.</summary>
    public string? InvalidationsJson { get; set; }

    /// <summary>JSON array of NextAction contracts recommended by this block.</summary>
    public string? RecommendedActionsJson { get; set; }

    /// <summary>Block completion status (Complete, Degraded, Failed, Skipped).</summary>
    public ReportBlockStatus Status { get; set; }

    /// <summary>JSON array of degraded flag strings when status is Degraded.</summary>
    public string? DegradedFlagsJson { get; set; }

    /// <summary>Human-readable description of missing evidence when degraded.</summary>
    public string? MissingEvidence { get; set; }

    /// <summary>Estimated confidence impact from degradation (e.g. "-0.15").</summary>
    public string? ConfidenceImpact { get; set; }

    /// <summary>Source stage that produced this block.</summary>
    public string? SourceStageType { get; set; }

    /// <summary>Optional link to a specific artifact ID (debate, proposal, risk).</summary>
    public long? SourceArtifactId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ResearchSession Session { get; set; } = null!;
}
