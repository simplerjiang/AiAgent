using SimplerJiangAiAgent.Api.Modules.Market.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Models;

public sealed record TradingPlanDraftRequestDto(
    string Symbol,
    long AnalysisHistoryId
);

public sealed record TradingPlanDraftDto(
    string Symbol,
    string Name,
    string Direction,
    string Status,
    decimal? TriggerPrice,
    decimal? InvalidPrice,
    decimal? StopLossPrice,
    decimal? TakeProfitPrice,
    decimal? TargetPrice,
    string? ExpectedCatalyst,
    string? InvalidConditions,
    string? RiskLimits,
    string? AnalysisSummary,
    long AnalysisHistoryId,
    string SourceAgent,
    string? UserNote,
    StockMarketContextDto? MarketContext
);

public sealed record TradingPlanCreateDto(
    string Symbol,
    string Name,
    string? Direction,
    decimal? TriggerPrice,
    decimal? InvalidPrice,
    decimal? StopLossPrice,
    decimal? TakeProfitPrice,
    decimal? TargetPrice,
    string? ExpectedCatalyst,
    string? InvalidConditions,
    string? RiskLimits,
    string? AnalysisSummary,
    long? AnalysisHistoryId,
    string? SourceAgent,
    string? UserNote,
    string? Status = null,
    string? ActiveScenario = null,
    DateOnly? PlanStartDate = null,
    DateOnly? PlanEndDate = null
);

public sealed record TradingPlanUpdateDto(
    string? Name,
    string? Direction,
    decimal? TriggerPrice,
    decimal? InvalidPrice,
    decimal? StopLossPrice,
    decimal? TakeProfitPrice,
    decimal? TargetPrice,
    string? ExpectedCatalyst,
    string? InvalidConditions,
    string? RiskLimits,
    string? AnalysisSummary,
    string? SourceAgent,
    string? UserNote,
    string? Status = null,
    string? ActiveScenario = null,
    DateOnly? PlanStartDate = null,
    DateOnly? PlanEndDate = null
);

public sealed record TradingPlanItemDto(
    long Id,
    string Symbol,
    string Name,
    string Direction,
    string Status,
    decimal? TriggerPrice,
    decimal? InvalidPrice,
    decimal? StopLossPrice,
    decimal? TakeProfitPrice,
    decimal? TargetPrice,
    string? ExpectedCatalyst,
    string? InvalidConditions,
    string? RiskLimits,
    string? AnalysisSummary,
    long? AnalysisHistoryId,
    string SourceAgent,
    string? UserNote,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? TriggeredAt,
    DateTime? InvalidatedAt,
    DateTime? CancelledAt,
    bool? WatchlistEnsured,
    StockMarketContextDto? MarketContextAtCreation,
    StockMarketContextDto? CurrentMarketContext,
    TradingPlanExecutionSummaryDto? ExecutionSummary = null,
    TradingPlanScenarioStatusDto? CurrentScenarioStatus = null,
    TradingPlanPositionContextDto? CurrentPositionSnapshot = null,
    string? ActiveScenario = null,
    DateOnly? PlanStartDate = null,
    DateOnly? PlanEndDate = null
);

public sealed record TradingPlanExecutionSummaryDto(
    int ExecutionCount,
    string? LatestAction,
    DateTime? LatestExecutedAt,
    int DeviatedCount,
    int UnplannedCount,
    string? LatestComplianceTag,
    IReadOnlyList<string> LatestDeviationTags,
    string? Summary
);

public sealed record TradingPlanScenarioStatusDto(
    string Code,
    string Label,
    string Reason,
    string SnapshotType,
    DateTime SnapshotAt,
    decimal? ReferencePrice,
    string? MarketStage,
    bool CounterTrendWarning,
    bool IsMainlineAligned,
    bool AbandonTriggered,
    string? PlanStatus,
    string? Summary
);

public sealed record TradingPlanPositionContextDto(
    string Symbol,
    string Name,
    int Quantity,
    decimal AverageCost,
    decimal? LatestPrice,
    decimal? MarketValue,
    decimal? UnrealizedPnL,
    decimal? PositionRatio,
    string SnapshotType,
    DateTime SnapshotAt,
    decimal? AvailableCash,
    decimal? TotalPositionRatio,
    string? Summary
);

public sealed record TradingPlanPortfolioSummaryDto(
    decimal TotalPositionRatio,
    decimal AvailableCash,
    decimal TotalUnrealizedPnL,
    string Summary
);

public sealed record TradingPlanExecutionContextDto(
    TradingPlanItemDto Plan,
    TradingPlanScenarioStatusDto? ScenarioStatus,
    TradingPlanPositionContextDto? CurrentPositionSnapshot,
    TradingPlanPortfolioSummaryDto PortfolioSummary,
    TradingPlanExecutionSummaryDto? ExecutionSummary
);

public sealed record TradingPlanRuntimeInsightDto(
    TradingPlanExecutionSummaryDto? ExecutionSummary,
    TradingPlanScenarioStatusDto? CurrentScenarioStatus,
    TradingPlanPositionContextDto? CurrentPositionSnapshot
);

public sealed record TradingPlanEventItemDto(
    long Id,
    long PlanId,
    string Symbol,
    string EventType,
    string Severity,
    string Message,
    decimal? SnapshotPrice,
    string? MetadataJson,
    DateTime OccurredAt
);

public sealed record SignalHistoryMetricsDto(
    string Direction,
    int SampleCount,
    decimal? HitRate5Day,
    decimal? AverageReturn5Day,
    string? Caveat
);

public sealed record RealTradeMetricsDto(
    int TotalTrades,
    int WinCount,
    decimal WinRate,
    decimal AveragePnL,
    decimal AverageReturnRate,
    string? Caveat
);

public sealed record TradingPlanDraftResponseDto(
    TradingPlanDraftDto Draft,
    SignalHistoryMetricsDto? SignalMetrics,
    RealTradeMetricsDto? RealTradeMetrics,
    MarketExecutionModeDto? ExecutionMode = null
);