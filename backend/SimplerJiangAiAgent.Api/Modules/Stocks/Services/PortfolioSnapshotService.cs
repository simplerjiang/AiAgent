using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IPortfolioSnapshotService
{
    Task<PortfolioSnapshotDto> GetSnapshotAsync();
    Task<PortfolioSettingsDto> GetSettingsAsync();
    Task<PortfolioSettingsDto> UpdateSettingsAsync(PortfolioSettingsUpdateDto dto);
    Task<IReadOnlyList<PositionItemDto>> GetPositionsAsync();
    Task<PositionItemDto?> GetPositionAsync(string symbol);
    Task<PortfolioExposureDto> GetExposureAsync();
    Task<PortfolioContextDto> GetPortfolioContextAsync();
}

public sealed class PortfolioSnapshotService : IPortfolioSnapshotService
{
    private readonly AppDbContext _db;
    private readonly IStockDataService _stockDataService;

    public PortfolioSnapshotService(AppDbContext db, IStockDataService stockDataService)
    {
        _db = db;
        _stockDataService = stockDataService;
    }

    public async Task<PortfolioSnapshotDto> GetSnapshotAsync()
    {
        var settings = await GetOrCreateSettingsAsync();
        var positions = await LoadActivePositionsAsync();

        var totalCapital = settings.TotalCapital;
        var totalCost = positions.Sum(p => p.TotalCost);
        var totalMarketValue = positions.Sum(GetMarketValueOrCost);
        var totalUnrealizedPnL = positions.Sum(p => p.UnrealizedPnL ?? 0);
        var availableCash = totalCapital - totalCost;
        var totalPositionRatio = totalCapital > 0 ? totalCost / totalCapital : 0;

        var items = positions.Select(p => MapPositionToDto(p, totalCapital)).ToList();

        return new PortfolioSnapshotDto(
            totalCapital, totalCost, totalMarketValue,
            totalUnrealizedPnL, availableCash, totalPositionRatio, items);
    }

    public async Task<PortfolioSettingsDto> GetSettingsAsync()
    {
        var settings = await GetOrCreateSettingsAsync();
        return new PortfolioSettingsDto(settings.TotalCapital, settings.UpdatedAt);
    }

    public async Task<PortfolioSettingsDto> UpdateSettingsAsync(PortfolioSettingsUpdateDto dto)
    {
        var settings = await GetOrCreateSettingsAsync();
        settings.TotalCapital = dto.TotalCapital;
        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new PortfolioSettingsDto(settings.TotalCapital, settings.UpdatedAt);
    }

    public async Task<IReadOnlyList<PositionItemDto>> GetPositionsAsync()
    {
        var settings = await GetOrCreateSettingsAsync();
        var positions = await LoadActivePositionsAsync();

        return positions.Select(p => MapPositionToDto(p, settings.TotalCapital)).ToList();
    }

    public async Task<PositionItemDto?> GetPositionAsync(string symbol)
    {
        var settings = await GetOrCreateSettingsAsync();
        var position = await _db.StockPositions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Symbol == symbol);

