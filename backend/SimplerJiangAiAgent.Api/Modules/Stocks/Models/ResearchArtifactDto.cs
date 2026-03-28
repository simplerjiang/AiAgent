namespace SimplerJiangAiAgent.Api.Modules.Stocks.Models;

// ── Debate messages ──────────────────────────────────────────────────

public sealed record ResearchDebateMessageDto(
    long Id,
    string Side,
    string RoleId,
    int RoundIndex,
    string Claim,
    string? SupportingEvidenceRefsJson,
    string? CounterTargetRole,
    string? CounterPointsJson,
    string? OpenQuestionsJson,
    string? LlmTraceId,
    DateTime CreatedAt);

// ── Manager verdict ──────────────────────────────────────────────────

public sealed record ResearchManagerVerdictDto(
    long Id,
    int RoundIndex,
    string? AdoptedBullPointsJson,
    string? AdoptedBearPointsJson,
    string? ShelvedDisputesJson,
    string? ResearchConclusion,
    string? InvestmentPlanDraftJson,
    bool IsConverged,
    string? LlmTraceId,
    DateTime CreatedAt);

// ── Trader proposal ──────────────────────────────────────────────────

public sealed record ResearchTraderProposalDto(
    long Id,
    int Version,
    string Status,
    string? Direction,
    string? EntryPlanJson,
    string? ExitPlanJson,
    string? PositionSizingJson,
    string? Rationale,
    long? SupersededByProposalId,
    string? LlmTraceId,
    DateTime CreatedAt);

// ── Risk assessment ──────────────────────────────────────────────────

public sealed record ResearchRiskAssessmentDto(
    long Id,
    string RoleId,
    string Tier,
    int RoundIndex,
    string? RiskLimitsJson,
    string? InvalidationsJson,
    string? ProposalAssessment,
    string? AnalysisContent,
    long? ResponseToArtifactId,
    string? LlmTraceId,
    DateTime CreatedAt);

// ── Aggregate per-turn structured artifacts ──────────────────────────

public sealed record ResearchTurnArtifactsDto(
    long TurnId,
    IReadOnlyList<ResearchDebateMessageDto> DebateMessages,
    IReadOnlyList<ResearchManagerVerdictDto> ManagerVerdicts,
    IReadOnlyList<ResearchTraderProposalDto> TraderProposals,
    IReadOnlyList<ResearchRiskAssessmentDto> RiskAssessments);
