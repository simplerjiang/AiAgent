# GOAL-018: 交易纪律闭环与胜率提升系统

> **版本**: v3.0  
> **日期**: 2026-04-01  
> **状态**: 首要目标  
> **定位**: 从“帮你看盘”升级到“帮你管住自己”——补齐交易闭环中最关键的纪律执行、复盘反馈与风险暴露可见性，引入 LLM 交易教练作为客观旁观者，并让持仓与资金状态流入每一个 AI 分析决策

---

## 一、问题诊断

当前系统已具备强大的**看盘与分析能力**：

- 多 Agent 结构化分析（GOAL-AGENT-001）
- K 线 / 分时 / 多策略信号叠加（GOAL-012-R3）
- 情绪轮动与市场阶段识别（GOAL-009）
- 交易计划引擎与触发 / 失效检测（GOAL-008）
- 本地资讯库与证据可追溯链路（GOAL-013）
- 回放校准基线与命中率指标（GOAL-AGENT-001-R3）

但**胜率低、信心不足**的根本原因不在于分析能力不够，而在于：

### 1. 没有闭环：做了计划但从不回头看执行结果

- 系统有 `TradingPlan`（计划）和 `TradingPlanEvent`（触发/失效事件），但**缺失最关键的一环**：交易执行记录。
- 用户建了"触发价 12.6 买入"的计划，但实际买了没有？买了多少？卖在哪里？盈亏多少？系统不知道。
- `ReplayCalibrationService` 算出的命中率是**纸面命中率**——只知道 AI 说对了几次方向，不知道用户真正赚了还是亏了。

### 2. 有分析但没有"该不该动手"的客观门槛

- Commander 给出 `analysis_opinion`、`confidence_score`、触发条件、失效条件。
- 但系统没有在用户做决策的时刻告诉他：**"过去 N 次类似信号的历史胜率是多少"**。
- `ReplayCalibrationService` 已经能算 `horizonMetrics`（1/3/5/10 日命中率），但这些数据没有在交易计划确认页面呈现。
- 结果：用户只看到"偏多，置信度 72"，但不知道这个置信度在历史上是否真的可信。

### 3. 没有仓位纪律的刚性约束

- 系统有 `SuggestedPositionScale`、`RiskLimits`，但全是建议，没有刚性。
- 专业交易员输的不是方向，是仓位：做对 10 次小仓位、做错 1 次重仓，照样亏。
- 用户无法一眼看到"我当前所有待执行计划的总预期仓位暴露是多少"。

### 4. 市场阶段识别已有，但没有和执行纪律挂钩

- 情绪轮动页能区分"主升 / 分歧 / 退潮 / 混沌"。
- 但这个信息没有刚性地影响执行行为：退潮期开新仓的胜率天然低，但系统不会阻止或提醒。
- 应该让市场阶段自动影响建议仓位和执行频率，而不是仅作为参考信息。

### 5. 没有"不做"的反馈机制

- 系统全在鼓励"分析更多、看更多、建更多计划"，但从不鼓励用户"今天别做"。
- 如果连续 3 个计划被失效（Invalid），系统不会主动提示"最近判断准确率下降"。
- 如果当日已触发 2 个计划，系统不会提示"今天已够活跃"。
### 6. 没有客观旁观者帮助反思

- 交易员最缺的不是信息，而是**有人旁观并指出盲点**。
- 系统不会结合市场环境、世界局势和用户行为，在收盘后告诉他“今天为什么亏了”或“这笔为什么赚了”。
- 没有定期的结构化总结机制，用户无法从历史中提炼规律、发现自己的行为模式偏差。
- **LLM 天然适合做这个“冷静的旁观者”角色——它不带情绪、不怕得罪人、能看全局数据。**
---

## 二、核心设计原则

> **交易胜率的提升，70% 来自纪律和风控，30% 来自分析准确度。**

本目标不增加新的分析能力，而是补齐"纪律执行 → 结果记录 → 复盘反馈 → 行为修正"的完整闭环。

