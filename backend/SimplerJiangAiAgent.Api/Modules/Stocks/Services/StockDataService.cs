using Microsoft.Extensions.Caching.Memory;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed class StockDataService : IStockDataService
{
    private readonly IMemoryCache _cache;
    private readonly IStockCrawler _defaultCrawler;
    private readonly IEnumerable<IStockCrawlerSource> _sources;

    public StockDataService(IMemoryCache cache, IStockCrawler defaultCrawler, IEnumerable<IStockCrawlerSource> sources)
    {
        _cache = cache;
        _defaultCrawler = defaultCrawler;
        _sources = sources;
    }

    public async Task<StockQuoteDto> GetQuoteAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
    {
        var crawler = ResolveSource(source);
        var cacheKey = $"quote:{crawler.SourceName}:{symbol}";
        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5);
            return await crawler.GetQuoteAsync(symbol, cancellationToken);
        });

        return result ?? new StockQuoteDto(symbol, symbol, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, DateTime.UtcNow, Array.Empty<StockNewsDto>(), Array.Empty<StockIndicatorDto>());
    }

    public async Task<MarketIndexDto> GetMarketIndexAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
    {
        var crawler = ResolveSource(source);
        var cacheKey = $"market:{crawler.SourceName}:{symbol}";
        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await crawler.GetMarketIndexAsync(symbol, cancellationToken);
        });

        return result ?? new MarketIndexDto(symbol, symbol, 0m, 0m, 0m, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<KLinePointDto>> GetKLineAsync(string symbol, string interval, int count, string? source = null, CancellationToken cancellationToken = default)
    {
        var crawler = ResolveSource(source);
        var cacheKey = $"kline:{crawler.SourceName}:{symbol}:{interval}:{count}";
        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await crawler.GetKLineAsync(symbol, interval, count, cancellationToken);
        });

        return result ?? Array.Empty<KLinePointDto>();
    }

    public async Task<IReadOnlyList<MinuteLinePointDto>> GetMinuteLineAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
    {
        var crawler = ResolveSource(source);
        var cacheKey = $"minute:{crawler.SourceName}:{symbol}";
        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await crawler.GetMinuteLineAsync(symbol, cancellationToken);
        });

        return result ?? Array.Empty<MinuteLinePointDto>();
    }

    public async Task<IReadOnlyList<IntradayMessageDto>> GetIntradayMessagesAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
    {
        var crawler = ResolveSource(source);
        var cacheKey = $"messages:{crawler.SourceName}:{symbol}";
        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            return await crawler.GetIntradayMessagesAsync(symbol, cancellationToken);
        });

        return result ?? Array.Empty<IntradayMessageDto>();
    }

    private IStockCrawler ResolveSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return _defaultCrawler;
        }

        var match = _sources.FirstOrDefault(s => s.SourceName.Equals(source, StringComparison.OrdinalIgnoreCase));
        return match ?? _defaultCrawler;
    }
}
