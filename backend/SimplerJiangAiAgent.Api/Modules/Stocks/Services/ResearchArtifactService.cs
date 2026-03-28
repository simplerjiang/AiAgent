using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IResearchArtifactService
{
    Task<ResearchTurnArtifactsDto?> GetTurnArtifactsAsync(long turnId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResearchDebateMessageDto>> GetDebateHistoryAsync(long sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResearchTraderProposalDto>> GetProposalHistoryAsync(long sessionId, CancellationToken cancellationToken = default);
}

public sealed class ResearchArtifactService : IResearchArtifactService
{
    private readonly AppDbContext _dbContext;

    public ResearchArtifactService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResearchTurnArtifactsDto?> GetTurnArtifactsAsync(long turnId, CancellationToken cancellationToken = default)
    {
        var turn = await _dbContext.ResearchTurns
            .AsNoTracking()
            .AnyAsync(t => t.Id == turnId, cancellationToken);

        if (!turn) return null;

        var debates = await _dbContext.ResearchDebateMessages
            .AsNoTracking()
            .Where(d => d.TurnId == turnId)
            .OrderBy(d => d.RoundIndex).ThenBy(d => d.CreatedAt)
            .Select(d => new ResearchDebateMessageDto(
                d.Id, d.Side.ToString(), d.RoleId, d.RoundIndex,
                d.Claim, d.SupportingEvidenceRefsJson, d.CounterTargetRole,
                d.CounterPointsJson, d.OpenQuestionsJson, d.LlmTraceId, d.CreatedAt))
            .ToArrayAsync(cancellationToken);

        var verdicts = await _dbContext.ResearchManagerVerdicts
            .AsNoTracking()
            .Where(v => v.TurnId == turnId)
            .OrderBy(v => v.RoundIndex)
            .Select(v => new ResearchManagerVerdictDto(
                v.Id, v.RoundIndex, v.AdoptedBullPointsJson, v.AdoptedBearPointsJson,
                v.ShelvedDisputesJson, v.ResearchConclusion, v.InvestmentPlanDraftJson,
                v.IsConverged, v.LlmTraceId, v.CreatedAt))
            .ToArrayAsync(cancellationToken);

        var proposals = await _dbContext.ResearchTraderProposals
            .AsNoTracking()
            .Where(p => p.TurnId == turnId)
            .OrderBy(p => p.Version)
            .Select(p => new ResearchTraderProposalDto(
                p.Id, p.Version, p.Status.ToString(), p.Direction,
                p.EntryPlanJson, p.ExitPlanJson, p.PositionSizingJson,
                p.Rationale, p.SupersededByProposalId, p.LlmTraceId, p.CreatedAt))
            .ToArrayAsync(cancellationToken);

        var risks = await _dbContext.ResearchRiskAssessments
            .AsNoTracking()
            .Where(r => r.TurnId == turnId)
            .OrderBy(r => r.RoundIndex).ThenBy(r => r.RoleId)
            .Select(r => new ResearchRiskAssessmentDto(
                r.Id, r.RoleId, r.Tier.ToString(), r.RoundIndex,
                r.RiskLimitsJson, r.InvalidationsJson, r.ProposalAssessment,
                r.AnalysisContent, r.ResponseToArtifactId, r.LlmTraceId, r.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return new ResearchTurnArtifactsDto(turnId, debates, verdicts, proposals, risks);
    }

    public async Task<IReadOnlyList<ResearchDebateMessageDto>> GetDebateHistoryAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResearchDebateMessages
            .AsNoTracking()
            .Where(d => d.SessionId == sessionId)
            .OrderBy(d => d.TurnId).ThenBy(d => d.RoundIndex).ThenBy(d => d.CreatedAt)
            .Select(d => new ResearchDebateMessageDto(
                d.Id, d.Side.ToString(), d.RoleId, d.RoundIndex,
                d.Claim, d.SupportingEvidenceRefsJson, d.CounterTargetRole,
                d.CounterPointsJson, d.OpenQuestionsJson, d.LlmTraceId, d.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResearchTraderProposalDto>> GetProposalHistoryAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResearchTraderProposals
            .AsNoTracking()
            .Where(p => p.SessionId == sessionId)
            .OrderBy(p => p.TurnId).ThenBy(p => p.Version)
            .Select(p => new ResearchTraderProposalDto(
                p.Id, p.Version, p.Status.ToString(), p.Direction,
                p.EntryPlanJson, p.ExitPlanJson, p.PositionSizingJson,
                p.Rationale, p.SupersededByProposalId, p.LlmTraceId, p.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }
}
