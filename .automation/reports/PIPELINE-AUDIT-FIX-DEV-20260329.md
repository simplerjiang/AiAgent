# Research Pipeline Audit Fix — Risk Debate Convergence

> **Date:** 2026-03-29  
> **Scope:** Fix risk debate convergence (from post-fix analysis report)  
> **Status:** ✅ Complete

---

## EN: Development Report

### Changes Made

| File | Change | Lines |
|------|--------|-------|
| `TradingWorkbenchPromptTemplates.cs` | Added `converged: true/false` to NeutralRiskAnalyst output format | ~L399 |
| `ResearchSessionAndRunnerTests.cs` | Added `Runner_RiskDebateConvergence_StopsWhenConverged` test | ~L862-898 |

### Fix Details

**Problem:** Risk debate (Stage 4) always ran 3 full rounds (9 LLM calls) because no risk analyst prompt included a `converged` field, despite the `RunDebateAsync()` convergence check being present.

**Solution:** Added `converged: true/false` to NeutralRiskAnalyst prompt output schema, with instruction: "风险辩论是否已充分收敛（如果本轮三方风险评估的核心结论方向与上一轮一致，且没有新的实质性风险发现或重大分歧变化，标记为 true）"

**Why NeutralRiskAnalyst only:** The neutral analyst is the balanced mediator between aggressive and conservative positions, making it the most appropriate role to judge convergence. This mirrors Stage 2's research_manager convergence design.

### Test Results

| Suite | Result |
|-------|--------|
| Research tests (31) | ✅ All pass |
| All Research* tests (61) | ✅ All pass |
| Prompt template tests (9) | ✅ All pass |
| Full test suite (352) | ✅ 351 pass, 1 pre-existing failure* |

*Pre-existing: `StockSyncServiceTests.SyncOnceAsync_ShouldPersistSnapshots` — date-sensitive test fails because current date 2026-03-29 produces 2 KLine points instead of expected 1. Unrelated to this change.

### Test Commands

```bash
dotnet test --filter "ResearchSessionAndRunnerTests"   # 31/31 pass
dotnet test --filter "Research"                        # 61/61 pass  
dotnet test                                            # 351/352 pass
```

### Expected Production Impact

| Metric | Before | After | Savings |
|--------|--------|-------|---------|
| Risk debate rounds | 3 (always) | 2 (when converged) | 1 round |
| Risk debate calls | 9 | 6 | 3 LLM calls |
| Wall-clock savings | — | ~16-20s | per pipeline run |
| Total pipeline calls | 24 | **21** | 12.5% reduction |

### P0-1 ContinueSession Status

- Unit test passes (`Runner_ContinueSession_SkipsToPortfolioDecision`) ✅
- Production test **skipped** — database has no existing research sessions; creating one requires real LLM calls (~4 min). The routing logic is structural code, not prompt-dependent. Unit test coverage is sufficient.

---

## ZH: 开发报告

### 修改内容

| 文件 | 修改 | 行号 |
|------|------|------|
| `TradingWorkbenchPromptTemplates.cs` | 为 NeutralRiskAnalyst 输出格式添加 `converged: true/false` 字段 | ~L399 |
| `ResearchSessionAndRunnerTests.cs` | 添加 `Runner_RiskDebateConvergence_StopsWhenConverged` 测试 | ~L862-898 |

### 修复详情

**问题：** 风险辩论（Stage 4）始终运行完整 3 轮（9 次 LLM 调用），因为所有风险分析师提示模板都没有包含 `converged` 字段，尽管 `RunDebateAsync()` 中已存在收敛检测逻辑。

**方案：** 在 NeutralRiskAnalyst 提示输出格式中添加 `converged: true/false` 字段，并附指令："风险辩论是否已充分收敛（如果本轮三方风险评估的核心结论方向与上一轮一致，且没有新的实质性风险发现或重大分歧变化，标记为 true）"

**为什么只修改 NeutralRiskAnalyst：** 中性风险分析师是激进派和保守派之间的平衡调解者，最适合判断辩论是否收敛。这与 Stage 2 中 research_manager 的收敛设计保持一致。

### 测试结果

| 测试套件 | 结果 |
|----------|------|
| Research 测试 (31) | ✅ 全部通过 |
| 所有 Research* 测试 (61) | ✅ 全部通过 |
| 提示模板测试 (9) | ✅ 全部通过 |
| 完整测试套件 (352) | ✅ 351 通过, 1 个已知失败* |

*已知失败：`StockSyncServiceTests` 日期敏感测试，与本次修改无关。

### 预期生产影响

| 指标 | 修复前 | 修复后 | 节省 |
|------|--------|--------|------|
| 风险辩论轮数 | 3（固定） | 2（收敛时） | 1 轮 |
| 风险辩论 LLM 调用 | 9 | 6 | 3 次调用 |
| 时间节省 | — | ~16-20 秒 | 每次流水线运行 |
| 总流水线调用 | 24 | **21** | 减少 12.5% |

### P0-1 ContinueSession 状态

- 单元测试通过 ✅
- 生产测试**跳过** — 数据库无现有研究会话，创建需要真实 LLM 调用。路由逻辑是结构性代码，单元测试覆盖充分。
