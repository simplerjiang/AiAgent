using System.Text.Json;
using System.Text.Json.Serialization;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;
using SimplerJiangAiAgent.Api.Infrastructure.Logging;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IStockAgentOrchestrator
{
    Task<StockAgentResponseDto> RunAsync(StockAgentRequestDto request, CancellationToken cancellationToken = default);
    Task<StockAgentResultDto> RunSingleAsync(StockAgentSingleRequestDto request, CancellationToken cancellationToken = default);
}

public sealed class StockAgentOrchestrator : IStockAgentOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IStockDataService _dataService;
    private readonly ILlmService _llmService;
    private readonly IFileLogWriter _fileLogWriter;

    public StockAgentOrchestrator(IStockDataService dataService, ILlmService llmService, IFileLogWriter fileLogWriter)
    {
        _dataService = dataService;
        _llmService = llmService;
        _fileLogWriter = fileLogWriter;
    }

    public async Task<StockAgentResponseDto> RunAsync(StockAgentRequestDto request, CancellationToken cancellationToken = default)
    {
        var symbol = request.Symbol?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("symbol 不能为空", nameof(request.Symbol));
        }

        var interval = string.IsNullOrWhiteSpace(request.Interval) ? "day" : request.Interval.Trim();
        var count = Math.Clamp(request.Count ?? 60, 10, 120);

        var context = await BuildContextAsync(symbol, interval, count, request.Source, cancellationToken);
        var quote = context.Quote;

        var subAgents = new[]
        {
            StockAgentKind.StockNews,
            StockAgentKind.SectorNews,
            StockAgentKind.FinancialAnalysis,
            StockAgentKind.TrendAnalysis
        };

        var subTasks = subAgents
            .Select(kind =>
            {
                var contextJson = SerializeContext(kind, context);
                return RunAgentAsync(kind, contextJson, request, Array.Empty<StockAgentResultDto>(), cancellationToken);
            })
            .ToArray();

        var subResults = await Task.WhenAll(subTasks);

        var commanderContextJson = SerializeContext(StockAgentKind.Commander, context);
        var commanderResult = await RunAgentAsync(StockAgentKind.Commander, commanderContextJson, request, subResults, cancellationToken);

        var results = new List<StockAgentResultDto> { commanderResult };
        results.AddRange(subResults);

        return new StockAgentResponseDto(quote.Symbol, quote.Name, quote.Timestamp, results);
    }

    public async Task<StockAgentResultDto> RunSingleAsync(StockAgentSingleRequestDto request, CancellationToken cancellationToken = default)
    {
        var symbol = request.Symbol?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("symbol 不能为空", nameof(request.Symbol));
        }

        var agentId = request.AgentId?.Trim() ?? string.Empty;
        if (!StockAgentCatalog.TryGetKind(agentId, out var kind))
        {
            throw new ArgumentException("agentId 无效", nameof(request.AgentId));
        }

        var interval = string.IsNullOrWhiteSpace(request.Interval) ? "day" : request.Interval.Trim();
        var count = Math.Clamp(request.Count ?? 60, 10, 120);

        var context = await BuildContextAsync(symbol, interval, count, request.Source, cancellationToken);
        var contextJson = SerializeContext(kind, context);
        var dependencies = request.DependencyResults ?? Array.Empty<StockAgentResultDto>();

        return await RunAgentAsync(kind, contextJson, new StockAgentRequestDto(
            symbol,
            request.Source,
            request.Provider,
            request.Model,
            request.Interval,
            request.Count,
            request.UseInternet), dependencies, cancellationToken);
    }

    private async Task<StockAgentContextDto> BuildContextAsync(
        string symbol,
        string interval,
        int count,
        string? source,
        CancellationToken cancellationToken)
    {
        var quote = await _dataService.GetQuoteAsync(symbol, source);
        var kLines = await _dataService.GetKLineAsync(symbol, interval, count, source);
        var minuteLines = await _dataService.GetMinuteLineAsync(symbol, source);
        var messages = await _dataService.GetIntradayMessagesAsync(symbol, source);

        return new StockAgentContextDto(
            quote,
            kLines.OrderBy(item => item.Date).TakeLast(60).ToArray(),
            minuteLines.OrderBy(item => item.Date).ThenBy(item => item.Time).TakeLast(120).ToArray(),
            messages.OrderByDescending(item => item.PublishedAt).Take(20).ToArray(),
            DateTime.Now);
    }

    private async Task<StockAgentResultDto> RunAgentAsync(
        StockAgentKind kind,
        string contextJson,
        StockAgentRequestDto request,
        IReadOnlyList<StockAgentResultDto> dependencyResults,
        CancellationToken cancellationToken)
    {
        var definition = StockAgentCatalog.GetDefinition(kind);
        var prompt = StockAgentPromptBuilder.BuildPrompt(kind, contextJson, dependencyResults);
        var provider = string.IsNullOrWhiteSpace(request.Provider) ? "openai" : request.Provider.Trim();

        try
        {
            var result = await _llmService.ChatAsync(
                provider,
                new LlmChatRequest(prompt, request.Model, 0.4, request.UseInternet),
                cancellationToken);

            var raw = result.Content?.Trim() ?? string.Empty;
            if (!StockAgentJsonParser.TryParse(raw, out var data, out var parseError))
            {
                _fileLogWriter.Write("LLM", $"parse_error agent={definition.Id} message={parseError} raw={raw}");

                var currentRaw = raw;
                for (var attempt = 1; attempt <= 2; attempt++)
                {
                    var repairPrompt = StockAgentPromptBuilder.BuildRepairPrompt(kind, currentRaw);
                    var repair = await _llmService.ChatAsync(
                        provider,
                        new LlmChatRequest(repairPrompt, request.Model, 0.2, false),
                        cancellationToken);

                    var repairRaw = repair.Content?.Trim() ?? string.Empty;
                    if (StockAgentJsonParser.TryParse(repairRaw, out var repairData, out _))
                    {
                        return new StockAgentResultDto(definition.Id, definition.Name, true, null, repairData, repairRaw);
                    }

                    _fileLogWriter.Write("LLM", $"parse_error agent={definition.Id} stage=repair attempt={attempt} raw={repairRaw}");
                    currentRaw = repairRaw;
                }

                return new StockAgentResultDto(definition.Id, definition.Name, false, parseError, null, currentRaw);
            }

            return new StockAgentResultDto(definition.Id, definition.Name, true, null, data, raw);
        }
        catch (Exception ex)
        {
            return new StockAgentResultDto(definition.Id, definition.Name, false, ex.Message, null, null);
        }
    }

    private sealed record StockAgentContextDto(
        StockQuoteDto Quote,
        IReadOnlyList<KLinePointDto> KLines,
        IReadOnlyList<MinuteLinePointDto> MinuteLines,
        IReadOnlyList<IntradayMessageDto> Messages,
        DateTime RequestTime
    );

    private static string SerializeContext(StockAgentKind kind, StockAgentContextDto context)
    {
        if (kind == StockAgentKind.TrendAnalysis)
        {
            return JsonSerializer.Serialize(context, JsonOptions);
        }

        var slimContext = new StockAgentSlimContextDto(
            context.Quote,
            context.Messages,
            context.RequestTime);

        return JsonSerializer.Serialize(slimContext, JsonOptions);
    }

    private sealed record StockAgentSlimContextDto(
        StockQuoteDto Quote,
        IReadOnlyList<IntradayMessageDto> Messages,
        DateTime RequestTime
    );
}

