using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend;

// ── DTOs ────────────────────────────────────────────────────────────────

public enum FollowUpStrategy
{
    PartialRerun,
    FullRerun,
    WorkbenchHandoff,
    DirectAnswer
}

public sealed record AgentInvocation(string RoleId, string? InputOverride, bool Required);

public sealed record FollowUpContextOverrides(
    string[]? TargetSectors,
    string[]? TargetStocks,
    string? TimeWindow,
    string? AdditionalConstraints);

public sealed record FollowUpPlan(
    string Intent,
    FollowUpStrategy Strategy,
    IReadOnlyList<AgentInvocation> Agents,
    FollowUpContextOverrides? Overrides,
    string Reasoning,
    int? FromStageIndex,
    decimal? Confidence);

// ── Interface ───────────────────────────────────────────────────────────

public interface IRecommendFollowUpRouter
{
    Task<FollowUpPlan> RouteAsync(long sessionId, string userMessage, CancellationToken ct = default);
}

// ── Implementation ──────────────────────────────────────────────────────

public sealed class RecommendFollowUpRouter : IRecommendFollowUpRouter
{
    private readonly AppDbContext _db;
    private readonly ILlmService _llmService;
    private readonly IRecommendRoleContractRegistry _contractRegistry;
    private readonly ILogger<RecommendFollowUpRouter> _logger;

    public RecommendFollowUpRouter(
        AppDbContext db,
        ILlmService llmService,
        IRecommendRoleContractRegistry contractRegistry,
        ILogger<RecommendFollowUpRouter> logger)
    {
        _db = db;
        _llmService = llmService;
        _contractRegistry = contractRegistry;
        _logger = logger;
    }

    public async Task<FollowUpPlan> RouteAsync(long sessionId, string userMessage, CancellationToken ct = default)
    {
        try
        {
            var contextSummary = await BuildContextSummaryAsync(sessionId, ct);
            var prompt = BuildRouterPrompt(contextSummary, userMessage);

            var result = await _llmService.ChatAsync(
                "default",
                new LlmChatRequest(prompt, null, 0.1),
                ct);

            var plan = ParseFollowUpPlan(result.Content);
            if (plan is not null)
            {
                var guardedPlan = ApplyDirectAnswerGuardrail(userMessage, plan);
                if (guardedPlan.Strategy != plan.Strategy)
                {
                    _logger.LogInformation(
                        "FollowUp router guardrail override: session={SessionId} originalStrategy={OriginalStrategy} finalStrategy={FinalStrategy}",
                        sessionId,
                        plan.Strategy,
                        guardedPlan.Strategy);
                }

                _logger.LogInformation(
                    "FollowUp router: session={SessionId} strategy={Strategy} reasoning={Reasoning}",
                    sessionId, guardedPlan.Strategy, guardedPlan.Reasoning);
                return guardedPlan;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "FollowUp router LLM call failed for session {SessionId}, falling back to heuristic", sessionId);
        }

        return DecideHeuristic(userMessage);
    }

    // ── Heuristic Fallback ──────────────────────────────────────────

