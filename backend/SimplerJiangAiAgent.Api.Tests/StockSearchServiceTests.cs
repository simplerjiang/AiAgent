using System.Net;
using System.Text;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public class StockSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ShouldParseTencentSmartboxPayload()
    {
        var raw = "v_hint=\"sh~000680~\\u79d1\\u521b\\u7efc\\u6307~kczz~ZS^sh~000688~\\u79d1\\u521b50~kc50~ZS\"";
        var handler = new FakeHttpMessageHandler(raw);
        var httpClient = new HttpClient(handler);
        var service = new StockSearchService(httpClient);

        var results = await service.SearchAsync("科创", 10);

        Assert.Equal(2, results.Count);
        var first = results[0];
        Assert.Equal("000680", first.Code);
        Assert.Equal("sh000680", first.Symbol);
        Assert.Equal("科创综指", first.Name);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;

        public FakeHttpMessageHandler(string response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new StringContent(_response, Encoding.UTF8, "text/plain");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}
