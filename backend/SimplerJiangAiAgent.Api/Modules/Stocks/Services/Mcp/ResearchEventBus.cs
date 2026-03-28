using System.Collections.Concurrent;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public enum ResearchEventType
{
    TurnStarted,
    StageStarted,
    RoleStarted,
    ToolDispatched,
    ToolProgress,
    ToolCompleted,
    RoleSummaryReady,
    RoleCompleted,
    RoleFailed,
    StageCompleted,
    StageFailed,
    TurnCompleted,
    TurnFailed,
    SystemNotice,
    DegradedNotice,
    RetryAttempt
}

public sealed record ResearchEvent(
    ResearchEventType EventType,
    long SessionId,
    long TurnId,
    long? StageId,
    string? RoleId,
    string? TraceId,
    string Summary,
    string? DetailJson,
    DateTime Timestamp);

public interface IResearchEventBus
{
    void Publish(ResearchEvent evt);
    IReadOnlyList<ResearchEvent> Drain(long turnId);
    IReadOnlyList<ResearchEvent> Peek(long turnId);
}

public sealed class ResearchEventBus : IResearchEventBus
{
    private readonly ConcurrentDictionary<long, ConcurrentQueue<ResearchEvent>> _queues = new();

    public void Publish(ResearchEvent evt)
    {
        var queue = _queues.GetOrAdd(evt.TurnId, _ => new());
        queue.Enqueue(evt);
    }

    public IReadOnlyList<ResearchEvent> Drain(long turnId)
    {
        if (!_queues.TryRemove(turnId, out var queue))
            return Array.Empty<ResearchEvent>();
        return queue.ToArray();
    }

    public IReadOnlyList<ResearchEvent> Peek(long turnId)
    {
        if (!_queues.TryGetValue(turnId, out var queue))
            return Array.Empty<ResearchEvent>();
        return queue.ToArray();
    }
}
