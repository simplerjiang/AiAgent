using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class EastmoneyCompanyProfileParserTests
{
    [Fact]
    public void Parse_ShouldExtractNameAndSector()
    {
        const string json = """
        {
          "jbzl": {
            "agjc": "浦发银行",
            "sshy": "银行"
          }
        }
        """;

        var profile = EastmoneyCompanyProfileParser.Parse("sh600000", json);

        Assert.Equal("sh600000", profile.Symbol);
        Assert.Equal("浦发银行", profile.Name);
        Assert.Equal("银行", profile.SectorName);
    }
}