using SimplerJiangAiAgent.Api.Data.Entities;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IStockAgentHistoryService
{
    Task<StockAgentAnalysisHistory> AddAsync(StockAgentAnalysisHistory entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockAgentAnalysisHistory>> GetListAsync(string symbol, CancellationToken cancellationToken = default);
    Task<StockAgentAnalysisHistory?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
