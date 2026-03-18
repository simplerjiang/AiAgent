# GOAL-AGENT-001 Stop Policy / Readiness Evaluator v1 Rule Design (2026-03-17)

## EN
### 1. Objective
This document defines how the Stock Copilot decides whether it should continue gathering data, conclude cautiously, conclude normally, or stop in a degraded state.

The core position is unchanged:
1. The planner may recommend the next step.
2. The governor enforces policy and budgets.
3. The readiness evaluator decides whether the task is sufficiently prepared for conclusion.
4. Final stop authority belongs to local hard-coded logic, not to the LLM.

### 2. Goals and Non-Goals
Goals:
1. Prevent infinite or low-value planner loops.
2. Make conclusion quality auditable and replayable.
3. Force conservative degradation when evidence quality is weak.
4. Standardize stop behavior across preset and freeform workflows.

Non-goals:
1. Replacing commander reasoning.
2. Guaranteeing a correct market forecast.
3. Letting the model self-certify its own sufficiency.

### 3. Output Statuses
External statuses remain fixed at four:
1. `need_more_data`
2. `ready_low_confidence`
3. `ready_normal`
4. `forced_stop_degraded`

Internal sub-reasons should also be persisted:
1. `missing_required_slots`
2. `local_data_unavailable`
3. `budget_exhausted`
4. `time_exhausted`
5. `too_many_failures`
6. `traceability_too_low`
7. `conflict_too_high`
8. `feature_gap`
9. `external_search_denied`
10. `duplicate_call_denied`

### 4. Inputs to the Readiness Evaluator
The evaluator consumes a `TaskState` snapshot containing at least:
1. `taskType`
2. `mode`
3. `toolBudget`
4. `toolTrace[]`
5. `evidence[]`
6. `features[]`
7. `coverageScore`
8. `freshnessScore`
9. `traceabilityScore`
10. `conflictScore`
11. `featureCompletenessScore`
12. `degradedPenaltyScore`
13. `hardStopFlags[]`
14. `requiredSlots`
15. `filledSlots`

### 5. Required Slot Model
Readiness should not rely only on a blended score. Each task type must define mandatory information slots.

#### 5.1 Stock Analysis
Required slots:
1. `quote`
2. `trend_features`
3. `stock_evidence`
4. `sector_context`

Optional but recommended slots:
1. `position_guidance`
2. `valuation_features`
3. `article_summary`

#### 5.2 Sector Analysis
Required slots:
1. `mainline_context`
2. `sector_trend`
3. `sector_evidence`
4. `leaders`

#### 5.3 Market Analysis
Required slots:
1. `market_snapshot`
2. `market_history`
3. `market_news`

#### 5.4 Overnight Tactical Selection
Required slots:
1. `market_snapshot`
2. `mainline_sectors`
3. `candidate_list`
4. `candidate_stock_features`
5. `candidate_stock_evidence`

#### 5.5 Strategy Draft / Plan Review
Required slots:
1. `target_object`
2. `latest_evidence`
3. `risk_boundary`
4. `invalidation_basis`

### 6. Budget Model
Budgets are mode-dependent.

#### 6.1 Fast Mode
1. Max tool steps: 6
2. Max article full-text reads: 1
3. Max external calls: 0 by default, 1 only if explicitly enabled
4. Time budget: 8 seconds

#### 6.2 Standard Mode
1. Max tool steps: 10
2. Max article full-text reads: 2
3. Max external calls: 2
4. Time budget: 15 seconds

#### 6.3 Deep Mode
1. Max tool steps: 16
2. Max article full-text reads: 4
3. Max external calls: 4
4. Time budget: 35 seconds

### 7. Hard Stop Rules
If any hard stop fires, the external status must be `forced_stop_degraded`.

Hard stop conditions:
1. Step budget exhausted.
2. Time budget exhausted.
3. Consecutive failures exceed threshold.
4. Required local source unavailable after allowed fallback sequence.
5. Governor denies all remaining next actions as duplicate, unsafe, or out of budget.
6. Task scope expands beyond policy boundary.

Recommended thresholds:
1. Consecutive failures: 2 in fast mode, 3 in standard mode, 4 in deep mode.
2. Same tool repeated with same args after failure: max 1 retry unless degraded flag explicitly allows retry.

