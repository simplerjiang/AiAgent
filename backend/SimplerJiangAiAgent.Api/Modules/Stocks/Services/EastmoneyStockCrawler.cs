using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed class EastmoneyStockCrawler : IStockCrawlerSource
{
    private readonly HttpClient _httpClient;

    public EastmoneyStockCrawler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string SourceName => "东方财富";

    public async Task<StockQuoteDto> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var secId = ToEastmoneySecId(symbol);
        var url = $"https://push2.eastmoney.com/api/qt/stock/get?secid={secId}&fields=f58,f43,f60,f170";
        var json = await _httpClient.GetStringAsync(url, cancellationToken);
        return EastmoneyStockParser.ParseQuote(symbol, json);
    }

    public Task<MarketIndexDto> GetMarketIndexAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return GetQuoteAsync(symbol, cancellationToken)
            .ContinueWith(task =>
            {
                var quote = task.Result;
                return new MarketIndexDto(quote.Symbol, quote.Name, quote.Price, quote.Change, quote.ChangePercent, quote.Timestamp);
            }, cancellationToken);
    }

    public Task<IReadOnlyList<KLinePointDto>> GetKLineAsync(string symbol, string interval, int count, CancellationToken cancellationToken = default)
    {
        // 东方财富 K 线接口在当前测试环境未返回数据，暂不启用
        IReadOnlyList<KLinePointDto> result = Array.Empty<KLinePointDto>();
        return Task.FromResult(result);
    }

    public async Task<IReadOnlyList<MinuteLinePointDto>> GetMinuteLineAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var secId = ToEastmoneySecId(symbol);
        var url = $"https://push2.eastmoney.com/api/qt/stock/trends2/get?secid={secId}&fields1=f1,f2,f3&fields2=f51,f52,f53,f54,f55,f56,f57,f58&ndays=1";
        var json = await _httpClient.GetStringAsync(url, cancellationToken);
        return EastmoneyStockParser.ParseTrends(symbol, json);
    }

    public Task<IReadOnlyList<IntradayMessageDto>> GetIntradayMessagesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // 暂无稳定接口
        IReadOnlyList<IntradayMessageDto> result = Array.Empty<IntradayMessageDto>();
        return Task.FromResult(result);
    }

    private static string ToEastmoneySecId(string symbol)
    {
        var normalized = StockSymbolNormalizer.Normalize(symbol);
        var code = normalized.Replace("sh", string.Empty).Replace("sz", string.Empty);
        var market = normalized.StartsWith("sh") ? "1" : "0";
        return $"{market}.{code}";
    }
}
