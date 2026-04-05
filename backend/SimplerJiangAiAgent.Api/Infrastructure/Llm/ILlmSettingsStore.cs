namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public interface ILlmSettingsStore
{
    Task<IReadOnlyCollection<LlmProviderSettings>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<string> GetActiveProviderKeyAsync(CancellationToken cancellationToken = default);
    Task<string> SetActiveProviderKeyAsync(string provider, CancellationToken cancellationToken = default);
    Task<string> ResolveProviderKeyAsync(string? provider, CancellationToken cancellationToken = default);
    Task<LlmProviderSettings?> GetProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<LlmProviderSettings> UpsertAsync(LlmProviderSettings settings, CancellationToken cancellationToken = default);
    Task<string> GetGlobalTavilyKeyAsync(CancellationToken cancellationToken = default);
    Task<(string Provider, string Model, int BatchSize)> GetNewsCleansingSettingsAsync(CancellationToken cancellationToken = default);
    Task SetNewsCleansingSettingsAsync(string provider, string model, int batchSize, CancellationToken cancellationToken = default);
}
