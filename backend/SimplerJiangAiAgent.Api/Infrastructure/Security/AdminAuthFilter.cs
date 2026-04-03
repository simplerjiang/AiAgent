using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SimplerJiangAiAgent.Api.Infrastructure.Security;

public sealed class AdminAuthFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // 本地桌面软件无需认证，直接通过
        return await next(context);
    }
}
