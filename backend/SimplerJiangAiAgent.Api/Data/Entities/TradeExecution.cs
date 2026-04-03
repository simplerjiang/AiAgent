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

    // Navigation
    public TradingPlan? Plan { get; set; }
}