        if (position is null) return null;
        await EnrichMissingNamesAsync(new List<StockPosition> { position });
        await RefreshRealtimeMetricsAsync(new List<StockPosition> { position });
        return MapPositionToDto(position, settings.TotalCapital);
    }

    private async Task<List<StockPosition>> LoadActivePositionsAsync()
    {
        var positions = await _db.StockPositions
            .AsNoTracking()
            .Where(p => p.QuantityLots > 0)
            .ToListAsync();

        await EnrichMissingNamesAsync(positions);
        await RefreshRealtimeMetricsAsync(positions);

        return positions;
    }

    private async Task<UserPortfolioSettings> GetOrCreateSettingsAsync()
    {
        var settings = await _db.UserPortfolioSettings.FirstOrDefaultAsync();
        if (settings is not null) return settings;

        settings = new UserPortfolioSettings
        {
            TotalCapital = 0,
            UpdatedAt = DateTime.UtcNow
        };
        _db.UserPortfolioSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    private static PositionItemDto MapPositionToDto(StockPosition p, decimal totalCapital)
    {
        var positionRatio = totalCapital > 0 ? p.TotalCost / totalCapital : p.PositionRatio;
        return new PositionItemDto(
            p.Id, p.Symbol, p.Name, p.QuantityLots, p.AverageCostPrice,
            p.TotalCost, p.LatestPrice, p.MarketValue,
            p.UnrealizedPnL, p.UnrealizedReturnRate, positionRatio);
    }

    private async Task RefreshRealtimeMetricsAsync(List<StockPosition> positions)
    {
        if (positions.Count == 0)
        {
            return;
        }

        var quoteTasks = positions
            .Where(p => !string.IsNullOrWhiteSpace(p.Symbol))
            .Select(async position =>
            {
                try
                {
                    var quote = await _stockDataService.GetQuoteAsync(position.Symbol);
                    return (Position: position, Quote: quote, Succeeded: true);
                }
                catch
                {
                    return (Position: position, Quote: default(StockQuoteDto), Succeeded: false);
                }
            })
            .ToList();

        if (quoteTasks.Count == 0)
        {
            return;
        }

        var quoteResults = await Task.WhenAll(quoteTasks);
        foreach (var result in quoteResults)
        {
            if (!result.Succeeded || result.Quote is null || result.Quote.Price <= 0)
            {
                continue;
            }

            ApplyRealtimeQuote(result.Position, result.Quote);
        }
    }

    private static void ApplyRealtimeQuote(StockPosition position, StockQuoteDto quote)
    {
        var latestPrice = decimal.Round(quote.Price, 2, MidpointRounding.AwayFromZero);
        var marketValue = decimal.Round(latestPrice * position.QuantityLots, 2, MidpointRounding.AwayFromZero);
        var unrealizedPnL = decimal.Round(marketValue - position.TotalCost, 2, MidpointRounding.AwayFromZero);

        position.LatestPrice = latestPrice;
        position.MarketValue = marketValue;
        position.UnrealizedPnL = unrealizedPnL;
        position.UnrealizedReturnRate = position.TotalCost != 0
            ? decimal.Round(unrealizedPnL / position.TotalCost, 6, MidpointRounding.AwayFromZero)
            : position.UnrealizedReturnRate;

        if (string.IsNullOrWhiteSpace(position.Name) && !string.IsNullOrWhiteSpace(quote.Name))
        {
            position.Name = quote.Name;
        }
    }

    private static decimal GetMarketValueOrCost(StockPosition position)
    {
        return position.MarketValue ?? position.TotalCost;
    }

    /// <summary>B36: 补全缺失的名称（只读内存补全，不写库）</summary>
    private async Task EnrichMissingNamesAsync(List<StockPosition> positions)
    {
        foreach (var p in positions)
        {
            if (!string.IsNullOrWhiteSpace(p.Name)) continue;

            var snapshot = await _db.StockQuoteSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Symbol == p.Symbol);
            if (snapshot is not null && !string.IsNullOrWhiteSpace(snapshot.Name))
            {
                p.Name = snapshot.Name;
                continue;
            }

            var w = await _db.ActiveWatchlists
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Symbol == p.Symbol);
            if (w is not null && !string.IsNullOrWhiteSpace(w.Name))
                p.Name = w.Name;
        }
    }

    public async Task<PortfolioExposureDto> GetExposureAsync()
    {
        var snapshot = await GetSnapshotAsync();
        var totalCapital = snapshot.TotalCapital;

        // Real exposure from positions
        var totalExposure = totalCapital > 0 ? snapshot.TotalMarketValue / totalCapital : 0;

        // Pending plans exposure estimate
        var pendingPlans = await _db.TradingPlans
            .AsNoTracking()
            .Where(p => p.Status == TradingPlanStatus.Pending || p.Status == TradingPlanStatus.Draft)
            .ToListAsync();

        decimal pendingMarketValue = 0;
        foreach (var plan in pendingPlans)
        {
            // Estimate pending exposure from suggested position scale and trigger price
            var scale = plan.SuggestedPositionScale ?? 0.1m;
            var estimatedValue = totalCapital * scale;
            pendingMarketValue += estimatedValue;
        }

        var pendingExposure = totalCapital > 0 ? pendingMarketValue / totalCapital : 0;
        var combinedExposure = totalExposure + pendingExposure;

        // Symbol-level exposures
        var symbolExposures = snapshot.Positions
            .Where(p => p.MarketValue.HasValue && p.MarketValue > 0)
            .Select(p => new SymbolExposureDto(
                p.Symbol, p.Name,
                totalCapital > 0 ? (p.MarketValue ?? 0) / totalCapital : 0,
                p.MarketValue ?? 0))
            .ToList();

        // Sector exposures — sector data not yet available on positions; return empty for now
        var sectorExposures = new List<SectorExposureDto>();

        // Market execution mode from latest sentiment snapshot
        MarketExecutionModeDto? currentMode = null;
        try
        {
            var latestSentiment = await _db.MarketSentimentSnapshots
                .AsNoTracking()
                .OrderByDescending(s => s.SnapshotTime)
                .FirstOrDefaultAsync();
            if (latestSentiment is not null)
            {
                currentMode = MarketExecutionModeMapper.GetMode(latestSentiment.StageLabel);
            }
        }
        catch { /* non-critical */ }

        return new PortfolioExposureDto(totalExposure, pendingExposure, combinedExposure, symbolExposures, sectorExposures, currentMode);
    }

    public async Task<PortfolioContextDto> GetPortfolioContextAsync()
    {
        var snapshot = await GetSnapshotAsync();
        var positions = snapshot.Positions.Select(p =>
            new PortfolioContextPositionDto(
                p.Symbol, p.Name, p.Quantity, p.AverageCost,
                p.LatestPrice, p.MarketValue, p.UnrealizedPnL, p.PositionRatio))
            .ToList();

        return new PortfolioContextDto(
            snapshot.TotalCapital,
            snapshot.TotalMarketValue,
            snapshot.TotalPositionRatio,
            snapshot.AvailableCash,
            snapshot.TotalUnrealizedPnL,
            positions);
    }
}
