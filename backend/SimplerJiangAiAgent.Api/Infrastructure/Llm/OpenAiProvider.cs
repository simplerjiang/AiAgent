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
        LogInfo($"request provider=openai model={model} useInternet={request.UseInternet} baseUrl={baseUrl} promptChars={SafeLength(request.Prompt)} systemChars={SafeLength(systemPrompt)}");
        LogPrompt("openai", model, request.Prompt, systemPrompt);

        if (request.UseInternet && ShouldUseGeminiInternet(baseUrl, model))
        {
            LogInfo($"route=gemini provider=openai model={model} promptChars={SafeLength(request.Prompt)} systemChars={SafeLength(systemPrompt)}");
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
            LogError($"error provider=openai model={model} status={response.StatusCode} body={responseText}");
            throw new InvalidOperationException($"OpenAI 请求失败: {response.StatusCode} {responseText}");
        }

        using var doc = JsonDocument.Parse(responseText);
        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            return new LlmChatResult(string.Empty);
        }

        var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        LogInfo($"response provider=openai model={model} status=ok");
        LogResponse("openai", model, content);
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
        LogInfo($"request provider=gemini model={model} useInternet={request.UseInternet} promptChars={SafeLength(prompt)} systemChars={SafeLength(systemPrompt)}");
        LogPrompt("gemini", model, prompt, systemPrompt);

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
            LogTools(payload["tools"], "gemini", model);
        }

        message.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode && request.UseInternet && responseText.Contains("tool_type", StringComparison.OrdinalIgnoreCase))
        {
            LogInfo($"retry provider=gemini model={model} reason=tool_type");
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
                LogError($"error provider=gemini model={model} status={retryResponse.StatusCode} body={responseText}");
                throw new InvalidOperationException($"Gemini 联网请求失败: {retryResponse.StatusCode} {responseText}");
            }
        }
        else if (!response.IsSuccessStatusCode)
        {
            LogError($"error provider=gemini model={model} status={response.StatusCode} body={responseText}");
            throw new InvalidOperationException($"Gemini 联网请求失败: {response.StatusCode} {responseText}");
        }

        using var doc = JsonDocument.Parse(responseText);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            return new LlmChatResult(string.Empty);
        }

        var contentNode = candidates[0].GetProperty("content");
        if (!contentNode.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
        {
            return new LlmChatResult(string.Empty);
        }

        var text = parts[0].GetProperty("text").GetString() ?? string.Empty;
        LogInfo($"response provider=gemini model={model} status=ok");
        LogResponse("gemini", model, text);
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
        LogInfo($"request-stream provider=openai model={model} useInternet={request.UseInternet} baseUrl={baseUrl} promptChars={SafeLength(request.Prompt)} systemChars={SafeLength(streamSystemPrompt)}");
        LogPrompt("openai-stream", model, request.Prompt, streamSystemPrompt);

        if (request.UseInternet && ShouldUseGeminiInternet(baseUrl, model))
        {
            LogInfo($"route=gemini-stream provider=openai model={model} promptChars={SafeLength(request.Prompt)} systemChars={SafeLength(streamSystemPrompt)}");
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
        LogInfo($"request provider=gemini-stream model={model} useInternet={request.UseInternet} promptChars={SafeLength(prompt)} systemChars={SafeLength(systemPrompt)}");
        LogPrompt("gemini-stream", model, prompt, systemPrompt);

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
            LogTools(payload["tools"], "gemini-stream", model);
        }

        message.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (request.UseInternet && errorText.Contains("tool_type", StringComparison.OrdinalIgnoreCase))
            {
                LogInfo($"retry provider=gemini-stream model={model} reason=tool_type");
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
                    LogError($"error provider=gemini-stream model={model} status={retryResponse.StatusCode} body={retryError}");
                    throw new InvalidOperationException($"Gemini 联网流式请求失败: {retryResponse.StatusCode} {retryError}");
                }

                await foreach (var chunk in StreamGeminiResponseAsync(retryResponse, cancellationToken))
                {
                    yield return chunk;
                }
                yield break;
            }

            LogError($"error provider=gemini-stream model={model} status={response.StatusCode} body={errorText}");
            throw new InvalidOperationException($"Gemini 联网流式请求失败: {response.StatusCode} {errorText}");
        }
        await foreach (var chunk in StreamGeminiResponseAsync(response, cancellationToken))
        {
            yield return chunk;
        }
    }

    private static List<string> ExtractGeminiChunks(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            return new List<string>();
        }

        var content = candidates[0].GetProperty("content");
        if (!content.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
        {
            return new List<string>();
        }

        var result = new List<string>();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textNode))
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

    private void LogInfo(string message)
    {
        _fileLogWriter.Write("LLM", message);
    }

    private void LogError(string message)
    {
        _fileLogWriter.Write("LLM", message);
    }

    private void LogTools(object? tools, string provider, string model)
    {
        if (tools is null)
        {
            return;
        }

        try
        {
            var text = JsonSerializer.Serialize(tools);
            _fileLogWriter.Write("LLM", $"tools provider={provider} model={model} payload={text}");
        }
        catch (Exception ex)
        {
            _fileLogWriter.Write("LLM", $"tools provider={provider} model={model} error={ex.Message}");
        }
    }

    private static int SafeLength(string? value)
    {
        return string.IsNullOrEmpty(value) ? 0 : value.Length;
    }

    private void LogPrompt(string provider, string model, string? prompt, string? systemPrompt)
    {
        _fileLogWriter.Write("LLM", $"prompt provider={provider} model={model} systemPrompt={systemPrompt ?? string.Empty}");
        _fileLogWriter.Write("LLM", $"prompt provider={provider} model={model} userPrompt={prompt ?? string.Empty}");
    }

    private void LogResponse(string provider, string model, string? content)
    {
        _fileLogWriter.Write("LLM", $"response provider={provider} model={model} content={content ?? string.Empty}");
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
