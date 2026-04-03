# GOAL-REC-R4: 追问路由器

> **前置**: R2 (Runner) + R3 (角色 Prompt)
> **交付**: FollowUpRouter + 部分重跑/全量重跑/Workbench 交接/直接回答

## 任务清单

### R4-1: RecommendFollowUpRouter
**位置**: `backend/.../Services/Recommend/RecommendFollowUpRouter.cs`

```csharp
public interface IRecommendFollowUpRouter
{
    Task<FollowUpPlan> RouteAsync(long sessionId, string userMessage, CancellationToken ct);
}
```

Router 是轻量快速的 LLM 调用（可用 IsPro=false 的小模型）。

### R4-2: FollowUpPlan DTO
```csharp
public record FollowUpPlan(
    string Intent,                          // 用户意图一句话
    FollowUpStrategy Strategy,              // partial_rerun / full_rerun / workbench_handoff / direct_answer
    IReadOnlyList<AgentInvocation> Agents,  // 需要执行的 Agent 列表
    FollowUpContextOverrides? Overrides,    // 限定板块/个股/时间窗
    string Reasoning                        // Router 决策推理
);

public enum FollowUpStrategy { PartialRerun, FullRerun, WorkbenchHandoff, DirectAnswer }

public record AgentInvocation(string RoleId, string? InputOverride, bool Required);

public record FollowUpContextOverrides(
    string[]? TargetSectors,
    string[]? TargetStocks,
    string? TimeWindow,
    string? AdditionalConstraints);
```

### R4-3: Router Prompt 模板
- 输入: 用户追问文本 + 当前推荐上下文摘要（板块列表、个股列表、最近辩论焦点）
- 输出: `FollowUpPlan` 结构化 JSON
- 必须包含 `reasoning` 字段以供审计
- 关键意图映射:
  - "XX 板块再选几只" → partial_rerun Stage 3
  - "换个方向" / "看消费/医药" → partial_rerun Stage 2-5
  - "XX 详细分析" → workbench_handoff
  - "重新推荐" → full_rerun
  - "为什么推荐 XX?" → direct_answer（从辩论记录提取）

### R4-4: 部分重跑执行
- Router 返回 `PartialRerun` 时
- 从指定阶段开始重跑，之前阶段复用已有 artifact
- 新建 Turn（TurnIndex + 1），保留 Session
- 只创建需要重跑的 Stage 的 StageSnapshot
- 修改 RoleExecutor 输入为 `上游已有 artifact + contextOverrides`

### R4-5: Workbench 交接
- Router 返回 `WorkbenchHandoff` 时
- 自动为目标个股创建 ResearchSession
- 返回前端跳转指令（含 sessionId）
- 前端收到后弹出确认 → 跳转到 Trading Workbench 页签

### R4-6: 直接回答
- Router 返回 `DirectAnswer` 时
- 不调用任何 Agent
- Router LLM 直接从 Session 辩论记录中提取答案
- 创建一个 FeedItem 记录

### R4-7: 单元测试
- partial_rerun 正确定位起始阶段测试
- full_rerun 全部 5 阶段重跑测试
- workbench_handoff 自动创建 ResearchSession 测试
- direct_answer 不调 Agent 的测试
- Router prompt 与 LLM 交互 mock 测试（验证意图分类准确）

## 验收标准
- [ ] POST `/api/recommend/sessions/{id}/follow-up` 可正确路由追问
- [ ] 部分重跑只执行指定阶段，复用上游 artifact
- [ ] Workbench 交接能正确创建 ResearchSession
- [ ] 直接回答不触发 Agent 执行
- [ ] Router reasoning 可审计
