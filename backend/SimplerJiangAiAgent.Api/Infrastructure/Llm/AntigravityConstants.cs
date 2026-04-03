namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public static class AntigravityConstants
{
    // OAuth 公共凭证（平台级，所有用户共用，来自官方开源插件 opencode-antigravity-auth-updated，MIT License）
    /// <summary>Default Antigravity platform OAuth Client ID (public credential, shared by all users).</summary>
    public const string DefaultClientId = "1071006060591-tmhssin2h21lcre235vtolojh4g403ep.apps.googleusercontent.com";

    /// <summary>Default Antigravity platform OAuth Client Secret (public credential, shared by all users).</summary>
    public const string DefaultClientSecret = "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";

    // OAuth URLs
    public const string AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    public const string TokenUrl = "https://oauth2.googleapis.com/token";
    public const string UserInfoUrl = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json";

    public static readonly string[] Scopes = new[]
    {
        "https://www.googleapis.com/auth/cloud-platform",
        "https://www.googleapis.com/auth/userinfo.email",
        "https://www.googleapis.com/auth/userinfo.profile",
        "https://www.googleapis.com/auth/cclog",
        "https://www.googleapis.com/auth/experimentsandconfigs"
    };

    // API 端点（generateContent 降级顺序）
    public const string EndpointDaily = "https://daily-cloudcode-pa.sandbox.googleapis.com";
    public const string EndpointAutopush = "https://autopush-cloudcode-pa.sandbox.googleapis.com";
    public const string EndpointProd = "https://cloudcode-pa.googleapis.com";

    public static readonly string[] GenerateEndpoints = new[] { EndpointDaily, EndpointAutopush, EndpointProd };
    // loadCodeAssist 端点顺序（prod 优先）
    public static readonly string[] LoadEndpoints = new[] { EndpointProd, EndpointDaily, EndpointAutopush };

    // 单端点超时（秒）
    public const int PerEndpointTimeoutSeconds = 30;

    // 默认值
    public const string DefaultProjectId = "rising-fact-p41fc";
    public const string FallbackVersion = "1.19.4";
    public const string VersionUrl = "https://antigravity-auto-updater-974169037036.us-central1.run.app";

    // User-Agent 模板（用于 generateContent）
    public const string UserAgentTemplate = "antigravity/{0} windows/amd64";

    // User-Agent（用于 loadCodeAssist，完整 Electron/Chrome UA）
    public const string FullUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Antigravity/{0} Chrome/138.0.7204.235 Electron/37.3.1 Safari/537.36";

    // 可用模型列表
    public static readonly string[] AvailableModels = new[]
    {
        "gemini-3-flash",
        "gemini-3-pro-high",
        "gemini-3-pro-low",
        "gemini-3.1-pro",
        "claude-sonnet-4-6",
        "claude-opus-4-6-thinking",
        "gpt-oss-120b-medium"
    };

    // 外部模型 → Antigravity 模型映射
    public static readonly Dictionary<string, string> ModelMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        // Gemini flash 系列 → gemini-3-flash
        { "gemini-2.0-flash", "gemini-3-flash" },
        { "gemini-2.0-flash-lite", "gemini-3-flash" },
        { "gemini-3.1-flash-lite-preview-thinking-high", "gemini-3-flash" },
        // Gemini pro 系列
        { "gemini-2.0-pro", "gemini-3-pro-high" },
        { "gemini-3.1-pro-preview-thinking-medium", "gemini-3.1-pro" },
        // GPT 系列 → gemini-3-flash (最便宜的替代)
        { "gpt-4.1-nano", "gemini-3-flash" },
        { "gpt-4.1-mini", "gemini-3-flash" },
        { "gpt-4o-mini", "gemini-3-flash" },
        { "gpt-4o", "gemini-3-pro-high" },
        { "gpt-4.1", "gemini-3-pro-high" },
        // Claude 系列
        { "claude-3.5-sonnet", "claude-sonnet-4-6" },
        { "claude-3-sonnet", "claude-sonnet-4-6" },
        { "claude-3-opus", "claude-opus-4-6-thinking" },
    };

    // 默认降级模型（最便宜）
    public const string DefaultFallbackModel = "gemini-3-flash";
}
