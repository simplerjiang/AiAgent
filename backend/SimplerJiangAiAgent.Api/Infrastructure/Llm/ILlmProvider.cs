namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public interface ILlmProvider
{
    string Name { get; }
    Task<LlmChatResult> ChatAsync(LlmProviderSettings settings, LlmChatRequest request, CancellationToken cancellationToken = default);
}
