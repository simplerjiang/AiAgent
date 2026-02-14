using HtmlAgilityPack;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using System.Globalization;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

internal static class SinaCompanyNewsParser
{
    public static IReadOnlyList<IntradayMessageDto> ParseCompanyNews(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return Array.Empty<IntradayMessageDto>();
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'datelist')]//li");
        if (nodes == null || nodes.Count == 0)
        {
            return Array.Empty<IntradayMessageDto>();
        }

        var list = new List<IntradayMessageDto>();
        foreach (var node in nodes)
        {
            var link = node.SelectSingleNode(".//a");
            var timeNode = node.SelectSingleNode(".//span");
            if (link == null)
            {
                continue;
            }

            var title = link.InnerText?.Trim() ?? string.Empty;
            var url = link.GetAttributeValue("href", null);
            var timeText = timeNode?.InnerText?.Trim();

            var publishedAt = ParseTime(timeText);
            list.Add(new IntradayMessageDto(title, "新浪", publishedAt, url));
        }

        return list;
    }

    private static DateTime ParseTime(string? timeText)
    {
        if (string.IsNullOrWhiteSpace(timeText))
        {
            return DateTime.UtcNow;
        }

        if (DateTime.TryParse(timeText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed;
        }

        if (TimeSpan.TryParse(timeText, CultureInfo.InvariantCulture, out var time))
        {
            var today = DateTime.Today;
            return new DateTime(today.Year, today.Month, today.Day, time.Hours, time.Minutes, 0);
        }

        return DateTime.UtcNow;
    }
}
