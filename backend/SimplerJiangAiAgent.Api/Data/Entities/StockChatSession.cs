using System.Collections.Generic;

namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class StockChatSession
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string SessionKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<StockChatMessage> Messages { get; set; } = new List<StockChatMessage>();
}
