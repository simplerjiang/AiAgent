using System.Text;
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

                try
                {
                    var toolResult = await DispatchToolAsync(toolName, context.Symbol, cancellationToken);
                    toolResults.Add($"[{toolName}]\n{toolResult}");

                    _eventBus.Publish(new ResearchEvent(
                        ResearchEventType.ToolCompleted,
                        context.SessionId, context.TurnId, context.StageId,
                        context.RoleId, null,
                        $"{toolName} completed", null, DateTime.UtcNow));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Role {RoleId}: tool {Tool} failed", context.RoleId, toolName);
                    degradedFlags.Add($"tool_error:{toolName}");

                    _eventBus.Publish(new ResearchEvent(
                        ResearchEventType.ToolCompleted,
                        context.SessionId, context.TurnId, context.StageId,
                        context.RoleId, null,
                        $"{toolName} failed: {ex.Message}", null, DateTime.UtcNow));
                }
            }
        }

        if (toolResults.Count < contract.MinimumEvidenceCount && contract.AllowsDirectQueryTools)
        {
            degradedFlags.Add($"insufficient_evidence:{toolResults.Count}/{contract.MinimumEvidenceCount}");
        }

        // Phase 2: Build prompt and call LLM
        try
        {
            var systemPrompt = TradingWorkbenchPromptTemplates.GetSystemPrompt(context.RoleId);
            var userContent = BuildUserContent(context, toolResults);
            var traceId = $"research-{context.SessionId}-{context.TurnId}-{context.RoleId}";

            var llmResult = await _llmService.ChatAsync("active",
                new LlmChatRequest($"{systemPrompt}\n\n{userContent}", null, 0.3, false, traceId),
                cancellationToken);

            _eventBus.Publish(new ResearchEvent(
                ResearchEventType.RoleSummaryReady,
                context.SessionId, context.TurnId, context.StageId,
                context.RoleId, llmResult.TraceId,
                $"Role {context.RoleId} LLM ready", null, DateTime.UtcNow));

            var status = degradedFlags.Count > 0 ? ResearchRoleStatus.Degraded : ResearchRoleStatus.Completed;

            _eventBus.Publish(new ResearchEvent(
                ResearchEventType.RoleCompleted,
                context.SessionId, context.TurnId, context.StageId,
                context.RoleId, llmResult.TraceId,
                $"Role {context.RoleId} {status}", null, DateTime.UtcNow));

            return new RoleExecutionResult(context.RoleId, status,
                JsonSerializer.Serialize(new { content = llmResult.Content }),
                llmResult.TraceId, degradedFlags, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Role {RoleId}: LLM failed", context.RoleId);

            _eventBus.Publish(new ResearchEvent(
                ResearchEventType.RoleFailed,
                context.SessionId, context.TurnId, context.StageId,
                context.RoleId, null,
                $"Role {context.RoleId} LLM failed: {ex.Message}", null, DateTime.UtcNow));

            return new RoleExecutionResult(context.RoleId, ResearchRoleStatus.Failed,
                null, null, degradedFlags, "LLM_FAILED", ex.Message);
        }
    }

    private async Task<string> DispatchToolAsync(string toolName, string symbol, CancellationToken ct)
    {
        return toolName switch
        {
            StockMcpToolNames.CompanyOverview => JsonSerializer.Serialize(await _mcpGateway.GetCompanyOverviewAsync(symbol, null, null, ct)),
            StockMcpToolNames.Product => JsonSerializer.Serialize(await _mcpGateway.GetProductAsync(symbol, null, null, ct)),
            StockMcpToolNames.Fundamentals => JsonSerializer.Serialize(await _mcpGateway.GetFundamentalsAsync(symbol, null, null, ct)),
            StockMcpToolNames.Shareholder => JsonSerializer.Serialize(await _mcpGateway.GetShareholderAsync(symbol, null, null, ct)),
            StockMcpToolNames.MarketContext => JsonSerializer.Serialize(await _mcpGateway.GetMarketContextAsync(symbol, null, null, ct)),
            StockMcpToolNames.SocialSentiment => JsonSerializer.Serialize(await _mcpGateway.GetSocialSentimentAsync(symbol, null, null, ct)),
            StockMcpToolNames.Kline => JsonSerializer.Serialize(await _mcpGateway.GetKlineAsync(symbol, "day", 60, null, null, null, ct)),
            StockMcpToolNames.Minute => JsonSerializer.Serialize(await _mcpGateway.GetMinuteAsync(symbol, null, null, null, ct)),
            StockMcpToolNames.Strategy => JsonSerializer.Serialize(await _mcpGateway.GetStrategyAsync(symbol, "day", 60, null, null, null, null, ct)),
            StockMcpToolNames.News => JsonSerializer.Serialize(await _mcpGateway.GetNewsAsync(symbol, "stock", null, null, ct)),
            StockMcpToolNames.Search => JsonSerializer.Serialize(await _mcpGateway.SearchAsync(symbol, true, null, ct)),
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
}
