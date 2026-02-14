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
}
