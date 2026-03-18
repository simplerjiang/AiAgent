# GOAL-AGENT-001 Stock Copilot Detailed Design Draft (2026-03-17)

## EN
### 1. Objective
Build a Stock Copilot that behaves like a controlled domain agent rather than a single-shot report generator.

The system should support two major interaction modes:
1. Preset workflows: one-click report generation similar to the current multi-agent flow, such as fundamental judgment, price-trend outlook, entry timing, resistance/support levels, take-profit/stop-loss suggestions, and tactical overnight recommendations.
2. Freeform workflows: user-defined prompts that let the agent decide which tools to call, which evidence to gather, and how much analysis is needed before giving a structured answer.

The target is not a fully autonomous trader. The target is a Local-First, evidence-traceable, tool-using stock decision copilot.

### 2. Product Positioning
The future Stock Copilot should unify three roles that are currently split or underdeveloped:
1. Research copilot: investigate a stock, sector, or market condition by actively gathering local facts and controlled external facts.
2. Tactical copilot: translate research into actionable but bounded outputs such as observation, trial entry, add, reduce, hold overnight, avoid, watch for breakout, or wait for confirmation.
3. Workflow copilot: help users create todo items, draft strategy suggestions, generate trading-plan drafts, and revisit them when new information arrives.

### 3. Core Design Principles
1. Local-First before external search.
2. Tool-using agent before giant prompt stuffing.
3. Hard-coded stop policy before model-owned stop policy.
4. Evidence traceability before fluent narrative.
5. Structured outputs before free-form opinions.
6. Strategy assistance, not auto-trading.
7. Multi-step reasoning allowed, but only through observable tool traces.

### 4. High-Level Architecture
The Stock Copilot should evolve from the current orchestrator into a layered agent runtime.

Recommended layers:
1. Entry Layer
   - UI preset actions
   - Freeform user prompt entry
   - Admin/dev workflow entry
2. Task Router
   - Classify task type
   - Pick preset plan or freeform agent plan
   - Choose standard vs deep analysis mode
3. Planner
   - Propose next tool call
   - State what information is still missing
   - Suggest whether the task is ready to conclude
4. Governor
   - Enforce Local-First policy
   - Enforce budgets, permissions, stop policy, and retry rules
   - Reject unsafe or redundant tool calls
5. Tool Executor
   - Execute MCP-style tools
   - Return structured outputs
6. Evidence Normalizer
   - Convert tool outputs into unified evidence objects and feature objects
7. Readiness Evaluator
   - Decide if the task needs more data, can conclude cautiously, can conclude normally, or must degrade and stop
8. Commander / Final Synthesizer
   - Produce final structured answer
   - Never invent upstream evidence
9. Trace & Review Layer
   - Persist tool traces, evidence usage, stop reason, and final output

### 5. Interaction Modes
#### 5.1 Preset Workflow Mode
Preset workflows are fixed task templates with fixed tool preferences, fixed stop policy, and fixed output contract.

Examples:
1. Fundamental report
2. Trend outlook report
3. Entry timing report
4. Support/resistance report
5. Overnight tactical recommendation
6. Sector leadership scan
7. Market regime scan
8. Trade-plan draft generation

Preset mode is ideal for:
1. Stable UI buttons
2. Repeatable testing
3. Historical calibration
4. Fast runtime

#### 5.2 Freeform Prompt Mode
The user provides a custom task such as:
1. Judge whether this stock has entered an early up-cycle.
2. Compare three semiconductor stocks and recommend the strongest overnight setup.
3. Read the latest announcements and tell me if this invalidates my existing trade plan.

Freeform mode still uses the same tool catalog, same governor, same stop policy, and same structured trace. Only the planner becomes more flexible.

### 6. Task Type Taxonomy
The router should classify incoming tasks into one of these families:
1. Stock analysis
2. Sector analysis
3. Market analysis
4. Cross-stock scan
5. Overnight tactical selection
6. Strategy drafting
7. Trade-plan review
8. Evidence verification
9. External-web augmentation
10. Developer/debug trace review

