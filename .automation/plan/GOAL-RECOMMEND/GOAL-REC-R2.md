# GOAL-REC-R2: 后端编排引擎

> **前置**: P0 完成
> **交付**: RecommendationRunner 5 阶段状态机 + RoleExecutor + API 端点 + SSE 事件流

## 任务清单

### R2-1: RecommendStageType 流水线定义
**位置**: `backend/.../Services/Recommend/RecommendStageDefinitions.cs`

定义 5 阶段流水线拓扑:
```
Stage 1: MarketScan     → Parallel(MacroAnalyst, SectorHunter, SmartMoneyAnalyst)
Stage 2: SectorDebate   → Debate(SectorBull, SectorBear, SectorJudge) maxRounds=3
Stage 3: StockPicking   → Parallel(LeaderPicker, GrowthPicker) → Sequential(ChartValidator)
Stage 4: StockDebate    → Debate(StockBull, StockBear) → Sequential(RiskReviewer)
Stage 5: FinalDecision  → Sequential(Director)
```

每阶段的输入/输出 artifact 映射、并行/串行/辩论模式。

### R2-2: RecommendationRoleExecutor
**位置**: `backend/.../Services/Recommend/RecommendationRoleExecutor.cs`

- 实现 `IRecommendationRoleExecutor`
- 比 Research 版更简单：无硬编码工具权限，全量注册工具 function calling schema
- 工具调用预算: 最多 5 次
- 输入: 角色 Prompt + 上游 artifact JSON + 全量工具 schema
- 输出: 结构化 JSON（由 Prompt 约束 schema）
- 超限处理: 第 5 次调用后注入"请立即输出你的结论"
- 复用 `ILlmService.ChatAsync` 的 function calling 能力
- 每次工具调用发 EventBus 事件

### R2-3: RecommendationRunner 5 阶段编排器
**位置**: `backend/.../Services/Recommend/RecommendationRunner.cs`

核心方法:
```csharp
public interface IRecommendationRunner
{
    Task RunTurnAsync(long turnId, CancellationToken ct = default);
}
```

职责:
- 从 DB 加载 Turn + Session
- 按阶段拓扑顺序执行
- 并行阶段: Task.WhenAll
- 辩论阶段: Bull → Bear → Judge，最多 3 轮
- 每阶段创建 StageSnapshot、每角色创建 RoleState
- 每步发 EventBus 事件（StageStarted, RoleStarted, RoleCompleted, StageCompleted...）
- 上游 artifact 透传给下游（通过 StageSnapshot.Summary / RoleState.OutputContentJson）
- 异常处理: 单角色失败 → 标记 Degraded 继续；整阶段失败 → 标记 Stage Failed

### R2-4: RecommendationSessionService
**位置**: `backend/.../Services/Recommend/RecommendationSessionService.cs`

```csharp
public interface IRecommendationSessionService
{
    Task<RecommendationSession> CreateSessionAsync(string userPrompt, CancellationToken ct);
    Task<RecommendationSession> GetSessionDetailAsync(long sessionId, CancellationToken ct);
    Task<IReadOnlyList<RecommendationSession>> ListSessionsAsync(int page, int pageSize, CancellationToken ct);
    Task<RecommendationTurn> SubmitFollowUpAsync(long sessionId, string userPrompt, CancellationToken ct);
}
```

### R2-5: API 端点注册
**位置**: 在 `StocksModule.cs` 或新建 `RecommendModule.cs` 中注册

| 方法 | 路由 | 说明 |
|------|------|------|
| POST | `/api/recommend/sessions` | 创建新推荐会话，启动 RunTurnAsync |
| GET | `/api/recommend/sessions/{id}` | 获取会话详情（含报告、阶段快照、Feed） |
| POST | `/api/recommend/sessions/{id}/follow-up` | 追问（走 Router → 部分/全量重跑） |
| GET | `/api/recommend/sessions/{id}/events` | SSE 事件流（复用 EventBus Peek/Drain） |
| GET | `/api/recommend/sessions` | 历史列表（分页） |

### R2-6: DI 注册
在 `StocksModule.AddStocksServices` 中注册:
```csharp
services.AddScoped<IRecommendationSessionService, RecommendationSessionService>();
services.AddScoped<IRecommendationRoleExecutor, RecommendationRoleExecutor>();
services.AddScoped<IRecommendationRunner, RecommendationRunner>();
```

### R2-7: SSE 事件流端点
- 复用 `IResearchEventBus` 架构
- 或创建独立的 `IRecommendEventBus`（如果事件类型差异大）
- GET `/api/recommend/sessions/{id}/events` → text/event-stream
- 前端通过 EventSource 消费

### R2-8: 单元测试
- Runner 5 阶段顺序执行测试（mock RoleExecutor）
- 并行阶段 3 角色同时执行测试
- 辩论阶段多轮测试
- 单角色失败 → Degraded 继续测试
- Session 创建/查询/列表基本 CRUD 测试
- SSE 事件流发送/消费测试

## 验收标准
- [ ] POST `/api/recommend/sessions` 可创建会话并触发 5 阶段执行
- [ ] GET `/api/recommend/sessions/{id}` 返回完整会话含阶段快照和角色输出
- [ ] GET `/api/recommend/sessions/{id}/events` SSE 流实时推送进度
- [ ] 并行阶段确实并发执行
- [ ] 辩论阶段 Bull→Bear→Judge 有序执行
- [ ] 单角色失败不阻塞整体流水线
- [ ] 单元测试全部通过
