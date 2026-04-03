using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using SimplerJiangAiAgent.Api.Infrastructure.Logging;

namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public sealed class AntigravityProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly AntigravityOAuthService _oauthService;
    private readonly IFileLogWriter _logWriter;

    public AntigravityProvider(HttpClient httpClient, AntigravityOAuthService oauthService, IFileLogWriter logWriter)
    {
        _httpClient = httpClient;
        _oauthService = oauthService;
        _logWriter = logWriter;
    }

    public string Name => "antigravity";

    public async Task<LlmChatResult> ChatAsync(LlmProviderSettings settings, LlmChatRequest request, CancellationToken cancellationToken = default)
    {
        if (!_oauthService.IsConfigured)
            throw new InvalidOperationException("Antigravity OAuth 未配置（缺少 ClientId/ClientSecret），此 provider 不可用");

        var refreshToken = settings.ApiKey;
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException("Antigravity refresh token 未配置，请先登录 Google 账号");

        var accessToken = await _oauthService.EnsureAccessTokenAsync(refreshToken, cancellationToken);
        var projectId = _oauthService.CachedProjectId ?? settings.Project;
        if (string.IsNullOrWhiteSpace(projectId))
            projectId = AntigravityConstants.DefaultProjectId;

        var model = ResolveModel(request.Model ?? settings.Model);
        var body = BuildRequestBody(projectId, model, request, settings);

        Log($"request model={model} projectId={projectId} promptChars={request.Prompt?.Length ?? 0}", request.TraceId);

        var (responseText, _) = await SendWithFallbackAsync(accessToken, refreshToken, body, streaming: false, cancellationToken);
        var result = ParseNonStreamingResponse(responseText);

        Log($"response model={model} status=ok contentChars={result.Content?.Length ?? 0}", request.TraceId);
        return result;
    }

    /// <summary>
    /// 流式 SSE 调用
    /// </summary>
    public async IAsyncEnumerable<string> StreamChatAsync(
        LlmProviderSettings settings,
        LlmChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_oauthService.IsConfigured)
            throw new InvalidOperationException("Antigravity OAuth 未配置（缺少 ClientId/ClientSecret），此 provider 不可用");

        var refreshToken = settings.ApiKey;
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException("Antigravity refresh token 未配置，请先登录 Google 账号");

        var accessToken = await _oauthService.EnsureAccessTokenAsync(refreshToken, cancellationToken);
        var projectId = _oauthService.CachedProjectId ?? settings.Project;
        if (string.IsNullOrWhiteSpace(projectId))
            projectId = AntigravityConstants.DefaultProjectId;

        var model = ResolveModel(request.Model ?? settings.Model);
        var body = BuildRequestBody(projectId, model, request, settings);

        Log($"request-stream model={model} projectId={projectId}", request.TraceId);

        // 流式使用主端点，不做端点降级（简化），加单端点超时
        var endpoint = AntigravityConstants.GenerateEndpoints[0];
        var url = $"{endpoint}/v1internal:streamGenerateContent?alt=sse";

        using var streamCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        streamCts.CancelAfter(TimeSpan.FromSeconds(AntigravityConstants.PerEndpointTimeoutSeconds));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.TryAddWithoutValidation("User-Agent",
            string.Format(AntigravityConstants.UserAgentTemplate, _oauthService.AntigravityVersion));
        httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
        httpRequest.Headers.Accept.Clear();
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, streamCts.Token);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // 401: 刷新 token 重试一次
            _oauthService.InvalidateAccessToken();
            accessToken = await _oauthService.EnsureAccessTokenAsync(refreshToken, cancellationToken);

            using var retryRequest = new HttpRequestMessage(HttpMethod.Post, url);
            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            retryRequest.Headers.TryAddWithoutValidation("User-Agent",
                string.Format(AntigravityConstants.UserAgentTemplate, _oauthService.AntigravityVersion));
            retryRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
            retryRequest.Headers.Accept.Clear();
            retryRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            using var retryResponse = await _httpClient.SendAsync(retryRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!retryResponse.IsSuccessStatusCode)
            {
                var errorText = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Antigravity stream 请求失败: {retryResponse.StatusCode} {errorText}");
            }

            await foreach (var chunk in ParseSseStreamAsync(retryResponse, cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Antigravity stream 请求失败: {response.StatusCode} {errorText}");
        }

        await foreach (var chunk in ParseSseStreamAsync(response, cancellationToken))
        {
            yield return chunk;
        }
    }

    // =========== 请求构造 ===========

    private static string BuildRequestBody(string projectId, string model, LlmChatRequest request, LlmProviderSettings settings)
    {
        var systemPrompt = OpenAiProvider.BuildSystemPrompt(settings.SystemPrompt, settings.ForceChinese);

        // 构造 Gemini 格式 contents
        var contents = new List<object>
        {
            new
            {
                role = "user",
                parts = new[] { new { text = request.Prompt } }
            }
        };

        var requestObj = new Dictionary<string, object>
        {
            ["contents"] = contents,
            ["generationConfig"] = new
            {
                maxOutputTokens = 8192,
                temperature = request.Temperature ?? 0.7
            }
        };

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            // systemInstruction 必须是对象，不能是字符串
            requestObj["systemInstruction"] = new
            {
                parts = new[] { new { text = systemPrompt } }
            };
        }

        // Google Search Grounding — 仅 Gemini 模型支持
        if (request.UseInternet && IsGeminiModel(model))
        {
            requestObj["tools"] = new object[] { new Dictionary<string, object> { ["googleSearch"] = new { } } };
        }

        var wrapper = new Dictionary<string, object>
        {
            ["project"] = projectId,
            ["model"] = model,
            ["request"] = requestObj,
            ["requestType"] = "agent",
            ["userAgent"] = "antigravity",
            ["requestId"] = $"agent-{Guid.NewGuid()}"
        };

        return JsonSerializer.Serialize(wrapper);
    }

    private static bool IsGeminiModel(string model)
    {
        return model.StartsWith("gemini", StringComparison.OrdinalIgnoreCase);
    }

    // =========== 端点降级 + Token 刷新重试 ===========

    private async Task<(string responseText, HttpStatusCode statusCode)> SendWithFallbackAsync(
        string accessToken, string refreshToken, string body, bool streaming, CancellationToken ct)
    {
        Exception? lastException = null;
        var path = streaming
            ? "/v1internal:streamGenerateContent?alt=sse"
            : "/v1internal:generateContent";

        foreach (var endpoint in AntigravityConstants.GenerateEndpoints)
        {
            var url = $"{endpoint}{path}";
            using var endpointCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            endpointCts.CancelAfter(TimeSpan.FromSeconds(AntigravityConstants.PerEndpointTimeoutSeconds));
            try
            {
                var (responseText, statusCode) = await SendSingleRequestAsync(accessToken, url, body, streaming, endpointCts.Token);

                // 401: 刷新 token 重试一次
                if (statusCode == HttpStatusCode.Unauthorized)
                {
                    Log($"401 from {endpoint}, refreshing token");
                    _oauthService.InvalidateAccessToken();
                    accessToken = await _oauthService.EnsureAccessTokenAsync(refreshToken, endpointCts.Token);
                    (responseText, statusCode) = await SendSingleRequestAsync(accessToken, url, body, streaming, endpointCts.Token);

                    if (statusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new InvalidOperationException("Antigravity token 刷新后仍然 401，请重新登录 Google 账号");
                    }
                }

                // 429: 速率限制
                if (statusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryDelay = ParseRetryDelay(responseText);
                    if (retryDelay.HasValue && retryDelay.Value <= TimeSpan.FromSeconds(5))
                    {
                        Log($"429 from {endpoint}, waiting {retryDelay.Value.TotalSeconds:F1}s");
                        await Task.Delay(retryDelay.Value, endpointCts.Token);
                        (responseText, statusCode) = await SendSingleRequestAsync(accessToken, url, body, streaming, endpointCts.Token);
                    }

                    if (statusCode == HttpStatusCode.TooManyRequests)
                    {
                        throw new InvalidOperationException($"Antigravity 速率限制: {responseText}");
                    }
                }

                // 版本号过旧
                if (responseText.Contains("no longer supported", StringComparison.OrdinalIgnoreCase))
                {
                    Log("Version no longer supported, updating version");
                    await _oauthService.UpdateVersionAsync(endpointCts.Token);
                    (responseText, statusCode) = await SendSingleRequestAsync(accessToken, url, body, streaming, endpointCts.Token);
                }

                // 5xx: 尝试下一个端点
                if ((int)statusCode >= 500)
                {
                    Log($"5xx from {endpoint}: {statusCode}");
                    lastException = new InvalidOperationException($"Antigravity {endpoint} 返回 {statusCode}: {responseText}");
                    continue;
                }

                // 4xx 其他错误
                if (!IsSuccessStatusCode(statusCode))
                {
                    throw new InvalidOperationException($"Antigravity 请求失败: {statusCode} {responseText}");
                }

                return (responseText, statusCode);
            }
            catch (HttpRequestException ex)
            {
                Log($"Network error with {endpoint}: {ex.Message}");
                lastException = ex;
                continue; // 网络错误，尝试下一个端点
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                Log($"endpoint {endpoint} timed out after {AntigravityConstants.PerEndpointTimeoutSeconds}s, trying next");
                lastException = new TimeoutException($"Antigravity endpoint {endpoint} timed out after {AntigravityConstants.PerEndpointTimeoutSeconds}s");
                continue;
            }
        }

        throw lastException ?? new InvalidOperationException("Antigravity: 所有端点均不可用");
    }

    private async Task<(string responseText, HttpStatusCode statusCode)> SendSingleRequestAsync(
        string accessToken, string url, string body, bool streaming, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("User-Agent",
            string.Format(AntigravityConstants.UserAgentTemplate, _oauthService.AntigravityVersion));
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        if (streaming)
        {
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        }

        using var response = await _httpClient.SendAsync(request, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);
        return (responseText, response.StatusCode);
    }

    // =========== 响应解析 ===========

    private static LlmChatResult ParseNonStreamingResponse(string responseText)
    {
        using var doc = JsonDocument.Parse(responseText);
        var root = doc.RootElement;

        // 检查错误响应
        if (root.TryGetProperty("error", out var error))
        {
            var message = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
            throw new InvalidOperationException($"Antigravity API error: {message}");
        }

        // 外层有 "response" envelope
        if (!root.TryGetProperty("response", out var responseObj))
        {
            throw new InvalidOperationException($"Antigravity: 响应缺少 response 字段: {responseText[..Math.Min(200, responseText.Length)]}");
        }

        if (!responseObj.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
        {
            return new LlmChatResult(string.Empty);
        }

        var content = candidates[0].GetProperty("content");
        if (!content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array
            || parts.GetArrayLength() == 0)
        {
            return new LlmChatResult(string.Empty);
        }

        // 拼接所有 text parts，跳过 thinking blocks
        var sb = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("thought", out var thought) && thought.GetBoolean())
                continue;
            if (part.TryGetProperty("text", out var text))
                sb.Append(text.GetString());
        }

        return new LlmChatResult(sb.ToString().Trim());
    }

    /// <summary>
    /// 解析 SSE 流式响应
    /// </summary>
    private static async IAsyncEnumerable<string> ParseSseStreamAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            var json = line.Substring(6).Trim();
            if (json == "[DONE]") break;

            string? text = null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // SSE 也有 response envelope
                JsonElement responseObj;
                if (root.TryGetProperty("response", out var resp))
                    responseObj = resp;
                else
                    responseObj = root;

                if (responseObj.TryGetProperty("candidates", out var candidates)
                    && candidates.ValueKind == JsonValueKind.Array
                    && candidates.GetArrayLength() > 0)
                {
                    var candidate = candidates[0];
                    if (candidate.TryGetProperty("content", out var content)
                        && content.TryGetProperty("parts", out var parts)
                        && parts.ValueKind == JsonValueKind.Array
                        && parts.GetArrayLength() > 0)
                    {
                        foreach (var part in parts.EnumerateArray())
                        {
                            if (part.TryGetProperty("thought", out var thought) && thought.GetBoolean())
                                continue;
                            if (part.TryGetProperty("text", out var t))
                            {
                                var val = t.GetString();
                                if (!string.IsNullOrEmpty(val))
                                    text = val;
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // 跳过无法解析的 SSE 行
                continue;
            }

            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
        }
    }

    // =========== 辅助方法 ===========

    private static TimeSpan? ParseRetryDelay(string responseText)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("details", out var details)
                && details.ValueKind == JsonValueKind.Array)
            {
                foreach (var detail in details.EnumerateArray())
                {
                    if (detail.TryGetProperty("retryDelay", out var delayStr))
                    {
                        var delayText = delayStr.GetString();
                        if (!string.IsNullOrWhiteSpace(delayText) && delayText.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                        {
                            if (double.TryParse(delayText.TrimEnd('s', 'S'), System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var seconds))
                            {
                                return TimeSpan.FromSeconds(seconds);
                            }
                        }
                    }
                }
            }
        }
        catch { }
        return null;
    }

    private static bool IsSuccessStatusCode(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 200 && (int)statusCode < 300;
    }

    private static string ResolveModel(string? requestedModel)
    {
        if (string.IsNullOrWhiteSpace(requestedModel))
            return AntigravityConstants.DefaultFallbackModel;

        // 已经是 Antigravity 原生模型
        if (AntigravityConstants.AvailableModels.Contains(requestedModel, StringComparer.OrdinalIgnoreCase))
            return requestedModel;

        // 查映射表
        if (AntigravityConstants.ModelMapping.TryGetValue(requestedModel, out var mapped))
            return mapped;

        // 兜底：按名称模糊匹配
        if (requestedModel.Contains("flash", StringComparison.OrdinalIgnoreCase) ||
            requestedModel.Contains("lite", StringComparison.OrdinalIgnoreCase) ||
            requestedModel.Contains("nano", StringComparison.OrdinalIgnoreCase) ||
            requestedModel.Contains("mini", StringComparison.OrdinalIgnoreCase))
            return "gemini-3-flash";

        if (requestedModel.Contains("pro", StringComparison.OrdinalIgnoreCase))
            return "gemini-3-pro-high";

        if (requestedModel.Contains("claude", StringComparison.OrdinalIgnoreCase))
            return "claude-sonnet-4-6";

        if (requestedModel.Contains("gpt", StringComparison.OrdinalIgnoreCase))
            return "gemini-3-flash";

        // 完全未知 → 最便宜
        return AntigravityConstants.DefaultFallbackModel;
    }

    private void Log(string message, string? traceId = null)
    {
        var prefix = string.IsNullOrWhiteSpace(traceId) ? "" : $"traceId={traceId} ";
        _logWriter.Write("ANTIGRAVITY", $"{prefix}{message}");
    }
}