Each task family should map to:
1. Required minimum tools
2. Optional tools
3. Stop policy profile
4. Output schema

### 7. MCP / Tool Catalog Design
The system should expose a domain tool catalog similar in spirit to GitHub Copilot tools, but controlled and stock-specific.

#### 7.1 Market Tools
1. `GetMarketSnapshot`
   - Return current market regime snapshot, breadth, stage, and key indices.
2. `GetMarketHistory`
   - Return last N daily market snapshots.
3. `GetMainlineSectors`
   - Return leading sectors, continuation scores, diffusion, and recent rank changes.
4. `GetMarketNews`
   - Return curated market news evidence from local facts.

#### 7.2 Sector Tools
1. `GetSectorRotation`
2. `GetSectorTrend`
3. `GetSectorNews`
4. `GetSectorLeaders`
5. `ScanSectorCandidates`

#### 7.3 Stock Data Tools
1. `GetStockQuote`
2. `GetStockDetailCache`
3. `GetStockKLines`
4. `GetStockMinuteLines`
5. `GetStockMessages`
6. `GetStockCompanyProfile`
7. `GetStockSignals`
8. `GetPositionGuidance`

#### 7.4 Indicator / Feature Tools
These should be produced by code, not by LLM arithmetic.

1. `GetTrendFeatures`
   - MA slope
   - MACD state
   - RSI
   - ATR
   - volatility percentile
   - breakout structure
   - support/resistance levels
2. `GetMomentumFeatures`
3. `GetValuationFeatures`
4. `GetFinancialFeatures`
5. `GetSectorRelativeStrength`
6. `GetHistoricalOutcomeFeatures`

#### 7.5 Local Fact Tools
1. `GetLocalStockNews`
2. `GetLocalSectorReports`
3. `GetLocalMarketReports`
4. `GetAnnouncements`
5. `GetResearchReports`
6. `GetLocalFactPackage`

#### 7.6 Article Reading Tools
1. `FetchArticleMetadata`
2. `FetchArticleFullText`
3. `GetArticleSummary`
4. `ExtractKeyParagraphs`

These tools should emit evidence objects with read status fields.

#### 7.7 External Web Tools
These are the closest analog to GitHub Copilot web fetch.

1. `SearchExternalWeb`
2. `FetchExternalPage`
3. `SearchTrustedSourcesOnly`
4. `ValidateExternalSource`

External tools must be gated by policy:
1. Use only when local evidence is insufficient.
2. Prefer trusted source allowlist.
3. Fetch page content through the backend, not directly by model browsing.

#### 7.8 Workflow / Productivity Tools
1. `CreateTodo`
2. `UpdateTodo`
3. `DraftTradingPlan`
4. `CreateStrategySuggestion`
5. `ListOpenPlans`
6. `ReviewExistingPlan`

These are the analogs to todo/task tools in coding agents.

#### 7.9 Review / Developer Tools
1. `GetAnalysisHistory`
2. `GetCommanderHistory`
3. `GetReplaySample`
4. `GetCalibrationMetrics`
5. `GetAgentTrace`

### 8. Tool Output Contracts
All tool outputs should be normalized into a small number of canonical structures.

#### 8.1 Evidence Object
Required fields:
1. `source`
2. `publishedAt`
3. `url`
4. `title`
5. `excerpt`
6. `readMode`
7. `readStatus`
8. `ingestedAt`

Optional fields:
1. `localFactId`
2. `sourceRecordId`
3. `articleSummary`
4. `fullTextHash`
5. `relevanceScore`
6. `trustTier`

#### 8.2 Feature Object
1. `featureName`
2. `value`
3. `unit`
4. `computedAt`
5. `sourceWindow`
6. `quality`

#### 8.3 Task State Object
1. `taskType`
2. `userIntent`
3. `symbols`
4. `sectorCodes`
5. `mode`
6. `toolBudget`
7. `coverageScore`
8. `traceabilityScore`
9. `conflictScore`
10. `degradedFlags`
11. `evidence[]`
12. `features[]`
13. `toolTrace[]`

