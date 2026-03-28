namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class ResearchManagerVerdict
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public long TurnId { get; set; }
    public long StageId { get; set; }
    public int RoundIndex { get; set; }

    /// <summary>JSON array of adopted bull points.</summary>
    public string? AdoptedBullPointsJson { get; set; }

    /// <summary>JSON array of adopted bear points.</summary>
    public string? AdoptedBearPointsJson { get; set; }

    /// <summary>JSON array of disputes shelved for future investigation.</summary>
    public string? ShelvedDisputesJson { get; set; }

    /// <summary>Structured research conclusion by the manager.</summary>
    public string? ResearchConclusion { get; set; }

    /// <summary>Draft investment plan proposed to the trader stage.</summary>
    public string? InvestmentPlanDraftJson { get; set; }

    /// <summary>True when the manager declares the debate CONVERGED.</summary>
    public bool IsConverged { get; set; }

    public string? LlmTraceId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ResearchSession Session { get; set; } = null!;
    public ResearchStageSnapshot Stage { get; set; } = null!;
}
