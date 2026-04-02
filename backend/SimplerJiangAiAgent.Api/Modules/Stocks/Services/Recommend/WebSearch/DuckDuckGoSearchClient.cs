using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend.WebSearch;

public sealed partial class DuckDuckGoSearchClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DuckDuckGoSearchClient> _logger;

    public DuckDuckGoSearchClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DuckDuckGoSearchClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private int TimeoutSeconds => int.TryParse(_configuration["WebSearch:DuckDuckGo:TimeoutSeconds"], out var t) ? t : 5;

    public async Task<IReadOnlyList<WebSearchItem>> SearchAsync(string query, SearchType type, WebSearchOptions? options, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var client = _httpClientFactory.CreateClient("WebSearch");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        var maxResults = options?.MaxResults ?? 10;
        var url = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}";
        if (type == SearchType.News)
            url += "&iar=news";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (compatible; SimplerJiangBot/1.0)");

        using var response = await client.SendAsync(request, cts.Token);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("DuckDuckGo 搜索失败: HTTP {StatusCode}", (int)response.StatusCode);
            throw new WebSearchProviderException("duckduckgo", $"HTTP {(int)response.StatusCode}");
        }

        var html = await response.Content.ReadAsStringAsync(cts.Token);
        return ParseHtmlResults(html, maxResults);
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("WebSearch");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://html.duckduckgo.com/html/?q=test");
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (compatible; SimplerJiangBot/1.0)");
            using var response = await client.SendAsync(request, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static IReadOnlyList<WebSearchItem> ParseHtmlResults(string html, int maxResults)
    {
        var items = new List<WebSearchItem>();

        // Parse DuckDuckGo HTML Lite result blocks: <a class="result__a" href="...">title</a> and <a class="result__snippet">snippet</a>
        var linkMatches = ResultLinkRegex().Matches(html);
        var snippetMatches = ResultSnippetRegex().Matches(html);

        for (var i = 0; i < linkMatches.Count && items.Count < maxResults; i++)
        {
            var rawUrl = WebUtility.HtmlDecode(linkMatches[i].Groups[1].Value);
            var title = StripHtmlTags(WebUtility.HtmlDecode(linkMatches[i].Groups[2].Value));

            // DuckDuckGo lite wraps URLs in a redirect; extract the real URL
            var actualUrl = ExtractActualUrl(rawUrl);
            var snippet = i < snippetMatches.Count
                ? StripHtmlTags(WebUtility.HtmlDecode(snippetMatches[i].Groups[1].Value))
                : "";

            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(actualUrl))
                items.Add(new WebSearchItem(title.Trim(), actualUrl, snippet.Trim(), null, ExtractHost(actualUrl)));
        }

        return items;
    }

    private static string ExtractActualUrl(string url)
    {
        // DuckDuckGo wraps: //duckduckgo.com/l/?uddg=https%3A%2F%2F...&rut=...
        if (url.Contains("uddg="))
        {
            var idx = url.IndexOf("uddg=", StringComparison.Ordinal) + 5;
            var end = url.IndexOf('&', idx);
            var encoded = end > idx ? url[idx..end] : url[idx..];
            return Uri.UnescapeDataString(encoded);
        }
        return url;
    }

    private static string StripHtmlTags(string s) => HtmlTagRegex().Replace(s, "");

    private static string? ExtractHost(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.Host;
        return null;
    }

    [GeneratedRegex(@"<a[^>]+class=""result__a""[^>]+href=""([^""]+)""[^>]*>(.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ResultLinkRegex();

    [GeneratedRegex(@"<a[^>]+class=""result__snippet""[^>]*>(.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ResultSnippetRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