### 9. Planner / Governor / Stop Policy Design
#### 9.1 Planner Role
The LLM planner should answer only:
1. What is missing?
2. What tool is the next best action?
3. Why is this tool needed?
4. Is current data enough for a low-confidence or normal-confidence conclusion?

The planner must not own final stop authority.

#### 9.2 Governor Role
The governor is hard-coded local logic.
It decides:
1. Whether a tool call is allowed
2. Whether it is redundant
3. Whether local data must be used first
4. Whether budget remains
5. Whether the agent must stop now

#### 9.3 Stop Policy
Stop should be based on hard-coded policy plus readiness scoring.

Suggested statuses:
1. `need_more_data`
2. `ready_low_confidence`
3. `ready_normal`
4. `forced_stop_degraded`

Suggested hard stop conditions:
1. Tool budget exhausted
2. Time budget exhausted
3. Consecutive failures exceeded threshold
4. Required data unavailable after fallback sequence

Suggested readiness dimensions:
1. `coverageScore`
2. `freshnessScore`
3. `traceabilityScore`
4. `conflictScore`
5. `featureCompletenessScore`
6. `degradedPenaltyScore`

Example stop rule:
1. If hard stop triggered -> `forced_stop_degraded`
2. Else if coverage below threshold -> continue
3. Else if traceability too low -> only `ready_low_confidence`
4. Else if conflict too high -> `ready_low_confidence`
5. Else -> `ready_normal`

### 10. Agent Runtime Loop
Recommended loop:
1. Parse user task
2. Load minimum local context
3. Run readiness evaluator
4. If ready, conclude
5. If not ready, ask planner for next action
6. Governor validates the action
7. Execute tool
8. Normalize tool output
9. Merge into task state
10. Repeat until stop policy ends the loop
11. Commander summarizes

Pseudo-flow:

```text
User Request
-> Task Router
-> Init Task State
-> Minimum Local Context Loader
-> Readiness Evaluator
-> Planner Suggestion
-> Governor Approval
-> Tool Execution
-> Evidence / Feature Normalization
-> Readiness Re-evaluation
-> Commander Final Output
-> Trace Persisted
```

### 11. Preset Workflow Designs
#### 11.1 Stock Fundamental Report
Required tools:
1. `GetStockDetailCache`
2. `GetCompanyProfile`
3. `GetFinancialFeatures`
4. `GetAnnouncements`
5. `GetArticleSummary` for critical filings

Output:
1. Fundamental quality
2. Valuation view
3. Earnings outlook
4. Major risks
5. Evidence table

#### 11.2 Stock Price Outlook Report
Required tools:
1. `GetStockQuote`
2. `GetStockKLines`
3. `GetTrendFeatures`
4. `GetSectorRelativeStrength`
5. `GetLocalStockNews`

Output:
1. Multi-horizon direction
2. Bull/base/bear distribution
3. Support/resistance
4. Key invalidation levels

#### 11.3 Entry Timing Report
Required tools:
1. `GetTrendFeatures`
2. `GetMinuteLines`
3. `GetSignals`
4. `GetSectorRotation`

Output:
1. Entry readiness
2. Better pullback zone / breakout zone
3. Confirmation checklist
4. Risk warning

#### 11.4 Overnight Tactical Recommendation
Target: recommend candidate stocks for next-day tactical monitoring.

Required process:
1. Scan market regime
2. Identify mainline sectors
3. Pull sector leaders and second-line candidates
4. Run stock-level tactical analysis on candidates
5. Rank by evidence quality, trend readiness, and risk asymmetry

Output:
1. Recommended overnight watchlist
2. Why chosen
3. What invalidates overnight hold bias
4. Next-day trigger checklist

### 12. Freeform Workflow Design
Freeform tasks should still be constrained by templates.

Suggested task template fields:
1. `intent`
2. `scope`
3. `targetSymbols`
4. `riskPreference`
5. `timeHorizon`
6. `analysisDepth`
7. `toolPolicy`

The planner uses the freeform prompt only to derive tool needs. The final output must still map to a structured schema appropriate to the task family.

