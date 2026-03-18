using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public class TencentStockParserTests
{
    [Fact]
    public void ParseQuote_ShouldExtractFields()
    {
        var payload = "v_sh600000=\"1~浦发银行~600000~10.50~10.40~0.96~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~0~20260318155718~0.10~0.96~10.60~10.20~10.50/652219/676223804~652219~67622~0.20~6.91~~10.60~10.20~1.16~3453.82~3453.82~0.47~11.42~9.34~0.75~3274\"";
        var extracted = TencentStockParser.ExtractPayload(payload);
        var quote = TencentStockParser.ParseQuote("sh600000", extracted);

        Assert.Equal("sh600000", quote.Symbol);
        Assert.Equal("浦发银行", quote.Name);
        Assert.Equal(10.50m, quote.Price);
        Assert.Equal(0.10m, quote.Change);
        Assert.Equal(0.96m, quote.ChangePercent);
        Assert.Equal(0.20m, quote.TurnoverRate);
        Assert.Equal(6.91m, quote.PeRatio);
        Assert.Equal(1.16m, quote.VolumeRatio);
    }
}
