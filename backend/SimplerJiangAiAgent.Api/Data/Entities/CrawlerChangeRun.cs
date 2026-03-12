namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class CrawlerChangeRun
{
    public long Id { get; set; }
    public string? TraceId { get; set; }
    public long QueueId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}