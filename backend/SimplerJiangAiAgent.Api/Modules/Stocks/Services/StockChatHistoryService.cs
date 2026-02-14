using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed class StockChatHistoryService : IStockChatHistoryService
{
    private readonly AppDbContext _dbContext;

    public StockChatHistoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StockChatSession> CreateSessionAsync(string symbol, string? title, string? sessionKey, CancellationToken cancellationToken = default)
    {
        var normalized = StockSymbolNormalizer.Normalize(symbol);
        var key = string.IsNullOrWhiteSpace(sessionKey) ? $"{normalized}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" : sessionKey.Trim();
        var existing = await _dbContext.StockChatSessions
            .FirstOrDefaultAsync(x => x.SessionKey == key, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var now = DateTime.UtcNow;
        var label = string.IsNullOrWhiteSpace(title) ? now.ToString("yyyy-MM-dd HH:mm") : title.Trim();
        var session = new StockChatSession
        {
            Symbol = normalized,
            SessionKey = key,
            Title = label,
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.StockChatSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<IReadOnlyList<StockChatSession>> GetSessionsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var normalized = StockSymbolNormalizer.Normalize(symbol);
        return await _dbContext.StockChatSessions
            .Where(x => x.Symbol == normalized)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockChatSession?> GetSessionAsync(string sessionKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockChatSessions
            .FirstOrDefaultAsync(x => x.SessionKey == sessionKey, cancellationToken);
    }

    public async Task<IReadOnlyList<StockChatMessage>> GetMessagesAsync(string sessionKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockChatMessages
            .Where(x => x.Session.SessionKey == sessionKey)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveMessagesAsync(string sessionKey, IReadOnlyList<StockChatMessageDto> messages, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.StockChatSessions
            .FirstOrDefaultAsync(x => x.SessionKey == sessionKey, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException("会话不存在");
        }

        var existing = await _dbContext.StockChatMessages
            .Where(x => x.SessionId == session.Id)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _dbContext.StockChatMessages.RemoveRange(existing);
        }

        var now = DateTime.UtcNow;
        var entities = messages
            .Where(m => !string.IsNullOrWhiteSpace(m.Role) && !string.IsNullOrWhiteSpace(m.Content))
            .Select(m => new StockChatMessage
            {
                SessionId = session.Id,
                Role = m.Role.Trim(),
                Content = m.Content,
                CreatedAt = m.Timestamp ?? now
            })
            .ToList();

        if (entities.Count > 0)
        {
            _dbContext.StockChatMessages.AddRange(entities);
        }

        session.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
