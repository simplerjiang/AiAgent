using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimplerJiangAiAgent.Api.Modules.Stocks;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class StocksModuleHttpClientTests
{
    [Fact]
    public void Register_AppliesConfiguredTimeoutToStockCrawlerClients()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StockCrawler:HttpTimeoutSeconds"] = "9"
            })
            .Build();

        new StocksModule().Register(services, configuration);

        using var provider = services.BuildServiceProvider();
        var eastmoneyCrawler = provider.GetRequiredService<EastmoneyStockCrawler>();
        var tencentCrawler = provider.GetRequiredService<TencentStockCrawler>();

        Assert.Equal(TimeSpan.FromSeconds(9), GetHttpClientTimeout(eastmoneyCrawler));
        Assert.Equal(TimeSpan.FromSeconds(9), GetHttpClientTimeout(tencentCrawler));
    }

    [Fact]
    public void Register_ClampsStockCrawlerTimeoutToSafeLowerBound()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StockCrawler:HttpTimeoutSeconds"] = "1"
            })
            .Build();

        new StocksModule().Register(services, configuration);

        using var provider = services.BuildServiceProvider();
        var eastmoneyCrawler = provider.GetRequiredService<EastmoneyStockCrawler>();

        Assert.Equal(TimeSpan.FromSeconds(5), GetHttpClientTimeout(eastmoneyCrawler));
    }

    private static TimeSpan GetHttpClientTimeout(object instance)
    {
        var field = instance.GetType().GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var httpClient = Assert.IsType<HttpClient>(field!.GetValue(instance));
        return httpClient.Timeout;
    }
}