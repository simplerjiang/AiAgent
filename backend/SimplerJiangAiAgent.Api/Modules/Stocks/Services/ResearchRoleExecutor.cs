using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed record RoleExecutionContext(
    long SessionId, long TurnId, long StageId,
    string Symbol, string RoleId, string UserPrompt,
    IReadOnlyList<string> UpstreamArtifacts);

public sealed record RoleExecutionResult(
    string RoleId,
    ResearchRoleStatus Status,
    string? OutputContentJson,
    string? LlmTraceId,
    IReadOnlyList<string> DegradedFlags,
    string? ErrorCode,
    string? ErrorMessage);

public interface IResearchRoleExecutor
{
    Task<RoleExecutionResult> ExecuteRoleAsync(RoleExecutionContext context, CancellationToken cancellationToken = default);
}

public sealed class ResearchRoleExecutor : IResearchRoleExecutor
{
    private readonly IMcpToolGateway _mcpGateway;
    private readonly IRoleToolPolicyService _policyService;
    private readonly IStockAgentRoleContractRegistry _contractRegistry;
    private readonly ILlmService _llmService;
    private readonly IResearchEventBus _eventBus;
    private readonly ILogger<ResearchRoleExecutor> _logger;

    public ResearchRoleExecutor(
        IMcpToolGateway mcpGateway,
        IRoleToolPolicyService policyService,
        IStockAgentRoleContractRegistry contractRegistry,
        ILlmService llmService,
        IResearchEventBus eventBus,
        ILogger<ResearchRoleExecutor> logger)
    {
        _mcpGateway = mcpGateway;
        _policyService = policyService;
        _contractRegistry = contractRegistry;
        _llmService = llmService;
        _eventBus = eventBus;
        _logger = logger;
    }

    private const int MaxLlmRetries = 2;
    private const int MaxToolRetries = 1;
    private static readonly int[] LlmRetryDelaysMs = [2000, 5000];
    private static readonly JsonSerializerOptions RelaxedJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<RoleExecutionResult> ExecuteRoleAsync(RoleExecutionContext context, CancellationToken cancellationToken = default)
    {
        var contract = _contractRegistry.GetRequired(context.RoleId);
        var degradedFlags = new List<string>();
        var toolResults = new List<string>();

        _eventBus.Publish(new ResearchEvent(
            ResearchEventType.RoleStarted,
            context.SessionId, context.TurnId, context.StageId,
            context.RoleId, null,
            $"Role {context.RoleId} started", null, DateTime.UtcNow));

        // Phase 1: Dispatch MCP tools if the role has direct query tools
        if (contract.AllowsDirectQueryTools && contract.PreferredMcpSequence.Count > 0)
        {
            var isLocalRequired = string.Equals(contract.ToolAccessMode, "local_required", StringComparison.OrdinalIgnoreCase);

            foreach (var toolName in contract.PreferredMcpSequence)
            {
                var auth = _policyService.AuthorizeRole(context.RoleId, toolName);
                if (!auth.IsAllowed)
                {
                    if (isLocalRequired)
                    {
                        return new RoleExecutionResult(context.RoleId, ResearchRoleStatus.Failed,
                            null, null, [$"tool_blocked:{toolName}"], "TOOL_BLOCKED",
                            $"Required tool {toolName} blocked: {auth.Reason}");
                    }
                    degradedFlags.Add($"tool_unavailable:{toolName}");
                    continue;
                }

                _eventBus.Publish(new ResearchEvent(
                    ResearchEventType.ToolDispatched,
                    context.SessionId, context.TurnId, context.StageId,
                    context.RoleId, null,
                    $"Dispatching {toolName}", null, DateTime.UtcNow));

                var toolSuccess = false;
                for (var attempt = 0; attempt <= MaxToolRetries; attempt++)
                {
                    try
                    {
                        if (attempt > 0)
                        {
                            _eventBus.Publish(new ResearchEvent(
                                ResearchEventType.RetryAttempt,
                                context.SessionId, context.TurnId, context.StageId,
                                context.RoleId, null,
                                $"Retrying {toolName} (attempt {attempt + 1})", null, DateTime.UtcNow));
                            await Task.Delay(2000, cancellationToken);
                        }

                        var toolResult = await DispatchToolAsync(toolName, context.Symbol, cancellationToken);
                        toolResults.Add($"[{toolName}]\n{toolResult}");

                        _eventBus.Publish(new ResearchEvent(
                            ResearchEventType.ToolCompleted,
                            context.SessionId, context.TurnId, context.StageId,
                            context.RoleId, null,
                            $"{toolName} completed", null, DateTime.UtcNow));
                        toolSuccess = true;
                        break;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) when (attempt < MaxToolRetries)
                    {
                        _logger.LogWarning(ex, "Role {RoleId}: tool {Tool} failed (attempt {Attempt}), will retry",
                            context.RoleId, toolName, attempt + 1);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Role {RoleId}: tool {Tool} failed after {MaxAttempts} attempts",
                            context.RoleId, toolName, MaxToolRetries + 1);
                    }
                }

                if (!toolSuccess)
                {
                    degradedFlags.Add($"tool_error:{toolName}");
                    _eventBus.Publish(new ResearchEvent(
                        ResearchEventType.ToolCompleted,
                        context.SessionId, context.TurnId, context.StageId,
                        context.RoleId, null,
                        $"{toolName} failed after retries", null, DateTime.UtcNow));
                }
            }
        }

