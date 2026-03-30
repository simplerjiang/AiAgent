using SimplerJiangAiAgent.Api.Infrastructure.Jobs;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class LocalFactIngestionServiceTests
{
    [Fact]
    public void BuildDomesticMarketReports_WhenKeywordsMissing_ShouldFallbackToRecentMessages()
    {
        var messages = new[]
        {
            new IntradayMessageDto("题材轮动观察", "新浪", new DateTime(2026, 3, 12, 9, 30, 0), "https://example.com/market")
        };

        var result = LocalFactIngestionService.BuildDomesticMarketReports(messages, new DateTime(2026, 3, 12, 9, 35, 0));

        Assert.Single(result);
        Assert.Equal("market", result[0].Level);
        Assert.Equal("sina-roll-market", result[0].SourceTag);
    }

    [Fact]
    public void BuildDomesticMarketReports_PrefersKeywordMatchedMessages()
    {
        var messages = new[]
        {
            new IntradayMessageDto("题材轮动观察", "新浪", new DateTime(2026, 3, 12, 9, 30, 0), "https://example.com/a"),
            new IntradayMessageDto("北向资金午评：指数震荡", "新浪", new DateTime(2026, 3, 12, 10, 0, 0), "https://example.com/b")
        };

        var result = LocalFactIngestionService.BuildDomesticMarketReports(messages, new DateTime(2026, 3, 12, 10, 5, 0));

        Assert.Single(result);
        Assert.Equal("北向资金午评：指数震荡", result[0].Title);
    }

    [Fact]
    public void BuildDomesticMarketReports_ShouldDropStaleMessages()
    {
        var messages = new[]
        {
            new IntradayMessageDto("去年市场回顾", "新浪", new DateTime(2025, 1, 24, 8, 0, 0, DateTimeKind.Utc), "https://example.com/old"),
            new IntradayMessageDto("A股午评：指数震荡", "新浪", new DateTime(2026, 3, 12, 10, 0, 0, DateTimeKind.Utc), "https://example.com/new")
        };

        var result = LocalFactIngestionService.BuildDomesticMarketReports(messages, new DateTime(2026, 3, 13, 0, 0, 0, DateTimeKind.Utc));

        var item = Assert.Single(result);
        Assert.Equal("A股午评：指数震荡", item.Title);
    }

    [Fact]
    public void BuildDomesticMarketReports_ShouldDropBlockedSelfMediaSources()
    {
        var messages = new[]
        {
            new IntradayMessageDto("A股午评：指数震荡", "自媒体热点", new DateTime(2026, 3, 12, 10, 0, 0, DateTimeKind.Utc), "https://example.com/blocked"),
            new IntradayMessageDto("大盘午评：成交额放大", "新浪财经", new DateTime(2026, 3, 12, 10, 5, 0, DateTimeKind.Utc), "https://example.com/ok")
        };

        var result = LocalFactIngestionService.BuildDomesticMarketReports(messages, new DateTime(2026, 3, 12, 10, 6, 0, DateTimeKind.Utc));

        var item = Assert.Single(result);
        Assert.Equal("大盘午评：成交额放大", item.Title);
        Assert.DoesNotContain(result, entry => entry.Source.Contains("自媒体", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EnsureMarketFreshAsync_WhenFreshButPendingAi_ShouldProcessPendingRows()
    {
        await using var dbContext = CreateDbContext();
        dbContext.LocalSectorReports.Add(new LocalSectorReport
        {
            Level = "market",
            SectorName = "大盘环境",
            Title = "待补标的大盘资讯",
            Source = "WSJ US Business",
            SourceTag = "wsj-us-business-rss",
            PublishTime = new DateTime(2026, 3, 13, 8, 0, 0, DateTimeKind.Utc),
            CrawledAt = DateTime.UtcNow,
            IsAiProcessed = false
        });
        await dbContext.SaveChangesAsync();

        var aiService = new StubAiEnrichmentService();
        var service = CreateService(dbContext, aiService);

        await service.EnsureMarketFreshAsync();

        Assert.Equal(1, aiService.MarketCalls);
    }

    [Fact]
    public async Task EnsureFreshAsync_WhenFreshButPendingAi_ShouldProcessSymbolRows()
    {
        await using var dbContext = CreateDbContext();
        var crawledAt = DateTime.UtcNow;
        dbContext.LocalStockNews.Add(new LocalStockNews
        {
            Symbol = "sh600000",
            Name = "浦发银行",
            SectorName = "银行",
            Title = "待补标个股资讯",
            Category = "company_news",
            Source = "新浪",
            SourceTag = "sina-company-news",
            PublishTime = new DateTime(2026, 3, 13, 8, 0, 0, DateTimeKind.Utc),
            CrawledAt = crawledAt,
            IsAiProcessed = false
        });
        dbContext.LocalSectorReports.Add(new LocalSectorReport
        {
            Symbol = "sh600000",
            SectorName = "银行",
            Level = "sector",
            Title = "待补标板块资讯",
            Source = "新浪财经搜索",
            SourceTag = "sina-sector-search",
            PublishTime = new DateTime(2026, 3, 13, 8, 30, 0, DateTimeKind.Utc),
            CrawledAt = crawledAt,
            IsAiProcessed = false
        });
        dbContext.LocalSectorReports.Add(new LocalSectorReport
        {
            Level = "market",
            SectorName = "大盘环境",
            Title = "已是新鲜的大盘资讯",
            Source = "WSJ US Business",
            SourceTag = "wsj-us-business-rss",
            PublishTime = new DateTime(2026, 3, 13, 7, 0, 0, DateTimeKind.Utc),
            CrawledAt = crawledAt,
            IsAiProcessed = true
        });
        await dbContext.SaveChangesAsync();

        var aiService = new StubAiEnrichmentService();
        var service = CreateService(dbContext, aiService);

        await service.EnsureFreshAsync("sh600000");

        Assert.Contains("sh600000", aiService.SymbolCalls);
    }

    [Fact]
    public void MergeStockNewsEntities_ShouldPreserveCachedEvidenceFieldsOnMatchedRows()
    {
        using var dbContext = CreateDbContext();
        var existing = new LocalStockNews
        {
            Id = 12,
            Symbol = "sh600000",
            Name = "浦发银行",
            SectorName = "银行",
            Title = "原公告",
            Category = "announcement",
            Source = "东方财富公告",
            SourceTag = "eastmoney-announcement",
            ExternalId = "notice-1",
            PublishTime = new DateTime(2026, 3, 17, 8, 0, 0, DateTimeKind.Utc),
            CrawledAt = new DateTime(2026, 3, 17, 8, 5, 0, DateTimeKind.Utc),
            Url = "https://example.com/notice-1",
            IsAiProcessed = true,
            AiSentiment = "利好",
            AiTarget = "个股:浦发银行",
            AiTags = "公告,分红",
            ArticleExcerpt = "缓存摘录",
            ArticleSummary = "缓存摘要",
            ReadMode = "url_fetched",
            ReadStatus = "full_text_read",
            IngestedAt = new DateTime(2026, 3, 17, 8, 10, 0, DateTimeKind.Utc)
        };
        dbContext.LocalStockNews.Add(existing);
        dbContext.SaveChanges();

        var incoming = new[]
        {
            new LocalStockNewsSeed(
                "sh600000",
                "浦发银行",
                "银行",
                "原公告",
                "announcement",
                "东方财富公告",
                "eastmoney-announcement",
                "notice-1",
                new DateTime(2026, 3, 17, 8, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 17, 10, 0, 0, DateTimeKind.Utc),
                "https://example.com/notice-1")
        };

        LocalFactIngestionService.MergeStockNewsEntities(
            dbContext.LocalStockNews.ToList(),
            incoming,
            dbContext.LocalStockNews);

        var row = Assert.Single(dbContext.LocalStockNews.Local);
        Assert.Same(existing, row);
        Assert.Equal("缓存摘录", row.ArticleExcerpt);
        Assert.Equal("缓存摘要", row.ArticleSummary);
        Assert.Equal("url_fetched", row.ReadMode);
        Assert.Equal("full_text_read", row.ReadStatus);
        Assert.Equal(new DateTime(2026, 3, 17, 10, 0, 0, DateTimeKind.Utc), row.CrawledAt);
    }

    [Fact]
    public async Task EnsureFreshAsync_SecondCallWithinSkipWindow_ShouldSkipCrawl()
    {
        await using var dbContext = CreateDbContext();
        var aiService = new StubAiEnrichmentService();
        var httpHandler = new CountingHttpMessageHandler();
        var service = new LocalFactIngestionService(
            dbContext,
            new HttpClient(httpHandler),
            Options.Create(new StockSyncOptions()),
            aiService,
            NullLogger<LocalFactIngestionService>.Instance);

        // First call — will attempt HTTP (and fail via stub), setting the crawl timestamp
        await service.EnsureFreshAsync("sh600000");
        var firstCallHttpCount = httpHandler.CallCount;

        // Second call — should skip crawl due to skip window, but still process AI pending
        await service.EnsureFreshAsync("sh600000");
        var secondCallHttpCount = httpHandler.CallCount - firstCallHttpCount;

        Assert.Equal(0, secondCallHttpCount);
        // AI enrichment should still be called both times
        Assert.Equal(2, aiService.SymbolCalls.Count(s => s == "sh600000"));
    }

    [Fact]
    public async Task EnsureMarketFreshAsync_SecondCallWithinSkipWindow_ShouldSkipCrawl()
    {
        await using var dbContext = CreateDbContext();
        var aiService = new StubAiEnrichmentService();
        var httpHandler = new CountingHttpMessageHandler();
        var service = new LocalFactIngestionService(
            dbContext,
            new HttpClient(httpHandler),
            Options.Create(new StockSyncOptions()),
            aiService,
            NullLogger<LocalFactIngestionService>.Instance);

        // First call — will attempt HTTP crawl
        await service.EnsureMarketFreshAsync();
        var firstCallHttpCount = httpHandler.CallCount;

        // Second call — should skip crawl due to skip window, but still process AI pending
        await service.EnsureMarketFreshAsync();
        var secondCallHttpCount = httpHandler.CallCount - firstCallHttpCount;

        Assert.Equal(0, secondCallHttpCount);
        // AI enrichment should still be called both times
        Assert.Equal(2, aiService.MarketCalls);
    }

    private static LocalFactIngestionService CreateService(AppDbContext dbContext, StubAiEnrichmentService aiService)
    {
        return new LocalFactIngestionService(
            dbContext,
            new HttpClient(new StubHttpMessageHandler()),
            Options.Create(new StockSyncOptions()),
            aiService,
            NullLogger<LocalFactIngestionService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private sealed class StubAiEnrichmentService : ILocalFactAiEnrichmentService
    {
        public int MarketCalls { get; private set; }
        public List<string> SymbolCalls { get; } = new();

        public Task ProcessMarketPendingAsync(CancellationToken cancellationToken = default)
        {
            MarketCalls += 1;
            return Task.CompletedTask;
        }

        public Task ProcessSymbolPendingAsync(string symbol, CancellationToken cancellationToken = default)
        {
            SymbolCalls.Add(symbol);
            return Task.CompletedTask;
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("HTTP should not be called in this test path.");
        }
    }

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":{\"data\":[]}}")
            });
        }
    }
}