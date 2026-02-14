using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public sealed class JsonFileLlmSettingsStore : ILlmSettingsStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public JsonFileLlmSettingsStore(IWebHostEnvironment environment)
    {
        var baseDir = Path.Combine(environment.ContentRootPath, "App_Data");
        _filePath = Path.Combine(baseDir, "llm-settings.json");
    }

    public async Task<IReadOnlyCollection<LlmProviderSettings>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadAsync(cancellationToken);
        return document.Providers.Values.ToArray();
    }

    public async Task<LlmProviderSettings?> GetProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return null;
        }

        var document = await LoadAsync(cancellationToken);
        document.Providers.TryGetValue(provider.Trim(), out var settings);
        return settings;
    }

    public async Task<LlmProviderSettings> UpsertAsync(LlmProviderSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.Provider))
        {
            throw new ArgumentException("Provider 不能为空", nameof(settings.Provider));
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var document = await LoadAsync(cancellationToken, requireLock: false);
            if (!document.Providers.TryGetValue(settings.Provider, out var existing))
            {
                existing = new LlmProviderSettings { Provider = settings.Provider };
            }

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                existing.ApiKey = settings.ApiKey.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
            {
                existing.BaseUrl = settings.BaseUrl.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.Model))
            {
                existing.Model = settings.Model.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.SystemPrompt))
            {
                existing.SystemPrompt = settings.SystemPrompt.Trim();
            }

            existing.ForceChinese = settings.ForceChinese;

            if (!string.IsNullOrWhiteSpace(settings.Organization))
            {
                existing.Organization = settings.Organization.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.Project))
            {
                existing.Project = settings.Project.Trim();
            }

            existing.Enabled = settings.Enabled;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            document.Providers[settings.Provider] = existing;

            await SaveAsync(document, cancellationToken);
            return existing;
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<LlmSettingsDocument> LoadAsync(CancellationToken cancellationToken, bool requireLock = true)
    {
        if (requireLock)
        {
            await _mutex.WaitAsync(cancellationToken);
        }

        try
        {
            if (!File.Exists(_filePath))
            {
                return new LlmSettingsDocument();
            }

            await using var stream = File.OpenRead(_filePath);
            var document = await JsonSerializer.DeserializeAsync<LlmSettingsDocument>(stream, _serializerOptions, cancellationToken);
            return document ?? new LlmSettingsDocument();
        }
        finally
        {
            if (requireLock)
            {
                _mutex.Release();
            }
        }
    }

    private async Task SaveAsync(LlmSettingsDocument document, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, document, _serializerOptions, cancellationToken);
    }
}
