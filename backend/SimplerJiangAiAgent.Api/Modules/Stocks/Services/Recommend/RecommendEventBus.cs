using System.Collections.Concurrent;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend;

public enum RecommendEventType
{
    TurnStarted,
    StageStarted,
    RoleStarted,
    RoleSummaryReady,
    ToolDispatched,
    ToolCompleted,
    RoleCompleted,
    RoleFailed,
    StageCompleted,
    StageFailed,
    TurnCompleted,
    TurnFailed,
    SystemNotice,
    DegradedNotice
}

public sealed record RecommendEvent(
    RecommendEventType EventType,
    long SessionId,
    long TurnId,
    long? StageId,
    string? StageType,
    string? RoleId,
    string? TraceId,
    string Summary,
    string? DetailJson,
    DateTime Timestamp);

public sealed record RecommendEventEnvelope(long Sequence, RecommendEvent Event);

public interface IRecommendEventBus
{
    void Publish(RecommendEvent evt);
    IReadOnlyList<RecommendEvent> Drain(long turnId);
    IReadOnlyList<RecommendEvent> Peek(long turnId);
    IReadOnlyList<RecommendEvent> Snapshot(long turnId);
    IReadOnlyList<RecommendEventEnvelope> SnapshotSince(long turnId, long afterSequence);
    void MarkTurnTerminal(long turnId, TimeSpan? retention = null);
    void Cleanup(long turnId);
}

public sealed class RecommendEventBus : IRecommendEventBus, IDisposable
{
    private static readonly TimeSpan DefaultRetention = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<long, TurnEventState> _turns = new();
    private readonly ConcurrentDictionary<long, CancellationTokenSource> _cleanupSchedules = new();

    public void Publish(RecommendEvent evt)
    {
        CancelScheduledCleanup(evt.TurnId);
        var state = _turns.GetOrAdd(evt.TurnId, _ => new TurnEventState());
        state.Publish(evt);
    }

    public IReadOnlyList<RecommendEvent> Drain(long turnId)
    {
        if (!_turns.TryGetValue(turnId, out var state))
            return Array.Empty<RecommendEvent>();

        return state.Drain();
    }

    public IReadOnlyList<RecommendEvent> Peek(long turnId)
    {
        if (!_turns.TryGetValue(turnId, out var state))
            return Array.Empty<RecommendEvent>();

        return state.Peek();
    }

    public IReadOnlyList<RecommendEvent> Snapshot(long turnId)
    {
        if (!_turns.TryGetValue(turnId, out var state))
            return Array.Empty<RecommendEvent>();

        return state.Snapshot();
    }

    public IReadOnlyList<RecommendEventEnvelope> SnapshotSince(long turnId, long afterSequence)
    {
        if (!_turns.TryGetValue(turnId, out var state))
            return Array.Empty<RecommendEventEnvelope>();

        return state.SnapshotSince(afterSequence);
    }

    public void MarkTurnTerminal(long turnId, TimeSpan? retention = null)
    {
        _turns.GetOrAdd(turnId, _ => new TurnEventState());
        ScheduleCleanup(turnId, retention ?? DefaultRetention);
    }

    public void Cleanup(long turnId)
    {
        _turns.TryRemove(turnId, out _);
        CancelScheduledCleanup(turnId);
    }

    public void Dispose()
    {
        foreach (var turnId in _cleanupSchedules.Keys)
        {
            CancelScheduledCleanup(turnId);
        }
    }

    private void ScheduleCleanup(long turnId, TimeSpan retention)
    {
        CancelScheduledCleanup(turnId);

        var cts = new CancellationTokenSource();
        _cleanupSchedules[turnId] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                if (retention > TimeSpan.Zero)
                {
                    await Task.Delay(retention, cts.Token);
                }

                if (!cts.IsCancellationRequested)
                {
                    Cleanup(turnId);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (_cleanupSchedules.TryGetValue(turnId, out var current) && ReferenceEquals(current, cts))
                {
                    _cleanupSchedules.TryRemove(turnId, out _);
                }

                cts.Dispose();
            }
        });
    }

    private void CancelScheduledCleanup(long turnId)
    {
        if (_cleanupSchedules.TryRemove(turnId, out var cts))
        {
            cts.Cancel();
        }
    }

    private sealed class TurnEventState
    {
        private readonly ConcurrentQueue<RecommendEventEnvelope> _queue = new();
        private readonly ConcurrentQueue<RecommendEventEnvelope> _history = new();
        private long _sequence;

        public void Publish(RecommendEvent evt)
        {
            var envelope = new RecommendEventEnvelope(Interlocked.Increment(ref _sequence), evt);
            _queue.Enqueue(envelope);
            _history.Enqueue(envelope);
        }

        public IReadOnlyList<RecommendEvent> Drain()
        {
            var list = new List<RecommendEvent>();
            while (_queue.TryDequeue(out var envelope))
            {
                list.Add(envelope.Event);
            }

            return list;
        }

        public IReadOnlyList<RecommendEvent> Peek() =>
            _queue.ToArray().Select(item => item.Event).ToArray();

        public IReadOnlyList<RecommendEvent> Snapshot() =>
            _history.ToArray().Select(item => item.Event).ToArray();

        public IReadOnlyList<RecommendEventEnvelope> SnapshotSince(long afterSequence) =>
            _history.ToArray()
                .Where(item => item.Sequence > afterSequence)
                .ToArray();
    }
}
