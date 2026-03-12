using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SimplerJiangAiAgent.Api.Infrastructure.Logging;

namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public sealed class OpenAiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly IFileLogWriter _fileLogWriter;

    public OpenAiProvider(HttpClient httpClient, IFileLogWriter fileLogWriter)
    {
        _httpClient = httpClient;
        _fileLogWriter = fileLogWriter;
    }

    public string Name => "openai";

    public async Task<LlmChatResult> ChatAsync(LlmProviderSettings settings, LlmChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API Key 未配置");
        }

        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://api.openai.com/v1"
            : settings.BaseUrl.TrimEnd('/');

        var model = string.IsNullOrWhiteSpace(request.Model) ? settings.Model : request.Model;
        if (string.IsNullOrWhiteSpace(model))
        {
            model = "gpt-4o-mini";
        }

        var systemPrompt = BuildSystemPrompt(settings.SystemPrompt, settings.ForceChinese);
        LogInfo($"request provider=openai model={model} useInternet={request.UseInternet} baseUrl={baseUrl} promptChars={SafeLength(request.Prompt)} systemChars={SafeLength(systemPrompt)}", request.TraceId);
        LogPrompt("openai", model, request.Prompt, systemPrompt, request.TraceId);

        if (request.UseInternet && ShouldUseGeminiInternet(baseUrl, model))
        {
            LogInfo($"route=gemini provider=openai model={model} promptChars={SafeLength(request.Prompt)} systemChars={SafeLength(systemPrompt)}", request.TraceId);
            return await ChatWithGeminiInternetAsync(settings, request, baseUrl, model, cancellationToken);
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        if (!string.IsNullOrWhiteSpace(settings.Organization))
        {
            message.Headers.Add("OpenAI-Organization", settings.Organization);
        }
        if (!string.IsNullOrWhiteSpace(settings.Project))
        {
            message.Headers.Add("OpenAI-Project", settings.Project);
        }

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new { role = "system", content = systemPrompt });
        }
        messages.Add(new { role = "user", content = request.Prompt });

        var payload = new
        {
            model,
            messages,
            temperature = request.Temperature ?? 0.7
        };

        message.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            LogError($"error provider=openai model={model} status={response.StatusCode} body={responseText}", request.TraceId);
            throw new InvalidOperationException($"OpenAI 请求失败: {response.StatusCode} {responseText}");
        }

        using var doc = ParseResponseDocument(responseText, "OpenAI", request.TraceId);
        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            return new LlmChatResult(string.Empty);
        }

        var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        LogInfo($"response provider=openai model={model} status=ok", request.TraceId);
        LogResponse("openai", model, content, request.TraceId);
        return new LlmChatResult(content.Trim());
    }

    internal static bool ShouldUseGeminiInternet(string baseUrl, string model)
    {
        if (model.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return baseUrl.Contains("dmxapi.cn", StringComparison.OrdinalIgnoreCase)
            || baseUrl.Contains("jeniya.cn", StringComparison.OrdinalIgnoreCase);
    }

    internal static string BuildSystemPrompt(string? systemPrompt, bool forceChinese)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            parts.Add(systemPrompt.Trim());
        }

        if (forceChinese)
        {
            parts.Add("请使用中文回答。");
        }

        return string.Join("\n", parts);
    }

    private async Task<LlmChatResult> ChatWithGeminiInternetAsync(
        LlmProviderSettings settings,
        LlmChatRequest request,
        string baseUrl,
        string model,
        CancellationToken cancellationToken)
    {
        var root = baseUrl;
        if (root.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) || root.EndsWith("/v1beta", StringComparison.OrdinalIgnoreCase))
        {
            root = root[..root.LastIndexOf('/')];
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{root}/v1beta/models/{model}:generateContent");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        var systemPrompt = BuildSystemPrompt(settings.SystemPrompt, settings.ForceChinese);
        var prompt = request.Prompt;
        LogInfo($"request provider=gemini model={model} useInternet={request.UseInternet} promptChars={SafeLength(prompt)} systemChars={SafeLength(systemPrompt)}", request.TraceId);
        LogPrompt("gemini", model, prompt, systemPrompt, request.TraceId);

        var forceJson = ShouldForceJsonResponse(prompt, systemPrompt);
        var generationConfig = new Dictionary<string, object?>
        {
            ["temperature"] = request.Temperature ?? 0.7
        };
        if (forceJson)
        {
            generationConfig["responseMimeType"] = "application/json";
        }

        var payload = new Dictionary<string, object?>
        {
            ["contents"] = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            ["generationConfig"] = generationConfig
        };
        if (forceJson)
        {
            payload["response_mime_type"] = "application/json";
        }

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            payload["system_instruction"] = new
            {
                parts = new[] { new { text = systemPrompt } }
            };
        }

        if (request.UseInternet)
        {
            payload["tools"] = BuildGeminiTools(model);
            LogTools(payload["tools"], "gemini", model, request.TraceId);
        }

        message.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode && request.UseInternet && responseText.Contains("tool_type", StringComparison.OrdinalIgnoreCase))
        {
            LogInfo($"retry provider=gemini model={model} reason=tool_type", request.TraceId);
            var retryPayload = new Dictionary<string, object?>(payload)
            {
                ["tools"] = BuildGeminiToolsFallback(model)
            };
            using var retryMessage = new HttpRequestMessage(HttpMethod.Post, $"{root}/v1beta/models/{model}:generateContent");
            retryMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            retryMessage.Content = JsonContent.Create(retryPayload);
            using var retryResponse = await _httpClient.SendAsync(retryMessage, cancellationToken);
            responseText = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!retryResponse.IsSuccessStatusCode)
            {
                LogError($"error provider=gemini model={model} status={retryResponse.StatusCode} body={responseText}", request.TraceId);
                throw new InvalidOperationException($"Gemini 联网请求失败: {retryResponse.StatusCode} {responseText}");
            }
        }
        else if (!response.IsSuccessStatusCode)
        {
            LogError($"error provider=gemini model={model} status={response.StatusCode} body={responseText}", request.TraceId);
            throw new InvalidOperationException($"Gemini 联网请求失败: {response.StatusCode} {responseText}");
        }

        using var doc = ParseResponseDocument(responseText, "Gemini", request.TraceId);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
        {
            return new LlmChatResult(string.Empty);
        }

        var firstCandidate = candidates[0];
        if (firstCandidate.ValueKind != JsonValueKind.Object
            || !firstCandidate.TryGetProperty("content", out var contentNode)
            || contentNode.ValueKind != JsonValueKind.Object
            || !contentNode.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array
            || parts.GetArrayLength() == 0)
        {
            return new LlmChatResult(string.Empty);
        }

        var text = parts[0].GetProperty("text").GetString() ?? string.Empty;
        LogInfo($"response provider=gemini model={model} status=ok", request.TraceId);
        LogResponse("gemini", model, text, request.TraceId);
        return new LlmChatResult(text.Trim());
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        LlmProviderSettings settings,
        LlmChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API Key 未配置");
        }

        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://api.openai.com/v1"
            : settings.BaseUrl.TrimEnd('/');

        var model = string.IsNullOrWhiteSpace(request.Model) ? settings.Model : request.Model;
        if (string.IsNullOrWhiteSpace(model))
        {
            model = "gpt-4o-mini";
        }

        var streamSystemPrompt = BuildSystemPrompt(settings.SystemPrompt, settings.ForceChinese);
        LogInfo($"request-stream provider=openai model={model} useInternet={request.UseInternet} baseUrl={baseUrl} promptChars={SafeLength(request.Prompt)} systemChars={SafeLength(streamSystemPrompt)}", request.TraceId);
        LogPrompt("openai-stream", model, request.Prompt, streamSystemPrompt, request.TraceId);

        if (request.UseInternet && ShouldUseGeminiInternet(baseUrl, model))
        {
            LogInfo($"route=gemini-stream provider=openai model={model} promptChars={SafeLength(request.Prompt)} systemChars={SafeLength(streamSystemPrompt)}", request.TraceId);
            await foreach (var chunk in StreamGeminiAsync(settings, request, baseUrl, model, cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        var result = await ChatAsync(settings, request, cancellationToken);
        if (!string.IsNullOrWhiteSpace(result.Content))
        {
            yield return result.Content;
        }
    }

    private async IAsyncEnumerable<string> StreamGeminiAsync(
        LlmProviderSettings settings,
        LlmChatRequest request,
        string baseUrl,
        string model,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var root = baseUrl;
        if (root.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) || root.EndsWith("/v1beta", StringComparison.OrdinalIgnoreCase))
        {
            root = root[..root.LastIndexOf('/')];
        }

        var url = $"{root}/v1beta/models/{model}:streamGenerateContent?key={settings.ApiKey}&alt=sse";
        using var message = new HttpRequestMessage(HttpMethod.Post, url);

        var systemPrompt = BuildSystemPrompt(settings.SystemPrompt, settings.ForceChinese);
        var prompt = request.Prompt;
        var forceJson = ShouldForceJsonResponse(prompt, systemPrompt);
        var generationConfig = new Dictionary<string, object?>
        {
            ["temperature"] = request.Temperature ?? 0.7
        };
        if (forceJson)
        {
            generationConfig["responseMimeType"] = "application/json";
        }

        var payload = new Dictionary<string, object?>
        {
            ["contents"] = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            ["generationConfig"] = generationConfig
        };
        if (forceJson)
        {
            payload["response_mime_type"] = "application/json";
        }
        LogInfo($"request provider=gemini-stream model={model} useInternet={request.UseInternet} promptChars={SafeLength(prompt)} systemChars={SafeLength(systemPrompt)}", request.TraceId);
        LogPrompt("gemini-stream", model, prompt, systemPrompt, request.TraceId);

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            payload["system_instruction"] = new
            {
                parts = new[] { new { text = systemPrompt } }
            };
        }

        if (request.UseInternet)
        {
            payload["tools"] = BuildGeminiTools(model);
            LogTools(payload["tools"], "gemini-stream", model, request.TraceId);
        }

        message.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (request.UseInternet && errorText.Contains("tool_type", StringComparison.OrdinalIgnoreCase))
            {
                LogInfo($"retry provider=gemini-stream model={model} reason=tool_type", request.TraceId);
                var retryPayload = new Dictionary<string, object?>(payload)
                {
                    ["tools"] = BuildGeminiToolsFallback(model)
                };
                using var retryMessage = new HttpRequestMessage(HttpMethod.Post, url);
                retryMessage.Content = JsonContent.Create(retryPayload);
                using var retryResponse = await _httpClient.SendAsync(retryMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!retryResponse.IsSuccessStatusCode)
                {
                    var retryError = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
                    LogError($"error provider=gemini-stream model={model} status={retryResponse.StatusCode} body={retryError}", request.TraceId);
                    throw new InvalidOperationException($"Gemini 联网流式请求失败: {retryResponse.StatusCode} {retryError}");
                }

                await foreach (var chunk in StreamGeminiResponseAsync(retryResponse, cancellationToken))
                {
                    yield return chunk;
                }
                yield break;
            }

            LogError($"error provider=gemini-stream model={model} status={response.StatusCode} body={errorText}", request.TraceId);
            throw new InvalidOperationException($"Gemini 联网流式请求失败: {response.StatusCode} {errorText}");
        }
        await foreach (var chunk in StreamGeminiResponseAsync(response, cancellationToken))
        {
            yield return chunk;
        }
    }

    private static List<string> ExtractGeminiChunks(string json)
    {
        if (!LooksLikeJson(json))
        {
            return new List<string>();
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
        {
            return new List<string>();
        }

        var firstCandidate = candidates[0];
        if (firstCandidate.ValueKind != JsonValueKind.Object
            || !firstCandidate.TryGetProperty("content", out var content)
            || content.ValueKind != JsonValueKind.Object
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array
            || parts.GetArrayLength() == 0)
        {
            return new List<string>();
        }

        var result = new List<string>();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.ValueKind == JsonValueKind.Object
                && part.TryGetProperty("text", out var textNode)
                && textNode.ValueKind == JsonValueKind.String)
            {
                var text = textNode.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    result.Add(text);
                }
            }
        }

        return result;
    }

    private void LogInfo(string message, string? traceId = null)
    {
        _fileLogWriter.Write("LLM", PrefixTraceId(traceId, message));
    }

    private void LogError(string message, string? traceId = null)
    {
        _fileLogWriter.Write("LLM", PrefixTraceId(traceId, message));
    }

    private void LogTools(object? tools, string provider, string model, string? traceId = null)
    {
        if (tools is null)
        {
            return;
        }

        try
        {
            var text = JsonSerializer.Serialize(tools);
            _fileLogWriter.Write("LLM", PrefixTraceId(traceId, $"tools provider={provider} model={model} payload={text}"));
        }
        catch (Exception ex)
        {
            _fileLogWriter.Write("LLM", PrefixTraceId(traceId, $"tools provider={provider} model={model} error={ex.Message}"));
        }
    }

    private static int SafeLength(string? value)
    {
        return string.IsNullOrEmpty(value) ? 0 : value.Length;
    }

    private void LogPrompt(string provider, string model, string? prompt, string? systemPrompt, string? traceId = null)
    {
        _fileLogWriter.Write("LLM", PrefixTraceId(traceId, $"prompt provider={provider} model={model} systemPrompt={systemPrompt ?? string.Empty}"));
        _fileLogWriter.Write("LLM", PrefixTraceId(traceId, $"prompt provider={provider} model={model} userPrompt={prompt ?? string.Empty}"));
    }

    private void LogResponse(string provider, string model, string? content, string? traceId = null)
    {
        _fileLogWriter.Write("LLM", PrefixTraceId(traceId, $"response provider={provider} model={model} content={content ?? string.Empty}"));
    }

    private static string PrefixTraceId(string? traceId, string message)
    {
        if (string.IsNullOrWhiteSpace(traceId) || message.Contains("traceId=", StringComparison.OrdinalIgnoreCase))
        {
            return message;
        }

        return $"traceId={traceId} {message}";
    }

    private static bool ShouldForceJsonResponse(string? prompt, string? systemPrompt)
    {
        var merged = string.Join("\n", new[] { systemPrompt, prompt }.Where(value => !string.IsNullOrWhiteSpace(value)))
            .ToLowerInvariant();

        if (merged.Contains("不要json") || merged.Contains("no json"))
        {
            return false;
        }

        return merged.Contains("必须输出严格json")
            || merged.Contains("只输出json")
            || merged.Contains("输出json")
            || merged.Contains("json结构")
            || merged.Contains("json对象")
            || merged.Contains("json数组");
    }

    private static object[] BuildGeminiTools(string model)
    {
        return new[] { new { googleSearch = new { } } };
    }

    private static object[] BuildGeminiToolsFallback(string model)
    {
        return new[] { new { google_search = new { } } };
    }

    private JsonDocument ParseResponseDocument(string content, string providerName, string? traceId)
    {
        if (!LooksLikeJson(content))
        {
            LogError($"error provider={providerName} responseType=non-json preview={BuildPreview(content)}", traceId);
            throw new InvalidOperationException($"{providerName} 返回了非 JSON 内容，可能是网关或 HTML 错页");
        }

        try
        {
            return JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            LogError($"error provider={providerName} responseType=invalid-json message={ex.Message} preview={BuildPreview(content)}", traceId);
            throw new InvalidOperationException($"{providerName} 返回 JSON 解析失败: {ex.Message}", ex);
        }
    }

    private static bool LooksLikeJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var trimmed = content.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static string BuildPreview(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var normalized = content
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return normalized.Length <= 160 ? normalized : normalized[..160];
    }

    private static async IAsyncEnumerable<string> StreamGeminiResponseAsync(
        HttpResponseMessage response,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var json = line[5..].Trim();
            if (string.IsNullOrWhiteSpace(json) || json.Equals("[DONE]", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            List<string> chunks;
            try
            {
                chunks = ExtractGeminiChunks(json);
            }
            catch (JsonException)
            {
                continue;
            }

            foreach (var chunk in chunks)
            {
                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    yield return chunk;
                }
            }
        }
    }
}
