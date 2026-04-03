using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;
using SimplerJiangAiAgent.Api.Infrastructure.Logging;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class AntigravityProviderTests
{
    private static IConfiguration BuildTestConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Antigravity:ClientId"] = "test-client-id",
                ["Antigravity:ClientSecret"] = "test-client-secret"
            })
            .Build();
    // =========== Test 1: 正常非流式响应解析 ===========
    [Fact]
    public async Task ChatAsync_ParsesNonStreamingResponse()
    {
        var handler = new AntigravityHandler();
        var httpClient = new HttpClient(handler);
        var oauthService = new AntigravityOAuthService(httpClient, new FakeLogWriter(), BuildTestConfig());
        var provider = new AntigravityProvider(httpClient, oauthService, new FakeLogWriter());

        var settings = new LlmProviderSettings
        {
            Provider = "antigravity",
            ProviderType = "antigravity",
            ApiKey = "fake-refresh-token",
            Project = "test-project",
            SystemPrompt = "你是助手",
            ForceChinese = true
        };

        var result = await provider.ChatAsync(settings,
            new LlmChatRequest("你好", "gemini-3-flash", 0.7),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("测试回复内容", result.Content);

        // 验证请求格式
        Assert.NotNull(handler.LastGenerateRequestBody);
        using var doc = JsonDocument.Parse(handler.LastGenerateRequestBody!);
        var root = doc.RootElement;
        Assert.Equal("test-project", root.GetProperty("project").GetString());
        Assert.Equal("gemini-3-flash", root.GetProperty("model").GetString());

        var request = root.GetProperty("request");
        var contents = request.GetProperty("contents");
        Assert.Equal("user", contents[0].GetProperty("role").GetString());

        // 验证 systemInstruction 是对象不是字符串
        var systemInstruction = request.GetProperty("systemInstruction");
        var parts = systemInstruction.GetProperty("parts");
        Assert.True(parts.GetArrayLength() > 0);
    }

    // =========== Test 2: 缺少 refresh_token 抛异常 ===========
    [Fact]
    public async Task ChatAsync_ThrowsWhenNoRefreshToken()
    {
        var handler = new AntigravityHandler();
        var httpClient = new HttpClient(handler);
        var oauthService = new AntigravityOAuthService(httpClient, new FakeLogWriter(), BuildTestConfig());
        var provider = new AntigravityProvider(httpClient, oauthService, new FakeLogWriter());

        var settings = new LlmProviderSettings
        {
            Provider = "antigravity",
            ApiKey = "" // 没有 refresh_token
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.ChatAsync(settings, new LlmChatRequest("你好", null, null), CancellationToken.None));

        Assert.Contains("refresh token", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // =========== Test 3: 流式 SSE 解析 ===========
    [Fact]
    public async Task StreamChatAsync_ParsesSseChunks()
    {
        var handler = new AntigravityStreamHandler();
        var httpClient = new HttpClient(handler);
        var oauthService = new AntigravityOAuthService(httpClient, new FakeLogWriter(), BuildTestConfig());
        var provider = new AntigravityProvider(httpClient, oauthService, new FakeLogWriter());

        var settings = new LlmProviderSettings
        {
            Provider = "antigravity",
            ProviderType = "antigravity",
            ApiKey = "fake-refresh-token",
            Project = "test-project"
        };

        var chunks = new List<string>();
        await foreach (var chunk in provider.StreamChatAsync(settings,
            new LlmChatRequest("你好", "gemini-3-flash", 0.7)))
        {
            chunks.Add(chunk);
        }

        Assert.Equal(new[] { "你", "好", "世界" }, chunks);
    }

    // =========== Test: 模型自动映射 ===========
    [Theory]
    [InlineData("gpt-4.1-nano", "gemini-3-flash")]
    [InlineData("gemini-3.1-flash-lite-preview-thinking-high", "gemini-3-flash")]
    [InlineData("gemini-2.0-flash", "gemini-3-flash")]
    [InlineData("gpt-4o", "gemini-3-pro-high")]
    [InlineData("claude-3.5-sonnet", "claude-sonnet-4-6")]
    [InlineData("gemini-3-flash", "gemini-3-flash")] // 原生模型不变
    [InlineData("gemini-3-pro-high", "gemini-3-pro-high")] // 原生模型不变
    [InlineData("unknown-model-xyz", "gemini-3-flash")] // 完全未知降级
    [InlineData(null, "gemini-3-flash")] // null 降级
    [InlineData("", "gemini-3-flash")] // 空字符串降级
    [InlineData("some-flash-variant", "gemini-3-flash")] // 模糊匹配 flash
    [InlineData("some-pro-variant", "gemini-3-pro-high")] // 模糊匹配 pro
    [InlineData("claude-future", "claude-sonnet-4-6")] // 模糊匹配 claude
    [InlineData("gpt-5-turbo", "gemini-3-flash")] // 模糊匹配 gpt
    public void ResolveModel_Maps_Correctly(string? input, string expected)
    {
        var method = typeof(AntigravityProvider).GetMethod("ResolveModel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method!.Invoke(null, new object?[] { input });
        Assert.Equal(expected, result);
    }

    // =========== Test 4: 错误响应解析 ===========
    [Fact]
    public async Task ChatAsync_ThrowsOnErrorResponse()
    {
        var handler = new AntigravityErrorHandler(HttpStatusCode.BadRequest,
            @"{""error"":{""code"":400,""message"":""Invalid model"",""status"":""INVALID_ARGUMENT""}}");
        var httpClient = new HttpClient(handler);
        var oauthService = new AntigravityOAuthService(httpClient, new FakeLogWriter(), BuildTestConfig());
        var provider = new AntigravityProvider(httpClient, oauthService, new FakeLogWriter());

        var settings = new LlmProviderSettings
        {
            Provider = "antigravity",
            ApiKey = "fake-refresh-token",
            Project = "test-project"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.ChatAsync(settings, new LlmChatRequest("你好", "bad-model", null), CancellationToken.None));

        Assert.Contains("400", ex.Message);
    }

    // =========== Test 5: 空 candidates 返回空内容 ===========
    [Fact]
    public async Task ChatAsync_ReturnsEmptyOnNoCandidates()
    {
        var handler = new AntigravityCustomResponseHandler(
            @"{""response"":{""candidates"":[]},""traceId"":""abc""}");
        var httpClient = new HttpClient(handler);
        var oauthService = new AntigravityOAuthService(httpClient, new FakeLogWriter(), BuildTestConfig());
        var provider = new AntigravityProvider(httpClient, oauthService, new FakeLogWriter());

        var settings = new LlmProviderSettings
        {
            Provider = "antigravity",
            ApiKey = "fake-refresh-token",
            Project = "test-project"
        };

        var result = await provider.ChatAsync(settings,
            new LlmChatRequest("你好", "gemini-3-flash", null), CancellationToken.None);

        Assert.Equal(string.Empty, result.Content);
    }

    // =========== Test 6: Thinking blocks 被过滤 ===========
    [Fact]
    public async Task ChatAsync_SkipsThinkingBlocks()
    {
        var responseJson = @"{
            ""response"": {
                ""candidates"": [{
                    ""content"": {
                        ""role"": ""model"",
                        ""parts"": [
                            { ""thought"": true, ""text"": ""让我思考一下..."" },
                            { ""text"": ""最终回答"" }
                        ]
                    },
                    ""finishReason"": ""STOP""
                }]
            },
            ""traceId"": ""abc""
        }";
        var handler = new AntigravityCustomResponseHandler(responseJson);
        var httpClient = new HttpClient(handler);
        var oauthService = new AntigravityOAuthService(httpClient, new FakeLogWriter(), BuildTestConfig());
        var provider = new AntigravityProvider(httpClient, oauthService, new FakeLogWriter());

        var settings = new LlmProviderSettings
        {
            Provider = "antigravity",
            ApiKey = "fake-refresh-token",
            Project = "test-project"
        };

        var result = await provider.ChatAsync(settings,
            new LlmChatRequest("你好", "claude-opus-4-6-thinking", null), CancellationToken.None);

        Assert.Equal("最终回答", result.Content);
        Assert.DoesNotContain("让我思考", result.Content);
    }

    // =========== Test 7: Token 刷新和缓存 ===========
    [Fact]
    public async Task EnsureAccessToken_CachesAndRefreshes()
    {
        var handler = new TokenRefreshHandler();
        var httpClient = new HttpClient(handler);
        var oauthService = new AntigravityOAuthService(httpClient, new FakeLogWriter(), BuildTestConfig());

        // 第一次调用应该触发 refresh
        var token1 = await oauthService.EnsureAccessTokenAsync("fake-refresh-token");
        Assert.Equal("ya29.test-access-token", token1);
        Assert.Equal(1, handler.RefreshCount);

        // 第二次调用应该使用缓存
        var token2 = await oauthService.EnsureAccessTokenAsync("fake-refresh-token");
        Assert.Equal("ya29.test-access-token", token2);
        Assert.Equal(1, handler.RefreshCount); // 没有增加

        // 失效后应该重新刷新
        oauthService.InvalidateAccessToken();
        var token3 = await oauthService.EnsureAccessTokenAsync("fake-refresh-token");
        Assert.Equal("ya29.test-access-token", token3);
        Assert.Equal(2, handler.RefreshCount);
    }

    // =========== Test 8: UseInternet + Gemini → googleSearch tool ===========
    [Fact]
    public void BuildRequestBody_WithUseInternet_AddsGoogleSearchTool()
    {
        var method = typeof(AntigravityProvider).GetMethod("BuildRequestBody",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (string)method!.Invoke(null, new object[]
        {
            "test-project",
            "gemini-3-flash",
            new LlmChatRequest("你好", "gemini-3-flash", 0.7, UseInternet: true),
            new LlmProviderSettings { SystemPrompt = "test", ForceChinese = false }
        })!;

        using var doc = JsonDocument.Parse(result);
        var request = doc.RootElement.GetProperty("request");
        Assert.True(request.TryGetProperty("tools", out var tools));
        Assert.Equal(JsonValueKind.Array, tools.ValueKind);
        Assert.Equal(1, tools.GetArrayLength());
        Assert.True(tools[0].TryGetProperty("googleSearch", out _));
    }

    // =========== Test 9: UseInternet + Claude → 无 tools ===========
    [Fact]
    public void BuildRequestBody_WithUseInternet_SkipsForClaudeModel()
    {
        var method = typeof(AntigravityProvider).GetMethod("BuildRequestBody",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (string)method!.Invoke(null, new object[]
        {
            "test-project",
            "claude-sonnet-4-6",
            new LlmChatRequest("你好", "claude-sonnet-4-6", 0.7, UseInternet: true),
            new LlmProviderSettings { SystemPrompt = "test", ForceChinese = false }
        })!;

        using var doc = JsonDocument.Parse(result);
        var request = doc.RootElement.GetProperty("request");
        Assert.False(request.TryGetProperty("tools", out _));
    }

    // =========== Test 10: UseInternet=false → 无 tools ===========
    [Fact]
    public void BuildRequestBody_WithoutUseInternet_NoTools()
    {
        var method = typeof(AntigravityProvider).GetMethod("BuildRequestBody",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (string)method!.Invoke(null, new object[]
        {
            "test-project",
            "gemini-3-flash",
            new LlmChatRequest("你好", "gemini-3-flash", 0.7, UseInternet: false),
            new LlmProviderSettings { SystemPrompt = "test", ForceChinese = false }
        })!;

        using var doc = JsonDocument.Parse(result);
        var request = doc.RootElement.GetProperty("request");
        Assert.False(request.TryGetProperty("tools", out _));
    }

    // =========== Handler 实现 ===========

    /// <summary>
    /// 模拟 Antigravity API：token 刷新 + generateContent
    /// </summary>
    private sealed class AntigravityHandler : HttpMessageHandler
    {
        public string? LastGenerateRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsoluteUri ?? "";

            // Token 刷新请求
            if (uri.Contains("oauth2.googleapis.com/token"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        @"{""access_token"":""ya29.test"",""expires_in"":3599,""token_type"":""Bearer""}",
                        Encoding.UTF8, "application/json")
                };
            }

            // generateContent 请求
            if (uri.Contains("generateContent"))
            {
                LastGenerateRequestBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);

                var response = @"{
                    ""response"": {
                        ""candidates"": [{
                            ""content"": {
                                ""role"": ""model"",
                                ""parts"": [{ ""text"": ""测试回复内容"" }]
                            },
                            ""finishReason"": ""STOP""
                        }],
                        ""usageMetadata"": {
                            ""promptTokenCount"": 10,
                            ""candidatesTokenCount"": 20,
                            ""totalTokenCount"": 30
                        }
                    },
                    ""traceId"": ""test-trace""
                }";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(response, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    /// <summary>
    /// 模拟 SSE 流式响应
    /// </summary>
    private sealed class AntigravityStreamHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsoluteUri ?? "";

            if (uri.Contains("oauth2.googleapis.com/token"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        @"{""access_token"":""ya29.test"",""expires_in"":3599,""token_type"":""Bearer""}",
                        Encoding.UTF8, "application/json")
                });
            }

            if (uri.Contains("streamGenerateContent"))
            {
                var sse =
                    "data: {\"response\":{\"candidates\":[{\"content\":{\"role\":\"model\",\"parts\":[{\"text\":\"你\"}]}}]},\"traceId\":\"t1\"}\n\n" +
                    "data: {\"response\":{\"candidates\":[{\"content\":{\"role\":\"model\",\"parts\":[{\"text\":\"好\"}]}}]},\"traceId\":\"t2\"}\n\n" +
                    "data: {\"response\":{\"candidates\":[{\"content\":{\"role\":\"model\",\"parts\":[{\"text\":\"世界\"}]},\"finishReason\":\"STOP\"}]},\"traceId\":\"t3\"}\n\n";

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    /// <summary>
    /// 模拟错误响应（所有端点都返回相同错误）
    /// </summary>
    private sealed class AntigravityErrorHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;

        public AntigravityErrorHandler(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsoluteUri ?? "";

            if (uri.Contains("oauth2.googleapis.com/token"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        @"{""access_token"":""ya29.test"",""expires_in"":3599,""token_type"":""Bearer""}",
                        Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }

    /// <summary>
    /// 返回自定义的成功响应
    /// </summary>
    private sealed class AntigravityCustomResponseHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public AntigravityCustomResponseHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsoluteUri ?? "";

            if (uri.Contains("oauth2.googleapis.com/token"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        @"{""access_token"":""ya29.test"",""expires_in"":3599,""token_type"":""Bearer""}",
                        Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            });
        }
    }

    /// <summary>
    /// 模拟 token 刷新，计数器跟踪调用次数
    /// </summary>
    private sealed class TokenRefreshHandler : HttpMessageHandler
    {
        public int RefreshCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsoluteUri ?? "";

            if (uri.Contains("oauth2.googleapis.com/token"))
            {
                RefreshCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        @"{""access_token"":""ya29.test-access-token"",""expires_in"":3599,""token_type"":""Bearer""}",
                        Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class FakeLogWriter : IFileLogWriter
    {
        public void Write(string category, string message) { }
    }
}