| 原则 | 说明 |
|------|------|
| **闭环优先** | 没有执行记录的交易计划 = 没有做过。复盘的前提是知道做了什么。 |
| **数据驱动信心** | 用历史信号胜率替代"感觉"。在扣扳机之前看到客观数据。 |
| **刚性约束 > 柔性建议** | 仓位上限、市场阶段限速不是"建议"，而是视觉化的硬门槛。 |
| **管住自己 > 多看东西** | 交易行为模式反馈比更多分析信号更重要。 |
| **LLM 当旁观者** | 让 LLM 做冷静的复盘教练：结合市场全局数据、世界局势、新闻事件，客观归因每笔交易的赢亏原因，督促用户反思。 |
| **持仓即上下文** | 用户的本金、当前持仓、仓位占比必须作为每一个 LLM Agent 请求的标准上下文，让 AI 分析生来就知道用户的真实仓位状态。 |
| **手动录入即可** | 不接券商 API，不做自动下单。用户手动录入执行情况足矣。 |

---

## 三、功能拆解

### R1: 交易执行记录、做T支持与自动收益核算（P0）

> 没有闭环就永远不知道问题在哪。这是整个系统的数据基石。

#### 核心概念

- **交易执行（TradeExecution）**：每一笔买入或卖出，包括普通交易和做T操作。
- **做T（日内回转）**：A股 T+1 机制下，卖出昨日持仓 + 当日买回（正T），或当日买入 + 卖出昨日持仓（反T）。系统自动识别同日同股的买卖配对为做T操作。
- **自动收益核算**：系统按加权平均成本法自动计算每笔卖出的盈亏金额、收益率，并聚合为日/周/月汇总。
- **持仓自动维护（StockPosition）**：每笔买入/卖出自动更新该股票的持仓数量和平均成本。持仓是交易记录的计算视图，不需要用户手动维护。
- **本金管理（UserPortfolioSettings）**：用户手动输入本金总额，系统据此计算仓位占比、可用资金余额。
- **Agent 合规标记**：录入交易时，系统自动关联当时生效的交易计划和最新 Agent 分析，标记该操作是否遵守了系统建议。

#### 后端

- 新增 `TradeExecution` 实体：

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | long | 主键 |
| PlanId | long? | 关联交易计划（可空，允许计划外交易） |
| Symbol | string | 股票代码 |
| Name | string | 股票名称 |
| Direction | enum | Buy / Sell |
| TradeType | enum | Normal（普通）/ DayTrade（做T） |
| ExecutedPrice | decimal | 成交价 |
| Quantity | int | 成交数量（股） |
| ExecutedAt | DateTime | 成交时间 |
| Commission | decimal? | 手续费（可选） |
| UserNote | string? | 用户备注 |
| CreatedAt | DateTime | 录入时间 |
| CostBasis | decimal? | 卖出时的加权平均成本（系统自动计算） |
| RealizedPnL | decimal? | 已实现盈亏金额（系统自动计算，卖出时填充） |
| ReturnRate | decimal? | 已实现收益率（系统自动计算） |
| ComplianceTag | enum | FollowedPlan / DeviatedFromPlan / Unplanned |
| AnalysisHistoryId | long? | 录入时最新的 Agent 分析快照 ID |
| AgentDirection | string? | 录入时 Agent 的方向建议（偏多/偏空/观望） |
| AgentConfidence | decimal? | 录入时 Agent 的置信度 |
| MarketStageAtTrade | string? | 录入时的市场阶段（主升/分歧/退潮/混沌） |

- 做T 自动识别逻辑：
  - 同一股票同一自然日内，既有买入又有卖出 → 自动将配对的记录标记为 `DayTrade`
  - 用户也可手动在录入时标记为做T
  - 做T盈亏 = (卖出价 - 买入价) × 配对数量

- 自动收益核算服务 `TradeAccountingService`：
  - 维护每只股票的加权平均成本
  - 每次录入卖出时自动计算 `CostBasis`、`RealizedPnL`、`ReturnRate`
  - 加权平均成本 = 历史累计买入金额 / 历史累计买入数量
  - 做T单独核算：正T盈亏 = (卖出价 - 买入价) × 数量；反T同理

