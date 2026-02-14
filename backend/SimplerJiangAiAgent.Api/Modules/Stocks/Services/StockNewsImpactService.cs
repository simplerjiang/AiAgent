using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IStockNewsImpactService
{
    StockNewsImpactDto Evaluate(string symbol, string name, IReadOnlyList<IntradayMessageDto> messages);
}

public sealed class StockNewsImpactService : IStockNewsImpactService
{
    private static readonly string[] PositiveKeywords =
    {
        "上涨", "增持", "回购", "中标", "签约", "业绩增长", "盈利", "利好", "上调", "扩产", "突破", "创新高", "获批", "订单", "超预期"
    };

    private static readonly string[] NegativeKeywords =
    {
        "下跌", "减持", "被罚", "违规", "亏损", "利空", "下调", "诉讼", "停产", "裁员", "风险", "暴雷", "业绩下滑", "大跌", "降级"
    };

    public StockNewsImpactDto Evaluate(string symbol, string name, IReadOnlyList<IntradayMessageDto> messages)
    {
        var items = messages
            .Select(BuildImpactItem)
            .OrderByDescending(item => Math.Abs(item.ImpactScore))
            .Take(20)
            .ToList();

        var positive = items.Count(item => item.Category == "利好");
        var negative = items.Count(item => item.Category == "利空");
        var neutral = items.Count(item => item.Category == "中性");

        var overall = positive == negative
            ? "中性"
            : positive > negative
                ? "利好偏多"
                : "利空偏多";

        var maxPositive = items.Where(item => item.ImpactScore > 0).Select(item => item.ImpactScore).DefaultIfEmpty(0).Max();
        var maxNegative = items.Where(item => item.ImpactScore < 0).Select(item => Math.Abs(item.ImpactScore)).DefaultIfEmpty(0).Max();

        var summary = new StockNewsImpactSummaryDto(
            positive,
            neutral,
            negative,
            overall,
            maxPositive,
            maxNegative
        );

        return new StockNewsImpactDto(symbol, name, DateTime.Now, summary, items);
    }

    private static StockNewsImpactItemDto BuildImpactItem(IntradayMessageDto message)
    {
        var title = message.Title ?? string.Empty;
        var posHits = CountHits(title, PositiveKeywords);
        var negHits = CountHits(title, NegativeKeywords);
        var score = Math.Clamp((posHits - negHits) * 20, -100, 100);

        var category = score >= 20
            ? "利好"
            : score <= -20
                ? "利空"
                : "中性";

        var reason = BuildReason(posHits, negHits);

        return new StockNewsImpactItemDto(
            title,
            message.Source ?? "",
            message.PublishedAt,
            message.Url,
            category,
            score,
            reason
        );
    }

    private static int CountHits(string title, IReadOnlyList<string> keywords)
    {
        var hits = 0;
        foreach (var keyword in keywords)
        {
            if (title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                hits += 1;
            }
        }
        return hits;
    }

    private static string? BuildReason(int positiveHits, int negativeHits)
    {
        if (positiveHits == 0 && negativeHits == 0)
        {
            return null;
        }

        return $"正向关键词:{positiveHits} 负向关键词:{negativeHits}";
    }
}
