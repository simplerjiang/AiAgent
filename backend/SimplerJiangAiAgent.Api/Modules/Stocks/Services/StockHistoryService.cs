using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed class StockHistoryService : IStockHistoryService
{
    private readonly AppDbContext _dbContext;
    private readonly IStockDataService _dataService;

    public StockHistoryService(AppDbContext dbContext, IStockDataService dataService)
    {
        _dbContext = dbContext;
        _dataService = dataService;
    }

    public async Task UpsertAsync(StockQuoteDto quote, CancellationToken cancellationToken = default)
    {
        var symbol = StockSymbolNormalizer.Normalize(quote.Symbol);
        var existing = await _dbContext.StockQueryHistories
            .FirstOrDefaultAsync(x => x.Symbol == symbol, cancellationToken);

        if (existing is null)
        {
            existing = new StockQueryHistory { Symbol = symbol };
            _dbContext.StockQueryHistories.Add(existing);
        }

        existing.Name = quote.Name;
        existing.Price = quote.Price;
        existing.ChangePercent = quote.ChangePercent;
        existing.TurnoverRate = quote.TurnoverRate;
        existing.PeRatio = quote.PeRatio;
        existing.High = quote.High;
        existing.Low = quote.Low;
        existing.Speed = quote.Speed;
        existing.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockQueryHistory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockQueryHistories
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockQueryHistory>> RefreshAsync(string? source = null, CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.StockQueryHistories
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var item in list)
        {
            var quote = await _dataService.GetQuoteAsync(item.Symbol, source, cancellationToken);
            item.Name = quote.Name;
            item.Price = quote.Price;
            item.ChangePercent = quote.ChangePercent;
            item.TurnoverRate = quote.TurnoverRate;
            item.PeRatio = quote.PeRatio;
            item.High = quote.High;
            item.Low = quote.Low;
            item.Speed = quote.Speed;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return list;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.StockQueryHistories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.StockQueryHistories.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
