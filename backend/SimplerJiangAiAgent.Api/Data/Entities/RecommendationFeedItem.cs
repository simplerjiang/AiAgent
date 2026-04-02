using System.Text.Json.Serialization;

namespace SimplerJiangAiAgent.Api.Data.Entities;

public enum RecommendFeedItemType
{
    RoleMessage,
    ToolEvent,
    StageTransition,
    SystemNotice,
    UserFollowUp,
    DegradedNotice,
    ErrorNotice
}

public sealed class RecommendationFeedItem
{
    public long Id { get; set; }
    public long TurnId { get; set; }
    public long? StageId { get; set; }
    public string? RoleId { get; set; }
    public RecommendFeedItemType ItemType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string? TraceId { get; set; }
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public RecommendationTurn Turn { get; set; } = null!;
}
