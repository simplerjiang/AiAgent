using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public class SinaCompanyNewsParserTests
{
    [Fact]
    public void ParseCompanyNews_ShouldExtractItems()
    {
        var html = "<div class='datelist'><ul>" +
                   "<li><span>10:00</span><a href='https://example.com/a'>测试新闻A</a></li>" +
                   "<li><span>11:30</span><a href='https://example.com/b'>测试新闻B</a></li>" +
                   "</ul></div>";

        var list = SinaCompanyNewsParser.ParseCompanyNews(html);

        Assert.Equal(2, list.Count);
        Assert.Equal("测试新闻A", list[0].Title);
        Assert.Equal("新浪", list[0].Source);
    }
}
