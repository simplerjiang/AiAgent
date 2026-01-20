using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IStockCrawler
{
    string SourceName { get; }

    // 获取股票行情与新闻/指标（当前为占位实现）
    Task<StockQuoteDto> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
}
