namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public sealed class LlmProviderSettings
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderType { get; set; } = "openai";
    public string ApiKey { get; set; } = string.Empty;
    public string TavilyApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public bool ForceChinese { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public int? OllamaNumCtx { get; set; }
    public int? OllamaNumGpu { get; set; }
    public string OllamaKeepAlive { get; set; } = string.Empty;
    public int? OllamaNumPredict { get; set; }
    public double? OllamaTemperature { get; set; }
    public int? OllamaTopK { get; set; }
    public double? OllamaTopP { get; set; }
    public double? OllamaMinP { get; set; }
    public string[] OllamaStop { get; set; } = Array.Empty<string>();
    public bool? OllamaThink { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LlmSettingsDocument
{
    public string ActiveProviderKey { get; set; } = "default";
    public Dictionary<string, LlmProviderSettings> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public NewsCleansingDocument? NewsCleansing { get; set; }
}

public sealed class NewsCleansingDocument
{
    public string Provider { get; set; } = "active";
    public string Model { get; set; } = "";
    public int BatchSize { get; set; } = 12;
}
