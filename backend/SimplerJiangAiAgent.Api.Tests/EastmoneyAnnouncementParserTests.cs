using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class EastmoneyAnnouncementParserTests
{
    [Fact]
    public void Parse_ShouldExtractAnnouncements()
    {
        const string json = """
        {
          "data": {
            "list": [
              {
                "art_code": "AN202603120001",
                "display_time": "2026-03-12 09:30:00:000",
                "title": "浦发银行：董事会决议公告"
              }
            ]
          }
        }
        """;

        var items = EastmoneyAnnouncementParser.Parse("sh600000", "浦发银行", "银行", json, new DateTime(2026, 3, 12, 10, 0, 0, DateTimeKind.Utc));

        var item = Assert.Single(items);
        Assert.Equal("sh600000", item.Symbol);
        Assert.Equal("announcement", item.Category);
        Assert.Equal("东方财富公告", item.Source);
        Assert.Equal("AN202603120001", item.ExternalId);
        Assert.Contains("AN202603120001", item.Url);
    }
}