using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class RecommendationSessionServiceTests
{
    [Fact]
    public void RecommendEventBus_SnapshotSince_UsesMonotonicSequenceCursor()
    {
        using var eventBus = new RecommendEventBus();

        eventBus.Publish(CreateEvent(7, RecommendEventType.StageStarted, "阶段开始"));
        eventBus.Publish(CreateEvent(7, RecommendEventType.RoleStarted, "角色开始"));

        var initial = eventBus.SnapshotSince(7, 0);

        Assert.Equal(2, initial.Count);
        Assert.True(initial[0].Sequence > 0);
        Assert.True(initial[1].Sequence > initial[0].Sequence);
        Assert.Equal(RecommendEventType.StageStarted, initial[0].Event.EventType);
        Assert.Equal(RecommendEventType.RoleStarted, initial[1].Event.EventType);

        eventBus.Publish(CreateEvent(7, RecommendEventType.RoleCompleted, "角色完成"));

        var resumed = eventBus.SnapshotSince(7, initial[1].Sequence);

        var resumedEvent = Assert.Single(resumed);
        Assert.True(resumedEvent.Sequence > initial[1].Sequence);
        Assert.Equal(RecommendEventType.RoleCompleted, resumedEvent.Event.EventType);
    }

    [Fact]
    public void RecommendEventBus_SnapshotHistory_SurvivesDrainUntilCleanup()
    {
        using var eventBus = new RecommendEventBus();

        eventBus.Publish(CreateEvent(8, RecommendEventType.StageStarted, "阶段开始"));
        eventBus.Publish(CreateEvent(8, RecommendEventType.TurnCompleted, "回合完成"));

        var drained = eventBus.Drain(8);
        var snapshot = eventBus.Snapshot(8);
        var resumable = eventBus.SnapshotSince(8, 0);

        Assert.Equal(2, drained.Count);
        Assert.Empty(eventBus.Drain(8));
        Assert.Equal(2, snapshot.Count);
        Assert.Equal(2, resumable.Count);

        eventBus.Cleanup(8);

        Assert.Empty(eventBus.Snapshot(8));
        Assert.Empty(eventBus.SnapshotSince(8, 0));
    }

    [Fact]
    public async Task RecommendEventBus_MarkTurnTerminal_CleansUpAfterRetentionWindow()
    {
        using var eventBus = new RecommendEventBus();

        eventBus.Publish(CreateEvent(9, RecommendEventType.TurnCompleted, "回合完成"));
        eventBus.MarkTurnTerminal(9, TimeSpan.Zero);

        for (var attempt = 0; attempt < 20 && eventBus.Snapshot(9).Count > 0; attempt++)
        {
            await Task.Delay(10);
        }

        Assert.Empty(eventBus.Snapshot(9));
    }

    [Fact]
    public async Task SubmitFollowUpAsync_SetsActiveTurnId_ToSavedTurnId()
    {
        await using var db = CreateDbContext();
        var eventBus = new RecommendEventBus();
        var router = new StaticFollowUpRouter(new FollowUpPlan(
            "全量重跑",
            FollowUpStrategy.FullRerun,
            [],
            null,
            "测试策略",
            null,
            0.8m));
        var service = CreateService(db, eventBus, router);

        var session = await service.CreateSessionAsync("先来一轮推荐");

        var (turn, _) = await service.SubmitFollowUpAsync(session.Id, "换个方向再看一次");
        var storedSession = await db.RecommendationSessions.AsNoTracking().FirstAsync(item => item.Id == session.Id);

        Assert.True(turn.Id > 0);
        Assert.Equal(turn.Id, storedSession.ActiveTurnId);
        Assert.Equal(1, turn.TurnIndex);
    }

    [Fact]
    public async Task GetSessionDetailAsync_MergesPersistedAndLiveFeedItems()
    {
        await using var db = CreateDbContext();
        var eventBus = new RecommendEventBus();
        var service = CreateService(db, eventBus, new StaticFollowUpRouter(new FollowUpPlan(
            "全量重跑",
            FollowUpStrategy.FullRerun,
            [],
            null,
            "测试策略",
            null,
            0.8m)));

        var session = await service.CreateSessionAsync("推荐半导体方向");
        var turn = await db.RecommendationTurns.FirstAsync(item => item.SessionId == session.Id);

        db.RecommendationFeedItems.Add(new RecommendationFeedItem
        {
            TurnId = turn.Id,
            ItemType = RecommendFeedItemType.RoleMessage,
            RoleId = RecommendAgentRoleIds.MacroAnalyst,
            Content = "{\"summary\":\"偏强\"}",
            MetadataJson = JsonSerializer.Serialize(new
            {
                eventType = "RoleSummaryReady",
                stageType = "MarketScan",
                detailJson = "{\"summary\":\"偏强\"}"
            }),
            TraceId = "trace-persisted",
            CreatedAt = DateTime.UtcNow.AddSeconds(-5)
        });

        session.Status = RecommendSessionStatus.Running;
        turn.Status = RecommendTurnStatus.Running;
        session.ActiveTurnId = turn.Id;
        await db.SaveChangesAsync();

        eventBus.Publish(new RecommendEvent(
            RecommendEventType.ToolCompleted,
            session.Id,
            turn.Id,
            null,
            "MarketScan",
            RecommendAgentRoleIds.MacroAnalyst,
            "trace-live",
            "工具 web_search 返回完成",
            "{\"toolName\":\"web_search\"}",
            DateTime.UtcNow));

        var detail = await service.GetSessionDetailAsync(session.Id);

        Assert.NotNull(detail);
        var detailTurn = Assert.Single(detail!.Turns);
        Assert.Equal(2, detailTurn.FeedItems.Count);

        var persisted = Assert.Single(detailTurn.FeedItems.Where(item => item.TraceId == "trace-persisted"));
        Assert.Equal("RoleSummaryReady", persisted.EventType);
        Assert.Equal("MarketScan", persisted.StageType);

        var live = Assert.Single(detailTurn.FeedItems.Where(item => item.TraceId == "trace-live"));
        Assert.Equal("ToolCompleted", live.EventType);
        Assert.Equal("MarketScan", live.StageType);
        Assert.Equal(2, detail.FeedItems.Count);
    }

    private static RecommendationSessionService CreateService(AppDbContext db, IRecommendEventBus eventBus, IRecommendFollowUpRouter router) =>
        new(db, eventBus, router, NullLogger<RecommendationSessionService>.Instance);

    private static RecommendEvent CreateEvent(long turnId, RecommendEventType eventType, string summary) =>
        new(eventType, 1, turnId, null, "MarketScan", RecommendAgentRoleIds.MacroAnalyst, null, summary, null, DateTime.UtcNow);

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class StaticFollowUpRouter : IRecommendFollowUpRouter
    {
        private readonly FollowUpPlan _plan;

        public StaticFollowUpRouter(FollowUpPlan plan) => _plan = plan;

        public Task<FollowUpPlan> RouteAsync(long sessionId, string userMessage, CancellationToken ct = default) =>
            Task.FromResult(_plan);
    }
}