### 8. Score Definitions
All readiness dimensions use a 0-100 scale.

#### 8.1 Coverage Score
Measures whether required and useful slots are filled.

Formula:
```text
coverageScore = requiredCoverage * 0.7 + optionalCoverage * 0.3

requiredCoverage = filledRequiredSlots / totalRequiredSlots * 100
optionalCoverage = filledOptionalSlots / max(totalOptionalSlots, 1) * 100
```

Rule:
If any required slot is missing, `ready_normal` is impossible.

#### 8.2 Freshness Score
Measures whether the loaded evidence and data are recent enough for the task.

Suggested freshness buckets:
1. Intraday quote/minute data older than 5 minutes during trading hours: severe penalty.
2. Stock or sector news older than 72 hours: moderate penalty.
3. Market narrative older than 24 hours in overnight scan: moderate penalty.
4. Research report older than 30 days: minor penalty.

Formula:
```text
freshnessScore = 100 - weightedAgePenalty
```

#### 8.3 Traceability Score
Measures how much of the final conclusion can be backed by readable, attributable evidence.

Suggested weights:
1. `full_text_read + verified`: 1.0
2. `summary_only + verified`: 0.8
3. `metadata_only + verified`: 0.45
4. `partial`: 0.35
5. `unverified`: 0.15
6. `fetch_failed`: 0.0

Formula:
```text
traceabilityScore = weightedEvidenceSupport / max(expectedEvidenceSupport, 1) * 100
```

Hard gate:
If traceabilityScore < 55, `ready_normal` is forbidden.

#### 8.4 Conflict Score
Measures contradiction among evidence, deterministic features, and agent interpretations.

Conflict sources:
1. Evidence polarity disagreement.
2. Trend features bullish while sector/market regime is clearly defensive.
3. Commander direction contradicts majority of high-trust evidence.
4. Valuation/fundamental judgment conflicts with recent critical announcement.

Formula:
```text
conflictScore = min(100, weightedConflictPoints)
```

Interpretation:
1. `< 25`: low conflict
2. `25-49`: manageable conflict
3. `50-69`: high conflict
4. `>= 70`: severe conflict

Hard gate:
If conflictScore >= 50, `ready_normal` is forbidden unless the conclusion is explicitly neutral or wait-and-see.

#### 8.5 Feature Completeness Score
Measures whether code-computed features required by task type are present.

Formula:
```text
featureCompletenessScore = presentRequiredFeatureFamilies / totalRequiredFeatureFamilies * 100
```

Example required feature families by task:
1. Stock price outlook: trend, momentum, sector relative strength.
2. Fundamental report: financial, valuation.
3. Overnight selection: trend, momentum, sector relative strength, market regime.

#### 8.6 Degraded Penalty Score
Represents operational damage accumulated during the loop.

Penalty examples:
1. Tool timeout: +12
2. Parse failure repaired successfully: +6
3. External source rejected as untrusted: +8
4. Full-text fetch failed on critical article: +15
5. Duplicate-call denial: +4
6. Source contamination filtered: +5

Cap:
`degradedPenaltyScore` should be capped at 40 for calculation, but all raw events should still be logged.

### 9. Composite Readiness Index
The blended index is useful, but only after hard gates and slot gates.

Formula:
```text
baseReadiness =
  coverageScore * 0.30 +
  freshnessScore * 0.20 +
  traceabilityScore * 0.20 +
  (100 - conflictScore) * 0.15 +
  featureCompletenessScore * 0.15

effectiveReadiness = max(0, baseReadiness - degradedPenaltyScore)
```

Suggested interpretation:
1. `>= 78`: eligible for `ready_normal` if hard gates pass.
2. `60-77`: eligible only for `ready_low_confidence`.
3. `< 60`: continue if budget remains, else `forced_stop_degraded`.

### 10. Final Decision Rules
Apply rules in this order.

#### 10.1 Rule Layer 1: Hard Stops
If any hard stop is true -> `forced_stop_degraded`.

#### 10.2 Rule Layer 2: Required Slot Gate
If any required slot missing:
1. If budget remains and an allowed tool can fill it -> `need_more_data`.
2. Else -> `forced_stop_degraded`.