- Agent 合规自动标记服务 `TradeComplianceService`：
  - 录入时自动查找该 Symbol 当前生效的 `TradingPlan`（状态为 Pending 或 Triggered）
  - 如果有匹配计划：
    - 方向一致 + 价格在计划触发价 ±5% 范围内 → `FollowedPlan`
    - 方向一致但价格偏离 > 5%，或方向相反 → `DeviatedFromPlan`
  - 如果无匹配计划 → `Unplanned`
  - 同时快照当前最新的 `StockAgentAnalysisHistory`，记录 Agent 方向与置信度
  - 快照当前 `MarketSentimentSnapshot` 阶段标签

- 新增 `UserPortfolioSettings` 实体：

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | long | 主键 |
| TotalCapital | decimal | 本金总额（用户手动输入） |
| UpdatedAt | DateTime | 最后修改时间 |

- 新增 `StockPosition` 计算视图（由 `TradeAccountingService` 维护）：

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | long | 主键 |
| Symbol | string | 股票代码 |
| Name | string | 股票名称 |
| Quantity | int | 当前持仓数量（买入累加、卖出扣减） |
| AverageCost | decimal | 加权平均成本价 |
| TotalCost | decimal | 持仓总成本 |
| LatestPrice | decimal? | 最新行情价（从 QuoteSnapshot 同步） |
| MarketValue | decimal? | 当前市值（Quantity × LatestPrice） |
| UnrealizedPnL | decimal? | 浮动盈亏 |
| UnrealizedReturnRate | decimal? | 浮动收益率 |
| PositionRatio | decimal? | 占总资金比例（MarketValue / TotalCapital） |
| UpdatedAt | DateTime | 最后更新时间 |

- 持仓自动维护逻辑：
  - 每次 `POST /api/trades` 或 `PUT /api/trades/{id}` 时，`TradeAccountingService` 同步更新对应 Symbol 的 `StockPosition`
  - 买入 → Quantity 增加，重算 AverageCost
  - 卖出 → Quantity 减少（不影响 AverageCost，直到清仓后重置）
  - Quantity 归零 → 该 Position 标记为已清仓但保留历史记录
  - 删除交易记录 → 从全部该 Symbol 的交易记录重新计算 Position

- **持仓快照服务 `PortfolioSnapshotService`**：
  - 聚合全部 `StockPosition`（Quantity > 0）+ `UserPortfolioSettings.TotalCapital`
  - 返回：总本金、总市值、总浮盈、可用资金（本金 - 总成本）、总仓位占比、单票明细
  - 此快照将作为 LLM Agent 上下文的一部分（见 R4 的 LLM 上下文注入设计）

- API 端点（在原有基础上新增）：
  - `PUT /api/portfolio/settings` — 设置/更新本金总额
  - `GET /api/portfolio/settings` — 查询当前本金设置
  - `GET /api/portfolio/positions` — 查询全部当前持仓
  - `GET /api/portfolio/positions/{symbol}` — 查询单只股票持仓
  - `GET /api/portfolio/snapshot` — 获取完整持仓快照（本金 + 全部持仓 + 仓位占比 + 浮盈）

- API 端点：
  - `POST /api/trades` — 录入执行记录（自动触发收益核算 + 合规标记）
  - `PUT /api/trades/{id}` — 修改执行记录（允许更正录入错误）
  - `DELETE /api/trades/{id}` — 删除执行记录
  - `GET /api/trades?symbol=&from=&to=&type=` — 查询执行记录（支持按做T筛选）
  - `GET /api/trades/summary?period=day|week|month` — 盈亏汇总（含胜率、盈亏比、做T盈亏）
  - `GET /api/trades/win-rate?from=&to=&symbol=` — 实盘胜率统计
  - `GET /api/trades/plan-deviation?planId=` — 计划 vs 执行偏差
  - `GET /api/trades/compliance-stats?from=&to=` — Agent 合规统计

