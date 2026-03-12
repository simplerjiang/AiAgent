using System.Text.Json;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

internal static class EastmoneyCompanyProfileParser
{
    public static EastmoneyCompanyProfileDto Parse(string symbol, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("jbzl", out var profileNode) || profileNode.ValueKind != JsonValueKind.Object)
        {
            return new EastmoneyCompanyProfileDto(symbol, symbol, null);
        }

        var name = profileNode.TryGetProperty("agjc", out var nameNode)
            ? nameNode.GetString() ?? symbol
            : symbol;
        var sectorName = profileNode.TryGetProperty("sshy", out var sectorNode)
            ? sectorNode.GetString()
            : null;

        return new EastmoneyCompanyProfileDto(symbol, name, string.IsNullOrWhiteSpace(sectorName) ? null : sectorName.Trim());
    }
}