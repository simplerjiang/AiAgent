using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IStockChatHistoryService
{
    Task<StockChatSession> CreateSessionAsync(string symbol, string? title, string? sessionKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockChatSession>> GetSessionsAsync(string symbol, CancellationToken cancellationToken = default);
    Task<StockChatSession?> GetSessionAsync(string sessionKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockChatMessage>> GetMessagesAsync(string sessionKey, CancellationToken cancellationToken = default);
    Task SaveMessagesAsync(string sessionKey, IReadOnlyList<StockChatMessageDto> messages, CancellationToken cancellationToken = default);
}
