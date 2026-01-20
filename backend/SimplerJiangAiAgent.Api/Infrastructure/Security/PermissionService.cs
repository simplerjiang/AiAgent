namespace SimplerJiangAiAgent.Api.Infrastructure.Security;

public interface IPermissionService
{
    // 预留：权限校验
    bool HasAccess(string scope);
}

public sealed class PermissionService : IPermissionService
{
    public bool HasAccess(string scope)
    {
        // TODO: 接入权限中心
        return true;
    }
}
