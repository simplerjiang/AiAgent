using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public sealed class JsonFileLlmSettingsStore : ILlmSettingsStore
{
    private readonly string _defaultsFilePath;
    private readonly string _localSecretsFilePath;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public JsonFileLlmSettingsStore(IWebHostEnvironment environment)
    {
        var baseDir = Path.Combine(environment.ContentRootPath, "App_Data");
        _defaultsFilePath = Path.Combine(baseDir, "llm-settings.json");
        _localSecretsFilePath = Path.Combine(baseDir, "llm-settings.local.json");
    }

    public async Task<IReadOnlyCollection<LlmProviderSettings>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadMergedAsync(cancellationToken);
        return document.Providers.Values.ToArray();
    }

    public async Task<LlmProviderSettings?> GetProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return null;
        }

        var document = await LoadMergedAsync(cancellationToken);
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
            var defaultsDocument = await LoadDocumentAsync(_defaultsFilePath, cancellationToken, requireLock: false);
            var localSecretsDocument = await LoadDocumentAsync(_localSecretsFilePath, cancellationToken, requireLock: false);

            if (!defaultsDocument.Providers.TryGetValue(settings.Provider, out var existingDefaults))
            {
                existingDefaults = new LlmProviderSettings { Provider = settings.Provider };
            }

            if (!localSecretsDocument.Providers.TryGetValue(settings.Provider, out var existingSecrets))
            {
                existingSecrets = new LlmProviderSettings { Provider = settings.Provider };
            }

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                existingSecrets.ApiKey = settings.ApiKey.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
            {
                existingDefaults.BaseUrl = settings.BaseUrl.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.Model))
            {
                existingDefaults.Model = settings.Model.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.SystemPrompt))
            {
                existingDefaults.SystemPrompt = settings.SystemPrompt.Trim();
            }

            existingDefaults.ForceChinese = settings.ForceChinese;

            if (!string.IsNullOrWhiteSpace(settings.Organization))
            {
                existingDefaults.Organization = settings.Organization.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.Project))
            {
                existingDefaults.Project = settings.Project.Trim();
            }

            existingDefaults.Enabled = settings.Enabled;
            existingDefaults.UpdatedAt = DateTimeOffset.UtcNow;
            existingDefaults.ApiKey = string.Empty;
            existingSecrets.Provider = settings.Provider;

            defaultsDocument.Providers[settings.Provider] = existingDefaults;
            if (!string.IsNullOrWhiteSpace(existingSecrets.ApiKey))
            {
                localSecretsDocument.Providers[settings.Provider] = new LlmProviderSettings
                {
                    Provider = settings.Provider,
                    ApiKey = existingSecrets.ApiKey,
                    UpdatedAt = existingDefaults.UpdatedAt
                };
            }
            else
            {
                localSecretsDocument.Providers.Remove(settings.Provider);
            }

            await SaveDocumentAsync(_defaultsFilePath, defaultsDocument, cancellationToken);
            await SaveDocumentAsync(_localSecretsFilePath, localSecretsDocument, cancellationToken, deleteWhenEmpty: true);

            return MergeSettings(existingDefaults, existingSecrets.ApiKey);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<LlmSettingsDocument> LoadMergedAsync(CancellationToken cancellationToken, bool requireLock = true)
    {
        if (requireLock)
        {
            await _mutex.WaitAsync(cancellationToken);
        }

        try
        {
            var defaultsDocument = await LoadDocumentAsync(_defaultsFilePath, cancellationToken, requireLock: false);
            var localSecretsDocument = await LoadDocumentAsync(_localSecretsFilePath, cancellationToken, requireLock: false);
            return MergeDocuments(defaultsDocument, localSecretsDocument);
        }
        finally
        {
            if (requireLock)
            {
                _mutex.Release();
            }
        }
    }

    private async Task<LlmSettingsDocument> LoadDocumentAsync(string filePath, CancellationToken cancellationToken, bool requireLock = true)
    {
        if (requireLock)
        {
            await _mutex.WaitAsync(cancellationToken);
        }

        try
        {
            if (!File.Exists(filePath))
            {
                return new LlmSettingsDocument();
            }

            await using var stream = File.OpenRead(filePath);
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

    private async Task SaveDocumentAsync(string filePath, LlmSettingsDocument document, CancellationToken cancellationToken, bool deleteWhenEmpty = false)
    {
        if (deleteWhenEmpty && document.Providers.Count == 0)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return;
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, _serializerOptions, cancellationToken);
    }

    private static LlmSettingsDocument MergeDocuments(LlmSettingsDocument defaultsDocument, LlmSettingsDocument localSecretsDocument)
    {
        var merged = new LlmSettingsDocument();

        foreach (var (provider, settings) in defaultsDocument.Providers)
        {
            merged.Providers[provider] = CloneWithoutSecret(settings);
        }

        foreach (var (provider, settings) in localSecretsDocument.Providers)
        {
            if (!merged.Providers.TryGetValue(provider, out var mergedSettings))
            {
                mergedSettings = new LlmProviderSettings { Provider = provider };
                merged.Providers[provider] = mergedSettings;
            }

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                mergedSettings.ApiKey = settings.ApiKey.Trim();
            }
        }

        foreach (var (provider, settings) in merged.Providers)
        {
            var envApiKey = ResolveApiKeyFromEnvironment(provider);
            if (!string.IsNullOrWhiteSpace(envApiKey))
            {
                settings.ApiKey = envApiKey;
            }
        }

        return merged;
    }

    private static LlmProviderSettings MergeSettings(LlmProviderSettings defaultsSettings, string apiKey)
    {
        var merged = CloneWithoutSecret(defaultsSettings);
        merged.ApiKey = string.IsNullOrWhiteSpace(apiKey)
            ? ResolveApiKeyFromEnvironment(defaultsSettings.Provider)
            : apiKey;
        return merged;
    }

    private static LlmProviderSettings CloneWithoutSecret(LlmProviderSettings source)
    {
        return new LlmProviderSettings
        {
            Provider = source.Provider,
            ApiKey = string.Empty,
            BaseUrl = source.BaseUrl,
            Model = source.Model,
            SystemPrompt = source.SystemPrompt,
            ForceChinese = source.ForceChinese,
            Organization = source.Organization,
            Project = source.Project,
            Enabled = source.Enabled,
            UpdatedAt = source.UpdatedAt
        };
    }

    private static string ResolveApiKeyFromEnvironment(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return string.Empty;
        }

        var normalizedProvider = provider.Trim().ToUpperInvariant();
        var providerScopedName = $"LLM__{normalizedProvider}__APIKEY";
        var providerScopedValue = Environment.GetEnvironmentVariable(providerScopedName);
        if (!string.IsNullOrWhiteSpace(providerScopedValue))
        {
            return providerScopedValue.Trim();
        }

        if (string.Equals(provider, "openai", StringComparison.OrdinalIgnoreCase))
        {
            var openAiValue = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(openAiValue))
            {
                return openAiValue.Trim();
            }
        }

        return string.Empty;
    }
}
