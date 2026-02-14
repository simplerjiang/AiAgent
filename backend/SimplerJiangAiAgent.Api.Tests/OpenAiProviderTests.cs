using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;
using SimplerJiangAiAgent.Api.Infrastructure.Logging;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class OpenAiProviderTests
{
    [Fact]
    public async Task ChatAsync_IncludesSystemPromptWhenProvided()
    {
        var handler = new CaptureHandler();
        var httpClient = new HttpClient(handler);
        var provider = new OpenAiProvider(httpClient, new FakeLogWriter());
        var settings = new LlmProviderSettings
        {
            Provider = "openai",
            ApiKey = "key",
            BaseUrl = "https://api.example.com/v1",
            SystemPrompt = "你是股票助手",
            ForceChinese = true
        };

        var result = await provider.ChatAsync(settings, new LlmChatRequest("hello", "gemini-3-pro-preview", 0.1, true), CancellationToken.None);

        Assert.Equal("ok", result.Content);
        Assert.NotNull(handler.LastRequestBody);
        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var contents = doc.RootElement.GetProperty("contents");
        var parts = contents[0].GetProperty("parts");
        var text = parts[0].GetProperty("text").GetString() ?? string.Empty;
        Assert.Contains("hello", text);
        var systemInstruction = doc.RootElement.GetProperty("system_instruction");
        var systemParts = systemInstruction.GetProperty("parts");
        var systemText = systemParts[0].GetProperty("text").GetString() ?? string.Empty;
        Assert.Contains("你是股票助手", systemText);
        Assert.Contains("请使用中文回答", systemText);
        var tools = doc.RootElement.GetProperty("tools");
        Assert.True(tools.GetArrayLength() > 0);
    }

    [Fact]
    public async Task StreamChatAsync_ParsesSseChunks()
    {
        var handler = new CaptureHandler();
        var httpClient = new HttpClient(handler);
        var provider = new OpenAiProvider(httpClient, new FakeLogWriter());
        var settings = new LlmProviderSettings
        {
            Provider = "openai",
            ApiKey = "key",
            BaseUrl = "https://www.dmxapi.cn/v1",
            SystemPrompt = "你是股票助手",
            ForceChinese = true
        };

        var chunks = new List<string>();
        await foreach (var chunk in provider.StreamChatAsync(settings, new LlmChatRequest("hello", "gemini-3-pro-preview", 0.1, true)))
        {
            chunks.Add(chunk);
        }

        Assert.Equal(new[] { "你", "好" }, chunks);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            if (request.RequestUri?.AbsolutePath.Contains("streamGenerateContent", StringComparison.OrdinalIgnoreCase) == true)
            {
                var sse = "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"你\"}]}}]}\n\n" +
                          "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"好\"}]}}]}\n\n" +
                          "data: [DONE]\n\n";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sse)
                };
            }

            var responseJson = request.RequestUri?.AbsolutePath.Contains("generateContent", StringComparison.OrdinalIgnoreCase) == true
                ? "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"ok\"}]}}]}"
                : "{\"choices\":[{\"message\":{\"content\":\"ok\"}}]}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            };
        }
    }

    private sealed class FakeLogWriter : IFileLogWriter
    {
        public void Write(string category, string message)
        {
        }
    }
}