#### 10.3 Rule Layer 3: Traceability and Conflict Gates
1. If `traceabilityScore < 55` -> at most `ready_low_confidence`.
2. If `conflictScore >= 50` -> at most `ready_low_confidence`.
3. If `featureCompletenessScore < 60` on a feature-driven task -> at most `ready_low_confidence`.

#### 10.4 Rule Layer 4: Composite Threshold
1. If `effectiveReadiness >= 78` -> `ready_normal`.
2. Else if `effectiveReadiness >= 60` -> `ready_low_confidence`.
3. Else if budget remains -> `need_more_data`.
4. Else -> `forced_stop_degraded`.

### 11. Governor Denial Rules
The governor should deny a planned tool call when:
1. It violates Local-First order.
2. It is a duplicate within freshness window.
3. It exceeds remaining step, time, article-read, or external-call budget.
4. It broadens scope from one symbol to many without explicit task support.
5. It attempts external browsing when local insufficiency has not been established.
6. It retries a known dead source without new justification.

When denied, the denial itself increases `degradedPenaltyScore` modestly and is written to `toolTrace[]`.

### 12. Minimum Local Context Loader
Before planner autonomy begins, every task should load a minimum deterministic local base.

Examples:
1. Stock analysis: `GetStockQuote`, `GetTrendFeatures`, `GetLocalStockNews`, `GetSectorRelativeStrength`.
2. Sector analysis: `GetMainlineSectors`, `GetSectorTrend`, `GetSectorNews`.
3. Market analysis: `GetMarketSnapshot`, `GetMarketHistory`, `GetMarketNews`.
4. Plan review: `ReviewExistingPlan`, `GetLocalStockNews`, `GetAnnouncements`.

This avoids wasting planner steps on obvious first actions.

### 13. External Search Escalation Rules
External tools may be considered only when all conditions hold:
1. Required slot remains unfilled after local path.
2. Task type allows external augmentation.
3. External-call budget remains.
4. Trusted-source path is available or general web is explicitly allowed.
5. Governor records the insufficiency reason in the trace.

Even after successful external retrieval:
1. Unverified content cannot unlock `ready_normal` alone.
2. External evidence must be normalized with `trustTier`, `readMode`, and `readStatus`.

### 14. Planner Interaction Contract
The planner should not return free-form essays. It should return a small structured suggestion.

Suggested planner output:
```json
{
  "missingSlots": ["article_summary"],
  "recommendedTool": "GetArticleSummary",
  "reason": "Latest earnings announcement is critical and only metadata is available",
  "expectedGain": "increase traceability and reduce conflict",
  "canConcludeNow": false,
  "conclusionModeIfNow": "low_confidence"
}
```

The governor and readiness evaluator decide whether to accept that suggestion.

### 15. Examples
#### 15.1 Example A: Stock Outlook, Normal Conclusion
1. Required slots all filled.
2. Coverage 88.
3. Freshness 82.
4. Traceability 79.
5. Conflict 18.
6. Feature completeness 92.
7. Degraded penalty 6.
8. Effective readiness = 79.8.
9. Result: `ready_normal`.

#### 15.2 Example B: Overnight Scan With Conflicting Evidence
1. Required slots all filled.
2. Coverage 84.
3. Freshness 77.
4. Traceability 63.
5. Conflict 58.
6. Feature completeness 86.
7. Degraded penalty 8.
8. Composite would be above 60, but conflict gate blocks normal confidence.
9. Result: `ready_low_confidence`.

#### 15.3 Example C: Critical Announcement Only Has Metadata
1. Required stock evidence exists only as `metadata_only`.
2. Traceability 42.
3. Planner requests `FetchArticleFullText`.
4. Governor allows one article read.
5. If fetch fails and budget ends, result becomes `forced_stop_degraded`.

### 16. Persistence Requirements
Each readiness evaluation pass should persist:
1. input snapshot digest
2. all six scores
3. hard stop flags
4. missing required slots
5. decision status
6. decision reasons
7. next allowed tool families

This is necessary for replay calibration and debugging false confidence.

### 17. Rollout Plan
1. Implement score calculation without changing commander output yet.
2. Persist evaluator snapshots behind developer mode.
3. Turn on low-confidence and degraded gating for commander.
4. Make planner suggestions depend on missing slots instead of free-form context greed.
5. Add replay-based threshold tuning.

