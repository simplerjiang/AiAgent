using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public class EastmoneyStockParserTests
{
    [Fact]
    public void ParseQuote_ShouldScalePrice()
    {
        var json = "{\"data\":{\"f58\":\"示例\",\"f43\":1103,\"f60\":1112,\"f170\":-81}}";
        var quote = EastmoneyStockParser.ParseQuote("sh600000", json);

        Assert.Equal(11.03m, quote.Price);
        Assert.Equal(-0.81m, quote.ChangePercent);
    }

    [Fact]
    public void ParseTrends_ShouldParseLines()
    {
        var json = "{\"data\":{\"trends\":[\"2026-01-20 09:15,11.12,11.10,11.12,11.10,690,0.00,11.120\"]}}";
        var list = EastmoneyStockParser.ParseTrends("sh600000", json);

        Assert.Single(list);
        Assert.Equal(11.12m, list[0].Price);
    }
}
