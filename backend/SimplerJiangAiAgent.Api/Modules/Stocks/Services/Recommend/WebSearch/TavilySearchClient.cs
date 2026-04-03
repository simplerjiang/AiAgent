using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend.WebSearch;

public sealed class TavilySearchClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILlmSettingsStore? _llmSettingsStore;
    private readonly ILogger<TavilySearchClient> _logger;

    public TavilySearchClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TavilySearchClient> logger,
        ILlmSettingsStore? llmSettingsStore = null)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _llmSettingsStore = llmSettingsStore;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ResolveApiKeySync());

    public async Task<IReadOnlyList<WebSearchItem>> SearchAsync(string query, SearchType type, WebSearchOptions? options, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var apiKey = await ResolveApiKeyAsync(ct);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new WebSearchProviderException("tavily", "API Key 未配置");

        var baseUrl = _configuration["WebSearch:Tavily:BaseUrl"]?.TrimEnd('/') ?? "https://api.tavily.com";
        var maxResults = options?.MaxResults ?? 10;

        var client = _httpClientFactory.CreateClient("WebSearch");
        using var response = await client.PostAsJsonAsync(
            baseUrl + "/search",
            new
            {
                api_key = apiKey,
                query,
                max_results = maxResults,
                search_depth = "advanced",
                include_answer = false,
                include_raw_content = false,
                topic = type == SearchType.News ? "news" : "general"
            },
            ct);

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Tavily 搜索失败: HTTP {StatusCode}, body={Body}", (int)response.StatusCode, responseBody);
            throw new WebSearchProviderException("tavily", $"HTTP {(int)response.StatusCode}");
        }

        var items = new List<WebSearchItem>();
        using var doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty("results", out var node) && node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var url = item.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
                var content = item.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                var source = item.TryGetProperty("source", out var s) ? s.GetString() : ExtractHost(url);
                var publishedAt = item.TryGetProperty("published_date", out var pd) && pd.ValueKind == JsonValueKind.String
                    ? ParseDate(pd.GetString()) : null;
                items.Add(new WebSearchItem(title, url, content, publishedAt, source));
            }
        }

        return items;
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct)
    {
        try
        {
            var apiKey = await ResolveApiKeyAsync(ct);
            return !string.IsNullOrWhiteSpace(apiKey);
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> ResolveApiKeyAsync(CancellationToken ct)
    {
        var configKey = _configuration["WebSearch:Tavily:ApiKey"];
        if (!string.IsNullOrWhiteSpace(configKey))
            return configKey.Trim();

        if (_llmSettingsStore is not null)
        {
            var activeProvider = await _llmSettingsStore.GetActiveProviderKeyAsync(ct);
            var settings = await _llmSettingsStore.GetProviderAsync(activeProvider, ct);
            if (!string.IsNullOrWhiteSpace(settings?.TavilyApiKey))
                return settings.TavilyApiKey.Trim();

            // Tavily API Key is global: fall back to any provider
            var globalKey = await _llmSettingsStore.GetGlobalTavilyKeyAsync(ct);
            if (!string.IsNullOrWhiteSpace(globalKey))
                return globalKey;
        }

        var envKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
            return envKey.Trim();

        return null;
    }

    private string? ResolveApiKeySync()
    {
        var configKey = _configuration["WebSearch:Tavily:ApiKey"];
        if (!string.IsNullOrWhiteSpace(configKey)) return configKey.Trim();
        var envKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey)) return envKey.Trim();
        return null;
    }

    private static string? ExtractHost(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.Host;
        return null;
    }

    private static DateTime? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, out var dt) ? dt : null;
    }
}
