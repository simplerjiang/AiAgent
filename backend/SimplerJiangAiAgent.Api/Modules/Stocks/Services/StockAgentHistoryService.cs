using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed class StockAgentHistoryService : IStockAgentHistoryService
{
    private readonly AppDbContext _dbContext;

    public StockAgentHistoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StockAgentAnalysisHistory> AddAsync(StockAgentAnalysisHistory entry, CancellationToken cancellationToken = default)
    {
        _dbContext.StockAgentAnalysisHistories.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<IReadOnlyList<StockAgentAnalysisHistory>> GetListAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockAgentAnalysisHistories
            .Where(x => x.Symbol == symbol)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockAgentAnalysisHistory?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockAgentAnalysisHistories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
