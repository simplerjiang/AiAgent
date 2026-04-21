namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum TradeDirection { Buy = 1, Sell = 2 }
public enum TradeType { Normal = 1, DayTrade = 2 }
public enum ComplianceTag { FollowedPlan = 1, DeviatedFromPlan = 2, Unplanned = 3 }

public sealed class TradeExecution
{
    public long Id { get; set; }
    public long? PlanId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public TradeType TradeType { get; set; }
    public decimal ExecutedPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime ExecutedAt { get; set; }
    public decimal? Commission { get; set; }
    public string? UserNote { get; set; }
    public DateTime CreatedAt { get; set; }

    // Auto-calculated on sell
    public decimal? CostBasis { get; set; }
    public decimal? RealizedPnL { get; set; }
    public decimal? ReturnRate { get; set; }

    // Compliance
    public ComplianceTag ComplianceTag { get; set; }
    public long? AnalysisHistoryId { get; set; }
    public string? AgentDirection { get; set; }
    public decimal? AgentConfidence { get; set; }
    public string? MarketStageAtTrade { get; set; }
    public string? PlanSourceAgent { get; set; }
    public string? PlanAction { get; set; }
    public string? ExecutionAction { get; set; }
    public string? DeviationTagsJson { get; set; }
    public string? DeviationNote { get; set; }
    public string? AbandonReason { get; set; }
    public string? ScenarioCode { get; set; }
    public string? ScenarioLabel { get; set; }
    public string? ScenarioReason { get; set; }
    public string? ScenarioSnapshotType { get; set; }
    public string? ScenarioSnapshotJson { get; set; }
    public string? PositionSnapshotJson { get; set; }
    public string? CoachTip { get; set; }

    // Navigation
    public TradingPlan? Plan { get; set; }
}
