using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;
using SimplerJiangAiAgent.Api.Infrastructure.Logging;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class LlmServiceTests
{
    [Fact]
    public async Task ChatAsync_UsesProviderAndReturnsContent()
    {
        var store = new FakeSettingsStore(new LlmProviderSettings
        {
            Provider = "openai",
            ApiKey = "key",
            Enabled = true,
            ForceChinese = true
        });
        var provider = new FakeProvider();
        var logs = new FakeLogWriter();
        var service = new LlmService(store, new[] { provider }, logs);

        var result = await service.ChatAsync("openai", new LlmChatRequest("hello", null, null, true), CancellationToken.None);

        Assert.Equal("echo:hello\n\n请使用中文回答。", result.Content);
        Assert.Equal("hello\n\n请使用中文回答。", provider.LastPrompt);
        Assert.Contains(logs.Entries, item => item.Contains("stage=request", StringComparison.Ordinal));
        Assert.Contains(logs.Entries, item => item.Contains("stage=response", StringComparison.Ordinal));
    }

    private sealed class FakeSettingsStore : ILlmSettingsStore
    {
        private readonly LlmProviderSettings _settings;

        public FakeSettingsStore(LlmProviderSettings settings)
        {
            _settings = settings;
        }

        public Task<IReadOnlyCollection<LlmProviderSettings>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<LlmProviderSettings>>(new[] { _settings });

        public Task<LlmProviderSettings?> GetProviderAsync(string provider, CancellationToken cancellationToken = default)
            => Task.FromResult<LlmProviderSettings?>(_settings);

        public Task<LlmProviderSettings> UpsertAsync(LlmProviderSettings settings, CancellationToken cancellationToken = default)
            => Task.FromResult(settings);
    }

    private sealed class FakeProvider : ILlmProvider
    {
        public string Name => "openai";
        public string? LastPrompt { get; private set; }

        public Task<LlmChatResult> ChatAsync(LlmProviderSettings settings, LlmChatRequest request, CancellationToken cancellationToken = default)
        {
            LastPrompt = request.Prompt;
            return Task.FromResult(new LlmChatResult($"echo:{request.Prompt}"));
        }
    }

    private sealed class FakeLogWriter : IFileLogWriter
    {
        public List<string> Entries { get; } = new();

        public void Write(string category, string message)
        {
            Entries.Add($"{category}:{message}");
        }
    }
}
