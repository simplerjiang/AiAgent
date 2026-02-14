using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public class StockNewsImpactServiceTests
{
    [Fact]
    public void Evaluate_UsesKeywordScoringToSummarizeImpact()
    {
        var service = new StockNewsImpactService();
        var messages = new List<IntradayMessageDto>
        {
            new("公司宣布回购计划", "新浪", DateTime.Today, null),
            new("公司被罚并遭遇诉讼", "新浪", DateTime.Today, null),
            new("公司召开业绩说明会", "新浪", DateTime.Today, null)
        };

        var result = service.Evaluate("sh000001", "测试公司", messages);

        Assert.Equal(1, result.Summary.Positive);
        Assert.Equal(1, result.Summary.Negative);
        Assert.Equal(1, result.Summary.Neutral);
        Assert.Equal(3, result.Events.Count);

        Assert.All(result.Events, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.EventType));
            Assert.True(item.TypeWeight > 0);
            Assert.True(item.SourceCredibility > 0);
            Assert.False(string.IsNullOrWhiteSpace(item.Theme));
            Assert.True(item.MergedCount >= 1);
        });
    }

    [Fact]
    public void Evaluate_MergesSameThemeEvents()
    {
        var service = new StockNewsImpactService();
        var now = DateTime.Now;
        var messages = new List<IntradayMessageDto>
        {
            new("公司公告：回购计划获批", "上交所公告", now.AddHours(-1), "https://a"),
            new("公司回购计划正式实施", "新浪财经", now.AddHours(-2), "https://b"),
            new("公司被罚并遭遇诉讼", "新浪财经", now.AddHours(-1), "https://c")
        };

        var result = service.Evaluate("sh000001", "测试公司", messages);

        Assert.True(result.Events.Count <= 3);
        Assert.Contains(result.Events, item => item.MergedCount >= 2 && item.Theme == "股份回购");
    }
}