#### 前端

- 交易计划卡片新增"录入执行"按钮 → 弹窗录入成交价 / 数量 / 时间 / 是否做T
- 计划外交易：独立的"快速录入"入口，无需关联计划
- 交易计划详情展示关联的全部执行记录与累计盈亏
- **本金设置**：设置页新增"我的本金"输入框，保存时调用 `PUT /api/portfolio/settings`
- **持仓总览卡片**（交易日志页 / 首页顶部）：
  - 总本金、总市值、总浮盈/浮亏、可用资金
  - 每只持仓股票：名称、数量、成本价、现价、浮盈率、仓位占比
  - 总仓位占比进度条：绿色（< 60%）、黄色（60-80%）、红色（> 80%）
- 新增 **"交易日志"** 页签：
  - 按时间线展示执行记录
  - 每条记录标注：计划内/计划外、做T/普通、盈亏金额、Agent合规标签
  - 做T记录用特殊颜色/图标标识
  - 日/周/月汇总卡片：
    - 总盈亏、胜率、盈亏比
    - 做T盈亏单独统计
    - 计划执行率（有计划的交易占比）
    - Agent建议遵守率
    - 最大单笔亏损

#### 验收标准

- 可录入买入/卖出执行，关联到已有交易计划
- 可录入计划外交易（无关联 PlanId）
- 可录入做T交易，系统自动识别同日配对
- 卖出时自动计算盈亏金额和收益率
- 录入时自动标记 Agent 合规状态（遵守计划/偏离计划/无计划操作）
- 查看日/周/月盈亏汇总，胜率和盈亏比正确计算
- 计划 vs 执行偏差可视化：实际成交价 vs 计划触发价的偏移
- 可设置本金总额，持仓数量和成本随交易录入自动更新
- 持仓总览正确显示每只股票的市值、浮盈、仓位占比
- 总仓位占比超过 80% 时出现视觉警告

---

### R2: 信号胜率与实盘胜率双线（P0）

> 在扣扳机之前看到客观数据——AI 纸面命中率和你的实际战绩都要看。

#### 后端

- 扩展 `/api/stocks/plans/draft` 响应，附加 `signalHistoryMetrics`：
  - 复用 `ReplayCalibrationService` 已有的 horizon metrics
  - 按当前 commander 方向（偏多/偏空/观望）筛选历史同方向信号
  - 返回：过去同方向信号的 5 日命中率、平均收益、样本数

- 新增 `/api/stocks/agents/signal-track-record?symbol=&direction=`：
  - 拉取该股票（或全局）历史上 commander 给出同方向建议时的后续实际表现
  - 来源：已有 `StockAgentAnalysisHistory` + `KLinePoints` 日线数据

- **新增**实盘胜率叠加：
  - 从 R1 的 `TradeExecution` 数据中计算用户在该股票上的实际交易胜率
  - 返回：实盘交易次数、胜率、平均盈亏比、最近 N 笔表现
  - 在 `/api/stocks/plans/draft` 中一并返回，与纸面信号胜率并列展示

#### 前端

- 交易计划确认弹窗新增 **"双线胜率"** 区块：
  - **AI 纸面命中率**："过去 N 次系统对该股给出'偏多'建议时，5 日内上涨概率 XX%"
  - **你的实盘胜率**："你过去 N 次买入该股，盈利 X 次，胜率 XX%，平均收益 X.X%"
  - 两条线对比展示，帮助用户看到"系统说得对不对"和"我自己执行得好不好"的差距
  - 样本数不足时明确标注"样本不足，仅供参考"
  - 用颜色编码：胜率 > 60% 绿色，40-60% 黄色，< 40% 红色

- 交易计划总览页增加"信号可信度"列 + "实盘胜率"列

#### 验收标准

- 确认计划时能看到 AI 纸面命中率 + 用户实盘胜率双线
- 样本数 < 5 时显示"样本不足"提示
- 纸面胜率与 ReplayCalibration 口径一致
- 实盘胜率与 R1 录入数据口径一致

---