### 18. Validation Notes
Actions performed for this draft:
1. Converted the earlier stop-policy concept into explicit thresholds, gates, and formulas.
2. Kept the public status model aligned with the previously approved four-state design.
3. Added slot-based hard gates so the system does not over-trust blended scoring.

Validation command to run after writing:
1. Editor diagnostics on this markdown file.

Known limitation:
1. Threshold numbers are v1 defaults and should later be tuned by replay data rather than treated as permanent constants.

## ZH
### 1. 目标
这份文档定义 Stock Copilot 什么时候继续查，什么时候低置信收尾，什么时候正常收尾，什么时候必须降级停止。

核心立场不变：
1. planner 只负责建议下一步。
2. governor 负责预算、权限和调用审核。
3. readiness evaluator 负责判断“现在够不够收尾”。
4. 最终停止权在本地 hard code，不在 LLM。

### 2. 目标与非目标
目标：
1. 防止 planner 死循环或低价值循环。
2. 让结论质量可审计、可 replay。
3. 在证据质量不足时强制保守降级。
4. 让预设流程与自由流程都复用同一套 stop 规则。

非目标：
1. 不替代 commander 的综合表述。
2. 不保证预测一定正确。
3. 不允许模型自己宣布“我已经足够好了”。

### 3. 对外状态
对外状态固定为四个：
1. `need_more_data`
2. `ready_low_confidence`
3. `ready_normal`
4. `forced_stop_degraded`

内部还应保存更细的原因：
1. `missing_required_slots`
2. `local_data_unavailable`
3. `budget_exhausted`
4. `time_exhausted`
5. `too_many_failures`
6. `traceability_too_low`
7. `conflict_too_high`
8. `feature_gap`
9. `external_search_denied`
10. `duplicate_call_denied`

### 4. Readiness Evaluator 的输入
至少需要以下 `TaskState` 字段：
1. `taskType`
2. `mode`
3. `toolBudget`
4. `toolTrace[]`
5. `evidence[]`
6. `features[]`
7. `coverageScore`
8. `freshnessScore`
9. `traceabilityScore`
10. `conflictScore`
11. `featureCompletenessScore`
12. `degradedPenaltyScore`
13. `hardStopFlags[]`
14. `requiredSlots`
15. `filledSlots`

### 5. 必填槽位模型
Readiness 不能只看一个综合分。每类任务都要定义“最低必备信息槽位”。

#### 5.1 个股分析
必填槽位：
1. `quote`
2. `trend_features`
3. `stock_evidence`
4. `sector_context`

推荐槽位：
1. `position_guidance`
2. `valuation_features`
3. `article_summary`

#### 5.2 板块分析
必填槽位：
1. `mainline_context`
2. `sector_trend`
3. `sector_evidence`
4. `leaders`

#### 5.3 大盘分析
必填槽位：
1. `market_snapshot`
2. `market_history`
3. `market_news`

#### 5.4 隔夜战术选股
必填槽位：
1. `market_snapshot`
2. `mainline_sectors`
3. `candidate_list`
4. `candidate_stock_features`
5. `candidate_stock_evidence`

#### 5.5 策略起草 / 计划复核
必填槽位：
1. `target_object`
2. `latest_evidence`
3. `risk_boundary`
4. `invalidation_basis`

### 6. 预算模型
预算按模式区分。

#### 6.1 Fast
1. 最大工具步数：6
2. 最大正文阅读：1
3. 最大外网调用：默认 0，显式开启后 1
4. 时间预算：8 秒

#### 6.2 Standard
1. 最大工具步数：10
2. 最大正文阅读：2
3. 最大外网调用：2
4. 时间预算：15 秒

#### 6.3 Deep
1. 最大工具步数：16
2. 最大正文阅读：4
3. 最大外网调用：4
4. 时间预算：35 秒

### 7. 硬停止规则
只要任一硬停止命中，对外状态就必须是 `forced_stop_degraded`。

硬停止条件：
1. 步数预算耗尽。
2. 时间预算耗尽。
3. 连续失败次数超过阈值。
4. 关键本地数据源经过允许的 fallback 后仍不可用。
5. governor 对所有剩余动作都判定为重复、不安全或超预算。
6. 任务 scope 被 planner 非法放大。

推荐阈值：
1. 连续失败次数：Fast 2 次，Standard 3 次，Deep 4 次。
2. 同一工具同一参数失败后，最多允许 1 次重试，除非 degraded flag 明确允许再试。

