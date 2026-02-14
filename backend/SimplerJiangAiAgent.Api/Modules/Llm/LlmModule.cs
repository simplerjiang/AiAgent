using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;
using SimplerJiangAiAgent.Api.Infrastructure.Security;
using SimplerJiangAiAgent.Api.Modules.Llm.Models;

namespace SimplerJiangAiAgent.Api.Modules.Llm;

public sealed class LlmModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        var timeoutSeconds = configuration.GetValue<int?>("Llm:HttpClientTimeoutSeconds") ?? 180;
        if (timeoutSeconds < 30)
        {
            timeoutSeconds = 30;
        }

        services.AddSingleton<ILlmSettingsStore, JsonFileLlmSettingsStore>();
        services.AddSingleton<ILlmService, LlmService>();
        services.AddSingleton<IAdminAuthService, AdminAuthService>();
        services.AddHttpClient<OpenAiProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });
        services.AddSingleton<ILlmProvider, OpenAiProvider>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/admin");

        adminGroup.MapPost("/login", (AdminLoginRequest request, IAdminAuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { message = "用户名或密码不能为空" });
            }

            if (!authService.ValidateCredentials(request.Username.Trim(), request.Password))
            {
                return Results.Unauthorized();
            }

            var token = authService.IssueToken();
            var expiresAt = authService.GetExpiry(token);
            return Results.Ok(new AdminLoginResponse(token, expiresAt));
        })
        .WithName("AdminLogin")
        .WithOpenApi();

        var secureAdminGroup = app.MapGroup("/api/admin").AddEndpointFilter<AdminAuthFilter>();

        secureAdminGroup.MapGet("/llm/settings", async (ILlmSettingsStore store) =>
        {
            var settings = await store.GetAllAsync();
            var result = settings.Select(ToResponse).ToArray();
            return Results.Ok(result);
        })
        .WithName("GetLlmSettings")
        .WithOpenApi();

        secureAdminGroup.MapGet("/llm/settings/{provider}", async (string provider, ILlmSettingsStore store) =>
        {
            var settings = await store.GetProviderAsync(provider);
            if (settings is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(ToResponse(settings));
        })
        .WithName("GetLlmProviderSettings")
        .WithOpenApi();

        secureAdminGroup.MapPut("/llm/settings/{provider}", async (string provider, LlmSettingsRequest request, ILlmSettingsStore store) =>
        {
            var updated = await store.UpsertAsync(new LlmProviderSettings
            {
                Provider = provider,
                ApiKey = request.ApiKey ?? string.Empty,
                BaseUrl = request.BaseUrl ?? string.Empty,
                Model = request.Model ?? string.Empty,
                SystemPrompt = request.SystemPrompt ?? string.Empty,
                ForceChinese = request.ForceChinese,
                Organization = request.Organization ?? string.Empty,
                Project = request.Project ?? string.Empty,
                Enabled = request.Enabled
            });

            return Results.Ok(ToResponse(updated));
        })
        .WithName("UpsertLlmProviderSettings")
        .WithOpenApi();

        secureAdminGroup.MapPost("/llm/test/{provider}", async (string provider, LlmChatRequestDto request, ILlmService llmService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return Results.BadRequest(new { message = "Prompt 不能为空" });
            }

            var result = await llmService.ChatAsync(provider, new LlmChatRequest(request.Prompt, request.Model, request.Temperature, request.UseInternet));
            return Results.Ok(new LlmChatResponseDto(result.Content));
        })
        .WithName("TestLlmProvider")
        .WithOpenApi();

        app.MapPost("/api/llm/chat/{provider}", async (string provider, LlmChatRequestDto request, ILlmService llmService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return Results.BadRequest(new { message = "Prompt 不能为空" });
            }

            try
            {
                var result = await llmService.ChatAsync(provider, new LlmChatRequest(request.Prompt, request.Model, request.Temperature, request.UseInternet));
                return Results.Ok(new LlmChatResponseDto(result.Content));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("ChatLlmProvider")
        .WithOpenApi();

        app.MapPost("/api/llm/chat/stream/{provider}", async (string provider, LlmChatRequestDto request, ILlmSettingsStore store, OpenAiProvider providerImpl, HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return Results.BadRequest(new { message = "Prompt 不能为空" });
            }

            var settings = await store.GetProviderAsync(provider) ?? new LlmProviderSettings { Provider = provider, Enabled = true };
            if (!settings.Enabled)
            {
                return Results.BadRequest(new { message = $"Provider {provider} 未启用" });
            }

            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.ContentType = "text/event-stream";

            try
            {
                await foreach (var chunk in providerImpl.StreamChatAsync(settings, new LlmChatRequest(request.Prompt, request.Model, request.Temperature, request.UseInternet), context.RequestAborted))
                {
                    await context.Response.WriteAsync($"data: {chunk}\n\n", context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                }
                await context.Response.WriteAsync("data: [DONE]\n\n", context.RequestAborted);
            }
            catch (Exception ex)
            {
                await context.Response.WriteAsync($"data: {ex.Message}\n\n", context.RequestAborted);
            }

            return Results.Empty;
        })
        .WithName("ChatLlmProviderStream")
        .WithOpenApi();
    }

    private static LlmSettingsResponse ToResponse(LlmProviderSettings settings)
    {
        var masked = MaskKey(settings.ApiKey);
        return new LlmSettingsResponse(
            settings.Provider,
            settings.BaseUrl,
            settings.Model,
            settings.SystemPrompt,
            settings.ForceChinese,
            settings.Organization,
            settings.Project,
            settings.Enabled,
            !string.IsNullOrWhiteSpace(settings.ApiKey),
            masked,
            settings.UpdatedAt);
    }

    private static string MaskKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var trimmed = key.Trim();
        if (trimmed.Length <= 8)
        {
            return "****";
        }

        return $"{trimmed[..4]}****{trimmed[^4..]}";
    }
}