### 13. Sector and Market Scanning Workflows
#### 13.1 Sector Agent
The sector agent should be capable of:
1. Searching sector strength
2. Identifying continuation vs exhaustion
3. Pulling leaders and lagging catch-up names
4. Ranking sectors for overnight tactical value

Suggested process:
1. Use `GetMainlineSectors`
2. Use `GetSectorTrend`
3. Use `GetSectorNews`
4. Use `GetSectorLeaders`
5. Generate sector candidate list

#### 13.2 Market Agent
The market agent should determine:
1. Regime stage
2. Risk appetite
3. Breadth and continuation
4. Whether tactical overnight selection should be aggressive, selective, or defensive

### 14. Strategy Suggestion System
The system should support strategy suggestions without auto-execution.

Suggested capabilities:
1. Draft a strategy hypothesis
2. Draft a trade plan
3. Create todo items for later verification
4. Generate watch conditions
5. Generate invalidation conditions

Example outputs:
1. Swing continuation setup
2. Sector rotation breakout setup
3. Earnings rebound setup
4. Mean reversion watch setup

### 15. Todo and Task System
To mirror GitHub Copilot task coordination, the Stock Copilot should maintain a lightweight task board per analysis session.

Todo examples:
1. Read latest earnings preview
2. Verify whether sector trend improved in last 3 trading days
3. Compare candidate A vs candidate B
4. Review existing pending trade plan

Todo objects should include:
1. `id`
2. `title`
3. `status`
4. `owner`
5. `sourceTaskId`
6. `createdAt`

### 16. Output Schema Design
Each final result should be structured.

Suggested universal fields:
1. `taskType`
2. `summary`
3. `analysisOpinion`
4. `confidenceScore`
5. `probabilityDistribution`
6. `keyDrivers`
7. `counterEvidence`
8. `triggerConditions`
9. `invalidationConditions`
10. `riskLimits`
11. `keyLevels`
12. `recommendedAction`
13. `evidence[]`
14. `toolTraceSummary`
15. `stopReason`

### 17. Multi-User / UI Surfaces
Recommended UI surfaces:
1. Stock Copilot panel in stock detail page
2. Sector Copilot tab
3. Market Copilot tab
4. Overnight Tactics workspace
5. Strategy Draft panel
6. Developer trace view

### 18. Observability and Audit
Every session should persist:
1. user request
2. task classification
3. tool calls in order
4. normalized evidence
5. readiness snapshots
6. stop reason
7. final result

This is mandatory if the product wants to claim agent-like reasoning.

### 19. Security and Safety Boundaries
1. No direct auto-trading.
2. No free unlimited web search.
3. No unbounded recursive tool use.
4. No high-confidence output without traceable evidence.
5. No external search result enters final reasoning without normalization.

### 20. Performance Profiles
Recommended modes:
1. Fast mode
   - Low tool budget
   - Mostly local data
   - Short report
2. Standard mode
   - Balanced tool budget
   - Local-first plus optional external fallback
3. Deep mode
   - More article reads
   - More cross-stock comparison
   - Higher latency tolerated

### 21. Suggested Implementation Order
1. Finish GOAL-AGENT-001-R1.
2. Finish GOAL-AGENT-001-R2.
3. Add internal tool runtime before full MCP protocol work.
4. Add planner/governor/readiness evaluator loop.
5. Add preset stock workflows.
6. Add freeform workflow.
7. Add sector/market scanning workflows.
8. Add todo/strategy draft tools.
9. Add replay calibration and acceptance baseline.
10. Expose the final tool catalog as MCP-compatible server endpoints.

### 22. Why This Is Better Than Current Full-Context Prompting
1. Lower token waste
2. Better observability
3. Better evidence control
4. Better reuse of local structured data
5. Easier calibration and replay
6. More natural multi-step reasoning

### 23. Main Risks
1. Latency growth
2. Tool explosion and schema complexity
3. Harder testing than current orchestrator mode
4. Planner loops if stop policy is weak
5. External web contamination if governor is weak

### 24. Final Recommendation
Do not jump directly from the current orchestrator to a fully open autonomous agent.

Build in three stages:
1. Toolized internal agent runtime
2. Controlled planner-governor loop
3. MCP-compatible external tool layer

