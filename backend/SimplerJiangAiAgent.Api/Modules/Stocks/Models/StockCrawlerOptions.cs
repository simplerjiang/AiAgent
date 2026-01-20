namespace SimplerJiangAiAgent.Api.Modules.Stocks.Models;

public sealed class StockCrawlerOptions
{
    public const string SectionName = "StockCrawler";

    // 预留：反爬策略开关（当前不实现，仅占位）
    public bool EnableAntiCrawlStub { get; set; } = false;

    // 预留：代理池开关（当前不实现，仅占位）
    public bool EnableProxyPoolStub { get; set; } = false;
}
