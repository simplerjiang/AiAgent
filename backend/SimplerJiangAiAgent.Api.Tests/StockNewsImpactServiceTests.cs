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
    }
}