    internal static FollowUpPlan DecideHeuristic(string userMessage)
    {
        var msg = userMessage ?? "";

        var explicitIntentPlan = TryResolveExplicitNonDirectAnswerIntent(msg);
        if (explicitIntentPlan is not null)
            return explicitIntentPlan;

        if (ContainsAny(msg, "重新推荐", "完全重来", "全部重做", "重新分析"))
        {
            return new FollowUpPlan(
                "用户要求全量重跑",
                FollowUpStrategy.FullRerun,
                [],
                null,
                "用户明确要求重新推荐，执行全量重跑。",
                null,
                0.8m);
        }

        if (ContainsAny(msg, "详细分析", "深入研究", "帮我看看", "个股研究") &&
            TryExtractStockMentions(msg, out var stocksForHandoff))
        {
            return new FollowUpPlan(
                "用户要求个股深入分析，交接到 Workbench",
                FollowUpStrategy.WorkbenchHandoff,
                [],
                new FollowUpContextOverrides(null, stocksForHandoff, null, null),
                "用户要求对特定个股详细分析，交接到 Trading Workbench。",
                null,
                0.75m);
        }

        if (ContainsAny(msg, "为什么", "原因", "依据", "解释", "怎么得出"))
        {
            return new FollowUpPlan(
                "用户追问推荐依据",
                FollowUpStrategy.DirectAnswer,
                [],
                null,
                "用户在询问推荐原因，可直接从辩论记录提取回答。",
                null,
                0.7m);
        }

        if (ContainsAny(msg, "换个方向", "看消费", "看医药", "换板块", "其他板块"))
        {
            return new FollowUpPlan(
                "用户要求更换板块方向",
                FollowUpStrategy.PartialRerun,
                [],
                null,
                "用户要求更换板块方向，从板块辩论阶段开始重跑。",
                1, // SectorDebate
                0.7m);
        }

        if (ContainsAny(msg, "再选几只", "多推荐", "补充", "其他股票", "再挑"))
        {
            return new FollowUpPlan(
                "用户要求补充选股",
                FollowUpStrategy.PartialRerun,
                [],
                null,
                "用户要求在当前板块中补充选股，从选股阶段开始重跑。",
                2, // StockPicking
                0.7m);
        }

        // Default: full rerun
        return new FollowUpPlan(
            "无法明确分类，默认全量重跑",
            FollowUpStrategy.FullRerun,
            [],
            null,
            "追问意图不够明确，安全起见执行全量重跑。",
            null,
            0.5m);
    }

    internal static FollowUpPlan ApplyDirectAnswerGuardrail(string userMessage, FollowUpPlan plan)
    {
        if (plan.Strategy != FollowUpStrategy.DirectAnswer)
            return plan;

        var explicitIntentPlan = TryResolveExplicitNonDirectAnswerIntent(userMessage);
        if (explicitIntentPlan is null)
            return plan;

        var reasoning = string.IsNullOrWhiteSpace(plan.Reasoning)
            ? explicitIntentPlan.Reasoning
            : $"{explicitIntentPlan.Reasoning} 原LLM路由: {plan.Reasoning}";

        return explicitIntentPlan with
        {
            Reasoning = reasoning,
            Confidence = explicitIntentPlan.Confidence ?? plan.Confidence
        };
    }

    // ── Prompt Builder ──────────────────────────────────────────────

    private static string BuildRouterPrompt(SessionContextSummary ctx, string userMessage)
    {
        var sb = new StringBuilder();
        sb.AppendLine(RecommendPromptTemplates.FollowUpRouter);
        sb.AppendLine();

        sb.AppendLine("## 当前推荐会话上下文");
        if (!string.IsNullOrWhiteSpace(ctx.LastUserIntent))
            sb.AppendLine($"最近用户意图: {ctx.LastUserIntent}");
        if (!string.IsNullOrWhiteSpace(ctx.MarketSentiment))
            sb.AppendLine($"市场情绪: {ctx.MarketSentiment}");
        if (ctx.SelectedSectors.Count > 0)
            sb.AppendLine($"已选板块: {string.Join(", ", ctx.SelectedSectors)}");
        if (ctx.RecommendedStocks.Count > 0)
            sb.AppendLine($"已推荐个股: {string.Join(", ", ctx.RecommendedStocks)}");
        if (ctx.DebateFocusPoints.Count > 0)
        {
            sb.AppendLine("辩论焦点:");
            foreach (var point in ctx.DebateFocusPoints.Take(5))
                sb.AppendLine($"  - {point}");
        }

        sb.AppendLine();
        sb.AppendLine($"## 用户追问\n{userMessage}");
        return sb.ToString();
    }

    // ── Context Summary Builder ─────────────────────────────────────

