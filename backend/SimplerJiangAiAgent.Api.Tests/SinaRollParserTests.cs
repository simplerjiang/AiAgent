using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public class SinaRollParserTests
{
    [Fact]
    public void ParseRollMessages_ShouldReturnItems()
    {
        var json = "{\"result\":{\"data\":[{\"title\":\"600000 测试消息\",\"url\":\"https://example.com\",\"media_name\":\"新浪财经\",\"ctime\":\"2025-01-01 10:00:00\"}]}}";
        var list = SinaRollParser.ParseRollMessages(json, "600000");

        Assert.Single(list);
        Assert.Contains("600000", list[0].Title);
    }
}
