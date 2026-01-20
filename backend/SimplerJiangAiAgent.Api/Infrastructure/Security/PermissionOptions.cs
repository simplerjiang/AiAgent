namespace SimplerJiangAiAgent.Api.Infrastructure.Security;

public sealed class PermissionOptions
{
    public const string SectionName = "Permission";

    // 预留：是否启用权限校验
    public bool Enabled { get; set; } = false;
}