    private async Task<SessionContextSummary> BuildContextSummaryAsync(long sessionId, CancellationToken ct)
    {
        var session = await _db.RecommendationSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return new SessionContextSummary();

        var latestTurn = await _db.RecommendationTurns
            .AsNoTracking()
            .Where(t => t.SessionId == sessionId)
            .OrderByDescending(t => t.TurnIndex)
            .FirstOrDefaultAsync(ct);

        var summary = new SessionContextSummary
        {
            LastUserIntent = session.LastUserIntent,
            MarketSentiment = session.MarketSentiment
        };

        if (latestTurn is null)
            return summary;

        // Load role states from the latest turn's stage snapshots
        var roleStates = await _db.RecommendationRoleStates
            .AsNoTracking()
            .Where(rs => rs.Stage.TurnId == latestTurn.Id && rs.Status == RecommendRoleStatus.Completed)
            .Select(rs => new { rs.RoleId, rs.OutputContentJson })
            .ToListAsync(ct);

        // Extract sectors from SectorJudge output
        var judgeOutput = roleStates
            .FirstOrDefault(rs => rs.RoleId == RecommendAgentRoleIds.SectorJudge)?.OutputContentJson;
        if (!string.IsNullOrWhiteSpace(judgeOutput))
        {
            summary.SelectedSectors.AddRange(ExtractJsonArrayField(judgeOutput, "selectedSectors", "name"));
        }

        // Extract stocks from Director or picker outputs
        var directorOutput = roleStates
            .FirstOrDefault(rs => rs.RoleId == RecommendAgentRoleIds.Director)?.OutputContentJson;
        if (!string.IsNullOrWhiteSpace(directorOutput))
        {
            summary.RecommendedStocks.AddRange(ExtractJsonArrayField(directorOutput, "stockCards", "symbol"));
            var stockNames = ExtractJsonArrayField(directorOutput, "stockCards", "name");
            for (int i = 0; i < summary.RecommendedStocks.Count && i < stockNames.Count; i++)
                summary.RecommendedStocks[i] = $"{summary.RecommendedStocks[i]}({stockNames[i]})";
        }

        // Extract debate focus from bull/bear outputs (just first few points)
        foreach (var debateRole in new[] { RecommendAgentRoleIds.SectorBull, RecommendAgentRoleIds.SectorBear,
                                            RecommendAgentRoleIds.StockBull, RecommendAgentRoleIds.StockBear })
        {
            var output = roleStates.FirstOrDefault(rs => rs.RoleId == debateRole)?.OutputContentJson;
            if (!string.IsNullOrWhiteSpace(output) && output.Length > 20)
            {
                var truncated = output.Length > 200 ? output[..200] + "..." : output;
                summary.DebateFocusPoints.Add($"[{debateRole}] {truncated}");
            }
        }

        return summary;
    }

    // ── LLM Response Parser ─────────────────────────────────────────

    internal static FollowUpPlan? ParseFollowUpPlan(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd <= jsonStart)
                return null;

            using var doc = JsonDocument.Parse(content[jsonStart..(jsonEnd + 1)]);
            var root = doc.RootElement;

            var strategyText = GetString(root, "strategy");
            if (!TryParseStrategy(strategyText, out var strategy))
                return null;

            var intent = GetString(root, "intent") ?? "追问";
            var reasoning = GetString(root, "reasoning") ?? "";

            int? fromStageIndex = null;
            if (root.TryGetProperty("fromStageIndex", out var stageEl) && stageEl.ValueKind == JsonValueKind.Number)
                fromStageIndex = stageEl.GetInt32();

            decimal? confidence = null;
            if (root.TryGetProperty("confidence", out var confEl) && confEl.TryGetDecimal(out var confVal))
                confidence = confVal;