internal enum StockAgentKind
{
    Commander,
    StockNews,
    SectorNews,
    FinancialAnalysis,
    TrendAnalysis
}

internal sealed record StockAgentDefinition(string Id, string Name);

internal static class StockAgentCatalog
{
    private static readonly IReadOnlyDictionary<StockAgentKind, StockAgentDefinition> Definitions =
        new Dictionary<StockAgentKind, StockAgentDefinition>
        {
            [StockAgentKind.Commander] = new("commander", "指挥Agent"),
            [StockAgentKind.StockNews] = new("stock_news", "个股资讯Agent"),
            [StockAgentKind.SectorNews] = new("sector_news", "板块资讯Agent"),
            [StockAgentKind.FinancialAnalysis] = new("financial_analysis", "个股分析Agent"),
            [StockAgentKind.TrendAnalysis] = new("trend_analysis", "走势分析Agent")
        };

    public static StockAgentDefinition GetDefinition(StockAgentKind kind)
    {
        return Definitions[kind];
    }

    public static bool TryGetKind(string? agentId, out StockAgentKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return false;
        }

        foreach (var entry in Definitions)
        {
            if (string.Equals(entry.Value.Id, agentId, StringComparison.OrdinalIgnoreCase))
            {
                kind = entry.Key;
                return true;
            }
        }

