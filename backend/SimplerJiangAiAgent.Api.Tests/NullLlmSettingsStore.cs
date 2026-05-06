using SimplerJiangAiAgent.Api.Infrastructure.Llm;

namespace SimplerJiangAiAgent.Api.Tests;

/// <summary>
/// Minimal ILlmSettingsStore stub for tests that need a constructor parameter but don't exercise LLM settings logic.
/// Returns "ollama" as the active provider by default.
/// </summary>
internal sealed class NullLlmSettingsStore : ILlmSettingsStore
{
    public static readonly NullLlmSettingsStore Instance = new();

    public Task<IReadOnlyCollection<LlmProviderSettings>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<LlmProviderSettings>>([]);

    public Task<string> GetActiveProviderKeyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult("ollama");

    public Task<string> SetActiveProviderKeyAsync(string provider, CancellationToken cancellationToken = default)
        => Task.FromResult(provider);

    public Task<string> ResolveProviderKeyAsync(string? provider, CancellationToken cancellationToken = default)
        => Task.FromResult(string.IsNullOrWhiteSpace(provider) || string.Equals(provider, "active", StringComparison.OrdinalIgnoreCase)
            ? "ollama"
            : provider);

    public Task<LlmProviderSettings?> GetProviderAsync(string provider, CancellationToken cancellationToken = default)
        => Task.FromResult<LlmProviderSettings?>(null);

    public Task<LlmProviderSettings> UpsertAsync(LlmProviderSettings settings, CancellationToken cancellationToken = default)
        => Task.FromResult(settings);

    public Task<string> GetGlobalTavilyKeyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(string.Empty);

    public Task<(string Provider, string Model, int BatchSize)> GetNewsCleansingSettingsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<(string, string, int)>(("active", string.Empty, 12));

    public Task SetNewsCleansingSettingsAsync(string provider, string model, int batchSize, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