### 8. 六个评分维度
所有维度统一为 0 到 100。

#### 8.1 Coverage Score
衡量必填与推荐槽位覆盖情况。

公式：
```text
coverageScore = requiredCoverage * 0.7 + optionalCoverage * 0.3

requiredCoverage = filledRequiredSlots / totalRequiredSlots * 100
optionalCoverage = filledOptionalSlots / max(totalOptionalSlots, 1) * 100
```

规则：
只要还有必填槽位缺失，就不可能进入 `ready_normal`。

#### 8.2 Freshness Score
衡量数据是否足够新。

建议惩罚：
1. 交易时段内，分时/行情超过 5 分钟未刷新：重罚。
2. 个股/板块资讯超过 72 小时：中罚。
3. 隔夜战术中的大盘叙事超过 24 小时：中罚。
4. 研报超过 30 天：轻罚。

#### 8.3 Traceability Score
衡量最终结论是否由“可读、可归因”的证据支撑。

建议权重：
1. `full_text_read + verified`: 1.0
2. `summary_only + verified`: 0.8
3. `metadata_only + verified`: 0.45
4. `partial`: 0.35
5. `unverified`: 0.15
6. `fetch_failed`: 0.0

硬闸门：
1. `traceabilityScore < 55` 时，不允许 `ready_normal`。

#### 8.4 Conflict Score
衡量 evidence、代码特征、综合结论之间的冲突程度。

冲突来源：
1. 资讯正负极性明显冲突。
2. 趋势特征偏多，但板块/大盘 clearly defensive。
3. commander 方向与高可信证据主流冲突。
4. 基本面判断与关键公告冲突。

解释区间：
1. `< 25`：低冲突
2. `25-49`：可管理
3. `50-69`：高冲突
4. `>= 70`：严重冲突

硬闸门：
1. `conflictScore >= 50` 时，除非结论是中性/等待，否则不能 `ready_normal`。

#### 8.5 Feature Completeness Score
衡量当前任务所需的代码特征族是否齐全。

例如：
1. 趋势报告至少需要趋势、动量、板块相对强弱。
2. 基本面报告至少需要财务和估值。
3. 隔夜选股至少需要趋势、动量、板块强弱、大盘状态。

#### 8.6 Degraded Penalty Score
表示本轮分析过程中积累的运行损伤。

建议罚分：
1. 工具超时：+12
2. 解析失败但修复成功：+6
3. 外部来源校验不通过：+8
4. 关键公告正文抓取失败：+15
5. 重复调用被拒：+4
6. 检测到污染并过滤：+5

上限：
1. 用于计算时建议封顶 40，但原始事件仍要完整落审计。

### 9. 综合 Readiness 指数
综合分只在硬闸门和槽位闸门通过后才有意义。

公式：
```text
baseReadiness =
  coverageScore * 0.30 +
  freshnessScore * 0.20 +
  traceabilityScore * 0.20 +
  (100 - conflictScore) * 0.15 +
  featureCompletenessScore * 0.15

effectiveReadiness = max(0, baseReadiness - degradedPenaltyScore)
```

建议解释：
1. `>= 78`：在闸门通过前提下可进入 `ready_normal`
2. `60-77`：最多 `ready_low_confidence`
3. `< 60`：若预算仍在则继续查，否则 `forced_stop_degraded`

### 10. 最终判定顺序
按下面顺序执行。

#### 10.1 第一层：硬停止
任一硬停止为真 -> `forced_stop_degraded`

#### 10.2 第二层：必填槽位
如果有必填槽位缺失：
1. 若预算仍在且存在允许的补槽工具 -> `need_more_data`
2. 否则 -> `forced_stop_degraded`

#### 10.3 第三层：可追溯与冲突闸门
1. `traceabilityScore < 55` -> 最多 `ready_low_confidence`
2. `conflictScore >= 50` -> 最多 `ready_low_confidence`
3. feature-driven 任务若 `featureCompletenessScore < 60` -> 最多 `ready_low_confidence`

#### 10.4 第四层：综合分阈值
1. `effectiveReadiness >= 78` -> `ready_normal`
2. `effectiveReadiness >= 60` -> `ready_low_confidence`
3. 否则若预算还在 -> `need_more_data`
4. 否则 -> `forced_stop_degraded`

