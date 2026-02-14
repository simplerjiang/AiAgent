using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IStockSearchService
{
    Task<IReadOnlyList<StockSearchResultDto>> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default);
}
