using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed record ResearchFollowUpRoutingDecision(
    ResearchContinuationMode ContinuationMode,
    int? FromStageIndex,
    string? ReuseScope,
    string? ChangeSummary,
    string? Reasoning,
    decimal? Confidence);

public interface IResearchFollowUpRoutingService
{
    Task<ResearchFollowUpRoutingDecision> DecideAsync(long sessionId, string userPrompt, CancellationToken cancellationToken = default);
    /// <summary>Instant keyword-based heuristic, no LLM call.</summary>
    ResearchFollowUpRoutingDecision DecideHeuristic(string userPrompt);
}

public sealed class ResearchFollowUpRoutingService : IResearchFollowUpRoutingService
{
    private readonly AppDbContext _dbContext;
    private readonly ILlmService _llmService;
    private readonly ILogger<ResearchFollowUpRoutingService> _logger;

    public ResearchFollowUpRoutingService(
        AppDbContext dbContext,
        ILlmService llmService,
        ILogger<ResearchFollowUpRoutingService> logger)
    {
        _dbContext = dbContext;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<ResearchFollowUpRoutingDecision> DecideAsync(long sessionId, string userPrompt, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.ResearchSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return BuildFallback(userPrompt);
        }

        var latestTurn = await _dbContext.ResearchTurns
            .AsNoTracking()
            .Where(t => t.SessionId == sessionId)
            .OrderByDescending(t => t.TurnIndex)
            .FirstOrDefaultAsync(cancellationToken);

        var latestDecision = latestTurn is null
            ? null
            : await _dbContext.ResearchDecisionSnapshots
                .AsNoTracking()
                .Where(d => d.TurnId == latestTurn.Id)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

        var latestBlocks = latestTurn is null
            ? Array.Empty<ResearchReportBlock>()
            : await _dbContext.ResearchReportBlocks
                .AsNoTracking()
                .Where(b => b.TurnId == latestTurn.Id)
                .OrderBy(b => b.BlockType)
                .ToArrayAsync(cancellationToken);

        try
        {
            var traceId = $"followup-router-{sessionId}-{Guid.NewGuid():N}";
            var llmResult = await _llmService.ChatAsync(
                "active",
                new LlmChatRequest(BuildPrompt(session, latestTurn, latestDecision, latestBlocks, userPrompt), null, 0.1, false, traceId),
                cancellationToken);

            return ParseDecision(llmResult.Content) ?? BuildFallback(userPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Follow-up routing failed for session {SessionId}; falling back to heuristic routing", sessionId);
            return BuildFallback(userPrompt);
        }
    }

    private static string BuildPrompt(
        ResearchSession session,
        ResearchTurn? latestTurn,
        ResearchDecisionSnapshot? latestDecision,
        IReadOnlyList<ResearchReportBlock> latestBlocks,
        string userPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是研究工作台中的 follow-up 路由组合经理。你的职责不是重新分析股票，而是判断这次追问应该如何复用已有研究。只输出 JSON，不要输出 Markdown。");
        sb.AppendLine();
        sb.AppendLine("可选 route：ContinueSession | PartialRerun | FullRerun | NewSession");
        sb.AppendLine("stageIndex 含义：0=公司概览，1=分析师团队，2=研究辩论，3=交易方案，4=风险评估，5=投资决策。");
        sb.AppendLine("规则：");
        sb.AppendLine("1. 如果追问只是澄清、解释、补充已有结论，优先 ContinueSession（无需 fromStageIndex，系统将只重跑最终决策阶段）。");
        sb.AppendLine("2. 如果只需要重做某一后段链路，优先 PartialRerun，并给出最早需要重跑的 stageIndex。");
        sb.AppendLine("3. 只有当前分析框架整体失效时才选择 FullRerun。");
        sb.AppendLine("4. 只有用户明确要求开启新的独立分析主题时才选择 NewSession。");
        sb.AppendLine("5. 输出 JSON 字段：route, fromStageIndex, reuseScope, changeSummary, reasoning, confidence。");
        sb.AppendLine();
        sb.AppendLine($"symbol: {session.Symbol}");
        sb.AppendLine($"sessionName: {session.Name}");
        if (latestTurn is not null)
        {
            sb.AppendLine($"latestPrompt: {latestTurn.UserPrompt}");
            sb.AppendLine($"latestContinuationMode: {latestTurn.ContinuationMode}");
        }
        if (latestDecision is not null)
        {
            sb.AppendLine($"latestRating: {latestDecision.Rating}");
            sb.AppendLine($"latestExecutiveSummary: {latestDecision.ExecutiveSummary}");
            sb.AppendLine($"latestRiskConsensus: {latestDecision.RiskConsensus}");
        }
        if (latestBlocks.Count > 0)
        {
            sb.AppendLine("latestBlocks:");
            foreach (var block in latestBlocks.Take(8))
            {
                var summary = block.Summary;
                if (!string.IsNullOrWhiteSpace(summary) && summary.Length > 180)
                {
                    summary = summary[..180];
                }

                sb.AppendLine($"- {block.BlockType}: {block.Headline} | {summary}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"userFollowUp: {userPrompt}");
        return sb.ToString();
    }

    private static ResearchFollowUpRoutingDecision? ParseDecision(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                return null;
            }

            var jsonText = ResearchRunner.UnwrapContentWrapper(content[jsonStart..(jsonEnd + 1)]);
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;
            var routeText = root.TryGetProperty("route", out var routeNode) ? routeNode.GetString() : null;
            if (!Enum.TryParse<ResearchContinuationMode>(routeText, true, out var route))
            {
                return null;
            }

            int? fromStageIndex = null;
            if (root.TryGetProperty("fromStageIndex", out var stageNode) && stageNode.ValueKind == JsonValueKind.Number)
            {
                fromStageIndex = stageNode.GetInt32();
            }

            decimal? confidence = null;
            if (root.TryGetProperty("confidence", out var confidenceNode) && confidenceNode.TryGetDecimal(out var confidenceValue))
            {
                confidence = confidenceValue;
            }

            return new ResearchFollowUpRoutingDecision(
                route,
                fromStageIndex,
                root.TryGetProperty("reuseScope", out var reuseScopeNode) ? reuseScopeNode.GetString() : null,
                root.TryGetProperty("changeSummary", out var changeSummaryNode) ? changeSummaryNode.GetString() : null,
                root.TryGetProperty("reasoning", out var reasoningNode) ? reasoningNode.GetString() : null,
                confidence);
        }
        catch
        {
            return null;
        }
    }

    public ResearchFollowUpRoutingDecision DecideHeuristic(string userPrompt) => BuildFallback(userPrompt);

    private static ResearchFollowUpRoutingDecision BuildFallback(string userPrompt)
    {
        var prompt = userPrompt ?? string.Empty;
        if (ContainsAny(prompt, "风险", "止损", "止盈", "仓位", "回撤"))
        {
            return new ResearchFollowUpRoutingDecision(
                ResearchContinuationMode.PartialRerun,
                4,
                "reuse_research_and_trade_plan",
                "用户追问聚焦风险与执行约束，仅重跑风险评估与最终决策。",
                "追问内容集中在风险控制和仓位管理，前序研究素材仍可复用。",
                0.66m);
        }

        if (ContainsAny(prompt, "交易", "买入", "卖出", "建仓", "减仓"))
        {
            return new ResearchFollowUpRoutingDecision(
                ResearchContinuationMode.PartialRerun,
                3,
                "reuse_research_debate",
                "用户追问聚焦交易动作与执行条件，从交易方案开始重跑更合适。",
                "交易层问题通常需要复用研究结论，再重做交易方案、风险评估与最终决策。",
                0.64m);
        }

        if (ContainsAny(prompt, "新闻", "公告", "消息面", "舆情", "社交"))
        {
            return new ResearchFollowUpRoutingDecision(
                ResearchContinuationMode.PartialRerun,
                1,
                "reuse_company_overview",
                "用户追问涉及新闻或情绪变化，需要从分析师团队阶段重新组织证据。",
                "消息面变化会影响多个分析师角色，因此从分析师团队起重跑更稳妥。",
                0.7m);
        }

        return new ResearchFollowUpRoutingDecision(
            ResearchContinuationMode.ContinueSession,
            null,
            "reuse_existing_materials",
            "用户追问更像是对已有结论做解释或补充，直接延续当前会话。",
            "没有出现明确要求重跑某段流程的信号，优先复用现有研究成果。",
            0.6m);
    }

    private static bool ContainsAny(string input, params string[] keywords) =>
        keywords.Any(keyword => input.Contains(keyword, StringComparison.OrdinalIgnoreCase));
}