### 11. Governor 拒绝规则
Governor 在以下情况应拒绝 planner 的工具调用：
1. 违反 Local-First 顺序。
2. freshness 窗口内重复调用。
3. 超过步数、时间、正文阅读或外网预算。
4. 无明确任务支持却把单票扩大成多票或扩大 scope。
5. 本地证据不足尚未成立，却试图直接走外网。
6. 已知死源无新理由却继续重试。

被拒绝本身也应记入 `toolTrace[]`，并对 `degradedPenaltyScore` 做小幅加罚。

### 12. 最小本地上下文加载器
在 planner 自由发挥前，每类任务都先自动加载最小本地上下文。

例如：
1. 个股分析：`GetStockQuote`、`GetTrendFeatures`、`GetLocalStockNews`、`GetSectorRelativeStrength`
2. 板块分析：`GetMainlineSectors`、`GetSectorTrend`、`GetSectorNews`
3. 大盘分析：`GetMarketSnapshot`、`GetMarketHistory`、`GetMarketNews`
4. 计划复核：`ReviewExistingPlan`、`GetLocalStockNews`、`GetAnnouncements`

这样可以避免 planner 把最 obvious 的首轮动作浪费成一轮决策步骤。

### 13. 外网升级条件
只有同时满足以下条件，才允许考虑外网工具：
1. 必填槽位经本地路径后仍未填满。
2. 当前任务类型允许外网补证。
3. 外网预算仍有余量。
4. 信任源路径可用，或明确允许一般搜索。
5. governor 在 trace 里写明“为何本地不足”。

即便外网抓取成功：
1. 仅靠 unverified 外部内容也不能解锁 `ready_normal`。
2. 外部结果仍必须归一化为带 `trustTier`、`readMode`、`readStatus` 的 evidence。

### 14. Planner 交互合同
Planner 不应返回长篇散文，而应返回小型结构化建议。

建议结构：
1. `missingSlots`
2. `recommendedTool`
3. `reason`
4. `expectedGain`
5. `canConcludeNow`
6. `conclusionModeIfNow`

是否执行这条建议，由 governor 和 readiness evaluator 决定。

### 15. 示例
#### 15.1 示例 A：个股趋势报告，正常收尾
1. 必填槽位齐全。
2. Coverage 88。
3. Freshness 82。
4. Traceability 79。
5. Conflict 18。
6. Feature completeness 92。
7. Degraded penalty 6。
8. Effective readiness = 79.8。
9. 结果：`ready_normal`。

#### 15.2 示例 B：隔夜选股，证据互相打架
1. 必填槽位齐全。
2. Coverage 84。
3. Freshness 77。
4. Traceability 63。
5. Conflict 58。
6. Feature completeness 86。
7. Degraded penalty 8。
8. 综合分虽然过了 60，但 conflict gate 阻止正常置信。
9. 结果：`ready_low_confidence`。

#### 15.3 示例 C：关键公告只有 metadata
1. 关键证据只有 `metadata_only`。
2. Traceability 42。
3. Planner 请求 `FetchArticleFullText`。
4. Governor 放行一次正文阅读。
5. 若抓取失败且预算耗尽，则结果为 `forced_stop_degraded`。

### 16. 持久化要求
每次 readiness 计算都应落库或落审计：
1. 输入快照摘要
2. 六个分数
3. 硬停止标记
4. 缺失槽位
5. 判定状态
6. 判定原因
7. 下一轮允许的工具家族

这是后续 replay 校准和排查“为什么它又自信过头”的基础。

### 17. 落地顺序
1. 先实现评分计算，但先不改变 commander 输出。
2. 在开发者模式持久化 evaluator 快照。
3. 打开 commander 的低置信和降级闸门。
4. 让 planner 基于 missing slots 做建议，而不是凭感觉贪上下文。
5. 再用 replay 数据微调阈值。

### 18. 本稿校验说明
本次动作：
1. 把上一版 stop-policy 概念稿落成显式阈值、闸门和公式。
2. 保持对外状态模型仍是已经确认的四态。
3. 增加基于槽位的硬闸门，避免系统误信综合分。

本稿建议校验：
1. 编辑器 markdown diagnostics。

当前限制：
1. 这些阈值只是 v1 默认值，后续必须由 replay 校准，而不是永久写死。