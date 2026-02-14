using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed class CompositeStockCrawler : IStockCrawler
{
    private readonly IEnumerable<IStockCrawlerSource> _crawlers;

    public CompositeStockCrawler(IEnumerable<IStockCrawlerSource> crawlers)
    {
        _crawlers = crawlers.ToArray();
    }

    public string SourceName => "聚合";

    public async Task<StockQuoteDto> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var baseQuote = await TryGetAsync(c => c.GetQuoteAsync(symbol, cancellationToken))
            ?? new StockQuoteDto(
                symbol,
                $"{symbol} 示例名称",
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                DateTime.UtcNow,
                Array.Empty<StockNewsDto>(),
                Array.Empty<StockIndicatorDto>()
            );

        // TODO: 这里可合并多来源数据
        return baseQuote with
        {
            News = BuildPlaceholderNews(baseQuote.Symbol),
            Indicators = BuildPlaceholderIndicators()
        };
    }

    public async Task<MarketIndexDto> GetMarketIndexAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var result = await TryGetAsync(c => c.GetMarketIndexAsync(symbol, cancellationToken));
        return result ?? new MarketIndexDto(symbol, symbol, 0m, 0m, 0m, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<KLinePointDto>> GetKLineAsync(string symbol, string interval, int count, CancellationToken cancellationToken = default)
    {
        var result = await TryGetAsync(c => c.GetKLineAsync(symbol, interval, count, cancellationToken));
        return result ?? Array.Empty<KLinePointDto>();
    }

    public async Task<IReadOnlyList<MinuteLinePointDto>> GetMinuteLineAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var result = await TryGetAsync(c => c.GetMinuteLineAsync(symbol, cancellationToken));
        return result ?? Array.Empty<MinuteLinePointDto>();
    }

    public async Task<IReadOnlyList<IntradayMessageDto>> GetIntradayMessagesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var all = new List<IntradayMessageDto>();
        foreach (var crawler in _crawlers)
        {
            try
            {
                var result = await crawler.GetIntradayMessagesAsync(symbol, cancellationToken);
                if (result.Count > 0)
                {
                    all.AddRange(result);
                }
            }
            catch
            {
                // 忽略单一来源错误
            }
        }

        return all
            .OrderByDescending(x => x.PublishedAt)
            .Take(30)
            .ToArray();
    }

    private async Task<T?> TryGetAsync<T>(Func<IStockCrawlerSource, Task<T>> action) where T : class
    {
        foreach (var crawler in _crawlers)
        {
            try
            {
                var result = await action(crawler);
                if (result is not null)
                {
                    return result;
                }
            }
            catch
            {
                // 忽略单一来源错误
            }
        }

        return null;
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
