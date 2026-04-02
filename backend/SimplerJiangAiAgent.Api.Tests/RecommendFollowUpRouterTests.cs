using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public class RecommendFollowUpRouterTests
{
    #region Heuristic Routing

    [Theory]
    [InlineData("重新推荐", FollowUpStrategy.FullRerun)]
    [InlineData("完全重来吧", FollowUpStrategy.FullRerun)]
    [InlineData("全部重做", FollowUpStrategy.FullRerun)]
    public void Heuristic_FullRerun_Detected(string userMessage, FollowUpStrategy expected)
    {
        var plan = RecommendFollowUpRouter.DecideHeuristic(userMessage);
        Assert.Equal(expected, plan.Strategy);
        Assert.Null(plan.FromStageIndex);
    }

    [Theory]
    [InlineData("换个方向看看消费板块", FollowUpStrategy.PartialRerun, 1)]
    [InlineData("看医药板块", FollowUpStrategy.PartialRerun, 1)]
    [InlineData("换板块", FollowUpStrategy.PartialRerun, 1)]
    public void Heuristic_PartialRerun_SectorDebate_Detected(string userMessage, FollowUpStrategy expected, int fromStage)
    {
        var plan = RecommendFollowUpRouter.DecideHeuristic(userMessage);
        Assert.Equal(expected, plan.Strategy);
        Assert.Equal(fromStage, plan.FromStageIndex);
    }

    [Theory]
    [InlineData("半导体板块再选几只", FollowUpStrategy.PartialRerun, 2)]
    [InlineData("多推荐几只股票", FollowUpStrategy.PartialRerun, 2)]
    [InlineData("补充一些其他股票", FollowUpStrategy.PartialRerun, 2)]
    public void Heuristic_PartialRerun_StockPicking_Detected(string userMessage, FollowUpStrategy expected, int fromStage)
    {
        var plan = RecommendFollowUpRouter.DecideHeuristic(userMessage);
        Assert.Equal(expected, plan.Strategy);
        Assert.Equal(fromStage, plan.FromStageIndex);
    }

    [Theory]
    [InlineData("为什么推荐这只股票？", FollowUpStrategy.DirectAnswer)]
    [InlineData("推荐的依据是什么", FollowUpStrategy.DirectAnswer)]
    [InlineData("解释一下选股原因", FollowUpStrategy.DirectAnswer)]
    public void Heuristic_DirectAnswer_Detected(string userMessage, FollowUpStrategy expected)
    {
        var plan = RecommendFollowUpRouter.DecideHeuristic(userMessage);
        Assert.Equal(expected, plan.Strategy);
    }

    [Fact]
    public void Heuristic_WorkbenchHandoff_WithStockCode()
    {
        var plan = RecommendFollowUpRouter.DecideHeuristic("帮我详细分析600519");
        Assert.Equal(FollowUpStrategy.WorkbenchHandoff, plan.Strategy);
        Assert.NotNull(plan.Overrides);
        Assert.Contains("600519", plan.Overrides!.TargetStocks!);
    }

    [Fact]
    public void Heuristic_Unknown_DefaultsToFullRerun()
    {
        var plan = RecommendFollowUpRouter.DecideHeuristic("说点别的吧");
        Assert.Equal(FollowUpStrategy.FullRerun, plan.Strategy);
        Assert.True(plan.Confidence <= 0.6m);
    }

    #endregion

    #region ParseFollowUpPlan

    [Fact]
    public void Parse_ValidPartialRerunJson_ReturnsCorrectPlan()
    {
        var json = """
        {
            "intent": "用户要求补充选股",
            "strategy": "partial_rerun",
            "fromStageIndex": 2,
            "reasoning": "从选股阶段开始重跑",
            "confidence": 0.85
        }
        """;

        var plan = RecommendFollowUpRouter.ParseFollowUpPlan(json);
        Assert.NotNull(plan);
        Assert.Equal(FollowUpStrategy.PartialRerun, plan!.Strategy);
        Assert.Equal(2, plan.FromStageIndex);
        Assert.Equal("用户要求补充选股", plan.Intent);
        Assert.Equal(0.85m, plan.Confidence);
    }

    [Fact]
    public void Parse_FullRerunJson_ReturnsCorrectPlan()
    {
        var json = """{"intent":"全量重跑","strategy":"full_rerun","reasoning":"用户要求重新推荐","confidence":0.9}""";
        var plan = RecommendFollowUpRouter.ParseFollowUpPlan(json);
        Assert.NotNull(plan);
        Assert.Equal(FollowUpStrategy.FullRerun, plan!.Strategy);
    }

    [Fact]
    public void Parse_WorkbenchHandoff_WithOverrides()
    {
        var json = """
        {
            "intent": "深入分析个股",
            "strategy": "workbench_handoff",
            "overrides": {"targetStocks": ["600519"], "targetSectors": null},
            "reasoning": "交接到 Workbench",
            "confidence": 0.8
        }
        """;

        var plan = RecommendFollowUpRouter.ParseFollowUpPlan(json);
        Assert.NotNull(plan);
        Assert.Equal(FollowUpStrategy.WorkbenchHandoff, plan!.Strategy);
        Assert.NotNull(plan.Overrides);
        Assert.Contains("600519", plan.Overrides!.TargetStocks!);
    }

    [Fact]
    public void Parse_DirectAnswer_ReturnsCorrectPlan()
    {
        var json = """{"intent":"解释推荐依据","strategy":"direct_answer","reasoning":"从辩论记录提取","confidence":0.75}""";
        var plan = RecommendFollowUpRouter.ParseFollowUpPlan(json);
        Assert.NotNull(plan);
        Assert.Equal(FollowUpStrategy.DirectAnswer, plan!.Strategy);
    }

    [Theory]
    [InlineData("重新推荐", FollowUpStrategy.FullRerun, null)]
    [InlineData("完全重来", FollowUpStrategy.FullRerun, null)]
    [InlineData("换个方向看看消费板块", FollowUpStrategy.PartialRerun, 1)]
    [InlineData("看医药", FollowUpStrategy.PartialRerun, 1)]
    [InlineData("板块再选几只股票", FollowUpStrategy.PartialRerun, 2)]
    [InlineData("补充股票", FollowUpStrategy.PartialRerun, 2)]
    public async Task RouteAsync_ExplicitRerunIntent_OverridesLlmDirectAnswer(
        string userMessage,
        FollowUpStrategy expectedStrategy,
        int? expectedFromStageIndex)
    {
        await using var db = CreateDbContext();
        var router = CreateRouter(db, """{"intent":"解释推荐依据","strategy":"direct_answer","reasoning":"LLM 认为可以直接回答","confidence":0.95}""");

        var plan = await router.RouteAsync(42, userMessage);

        Assert.Equal(expectedStrategy, plan.Strategy);
        Assert.Equal(expectedFromStageIndex, plan.FromStageIndex);
        Assert.NotEqual(FollowUpStrategy.DirectAnswer, plan.Strategy);
        Assert.Contains("覆盖 DirectAnswer 路由", plan.Reasoning);
    }

    [Fact]
    public async Task RouteAsync_ExplicitWorkbenchIntent_OverridesLlmDirectAnswer()
    {
        await using var db = CreateDbContext();
        var router = CreateRouter(db, """{"intent":"解释推荐依据","strategy":"direct_answer","reasoning":"LLM 认为可以直接回答","confidence":0.95}""");

        var plan = await router.RouteAsync(42, "详细分析600519");

        Assert.Equal(FollowUpStrategy.WorkbenchHandoff, plan.Strategy);
        Assert.NotNull(plan.Overrides);
        Assert.Contains("600519", plan.Overrides!.TargetStocks!);
        Assert.Contains("覆盖 DirectAnswer 路由", plan.Reasoning);
    }

    [Fact]
    public async Task RouteAsync_AmbiguousQuestion_KeepsLlmDirectAnswer()
    {
        await using var db = CreateDbContext();
        var router = CreateRouter(db, """{"intent":"解释推荐依据","strategy":"direct_answer","reasoning":"LLM 认为可以直接回答","confidence":0.95}""");

        var plan = await router.RouteAsync(42, "为什么推荐这只股票？");

        Assert.Equal(FollowUpStrategy.DirectAnswer, plan.Strategy);
        Assert.Equal("LLM 认为可以直接回答", plan.Reasoning);
    }

    [Fact]
    public void Parse_WithAgentsList()
    {
        var json = """
        {
            "intent": "补充龙头股",
            "strategy": "partial_rerun",
            "fromStageIndex": 2,
            "agents": [
                {"roleId": "recommend_leader_picker", "required": true},
                {"roleId": "recommend_chart_validator", "required": true}
            ],
            "reasoning": "只需龙头猎手和技术验证",
            "confidence": 0.7
        }
        """;

        var plan = RecommendFollowUpRouter.ParseFollowUpPlan(json);
        Assert.NotNull(plan);
        Assert.Equal(2, plan!.Agents.Count);
        Assert.Equal("recommend_leader_picker", plan.Agents[0].RoleId);
        Assert.True(plan.Agents[0].Required);
    }

    [Fact]
    public void Parse_WithSurroundingText_StillParses()
    {
        var content = """好的，分析结果如下：{"intent":"重新推荐","strategy":"full_rerun","reasoning":"test","confidence":0.9}以上是路由结果。""";
        var plan = RecommendFollowUpRouter.ParseFollowUpPlan(content);
        Assert.NotNull(plan);
        Assert.Equal(FollowUpStrategy.FullRerun, plan!.Strategy);
    }

    [Fact]
    public void Parse_InvalidStrategy_ReturnsNull()
    {
        var json = """{"intent":"test","strategy":"invalid_strategy","reasoning":"test","confidence":0.5}""";
        var plan = RecommendFollowUpRouter.ParseFollowUpPlan(json);
        Assert.Null(plan);
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsNull()
    {
        Assert.Null(RecommendFollowUpRouter.ParseFollowUpPlan(null));
        Assert.Null(RecommendFollowUpRouter.ParseFollowUpPlan(""));
        Assert.Null(RecommendFollowUpRouter.ParseFollowUpPlan("no json here"));
    }

    [Fact]
    public void Parse_MalformedJson_ReturnsNull()
    {
        var plan = RecommendFollowUpRouter.ParseFollowUpPlan("{\"intent\":\"test\",\"strategy\":");
        Assert.Null(plan);
    }

    #endregion

    #region Strategy Mapping

    [Fact]
    public void AllStrategies_HaveCorrespondingContinuationMode()
    {
        // Verify the mapping between FollowUpStrategy and ContinuationMode
        Assert.Equal(4, Enum.GetValues<FollowUpStrategy>().Length);
        Assert.Contains(FollowUpStrategy.PartialRerun, Enum.GetValues<FollowUpStrategy>());
        Assert.Contains(FollowUpStrategy.FullRerun, Enum.GetValues<FollowUpStrategy>());
        Assert.Contains(FollowUpStrategy.WorkbenchHandoff, Enum.GetValues<FollowUpStrategy>());
        Assert.Contains(FollowUpStrategy.DirectAnswer, Enum.GetValues<FollowUpStrategy>());
    }

    #endregion

    private static RecommendFollowUpRouter CreateRouter(AppDbContext db, string llmContent) =>
        new(
            db,
            new StaticLlmService(llmContent),
            new RecommendRoleContractRegistry(),
            NullLogger<RecommendFollowUpRouter>.Instance);

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class StaticLlmService : ILlmService
    {
        private readonly string _content;

        public StaticLlmService(string content) => _content = content;

        public Task<LlmChatResult> ChatAsync(string provider, LlmChatRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new LlmChatResult(_content, "trace-test"));
    }
}
