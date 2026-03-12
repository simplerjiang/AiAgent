using SimplerJiangAiAgent.Api.Infrastructure.Jobs;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class LocalFactIngestionServiceTests
{
    [Fact]
    public void BuildSectorReports_ShouldMatchSectorAliases()
    {
        var messages = new[]
        {
            new IntradayMessageDto("银行板块盘中拉升", "新浪", new DateTime(2026, 3, 12, 10, 0, 0), "https://example.com/sector")
        };

        var result = LocalFactIngestionService.BuildSectorReports("sh600000", "银行Ⅱ", messages, new DateTime(2026, 3, 12, 10, 5, 0));

        Assert.Single(result);
        Assert.Equal("sector", result[0].Level);
    }

    [Fact]
    public void BuildMarketReports_WhenKeywordsMissing_ShouldFallbackToRecentMessages()
    {
        var messages = new[]
        {
            new IntradayMessageDto("题材轮动观察", "新浪", new DateTime(2026, 3, 12, 9, 30, 0), "https://example.com/market")
        };

        var result = LocalFactIngestionService.BuildMarketReports(messages, new DateTime(2026, 3, 12, 9, 35, 0));

        Assert.Single(result);
        Assert.Equal("market", result[0].Level);
    }
}