using System.Globalization;
using System.Text.Json;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

internal static class EastmoneyStockParser
{
    public static StockQuoteDto ParseQuote(string symbol, string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("data", out var dataNode))
        {
            return EmptyQuote(symbol);
        }

        var name = dataNode.TryGetProperty("f58", out var nameNode) ? nameNode.GetString() ?? symbol : symbol;
        var price = ParseScaledDecimal(dataNode, "f43");
        var prevClose = ParseScaledDecimal(dataNode, "f60");
        var percent = ParseScaledDecimal(dataNode, "f170");

        var change = price - prevClose;
        var changePercent = prevClose == 0 ? percent : Math.Round(change / prevClose * 100, 2);

        return new StockQuoteDto(symbol, name, price, change, changePercent, 0m, 0m, 0m, 0m, 0m, DateTime.UtcNow,
            Array.Empty<StockNewsDto>(), Array.Empty<StockIndicatorDto>());
    }

    public static IReadOnlyList<MinuteLinePointDto> ParseTrends(string symbol, string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("data", out var dataNode))
        {
            return Array.Empty<MinuteLinePointDto>();
        }

        if (!dataNode.TryGetProperty("trends", out var trendsNode) || trendsNode.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<MinuteLinePointDto>();
        }

        var points = new List<MinuteLinePointDto>();
        foreach (var item in trendsNode.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var raw = item.GetString() ?? string.Empty;
            var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 6)
            {
                continue;
            }

            if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
            {
                continue;
            }

            var price = ParseDecimal(parts[1]);
            var avg = ParseDecimal(parts[2]);
            var volume = ParseDecimal(parts[5]);
            points.Add(new MinuteLinePointDto(DateOnly.FromDateTime(dateTime), dateTime.TimeOfDay, price, avg, volume));
        }

        return points;
    }

    private static decimal ParseScaledDecimal(JsonElement dataNode, string field)
    {
        if (!dataNode.TryGetProperty(field, out var node))
        {
            return 0m;
        }

        if (node.ValueKind == JsonValueKind.Number && node.TryGetDecimal(out var value))
        {
            return value / 100m;
        }

        if (node.ValueKind == JsonValueKind.String && decimal.TryParse(node.GetString(), out var textValue))
        {
            return textValue / 100m;
        }

        return 0m;
    }

    private static decimal ParseDecimal(string? input)
    {
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }
        return 0m;
    }

    private static StockQuoteDto EmptyQuote(string symbol)
    {
        return new StockQuoteDto(symbol, symbol, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, DateTime.UtcNow,
            Array.Empty<StockNewsDto>(), Array.Empty<StockIndicatorDto>());
    }
}
