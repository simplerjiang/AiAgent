namespace SimplerJiangAiAgent.Api.Modules.Llm.Models;

public sealed record LlmSettingsRequest(
    string? ApiKey,
    string? TavilyApiKey,
    string? BaseUrl,
    string? Model,
    string? SystemPrompt,
    bool ForceChinese,
    string? Organization,
    string? Project,
    bool Enabled,
    string? ProviderType = null,
    int? OllamaNumCtx = null,
    int? OllamaNumGpu = null,
    string? OllamaKeepAlive = null,
    int? OllamaNumPredict = null,
    double? OllamaTemperature = null,
    int? OllamaTopK = null,
    double? OllamaTopP = null,
    double? OllamaMinP = null,
    string[]? OllamaStop = null,
    bool? OllamaThink = null);

public sealed record LlmSettingsResponse(
    string Provider,
    string ProviderType,
    string BaseUrl,
    string Model,
    string SystemPrompt,
    bool ForceChinese,
    string Organization,
    string Project,
    bool Enabled,
    bool HasApiKey,
    string ApiKeyMasked,
    bool HasTavilyApiKey,
    string TavilyApiKeyMasked,
    int? OllamaNumCtx,
    int? OllamaNumGpu,
    string OllamaKeepAlive,
    int? OllamaNumPredict,
    double? OllamaTemperature,
    int? OllamaTopK,
    double? OllamaTopP,
    double? OllamaMinP,
    string[] OllamaStop,
    bool OllamaThink,
    DateTimeOffset UpdatedAt);

public sealed record ActiveLlmProviderResponse(
    string ActiveProviderKey,
    string[] ProviderKeys);

public sealed record ActiveLlmProviderRequest(
    string ActiveProviderKey);

public sealed record LlmOnboardingStatusResponse(
    bool HasAnyApiKey,
    bool RequiresOnboarding,
    string ActiveProviderKey,
    string RecommendedTabKey);

public sealed record NewsCleansingSettingsRequest(
    string? Provider,
    string? Model,
    int? BatchSize);

public sealed record NewsCleansingSettingsResponse(
    string Provider,
    string Model,
    int BatchSize);