That path preserves Local-First safety while still giving the product a true Stock Copilot experience.

## ZH
### 1. 目标
构建一个真正意义上的 Stock Copilot。它不再只是“一次性把上下文打包发给模型”的报告生成器，而是一个受控、可追溯、会按需调用工具的股票分析 Agent。

系统需要支持两种主要交互模式：
1. 预设工作流：类似当前一键生成多份报告，例如基本面判断、趋势预测、入场时机、支撑阻力、止盈止损建议、隔夜战术推荐等。
2. 自由提示词工作流：用户自己输入任务，让 Agent 自行决定要调用哪些工具、补哪些信息、什么时候停止并给出结构化结论。

目标不是自动交易系统，而是一个 Local-First、证据可追溯、可调用工具的决策辅助 Copilot。

### 2. 产品定位
未来的 Stock Copilot 应统一三类能力：
1. 研究 Copilot：围绕个股、板块、大盘主动调取本地事实与受控外部事实。
2. 战术 Copilot：把研究结果翻译成可执行但有边界的建议，例如观察、试仓、加仓、减仓、隔夜持有、等待确认。
3. 工作流 Copilot：帮助用户创建 todo、起草策略建议、生成交易计划草稿、并在新信息到来时复核已有计划。

### 3. 核心设计原则
1. Local-First 优先于外网搜索。
2. 工具型 Agent 优先于大 prompt 一次性灌输。
3. 停止条件由本地硬规则主导，不由模型独占。
4. 证据可追溯优先于文案流畅。
5. 结构化输出优先于自由发挥。
6. 做策略辅助，不做自动下单。
7. 允许多步推理，但必须留下可观察的工具调用轨迹。

### 4. 总体架构
建议从当前 orchestrator 升级为分层 Agent Runtime。

推荐层次：
1. 入口层
   - UI 预设动作入口
   - 自由提示词入口
   - 管理员/开发者入口
2. 任务路由层
   - 判断任务类型
   - 选择预设流程或自由流程
   - 选择标准/深度模式
3. Planner 层
   - 提出下一步应该调用哪个工具
   - 说明当前还缺什么
   - 给出“是否接近可收尾”的建议
4. Governor 层
   - 强制执行 Local-First、预算、权限、停止条件、重试策略
   - 拒绝不合规或重复调用
5. Tool Executor 层
   - 真正执行 MCP 风格工具
6. Evidence Normalizer 层
   - 把工具结果统一转成 evidence object 和 feature object
7. Readiness Evaluator 层
   - 判断当前是否还需要继续查、是否只能低置信度结束、是否可正常结束、是否必须降级结束
8. Commander 层
   - 只负责最终综合输出
   - 不得凭空发明上游证据
9. Trace / Review 层
   - 持久化工具调用轨迹、证据使用情况、停止原因和最终结果

### 5. 交互模式设计
#### 5.1 预设工作流模式
适合稳定按钮、稳定输出、稳定测试。

可先落地的预设：
1. 个股基本面报告
2. 个股趋势预测报告
3. 入场时机报告
4. 支撑/阻力位报告
5. 隔夜战术推荐
6. 板块领涨与补涨扫描
7. 大盘阶段判断
8. 交易计划草稿生成

#### 5.2 自由提示词模式
用户直接提任务，例如：
1. 判断这只票是不是刚进入上涨周期。
2. 比较三只半导体股票，推荐最适合隔夜持有的那一只。
3. 读一下最新公告，判断是否足以推翻我现有的交易计划。

自由模式仍然必须走同一套工具目录、同一套 governor、同一套 stop policy 和 trace 结构，只是 planner 更灵活。

### 6. 任务类型体系
建议先把任务路由成以下类型之一：
1. 个股分析
2. 板块分析
3. 大盘分析
4. 跨股票扫描
5. 隔夜战术选股
6. 策略起草
7. 交易计划复核
8. 证据核验
9. 外网补证
10. 开发者/调试回放

每类任务都应绑定：
1. 最小必需工具
2. 可选工具
3. 停止条件模板
4. 输出 schema

