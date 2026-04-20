using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class PortfolioSnapshotServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_ShouldRecalculatePnL_FromRealtimeQuote()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserPortfolioSettings.Add(new UserPortfolioSettings
        {
            TotalCapital = 100000m,
            UpdatedAt = DateTime.UtcNow
        });
        dbContext.StockPositions.Add(new StockPosition
        {
            Symbol = "sh600000",
            Name = "浦发银行",
            QuantityLots = 1000,
            AverageCostPrice = 10m,
            TotalCost = 10000m,
            LatestPrice = 10m,
            MarketValue = 10000m,
            UnrealizedPnL = 0m,
            UnrealizedReturnRate = 0m,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var quoteService = new FakeStockDataService();
        quoteService.SetQuote("sh600000", 12.34m, "浦发银行");

        var service = new PortfolioSnapshotService(dbContext, quoteService);

        var snapshot = await service.GetSnapshotAsync();

        var position = Assert.Single(snapshot.Positions);
        Assert.Equal(12.34m, position.LatestPrice);
        Assert.Equal(12340m, position.MarketValue);
        Assert.Equal(2340m, position.UnrealizedPnL);
        Assert.Equal(0.234m, position.UnrealizedReturnRate);
        Assert.Equal(12340m, snapshot.TotalMarketValue);
        Assert.Equal(2340m, snapshot.TotalUnrealizedPnL);
    }

    [Fact]
    public async Task GetSnapshotAsync_ShouldFallbackToPersistedValues_WhenSingleQuoteFails()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserPortfolioSettings.Add(new UserPortfolioSettings
        {
            TotalCapital = 50000m,
            UpdatedAt = DateTime.UtcNow
        });
        dbContext.StockPositions.AddRange(
            new StockPosition
            {
                Symbol = "sh600000",
                Name = "浦发银行",
                QuantityLots = 1000,
                AverageCostPrice = 10m,
                TotalCost = 10000m,
                LatestPrice = 10m,
                MarketValue = 10000m,
                UnrealizedPnL = 0m,
                UnrealizedReturnRate = 0m,
                UpdatedAt = DateTime.UtcNow
            },
            new StockPosition
            {
                Symbol = "sz000001",
                Name = "平安银行",
                QuantityLots = 1000,
                AverageCostPrice = 7.5m,
                TotalCost = 7500m,
                LatestPrice = 8m,
                MarketValue = 8000m,
                UnrealizedPnL = 500m,
                UnrealizedReturnRate = 0.066667m,
                UpdatedAt = DateTime.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var quoteService = new FakeStockDataService();
        quoteService.SetQuote("sh600000", 11m, "浦发银行");
        quoteService.ThrowFor("sz000001");

        var service = new PortfolioSnapshotService(dbContext, quoteService);

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal(19000m, snapshot.TotalMarketValue);
        Assert.Equal(1500m, snapshot.TotalUnrealizedPnL);

        var updated = Assert.Single(snapshot.Positions.Where(p => p.Symbol == "sh600000"));
        Assert.Equal(11m, updated.LatestPrice);
        Assert.Equal(11000m, updated.MarketValue);
        Assert.Equal(1000m, updated.UnrealizedPnL);

        var fallback = Assert.Single(snapshot.Positions.Where(p => p.Symbol == "sz000001"));
        Assert.Equal(8m, fallback.LatestPrice);
        Assert.Equal(8000m, fallback.MarketValue);
        Assert.Equal(500m, fallback.UnrealizedPnL);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private sealed class FakeStockDataService : IStockDataService
    {
        private readonly Dictionary<string, StockQuoteDto> _quotes = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _symbolsThatThrow = new(StringComparer.OrdinalIgnoreCase);

        public void SetQuote(string symbol, decimal price, string name)
        {
            _quotes[symbol] = new StockQuoteDto(
                symbol,
                name,
                price,
                0m,
                0m,
                0m,
                0m,
                price,
                price,
                0m,
                DateTime.UtcNow,
                Array.Empty<StockNewsDto>(),
                Array.Empty<StockIndicatorDto>());
        }

        public void ThrowFor(string symbol)
        {
            _symbolsThatThrow.Add(symbol);
        }

        public Task<StockQuoteDto> GetQuoteAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
        {
            if (_symbolsThatThrow.Contains(symbol))
            {
                throw new InvalidOperationException($"quote failed for {symbol}");
            }

            if (_quotes.TryGetValue(symbol, out var quote))
            {
                return Task.FromResult(quote);
            }

            throw new InvalidOperationException($"Missing fake quote for {symbol}");
        }

        public Task<MarketIndexDto> GetMarketIndexAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<KLinePointDto>> GetKLineAsync(string symbol, string interval, int count, string? source = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<MinuteLinePointDto>> GetMinuteLineAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<IntradayMessageDto>> GetIntradayMessagesAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}