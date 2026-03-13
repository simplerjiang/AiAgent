using System.Text.Json;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

internal static class EastmoneyCompanyProfileParser
{
    public static EastmoneyCompanyProfileDto Parse(string symbol, string json, string? shareholderJson = null)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("jbzl", out var profileNode) || profileNode.ValueKind != JsonValueKind.Object)
        {
            return new EastmoneyCompanyProfileDto(symbol, symbol, null, ParseShareholderCount(shareholderJson));
        }

        var name = profileNode.TryGetProperty("agjc", out var nameNode)
            ? nameNode.GetString() ?? symbol
            : symbol;
        var sectorName = profileNode.TryGetProperty("sshy", out var sectorNode)
            ? sectorNode.GetString()
            : null;

        var shareholderCount = ParseShareholderCount(shareholderJson);

        return new EastmoneyCompanyProfileDto(
            symbol,
            name,
            string.IsNullOrWhiteSpace(sectorName) ? null : sectorName.Trim(),
            shareholderCount);
    }

    private static int? ParseShareholderCount(string? shareholderJson)
    {
        if (string.IsNullOrWhiteSpace(shareholderJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(shareholderJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("gdrs", out var gdrsNode) || gdrsNode.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in gdrsNode.EnumerateArray())
        {
            if (!item.TryGetProperty("HOLDER_TOTAL_NUM", out var countNode))
            {
                continue;
            }

            if (countNode.ValueKind == JsonValueKind.Number && countNode.TryGetInt32(out var count))
            {
                return count;
            }

            if (countNode.ValueKind == JsonValueKind.String && int.TryParse(countNode.GetString(), out count))
            {
                return count;
            }
        }

        return null;
    }
}