### 7. MCP / 工具目录设计
这个目录要像 GitHub Copilot 的工具系统，但必须是受控且股票领域专用。

#### 7.1 大盘工具
1. `GetMarketSnapshot`
2. `GetMarketHistory`
3. `GetMainlineSectors`
4. `GetMarketNews`

#### 7.2 板块工具
1. `GetSectorRotation`
2. `GetSectorTrend`
3. `GetSectorNews`
4. `GetSectorLeaders`
5. `ScanSectorCandidates`

#### 7.3 个股数据工具
1. `GetStockQuote`
2. `GetStockDetailCache`
3. `GetStockKLines`
4. `GetStockMinuteLines`
5. `GetStockMessages`
6. `GetStockCompanyProfile`
7. `GetStockSignals`
8. `GetPositionGuidance`

#### 7.4 指标 / 特征工具
这些指标必须由代码算，不由 LLM 临场硬算。

1. `GetTrendFeatures`
   - MA 斜率
   - MACD 状态
   - RSI
   - ATR
   - 波动率分位
   - 突破结构
   - 支撑阻力位
2. `GetMomentumFeatures`
3. `GetValuationFeatures`
4. `GetFinancialFeatures`
5. `GetSectorRelativeStrength`
6. `GetHistoricalOutcomeFeatures`

#### 7.5 本地事实工具
1. `GetLocalStockNews`
2. `GetLocalSectorReports`
3. `GetLocalMarketReports`
4. `GetAnnouncements`
5. `GetResearchReports`
6. `GetLocalFactPackage`

#### 7.6 正文阅读工具
1. `FetchArticleMetadata`
2. `FetchArticleFullText`
3. `GetArticleSummary`
4. `ExtractKeyParagraphs`

这些工具的输出必须统一转成 evidence object，并带阅读状态。

#### 7.7 外网工具
这是最接近 GitHub Copilot web fetch 的部分。

1. `SearchExternalWeb`
2. `FetchExternalPage`
3. `SearchTrustedSourcesOnly`
4. `ValidateExternalSource`

外网工具必须受以下策略约束：
1. 只有本地证据不足时才允许调用。
2. 优先信任源白名单。
3. 页面抓取通过后端完成，不能让模型自由浏览网页。

#### 7.8 工作流 / 效率工具
1. `CreateTodo`
2. `UpdateTodo`
3. `DraftTradingPlan`
4. `CreateStrategySuggestion`
5. `ListOpenPlans`
6. `ReviewExistingPlan`

这些就是你提到的类似 GitHub Copilot 里的 todo / task 工具。

#### 7.9 开发 / 复盘工具
1. `GetAnalysisHistory`
2. `GetCommanderHistory`
3. `GetReplaySample`
4. `GetCalibrationMetrics`
5. `GetAgentTrace`

### 8. 工具输出统一 contract
所有工具输出都应该归一到少量核心结构。

#### 8.1 Evidence Object
必选字段：
1. `source`
2. `publishedAt`
3. `url`
4. `title`
5. `excerpt`
6. `readMode`
7. `readStatus`
8. `ingestedAt`

可选字段：
1. `localFactId`
2. `sourceRecordId`
3. `articleSummary`
4. `fullTextHash`
5. `relevanceScore`
6. `trustTier`

#### 8.2 Feature Object
1. `featureName`
2. `value`
3. `unit`
4. `computedAt`
5. `sourceWindow`
6. `quality`

#### 8.3 Task State Object
1. `taskType`
2. `userIntent`
3. `symbols`
4. `sectorCodes`
5. `mode`
6. `toolBudget`
7. `coverageScore`
8. `traceabilityScore`
9. `conflictScore`
10. `degradedFlags`
11. `evidence[]`
12. `features[]`
13. `toolTrace[]`

### 9. Planner / Governor / Stop Policy 设计
#### 9.1 Planner 角色
LLM planner 只回答：
1. 当前缺什么信息？
2. 下一步最值得调用哪个工具？
3. 为什么调用？
4. 现在是否足够输出低置信或正常置信结论？

planner 不能拥有最终停止权。

