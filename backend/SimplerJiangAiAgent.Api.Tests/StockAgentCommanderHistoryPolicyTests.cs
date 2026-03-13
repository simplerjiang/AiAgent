using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class StockAgentCommanderHistoryPolicyTests
{
    [Fact]
    public void Build_UsesFiveDayWindowAndMaxItemsByDefault()
    {
        var now = new DateTime(2026, 3, 3, 12, 0, 0);
        var list = new List<StockAgentAnalysisHistory>();

        for (var i = 0; i < 12; i++)
        {
            list.Add(CreateEntry(now.AddHours(-i * 4), "加仓", 70 + i));
        }

        var result = StockAgentCommanderHistoryPolicy.Build(list, now);

        Assert.Equal(5, result.LookbackDays);
        Assert.Equal(8, result.MaxItems);
        Assert.Equal(8, result.IncludedCount);
        Assert.All(result.Items, item => Assert.Equal("加仓", item.Direction));
    }

    [Fact]
    public void Build_ClampsLookbackDaysToRangeThreeToSeven()
    {
        var now = new DateTime(2026, 3, 3, 12, 0, 0);
        var list = new List<StockAgentAnalysisHistory>
        {
            CreateEntry(now.AddDays(-1), "观察", 60),
            CreateEntry(now.AddDays(-6), "减仓", 55),
            CreateEntry(now.AddDays(-8), "清仓", 40)
        };

        var result = StockAgentCommanderHistoryPolicy.Build(list, now, lookbackDays: 10, maxItems: 10);

        Assert.Equal(7, result.LookbackDays);
        Assert.Equal(2, result.IncludedCount);
        Assert.DoesNotContain(result.Items, item => item.Direction == "清仓");
    }

    [Fact]
    public void Build_ExtractsCommanderFieldsFromResultJson()
    {
        var now = new DateTime(2026, 3, 3, 12, 0, 0);
        var list = new List<StockAgentAnalysisHistory>
        {
            new()
            {
                Symbol = "sz000001",
                Name = "平安银行",
                CreatedAt = now.AddHours(-2),
                ResultJson = """
                {
                  "symbol": "sz000001",
                  "agents": [
                    {
                      "agentId": "commander",
                      "data": {
                        "summary": "结论摘要",
                        "analysis_opinion": "减仓为主，等待下一个确认信号。",
                        "confidence_score": 66.5,
                        "trigger_conditions": "跌破20日均线",
                        "invalid_conditions": "放量站回20日均线",
                        "risk_warning": "单日回撤不超过2%",
                        "evidence": [
                          { "point": "北向资金净流出", "source": "交易所" },
                          { "point": "板块强度回落", "source": "资讯" }
                        ]
                      }
                    }
                  ]
                }
                """
            }
        };

        var result = StockAgentCommanderHistoryPolicy.Build(list, now);

        Assert.Single(result.Items);
        var item = result.Items[0];
        Assert.Equal("减仓", item.Direction);
        Assert.Equal(66.5m, item.Confidence);
        Assert.Equal("减仓为主，等待下一个确认信号。", item.Summary);
        Assert.Contains("跌破20日均线", item.Triggers);
        Assert.Contains("放量站回20日均线", item.Invalidations);
        Assert.Contains("单日回撤不超过2%", item.RiskLimits);
        Assert.Contains("北向资金净流出", item.EvidenceSummary);
    }

    private static StockAgentAnalysisHistory CreateEntry(DateTime createdAt, string action, decimal confidence)
    {
        return new StockAgentAnalysisHistory
        {
            Symbol = "sz000001",
            Name = "平安银行",
            CreatedAt = createdAt,
            ResultJson = $$"""
            {
              "agents": [
                {
                  "agentId": "commander",
                  "data": {
                    "summary": "历史{{action}}",
                    "analysis_opinion": "{{action}}为主，等待下一步确认。",
                    "confidence_score": {{confidence}},
                    "trigger_conditions": "触发{{action}}",
                    "invalid_conditions": "失效{{action}}",
                    "risk_warning": "风控{{action}}",
                    "evidence": [
                      { "point": "证据{{action}}", "source": "source" }
                    ]
                  }
                }
              ]
            }
            """
        };
    }
}