            // Parse agents array
            var agents = new List<AgentInvocation>();
            if (root.TryGetProperty("agents", out var agentsEl) && agentsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var agentEl in agentsEl.EnumerateArray())
                {
                    var roleId = GetString(agentEl, "roleId");
                    if (!string.IsNullOrWhiteSpace(roleId))
                    {
                        agents.Add(new AgentInvocation(
                            roleId,
                            GetString(agentEl, "inputOverride"),
                            agentEl.TryGetProperty("required", out var reqEl) && reqEl.ValueKind == JsonValueKind.True));
                    }
                }
            }

            // Parse overrides
            FollowUpContextOverrides? overrides = null;
            if (root.TryGetProperty("overrides", out var overEl) && overEl.ValueKind == JsonValueKind.Object)
            {
                overrides = new FollowUpContextOverrides(
                    GetStringArray(overEl, "targetSectors"),
                    GetStringArray(overEl, "targetStocks"),
                    GetString(overEl, "timeWindow"),
                    GetString(overEl, "additionalConstraints"));
            }

            return new FollowUpPlan(intent, strategy, agents, overrides, reasoning, fromStageIndex, confidence);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static bool TryParseStrategy(string? text, out FollowUpStrategy strategy)
    {
        strategy = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        return text.ToLowerInvariant() switch
        {
            "partial_rerun" or "partialrerun" => Assign(FollowUpStrategy.PartialRerun, out strategy),
            "full_rerun" or "fullrerun" => Assign(FollowUpStrategy.FullRerun, out strategy),
            "workbench_handoff" or "workbenchhandoff" => Assign(FollowUpStrategy.WorkbenchHandoff, out strategy),
            "direct_answer" or "directanswer" => Assign(FollowUpStrategy.DirectAnswer, out strategy),
            _ => false
        };

        static bool Assign(FollowUpStrategy val, out FollowUpStrategy target) { target = val; return true; }
    }

    private static string? GetString(JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }

    private static string[]? GetStringArray(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return null;
        return arr.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .ToArray();
    }

    private static List<string> ExtractJsonArrayField(string json, string arrayProp, string fieldProp)
    {
        var results = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(arrayProp, out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty(fieldProp, out var field) && field.ValueKind == JsonValueKind.String)
                    {
                        var val = field.GetString();
                        if (!string.IsNullOrWhiteSpace(val))
                            results.Add(val);
                    }
                }
            }
        }
        catch { /* tolerant parse */ }
        return results;
    }

    private static FollowUpPlan? TryResolveExplicitNonDirectAnswerIntent(string userMessage)
    {
        var msg = userMessage ?? "";

        if (ContainsAny(msg, "重新推荐", "完全重来", "全部重做"))
        {
            return new FollowUpPlan(
                "用户要求全量重跑",
                FollowUpStrategy.FullRerun,
                [],
                null,
                "命中显式全量重跑意图，覆盖 DirectAnswer 路由。",
                null,
                0.9m);
        }

        if (ContainsAny(msg, "详细分析", "深度分析", "深入研究", "个股研究") &&
            TryExtractStockMentions(msg, out var stocksForHandoff))
        {
            return new FollowUpPlan(
                "用户要求个股深入分析，交接到 Workbench",
                FollowUpStrategy.WorkbenchHandoff,
                [],
                new FollowUpContextOverrides(null, stocksForHandoff, null, null),
                "命中显式个股深挖意图，覆盖 DirectAnswer 路由并交接到 Workbench。",
                null,
                0.85m);
        }

        if (ContainsAny(msg, "换个方向", "看消费", "看医药"))
        {
            return new FollowUpPlan(
                "用户要求更换板块方向",
                FollowUpStrategy.PartialRerun,
                [],
                null,
                "命中显式换方向意图，覆盖 DirectAnswer 路由并从板块辩论阶段重跑。",
                1,
                0.85m);
        }

        if (ContainsAny(msg, "再选几只", "补充股票"))
        {
            return new FollowUpPlan(
                "用户要求补充选股",
                FollowUpStrategy.PartialRerun,
                [],
                null,
                "命中显式补充选股意图，覆盖 DirectAnswer 路由并从选股阶段重跑。",
                2,
                0.85m);
        }

        return null;
    }

    private static bool TryExtractStockMentions(string msg, out string[] stocks)
    {
        // Match 6-digit stock codes; use lookaround instead of \b for Chinese text compatibility
        var matches = System.Text.RegularExpressions.Regex.Matches(msg, @"(?<!\d)\d{6}(?!\d)");
        if (matches.Count > 0)
        {
            stocks = matches.Select(m => m.Value).Distinct().ToArray();
            return true;
        }
        stocks = [];
        return false;
    }

    private static bool ContainsAny(string input, params string[] keywords) =>
        keywords.Any(k => input.Contains(k, StringComparison.OrdinalIgnoreCase));

    private sealed class SessionContextSummary
    {
        public string? LastUserIntent { get; set; }
        public string? MarketSentiment { get; set; }
        public List<string> SelectedSectors { get; } = [];
        public List<string> RecommendedStocks { get; } = [];
        public List<string> DebateFocusPoints { get; } = [];
    }
}
