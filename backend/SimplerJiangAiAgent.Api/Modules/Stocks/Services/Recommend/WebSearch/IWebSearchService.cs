namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend.WebSearch;

public interface IWebSearchService
{
    Task<WebSearchResult> SearchAsync(string query, SearchType type, WebSearchOptions? options = null, CancellationToken ct = default);
    Task<WebReadResult> ReadUrlAsync(string url, int maxChars = 8000, CancellationToken ct = default);
    WebSearchHealthStatus GetHealthStatus();
}

public enum SearchType { Web, News, Finance }

public sealed record WebSearchOptions(string? TimeRange = null, int MaxResults = 10, string? Language = "zh");

public sealed record WebSearchResult(IReadOnlyList<WebSearchItem> Items, string Provider, bool IsDegraded);

public sealed record WebSearchItem(string Title, string Url, string Snippet, DateTime? PublishedAt, string? Source);

public sealed record WebReadResult(string Content, string Url, int OriginalLength, bool Truncated);

public sealed record WebSearchHealthStatus(string ActiveProvider, bool TavilyHealthy, bool SearxngHealthy, bool DuckDuckGoHealthy);
