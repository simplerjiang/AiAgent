using System.Text.Json;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

internal static class SinaRollParser
{
    public static IReadOnlyList<IntradayMessageDto> ParseRollMessages(string json, string code)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("result", out var resultNode))
        {
            return Array.Empty<IntradayMessageDto>();
        }

        if (!resultNode.TryGetProperty("data", out var dataNode) || dataNode.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<IntradayMessageDto>();
        }

        var list = new List<IntradayMessageDto>();
        foreach (var item in dataNode.EnumerateArray())
        {
            var title = item.GetProperty("title").GetString() ?? string.Empty;
            var url = item.TryGetProperty("url", out var urlNode) ? urlNode.GetString() : null;
            var source = item.TryGetProperty("media_name", out var mediaNode) ? mediaNode.GetString() ?? "新浪" : "新浪";
            var timeString = item.TryGetProperty("ctime", out var timeNode) ? timeNode.GetString() : null;
            var publishedAt = DateTime.TryParse(timeString, out var parsed) ? parsed : DateTime.UtcNow;

            list.Add(new IntradayMessageDto(title, source, publishedAt, url));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return list.Take(10).ToArray();
        }

        var filtered = list.Where(x => x.Title.Contains(code, StringComparison.OrdinalIgnoreCase)).ToArray();
        return filtered.Length > 0 ? filtered : list.Take(10).ToArray();
    }
}