### R3: LLM 交易教练——复盘与反思（P0）

> LLM 不只帮你分析股票，更应该是一个冷静的旁观者——帮你看清楚自己做了什么、为什么赚或亏、哪里需要调整。

#### 核心理念

用户关键洞察：**"我需要一个旁观者来督促我，LLM 是一个好的旁观人。"**

这个功能让 LLM 从"帮你分析股票"升级到"帮你审视自己"的教练角色：
- 结合世界局势、市场环境、市场情绪，告诉用户为什么今天赚了或亏了
- 不只看单笔交易，而是看行为模式：最近是不是在追涨？退潮期是不是还在加仓？
- 给出具体的改进建议，而不是空洞的鼓励

#### 后端

- 新增 `TradeReview` 实体：

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | long | 主键 |
| ReviewType | enum | Daily / Weekly / Monthly / Custom |
| PeriodStart | DateTime | 复盘周期开始 |
| PeriodEnd | DateTime | 复盘周期结束 |
| TradeCount | int | 周期内交易笔数 |
| TotalPnL | decimal | 周期内总盈亏 |
| WinRate | decimal | 周期内胜率 |
| ComplianceRate | decimal | Agent建议遵守率 |
| ReviewContent | string | LLM 生成的复盘报告（Markdown） |
| ContextSummaryJson | string? | 喂给 LLM 的上下文摘要（审计用） |
| LlmTraceId | string? | LLM 调用 trace ID |
| CreatedAt | DateTime | 生成时间 |

- 新增 `TradeReviewService`：
  - 收集复盘周期内的全部 `TradeExecution`，按股票分组
  - 收集每笔交易时的 Agent 建议快照（方向、置信度、关键证据）
  - 收集周期内的市场阶段变化（从 `MarketSentimentSnapshot`）
  - 收集周期内的重大新闻事件（从本地事实库 `LocalStockNews` + `LocalSectorReport`）
  - 收集周期内板块轮动变化（从 `SectorRotationSnapshot`）
  - 组装结构化上下文，调用 LLM 生成复盘报告

- LLM 交易教练 prompt 设计要点：
  - **角色设定**：你是一个冷静、客观、直言不讳的交易教练。你不会恭维用户，而是实事求是地指出问题。
  - **输入数据**：交易记录 + Agent建议 + 市场阶段 + 新闻事件 + 板块轮动
  - **输出结构**：
    1. **市场环境回顾**：本期市场处于什么阶段？主线板块是什么？有什么重大事件？
    2. **操作回顾**：用户做了哪些交易？每笔交易的结果如何？
    3. **赢亏归因**：赚钱的交易为什么赚了（顺势/选股准/买点好）？亏钱的交易为什么亏了（逆势/追涨/止损迟/无计划）？
    4. **纪律评估**：用户有多少交易遵守了系统建议？偏离了多少次？偏离的结果如何？
    5. **行为模式观察**：用户是否有反复出现的行为模式（追涨、频繁交易、退潮期加仓、忽视止损）？
    6. **改进建议**：针对以上观察，给出 2-3 条具体的、可执行的改进建议
  - **语气要求**：直接、诚实、有数据支撑。像一个真正关心你但不客气的教练。

- API 端点：
  - `POST /api/trades/reviews/generate` — 手动触发生成复盘（参数：type=daily|weekly|monthly|custom, from, to）
  - `GET /api/trades/reviews?type=&from=&to=` — 查询已生成的复盘列表
  - `GET /api/trades/reviews/{id}` — 查看复盘详情

#### 前端

- 交易日志页新增 **"生成复盘总结"** 按钮：
  - 默认为"今日复盘"（自动选择当日日期范围）
  - 可选"本周复盘"、"本月复盘"、"自定义时段"
  - 点击后调用 LLM 生成，展示加载状态
  - 生成完毕后直接展示 Markdown 渲染的复盘报告

- 复盘报告展示：
  - 顶部统计卡片：交易笔数、总盈亏、胜率、Agent遵守率
  - 正文：LLM 生成的结构化分析（Markdown 渲染）
  - 底部：本期关键指标对比上期（环比变化）