        if (toolResults.Count < contract.MinimumEvidenceCount && contract.AllowsDirectQueryTools)
        {
            degradedFlags.Add($"insufficient_evidence:{toolResults.Count}/{contract.MinimumEvidenceCount}");
        }

        // Phase 2: Build prompt and call LLM (with retry)
        var systemPrompt = TradingWorkbenchPromptTemplates.GetSystemPrompt(context.RoleId);
        var userContent = BuildUserContent(context, toolResults);
        var traceId = $"research-{context.SessionId}-{context.TurnId}-{context.RoleId}";
        Exception? lastLlmException = null;

        for (var attempt = 0; attempt <= MaxLlmRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    var delayMs = LlmRetryDelaysMs[Math.Min(attempt - 1, LlmRetryDelaysMs.Length - 1)];
                    _eventBus.Publish(new ResearchEvent(
                        ResearchEventType.RetryAttempt,
                        context.SessionId, context.TurnId, context.StageId,
                        context.RoleId, null,
                        $"LLM retry attempt {attempt + 1}/{MaxLlmRetries + 1} (waiting {delayMs}ms)",
                        null, DateTime.UtcNow));
                    await Task.Delay(delayMs, cancellationToken);
                }

                var llmResult = await _llmService.ChatAsync("active",
                    new LlmChatRequest($"{systemPrompt}\n\n{userContent}", null, 0.3, false, traceId),
                    cancellationToken);

                var feedSummary = ExtractFeedSummary(llmResult.Content);

                _eventBus.Publish(new ResearchEvent(
                    ResearchEventType.RoleSummaryReady,
                    context.SessionId, context.TurnId, context.StageId,
                    context.RoleId, llmResult.TraceId,
                    feedSummary, null, DateTime.UtcNow));

                var status = degradedFlags.Count > 0 ? ResearchRoleStatus.Degraded : ResearchRoleStatus.Completed;

                _eventBus.Publish(new ResearchEvent(
                    ResearchEventType.RoleCompleted,
                    context.SessionId, context.TurnId, context.StageId,
                    context.RoleId, llmResult.TraceId,
                    $"Role {context.RoleId} {status}", null, DateTime.UtcNow));

