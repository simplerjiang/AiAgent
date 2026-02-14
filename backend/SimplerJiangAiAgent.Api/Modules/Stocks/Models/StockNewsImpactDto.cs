namespace SimplerJiangAiAgent.Api.Modules.Stocks.Models;

public sealed record StockNewsImpactItemDto(
    string Title,
    string Source,
    DateTime PublishedAt,
    string? Url,
    string EventType,
    decimal TypeWeight,
    decimal SourceCredibility,
    string Theme,
    int MergedCount,
    string Category,
    int ImpactScore,
    string? Reason
);

public sealed record StockNewsImpactSummaryDto(
    int Positive,
    int Neutral,
    int Negative,
    string Overall,
    int MaxPositiveScore,
    int MaxNegativeScore
);

public sealed record StockNewsImpactDto(
    string Symbol,
    string Name,
    DateTime GeneratedAt,
    StockNewsImpactSummaryDto Summary,
    IReadOnlyList<StockNewsImpactItemDto> Events
);
