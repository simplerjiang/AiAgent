using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface ITradingBehaviorService
{
    Task<TradeBehaviorStatsDto> GetBehaviorStatsAsync();
}

public sealed class TradingBehaviorService : ITradingBehaviorService
{
    private readonly AppDbContext _db;

    public TradingBehaviorService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TradeBehaviorStatsDto> GetBehaviorStatsAsync()
    {
        var now = DateTime.UtcNow;
        var date7 = now.AddDays(-7);
        var date30 = now.AddDays(-30);

        // ── 交易频率 ──
        var trades7Days = await _db.Set<TradeExecution>()
            .CountAsync(t => t.ExecutedAt >= date7);
        var trades30Days = await _db.Set<TradeExecution>()
            .CountAsync(t => t.ExecutedAt >= date30);
        var avgDaily7 = trades7Days / 7m;
        var avgDaily30 = trades30Days / 30m;

        // ── 计划执行率 ──
        var plannedTrades30 = await _db.Set<TradeExecution>()
            .CountAsync(t => t.ExecutedAt >= date30 && t.PlanId != null);
        var planExecutionRate = trades30Days > 0 ? (decimal)plannedTrades30 / trades30Days : 1m;

        // ── 连续亏损 ──
        var sellTrades = await _db.Set<TradeExecution>()
            .Where(t => t.Direction == TradeDirection.Sell && t.ExecutedAt >= date30)
            .OrderByDescending(t => t.ExecutedAt)
            .Select(t => t.RealizedPnL)
            .ToListAsync();

        int currentLossStreak = 0;
        foreach (var pnl in sellTrades)
        {
            if (pnl.HasValue && pnl.Value < 0) currentLossStreak++;
            else break;
        }

        int maxLossStreak = 0;
        int streak = 0;
        foreach (var pnl in sellTrades)
        {
            if (pnl.HasValue && pnl.Value < 0)
            {
                streak++;
                if (streak > maxLossStreak) maxLossStreak = streak;
            }
            else
            {
                streak = 0;
            }
        }

        // ── 追涨统计（简化：Unplanned 买入 ≈ 冲动交易） ──
        var buyTrades30 = await _db.Set<TradeExecution>()
            .CountAsync(t => t.Direction == TradeDirection.Buy && t.ExecutedAt >= date30);
        var chasingBuyCount = await _db.Set<TradeExecution>()
            .CountAsync(t => t.Direction == TradeDirection.Buy
                && t.ExecutedAt >= date30
                && t.ComplianceTag == ComplianceTag.Unplanned);
        var chasingBuyRate = buyTrades30 > 0 ? (decimal)chasingBuyCount / buyTrades30 : 0m;

        // ── 过度交易 ──
        var isOverTrading = avgDaily30 > 0 && avgDaily7 > avgDaily30 * 1.5m;

        // ── 纪律分数 ──
        int score = 100;
        if (planExecutionRate < 0.5m) score -= 20;
        else if (planExecutionRate < 0.8m) score -= 10;
        if (currentLossStreak >= 3) score -= 15;
        if (isOverTrading) score -= 15;
        if (chasingBuyRate > 0.5m) score -= 20;
        else if (chasingBuyRate > 0.3m) score -= 10;
        if (score < 0) score = 0;

        // ── 告警生成 ──
        var alerts = new List<BehaviorAlertDto>();

        // 连续 3 个计划 Invalid
        var recentPlanStatuses = await _db.TradingPlans
            .OrderByDescending(p => p.CreatedAt)
            .Take(3)
            .Select(p => p.Status)
            .ToListAsync();
        if (recentPlanStatuses.Count == 3 && recentPlanStatuses.All(s => s == TradingPlanStatus.Invalid))
        {
            alerts.Add(new BehaviorAlertDto("AccuracyDown", "warning", "最近 3 个计划均已失效，建议复盘分析逻辑"));
        }

        // 当日已触发 ≥ 2 计划
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var todayTriggeredCount = await _db.Set<TradeExecution>()
            .CountAsync(t => t.ExecutedAt >= todayStart);
        if (todayTriggeredCount >= 2)
        {
            alerts.Add(new BehaviorAlertDto("TooActive", "info", $"今日已执行 {todayTriggeredCount} 笔交易，已足够活跃"));
        }

        // 连续 3 笔亏损
        if (currentLossStreak >= 3)
        {
            alerts.Add(new BehaviorAlertDto("LossStreak", "warning", $"当前连续 {currentLossStreak} 笔亏损，建议降低仓位或暂停交易"));
        }

        // 计划外交易占比 > 50%
        if (trades30Days > 0 && planExecutionRate < 0.5m)
        {
            alerts.Add(new BehaviorAlertDto("LowDiscipline", "danger", "近 30 天计划外交易占比超过 50%，纪律执行偏低"));
        }

        return new TradeBehaviorStatsDto(
            Trades7Days: trades7Days,
            Trades30Days: trades30Days,
            AvgDailyTrades7Days: Math.Round(avgDaily7, 1),
            AvgDailyTrades30Days: Math.Round(avgDaily30, 1),
            PlannedTrades30Days: plannedTrades30,
            TotalTrades30Days: trades30Days,
            PlanExecutionRate: Math.Round(planExecutionRate, 3),
            CurrentLossStreak: currentLossStreak,
            MaxLossStreak30Days: maxLossStreak,
            ChasingBuyCount30Days: chasingBuyCount,
            ChasingBuyRate: Math.Round(chasingBuyRate, 3),
            IsOverTrading: isOverTrading,
            DisciplineScore: score,
            ActiveAlerts: alerts
        );
    }
}
