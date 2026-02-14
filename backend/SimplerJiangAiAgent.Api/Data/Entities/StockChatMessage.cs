namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class StockChatMessage
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public StockChatSession Session { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
