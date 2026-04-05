using System.Net.Http.Json;
using System.Text.Json;
using SimplerJiangAiAgent.Api.Infrastructure.Logging;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend.WebSearch;

namespace SimplerJiangAiAgent.Api.Infrastructure.Llm;

public sealed class OllamaProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly IFileLogWriter _fileLogWriter;
    private readonly TavilySearchClient? _tavilyClient;

    public OllamaProvider(HttpClient httpClient, IFileLogWriter fileLogWriter, TavilySearchClient? tavilyClient = null)
    {
        _httpClient = httpClient;
        _fileLogWriter = fileLogWriter;
        _tavilyClient = tavilyClient;
    }

    public string Name => "ollama";

    public async Task<LlmChatResult> ChatAsync(LlmProviderSettings settings, LlmChatRequest request, CancellationToken cancellationToken = default)
    {
        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "http://localhost:11434"
            : settings.BaseUrl.TrimEnd('/');

        var model = string.IsNullOrWhiteSpace(request.Model) ? settings.Model : request.Model;
        if (string.IsNullOrWhiteSpace(model))
        {
            model = "gemma4:e4b";
        }

        // --- Internet search via Tavily ---
        string? searchContext = null;
        if (request.UseInternet && _tavilyClient is not null)
        {
            try
            {
                LogInfo($"internet-search provider=ollama query={SafeLength(request.Prompt)} chars", request.TraceId);
                var searchResults = await _tavilyClient.SearchAsync(
                    request.Prompt,
                    SearchType.Web,
                    new WebSearchOptions { MaxResults = 5 },
                    cancellationToken);

                if (searchResults.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("以下是来自互联网的最新搜索结果，请参考这些信息回答用户问题：");
                    sb.AppendLine();
                    foreach (var item in searchResults)
                    {
                        sb.AppendLine($"### {item.Title}");
                        if (!string.IsNullOrWhiteSpace(item.Source))
                            sb.AppendLine($"来源: {item.Source}");
                        if (item.PublishedAt.HasValue)
                            sb.AppendLine($"日期: {item.PublishedAt.Value:yyyy-MM-dd}");
                        sb.AppendLine(item.Snippet);
                        sb.AppendLine();
                    }
                    searchContext = sb.ToString();
                    LogInfo($"internet-search provider=ollama results={searchResults.Count}", request.TraceId);
                }
            }
            catch (Exception ex)
            {
                LogError($"internet-search failed provider=ollama error={ex.Message}", request.TraceId);
            }
        }

        var systemPrompt = BuildSystemPrompt(settings.SystemPrompt, settings.ForceChinese);
        if (!string.IsNullOrWhiteSpace(searchContext))
        {
            systemPrompt = string.IsNullOrWhiteSpace(systemPrompt)
                ? searchContext
                : systemPrompt + "\n\n" + searchContext;
        }

        LogInfo($"request provider=ollama model={model} baseUrl={baseUrl} promptChars={SafeLength(request.Prompt)} systemChars={SafeLength(systemPrompt)}", request.TraceId);
        LogPrompt("ollama", model, request.Prompt, systemPrompt, request.TraceId);

        var url = $"{baseUrl}/v1/chat/completions";

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
            temperature = request.Temperature ?? 0.3,
            stream = false
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = JsonContent.Create(payload);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            var reason = ex.InnerException?.Message ?? ex.Message;
            LogError($"error provider=ollama model={model} stage=send uri={url} message={ex.Message} inner={reason}", request.TraceId);
            throw new InvalidOperationException($"Ollama 服务未运行或不可达: {reason}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogError($"error provider=ollama model={model} stage=send uri={url} type=Timeout message={ex.Message}", request.TraceId);
            throw new InvalidOperationException($"Ollama 请求超时，请检查本地服务状态。uri={url}", ex);
        }

        using (response)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogError($"error provider=ollama model={model} status={response.StatusCode} body={responseText}", request.TraceId);
                throw new InvalidOperationException($"Ollama 请求失败: {response.StatusCode} {responseText}");
            }

            using var doc = JsonDocument.Parse(responseText);
            if (!doc.RootElement.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
            {
                return new LlmChatResult(string.Empty);
            }

            var firstChoice = choices[0];
            if (!firstChoice.TryGetProperty("message", out var message)
                || message.ValueKind != JsonValueKind.Object
                || !message.TryGetProperty("content", out var contentEl))
            {
                return new LlmChatResult(string.Empty);
            }

            var content = contentEl.ValueKind == JsonValueKind.String
                ? contentEl.GetString() ?? string.Empty
                : string.Empty;
            LogInfo($"response provider=ollama model={model} status=ok", request.TraceId);
            LogResponse("ollama", model, content, request.TraceId);
            return new LlmChatResult(content.Trim());
        }
    }

    private static string BuildSystemPrompt(string? systemPrompt, bool forceChinese)
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

    private void LogInfo(string message, string? traceId = null)
    {
        _fileLogWriter.Write("LLM-AUDIT", PrefixTraceId(traceId, message));
    }

    private void LogError(string message, string? traceId = null)
    {
        _fileLogWriter.Write("LLM-AUDIT", PrefixTraceId(traceId, message));
    }

    private void LogPrompt(string provider, string model, string? prompt, string? systemPrompt, string? traceId = null)
    {
        _fileLogWriter.Write("LLM-AUDIT", PrefixTraceId(traceId, $"prompt provider={provider} model={model} systemPrompt={systemPrompt ?? string.Empty}"));
        _fileLogWriter.Write("LLM-AUDIT", PrefixTraceId(traceId, $"prompt provider={provider} model={model} userPrompt={prompt ?? string.Empty}"));
    }

    private void LogResponse(string provider, string model, string? content, string? traceId = null)
    {
        _fileLogWriter.Write("LLM-AUDIT", PrefixTraceId(traceId, $"response provider={provider} model={model} content={content ?? string.Empty}"));
    }

    private static string PrefixTraceId(string? traceId, string message)
    {
        if (string.IsNullOrWhiteSpace(traceId) || message.Contains("traceId=", StringComparison.OrdinalIgnoreCase))
        {
            return message;
        }

        return $"traceId={traceId} {message}";
    }

    private static int SafeLength(string? value)
    {
        return string.IsNullOrEmpty(value) ? 0 : value.Length;
    }
}
