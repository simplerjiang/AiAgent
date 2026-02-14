using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SimplerJiangAiAgent.Api.Infrastructure.Security;

public sealed class AdminAuthFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var authService = context.HttpContext.RequestServices.GetRequiredService<IAdminAuthService>();
        var authHeader = context.HttpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Unauthorized();
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token) || !authService.IsTokenValid(token))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
