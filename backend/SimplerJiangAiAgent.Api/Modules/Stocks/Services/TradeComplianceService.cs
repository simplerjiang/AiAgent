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
    private readonly AppDbContext _db;

    public TradeComplianceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task TagComplianceAsync(TradeExecution trade)
    {
        // Find matching plan for this symbol (Pending or Triggered)
        var matchingPlan = await _db.TradingPlans
            .Where(p => p.Symbol == trade.Symbol
                && (p.Status == TradingPlanStatus.Pending || p.Status == TradingPlanStatus.Triggered))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (matchingPlan is null)
        {
            trade.ComplianceTag = ComplianceTag.Unplanned;
        }
        else
        {
            trade.PlanId ??= matchingPlan.Id;

            // Check direction alignment
            var planIsBuy = matchingPlan.Direction == TradingPlanDirection.Long;
            var tradeIsBuy = trade.Direction == TradeDirection.Buy;
            var directionMatch = planIsBuy == tradeIsBuy;

            // Check price deviation (±5%)
            var priceDeviation = true;
            if (matchingPlan.TriggerPrice.HasValue && matchingPlan.TriggerPrice.Value > 0)
            {
                var deviation = Math.Abs(trade.ExecutedPrice - matchingPlan.TriggerPrice.Value) / matchingPlan.TriggerPrice.Value;
                priceDeviation = deviation <= 0.05m;
            }

            trade.ComplianceTag = directionMatch && priceDeviation
                ? ComplianceTag.FollowedPlan
                : ComplianceTag.DeviatedFromPlan;
        }

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
            t.AgentDirection, t.AgentConfidence, t.MarketStageAtTrade)).ToList();

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
}
