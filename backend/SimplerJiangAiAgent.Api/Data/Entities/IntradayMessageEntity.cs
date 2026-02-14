namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class IntradayMessageEntity
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string? Url { get; set; }
}
