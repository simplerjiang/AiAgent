using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface ITradeExecutionInsightService
{
    Task<IReadOnlyDictionary<long, TradingPlanRuntimeInsightDto>> GetPlanInsightsAsync(IReadOnlyCollection<TradingPlan> plans, bool useLiveQuote = false, CancellationToken cancellationToken = default);
    Task<TradingPlanRuntimeInsightDto?> GetPlanInsightAsync(TradingPlan plan, bool useLiveQuote = true, CancellationToken cancellationToken = default);
    Task<TradingPlanPortfolioSummaryDto> GetPortfolioSummaryAsync(CancellationToken cancellationToken = default);
    Task EnrichTradeExecutionAsync(TradeExecution trade, bool useLiveQuote = true, CancellationToken cancellationToken = default);
}

public sealed class TradeExecutionInsightService : ITradeExecutionInsightService
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppDbContext _db;
    private readonly IStockMarketContextService _marketContextService;
    private readonly IPortfolioSnapshotService _portfolioSnapshotService;
    private readonly IStockDataService _stockDataService;
    private readonly ILogger<TradeExecutionInsightService>? _logger;

    public TradeExecutionInsightService(
        AppDbContext db,
        IStockMarketContextService marketContextService,
        IPortfolioSnapshotService portfolioSnapshotService,
        IStockDataService stockDataService,
        ILogger<TradeExecutionInsightService>? logger = null)
    {
        _db = db;
        _marketContextService = marketContextService;
        _portfolioSnapshotService = portfolioSnapshotService;
        _stockDataService = stockDataService;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<long, TradingPlanRuntimeInsightDto>> GetPlanInsightsAsync(IReadOnlyCollection<TradingPlan> plans, bool useLiveQuote = false, CancellationToken cancellationToken = default)
    {
        if (plans.Count == 0)
        {
            return new Dictionary<long, TradingPlanRuntimeInsightDto>();
        }

        var planIds = plans.Select(item => item.Id).ToArray();
        var symbols = plans.Select(item => item.Symbol).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var executions = await _db.TradeExecutions
            .AsNoTracking()
            .Where(item => item.PlanId.HasValue && planIds.Contains(item.PlanId.Value))
            .OrderByDescending(item => item.ExecutedAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        var positions = await _db.StockPositions
            .AsNoTracking()
            .Where(item => symbols.Contains(item.Symbol))
            .ToListAsync(cancellationToken);

        var planEvents = await _db.TradingPlanEvents
            .AsNoTracking()
            .Where(item => planIds.Contains(item.PlanId))
            .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        var quoteSnapshots = useLiveQuote
            ? new Dictionary<string, StockQuoteDto?>(StringComparer.OrdinalIgnoreCase)
            : await LoadQuoteSnapshotsAsync(symbols, cancellationToken);

        var contexts = await LoadMarketContextsSafelyAsync(plans, cancellationToken);
        var executionMap = executions.GroupBy(item => item.PlanId!.Value).ToDictionary(group => group.Key, group => group.ToList());
        var positionMap = positions.ToDictionary(item => item.Symbol, item => item, StringComparer.OrdinalIgnoreCase);
        var eventMap = planEvents.GroupBy(item => item.PlanId).ToDictionary(group => group.Key, group => group.First());
        var result = new Dictionary<long, TradingPlanRuntimeInsightDto>();

        for (var index = 0; index < plans.Count; index++)
        {
            var plan = plans.ElementAt(index);
            var latestQuote = await ResolveQuoteAsync(plan.Symbol, quoteSnapshots, useLiveQuote, cancellationToken);
            var insight = BuildPlanInsight(
                plan,
                executionMap.GetValueOrDefault(plan.Id) ?? new List<TradeExecution>(),
                positionMap.GetValueOrDefault(plan.Symbol),
                eventMap.GetValueOrDefault(plan.Id),
                latestQuote,
                contexts[index]);
            result[plan.Id] = insight;
        }

        return result;
    }

    public async Task<TradingPlanRuntimeInsightDto?> GetPlanInsightAsync(TradingPlan plan, bool useLiveQuote = true, CancellationToken cancellationToken = default)
    {
        var insights = await GetPlanInsightsAsync(new[] { plan }, useLiveQuote, cancellationToken);
        return insights.GetValueOrDefault(plan.Id);
    }

    public async Task<TradingPlanPortfolioSummaryDto> GetPortfolioSummaryAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _portfolioSnapshotService.GetSnapshotAsync();
        return new TradingPlanPortfolioSummaryDto(
            snapshot.TotalPositionRatio,
            snapshot.AvailableCash,
            snapshot.TotalUnrealizedPnL,
            $"当前总仓位 {snapshot.TotalPositionRatio:P1} · 可用资金 {snapshot.AvailableCash:F2} · 浮盈 {snapshot.TotalUnrealizedPnL:+0.00;-0.00;0.00}");
    }

    public async Task EnrichTradeExecutionAsync(TradeExecution trade, bool useLiveQuote = true, CancellationToken cancellationToken = default)
    {
        trade.ExecutionAction = NormalizeOptional(trade.ExecutionAction) ?? DeriveExecutionAction(trade);

        TradingPlan? plan = null;
        if (trade.PlanId.HasValue)
        {
            plan = await _db.TradingPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == trade.PlanId.Value, cancellationToken);
        }

        trade.PlanSourceAgent = NormalizeOptional(trade.PlanSourceAgent) ?? plan?.SourceAgent;
        trade.PlanAction = NormalizeOptional(trade.PlanAction) ?? DerivePlanAction(plan);

        if (!string.IsNullOrWhiteSpace(trade.ScenarioSnapshotJson) && !string.IsNullOrWhiteSpace(trade.PositionSnapshotJson) && !useLiveQuote)
        {
            trade.CoachTip = BuildCoachTip(trade, ParseScenarioSnapshot(trade.ScenarioSnapshotJson));
            return;
        }

        var latestQuote = await ResolveQuoteAsync(trade.Symbol, new Dictionary<string, StockQuoteDto?>(StringComparer.OrdinalIgnoreCase), useLiveQuote, cancellationToken);
        var currentContext = plan is null ? null : await _marketContextService.GetLatestAsync(plan.Symbol, cancellationToken);
        var latestPlanEvent = plan is null
            ? null
            : await _db.TradingPlanEvents
                .AsNoTracking()
                .Where(item => item.PlanId == plan.Id)
                .OrderByDescending(item => item.OccurredAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

        var scenario = plan is null
            ? BuildUnplannedScenario(trade, latestQuote)
            : BuildScenarioStatus(plan, latestQuote, currentContext, latestPlanEvent, "Historical");
        var position = await BuildPositionSnapshotAsync(trade.Symbol, latestQuote, "Historical", cancellationToken);

        trade.ScenarioCode = scenario?.Code;
        trade.ScenarioLabel = scenario?.Label;
        trade.ScenarioReason = scenario?.Reason;
        trade.ScenarioSnapshotType = scenario?.SnapshotType;
        trade.ScenarioSnapshotJson = scenario is null ? null : JsonSerializer.Serialize(scenario, SnapshotJsonOptions);
        trade.PositionSnapshotJson = position is null ? null : JsonSerializer.Serialize(position, SnapshotJsonOptions);
        trade.CoachTip = BuildCoachTip(trade, scenario);
    }

    private async Task<Modules.Market.Models.StockMarketContextDto?[]> LoadMarketContextsSafelyAsync(
        IReadOnlyCollection<TradingPlan> plans,
        CancellationToken cancellationToken)
    {
        var planList = plans as IList<TradingPlan> ?? plans.ToList();
        var contexts = new Modules.Market.Models.StockMarketContextDto?[planList.Count];
        var cache = new Dictionary<string, Modules.Market.Models.StockMarketContextDto?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < planList.Count; index++)
        {
            var symbol = planList[index].Symbol;
            if (string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            if (!cache.TryGetValue(symbol, out var context))
            {
                try
                {
                    context = await _marketContextService.GetLatestAsync(symbol, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "加载交易计划执行洞察市场上下文失败，将按无上下文降级。symbol={Symbol}", symbol);
                    context = null;
                }

                cache[symbol] = context;
            }

            contexts[index] = context;
        }

        return contexts;
    }

    private TradingPlanRuntimeInsightDto BuildPlanInsight(
        TradingPlan plan,
        IReadOnlyList<TradeExecution> executions,
        StockPosition? position,
        TradingPlanEvent? latestEvent,
        StockQuoteDto? latestQuote,
        Modules.Market.Models.StockMarketContextDto? currentContext)
    {
        var executionSummary = BuildExecutionSummary(executions);
        var scenarioStatus = BuildScenarioStatus(plan, latestQuote, currentContext, latestEvent, "Current");
        var positionSnapshot = BuildPositionSnapshot(plan.Symbol, position, latestQuote, "Current");
        return new TradingPlanRuntimeInsightDto(executionSummary, scenarioStatus, positionSnapshot);
    }

    private static TradingPlanExecutionSummaryDto? BuildExecutionSummary(IReadOnlyList<TradeExecution> executions)
    {
        if (executions.Count == 0)
        {
            return null;
        }

        var latest = executions[0];
        var deviatedCount = executions.Count(item => item.ComplianceTag == ComplianceTag.DeviatedFromPlan);
        var unplannedCount = executions.Count(item => item.ComplianceTag == ComplianceTag.Unplanned);
        var latestTags = ParseDeviationTags(latest.DeviationTagsJson);
        var summaryParts = new List<string>
        {
            $"已执行 {executions.Count} 次",
            $"最近 {latest.ExecutionAction ?? latest.Direction.ToString()}",
            $"偏差 {deviatedCount} 次"
        };

        if (unplannedCount > 0)
        {
            summaryParts.Add($"无计划 {unplannedCount} 次");
        }

        var summary = string.Join(" · ", summaryParts);

        return new TradingPlanExecutionSummaryDto(
            executions.Count,
            latest.ExecutionAction ?? latest.Direction.ToString(),
            latest.ExecutedAt,
            deviatedCount,
            unplannedCount,
            latest.ComplianceTag.ToString(),
            latestTags,
            summary);
    }

    private async Task<TradingPlanPositionContextDto?> BuildPositionSnapshotAsync(string symbol, StockQuoteDto? latestQuote, string snapshotType, CancellationToken cancellationToken)
    {
        var position = await _db.StockPositions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Symbol == symbol, cancellationToken);

        return BuildPositionSnapshot(symbol, position, latestQuote, snapshotType);
    }

    private static TradingPlanPositionContextDto? BuildPositionSnapshot(string symbol, StockPosition? position, StockQuoteDto? latestQuote, string snapshotType)
    {
        if (position is null)
        {
            return null;
        }

        var latestPrice = latestQuote?.Price ?? position.LatestPrice;
        var marketValue = latestPrice.HasValue ? Math.Round(latestPrice.Value * position.QuantityLots, 2, MidpointRounding.AwayFromZero) : position.MarketValue;
        var unrealizedPnL = latestPrice.HasValue ? Math.Round((latestPrice.Value - position.AverageCostPrice) * position.QuantityLots, 2, MidpointRounding.AwayFromZero) : position.UnrealizedPnL;
        var summary = position.QuantityLots > 0
            ? $"当前持仓 {position.QuantityLots} 股 · 成本 {position.AverageCostPrice:F2} · 浮盈 {unrealizedPnL:+0.00;-0.00;0.00}"
            : "当前无持仓";

        return new TradingPlanPositionContextDto(
            symbol,
            position.Name,
            position.QuantityLots,
            position.AverageCostPrice,
            latestPrice,
            marketValue,
            unrealizedPnL,
            position.PositionRatio,
            snapshotType,
            latestQuote?.Timestamp ?? position.UpdatedAt,
            null,
            null,
            summary);
    }

    private static TradingPlanScenarioStatusDto BuildUnplannedScenario(TradeExecution trade, StockQuoteDto? latestQuote)
    {
        var snapshotAt = latestQuote?.Timestamp ?? trade.ExecutedAt;
        var summary = "无对应计划，后续以复盘和仓位保护为主。";
        return new TradingPlanScenarioStatusDto(
            "Unplanned",
            "无计划",
            summary,
            "Historical",
            snapshotAt,
            latestQuote?.Price,
            trade.MarketStageAtTrade,
            false,
            false,
            false,
            null,
            summary);
    }

    private static TradingPlanScenarioStatusDto BuildScenarioStatus(
        TradingPlan plan,
        StockQuoteDto? latestQuote,
        Modules.Market.Models.StockMarketContextDto? currentContext,
        TradingPlanEvent? latestEvent,
        string snapshotType)
    {
        var currentPrice = latestQuote?.Price;
        var snapshotAt = latestQuote?.Timestamp ?? DateTime.UtcNow;
        var planStatus = NormalizePlanStatus(plan.Status).ToString();

        if (IsAbandonTriggered(plan, currentPrice, latestEvent))
        {
            var reason = latestEvent?.Severity == TradingPlanEventSeverity.Critical
                ? $"最新告警提示：{latestEvent.Message}"
                : $"当前价格 {FormatPrice(currentPrice)} 已触发失效位 {FormatPrice(plan.InvalidPrice)}";
            return new TradingPlanScenarioStatusDto(
                "Abandon",
                "放弃条件命中",
                reason,
                snapshotType,
                snapshotAt,
                currentPrice,
                currentContext?.StageLabel,
                currentContext?.CounterTrendWarning ?? false,
                currentContext?.IsMainlineAligned ?? false,
                true,
                planStatus,
                $"放弃条件命中 · {reason}");
        }

        if (IsPrimaryScenario(plan, currentPrice))
        {
            var reason = plan.Status == TradingPlanStatus.Triggered
                ? "计划已进入执行态，优先按主场景跟踪。"
                : $"当前价格 {FormatPrice(currentPrice)} 已接近/触及触发位 {FormatPrice(plan.TriggerPrice)}。";
            return new TradingPlanScenarioStatusDto(
                "Primary",
                "主场景",
                reason,
                snapshotType,
                snapshotAt,
                currentPrice,
                currentContext?.StageLabel,
                currentContext?.CounterTrendWarning ?? false,
                currentContext?.IsMainlineAligned ?? false,
                false,
                planStatus,
                $"主场景 · {reason}");
        }

        if (IsBackupScenario(currentContext))
        {
            var reason = currentContext?.CounterTrendWarning == true
                ? "当前市场节奏存在逆势提示，先按备选场景控制动作。"
                : $"当前市场阶段为 {currentContext?.StageLabel ?? "未知"}，主线匹配度不足。";
            return new TradingPlanScenarioStatusDto(
                "Backup",
                "备选场景",
                reason,
                snapshotType,
                snapshotAt,
                currentPrice,
                currentContext?.StageLabel,
                currentContext?.CounterTrendWarning ?? false,
                currentContext?.IsMainlineAligned ?? false,
                false,
                planStatus,
                $"备选场景 · {reason}");
        }

        var watchingReason = plan.TriggerPrice.HasValue
            ? $"当前价格 {FormatPrice(currentPrice)} 尚未到达触发位 {FormatPrice(plan.TriggerPrice)}。"
            : "等待下一次触发信号确认。";
        return new TradingPlanScenarioStatusDto(
            "Watch",
            "待观察",
            watchingReason,
            snapshotType,
            snapshotAt,
            currentPrice,
            currentContext?.StageLabel,
            currentContext?.CounterTrendWarning ?? false,
            currentContext?.IsMainlineAligned ?? false,
            false,
            planStatus,
            $"待观察 · {watchingReason}");
    }

    private static bool IsPrimaryScenario(TradingPlan plan, decimal? currentPrice)
    {
        if (plan.Status == TradingPlanStatus.Triggered)
        {
            return true;
        }

        if (!plan.TriggerPrice.HasValue || !currentPrice.HasValue)
        {
            return false;
        }

        return plan.Direction == TradingPlanDirection.Long
            ? currentPrice.Value >= plan.TriggerPrice.Value * 0.995m
            : currentPrice.Value <= plan.TriggerPrice.Value * 1.005m;
    }

    private static bool IsBackupScenario(Modules.Market.Models.StockMarketContextDto? currentContext)
    {
        if (currentContext is null)
        {
            return false;
        }

        return currentContext.CounterTrendWarning
            || !currentContext.IsMainlineAligned
            || string.Equals(currentContext.StageLabel, "混沌", StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentContext.StageLabel, "退潮", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAbandonTriggered(TradingPlan plan, decimal? currentPrice, TradingPlanEvent? latestEvent)
    {
        if (plan.Status is TradingPlanStatus.Invalid or TradingPlanStatus.Cancelled)
        {
            return true;
        }

        if (latestEvent?.Severity == TradingPlanEventSeverity.Critical)
        {
            return true;
        }

        if (!plan.InvalidPrice.HasValue || !currentPrice.HasValue)
        {
            return false;
        }

        return plan.Direction == TradingPlanDirection.Long
            ? currentPrice.Value <= plan.InvalidPrice.Value
            : currentPrice.Value >= plan.InvalidPrice.Value;
    }

    private static string BuildCoachTip(TradeExecution trade, TradingPlanScenarioStatusDto? scenario)
    {
        var tags = ParseDeviationTags(trade.DeviationTagsJson);
        if (trade.ComplianceTag == ComplianceTag.Unplanned)
        {
            return "这是一次无计划交易，建议先补齐触发位与失效位，再决定后续动作。";
        }

        if (scenario?.AbandonTriggered == true)
        {
            return "场景已触发放弃条件，后续更应优先保护仓位而不是继续加码。";
        }

        if (tags.Contains("追价"))
        {
            return "执行价格明显偏离触发位，后续先复核追价原因与仓位控制。";
        }

        if (tags.Contains("超仓"))
        {
            return "本次执行疑似超出原计划仓位，建议优先检查总风险暴露是否仍可接受。";
        }

        if (trade.ComplianceTag == ComplianceTag.DeviatedFromPlan || tags.Count > 0)
        {
            return "这笔执行偏离了原预案，建议复核动作、触发位和市场场景是否仍匹配。";
        }

        return "本次执行基本仍在预案内，继续跟踪场景变化与仓位节奏。";
    }

    private async Task<Dictionary<string, StockQuoteDto?>> LoadQuoteSnapshotsAsync(IReadOnlyCollection<string> symbols, CancellationToken cancellationToken)
    {
        if (symbols.Count == 0)
        {
            return new Dictionary<string, StockQuoteDto?>(StringComparer.OrdinalIgnoreCase);
        }

        var snapshots = await _db.StockQuoteSnapshots
            .AsNoTracking()
            .Where(item => symbols.Contains(item.Symbol))
            .OrderByDescending(item => item.Timestamp)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(item => item.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var latest = group.First();
                    return new StockQuoteDto(
                        latest.Symbol,
                        latest.Name,
                        latest.Price,
                        latest.Change,
                        latest.ChangePercent,
                        0m,
                        latest.PeRatio,
                        0m,
                        0m,
                        0m,
                        latest.Timestamp,
                        Array.Empty<StockNewsDto>(),
                        Array.Empty<StockIndicatorDto>(),
                        latest.FloatMarketCap,
                        latest.VolumeRatio,
                        latest.ShareholderCount,
                        latest.SectorName);
                },
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<StockQuoteDto?> ResolveQuoteAsync(string symbol, IDictionary<string, StockQuoteDto?> cache, bool useLiveQuote, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(symbol, out var cached))
        {
            return cached;
        }

        if (useLiveQuote)
        {
            try
            {
                var liveQuote = await _stockDataService.GetQuoteAsync(symbol, null, cancellationToken);
                cache[symbol] = liveQuote;
                return liveQuote;
            }
            catch
            {
                // fall back to cache/database snapshot below
            }
        }

        var snapshot = await _db.StockQuoteSnapshots
            .AsNoTracking()
            .Where(item => item.Symbol == symbol)
            .OrderByDescending(item => item.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
        {
            cache[symbol] = null;
            return null;
        }

        var quote = new StockQuoteDto(
            snapshot.Symbol,
            snapshot.Name,
            snapshot.Price,
            snapshot.Change,
            snapshot.ChangePercent,
            0m,
            snapshot.PeRatio,
            0m,
            0m,
            0m,
            snapshot.Timestamp,
            Array.Empty<StockNewsDto>(),
            Array.Empty<StockIndicatorDto>(),
            snapshot.FloatMarketCap,
            snapshot.VolumeRatio,
            snapshot.ShareholderCount,
            snapshot.SectorName);
        cache[symbol] = quote;
        return quote;
    }

    private static IReadOnlyList<string> ParseDeviationTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(value, SnapshotJsonOptions)
                   ?.Where(item => !string.IsNullOrWhiteSpace(item))
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .ToArray()
                   ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static TradingPlanScenarioStatusDto? ParseScenarioSnapshot(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TradingPlanScenarioStatusDto>(value, SnapshotJsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string DeriveExecutionAction(TradeExecution trade)
    {
        return trade.Direction switch
        {
            TradeDirection.Buy when trade.TradeType == TradeType.DayTrade => "做T买入",
            TradeDirection.Sell when trade.TradeType == TradeType.DayTrade => "做T卖出",
            TradeDirection.Buy => "买入执行",
            _ => "卖出执行"
        };
    }

    private static string? DerivePlanAction(TradingPlan? plan)
    {
        return plan?.Direction == TradingPlanDirection.Short ? "计划卖出" : plan is null ? null : "计划买入";
    }

    private static string NormalizeOptional(string? value)
    {
        var result = value?.Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string FormatPrice(decimal? value)
    {
        return value.HasValue ? value.Value.ToString("0.00") : "--";
    }

    private static TradingPlanStatus NormalizePlanStatus(TradingPlanStatus status)
    {
        return status == TradingPlanStatus.Draft ? TradingPlanStatus.Pending : status;
    }
}