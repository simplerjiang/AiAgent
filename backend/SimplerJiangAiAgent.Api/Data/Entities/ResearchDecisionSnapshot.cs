namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class ResearchDecisionSnapshot
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public long TurnId { get; set; }
    public long? SupersededByDecisionId { get; set; }
    public string? Rating { get; set; }
    public string? Action { get; set; }
    public string? ExecutiveSummary { get; set; }
    public string? InvestmentThesis { get; set; }
    public string? FinalDecisionJson { get; set; }
    public string? RiskConsensus { get; set; }
    public string? DissentJson { get; set; }
    public string? NextActionsJson { get; set; }
    public string? InvalidationConditionsJson { get; set; }
    public string? SupportingEvidenceJson { get; set; }
    public string? CounterEvidenceJson { get; set; }
    public string? ConfidenceExplanation { get; set; }
    public decimal? Confidence { get; set; }
    public DateTime CreatedAt { get; set; }

    public ResearchSession Session { get; set; } = null!;
}
