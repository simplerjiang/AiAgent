namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public sealed class StockSyncOptions
{
    public const string SectionName = "StockSync";

    // 同步间隔（秒）
    public int IntervalSeconds { get; set; } = 60;

    // 默认同步的大盘指数
    public string MarketIndexSymbol { get; set; } = "sh000001";

    // 需要同步的股票列表
    public List<string> Symbols { get; set; } = new();
}
