# GOAL-REC-P0: 编译恢复与规格冻结

> **优先级**: 最高（阻塞所有后续任务）
> **前置**: 无
> **交付**: 项目可编译 + 实体/枚举冻结 + 角色常量 + Schema 初始化器

## 现状
- AppDbContext.cs 第 60-64 行已声明 5 个 DbSet（RecommendationSession/Turn/StageSnapshot/RoleState/FeedItem）
- AppDbContext.cs 第 520-560 行已配置 EF Fluent API（索引、枚举转字符串、级联删除）
- Program.cs 第 115 行引用 `RecommendSessionSchemaInitializer.EnsureAsync(dbContext)`
- **以上引用的实体类和初始化器均不存在，项目无法编译**

## 任务清单

### P0-1: 创建 Recommendation 实体文件（参照 Research 同构）
**位置**: `backend/SimplerJiangAiAgent.Api/Data/Entities/`

| 文件 | 参照 | 关键字段 |
|------|------|----------|
| `RecommendationSession.cs` | ResearchSession | Id, SessionKey, Status(enum), ActiveTurnId, LastUserIntent, MarketSentiment, TopSectorsJson, CreatedAt, UpdatedAt, Turns navigation |
| `RecommendationTurn.cs` | ResearchTurn | Id, SessionId, TurnIndex, UserPrompt, Status(enum), ContinuationMode(enum), RoutingDecision/Reasoning/Confidence, RequestedAt, StartedAt, CompletedAt, StageSnapshots nav, FeedItems nav |
| `RecommendationStageSnapshot.cs` | ResearchStageSnapshot | Id, TurnId, StageType(enum:5阶段), StageRunIndex, ExecutionMode(enum), Status(enum), ActiveRoleIdsJson, Summary, StartedAt, CompletedAt, RoleStates nav |
| `RecommendationRoleState.cs` | ResearchRoleState | Id, StageId, RoleId, RunIndex, Status(enum), ToolPolicyClass, InputRefsJson, OutputRefsJson, OutputContentJson, ErrorCode/Message, LlmTraceId, StartedAt, CompletedAt |
| `RecommendationFeedItem.cs` | ResearchFeedItem | Id, TurnId, StageId, RoleId, ItemType(enum), Content, MetadataJson, TraceId, CreatedAt |

**枚举定义**（写在各自实体文件顶部）:
```
RecommendSessionStatus: Idle, Running, Degraded, Completed, Failed, Closed
RecommendTurnStatus: Draft, Queued, Running, Completed, Failed, Cancelled
RecommendContinuationMode: NewSession, PartialRerun, FullRerun, WorkbenchHandoff, DirectAnswer
RecommendStageType: MarketScan, SectorDebate, StockPicking, StockDebate, FinalDecision
RecommendStageStatus: Pending, Running, Completed, Degraded, Failed, Skipped
RecommendStageExecutionMode: Sequential, Parallel, Debate
RecommendRoleStatus: Pending, Running, Completed, Degraded, Failed, Skipped
RecommendFeedItemType: RoleMessage, ToolEvent, StageTransition, SystemNotice, UserFollowUp, DegradedNotice, ErrorNotice
```

### P0-2: 创建 RecommendSessionSchemaInitializer
**位置**: `backend/SimplerJiangAiAgent.Api/Modules/Stocks/Services/Recommend/RecommendSessionSchemaInitializer.cs`

参照现有 Research schema initializer 模式，使用 `dbContext.Database.ExecuteSqlRawAsync` 确保表存在。
需确保 5 张表全部创建：RecommendationSessions, RecommendationTurns, RecommendationStageSnapshots, RecommendationRoleStates, RecommendationFeedItems。

### P0-3: 创建 RecommendAgentRoleIds 常量类
**位置**: `backend/SimplerJiangAiAgent.Api/Modules/Stocks/Services/Recommend/RecommendAgentRoleIds.cs`

```csharp
public static class RecommendAgentRoleIds
{
    // Stage 1: 市场扫描
    public const string MacroAnalyst = "recommend_macro_analyst";
    public const string SectorHunter = "recommend_sector_hunter";
    public const string SmartMoneyAnalyst = "recommend_smart_money";
    // Stage 2: 板块辩论
    public const string SectorBull = "recommend_sector_bull";
    public const string SectorBear = "recommend_sector_bear";
    public const string SectorJudge = "recommend_sector_judge";
    // Stage 3: 选股精选
    public const string LeaderPicker = "recommend_leader_picker";
    public const string GrowthPicker = "recommend_growth_picker";
    public const string ChartValidator = "recommend_chart_validator";
    // Stage 4: 个股辩论
    public const string StockBull = "recommend_stock_bull";
    public const string StockBear = "recommend_stock_bear";
    public const string RiskReviewer = "recommend_risk_reviewer";
    // Stage 5: 决策
    public const string Director = "recommend_director";
    // Router
    public const string FollowUpRouter = "recommend_router";
}
```

### P0-4: 验证编译通过
```
dotnet build backend\SimplerJiangAiAgent.Api\SimplerJiangAiAgent.Api.csproj
```
必须 0 error。

## 验收标准
- [ ] `dotnet build` 0 error
- [ ] 5 个实体文件存在且枚举完整
- [ ] RecommendSessionSchemaInitializer 存在且 Program.cs 引用可解析
- [ ] RecommendAgentRoleIds 含 14 个角色常量
- [ ] AppDbContext 中已有的 Fluent API 配置与新实体兼容
