# ISSUE-20260303 Development Report (Phase 2 Partial) / 开发报告（问题2阶段性）

Date: 2026-03-03
Task: ISSUE-20260302
Stage: Dev + Test (Phase 2 Partial)

## EN (for agents)

### Scope
Start Issue 2 with same rhythm and land the highest-priority enforceable pieces:
1) Commander-only 3-7 day historical conclusion injection.
2) Mandatory revision structure for decision-change explanation.

### Code Changes
1. Commander-only history injection
- Updated `StockAgentOrchestrator` constructor to inject `IStockAgentHistoryService`.
- Added `BuildCommanderHistoryAsync` to load symbol history and build compact history package.
- Commander context now uses `SerializeCommanderContext(...)` with `CommanderHistory` payload.
- Non-commander agents continue using existing context serialization without history package.

2. History extraction policy (new)
- Added `StockAgentCommanderHistoryPolicy` with:
  - lookback clamp: 3..7 days (default 5),
  - max item clamp: 5..10 (default 8),
  - compact field extraction from historical commander result:
    direction, confidence, triggers, invalidations, riskLimits, evidence summary, summary.

3. Revision (change-reason) mandatory schema
- Updated commander prompt template to require comparison with latest historical conclusion.
- Added mandatory `revision` block in commander output schema:
  - `required` (bool), `reason` (string|null), `previousDirection` (string|null).
- Updated commander schema template in repair prompt.
- Updated normalizer to always ensure `revision` fields exist.

### Tests Added/Updated
- Updated `StockAgentPromptBuilderTests`:
  - assert commander prompt contains `revision` and historical guidance text.
- Updated `StockAgentResultNormalizerTests`:
  - assert commander normalized output includes `revision` defaults.
- Added `StockAgentCommanderHistoryPolicyTests`:
  - window/max clamp,
  - filtering by lookback,
  - commander field extraction correctness.

### Test Commands & Results
1) Focused tests
- `dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj --filter "StockAgentPromptBuilderTests|StockAgentResultNormalizerTests|StockAgentCommanderHistoryPolicyTests"`
- Result: PASS (11/11)

2) Full backend tests
- `dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj`
- Result: PASS (41/41)

### Remaining for full Issue-2 closure
- Multi-timeframe fusion & divergence-state labeling.
- State machine with hysteresis (continuation/range/reversal) and strong-counterevidence override behavior.
- Scenario replay metrics for no-unexplained-revision target.

---

## 中文（给用户）

### 本次范围
按同样节奏启动问题2，优先落地可强约束的两项：
1）仅指挥者读取近 3-7 天历史结论；
2）改判原因（revision）结构化强制输出。

### 代码改动
1. 指挥者专用历史注入
- 在 `StockAgentOrchestrator` 注入 `IStockAgentHistoryService`。
- 新增 `BuildCommanderHistoryAsync`，按标的读取历史并构建精简历史包。
- 指挥者上下文改为 `SerializeCommanderContext(...)`，包含 `CommanderHistory`。
- 非指挥者保持原上下文，不携带历史包。

2. 历史提取策略（新增）
- 新增 `StockAgentCommanderHistoryPolicy`：
  - 回看窗口限制 3..7 天（默认 5 天）；
  - 条数限制 5..10（默认 8 条）；
  - 从历史 commander 结果提取精简字段：
    方向、置信度、触发/失效/风险上限、证据摘要、总结。

3. 改判原因 revision 强制结构
- 指挥者 Prompt 增加“与最近一次结论比较”的硬约束。
- 输出 JSON 强制新增 `revision`：
  - `required`、`reason`、`previousDirection`。
- 修复提示模板与 normalizer 同步补齐 `revision` 默认字段。

### 新增/更新测试
- 更新 `StockAgentPromptBuilderTests`：验证 commander prompt 含 `revision` 与历史结论约束。
- 更新 `StockAgentResultNormalizerTests`：验证 commander 输出默认补齐 `revision`。
- 新增 `StockAgentCommanderHistoryPolicyTests`：验证窗口/条数限制与字段提取正确。

### 测试命令与结果
1）定向测试
- `dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj --filter "StockAgentPromptBuilderTests|StockAgentResultNormalizerTests|StockAgentCommanderHistoryPolicyTests"`
- 结果：通过（11/11）

2）后端全量测试
- `dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj`
- 结果：通过（41/41）

### 问题2剩余项
- 多周期融合与“分歧态”标注。
- 状态机与滞后机制（延续/震荡/反转 + 强反证覆盖先验）。
- 场景回放指标（无解释改判比例）闭环验证。
