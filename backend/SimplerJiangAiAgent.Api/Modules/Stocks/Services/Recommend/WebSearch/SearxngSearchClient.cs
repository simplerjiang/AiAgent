using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend.WebSearch;

public sealed class SearxngSearchClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SearxngSearchClient> _logger;

    public SearxngSearchClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SearxngSearchClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private string BaseUrl => _configuration["WebSearch:Searxng:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:8080";
    private int TimeoutSeconds => int.TryParse(_configuration["WebSearch:Searxng:TimeoutSeconds"], out var t) ? t : 3;

    public async Task<IReadOnlyList<WebSearchItem>> SearchAsync(string query, SearchType type, WebSearchOptions? options, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var client = _httpClientFactory.CreateClient("WebSearch");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        var language = options?.Language ?? "zh";
        var timeRange = options?.TimeRange;
        var maxResults = options?.MaxResults ?? 10;

        var categories = type == SearchType.News ? "news" : "general";
        var url = $"{BaseUrl}/search?q={Uri.EscapeDataString(query)}&format=json&language={Uri.EscapeDataString(language)}&categories={categories}";
        if (!string.IsNullOrWhiteSpace(timeRange))
            url += $"&time_range={Uri.EscapeDataString(timeRange)}";

        using var response = await client.GetAsync(url, cts.Token);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("SearXNG 搜索失败: HTTP {StatusCode}", (int)response.StatusCode);
            throw new WebSearchProviderException("searxng", $"HTTP {(int)response.StatusCode}");
        }

        var items = new List<WebSearchItem>();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cts.Token));
        if (doc.RootElement.TryGetProperty("results", out var node) && node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var itemUrl = item.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
                var content = item.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                var source = item.TryGetProperty("engine", out var e) ? e.GetString() : null;
                var publishedAt = item.TryGetProperty("publishedDate", out var pd) && pd.ValueKind == JsonValueKind.String
                    ? ParseDate(pd.GetString()) : null;
                items.Add(new WebSearchItem(title, itemUrl, content, publishedAt, source));
                if (items.Count >= maxResults) break;
            }
        }

        if (items.Count == 0)
            throw new WebSearchProviderException("searxng", "返回 0 结果");

        return items;
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("WebSearch");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));
            using var response = await client.GetAsync($"{BaseUrl}/search?q=test&format=json", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static DateTime? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, out var dt) ? dt : null;
    }
}
