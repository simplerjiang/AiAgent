using Microsoft.Extensions.Logging;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend.WebSearch;

public sealed class WebSearchService : IWebSearchService
{
    private readonly TavilySearchClient _tavily;
    private readonly SearxngSearchClient _searxng;
    private readonly DuckDuckGoSearchClient _duckDuckGo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebSearchService> _logger;

    private volatile bool _tavilyHealthy = true;
    private volatile bool _searxngHealthy = true;
    private volatile bool _duckDuckGoHealthy = true;
    private DateTime _lastHealthCheck = DateTime.MinValue;
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromMinutes(5);
    private readonly object _healthLock = new();

    public WebSearchService(
        TavilySearchClient tavily,
        SearxngSearchClient searxng,
        DuckDuckGoSearchClient duckDuckGo,
        IHttpClientFactory httpClientFactory,
        ILogger<WebSearchService> logger)
    {
        _tavily = tavily;
        _searxng = searxng;
        _duckDuckGo = duckDuckGo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<WebSearchResult> SearchAsync(string query, SearchType type, WebSearchOptions? options = null, CancellationToken ct = default)
    {
        await LazyHealthCheckAsync(ct);

        // Tavily (primary)
        if (_tavilyHealthy)
        {
            try
            {
                var items = await _tavily.SearchAsync(query, type, options, ct);
                return new WebSearchResult(items, "tavily", false);
            }
            catch (WebSearchProviderException ex)
            {
                _logger.LogWarning(ex, "Tavily 搜索失败，降级到 SearXNG");
                _tavilyHealthy = false;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Tavily 网络异常，降级到 SearXNG");
                _tavilyHealthy = false;
            }
        }

        // SearXNG (fallback 1)
        if (_searxngHealthy)
        {
            try
            {
                var items = await _searxng.SearchAsync(query, type, options, ct);
                return new WebSearchResult(items, "searxng", true);
            }
            catch (WebSearchProviderException ex)
            {
                _logger.LogWarning(ex, "SearXNG 搜索失败，降级到 DuckDuckGo");
                _searxngHealthy = false;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "SearXNG 网络异常，降级到 DuckDuckGo");
                _searxngHealthy = false;
            }
        }

        // DuckDuckGo (fallback 2 - last resort, returns empty on failure)
        try
        {
            var items = await _duckDuckGo.SearchAsync(query, type, options, ct);
            return new WebSearchResult(items, "duckduckgo", true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DuckDuckGo 搜索也失败，返回空结果");
            _duckDuckGoHealthy = false;
            return new WebSearchResult(Array.Empty<WebSearchItem>(), "none", true);
        }
    }

    public async Task<WebReadResult> ReadUrlAsync(string url, int maxChars = 8000, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("WebSearch");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (compatible; SimplerJiangBot/1.0)");

            using var response = await client.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            var raw = await response.Content.ReadAsStringAsync(cts.Token);
            var truncated = raw.Length > maxChars;
            var content = truncated ? raw[..maxChars] : raw;
            return new WebReadResult(content, url, raw.Length, truncated);
        }
        catch (HttpRequestException ex)
        {
            throw new WebSearchProviderException("url-reader", $"Failed to read {url}: {ex.Message}", ex);
        }
    }

    public WebSearchHealthStatus GetHealthStatus()
    {
        var activeProvider = _tavilyHealthy ? "tavily"
            : _searxngHealthy ? "searxng"
            : _duckDuckGoHealthy ? "duckduckgo"
            : "none";

        return new WebSearchHealthStatus(activeProvider, _tavilyHealthy, _searxngHealthy, _duckDuckGoHealthy);
    }

    private async Task LazyHealthCheckAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        bool needsCheck;
        lock (_healthLock)
        {
            needsCheck = now - _lastHealthCheck > HealthCheckInterval;
            if (needsCheck) _lastHealthCheck = now;
        }

        if (!needsCheck) return;

        try
        {
            _tavilyHealthy = await _tavily.HealthCheckAsync(ct);
        }
        catch { _tavilyHealthy = false; }

        try
        {
            _searxngHealthy = await _searxng.HealthCheckAsync(ct);
        }
        catch { _searxngHealthy = false; }

        try
        {
            _duckDuckGoHealthy = await _duckDuckGo.HealthCheckAsync(ct);
        }
        catch { _duckDuckGoHealthy = false; }

        _logger.LogInformation("WebSearch 健康检查完成: Tavily={Tavily}, SearXNG={Searxng}, DuckDuckGo={DuckDuckGo}",
            _tavilyHealthy, _searxngHealthy, _duckDuckGoHealthy);
    }
}
