namespace SimplerJiangAiAgent.Api.Modules.Llm.Models;

public sealed record LlmChatRequestDto(string Prompt, string? Model, double? Temperature, bool UseInternet = false);

public sealed record LlmChatResponseDto(string Content);
