using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IResearchRunner
{
    Task RunTurnAsync(long turnId, CancellationToken cancellationToken = default);
}

public sealed class ResearchRunner : IResearchRunner
{
    private const int MaxDebateRounds = 3;

    private static readonly IReadOnlyList<StageDefinition> Pipeline =
    [
        new(ResearchStageType.CompanyOverviewPreflight, ResearchStageExecutionMode.Sequential,
            [StockAgentRoleIds.CompanyOverviewAnalyst]),
        new(ResearchStageType.AnalystTeam, ResearchStageExecutionMode.Parallel,
            [StockAgentRoleIds.MarketAnalyst, StockAgentRoleIds.SocialSentimentAnalyst,
             StockAgentRoleIds.NewsAnalyst, StockAgentRoleIds.FundamentalsAnalyst,
             StockAgentRoleIds.ShareholderAnalyst, StockAgentRoleIds.ProductAnalyst]),
        new(ResearchStageType.ResearchDebate, ResearchStageExecutionMode.Debate,
            [StockAgentRoleIds.BullResearcher, StockAgentRoleIds.BearResearcher,
             StockAgentRoleIds.ResearchManager]),
        new(ResearchStageType.TraderProposal, ResearchStageExecutionMode.Sequential,
            [StockAgentRoleIds.Trader]),
        new(ResearchStageType.RiskDebate, ResearchStageExecutionMode.Debate,
            [StockAgentRoleIds.AggressiveRiskAnalyst, StockAgentRoleIds.NeutralRiskAnalyst,
             StockAgentRoleIds.ConservativeRiskAnalyst]),
        new(ResearchStageType.PortfolioDecision, ResearchStageExecutionMode.Sequential,
            [StockAgentRoleIds.PortfolioManager]),
    ];

    private readonly AppDbContext _dbContext;
    private readonly IResearchRoleExecutor _roleExecutor;
    private readonly IResearchEventBus _eventBus;
    private readonly ILogger<ResearchRunner> _logger;

