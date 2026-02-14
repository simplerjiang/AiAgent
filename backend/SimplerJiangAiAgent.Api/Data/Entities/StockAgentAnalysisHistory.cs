namespace SimplerJiangAiAgent.Api.Data.Entities;

public sealed class StockAgentAnalysisHistory
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public bool UseInternet { get; set; }
    public string? Summary { get; set; }
    public string ResultJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
