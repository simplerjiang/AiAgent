namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public static class LlmResponseFormats
{
	public const string Json = "json";
}

public sealed record LlmChatRequest(
	string Prompt,
	string? Model,
	double? Temperature,
	bool UseInternet = false,
	string? TraceId = null,
	string? ResponseFormat = null,
	int? MaxOutputTokens = null);

public sealed record LlmChatResult(string Content, string? TraceId = null);