                return new RoleExecutionResult(context.RoleId, status,
                    JsonSerializer.Serialize(new { content = llmResult.Content }, RelaxedJsonOptions),
                    llmResult.TraceId, degradedFlags, null, null);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (attempt < MaxLlmRetries)
            {
                lastLlmException = ex;
                _logger.LogWarning(ex, "Role {RoleId}: LLM attempt {Attempt} failed, will retry",
                    context.RoleId, attempt + 1);
            }
            catch (Exception ex)
            {
                lastLlmException = ex;
            }
        }

        // All LLM retries exhausted
        _logger.LogError(lastLlmException, "Role {RoleId}: LLM failed after {MaxAttempts} attempts",
            context.RoleId, MaxLlmRetries + 1);

        _eventBus.Publish(new ResearchEvent(
            ResearchEventType.RoleFailed,
            context.SessionId, context.TurnId, context.StageId,
            context.RoleId, null,
            $"Role {context.RoleId} LLM failed after {MaxLlmRetries + 1} attempts: {lastLlmException?.Message}",
            null, DateTime.UtcNow));

        return new RoleExecutionResult(context.RoleId, ResearchRoleStatus.Failed,
            null, null, degradedFlags, "LLM_FAILED", lastLlmException?.Message);
    }

    private async Task<string> DispatchToolAsync(string toolName, string symbol, CancellationToken ct)
    {
        return toolName switch
        {
            StockMcpToolNames.CompanyOverview => JsonSerializer.Serialize(await _mcpGateway.GetCompanyOverviewAsync(symbol, null, null, ct), RelaxedJsonOptions),
            StockMcpToolNames.Product => JsonSerializer.Serialize(await _mcpGateway.GetProductAsync(symbol, null, null, ct), RelaxedJsonOptions),
            StockMcpToolNames.Fundamentals => JsonSerializer.Serialize(await _mcpGateway.GetFundamentalsAsync(symbol, null, null, ct), RelaxedJsonOptions),
            StockMcpToolNames.Shareholder => JsonSerializer.Serialize(await _mcpGateway.GetShareholderAsync(symbol, null, null, ct), RelaxedJsonOptions),
            StockMcpToolNames.MarketContext => JsonSerializer.Serialize(await _mcpGateway.GetMarketContextAsync(symbol, null, null, ct), RelaxedJsonOptions),
            StockMcpToolNames.SocialSentiment => JsonSerializer.Serialize(await _mcpGateway.GetSocialSentimentAsync(symbol, null, null, ct), RelaxedJsonOptions),
            StockMcpToolNames.Kline => JsonSerializer.Serialize(await _mcpGateway.GetKlineAsync(symbol, "day", 60, null, null, null, ct), RelaxedJsonOptions),
            StockMcpToolNames.Minute => JsonSerializer.Serialize(await _mcpGateway.GetMinuteAsync(symbol, null, null, null, ct), RelaxedJsonOptions),
            StockMcpToolNames.Strategy => JsonSerializer.Serialize(await _mcpGateway.GetStrategyAsync(symbol, "day", 60, null, null, null, null, ct), RelaxedJsonOptions),
            StockMcpToolNames.News => JsonSerializer.Serialize(await _mcpGateway.GetNewsAsync(symbol, "stock", null, null, ct), RelaxedJsonOptions),
            StockMcpToolNames.Search => JsonSerializer.Serialize(await _mcpGateway.SearchAsync(symbol, true, null, ct), RelaxedJsonOptions),
            _ => throw new ArgumentException($"Unknown tool: {toolName}")
        };
    }

    private static string BuildUserContent(RoleExecutionContext context, List<string> toolResults)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## 目标个股: {context.Symbol}");
        sb.AppendLine($"## 用户意图: {context.UserPrompt}");

        if (context.UpstreamArtifacts.Count > 0)
        {
            sb.AppendLine("\n## 上游角色产出:");
            foreach (var a in context.UpstreamArtifacts)
            {
                sb.AppendLine(a);
                sb.AppendLine("---");
            }
        }

        if (toolResults.Count > 0)
        {
            sb.AppendLine("\n## 本地工具数据:");
            foreach (var r in toolResults)
            {
                sb.AppendLine(r);
                sb.AppendLine("---");
            }
        }

        return sb.ToString();
    }

    /// <summary>Extract a human-readable summary from LLM output for feed display.</summary>
    internal static string ExtractFeedSummary(string? llmContent, int maxLength = 600)
    {
        if (string.IsNullOrWhiteSpace(llmContent)) return "分析完成";

        try
        {
            var jsonStart = llmContent.IndexOf('{');
            var jsonEnd = llmContent.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                using var doc = JsonDocument.Parse(llmContent[jsonStart..(jsonEnd + 1)]);
                var root = doc.RootElement;
                foreach (var field in new[] { "summary", "analysis", "headline", "executive_summary",
                    "rationale", "recommendation", "conclusion", "verdict", "assessment", "claim" })
                {
                    if (root.TryGetProperty(field, out var val) && val.ValueKind == JsonValueKind.String)
                    {
                        var text = val.GetString();
                        if (!string.IsNullOrWhiteSpace(text)) return text;
                    }
                }

                // No named summary field found — try first substantial string property
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var text = prop.Value.GetString();
                        if (text is not null && text.Length > 30) return text;
                    }
                }
            }
        }
        catch { /* JSON parse failed — try plain text */ }

        // Fallback: if it looks like raw JSON, give generic message
        var trimmed = llmContent.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            return "分析完成（详见研究报告）";

        // Plain text/markdown — use as-is, truncated
        return llmContent.Length > maxLength ? llmContent[..maxLength] + "..." : llmContent;
    }
}