- 复盘历史列表：
  - 按时间倒序展示所有已生成的复盘
  - 每条显示：日期范围、类型、盈亏、胜率、一句话摘要
  - 点击展开完整报告

#### 验收标准

- 点击"生成今日复盘"，LLM 生成包含市场环境、操作回顾、赢亏归因、纪律评估、行为模式、改进建议的完整报告
- 复盘报告引用了当日/本期的真实交易数据、市场阶段和新闻事件
- 可选择周/月/自定义时段生成总结
- 复盘历史可查看和回溯
- LLM 的归因分析有数据支撑，不是空话（如："你在退潮期连续开了 3 个新仓，全部亏损"，而不是"建议注意风险"）

---

### R4: 持仓上下文注入 LLM Agent 与风险暴露管控（P0）

> AI 分析时如果不知道你的仓位，建议就是空谈。让每个 Agent 都能看到你的真实持仓。

#### 核心理念

目前 `StockAgentOrchestrator` 向 4 个子 Agent + Commander 提供的上下文包括：行情、K 线、新闻、本地事实、确定性特征、查询策略。**但完全缺失用户的持仓信息。**

这导致：
- Commander 建议"加仓"，但不知道用户已经满仓
- Trend Agent 看到突破，但不知道用户在这只股票上已经重仓
- Financial Agent 建议分批建仓，但不知道用户可用资金只剩 5%
- 交易计划建议 `SuggestedPositionScale`，但没有和真实仓位对比

**解决方案**：将用户的持仓快照作为标准上下文字段，注入到每一个 LLM Agent 请求中。

#### 后端

- 新增 `PortfolioContextDto`：

```csharp
public sealed record PortfolioContextDto(
    decimal TotalCapital,           // 本金总额
    decimal TotalMarketValue,       // 总持仓市值
    decimal TotalPositionRatio,     // 总仓位占比 (0-1)
    decimal AvailableCash,          // 可用资金 (本金 - 总成本)
    decimal TotalUnrealizedPnL,     // 总浮盈/浮亏
    string MarketStage,             // 当前市场阶段
    IReadOnlyList<PositionItemDto> Positions  // 逐股持仓明细
);

public sealed record PositionItemDto(
    string Symbol,
    string Name,
    int Quantity,
    decimal AverageCost,
    decimal? LatestPrice,
    decimal? MarketValue,
    decimal? UnrealizedPnL,
    decimal? PositionRatio          // 该股占总资金比例
);
```

- **注入 `StockAgentOrchestrator`**：
  - `BuildContextAsync()` 新增调用 `PortfolioSnapshotService.GetSnapshotAsync()`
  - `StockAgentContextDto` 新增 `PortfolioContext` 字段
  - `SerializeContext()` 和 `SerializeCommanderContext()` 均包含 `PortfolioContext`
  - 这意味着 **所有 5 个 Agent（stock_news、sector_news、financial_analysis、trend_analysis、commander）都能看到用户的持仓**

- **注入 `StockCopilotMcpService`**：
  - Copilot 对话的 MCP 工具调用也携带 `PortfolioContext`
  - 用户通过 Copilot 提问"该不该加仓"时，LLM 能直接看到当前仓位

- **Prompt 层引导**：
  - 各 Agent 的 system prompt 新增持仓相关指引：
    - Commander："用户当前持仓如上下文所示。如果该股已重仓（占比 > 20%），在建议加仓时额外强调风险集中度。"
    - Trend Agent："在判断突破信号时，参考用户当前是否已持有该股及仓位大小。"
    - Financial Agent："在给出建仓/加仓建议时，参考用户可用资金比例。"

- 风险暴露管控：
  - `/api/portfolio/exposure` 端点改为基于**真实持仓**计算：
    - 总暴露 = 全部持仓市值 / 本金
    - 单票集中度 = 单只股票市值 / 本金
    - 板块集中度 = 同板块股票市值合计 / 本金
  - 增加 `Pending` 计划的预期暴露叠加：
    - 真实暴露 + 待执行计划潜在暴露 = 总风险敞口

