namespace SimplerJiangAiAgent.Api.Modules.Llm.Models;

public sealed record AdminLoginRequest(string Username, string Password);

public sealed record AdminLoginResponse(string Token, DateTimeOffset ExpiresAt);
