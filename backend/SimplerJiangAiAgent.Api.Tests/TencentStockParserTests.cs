using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public class TencentStockParserTests
{
    [Fact]
    public void ParseQuote_ShouldExtractFields()
    {
        var payload = "v_sh600000=\"1~浦发银行~600000~10.50~10.40~0.96~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0\"";
        var extracted = TencentStockParser.ExtractPayload(payload);
        var quote = TencentStockParser.ParseQuote("sh600000", extracted);

        Assert.Equal("sh600000", quote.Symbol);
        Assert.Equal("浦发银行", quote.Name);
        Assert.Equal(10.50m, quote.Price);
        Assert.Equal(0.10m, quote.Change);
        Assert.Equal(0.96m, quote.ChangePercent);
    }
}