        return false;
    }
}

internal static class StockAgentPromptBuilder
{
    public static string BuildPrompt(
        StockAgentKind kind,
        string contextJson,
        IReadOnlyList<StockAgentResultDto> dependencyResults)
    {
        return kind switch
        {
            StockAgentKind.Commander => BuildCommanderPrompt(contextJson, dependencyResults),
            StockAgentKind.StockNews => BuildStockNewsPrompt(contextJson),
            StockAgentKind.SectorNews => BuildSectorNewsPrompt(contextJson),
            StockAgentKind.FinancialAnalysis => BuildFinancialPrompt(contextJson),
            StockAgentKind.TrendAnalysis => BuildTrendPrompt(contextJson),
            _ => BuildStockNewsPrompt(contextJson)
        };
    }

    private static string BuildCommanderPrompt(string contextJson, IReadOnlyList<StockAgentResultDto> dependencyResults)
    {
        var agentInputs = dependencyResults.Select(item => new
        {
            item.AgentId,
            item.AgentName,
            item.Success,
            item.Error,
            Data = item.Data
        });
        var agentsJson = JsonSerializer.Serialize(agentInputs, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        const string template =
            "你是指挥Agent。根据给定的股票上下文、以及其他Agent的输出，产出最终评估。\n" +
            "要求：\n" +
            "1. 必须输出严格JSON，不要Markdown，不要代码块，不要多余文字。\n" +
            "2. 所有字段必须存在；没有数据用null或空数组。\n" +
            "3. 百分比字段用数值，不带%符号。\n" +
            "4. 评估必须基于其他Agent输出汇总后再给出分数与结论。\n\n" +
            "输出JSON结构：\n" +
            "{\n" +
            "  \"agent\": \"commander\",\n" +
            "  \"summary\": \"string\",\n" +
            "  \"metrics\": {\n" +
            "    \"price\": number,\n" +
            "    \"changePercent\": number,\n" +
            "    \"turnoverRate\": number,\n" +
            "    \"innerVolume\": number|null,\n" +
            "    \"outerVolume\": number|null,\n" +
            "    \"sector\": \"string|null\",\n" +
            "    \"date\": \"YYYY-MM-DD\"\n" +
            "  },\n" +
            "  \"recommendation\": {\n" +
            "    \"entryScore\": number,\n" +
            "    \"valuationScore\": number,\n" +
            "    \"confidence\": number,\n" +
            "    \"rating\": \"string\"\n" +
            "  },\n" +
            "  \"reasons\": [\"string\"],\n" +
            "  \"risks\": [\"string\"],\n" +
            "  \"chart\": null\n" +
            "}\n\n" +
            "股票上下文JSON：\n";

        return string.Concat(template, contextJson, "\n\n其他Agent输出JSON：\n", agentsJson);
    }

    private static string BuildStockNewsPrompt(string contextJson)
    {
        const string template =
            "你是个股资讯Agent。请联网获取当前及近期该股票的重要消息，并做情绪统计。\n" +
            "要求：\n" +
            "1. 必须输出严格JSON，不要Markdown，不要代码块，不要多余文字。\n" +
            "2. 所有字段必须存在；没有数据用null或空数组。\n" +
            "3. 百分比字段用数值，不带%符号。\n\n" +
            "输出JSON结构：\n" +
            "{\n" +
            "  \"agent\": \"stock_news\",\n" +
            "  \"summary\": \"string\",\n" +
            "  \"sentiment\": {\n" +
            "    \"positive\": number,\n" +
            "    \"neutral\": number,\n" +
            "    \"negative\": number,\n" +
            "    \"overall\": \"string\"\n" +
            "  },\n" +
            "  \"events\": [\n" +
            "    {\n" +
            "      \"title\": \"string\",\n" +
            "      \"category\": \"利好|中性|利空\",\n" +
            "      \"publishedAt\": \"YYYY-MM-DD HH:mm\",\n" +
            "      \"source\": \"string\",\n" +
            "      \"impact\": number,\n" +
            "      \"url\": \"string|null\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"signals\": [\"string\"],\n" +
            "  \"risks\": [\"string\"],\n" +
            "  \"chart\": null\n" +
            "}\n\n" +
            "股票上下文JSON：\n";

        return string.Concat(template, contextJson);
    }

    private static string BuildSectorNewsPrompt(string contextJson)
    {
        const string template =
            "你是板块资讯Agent。请联网获取该股票所属板块的最新资讯和同板块个股涨跌。\n" +
            "要求：\n" +
            "1. 必须输出严格JSON，不要Markdown，不要代码块，不要多余文字。\n" +
            "2. 所有字段必须存在；没有数据用null或空数组。\n" +
            "3. 百分比字段用数值，不带%符号。\n\n" +
            "输出JSON结构：\n" +
            "{\n" +
            "  \"agent\": \"sector_news\",\n" +
            "  \"sector\": \"string\",\n" +
            "  \"summary\": \"string\",\n" +
            "  \"sectorChangePercent\": number,\n" +
            "  \"topMovers\": [\n" +
            "    {\n" +
            "      \"symbol\": \"string\",\n" +
            "      \"name\": \"string\",\n" +
            "      \"changePercent\": number,\n" +
            "      \"reason\": \"string\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"signals\": [\"string\"],\n" +
            "  \"risks\": [\"string\"],\n" +
            "  \"chart\": null\n" +
            "}\n\n" +
            "股票上下文JSON：\n";

        return string.Concat(template, contextJson);
    }

    private static string BuildFinancialPrompt(string contextJson)
    {
        const string template =
            "你是个股分析Agent。请联网获取近期财报并重点分析扣非利润、机构持仓、估值。\n" +
            "要求：\n" +
            "1. 必须输出严格JSON，不要Markdown，不要代码块，不要多余文字。\n" +
            "2. 所有字段必须存在；没有数据用null或空数组。\n" +
            "3. 百分比字段用数值，不带%符号。\n\n" +
            "输出JSON结构：\n" +
            "{\n" +
            "  \"agent\": \"financial_analysis\",\n" +
            "  \"summary\": \"string\",\n" +
            "  \"metrics\": {\n" +
            "    \"revenue\": number|null,\n" +
            "    \"revenueYoY\": number|null,\n" +
            "    \"netProfit\": number|null,\n" +
            "    \"netProfitYoY\": number|null,\n" +
            "    \"nonRecurringProfit\": number|null,\n" +
            "    \"institutionHoldingPercent\": number|null,\n" +
            "    \"institutionTargetPrice\": number|null\n" +
            "  },\n" +
            "  \"highlights\": [\"string\"],\n" +
            "  \"risks\": [\"string\"],\n" +
            "  \"chart\": null\n" +
            "}\n\n" +
            "股票上下文JSON：\n";

        return string.Concat(template, contextJson);
    }

    private static string BuildTrendPrompt(string contextJson)
    {
        const string template =
            "你是股票走势分析Agent。基于日K、分时、成交量数据分析未来走势。\n" +
            "要求：\n" +
            "1. 必须输出严格JSON，不要Markdown，不要代码块，不要多余文字。\n" +
            "2. 所有字段必须存在；没有数据用null或空数组。\n" +
            "3. 百分比字段用数值，不带%符号。\n\n" +
            "输出JSON结构：\n" +
            "{\n" +
            "  \"agent\": \"trend_analysis\",\n" +
            "  \"summary\": \"string\",\n" +
            "  \"timeframeSignals\": [\n" +
            "    {\n" +
            "      \"timeframe\": \"1D|1W|1M\",\n" +
            "      \"trend\": \"上涨|震荡|下跌\",\n" +
            "      \"confidence\": number\n" +
            "    }\n" +
            "  ],\n" +
            "  \"forecast\": [\n" +
            "    {\n" +
            "      \"label\": \"T+1\",\n" +
            "      \"price\": number,\n" +
            "      \"confidence\": number\n" +
            "    }\n" +
            "  ],\n" +
            "  \"signals\": [\"string\"],\n" +
            "  \"risks\": [\"string\"],\n" +
            "  \"chart\": {\n" +
            "    \"type\": \"line\",\n" +
            "    \"title\": \"未来价格走势\",\n" +
            "    \"labels\": [\"string\"],\n" +
            "    \"values\": [number]\n" +
            "  }\n" +
            "}\n\n" +
            "股票上下文JSON：\n";

        return string.Concat(template, contextJson);
    }

    public static string BuildRepairPrompt(StockAgentKind kind, string raw)
    {
        var schema = GetSchemaTemplate(kind);
        return
            "你刚才的输出不是严格JSON。请只输出一个JSON对象，不要任何解释、Markdown或代码块。\n" +
            "必须严格符合以下JSON结构，字段必须完整，没有数据用null或空数组。\n\n" +
            "JSON结构：\n" + schema + "\n\n" +
            "原始输出：\n" + raw;
    }

    private static string GetSchemaTemplate(StockAgentKind kind)
    {
        return kind switch
        {
            StockAgentKind.Commander =>
                "{\n" +
                "  \"agent\": \"commander\",\n" +
                "  \"summary\": \"string\",\n" +
                "  \"metrics\": {\n" +
                "    \"price\": number,\n" +
                "    \"changePercent\": number,\n" +
                "    \"turnoverRate\": number,\n" +
                "    \"innerVolume\": number|null,\n" +
                "    \"outerVolume\": number|null,\n" +
                "    \"sector\": \"string|null\",\n" +
                "    \"date\": \"YYYY-MM-DD\"\n" +
                "  },\n" +
                "  \"recommendation\": {\n" +
                "    \"entryScore\": number,\n" +
                "    \"valuationScore\": number,\n" +
                "    \"confidence\": number,\n" +
                "    \"rating\": \"string\"\n" +
                "  },\n" +
                "  \"reasons\": [\"string\"],\n" +
                "  \"risks\": [\"string\"],\n" +
                "  \"chart\": null\n" +
                "}",
            StockAgentKind.StockNews =>
                "{\n" +
                "  \"agent\": \"stock_news\",\n" +
                "  \"summary\": \"string\",\n" +
                "  \"sentiment\": {\n" +
                "    \"positive\": number,\n" +
                "    \"neutral\": number,\n" +
                "    \"negative\": number,\n" +
                "    \"overall\": \"string\"\n" +
                "  },\n" +
                "  \"events\": [\n" +
                "    {\n" +
                "      \"title\": \"string\",\n" +
                "      \"category\": \"利好|中性|利空\",\n" +
                "      \"publishedAt\": \"YYYY-MM-DD HH:mm\",\n" +
                "      \"source\": \"string\",\n" +
                "      \"impact\": number,\n" +
                "      \"url\": \"string|null\"\n" +
                "    }\n" +
                "  ],\n" +
                "  \"signals\": [\"string\"],\n" +
                "  \"risks\": [\"string\"],\n" +
                "  \"chart\": null\n" +
                "}",
            StockAgentKind.SectorNews =>
                "{\n" +
                "  \"agent\": \"sector_news\",\n" +
                "  \"sector\": \"string\",\n" +
                "  \"summary\": \"string\",\n" +
                "  \"sectorChangePercent\": number,\n" +
                "  \"topMovers\": [\n" +
                "    {\n" +
                "      \"symbol\": \"string\",\n" +
                "      \"name\": \"string\",\n" +
                "      \"changePercent\": number,\n" +
                "      \"reason\": \"string\"\n" +
                "    }\n" +
                "  ],\n" +
                "  \"signals\": [\"string\"],\n" +
                "  \"risks\": [\"string\"],\n" +
                "  \"chart\": null\n" +
                "}",
            StockAgentKind.FinancialAnalysis =>
                "{\n" +
                "  \"agent\": \"financial_analysis\",\n" +
                "  \"summary\": \"string\",\n" +
                "  \"metrics\": {\n" +
                "    \"revenue\": number|null,\n" +
                "    \"revenueYoY\": number|null,\n" +
                "    \"netProfit\": number|null,\n" +
                "    \"netProfitYoY\": number|null,\n" +
                "    \"nonRecurringProfit\": number|null,\n" +
                "    \"institutionHoldingPercent\": number|null,\n" +
                "    \"institutionTargetPrice\": number|null\n" +
                "  },\n" +
                "  \"highlights\": [\"string\"],\n" +
                "  \"risks\": [\"string\"],\n" +
                "  \"chart\": null\n" +
                "}",
            StockAgentKind.TrendAnalysis =>
                "{\n" +
                "  \"agent\": \"trend_analysis\",\n" +
                "  \"summary\": \"string\",\n" +
                "  \"timeframeSignals\": [\n" +
                "    {\n" +
                "      \"timeframe\": \"1D|1W|1M\",\n" +
                "      \"trend\": \"上涨|震荡|下跌\",\n" +
                "      \"confidence\": number\n" +
                "    }\n" +
                "  ],\n" +
                "  \"forecast\": [\n" +
                "    {\n" +
                "      \"label\": \"T+1\",\n" +
                "      \"price\": number,\n" +
                "      \"confidence\": number\n" +
                "    }\n" +
                "  ],\n" +
                "  \"signals\": [\"string\"],\n" +
                "  \"risks\": [\"string\"],\n" +
                "  \"chart\": {\n" +
                "    \"type\": \"line\",\n" +
                "    \"title\": \"未来价格走势\",\n" +
                "    \"labels\": [\"string\"],\n" +
                "    \"values\": [number]\n" +
                "  }\n" +
                "}",
            _ => "{}"
        };
    }
}

internal static class StockAgentJsonParser
{
    public static bool TryParse(string? content, out JsonElement? data, out string? error)
    {
        data = null;
        error = null;

        if (string.IsNullOrWhiteSpace(content))
        {
            error = "LLM 返回为空";
            return false;
        }

        var jsonText = ExtractJson(content);
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            error = "未找到JSON内容";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "JSON根节点不是对象";
                return false;
            }
            data = doc.RootElement.Clone();
            return true;
        }
        catch (Exception ex)
        {
            error = $"JSON解析失败: {ex.Message}";
            return false;
        }
    }

    internal static string? ExtractJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            var fenceIndex = trimmed.IndexOf('\n');
            if (fenceIndex >= 0)
            {
                trimmed = trimmed[(fenceIndex + 1)..];
            }
            var endFence = trimmed.LastIndexOf("```", StringComparison.OrdinalIgnoreCase);
            if (endFence >= 0)
            {
                trimmed = trimmed[..endFence];
            }
            trimmed = trimmed.Trim();
        }

        var start = trimmed.IndexOf('{');
        if (start < 0)
        {
            return null;
        }

        var depth = 0;
        var inString = false;
        var escape = false;
        var end = -1;

        for (var i = start; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];
            if (inString)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escape = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{')
            {
                depth++;
                continue;
            }

            if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    end = i;
                    break;
                }
            }
        }

        if (end < 0 || end <= start)
        {
            return null;
        }

        return trimmed[start..(end + 1)];
    }
}