#### 9.2 Governor 角色
Governor 是本地 hard code。
它负责判断：
1. 这个工具调用是否允许
2. 是否重复
3. 是否应该先查本地再查外网
4. 预算是否足够
5. 现在是否应该停止

#### 9.3 停止条件设计
停止应由本地 stop policy + readiness 评分共同决定。

建议状态：
1. `need_more_data`
2. `ready_low_confidence`
3. `ready_normal`
4. `forced_stop_degraded`

建议硬停止条件：
1. 工具预算耗尽
2. 时延预算耗尽
3. 连续失败超过阈值
4. 关键数据源经 fallback 后仍不可用

建议 readiness 维度：
1. `coverageScore`
2. `freshnessScore`
3. `traceabilityScore`
4. `conflictScore`
5. `featureCompletenessScore`
6. `degradedPenaltyScore`

示例 stop rule：
1. 硬停止命中 -> `forced_stop_degraded`
2. coverage 不足 -> 继续查
3. traceability 太低 -> 只能 `ready_low_confidence`
4. conflict 太高 -> 只能 `ready_low_confidence`
5. 其余达标 -> `ready_normal`

### 10. Agent 工具循环设计
建议循环如下：
1. 解析用户任务
2. 加载最小本地上下文
3. 运行 readiness evaluator
4. 如果可以结束，则直接进入 commander
5. 如果不能结束，则 planner 提出下一步动作
6. governor 审核动作
7. executor 执行工具
8. normalizer 归一化
9. 合并进 task state
10. 继续下一轮
11. commander 最终收尾

推荐流程图：

```text
用户请求
-> Task Router
-> Init Task State
-> Minimum Local Context Loader
-> Readiness Evaluator
-> Planner Suggestion
-> Governor Approval
-> Tool Execution
-> Evidence / Feature Normalization
-> Readiness Re-evaluation
-> Commander Final Output
-> Trace Persisted
```

### 11. 预设工作流设计
#### 11.1 个股基本面报告
必需工具：
1. `GetStockDetailCache`
2. `GetCompanyProfile`
3. `GetFinancialFeatures`
4. `GetAnnouncements`
5. 关键公告场景下的 `GetArticleSummary`

输出：
1. 基本面质量判断
2. 估值判断
3. 盈利展望
4. 风险点
5. 证据列表

#### 11.2 个股趋势预测报告
必需工具：
1. `GetStockQuote`
2. `GetStockKLines`
3. `GetTrendFeatures`
4. `GetSectorRelativeStrength`
5. `GetLocalStockNews`

输出：
1. 多周期方向
2. bull/base/bear 概率分布
3. 支撑阻力
4. 关键失效位

#### 11.3 入场时机报告
必需工具：
1. `GetTrendFeatures`
2. `GetMinuteLines`
3. `GetSignals`
4. `GetSectorRotation`

输出：
1. 当前是否接近可入场
2. 更合理的回踩区 / 突破区
3. 确认条件清单
4. 风险提示

#### 11.4 隔夜战术推荐
目标：推荐第二天值得重点盯盘的战术股票。

建议流程：
1. 扫描大盘 regime
2. 找主线板块
3. 拉板块龙头与补涨候选
4. 对候选做个股级战术分析
5. 按证据质量、趋势 readiness、风险收益比排序

输出：
1. 隔夜候选池
2. 入选原因
3. 失效条件
4. 次日观察 checklist

### 12. 自由工作流设计
自由任务不能裸奔，仍要套模板。

建议模板字段：
1. `intent`
2. `scope`
3. `targetSymbols`
4. `riskPreference`
5. `timeHorizon`
6. `analysisDepth`
7. `toolPolicy`

planner 只用自由提示词来推导工具需求；最终输出仍必须映射到结构化 schema。

### 13. 板块与大盘扫描型工作流
#### 13.1 板块 Agent
它应具备：
1. 搜索板块强弱
2. 判断延续还是衰竭
3. 找龙头和补涨标的
4. 给隔夜战术价值排序

建议流程：
1. `GetMainlineSectors`
2. `GetSectorTrend`
3. `GetSectorNews`
4. `GetSectorLeaders`
5. 输出板块候选池

