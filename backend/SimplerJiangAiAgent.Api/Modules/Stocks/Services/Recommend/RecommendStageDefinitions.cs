using SimplerJiangAiAgent.Api.Data.Entities;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend;

public sealed record RecommendStageStep(
    RecommendStageType StageType,
    RecommendStageExecutionMode ExecutionMode,
    IReadOnlyList<string> RoleIds,
    int MaxDebateRounds = 0);

public sealed record RecommendPipelineStage(
    int StageIndex,
    IReadOnlyList<RecommendStageStep> Steps);

public static class RecommendStageDefinitions
{
    public static IReadOnlyList<RecommendPipelineStage> GetPipeline() => Pipeline;

    private static readonly IReadOnlyList<RecommendPipelineStage> Pipeline =
    [
        // Stage 1: MarketScan → Parallel(MacroAnalyst, SectorHunter, SmartMoneyAnalyst)
        new(0, [
            new(RecommendStageType.MarketScan, RecommendStageExecutionMode.Parallel,
                [RecommendAgentRoleIds.MacroAnalyst, RecommendAgentRoleIds.SectorHunter, RecommendAgentRoleIds.SmartMoneyAnalyst])
        ]),

        // Stage 2: SectorDebate → Debate(SectorBull, SectorBear, SectorJudge) maxDebateRounds=3
        new(1, [
            new(RecommendStageType.SectorDebate, RecommendStageExecutionMode.Debate,
                [RecommendAgentRoleIds.SectorBull, RecommendAgentRoleIds.SectorBear, RecommendAgentRoleIds.SectorJudge],
                MaxDebateRounds: 3)
        ]),

        // Stage 3: StockPicking → Parallel(LeaderPicker, GrowthPicker) → Sequential(ChartValidator)
        new(2, [
            new(RecommendStageType.StockPicking, RecommendStageExecutionMode.Parallel,
                [RecommendAgentRoleIds.LeaderPicker, RecommendAgentRoleIds.GrowthPicker]),
            new(RecommendStageType.StockPicking, RecommendStageExecutionMode.Sequential,
                [RecommendAgentRoleIds.ChartValidator])
        ]),

        // Stage 4: StockDebate → Debate(StockBull, StockBear) → Sequential(RiskReviewer)
        new(3, [
            new(RecommendStageType.StockDebate, RecommendStageExecutionMode.Debate,
                [RecommendAgentRoleIds.StockBull, RecommendAgentRoleIds.StockBear],
                MaxDebateRounds: 3),
            new(RecommendStageType.StockDebate, RecommendStageExecutionMode.Sequential,
                [RecommendAgentRoleIds.RiskReviewer])
        ]),

        // Stage 5: FinalDecision → Sequential(Director)
        new(4, [
            new(RecommendStageType.FinalDecision, RecommendStageExecutionMode.Sequential,
                [RecommendAgentRoleIds.Director])
        ]),
    ];
}
