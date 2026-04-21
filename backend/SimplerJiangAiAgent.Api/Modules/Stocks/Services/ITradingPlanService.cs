using System.Text.Json;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Infrastructure.Jobs;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface ITradingPlanService
{
    Task<IReadOnlyList<TradingPlan>> GetListAsync(string? symbol, int take = 20, CancellationToken cancellationToken = default);
    Task<TradingPlan?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<TradingPlanSaveResult> CreateAsync(TradingPlanCreateDto request, CancellationToken cancellationToken = default);
    Task<TradingPlan?> UpdateAsync(long id, TradingPlanUpdateDto request, CancellationToken cancellationToken = default);
    Task<TradingPlan?> CancelAsync(long id, CancellationToken cancellationToken = default);
    Task<TradingPlan?> ResumeAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public sealed record TradingPlanSaveResult(TradingPlan Plan, bool WatchlistEnsured);

public sealed class TradingPlanService : ITradingPlanService
{
    private static readonly TimeZoneInfo ChinaTimeZone = ResolveChinaTimeZone();
    private static readonly Expression<Func<TradingPlan, bool>> RenderablePlanPredicate = item =>
        item.Symbol != null
        && item.Symbol.Trim() != string.Empty
        && item.Name != null
        && item.Name.Trim() != string.Empty;
    private static readonly Expression<Func<TradingPlan, bool>> NonTerminalPlanWithEndDatePredicate = item =>
        item.Status != TradingPlanStatus.Invalid
        && item.Status != TradingPlanStatus.Cancelled
        && item.PlanEndDate != null;
    private readonly AppDbContext _dbContext;
    private readonly IActiveWatchlistService _watchlistService;
    private readonly IStockMarketContextService _marketContextService;

    public TradingPlanService(AppDbContext dbContext, IActiveWatchlistService watchlistService, IStockMarketContextService marketContextService)
    {
        _dbContext = dbContext;
        _watchlistService = watchlistService;
        _marketContextService = marketContextService;
    }

    public async Task<IReadOnlyList<TradingPlan>> GetListAsync(string? symbol, int take = 20, CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = string.IsNullOrWhiteSpace(symbol) ? null : StockSymbolNormalizer.Normalize(symbol);
        await NormalizeExpiredPlansAsync(normalizedSymbol, null, cancellationToken);

        var query = _dbContext.TradingPlans
            .AsNoTracking()
            .Where(RenderablePlanPredicate)
            .OrderByDescending(item => item.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedSymbol))
        {
            query = query.Where(item => item.Symbol == normalizedSymbol);
        }

        return await query
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<TradingPlan?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await NormalizeExpiredPlansAsync(null, id, cancellationToken);

        return await _dbContext.TradingPlans
            .AsNoTracking()
            .Where(RenderablePlanPredicate)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<TradingPlanSaveResult> CreateAsync(TradingPlanCreateDto request, CancellationToken cancellationToken = default)
    {
        var normalized = StockSymbolNormalizer.Normalize(request.Symbol);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("symbol 不能为空", nameof(request.Symbol));
        }

        var normalizedAnalysisHistoryId = request.AnalysisHistoryId is > 0
            ? request.AnalysisHistoryId.Value
            : (long?)null;

        StockAgentAnalysisHistory? history = null;
        if (normalizedAnalysisHistoryId.HasValue)
        {
            history = await _dbContext.StockAgentAnalysisHistories
                .FirstOrDefaultAsync(item => item.Id == normalizedAnalysisHistoryId.Value, cancellationToken);
            if (history is null)
            {
                throw new InvalidOperationException("分析历史不存在");
            }

            if (!string.Equals(history.Symbol, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("分析历史与当前股票不匹配");
            }
        }

        var now = DateTime.UtcNow;
        var name = NormalizeRequiredName(request.Name, history?.Name ?? string.Empty);
        var marketContext = await _marketContextService.GetLatestAsync(normalized, cancellationToken);
        var plan = new TradingPlan
        {
            PlanKey = GeneratePlanKey(),
            Symbol = normalized,
            Name = name,
            Title = NormalizeLegacyTitle(name),
            Direction = ParseDirection(request.Direction),
            Status = ParseRequestedStatus(request.Status, TradingPlanStatus.Pending),
            TriggerPrice = request.TriggerPrice,
            InvalidPrice = request.InvalidPrice,
            StopLossPrice = request.StopLossPrice,
            TakeProfitPrice = request.TakeProfitPrice,
            TargetPrice = request.TargetPrice,
            ExpectedCatalyst = NormalizeOptional(request.ExpectedCatalyst),
            InvalidConditions = NormalizeOptional(request.InvalidConditions),
            RiskLimits = NormalizeOptional(request.RiskLimits),
            AnalysisSummary = NormalizeOptional(request.AnalysisSummary),
            AnalysisHistoryId = normalizedAnalysisHistoryId,
            SourceAgent = NormalizeOptional(request.SourceAgent) ?? (history is null ? "manual" : "commander"),
            UserNote = NormalizeOptional(request.UserNote),
            ActiveScenario = NormalizeScenarioKey(request.ActiveScenario) ?? "Primary",
            PlanStartDate = request.PlanStartDate,
            PlanEndDate = request.PlanEndDate,
            MarketStageLabelAtCreation = marketContext?.StageLabel,
            StageConfidenceAtCreation = marketContext?.StageConfidence,
            SuggestedPositionScale = marketContext?.SuggestedPositionScale,
            ExecutionFrequencyLabel = marketContext?.ExecutionFrequencyLabel,
            MainlineSectorName = marketContext?.MainlineSectorName,
            MainlineScoreAtCreation = marketContext?.MainlineScore,
            SectorNameAtCreation = marketContext?.StockSectorName,
            SectorCodeAtCreation = marketContext?.SectorCode,
            CreatedAt = now,
            UpdatedAt = now
        };

        ValidatePlanDateRange(plan.PlanStartDate, plan.PlanEndDate);
        ApplyExpiryIfNeeded(plan, now);

        _dbContext.TradingPlans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _watchlistService.UpsertAsync(plan.Symbol, plan.Name, "trading-plan", $"plan:{plan.Id}", true, cancellationToken);
        return new TradingPlanSaveResult(plan, true);
    }

    public async Task<TradingPlan?> UpdateAsync(long id, TradingPlanUpdateDto request, CancellationToken cancellationToken = default)
    {
        await NormalizeExpiredPlansAsync(null, id, cancellationToken);

        var plan = await _dbContext.TradingPlans.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        if (!IsEditableStatus(plan.Status))
        {
            throw new InvalidOperationException("仅 Pending / Draft / ReviewRequired 计划允许编辑");
        }

        plan.Name = NormalizeRequiredName(request.Name, plan.Name);
        EnsureLegacyCompatibility(plan);
        plan.Direction = ParseDirection(request.Direction, plan.Direction);
        plan.Status = ParseRequestedStatus(request.Status, plan.Status);
        plan.TriggerPrice = request.TriggerPrice;
        plan.InvalidPrice = request.InvalidPrice;
        plan.StopLossPrice = request.StopLossPrice;
        plan.TakeProfitPrice = request.TakeProfitPrice;
        plan.TargetPrice = request.TargetPrice;
        plan.ExpectedCatalyst = NormalizeOptional(request.ExpectedCatalyst);
        plan.InvalidConditions = NormalizeOptional(request.InvalidConditions);
        plan.RiskLimits = NormalizeOptional(request.RiskLimits);
        plan.AnalysisSummary = NormalizeOptional(request.AnalysisSummary);
        plan.SourceAgent = NormalizeOptional(request.SourceAgent) ?? plan.SourceAgent;
        plan.UserNote = NormalizeOptional(request.UserNote);
        plan.ActiveScenario = NormalizeScenarioKey(request.ActiveScenario) ?? plan.ActiveScenario ?? "Primary";
        plan.PlanStartDate = request.PlanStartDate;
        plan.PlanEndDate = request.PlanEndDate;
        plan.UpdatedAt = DateTime.UtcNow;

        ValidatePlanDateRange(plan.PlanStartDate, plan.PlanEndDate);
        ApplyExpiryIfNeeded(plan, plan.UpdatedAt);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return plan;
    }

    public async Task<TradingPlan?> CancelAsync(long id, CancellationToken cancellationToken = default)
    {
        await NormalizeExpiredPlansAsync(null, id, cancellationToken);

        var plan = await _dbContext.TradingPlans.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        if (plan.Status is TradingPlanStatus.Cancelled or TradingPlanStatus.Invalid)
        {
            return plan;
        }

        EnsureLegacyCompatibility(plan);
        plan.Status = TradingPlanStatus.Cancelled;
        plan.CancelledAt = DateTime.UtcNow;
        plan.UpdatedAt = plan.CancelledAt.Value;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return plan;
    }

    public async Task<TradingPlan?> ResumeAsync(long id, CancellationToken cancellationToken = default)
    {
        await NormalizeExpiredPlansAsync(null, id, cancellationToken);

        var plan = await _dbContext.TradingPlans.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        if (plan.Status != TradingPlanStatus.ReviewRequired)
        {
            throw new InvalidOperationException("仅 ReviewRequired 计划允许恢复观察");
        }

        EnsureLegacyCompatibility(plan);
        plan.Status = TradingPlanStatus.Pending;
        plan.UpdatedAt = DateTime.UtcNow;
        _dbContext.TradingPlanEvents.Add(new TradingPlanEvent
        {
            PlanId = plan.Id,
            Symbol = plan.Symbol,
            EventType = TradingPlanEventType.ReviewCleared,
            Strategy = "manual-review",
            Reason = "人工复核后恢复观察",
            CreatedAt = plan.UpdatedAt,
            Severity = TradingPlanEventSeverity.Info,
            Message = "人工复核后已恢复观察。",
            MetadataJson = JsonSerializer.Serialize(new { resumedAt = plan.UpdatedAt }),
            OccurredAt = plan.UpdatedAt
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return plan;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.TradingPlans.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (plan is null)
        {
            return false;
        }

        _dbContext.TradingPlans.Remove(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeRequiredName(string? preferred, string fallback)
    {
        var value = NormalizeOptional(preferred);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
    }

    private static void EnsureLegacyCompatibility(TradingPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.PlanKey))
        {
            plan.PlanKey = GeneratePlanKey();
        }

        plan.Title = NormalizeLegacyTitle(plan.Name);
    }

    private static string GeneratePlanKey()
    {
        return $"plan-{Guid.NewGuid():N}";
    }

    private static string NormalizeLegacyTitle(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        var result = value?.Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string? NormalizeScenarioKey(string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            return null;
        }

        if (string.Equals(normalized, "主场景", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Primary", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Main", StringComparison.OrdinalIgnoreCase))
        {
            return "Primary";
        }

        if (string.Equals(normalized, "备选场景", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Backup", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Alternative", StringComparison.OrdinalIgnoreCase))
        {
            return "Backup";
        }

        return normalized;
    }

    private static TradingPlanStatus ParseRequestedStatus(string? value, TradingPlanStatus fallback)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            return fallback;
        }

        if (Enum.TryParse<TradingPlanStatus>(normalized, true, out var parsed))
        {
            return parsed;
        }

        if (string.Equals(normalized, "Archived", StringComparison.OrdinalIgnoreCase))
        {
            return TradingPlanStatus.Cancelled;
        }

        if (string.Equals(normalized, "NeedsReview", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Review", StringComparison.OrdinalIgnoreCase))
        {
            return TradingPlanStatus.ReviewRequired;
        }

        return fallback;
    }

    private static void ValidatePlanDateRange(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            throw new InvalidOperationException("开始日期不能晚于结束日期");
        }
    }

    private static void ApplyExpiryIfNeeded(TradingPlan plan, DateTime nowUtc)
    {
        if (plan.PlanEndDate is null || IsTerminalStatus(plan.Status))
        {
            return;
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ChinaTimeZone));
        if (plan.PlanEndDate.Value >= today)
        {
            return;
        }

        plan.Status = TradingPlanStatus.Invalid;
        plan.InvalidatedAt ??= nowUtc;
        plan.UpdatedAt = nowUtc;
    }

    private static TradingPlanDirection ParseDirection(string? value, TradingPlanDirection fallback = TradingPlanDirection.Long)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return Enum.TryParse<TradingPlanDirection>(value.Trim(), true, out var direction)
            ? direction
            : fallback;
    }

    private static bool IsEditableStatus(TradingPlanStatus status)
    {
        return status is TradingPlanStatus.Pending or TradingPlanStatus.Draft or TradingPlanStatus.ReviewRequired;
    }

    private static bool IsTerminalStatus(TradingPlanStatus status)
    {
        return status is TradingPlanStatus.Invalid or TradingPlanStatus.Cancelled;
    }

    private async Task NormalizeExpiredPlansAsync(string? symbol, long? id, CancellationToken cancellationToken)
    {
        var query = _dbContext.TradingPlans
            .Where(NonTerminalPlanWithEndDatePredicate)
            .AsQueryable();

        if (id.HasValue)
        {
            query = query.Where(item => item.Id == id.Value);
        }
        else if (!string.IsNullOrWhiteSpace(symbol))
        {
            query = query.Where(item => item.Symbol == symbol);
        }

        var candidates = await query.ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var changed = false;
        foreach (var candidate in candidates)
        {
            var previousStatus = candidate.Status;
            var previousInvalidatedAt = candidate.InvalidatedAt;
            var previousUpdatedAt = candidate.UpdatedAt;
            ApplyExpiryIfNeeded(candidate, now);
            if (candidate.Status != previousStatus
                || candidate.InvalidatedAt != previousInvalidatedAt
                || candidate.UpdatedAt != previousUpdatedAt)
            {
                changed = true;
            }
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static TimeZoneInfo ResolveChinaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("China Standard Time", TimeSpan.FromHours(8), "China Standard Time", "China Standard Time");
        }
    }
}