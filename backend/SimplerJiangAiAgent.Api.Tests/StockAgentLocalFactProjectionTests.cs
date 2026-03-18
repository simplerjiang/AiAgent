using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class StockAgentLocalFactProjectionTests
{
    [Fact]
    public void Create_ShouldKeepAiLabelsAndFundamentalsForAgentContext()
    {
        var source = new LocalFactPackageDto(
            "sh600000",
            "浦发银行",
            "银行",
            new[]
            {
                new LocalNewsItemDto(
                    12,
                    "stock_news:12",
                    "Bank stocks rise after policy support",
                    "政策支持后银行股走强",
                    "WSJ US Business",
                    "wsj-us-business-rss",
                    "company_news",
                    "利好",
                    new DateTime(2026, 3, 13, 8, 0, 0),
                    new DateTime(2026, 3, 13, 8, 5, 0),
                    "https://example.com/a",
                    "政策支持后银行股走强，市场聚焦资金回流。",
                    "政策支持带动银行股情绪改善。",
                    "url_fetched",
                    "summary_only",
                    new DateTime(2026, 3, 13, 8, 6, 0),
                    "板块:银行",
                    new[] { "政策红利", "资金面" })
            },
            Array.Empty<LocalNewsItemDto>(),
            Array.Empty<LocalNewsItemDto>(),
            new DateTime(2026, 3, 13, 8, 10, 0),
            new[]
            {
                new LocalFundamentalFactDto("机构目标价", "13.80", "东方财富")
            });

        var projected = StockAgentLocalFactProjection.Create(source);

        Assert.Single(projected.StockNews);
        Assert.Equal("政策支持后银行股走强", projected.StockNews[0].TranslatedTitle);
        Assert.Equal("利好", projected.StockNews[0].Sentiment);
        Assert.Equal("url_fetched", projected.StockNews[0].ReadMode);
        Assert.Equal("summary_only", projected.StockNews[0].ReadStatus);
        Assert.Equal("板块:银行", projected.StockNews[0].AiTarget);
        Assert.Contains("政策红利", projected.StockNews[0].AiTags);
        Assert.Single(projected.FundamentalFacts);
        Assert.Equal("机构目标价", projected.FundamentalFacts[0].Label);
        var json = System.Text.Json.JsonSerializer.Serialize(projected);
        Assert.Contains("AiTarget", json);
        Assert.Contains("AiTags", json);
        Assert.Contains("Sentiment", json);
    }
}