    public ResearchRunner(
        AppDbContext dbContext,
        IResearchRoleExecutor roleExecutor,
        IResearchEventBus eventBus,
        ILogger<ResearchRunner> logger)
    {
        _dbContext = dbContext;
        _roleExecutor = roleExecutor;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task RunTurnAsync(long turnId, CancellationToken cancellationToken = default)
    {
        // Load turn without cancellation token so we can always set status on cancel
        var turn = await _dbContext.ResearchTurns
            .Include(t => t.Session)
            .FirstOrDefaultAsync(t => t.Id == turnId, CancellationToken.None)
            ?? throw new InvalidOperationException($"Turn {turnId} not found");

        var session = turn.Session;
        turn.Status = ResearchTurnStatus.Running;
        turn.StartedAt = DateTime.UtcNow;
        session.Status = ResearchSessionStatus.Running;
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        _eventBus.Publish(new ResearchEvent(
            ResearchEventType.TurnStarted, session.Id, turn.Id, null, null, null,
            $"Turn {turn.TurnIndex} started for {session.Symbol}", null, DateTime.UtcNow));

        var upstreamArtifacts = new List<string>();
        var turnDegraded = false;

        try
        {
            for (var i = 0; i < Pipeline.Count; i++)
            {
                var stageDef = Pipeline[i];
                cancellationToken.ThrowIfCancellationRequested();

                session.ActiveStage = stageDef.StageType.ToString();
                session.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                var stageResult = await RunStageAsync(session, turn, stageDef, i, upstreamArtifacts, cancellationToken);

                if (stageResult.Status == ResearchStageStatus.Failed)
                {
                    turn.Status = ResearchTurnStatus.Failed;
                    turn.StopReason = $"Stage {stageDef.StageType} failed";
                    turn.CompletedAt = DateTime.UtcNow;
                    session.Status = ResearchSessionStatus.Failed;
                    session.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _eventBus.Publish(new ResearchEvent(
                        ResearchEventType.TurnFailed, session.Id, turn.Id, null, null, null,
                        $"Turn failed at {stageDef.StageType}", null, DateTime.UtcNow));

                    await PersistFeedItemsAsync(turn.Id, CancellationToken.None);
                    return;
                }

                if (stageResult.Status == ResearchStageStatus.Degraded)
                    turnDegraded = true;

                upstreamArtifacts.AddRange(stageResult.Outputs);

                if (stageDef.StageType == ResearchStageType.PortfolioDecision && stageResult.Outputs.Count > 0)
                    await CreateDecisionSnapshotAsync(session, turn, stageResult.Outputs[0], cancellationToken);
            }

            turn.Status = ResearchTurnStatus.Completed;
            turn.CompletedAt = DateTime.UtcNow;
            session.Status = turnDegraded ? ResearchSessionStatus.Degraded : ResearchSessionStatus.Completed;
            session.ActiveStage = null;
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _eventBus.Publish(new ResearchEvent(
                ResearchEventType.TurnCompleted, session.Id, turn.Id, null, null, null,
                $"Turn {turn.TurnIndex} completed", null, DateTime.UtcNow));
        }
        catch (OperationCanceledException)
        {
            turn.Status = ResearchTurnStatus.Cancelled;
            turn.CompletedAt = DateTime.UtcNow;
            session.Status = ResearchSessionStatus.Idle;
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Turn {TurnId} failed", turnId);
            turn.Status = ResearchTurnStatus.Failed;
            turn.StopReason = ex.Message;
            turn.CompletedAt = DateTime.UtcNow;
            session.Status = ResearchSessionStatus.Failed;
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            _eventBus.Publish(new ResearchEvent(
                ResearchEventType.TurnFailed, session.Id, turn.Id, null, null, null,
                $"Turn failed: {ex.Message}", null, DateTime.UtcNow));
        }
        finally
        {
            await PersistFeedItemsAsync(turn.Id, CancellationToken.None);
        }
    }

    private async Task<StageResult> RunStageAsync(
        ResearchSession session, ResearchTurn turn,
        StageDefinition stageDef, int stageRunIndex,
        IReadOnlyList<string> upstreamArtifacts,
        CancellationToken cancellationToken)
    {
        var stage = new ResearchStageSnapshot
        {
            TurnId = turn.Id,
            StageType = stageDef.StageType,
            StageRunIndex = stageRunIndex,
            ExecutionMode = stageDef.ExecutionMode,
            Status = ResearchStageStatus.Running,
            ActiveRoleIdsJson = JsonSerializer.Serialize(stageDef.RoleIds),
            StartedAt = DateTime.UtcNow
        };
        _dbContext.ResearchStageSnapshots.Add(stage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _eventBus.Publish(new ResearchEvent(
            ResearchEventType.StageStarted, session.Id, turn.Id, stage.Id, null, null,
            $"Stage {stageDef.StageType} ({stageDef.ExecutionMode})", null, DateTime.UtcNow));

        var outputs = new List<string>();
        var degradedFlags = new List<string>();
        var failed = false;

        if (stageDef.ExecutionMode == ResearchStageExecutionMode.Debate)
        {
            var dr = await RunDebateAsync(session, turn, stage, stageDef, upstreamArtifacts, cancellationToken);
            outputs.AddRange(dr.Outputs);
            degradedFlags.AddRange(dr.DegradedFlags);
            failed = dr.Failed;
        }
        else if (stageDef.ExecutionMode == ResearchStageExecutionMode.Parallel)
        {
            var pr = await RunParallelAsync(session, turn, stage, stageDef.RoleIds, 0, upstreamArtifacts, cancellationToken);
            outputs.AddRange(pr.Outputs);
            degradedFlags.AddRange(pr.DegradedFlags);
            failed = pr.Failed;
        }
        else
        {
            foreach (var roleId in stageDef.RoleIds)
            {
                var r = await ExecuteAndPersistRoleAsync(session, turn, stage, roleId, 0, upstreamArtifacts, cancellationToken);
                if (r.Status == ResearchRoleStatus.Failed) { failed = true; break; }
                if (r.Status == ResearchRoleStatus.Degraded) degradedFlags.AddRange(r.DegradedFlags);
                if (r.OutputContentJson is not null) outputs.Add($"[{roleId}]\n{r.OutputContentJson}");
            }
        }

        stage.Status = failed ? ResearchStageStatus.Failed
            : degradedFlags.Count > 0 ? ResearchStageStatus.Degraded
            : ResearchStageStatus.Completed;
        stage.CompletedAt = DateTime.UtcNow;
        stage.Summary = $"{outputs.Count} outputs, {degradedFlags.Count} degraded";
        if (degradedFlags.Count > 0) stage.DegradedFlagsJson = JsonSerializer.Serialize(degradedFlags);
        if (failed) stage.StopReason = "Role failure with required tools";
        await _dbContext.SaveChangesAsync(cancellationToken);

        _eventBus.Publish(new ResearchEvent(
            failed ? ResearchEventType.StageFailed : ResearchEventType.StageCompleted,
            session.Id, turn.Id, stage.Id, null, null,
            $"Stage {stageDef.StageType} {stage.Status}", null, DateTime.UtcNow));

        return new StageResult(stage.Status, outputs, degradedFlags);
    }

    private async Task<DebateResult> RunDebateAsync(
        ResearchSession session, ResearchTurn turn, ResearchStageSnapshot stage,
        StageDefinition stageDef, IReadOnlyList<string> upstreamArtifacts,
        CancellationToken cancellationToken)
    {
        var allOutputs = new List<string>();
        var degradedFlags = new List<string>();

        for (var round = 0; round < MaxDebateRounds; round++)
        {
            var roundContext = new List<string>(upstreamArtifacts);
            roundContext.AddRange(allOutputs);

            var rr = await RunParallelAsync(session, turn, stage, stageDef.RoleIds, round, roundContext, cancellationToken);
            if (rr.Failed) return new DebateResult(allOutputs, degradedFlags, true);

            degradedFlags.AddRange(rr.DegradedFlags);
            allOutputs.AddRange(rr.Outputs);

            if (round > 0 && rr.Outputs.Count > 0)
            {
                var last = rr.Outputs[^1];
                if (last.Contains("CONVERGED", StringComparison.OrdinalIgnoreCase) ||
                    last.Contains("收敛", StringComparison.Ordinal))
                {
                    _logger.LogInformation("Debate {StageType} converged at round {Round}", stageDef.StageType, round + 1);
                    break;
                }
            }
        }

        return new DebateResult(allOutputs, degradedFlags, false);
    }

    private async Task<ParallelResult> RunParallelAsync(
        ResearchSession session, ResearchTurn turn, ResearchStageSnapshot stage,
        IReadOnlyList<string> roleIds, int runIndex,
        IReadOnlyList<string> upstreamArtifacts,
        CancellationToken cancellationToken)
    {
        // Phase 1: Create RoleState records sequentially (DbContext is NOT thread-safe)
        var roleStates = new List<ResearchRoleState>();
        var execContexts = new List<RoleExecutionContext>();

        foreach (var roleId in roleIds)
        {
            var roleState = new ResearchRoleState
            {
                StageId = stage.Id,
                RoleId = roleId,
                RunIndex = runIndex,
                Status = ResearchRoleStatus.Running,
                StartedAt = DateTime.UtcNow
            };
            _dbContext.ResearchRoleStates.Add(roleState);
            roleStates.Add(roleState);

            execContexts.Add(new RoleExecutionContext(
                session.Id, turn.Id, stage.Id, session.Symbol, roleId,
                turn.UserPrompt, upstreamArtifacts));
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Phase 2: Execute roles in parallel (IO-bound LLM + MCP calls, no DbContext access)
        var executionTasks = execContexts.Select(ctx =>
            _roleExecutor.ExecuteRoleAsync(ctx, cancellationToken));
        var results = await Task.WhenAll(executionTasks);

        // Phase 3: Persist results sequentially
        var outputs = new List<string>();
        var degradedFlags = new List<string>();
        var failed = false;

        for (var i = 0; i < results.Length; i++)
        {
            var r = results[i];
            var roleState = roleStates[i];

            roleState.Status = r.Status;
            roleState.OutputContentJson = r.OutputContentJson;
            roleState.LlmTraceId = r.LlmTraceId;
            roleState.DegradedFlagsJson = r.DegradedFlags.Count > 0 ? JsonSerializer.Serialize(r.DegradedFlags) : null;
            roleState.ErrorCode = r.ErrorCode;
            roleState.ErrorMessage = r.ErrorMessage;
            roleState.CompletedAt = DateTime.UtcNow;

            if (r.Status == ResearchRoleStatus.Failed) failed = true;
            if (r.Status == ResearchRoleStatus.Degraded) degradedFlags.AddRange(r.DegradedFlags);
            if (r.OutputContentJson is not null) outputs.Add($"[{r.RoleId}]\n{r.OutputContentJson}");
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ParallelResult(outputs, degradedFlags, failed);
    }

    private async Task<RoleExecutionResult> ExecuteAndPersistRoleAsync(
        ResearchSession session, ResearchTurn turn, ResearchStageSnapshot stage,
        string roleId, int runIndex, IReadOnlyList<string> upstreamArtifacts,
        CancellationToken cancellationToken)
    {
        var roleState = new ResearchRoleState
        {
            StageId = stage.Id,
            RoleId = roleId,
            RunIndex = runIndex,
            Status = ResearchRoleStatus.Running,
            StartedAt = DateTime.UtcNow
        };
        _dbContext.ResearchRoleStates.Add(roleState);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var context = new RoleExecutionContext(
            session.Id, turn.Id, stage.Id, session.Symbol, roleId,
            turn.UserPrompt, upstreamArtifacts);

        var result = await _roleExecutor.ExecuteRoleAsync(context, cancellationToken);

        roleState.Status = result.Status;
        roleState.OutputContentJson = result.OutputContentJson;
        roleState.LlmTraceId = result.LlmTraceId;
        roleState.DegradedFlagsJson = result.DegradedFlags.Count > 0 ? JsonSerializer.Serialize(result.DegradedFlags) : null;
        roleState.ErrorCode = result.ErrorCode;
        roleState.ErrorMessage = result.ErrorMessage;
        roleState.CompletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task CreateDecisionSnapshotAsync(
        ResearchSession session, ResearchTurn turn,
        string outputContent, CancellationToken cancellationToken)
    {
        try
        {
            // Strip [roleId]\n prefix if present from stage output collection
            var jsonStart = outputContent.IndexOf('{');
            if (jsonStart > 0)
                outputContent = outputContent[jsonStart..];

            using var doc = JsonDocument.Parse(outputContent);
            var root = doc.RootElement;
            string? innerContent = null;
            if (root.TryGetProperty("content", out var c))
                innerContent = c.GetString();

            using var decDoc = innerContent is not null ? JsonDocument.Parse(innerContent) : null;
            var dec = decDoc?.RootElement ?? root;

            var rating = dec.TryGetProperty("rating", out var r) ? r.GetString() : null;
            var action = dec.TryGetProperty("action", out var a) ? a.GetString() : null;
            var summary = dec.TryGetProperty("executive_summary", out var s) ? s.GetString() : null;
            var confidence = dec.TryGetProperty("confidence", out var cv) && cv.TryGetDecimal(out var d) ? d : (decimal?)null;

            var decision = new ResearchDecisionSnapshot
            {
                SessionId = session.Id, TurnId = turn.Id,
                Rating = rating, Action = action, ExecutiveSummary = summary,
                FinalDecisionJson = outputContent, Confidence = confidence,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.ResearchDecisionSnapshots.Add(decision);
            session.LatestRating = rating;
            session.LatestDecisionHeadline = summary?.Length > 200 ? summary[..200] : summary;
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse decision, storing raw");
            _dbContext.ResearchDecisionSnapshots.Add(new ResearchDecisionSnapshot
            {
                SessionId = session.Id, TurnId = turn.Id,
                FinalDecisionJson = outputContent, CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task PersistFeedItemsAsync(long turnId, CancellationToken cancellationToken)
    {
        var events = _eventBus.Drain(turnId);
        foreach (var evt in events)
        {
            _dbContext.ResearchFeedItems.Add(new ResearchFeedItem
            {
                TurnId = evt.TurnId,
                StageId = evt.StageId,
                RoleId = evt.RoleId,
                ItemType = MapEventToFeedType(evt.EventType),
                Content = evt.Summary,
                MetadataJson = evt.DetailJson,
                TraceId = evt.TraceId,
                CreatedAt = evt.Timestamp
            });
        }
        if (events.Count > 0)
            await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ResearchFeedItemType MapEventToFeedType(ResearchEventType t) => t switch
    {
        ResearchEventType.RoleStarted or ResearchEventType.RoleSummaryReady or
        ResearchEventType.RoleCompleted or ResearchEventType.RoleFailed => ResearchFeedItemType.RoleMessage,
        ResearchEventType.ToolDispatched or ResearchEventType.ToolProgress or
        ResearchEventType.ToolCompleted => ResearchFeedItemType.ToolEvent,
        ResearchEventType.StageStarted or ResearchEventType.StageCompleted or
        ResearchEventType.StageFailed => ResearchFeedItemType.StageTransition,
        ResearchEventType.DegradedNotice => ResearchFeedItemType.DegradedNotice,
        ResearchEventType.TurnFailed => ResearchFeedItemType.ErrorNotice,
        _ => ResearchFeedItemType.SystemNotice
    };

    private sealed record StageDefinition(ResearchStageType StageType, ResearchStageExecutionMode ExecutionMode, IReadOnlyList<string> RoleIds);
    private sealed record StageResult(ResearchStageStatus Status, List<string> Outputs, List<string> DegradedFlags);
    private sealed record DebateResult(List<string> Outputs, List<string> DegradedFlags, bool Failed);
    private sealed record ParallelResult(List<string> Outputs, List<string> DegradedFlags, bool Failed);
}
