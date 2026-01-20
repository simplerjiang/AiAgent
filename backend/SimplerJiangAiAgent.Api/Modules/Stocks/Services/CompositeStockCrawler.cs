using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed class CompositeStockCrawler : IStockCrawler
{
    private readonly IEnumerable<IStockCrawler> _crawlers;

    public CompositeStockCrawler(IEnumerable<IStockCrawler> crawlers)
    {
        // 过滤掉自己，避免递归
        _crawlers = crawlers.Where(c => c is not CompositeStockCrawler).ToArray();
    }

    public string SourceName => "聚合";

    public async Task<StockQuoteDto> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // 当前策略：优先取第一个可用来源
        var crawler = _crawlers.FirstOrDefault();
        if (crawler is null)
        {
            return new StockQuoteDto(
                symbol,
                $"{symbol} 示例名称",
                0m,
                0m,
                0m,
                DateTime.UtcNow,
                Array.Empty<StockNewsDto>(),
                Array.Empty<StockIndicatorDto>()
            );
        }

        var baseQuote = await crawler.GetQuoteAsync(symbol, cancellationToken);

        // TODO: 这里可合并多来源数据
        return baseQuote with
        {
            News = BuildPlaceholderNews(baseQuote.Symbol),
            Indicators = BuildPlaceholderIndicators()
        };
    }

    private static IReadOnlyList<StockNewsDto> BuildPlaceholderNews(string symbol)
    {
        return new List<StockNewsDto>
        {
            new(
                $"{symbol} 新闻示例标题",
                "https://example.com",
                "示例来源",
                DateTime.UtcNow.AddHours(-2)
            )
        };
    }

    private static IReadOnlyList<StockIndicatorDto> BuildPlaceholderIndicators()
    {
        return new List<StockIndicatorDto>
        {
            new("MA5", 0m, null),
            new("MA10", 0m, null),
            new("RSI", 0m, null)
        };
    }
}
