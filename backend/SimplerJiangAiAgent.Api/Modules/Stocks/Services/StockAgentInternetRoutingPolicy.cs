using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

internal static class StockAgentInternetRoutingPolicy
{
    public static StockAgentQueryPolicyDto Build(string symbol, bool requestedUseInternet)
    {
        if (!requestedUseInternet)
        {
            return new StockAgentQueryPolicyDto(false, "request-disabled", "local-facts-only");
        }

        var normalized = StockSymbolNormalizer.Normalize(symbol);
        var isDomesticAshare = normalized.Length == 8
            && (normalized.StartsWith("sh", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith("sz", StringComparison.OrdinalIgnoreCase));

        if (isDomesticAshare)
        {
            return new StockAgentQueryPolicyDto(false, "domestic-a-share-single-symbol", "local-facts-only");
        }

        return new StockAgentQueryPolicyDto(true, "eligible-global-query", "internet-allowed");
    }

    public static bool ResolveUseInternet(string symbol, StockAgentKind kind, bool requestedUseInternet)
    {
        if (kind == StockAgentKind.Commander || kind == StockAgentKind.TrendAnalysis)
        {
            return false;
        }

        return Build(symbol, requestedUseInternet).AllowInternet;
    }
}