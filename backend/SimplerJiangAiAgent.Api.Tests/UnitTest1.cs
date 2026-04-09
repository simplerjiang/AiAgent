using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public class StockSymbolNormalizerTests
{
    [Theory]
    [InlineData("600000", "sh600000")]
    [InlineData("000001", "sz000001")]
    [InlineData("sh600000", "sh600000")]
    [InlineData("SZ000001", "sz000001")]
    public void Normalize_ShouldReturnExpected(string input, string expected)
    {
        var result = StockSymbolNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("600519", new[] { "600519", "sh600519", "SH600519" })]
    [InlineData("sh600519", new[] { "sh600519", "SH600519", "600519" })]
    [InlineData("SZ000001", new[] { "SZ000001", "sz000001", "000001" })]
    public void BuildSymbolAliases_ShouldCoverRawAndNormalizedForms(string input, string[] expectedAliases)
    {
        var aliases = FinancialDataReadService.BuildSymbolAliases(input);

        foreach (var expectedAlias in expectedAliases)
        {
            Assert.Contains(expectedAlias, aliases);
        }
    }
}
