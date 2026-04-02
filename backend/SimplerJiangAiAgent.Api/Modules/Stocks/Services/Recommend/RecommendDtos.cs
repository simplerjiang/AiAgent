namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend;

public sealed record RecommendCreateSessionRequestDto(string UserPrompt);
public sealed record RecommendFollowUpRequestDto(string UserPrompt);
public sealed record RecommendRetryFromStageRequestDto(int FromStageIndex);

public sealed record RecommendSessionSummaryDto(
	long Id,
	string SessionKey,
	string Status,
	string? LastUserIntent,
	long? ActiveTurnId,
	DateTime CreatedAt,
	DateTime UpdatedAt);

public sealed record RecommendSessionDetailDto(
	long Id,
	string SessionKey,
	string Status,
	long? ActiveTurnId,
	string? LastUserIntent,
	string? MarketSentiment,
	IReadOnlyList<RecommendTurnDto> Turns,
	IReadOnlyList<RecommendFeedItemDto> FeedItems,
	DateTime CreatedAt,
	DateTime UpdatedAt);

public sealed record RecommendTurnDto(
	long Id,
	int TurnIndex,
	string UserPrompt,
	string Status,
	string ContinuationMode,
	string? RoutingDecision,
	string? RoutingReasoning,
	decimal? RoutingConfidence,
	DateTime RequestedAt,
	DateTime? StartedAt,
	DateTime? CompletedAt,
	IReadOnlyList<RecommendStageSnapshotDto> StageSnapshots,
	IReadOnlyList<RecommendFeedItemDto> FeedItems);

public sealed record RecommendStageSnapshotDto(
	long Id,
	string StageType,
	int StageRunIndex,
	string ExecutionMode,
	string Status,
	string? Summary,
	IReadOnlyList<RecommendRoleStateDto> RoleStates,
	DateTime? StartedAt,
	DateTime? CompletedAt);

public sealed record RecommendRoleStateDto(
	long Id,
	string RoleId,
	int RunIndex,
	string Status,
	string? ErrorCode,
	string? ErrorMessage,
	string? LlmTraceId,
	string? OutputContentJson,
	DateTime? StartedAt,
	DateTime? CompletedAt);

public sealed record RecommendFeedItemDto(
	long Id,
	long TurnId,
	string ItemType,
	string? EventType,
	string? RoleId,
	string Summary,
	string? DetailJson,
	string? StageType,
	string? TraceId,
	DateTime Timestamp);