#### 13.2 大盘 Agent
它应判断：
1. 当前阶段
2. 风险偏好
3. 广度和延续性
4. 今天隔夜策略该偏进攻、偏精选还是偏防守

### 14. 策略建议系统
系统应支持策略建议，但不直接执行。

建议能力：
1. 起草策略假设
2. 起草交易计划
3. 创建后续核验 todo
4. 生成观察条件
5. 生成失效条件

例子：
1. 趋势延续策略
2. 板块轮动突破策略
3. 业绩修复博弈策略
4. 均值回归观察策略

### 15. Todo / Task 系统
为了接近 GitHub Copilot 的任务协作体验，Stock Copilot 应维护轻量任务板。

Todo 例子：
1. 阅读最新业绩预告
2. 验证板块 3 日持续性是否增强
3. 比较候选股 A 与 B
4. 复核现有 Pending 交易计划

Todo 字段建议：
1. `id`
2. `title`
3. `status`
4. `owner`
5. `sourceTaskId`
6. `createdAt`

### 16. 最终输出 schema
每个最终结果都应结构化。

建议通用字段：
1. `taskType`
2. `summary`
3. `analysisOpinion`
4. `confidenceScore`
5. `probabilityDistribution`
6. `keyDrivers`
7. `counterEvidence`
8. `triggerConditions`
9. `invalidationConditions`
10. `riskLimits`
11. `keyLevels`
12. `recommendedAction`
13. `evidence[]`
14. `toolTraceSummary`
15. `stopReason`

### 17. UI 形态
建议至少有这些 UI 面：
1. 个股页 Stock Copilot 面板
2. 板块 Copilot 页签
3. 大盘 Copilot 页签
4. 隔夜战术工作台
5. 策略建议草稿面板
6. 开发者 trace 面板

### 18. 可观测与审计
每次会话至少持久化：
1. 用户请求
2. 任务分类
3. 工具调用顺序
4. evidence 归一化结果
5. readiness 快照
6. stop reason
7. 最终结果

如果未来要对外声称“这是 Agent 推理”，这层是必须的。

### 19. 安全与边界
1. 不允许自动下单
2. 不允许无限外网搜索
3. 不允许无限递归调用工具
4. 没有可回溯 evidence 不允许高置信输出
5. 外网结果未经归一化不得直接进入最终推理

### 20. 性能档位
建议提供三档：
1. Fast
   - 小预算
   - 主要用本地数据
   - 简短报告
2. Standard
   - 平衡预算
   - Local-First + 外网 fallback
3. Deep
   - 更多正文阅读
   - 更多跨股票比较
   - 更高延迟容忍

### 21. 建议实施顺序
1. 先完成 GOAL-AGENT-001-R1
2. 再完成 GOAL-AGENT-001-R2
3. 先做内部 tool runtime，再做正式 MCP 协议层
4. 再做 planner / governor / readiness evaluator 循环
5. 先落地预设个股工作流
6. 再落地自由工作流
7. 再落地板块 / 大盘扫描
8. 再加 todo / 策略建议工具
9. 再做 replay 校准闭环
10. 最后把工具目录包装成 MCP-compatible server

### 22. 为什么它比当前“大上下文一次性发给模型”更好
1. 更省 token
2. 更强可观测性
3. 更强证据控制
4. 更能复用本地结构化数据
5. 更容易 replay 和校准
6. 更像真实研究流程

### 23. 主要风险
1. 延迟会上升
2. 工具数量和 schema 复杂度会提升
3. 测试难度高于当前 orchestrator
4. stop policy 弱时会出现 planner 死循环
5. governor 弱时会引入外网污染

### 24. 最终建议
不要从当前 orchestrator 一步跳成完全开放式 автономous agent。

更稳的路线是三阶段：
1. 内部工具化 Agent Runtime
2. 受控 planner-governor 循环
3. MCP-compatible 外部工具层

这样既能保持 Local-First 安全边界，也能真正做出“像 Copilot 一样会按需取数和思考”的 Stock Copilot。