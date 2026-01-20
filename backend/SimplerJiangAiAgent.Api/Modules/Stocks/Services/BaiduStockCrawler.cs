using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed class BaiduStockCrawler : IStockCrawler
{
    public string SourceName => "百度";

    public Task<StockQuoteDto> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // TODO: 接入公开接口并解析数据
        var quote = new StockQuoteDto(
            symbol,
            $"{symbol} 示例名称",
            0m,
            0m,
            0m,
            DateTime.UtcNow,
            Array.Empty<StockNewsDto>(),
            Array.Empty<StockIndicatorDto>()
        );
        return Task.FromResult(quote);
    }
}