- API 端点：
  - `GET /api/portfolio/exposure` — 基于真实持仓的风险暴露分析
  - `GET /api/portfolio/context` — 获取 LLM 上下文格式的持仓快照（调试用）

#### 前端

- 交易计划总览顶部新增"仓位暴露条"：
  - 真实仓位占比（来自实际持仓）+ 待执行计划预期占比
  - 总暴露 / 剩余可用 / 单票最大暴露，用进度条可视化
  - 超过 80% 暴露时变红色警告
  - 新建计划时显示"加上此计划后总暴露将达 XX%"

- 分析结果页面增加持仓上下文提示：
  - 当 Commander 建议"加仓"且该股仓位已 > 15% 时，展示黄色提示条
  - 当总仓位 > 80% 且仍有"加仓"建议时，展示红色风险提示

#### 验收标准

- 所有 Agent（stock_news、sector_news、financial_analysis、trend_analysis、commander）的请求上下文中包含 `portfolioContext` 字段
- Commander 在用户已重仓某股时，输出中体现对仓位集中风险的提醒
- 仓位暴露条基于真实持仓计算
- 超过预设阈值时出现视觉警告
- Agent 分析页面在高仓位时展示风险提示

---

### R5: 市场阶段 → 执行纪律联动（P1）

> 让"退潮少做"变成系统行为而非口头纪律。

#### 后端

- 扩展 `MarketSentimentSnapshot` 到执行模式映射：

| 市场阶段 | 默认执行模式 | 仓位系数 | 新建计划门槛 |
|----------|------------|---------|------------|
| 主升 | 积极 | 1.0× | 正常 |
| 分歧 | 谨慎 | 0.7× | 需确认 |
| 退潮 | 防守 | 0.4× | 需强确认 + 风险提示 |
| 混沌 | 观望 | 0.3× | 建议不新建，可强制 |

- `/api/stocks/plans/draft` 返回当前执行模式与仓位系数
- `/api/portfolio/exposure` 返回当前阶段下的调整后建议

#### 前端

- 新建计划弹窗显示当前市场阶段与执行模式：
  - 退潮期："当前市场退潮，建议仓位已自动下调至 40%，确定继续？"按钮变黄
  - 混沌期："当前市场混沌，不建议新建计划" 按钮变红但仍可点击

- 交易计划总览的"市场快链路"条带增加执行模式标签

#### 验收标准

- 不同市场阶段下，建议仓位自动调整
- 退潮 / 混沌期新建计划有明确的确认门槛
- 执行模式在 UI 中清晰展示

---

### R6: 交易行为模式反馈——冷静仪表盘（P2）

> 从"帮你分析股票"升级到"帮你管理自己"。

#### 后端

- 新增 `/api/trades/behavior-stats` 端点：
  - 最近 7 日 / 30 日交易频率
  - 计划执行率（有计划的交易占比）
  - 连续亏损计数（当前连亏 streak）
  - 追涨统计（买入价 > 当日平均价的比例）
  - 过度交易指标（日均交易次数与历史均值的比较）

- 行为触发服务 `TradingBehaviorAlertService`：
  - 连续 3 计划 Invalid → 生成"准确率下降"提示
  - 当日已触发 ≥ 2 计划 → 生成"今日已足够活跃"提示
  - 连续 3 笔亏损 → 生成"建议降低仓位"提示
  - 计划外交易占比 > 50% → 生成"纪律执行偏低"提示

#### 前端

- 首页或股票信息页新增"交易健康度"小卡片：
  - 本周交易纪律分数（0-100，基于计划执行率、连亏控制、仓位合规）
  - 连亏 streak 可视化
  - 最近违规行为高亮

- 行为提示以 toast 或卡片警告形式展示，不弹窗打断

#### 验收标准

- 可查看最近 7 / 30 日交易行为统计
- 连续亏损和过度交易触发可视化提示
- 交易纪律分数由系统自动计算

---

## 四、实施优先级与依赖

