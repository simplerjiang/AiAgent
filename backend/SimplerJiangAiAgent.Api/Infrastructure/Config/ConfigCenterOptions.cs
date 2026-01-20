namespace SimplerJiangAiAgent.Api.Infrastructure.Config;

public sealed class ConfigCenterOptions
{
    public const string SectionName = "ConfigCenter";

    // 预留：配置中心地址
    public string? Endpoint { get; set; }

    // 预留：配置刷新间隔（秒）
    public int RefreshIntervalSeconds { get; set; } = 300;
}
