using System.Text.Json;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class StockAgentJsonParserTests
{
    [Fact]
    public void TryParse_ExtractsJsonFromFence()
    {
        var content = "```json\n{\"agent\":\"stock_news\",\"summary\":\"ok\"}\n```";

        var ok = StockAgentJsonParser.TryParse(content, out var data, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.True(data.HasValue);
        Assert.Equal("stock_news", data!.Value.GetProperty("agent").GetString());
    }

    [Fact]
    public void TryParse_ReturnsErrorForInvalidJson()
    {
        var content = "not json";

        var ok = StockAgentJsonParser.TryParse(content, out var data, out var error);

        Assert.False(ok);
        Assert.False(data.HasValue);
        Assert.NotNull(error);
    }
}