```
R1 交易执行记录 ──┬──► R3 LLM 交易教练
                  │         ▲
R4 持仓注入Agent ─┤         │
                  ▼         │
R2 信号+实盘胜率 ─┤    R6 冷静仪表盘
                  │         ▲
                  ├─────────┘
R5 阶段执行联动 ──┘
```

| 切片 | 优先级 | 依赖 | 预估体量 |
|------|--------|------|---------|
| R1 | **P0** | GOAL-008（TradingPlan 实体已有） | 大 |
| R2 | **P0** | GOAL-AGENT-001-R3（ReplayCalibration 已有）+ R1 | 小 |
| R3 | **P0** | R1 + GOAL-013（本地事实库已有）+ GOAL-009（市场阶段已有） | 中 |
| R4 | **P0** | R1（持仓数据）+ StockAgentOrchestrator（已有） | 中 |
| R5 | P1 | GOAL-009（市场阶段已有） | 小 |
| R6 | P2 | R1 + R4 + R5 | 中 |

---

## 五、与现有目标的关系

| 现有目标 | 关系 |
|----------|------|
| GOAL-008 交易计划引擎 | R1 直接扩展 TradingPlan，在同一模块内增加执行层 |
| GOAL-AGENT-001-R3 回放校准 | R2 直接复用 ReplayCalibrationService，与实盘胜率形成双线对比 |
| GOAL-009 情绪轮动 | R3 消费市场阶段做复盘归因；R5 消费 MarketSentimentSnapshot 做执行联动 |
| GOAL-013 本地资讯库 | R3 消费 LocalStockNews + LocalSectorReport 做复盘时的新闻归因上下文 |
| GOAL-010 执行风控闸门 | R4/R5/R6 是 GOAL-010 的产品化前置；未来做更严格的刚性闸门时可基于 R4 仓位模型扩展 |
| StockAgentOrchestrator | R4 直接修改 Orchestrator 的 BuildContextAsync → SerializeContext/SerializeCommanderContext 管线，向所有 5 个 Agent 注入持仓上下文 |
| StockCopilotMcpService | R4 在 Copilot MCP Tool 层同步注入 PortfolioContext，使聊天场景也感知持仓 |
| GOAL-011 复盘闭环 | R1/R3/R6 是 GOAL-011 的数据底座；有了执行记录、LLM复盘和行为统计后，自动归因和周月报告才有数据来源 |
| GOAL-RECOMMEND 推荐系统 | 优先级低于本目标；在纪律闭环建立之前，更多推荐 = 更多过度交易风险 |
| GOAL-017 量化引擎 | 长期有价值，但短期对胜率帮助不如纪律系统 |

---

## 六、核心认知

> 当前系统已经很擅长"给你看东西"，但还不擅长"帮你管住自己"。
>
> 多 Agent 分析、K 线策略叠加、情绪轮动、资讯聚合——这些都是 30% 的分析准确度部分。  
> 交易执行记录、信号历史胜率、仓位暴露可见、市场阶段限速、行为模式反馈——这些是 70% 的纪律与风控部分。
>
> v3.0 的关键增量有两个：
> 1. **持仓即上下文**：用户的真实本金和仓位情况随每一次 LLM 请求一起发送，让 Agent 从"纸上谈兵"变为"知道你实际持有什么、承受多大风险"再给建议。
> 2. **LLM 交易教练**：让系统不仅告诉你"该买什么"，更告诉你"你做了什么、为什么赚或亏、哪里需要改"。
>
> LLM 天然适合做这个冷静的旁观者——它不带情绪、不怕得罪人、能看全局数据、能从历史中发现你自己看不到的模式。
> 而且当它知道你的真实持仓时，它的建议才能从"理论上正确"变成"对你实际有用"。
>
> GOAL-018 的目标不是让系统"更聪明"，而是让用户**知道自己做了什么、做对了什么、哪里需要改**，
> 并且让 Agent **知道用户的真实处境**再给出切实可操作的建议。
> 这才是真正提高胜率的路径。
