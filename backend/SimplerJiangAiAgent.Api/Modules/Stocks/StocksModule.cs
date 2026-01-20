using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Modules.Stocks;

public sealed class StocksModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        // 爬虫配置（预留反爬/代理池）
        services.Configure<StockCrawlerOptions>(configuration.GetSection(StockCrawlerOptions.SectionName));

        // 来源爬虫（占位实现，后续替换为真实解析逻辑）
        services.AddSingleton<IStockCrawler, TencentStockCrawler>();
        services.AddSingleton<IStockCrawler, SinaStockCrawler>();
        services.AddSingleton<IStockCrawler, BaiduStockCrawler>();
        services.AddSingleton<IStockCrawler, CompositeStockCrawler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stocks");

        // 查询单个股票信息（包含新闻与指标）
        group.MapGet("/quote", async (string symbol, IStockCrawler crawler) =>
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return Results.BadRequest(new { message = "symbol 不能为空" });
            }

            var result = await crawler.GetQuoteAsync(symbol.Trim());
            return Results.Ok(result);
        })
        .WithName("GetStockQuote")
        .WithOpenApi();

        // 查看当前启用的数据来源
        group.MapGet("/sources", (IEnumerable<IStockCrawler> crawlers) =>
        {
            var sources = crawlers
                .Where(c => c is not CompositeStockCrawler)
                .Select(c => c.SourceName)
                .Distinct()
                .ToArray();
            return Results.Ok(sources);
        })
        .WithName("GetStockSources")
        .WithOpenApi();
    }
}
