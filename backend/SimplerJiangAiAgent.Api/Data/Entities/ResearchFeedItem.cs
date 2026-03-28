namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum ResearchFeedItemType
{
    RoleMessage,
    ToolEvent,
    StageTransition,
    SystemNotice,
    UserFollowUp,
    DegradedNotice,
    ErrorNotice
}

public sealed class ResearchFeedItem
{
    public long Id { get; set; }
    public long TurnId { get; set; }
    public long? StageId { get; set; }
    public string? RoleId { get; set; }
    public ResearchFeedItemType ItemType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string? TraceId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ResearchTurn Turn { get; set; } = null!;
}
