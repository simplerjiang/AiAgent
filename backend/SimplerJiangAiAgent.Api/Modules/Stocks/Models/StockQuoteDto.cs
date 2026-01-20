namespace SimplerJiangAiAgent.Api.Modules.Stocks.Models;

public sealed record StockQuoteDto(
    string Symbol,
    string Name,
    decimal Price,
    decimal Change,
    decimal ChangePercent,
    DateTime Timestamp,
    IReadOnlyList<StockNewsDto> News,
    IReadOnlyList<StockIndicatorDto> Indicators
);

public sealed record StockNewsDto(
    string Title,
    string Url,
    string Source,
    DateTime PublishedAt
);

public sealed record StockIndicatorDto(
    string Name,
    decimal Value,
    string? Unit
);
