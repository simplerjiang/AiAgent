using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class StockAgentInternetRoutingPolicyTests
{
    [Fact]
    public void Build_ShouldDisableInternet_ForDomesticAshareSymbol()
    {
        var policy = StockAgentInternetRoutingPolicy.Build("600000", true);

        Assert.False(policy.AllowInternet);
        Assert.Equal("local-facts-only", policy.Mode);
    }

    [Fact]
    public void ResolveUseInternet_ShouldAlwaysDisableCommander()
    {
        var result = StockAgentInternetRoutingPolicy.ResolveUseInternet("usAAPL", StockAgentKind.Commander, true);

        Assert.False(result);
    }
}