# ISSUE-20260303 DEV PHASE2 CONTINUED (EN + ZH)

## EN
### Scope
- Continue unfinished items of ISSUE-20260302 / Problem 2.
- Complete consistency engine and market-state stabilization for Commander output.

### Implemented Changes
1. Commander context enhancement
- File: `backend/SimplerJiangAiAgent.Api/Modules/Stocks/Services/StockAgentOrchestrator.cs`
- Added recent K-line payload (`last 30`) into Commander-only context for deterministic state evaluation.

2. Consistency + state-machine guardrails
- Added `StockAgentCommanderConsistencyGuardrails.Apply(...)` and invoked it in both normal parse and repair parse paths.
- Implemented:
  - Multi-timeframe fusion (1D/1W/1M from `trend_analysis.timeframeSignals`)
  - Divergence flagging (`consistency.status = 分歧态` when short/mid conflict)
  - Market state classification (`延续/震荡/反转`) from MA relationship on K-lines
  - Hysteresis mechanism: suppress direction flip when confidence is low and no strong counter-evidence
  - Strong counter-evidence override (invalidations rules)
  - Auto-fill revision reason when direction changes but reason missing

3. Prompt/schema + normalization updates
- Commander prompt and repair schema now require:
  - `consistency` object
  - `marketState` object
- Normalizer now guarantees default values for the two objects.

4. Unit tests
- Updated:
  - `backend/SimplerJiangAiAgent.Api.Tests/StockAgentPromptBuilderTests.cs`
  - `backend/SimplerJiangAiAgent.Api.Tests/StockAgentResultNormalizerTests.cs`
- Added:
  - `backend/SimplerJiangAiAgent.Api.Tests/StockAgentCommanderConsistencyGuardrailsTests.cs`
- Covered cases:
  - Timeframe conflict -> divergence output
  - Low-confidence direction change -> hysteresis applied
  - Strong counter-evidence -> direction change allowed with override reason

### Test Commands & Results
1. Focused tests
- Command:
  - `dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj --filter "StockAgentPromptBuilderTests|StockAgentResultNormalizerTests|StockAgentCommanderConsistencyGuardrailsTests"`
- Result:
  - Passed: 11
  - Failed: 0

2. Full backend tests
- Command:
  - `dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj`
- Result:
  - Passed: 44
  - Failed: 0

### Remaining
- Scenario replay metric for Problem 2 acceptance item A:
  - “No unexplained revision ratio approaches 0” still pending replay verification.

---

## ZH
### 范围
- 继续完成 ISSUE-20260302 的问题2未完成项。
- 完成指挥者输出的一致性约束与状态稳定机制。

### 已完成改动
1. Commander 上下文增强
- 文件：`backend/SimplerJiangAiAgent.Api/Modules/Stocks/Services/StockAgentOrchestrator.cs`
- 仅在 Commander 上下文中新增最近 30 条 K 线，用于确定性状态判定。

2. 一致性引擎 + 状态机守护
- 新增 `StockAgentCommanderConsistencyGuardrails.Apply(...)`，并在正常解析与 repair 解析两条路径都执行。
- 已实现：
  - 多周期融合（读取 `trend_analysis.timeframeSignals` 的 1D/1W/1M）
  - 短中周期冲突时输出分歧态（`consistency.status=分歧态`）
  - 基于 K 线均线关系判定市场状态（延续/震荡/反转）
  - 滞后机制：低置信度且无强反证时抑制方向翻转
  - 强反证覆盖：根据 invalidations 触发覆盖
  - 改判缺少原因时自动补齐 revision.reason

3. Prompt/Schema 与 Normalizer
- Commander Prompt 与 Repair Schema 增加并强制：
  - `consistency`
  - `marketState`
- Normalizer 增加默认值兜底，确保字段稳定存在。

4. 单元测试
- 已更新：
  - `backend/SimplerJiangAiAgent.Api.Tests/StockAgentPromptBuilderTests.cs`
  - `backend/SimplerJiangAiAgent.Api.Tests/StockAgentResultNormalizerTests.cs`
- 已新增：
  - `backend/SimplerJiangAiAgent.Api.Tests/StockAgentCommanderConsistencyGuardrailsTests.cs`
- 覆盖场景：
  - 多周期冲突 -> 分歧态
  - 低置信度改判 -> 触发滞后机制
  - 强反证存在 -> 允许改判并记录覆盖原因

### 测试命令与结果
1. 定向测试
- 命令：
  - `dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj --filter "StockAgentPromptBuilderTests|StockAgentResultNormalizerTests|StockAgentCommanderConsistencyGuardrailsTests"`
- 结果：11/11 通过

2. 后端全量测试
- 命令：
  - `dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj`
- 结果：44/44 通过

### 剩余项
- 问题2验收 A（“无解释改判比例趋近0”）尚需场景回放统计验证。
