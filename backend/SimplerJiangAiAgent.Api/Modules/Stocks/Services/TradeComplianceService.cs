using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface ITradeComplianceService
{
    Task TagComplianceAsync(TradeExecution trade);
    Task<ComplianceStatsDto> GetComplianceStatsAsync(DateTime? from, DateTime? to);
    Task<PlanDeviationDto?> GetPlanDeviationAsync(long planId);
}

public sealed class TradeComplianceService : ITradeComplianceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _db;

    public TradeComplianceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task TagComplianceAsync(TradeExecution trade)
    {
        TradingPlan? matchingPlan = null;
        if (trade.PlanId.HasValue)
        {
            matchingPlan = await _db.TradingPlans.FirstOrDefaultAsync(item => item.Id == trade.PlanId.Value);
        }

        matchingPlan ??= await _db.TradingPlans
            .Where(p => p.Symbol == trade.Symbol
                && (p.Status == TradingPlanStatus.Pending || p.Status == TradingPlanStatus.Triggered || p.Status == TradingPlanStatus.ReviewRequired))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        var deviationTags = ParseDeviationTags(trade.DeviationTagsJson).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (matchingPlan is null)
        {
            trade.ComplianceTag = ComplianceTag.Unplanned;
            deviationTags.Add("无计划交易");
        }
        else
        {
            trade.PlanId ??= matchingPlan.Id;
            trade.PlanSourceAgent ??= matchingPlan.SourceAgent;
            trade.PlanAction ??= matchingPlan.Direction == TradingPlanDirection.Short ? "计划卖出" : "计划买入";

            // Check direction alignment
            var planIsBuy = matchingPlan.Direction == TradingPlanDirection.Long;
            var tradeIsBuy = trade.Direction == TradeDirection.Buy;
            var directionMatch = planIsBuy == tradeIsBuy;
            if (!directionMatch)
            {
                deviationTags.Add("动作偏离");
            }

            var priceDeviation = true;
            if (matchingPlan.TriggerPrice.HasValue && matchingPlan.TriggerPrice.Value > 0)
            {
                var deviation = Math.Abs(trade.ExecutedPrice - matchingPlan.TriggerPrice.Value) / matchingPlan.TriggerPrice.Value;
                priceDeviation = deviation <= 0.01m;

                if (deviation > 0.01m)
                {
                    deviationTags.Add("未按触发位");
                }

                if (matchingPlan.Direction == TradingPlanDirection.Long)
                {
                    if (trade.ExecutedPrice < matchingPlan.TriggerPrice.Value * 0.99m)
                    {
                        deviationTags.Add("低于触发价成交");
                    }

                    if (trade.ExecutedPrice > matchingPlan.TriggerPrice.Value * 1.02m)
                    {
                        deviationTags.Add("高于触发价成交");
                    }

                    if (trade.Direction == TradeDirection.Buy && trade.ExecutedPrice > matchingPlan.TriggerPrice.Value * 1.03m)
                    {
                        deviationTags.Add("追价");
                    }
                }
                else
                {
                    if (trade.ExecutedPrice > matchingPlan.TriggerPrice.Value * 1.01m)
                    {
                        deviationTags.Add("低于触发价成交");
                    }

                    if (trade.ExecutedPrice < matchingPlan.TriggerPrice.Value * 0.98m)
                    {
                        deviationTags.Add("高于触发价成交");
                    }
                }
            }

            var settings = await _db.UserPortfolioSettings.AsNoTracking().FirstOrDefaultAsync();
            if (settings is not null && settings.TotalCapital > 0 && matchingPlan.SuggestedPositionScale.HasValue && matchingPlan.SuggestedPositionScale.Value > 0)
            {
                var plannedValue = settings.TotalCapital * matchingPlan.SuggestedPositionScale.Value;
                var tradeValue = trade.ExecutedPrice * trade.Quantity;
                if (plannedValue > 0 && tradeValue > plannedValue * 1.15m)
                {
                    deviationTags.Add("超仓");
                }
            }

            trade.ComplianceTag = directionMatch && priceDeviation && deviationTags.Count == 0
                ? ComplianceTag.FollowedPlan
                : ComplianceTag.DeviatedFromPlan;
        }

        trade.DeviationTagsJson = deviationTags.Count == 0 ? null : JsonSerializer.Serialize(deviationTags.OrderBy(item => item), JsonOptions);

        // Snapshot latest analysis history
        var latestAnalysis = await _db.StockAgentAnalysisHistories
            .Where(a => a.Symbol == trade.Symbol)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestAnalysis is not null)
        {
            trade.AnalysisHistoryId = latestAnalysis.Id;

            // Extract agent direction & confidence from commander result
            if (!string.IsNullOrEmpty(latestAnalysis.ResultJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(latestAnalysis.ResultJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("analysis_opinion", out var opinion))
                        trade.AgentDirection = opinion.GetString();
                    if (root.TryGetProperty("confidence_score", out var conf) && conf.ValueKind == JsonValueKind.Number)
                        trade.AgentConfidence = conf.GetDecimal();
                }
                catch { /* tolerant: non-critical enrichment */ }
            }
        }

        // Snapshot latest market sentiment
        var latestSentiment = await _db.MarketSentimentSnapshots
            .OrderByDescending(s => s.SnapshotTime)
            .FirstOrDefaultAsync();

        if (latestSentiment is not null)
        {
            trade.MarketStageAtTrade = latestSentiment.StageLabel;
        }
    }

    public async Task<ComplianceStatsDto> GetComplianceStatsAsync(DateTime? from, DateTime? to)
    {
        var query = _db.TradeExecutions.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.ExecutedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.ExecutedAt <= to.Value);

        var trades = await query.ToListAsync();
        var total = trades.Count;
        var followed = trades.Count(t => t.ComplianceTag == ComplianceTag.FollowedPlan);
        var deviated = trades.Count(t => t.ComplianceTag == ComplianceTag.DeviatedFromPlan);
        var unplanned = trades.Count(t => t.ComplianceTag == ComplianceTag.Unplanned);

        return new ComplianceStatsDto(
            total, followed, deviated, unplanned,
            total > 0 ? (decimal)followed / total : 0,
            total > 0 ? (decimal)deviated / total : 0,
            total > 0 ? (decimal)unplanned / total : 0);
    }

    public async Task<PlanDeviationDto?> GetPlanDeviationAsync(long planId)
    {
        var plan = await _db.TradingPlans.FindAsync(planId);
        if (plan is null) return null;

        var executions = await _db.TradeExecutions
            .Include(t => t.Plan)
            .AsNoTracking()
            .Where(t => t.PlanId == planId)
            .OrderBy(t => t.ExecutedAt)
            .ToListAsync();

        var items = executions.Select(t => new TradeExecutionItemDto(
            t.Id, t.PlanId, t.Plan?.Title,
            t.Symbol, t.Name,
            t.Direction.ToString(), t.TradeType.ToString(),
            t.ExecutedPrice, t.Quantity, t.ExecutedAt,
            t.Commission, t.UserNote, t.CreatedAt,
            t.CostBasis, t.RealizedPnL, t.ReturnRate,
            t.ComplianceTag.ToString(),
            t.AgentDirection, t.AgentConfidence, t.MarketStageAtTrade,
            t.PlanSourceAgent, t.PlanAction, t.ExecutionAction,
            ParseDeviationTags(t.DeviationTagsJson), t.DeviationNote, t.AbandonReason,
            ParseScenarioSnapshot(t.ScenarioSnapshotJson), ParsePositionSnapshot(t.PositionSnapshotJson), t.CoachTip)).ToList();

        var avgExecutedPrice = executions.Count > 0
            ? executions.Average(t => t.ExecutedPrice)
            : (decimal?)null;

        var priceDeviation = avgExecutedPrice.HasValue && plan.TriggerPrice.HasValue && plan.TriggerPrice.Value > 0
            ? (avgExecutedPrice.Value - plan.TriggerPrice.Value) / plan.TriggerPrice.Value
            : (decimal?)null;

        return new PlanDeviationDto(
            plan.Id, plan.Title, plan.Direction.ToString(),
            plan.TriggerPrice, items, avgExecutedPrice, priceDeviation);
    }

    private static IReadOnlyList<string> ParseDeviationTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(value, JsonOptions)
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
            return JsonSerializer.Deserialize<TradingPlanScenarioStatusDto>(value, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static TradingPlanPositionContextDto? ParsePositionSnapshot(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TradingPlanPositionContextDto>(value, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
