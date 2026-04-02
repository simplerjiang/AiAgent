namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend;

public static class RecommendAgentRoleIds
{
    // Stage 1: 市场扫描
    public const string MacroAnalyst = "recommend_macro_analyst";
    public const string SectorHunter = "recommend_sector_hunter";
    public const string SmartMoneyAnalyst = "recommend_smart_money";

    // Stage 2: 板块辩论
    public const string SectorBull = "recommend_sector_bull";
    public const string SectorBear = "recommend_sector_bear";
    public const string SectorJudge = "recommend_sector_judge";

    // Stage 3: 选股精选
    public const string LeaderPicker = "recommend_leader_picker";
    public const string GrowthPicker = "recommend_growth_picker";
    public const string ChartValidator = "recommend_chart_validator";

    // Stage 4: 个股辩论
    public const string StockBull = "recommend_stock_bull";
    public const string StockBear = "recommend_stock_bear";
    public const string RiskReviewer = "recommend_risk_reviewer";

    // Stage 5: 推荐决策
    public const string Director = "recommend_director";

    // Router
    public const string FollowUpRouter = "recommend_router";
}
