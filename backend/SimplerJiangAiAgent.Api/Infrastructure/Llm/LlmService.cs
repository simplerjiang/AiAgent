namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public interface ILlmService
{
    Task<LlmChatResult> ChatAsync(string provider, LlmChatRequest request, CancellationToken cancellationToken = default);
}

public sealed class LlmService : ILlmService
{
    private readonly ILlmSettingsStore _settingsStore;
    private readonly IReadOnlyCollection<ILlmProvider> _providers;

    public LlmService(ILlmSettingsStore settingsStore, IEnumerable<ILlmProvider> providers)
    {
        _settingsStore = settingsStore;
        _providers = providers.ToArray();
    }

    public async Task<LlmChatResult> ChatAsync(string provider, LlmChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("provider 不能为空", nameof(provider));
        }

        var settings = await _settingsStore.GetProviderAsync(provider, cancellationToken)
            ?? new LlmProviderSettings { Provider = provider, Enabled = true };

        if (!settings.Enabled)
        {
            throw new InvalidOperationException($"Provider {provider} 未启用");
        }

        var target = _providers.FirstOrDefault(item => string.Equals(item.Name, provider, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            throw new InvalidOperationException($"未找到 provider: {provider}");
        }

        var finalRequest = request;
        if (settings.ForceChinese)
        {
            var hint = "请使用中文回答。";
            if (!request.Prompt.Contains(hint, StringComparison.OrdinalIgnoreCase))
            {
                finalRequest = request with { Prompt = $"{request.Prompt}\n\n{hint}" };
            }
        }

        return await target.ChatAsync(settings, finalRequest, cancellationToken);
    }
}
