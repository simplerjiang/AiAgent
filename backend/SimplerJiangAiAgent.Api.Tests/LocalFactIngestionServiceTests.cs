using System.Reflection;
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
    public async Task EnsureMarketFreshAsync_ShouldUseRequestPathPendingModeForCrawlAndSkipWindow()
    {
        ResetMarketRefreshState();

        await using var dbContext = CreateDbContext();
        var aiService = new StubAiEnrichmentService();
        var httpHandler = new CountingHttpMessageHandler();
        var service = new LocalFactIngestionService(
            dbContext,
            new HttpClient(httpHandler),
            Options.Create(new StockSyncOptions()),
            aiService,
            NullLogger<LocalFactIngestionService>.Instance);

        await service.EnsureMarketFreshAsync();
        await service.EnsureMarketFreshAsync();

        Assert.Equal(
            [LocalFactMarketPendingMode.RequestPath, LocalFactMarketPendingMode.RequestPath],
            aiService.MarketModes);
    }

    [Fact]
    public async Task EnsureMarketFreshAsync_ShouldCollapseDuplicateStoredMarketRowsBeforeUpsert()
    {
        ResetMarketRefreshState();

        await using var dbContext = CreateDbContext();
        var publishTime = DateTime.UtcNow.AddHours(-2);
        var crawledAt = DateTime.UtcNow.AddHours(-1);
        dbContext.LocalSectorReports.AddRange(
            new LocalSectorReport
            {
                Level = "market",
                SectorName = "大盘环境",
                Title = "Global market braces for Fed decision",
                Source = "Reuters",
                SourceTag = "gnews-reuters",
                ExternalId = "dup-key",
                PublishTime = publishTime,
                CrawledAt = crawledAt,
                Url = "https://example.com/market/dup"
            },
            new LocalSectorReport
            {
                Level = "market",
                SectorName = "大盘环境",
                Title = "Global market braces for Fed decision",
                Source = "Reuters",
                SourceTag = "gnews-reuters",
                ExternalId = "dup-key",
                PublishTime = publishTime,
                CrawledAt = crawledAt.AddMinutes(5),
                Url = "https://example.com/market/dup",
                IsAiProcessed = true,
                TranslatedTitle = "已清洗标题",
                AiSentiment = "利好",
                AiTarget = "大盘",
                AiTags = "[\"宏观货币\"]",
                ArticleExcerpt = "缓存摘录",
                ArticleSummary = "缓存摘要",
                ReadMode = "url_fetched",
                ReadStatus = "full_text_read",
                IngestedAt = crawledAt.AddMinutes(7)
            });
        await dbContext.SaveChangesAsync();

        var aiService = new StubAiEnrichmentService();
        var httpHandler = new StaticRssHttpMessageHandler(
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><rss><channel><item><title>Global market braces for Fed decision</title><link>https://example.com/market/dup</link><guid>dup-key</guid><pubDate>{publishTime:R}</pubDate></item></channel></rss>");
        var service = new LocalFactIngestionService(
            dbContext,
            new HttpClient(httpHandler),
            Options.Create(new StockSyncOptions()),
            aiService,
            NullLogger<LocalFactIngestionService>.Instance);

        await service.EnsureMarketFreshAsync();

        var duplicates = await dbContext.LocalSectorReports
            .Where(item => item.ExternalId == "dup-key")
            .ToListAsync();

        var stored = Assert.Single(duplicates);
        Assert.True(stored.IsAiProcessed);
        Assert.Equal("已清洗标题", stored.TranslatedTitle);
        Assert.Equal("缓存摘要", stored.ArticleSummary);
        Assert.Equal("full_text_read", stored.ReadStatus);
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
    public async Task EnsureFreshAsync_ShouldNotTriggerMarketPendingProcessing()
    {
        ResetSymbolRefreshState();
        SetMarketRefreshState(DateTime.UtcNow);

        await using var dbContext = CreateDbContext();
        var aiService = new StubAiEnrichmentService();
        var service = CreateService(dbContext, aiService);

        await service.EnsureFreshAsync("sh600000");

        Assert.Equal(0, aiService.MarketCalls);
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
        ResetSymbolRefreshState();

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
        ResetMarketRefreshState();

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

    [Fact]
    public async Task EnsureMarketFreshAsync_WhenOptionalFeedsAreSlow_ShouldNotBlockCoreMarketRefresh()
    {
        ResetMarketRefreshState();

        await using var dbContext = CreateDbContext();
        var aiService = new StubAiEnrichmentService();
        var handler = new MarketRefreshBudgetHttpMessageHandler(TimeSpan.FromSeconds(3));
        var service = CreateService(
            dbContext,
            aiService,
            handler,
            marketCoreSourceTimeout: TimeSpan.FromMilliseconds(250),
            marketOptionalSourceTimeout: TimeSpan.FromMilliseconds(50),
            marketOptionalBatchTimeout: TimeSpan.FromMilliseconds(100));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await service.EnsureMarketFreshAsync();
        stopwatch.Stop();

        var marketRows = await dbContext.LocalSectorReports
            .Where(item => item.Level == "market")
            .ToListAsync();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(1500), $"Expected soft-timeout path to finish quickly, actual={stopwatch.Elapsed.TotalMilliseconds}ms");
        Assert.True(handler.OptionalRequestCount > 0);
        Assert.Contains(marketRows, item => item.SourceTag == "sina-roll-market" && item.Title == "A股午评：指数震荡回升");
        Assert.DoesNotContain(marketRows, item => item.Title == handler.OptionalMarketTitle);
        Assert.Equal(1, aiService.MarketCalls);
    }

    [Fact]
    public async Task EnsureMarketFreshAsync_ShouldPersistCoreMarketRowsWithUnchangedContract()
    {
        ResetMarketRefreshState();

        await using var dbContext = CreateDbContext();
        var aiService = new StubAiEnrichmentService();
        var handler = new MarketRefreshBudgetHttpMessageHandler(TimeSpan.Zero);
        var service = CreateService(
            dbContext,
            aiService,
            handler,
            marketCoreSourceTimeout: TimeSpan.FromMilliseconds(250),
            marketOptionalSourceTimeout: TimeSpan.FromMilliseconds(250),
            marketOptionalBatchTimeout: TimeSpan.FromMilliseconds(250));

        await service.EnsureMarketFreshAsync();

        var row = await dbContext.LocalSectorReports
            .Where(item => item.SourceTag == "sina-roll-market")
            .SingleAsync();

        Assert.Equal("market", row.Level);
        Assert.Equal("大盘环境", row.SectorName);
        Assert.Equal("A股午评：指数震荡回升", row.Title);
        Assert.Equal("新浪财经", row.Source);
        Assert.Equal("https://example.com/core-market", row.Url);
        Assert.False(row.IsAiProcessed);
        Assert.Equal(1, aiService.MarketCalls);
    }

    private static LocalFactIngestionService CreateService(
        AppDbContext dbContext,
        StubAiEnrichmentService aiService,
        HttpMessageHandler? handler = null,
        TimeSpan? marketCoreSourceTimeout = null,
        TimeSpan? marketOptionalSourceTimeout = null,
        TimeSpan? marketOptionalBatchTimeout = null)
    {
        return new LocalFactIngestionService(
            dbContext,
            new HttpClient(handler ?? new StubHttpMessageHandler()),
            Options.Create(new StockSyncOptions()),
            aiService,
            NullLogger<LocalFactIngestionService>.Instance,
            marketCoreSourceTimeout,
            marketOptionalSourceTimeout,
            marketOptionalBatchTimeout);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static void ResetMarketRefreshState()
    {
        SetMarketRefreshState(DateTime.MinValue);
    }

    private static void SetMarketRefreshState(DateTime timestamp)
    {
        var field = typeof(LocalFactIngestionService).GetField("_lastMarketCrawlTicks", BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, timestamp.Ticks);
    }

    private static void ResetSymbolRefreshState()
    {
        ClearStaticField("SymbolCrawlTimestamps");
        ClearStaticField("SymbolRefreshGates");
    }

    private static void ClearStaticField(string fieldName)
    {
        var field = typeof(LocalFactIngestionService).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        var value = field?.GetValue(null);
        value?.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public)?.Invoke(value, null);
    }

    private sealed class StubAiEnrichmentService : ILocalFactAiEnrichmentService
    {
        public int MarketCalls { get; private set; }
        public List<LocalFactMarketPendingMode> MarketModes { get; } = [];
        public List<string> SymbolCalls { get; } = new();

        public Task ProcessMarketPendingAsync(
            CancellationToken cancellationToken = default,
            LocalFactMarketPendingMode mode = LocalFactMarketPendingMode.Default)
        {
            MarketCalls += 1;
            MarketModes.Add(mode);
            return Task.CompletedTask;
        }

        public Task ProcessSymbolPendingAsync(string symbol, CancellationToken cancellationToken = default)
        {
            SymbolCalls.Add(symbol);
            return Task.CompletedTask;
        }

        public Task<LocalFactPendingProcessSummary> ProcessPendingBatchAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LocalFactPendingProcessSummary(
                new LocalFactPendingCounts(0, 0, 0),
                new LocalFactPendingCounts(0, 0, 0),
                true,
                null,
                new LocalFactPendingContinuation(false, "completed")));
        }

        public Task<LocalFactPendingCounts> GetPendingCountsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LocalFactPendingCounts(0, 0, 0));
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

    private sealed class StaticRssHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _xml;

        public StaticRssHttpMessageHandler(string xml)
        {
            _xml = xml;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_xml)
            });
        }
    }

    private sealed class MarketRefreshBudgetHttpMessageHandler : HttpMessageHandler
    {
        private int _optionalRequestCount;

        public MarketRefreshBudgetHttpMessageHandler(TimeSpan optionalDelay)
        {
            OptionalDelay = optionalDelay;
        }

        public TimeSpan OptionalDelay { get; }

        public int OptionalRequestCount => _optionalRequestCount;

        public string OptionalMarketTitle => "Global market pulse cools after Fed decision";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;
            if (url.Contains("feed.mix.sina.com.cn/api/roll/get", StringComparison.OrdinalIgnoreCase))
            {
                var recentRollTime = DateTime.UtcNow.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss");
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"result\":{{\"data\":[{{\"title\":\"A股午评：指数震荡回升\",\"url\":\"https://example.com/core-market\",\"media_name\":\"新浪财经\",\"ctime\":\"{recentRollTime}\"}}]}}}}")
                };
            }

            if (url.Contains("np-listapi.eastmoney.com/comm/web/getNewsByColumns", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":{\"list\":[]}}")
                };
            }

            if (url.Contains("cls.cn/nodeapi/updateTelegraphList", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":{\"roll_data\":[]}}")
                };
            }

            System.Threading.Interlocked.Increment(ref _optionalRequestCount);
            if (OptionalDelay > TimeSpan.Zero)
            {
                await Task.Delay(OptionalDelay, cancellationToken);
            }

            var publishedAt = DateTime.UtcNow.AddMinutes(-5).ToString("R");
            var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><rss><channel><item><title>{OptionalMarketTitle}</title><link>https://example.com/optional-market</link><guid>optional-market</guid><pubDate>{publishedAt}</pubDate></item></channel></rss>";
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(xml)
            };
        }
    }
}