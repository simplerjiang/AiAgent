using Baostock.NET.Models;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using SimplerJiangAiAgent.Api.Services;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

/// <summary>
/// K 线历史数据主源：通过 Baostock.NET 获取日/周/月 K 线。
/// 实时行情等接口返回空，由 CompositeStockCrawler 回退到其他源。
/// </summary>
public sealed class BaostockStockCrawler : IStockCrawlerSource
{
    private readonly IBaostockClientFactory _clientFactory;
    private readonly ILogger<BaostockStockCrawler> _logger;

    public BaostockStockCrawler(IBaostockClientFactory clientFactory, ILogger<BaostockStockCrawler> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public string SourceName => "Baostock";

    public async Task<IReadOnlyList<KLinePointDto>> GetKLineAsync(
        string symbol, string interval, int count, CancellationToken ct = default)
    {
        try
        {
            await using var lease = await _clientFactory.GetClientAsync(ct);

            var frequency = MapFrequency(interval);
            var endDate = DateTime.Now.ToString("yyyy-MM-dd");
            var startDate = CalculateStartDate(interval, count);

            var results = new List<KLinePointDto>();
            await foreach (var row in lease.Client.QueryHistoryKDataPlusAsync(
                symbol, null, startDate, endDate, frequency, AdjustFlag.PreAdjust, ct))
            {
                if (row.Open is null || row.High is null || row.Low is null || row.Close is null)
                    continue;

                results.Add(new KLinePointDto(
                    row.Date.ToDateTime(TimeOnly.MinValue),
                    row.Open.Value,
                    row.Close.Value,
                    row.High.Value,
                    row.Low.Value,
                    row.Volume ?? 0));
            }

            return results.TakeLast(count).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Baostock K-line query failed for {Symbol}", symbol);
            return Array.Empty<KLinePointDto>();
        }
    }

    public Task<StockQuoteDto?> GetQuoteAsync(string symbol, CancellationToken ct = default)
        => Task.FromResult<StockQuoteDto?>(null);

    public Task<MarketIndexDto> GetMarketIndexAsync(string symbol, CancellationToken ct = default)
        => Task.FromResult(new MarketIndexDto(symbol, symbol, 0m, 0m, 0m, DateTime.UtcNow));

    public Task<IReadOnlyList<MinuteLinePointDto>> GetMinuteLineAsync(string symbol, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<MinuteLinePointDto>>(Array.Empty<MinuteLinePointDto>());

    public Task<IReadOnlyList<IntradayMessageDto>> GetIntradayMessagesAsync(string symbol, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<IntradayMessageDto>>(Array.Empty<IntradayMessageDto>());

    private static KLineFrequency MapFrequency(string interval)
    {
        return interval?.ToLowerInvariant() switch
        {
            "week" => KLineFrequency.Week,
            "month" => KLineFrequency.Month,
            _ => KLineFrequency.Day
        };
    }

    private static string CalculateStartDate(string interval, int count)
    {
        var days = interval?.ToLowerInvariant() switch
        {
            "week" => count * 7 + 30,
            "month" => count * 31 + 60,
            _ => count * 2 + 30 // 交易日约占日历日 ~60%
        };
        return DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd");
    }
}
