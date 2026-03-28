namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum ResearchDebateSide { Bull, Bear, Manager }

public sealed class ResearchDebateMessage
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public long TurnId { get; set; }
    public long StageId { get; set; }
    public ResearchDebateSide Side { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public int RoundIndex { get; set; }

    /// <summary>Core claim or counter-argument made in this message.</summary>
    public string Claim { get; set; } = string.Empty;

    /// <summary>JSON array of evidence reference keys supporting the claim.</summary>
    public string? SupportingEvidenceRefsJson { get; set; }

    /// <summary>Role ID of the speaker being countered, null on first round.</summary>
    public string? CounterTargetRole { get; set; }

    /// <summary>JSON array of specific counter-points against the target.</summary>
    public string? CounterPointsJson { get; set; }

    /// <summary>JSON array of open questions raised for next round.</summary>
    public string? OpenQuestionsJson { get; set; }

    public string? LlmTraceId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ResearchSession Session { get; set; } = null!;
    public ResearchStageSnapshot Stage { get; set; } = null!;
}
