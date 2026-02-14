namespace SimplerJiangAiAgent.Api.Modules.Stocks.Models;

public sealed record StockQuoteDto(
    string Symbol,
    string Name,
    decimal Price,
    decimal Change,
    decimal ChangePercent,
    decimal TurnoverRate,
    decimal PeRatio,
    decimal High,
    decimal Low,
    decimal Speed,
    DateTime Timestamp,
    IReadOnlyList<StockNewsDto> News,
    IReadOnlyList<StockIndicatorDto> Indicators
);

public sealed record MarketIndexDto(
    string Symbol,
    string Name,
    decimal Price,
    decimal Change,
    decimal ChangePercent,
    DateTime Timestamp
);

public sealed record KLinePointDto(
    DateTime Date,
    decimal Open,
    decimal Close,
    decimal High,
    decimal Low,
    decimal Volume
);

public sealed record MinuteLinePointDto(
    DateOnly Date,
    TimeSpan Time,
    decimal Price,
    decimal AveragePrice,
    decimal Volume
);

public sealed record IntradayMessageDto(
    string Title,
    string Source,
    DateTime PublishedAt,
    string? Url
);

public sealed record StockDetailDto(
    StockQuoteDto Quote,
    IReadOnlyList<KLinePointDto> KLines,
    IReadOnlyList<MinuteLinePointDto> MinuteLines,
    IReadOnlyList<IntradayMessageDto> Messages
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
