using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Market.Models;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class TradeExecutionWorkflowTests
{
    [Fact]
    public async Task RecordTradeAsync_FromPlan_PersistsDeviationAndSnapshots()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        var plan = await SeedPlanScenarioAsync(db, quotePrice: 10.35m, invalidPrice: 9.50m);

        var accounting = CreateTradeAccountingService(db, options, livePrice: 10.40m);
        var executedAt = new DateTime(2026, 4, 20, 9, 35, 0, DateTimeKind.Utc);

        var trade = await accounting.RecordTradeAsync(new TradeExecutionCreateDto(
            plan.Id,
            "000001",
            "平安银行",
            "Buy",
            "Normal",
            10.40m,
            1000,
            executedAt,
            3.2m,
            "README 回归录单",
            null,
            "买入执行",
            null,
            "放量后追入",
            null));

        Assert.Equal(plan.Id, trade.PlanId);
        Assert.Equal(ComplianceTag.DeviatedFromPlan, trade.ComplianceTag);
        var persistedTags = JsonSerializer.Deserialize<string[]>(trade.DeviationTagsJson ?? "[]") ?? Array.Empty<string>();
        Assert.Contains("未按触发位", persistedTags);
        Assert.Contains("超仓", persistedTags);
        Assert.Equal("主场景", trade.ScenarioLabel);
        Assert.Equal("Historical", trade.ScenarioSnapshotType);
        Assert.False(string.IsNullOrWhiteSpace(trade.PositionSnapshotJson));
        Assert.False(string.IsNullOrWhiteSpace(trade.CoachTip));

        var persistedPlan = await db.TradingPlans.FirstAsync(item => item.Id == plan.Id);
        Assert.Equal(TradingPlanStatus.Triggered, persistedPlan.Status);
        Assert.Equal(executedAt, persistedPlan.TriggeredAt);

        var items = await accounting.GetTradesAsync("sz000001", null, null, null);
        var item = Assert.Single(items);
        Assert.Contains("未按触发位", item.DeviationTags);
        Assert.NotNull(item.ScenarioSnapshot);
        Assert.Equal("主场景", item.ScenarioSnapshot!.Label);
        Assert.NotNull(item.PositionSnapshot);
    }

    [Fact]
    public async Task GetPlanInsightAsync_AggregatesExecutionsAndFlagsAbandonCondition()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        var plan = await SeedPlanScenarioAsync(db, quotePrice: 9.40m, invalidPrice: 9.50m);

        db.TradeExecutions.Add(new TradeExecution
        {
            PlanId = plan.Id,
            Symbol = plan.Symbol,
            Name = plan.Name,
            Direction = TradeDirection.Buy,
            TradeType = TradeType.Normal,
            ExecutedPrice = 10.00m,
            Quantity = 500,
            ExecutedAt = new DateTime(2026, 4, 19, 9, 30, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 4, 19, 9, 30, 0, DateTimeKind.Utc),
            ComplianceTag = ComplianceTag.DeviatedFromPlan,
            ExecutionAction = "买入执行",
              DeviationTagsJson = JsonSerializer.Serialize(new[] { "未按触发位", "低于触发价成交" })
        });
        await db.SaveChangesAsync();

        var marketContextService = new StockMarketContextService(options);
        var stockDataService = new StubStockDataService(new StockQuoteDto(
            plan.Symbol,
            plan.Name,
            9.40m,
            -0.20m,
            -2m,
            0m,
            0m,
            0m,
            0m,
            0m,
            DateTime.UtcNow,
            Array.Empty<StockNewsDto>(),
            Array.Empty<StockIndicatorDto>(),
            0m,
            0m,
            null,
            "银行"));
        var portfolioSnapshotService = new PortfolioSnapshotService(db, stockDataService);
        var insightService = new TradeExecutionInsightService(db, marketContextService, portfolioSnapshotService, stockDataService);

        var insight = await insightService.GetPlanInsightAsync(plan, useLiveQuote: false);

        Assert.NotNull(insight);
        Assert.NotNull(insight!.ExecutionSummary);
        Assert.Equal(1, insight.ExecutionSummary!.ExecutionCount);
        Assert.Equal(1, insight.ExecutionSummary.DeviatedCount);
        Assert.DoesNotContain("待复盘", insight.ExecutionSummary.Summary);
        Assert.Equal("放弃条件命中", insight.CurrentScenarioStatus!.Label);
        Assert.True(insight.CurrentScenarioStatus.AbandonTriggered);
        Assert.NotNull(insight.CurrentPositionSnapshot);
        Assert.Equal("Current", insight.CurrentPositionSnapshot!.SnapshotType);
    }

    [Fact]
    public async Task GetPlanInsightsAsync_WhenMarketContextLookupThrows_StillBuildsInsight()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        var plan = await SeedPlanScenarioAsync(db, quotePrice: 10.35m, invalidPrice: 9.50m);

        var stockDataService = new StubStockDataService(new StockQuoteDto(
            plan.Symbol,
            plan.Name,
            10.35m,
            0.15m,
            1.47m,
            0m,
            0m,
            0m,
            0m,
            0m,
            DateTime.UtcNow,
            Array.Empty<StockNewsDto>(),
            Array.Empty<StockIndicatorDto>(),
            0m,
            0m,
            null,
            "银行"));
        var portfolioSnapshotService = new PortfolioSnapshotService(db, stockDataService);
        var insightService = new TradeExecutionInsightService(db, new AlwaysThrowStockMarketContextService(), portfolioSnapshotService, stockDataService);

        var insights = await insightService.GetPlanInsightsAsync(new[] { plan }, useLiveQuote: false);

        var insight = Assert.Single(insights);
        Assert.Equal(plan.Id, insight.Key);
        Assert.NotNull(insight.Value.CurrentScenarioStatus);
        Assert.Equal("主场景", insight.Value.CurrentScenarioStatus!.Label);
        Assert.False(insight.Value.CurrentScenarioStatus.CounterTrendWarning);
    }

    private static TradeAccountingService CreateTradeAccountingService(AppDbContext db, DbContextOptions<AppDbContext> options, decimal livePrice)
    {
        var marketContextService = new StockMarketContextService(options);
        var stockDataService = new StubStockDataService(new StockQuoteDto(
            "sz000001",
            "平安银行",
            livePrice,
            0.15m,
            1.47m,
            0m,
            0m,
            0m,
            0m,
            0m,
            DateTime.UtcNow,
            Array.Empty<StockNewsDto>(),
            Array.Empty<StockIndicatorDto>(),
            0m,
            0m,
            null,
            "银行"));
        var portfolioSnapshotService = new PortfolioSnapshotService(db, stockDataService);
        var insightService = new TradeExecutionInsightService(db, marketContextService, portfolioSnapshotService, stockDataService);
        var complianceService = new TradeComplianceService(db);
        return new TradeAccountingService(db, complianceService, insightService);
    }

    private static async Task<TradingPlan> SeedPlanScenarioAsync(AppDbContext db, decimal quotePrice, decimal invalidPrice)
    {
        db.UserPortfolioSettings.Add(new UserPortfolioSettings
        {
            TotalCapital = 100000m,
            UpdatedAt = DateTime.UtcNow
        });
        db.StockPositions.Add(new StockPosition
        {
            Symbol = "sz000001",
            Name = "平安银行",
            QuantityLots = 1000,
            AverageCostPrice = 9.00m,
            TotalCost = 9000m,
            LatestPrice = quotePrice,
            MarketValue = quotePrice * 1000,
            UnrealizedPnL = (quotePrice - 9.00m) * 1000,
            UnrealizedReturnRate = 0.15m,
            PositionRatio = 0.09m,
            UpdatedAt = DateTime.UtcNow
        });
        db.StockQuoteSnapshots.Add(new StockQuoteSnapshot
        {
            Symbol = "sz000001",
            Name = "平安银行",
            Price = quotePrice,
            Change = 0.12m,
            ChangePercent = 1.2m,
            SectorName = "银行",
            Timestamp = DateTime.UtcNow
        });
        db.MarketSentimentSnapshots.Add(new MarketSentimentSnapshot
        {
            TradingDate = new DateTime(2026, 4, 20),
            SnapshotTime = new DateTime(2026, 4, 20, 6, 45, 0, DateTimeKind.Utc),
            SessionPhase = "盘中",
            StageLabel = "主升",
            StageLabelV2 = "主升",
            StageScore = 80m,
            StageConfidence = 82m,
            SourceTag = "test",
            CreatedAt = DateTime.UtcNow
        });
        db.SectorRotationSnapshots.Add(new SectorRotationSnapshot
        {
            TradingDate = new DateTime(2026, 4, 20),
            SnapshotTime = new DateTime(2026, 4, 20, 6, 45, 0, DateTimeKind.Utc),
            BoardType = "concept",
            SectorCode = "BK001",
            SectorName = "银行",
            RankNo = 1,
            StrengthScore = 85m,
            StrengthAvg5d = 80m,
            StrengthAvg10d = 78m,
            DiffusionRate = 75m,
            MainlineScore = 88m,
            IsMainline = true,
            NewsSentiment = "利好",
            SourceTag = "test",
            CreatedAt = DateTime.UtcNow
        });

        var plan = new TradingPlan
        {
            PlanKey = "plan-test-0001",
            Title = "平安银行突破计划",
            Symbol = "sz000001",
            Name = "平安银行",
            Direction = TradingPlanDirection.Long,
            Status = TradingPlanStatus.Pending,
            TriggerPrice = 10.00m,
            InvalidPrice = invalidPrice,
            StopLossPrice = 9.70m,
            TakeProfitPrice = 10.80m,
            TargetPrice = 11.20m,
            AnalysisSummary = "等待突破确认",
            SourceAgent = "manual",
            SuggestedPositionScale = 0.05m,
            MarketStageLabelAtCreation = "主升",
            StageConfidenceAtCreation = 82m,
            ExecutionFrequencyLabel = "积极",
            MainlineSectorName = "银行",
            MainlineScoreAtCreation = 88m,
            SectorNameAtCreation = "银行",
            SectorCodeAtCreation = "BK001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.TradingPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
    }
}

internal sealed class AlwaysThrowStockMarketContextService : IStockMarketContextService
{
    public Task<StockMarketContextDto?> GetLatestAsync(string symbol, CancellationToken cancellationToken = default)
        => GetLatestAsync(symbol, null, cancellationToken);

    public Task<StockMarketContextDto?> GetLatestAsync(string symbol, string? sectorNameHint, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException($"boom: {symbol}");
}

internal sealed class StubStockDataService : IStockDataService
{
    private readonly StockQuoteDto _quote;

    public StubStockDataService(StockQuoteDto quote)
    {
        _quote = quote;
    }

    public Task<StockQuoteDto> GetQuoteAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_quote with { Symbol = symbol == "000001" ? "sz000001" : symbol });
    }

    public Task<MarketIndexDto> GetMarketIndexAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<KLinePointDto>> GetKLineAsync(string symbol, string interval, int count, string? source = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<MinuteLinePointDto>> GetMinuteLineAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<IntradayMessageDto>> GetIntradayMessagesAsync(string symbol, string? source = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
