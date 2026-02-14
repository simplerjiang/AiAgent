namespace SimplerJiangAiAgent.Api.Modules.Stocks.Models;

public sealed record StockChatSessionCreateDto(
    string Symbol,
    string? Title,
    string? SessionKey
);

public sealed record StockChatSessionDto(
    string SessionKey,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record StockChatMessageDto(
    string Role,
    string Content,
    DateTime? Timestamp
);

public sealed record StockChatMessagesRequestDto(
    IReadOnlyList<StockChatMessageDto> Messages
);
