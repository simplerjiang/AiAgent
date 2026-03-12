namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

internal static class LocalNewsSentimentClassifier
{
    private static readonly string[] PositiveKeywords =
    {
        "上涨", "增持", "回购", "中标", "签约", "业绩增长", "盈利", "利好", "上调", "扩产", "突破", "创新高", "获批", "订单", "超预期", "分红", "扭亏"
    };

    private static readonly string[] NegativeKeywords =
    {
        "下跌", "减持", "被罚", "违规", "亏损", "利空", "下调", "诉讼", "停产", "裁员", "风险", "暴雷", "业绩下滑", "大跌", "降级", "问询", "终止"
    };

    public static string Classify(string? title, string? category = null)
    {
        var text = string.Join(' ', new[] { category, title }.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (string.IsNullOrWhiteSpace(text))
        {
            return "中性";
        }

        var positiveHits = CountHits(text, PositiveKeywords);
        var negativeHits = CountHits(text, NegativeKeywords);
        if (positiveHits == negativeHits)
        {
            return "中性";
        }

        return positiveHits > negativeHits ? "利好" : "利空";
    }

    private static int CountHits(string text, IReadOnlyList<string> keywords)
    {
        var count = 0;
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                count += 1;
            }
        }

        return count;
    }
}