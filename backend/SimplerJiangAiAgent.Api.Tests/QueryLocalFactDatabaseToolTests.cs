using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class QueryLocalFactDatabaseToolTests
{
    [Fact]
    public async Task QueryAsync_ShouldReturnStockSectorAndMarketBuckets()
    {
        await using var dbContext = CreateDbContext();
        dbContext.LocalStockNews.Add(new LocalStockNews
        {
            Symbol = "sh600000",
            Name = "浦发银行",
            SectorName = "银行",
            Title = "浦发银行公告",
            Category = "announcement",
            Source = "东方财富公告",
            SourceTag = "eastmoney-announcement",
            PublishTime = new DateTime(2026, 3, 12, 9, 30, 0),
            CrawledAt = new DateTime(2026, 3, 12, 9, 31, 0),
            Url = "https://example.com/a"
        });
        dbContext.LocalSectorReports.AddRange(
            new LocalSectorReport
            {
                Symbol = "sh600000",
                SectorName = "银行",
                Level = "sector",
                Title = "银行板块震荡上行",
                Source = "新浪",
                SourceTag = "sina-roll-sector",
                PublishTime = new DateTime(2026, 3, 12, 9, 0, 0),
                CrawledAt = new DateTime(2026, 3, 12, 9, 31, 0)
            },
            new LocalSectorReport
            {
                Symbol = null,
                SectorName = "大盘环境",
                Level = "market",
                Title = "A股早评：指数震荡",
                Source = "新浪",
                SourceTag = "sina-roll-market",
                PublishTime = new DateTime(2026, 3, 12, 8, 50, 0),
                CrawledAt = new DateTime(2026, 3, 12, 9, 31, 0)
            });
        await dbContext.SaveChangesAsync();

        var tool = new QueryLocalFactDatabaseTool(dbContext);
        var result = await tool.QueryAsync("600000");

        Assert.Equal("sh600000", result.Symbol);
        Assert.Equal("浦发银行", result.Name);
        Assert.Equal("银行", result.SectorName);
        Assert.Single(result.StockNews);
        Assert.Single(result.SectorReports);
        Assert.Single(result.MarketReports);
        Assert.Equal("中性", result.StockNews[0].Sentiment);
        Assert.Equal("中性", result.SectorReports[0].Sentiment);
        Assert.Equal("中性", result.MarketReports[0].Sentiment);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}