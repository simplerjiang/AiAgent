# GOAL-AGENT-NEW-004 计划书（2026-03-28）

## 目标摘要

修复 Agent 工作台 6 项用户反馈的 Bug 和缺失功能，同时回补 GOAL-AGENT-NEW-003 的残留缺口。

---

## GOAL-AGENT-NEW-003 遗留问题复检

| # | 问题 | 根因 | 处置 |
|---|------|------|------|
| G3-1 | 讨论动态里追问时不显示用户消息，而是 "Turn 2 started for sz000021" | `ResearchRunner.cs:72` 发布 TurnStarted 事件用 `$"Turn {turn.TurnIndex} started for {session.Symbol}"` 作为 Summary，用户 prompt 从未生成独立 Feed 条目 | → 并入 GOAL-004 R1 修复 |
| G3-2 | 讨论区 MCP 工具事件"Dispatching X"不可点击展开 | GOAL-003 只在 Progress 页加了折叠面板，Feed 页的 tool 事件仍为不可交互纯文本；`MetadataJson` 在 dispatch 时为 null | → 并入 GOAL-004 R6 修复 |
| G3-3 | 路由卡在浏览器快照中偶尔不显示完整置信度/理由 | 后端已正确持久化；属于前端 polling 时序（异步 session detail 刷新未完成时快照），不是逻辑 bug | 不改，记录为已知时序现象 |

---

## 6 项需求详细分解

### R1 — 讨论动态应展示用户消息（Bug Fix）

**现象**：用户在追问框输入文字并发送后，讨论区只出现 "Turn 2 started for sz000021" 而不显示用户写的内容。

**根因**：
- `ResearchRunner.cs` 第 72 行：`TurnStarted` 事件的 Summary 是系统生成文本 `$"Turn {turn.TurnIndex} started for {session.Symbol}"`
- `ResearchFeedItemType` 枚举中有 `UserFollowUp` 但从未使用
- 用户 prompt 存储在 `ResearchTurn.UserPrompt` 但没有转化为 Feed 条目

**修复方案（后端）**：
1. 在 `ResearchRunner.RunTurnAsync()` 的 `TurnStarted` 事件发布之后，额外发布一个 `UserFollowUp` 类型事件，Summary 设为 `turn.UserPrompt`
2. 保留原有 `TurnStarted` 事件作为系统标记（可用于 divider），只需把它的 Summary 改为更可读的中文如 `"第 {N} 轮分析开始"`

**修复方案（前端）**：
- `TradingWorkbenchFeed.vue` 的 `itemKind()` 已经把 `userfollowup` 归类为 `'user'`，会右对齐显示用户气泡 → 无需前端改动

**验收**：讨论区能看到蓝色右侧气泡显示用户输入的追问文字。

---

### R2 — "下一步操作" 按钮点击无效（Bug Fix）

**现象**：报告页底部的 4 个"下一步操作"按钮点击后无任何反应。

**根因**：
- `TradingWorkbench.vue` 中 `handleNextAction(action)` 对 `ViewDailyChart` / `ViewMinuteChart` emit `navigate-chart`，对 `DraftTradingPlan` emit `navigate-plan`
- 但父组件 `StockInfoTab.vue` 中 `<TradingWorkbench>` **没有监听这两个 emit 事件**，事件被完全丢弃
- 其他 actionType（如刷新新闻、深入分析等）未做任何处理，直接跳过

**修复方案**：
1. **前端（StockInfoTab.vue）**：在 `<TradingWorkbench>` 标签上添加 `@navigate-chart` 和 `@navigate-plan` 监听器
   - `navigate-chart`：切换到对应图表视图（日 K / 分时）
   - `navigate-plan`：打开交易计划模态框
2. **前端（TradingWorkbench.vue）**：补充更多 actionType 的处理逻辑：
   - `RefreshNews` → 调用 `wb.submitFollowUp('刷新新闻分析', { continuationMode: 'RefreshNews' })`
   - `DeepAnalysis` → 把 action.description 作为 prompt 提交追问
   - 未识别的 actionType → console.warn 并展示 toast 提示

**验收**：点击"查看日 K" → 图表切换到日 K 视图；点击"制定交易计划" → 弹出计划模态框。

---

### R3 — 文字太小，放大字体（UX 改进）

**现象**：工作台区域文字普遍 9-11px，用户阅读困难。

**当前基线统计**：
- Report 页：11px 为主体，9-10px 用于标签/置信度
- Progress 页：10-11px 全域
- Feed 页：11-12px 气泡内容
- Composer 页：10-11px

**修复方案**：
全工作台统一提升 2px：

| 元素类别 | 当前 | 调整后 |
|---------|------|--------|
| 正文/内容区 | 11px | 13px |
| 标签/辅助文字 | 10px | 12px |
| 极小标签（状态 badge、时间戳） | 9px | 11px |
| 标题/区块头 | 11-12px | 13-14px |

涉及文件：
- `TradingWorkbenchReport.vue`
- `TradingWorkbenchProgress.vue`
- `TradingWorkbenchFeed.vue`
- `TradingWorkbenchComposer.vue`
- `TradingWorkbenchHeader.vue`

**验收**：Browser MCP 确认文字清晰可读，不再有 9px 文字。

---

### R4 — JSON→Markdown 字段翻译覆盖不足（增强）

**现象**：很多字段仍然以英文 key 显示（如 `stageConfidence`、`mainlineScore`、`crawledAt` 等），JSON→Markdown 的 KEY_LABELS 只覆盖 42 个常用 key，而实际 MCP 输出中有 80+ 不同 key。

**用户建议**：接入翻译库或后端处理。

**修复方案（双管齐下）**：

**方案 A — 前端 KEY_LABELS 大量扩充（立即见效）**：
在 `jsonMarkdownService.js` 的 KEY_LABELS 中补充 ~60 个常见字段的中文映射，覆盖：
- MCP 元数据类：`traceId`→追踪ID、`taskId`→任务ID、`toolName`→工具名称、`latencyMs`→延迟(ms)、`warnings`→告警、`degradedFlags`→降级标记
- 证据类：`crawledAt`→采集时间、`ingestedAt`→入库时间、`level`→级别、`sentiment`→情绪、`target`→目标、`tags`→标签、`localFactId`→本地事实ID
- 市场类：`stageConfidence`→阶段置信度、`mainlineScore`→主线强度、`mainlineSectorName`→主线板块、`advancers`→上涨家数、`decliners`→下跌家数、`limitUpCount`→涨停数、`limitDownCount`→跌停数
- 技术指标类：`signal`→信号、`numericValue`→数值、`state`→状态、`timeframe`→时间框架、`interval`→周期
- 基本面类：`label`→字段名、`value`→字段值、`facts`→事实列表、`businessScope`→经营范围、`listingBoard`→上市板块
- 其他：`headline`→标题、`dataCoverage`→数据覆盖、`acquiredCount`→已获取数、`displayedCount`→已展示数、`missingFields`→缺失字段

**方案 B — 后端动态翻译兜底（长期生效）**：
1. 新增后端工具方法 `ChineseKeyLabelService`，内置与前端同步的字典 + camelCase 自动中文化兜底
2. LLM Agent 输出在序列化到 Feed/Report 前，经过一次 key 翻译预处理
3. 这样即使前端字典缺失，后端已预翻译 → 前端直接显示中文

**本期优先做方案 A（覆盖 80%+场景），方案 B 放入 P1 跟进。**

**验收**：Browser MCP 中不再出现常见英文 key 如 `stageConfidence`、`mainlineScore`、`crawledAt` 等原始英文文本。

---

### R5 — 用户持仓信息录入与分析注入（新功能）

**现象**：用户无法告诉系统自己持有多少股、成本价多少，Agent 分析时无法考虑用户的真实仓位情况。

**需求细节**：
- 选中股票后，允许用户填写：持仓数量（单位：手）、平均成本价
- 数据存入数据库
- 在分析时携带给 4 个关键 Agent：研究辩论、交易方案、风险评估、投资决策

**实现方案**：

**后端**：
1. 新增实体 `StockPosition`：
   - `Id` (long, PK)
   - `Symbol` (string, indexed)
   - `QuantityLots` (int, 持仓手数)
   - `AverageCostPrice` (decimal, 平均成本)
   - `Notes` (string?, 备注)
   - `UpdatedAt` (DateTime)
2. 新增 `DbSet<StockPosition>` 和 Schema Initializer 补丁
3. 新增 API 端点：
   - `GET /api/stocks/position?symbol=XXX` → 返回当前持仓
   - `PUT /api/stocks/position` → 创建或更新持仓
4. 在 `ResearchRunner` 或各阶段 prompt 构建时注入持仓上下文：
   - 读取当前 symbol 的 `StockPosition`
   - 写入 prompt 上下文块：`"用户当前持仓：{X} 手，均价 {Y} 元"`
   - 注入到 `ResearchDebate`、`TraderProposal`、`RiskDebate`、`PortfolioDecision` 四个阶段的 prompt 模板

**前端**：
1. 在股票详情区（StockInfoTab 或工作台 Header 区域）增加持仓输入控件：
   - 两个 input：手数 + 成本价
   - 保存按钮 → `PUT /api/stocks/position`
   - 显示当前持仓状态摘要
2. 工作台 Header 区域显示持仓概要标签（如 "持仓 500 手 @ ¥15.30"）

**验收**：
1. 输入持仓信息并保存成功
2. 发起分析后，在讨论区的交易员/风控/投资决策 Agent 消息中能看到引用用户持仓信息

---

### R6 — Feed 区 MCP 工具事件可展开查看请求/响应内容（增强 + GOAL-003 补全）

**现象**：讨论动态里的 "Dispatching StockStrategyMcp" 只是一行纯文本，用户想点击它看到：
- Agent 向 MCP 发了什么请求（symbol、参数）
- MCP 返回了哪些信息给 Agent

**当前状态**：
- Feed 条目 `MetadataJson` 在 tool dispatch 时为 null
- Tool 执行完成后的响应数据存在 `ResearchRoleState.OutputRefsJson` 中，但未关联到 Feed 条目
- GOAL-003 Progress 页有折叠面板，但 Feed 页无交互

**修复方案**：

**后端**：
1. 在 `ResearchRoleExecutor.cs` 的 tool dispatch 事件中，将请求参数写入 `DetailJson`：
   ```csharp
   new ResearchEvent(..., "Dispatching {toolName}",
       JsonSerializer.Serialize(new { toolName, symbol = context.Symbol, requestedAt = DateTime.UtcNow }),
       DateTime.UtcNow)
   ```
2. 在 tool completed 事件中，将工具摘要和完整响应写入 `DetailJson`：
   ```csharp
   new ResearchEvent(..., $"{toolName} 完成",
       JsonSerializer.Serialize(new { toolName, status = "Completed", summary = BuildToolSummary(toolName, toolResult), resultJson = toolResult }),
       DateTime.UtcNow)
   ```
3. `PersistFeedItemsAsync` 已经把 `evt.DetailJson` 写入 `MetadataJson`，无需改动

**前端**：
1. `TradingWorkbenchFeed.vue` 中的 tool 事件区块改为可点击：
   - 点击后展开折叠面板
   - 面板内容从 `item.metadataJson` 中读取并通过 `valueToSafeHtml` 渲染
   - 如果 `metadataJson` 包含 `resultJson`，渲染为可读 Markdown
   - 如果只有请求参数（dispatch 时），显示请求概要
2. 样式：tool 行加 cursor: pointer 和 hover 效果，展开后左侧加蓝色竖线

**验收**：点击 "Dispatching StockStrategyMcp" → 展开面板显示请求参数（symbol 等）；tool 完成后点击 → 展开显示响应摘要和关键数据。

---

## 执行排序与依赖

```
Phase A（后端先行，1-2 天）：
  R1 Feed 用户消息修复 ← 最简单，先修
  R5 StockPosition 实体 + API + prompt 注入
  R6 Tool 事件 MetadataJson 存储（后端侧）
  R4-B ChineseKeyLabelService（如时间允许，否则 P1）

Phase B（前端，2-3 天）：
  R3 全工作台字体放大
  R4-A KEY_LABELS 大量扩充
  R2 下一步操作按钮事件接线
  R5 持仓输入 UI 控件
  R6 Feed 区 tool 事件可展开面板

Phase C（联调验收，1 天）：
  全链路单测
  Browser MCP 多场景验收
  打包桌面链路验证
```

---

## 测试计划

### 后端单测
- R1：验证 `PersistFeedItemsAsync` 后 Feed 中包含 `UserFollowUp` 类型的条目且 Content = 用户 prompt
- R5：验证 `StockPosition` CRUD + prompt 注入包含持仓文本
- R6：验证 tool dispatch/completed 事件的 `DetailJson` 非空且格式正确

### 前端单测
- R2：验证 `handleNextAction` + `emit` 链路（mock emit 接收器）
- R3：快照测试确认无 9px/10px 残留
- R4：`jsonMarkdownService.spec.js` 补充新增 key 的翻译断言
- R6：验证 feed tool 事件可展开/收起

### Browser MCP
- 全流程：搜索标的 → 填写持仓 → 发起分析 → 讨论区查看用户消息 + MCP 展开 → 报告页点击下一步操作 → 确认字体大小提升
- Console error 检查
- 多标的验证

---

## 风险与缓解

| 风险 | 缓解策略 |
|------|----------|
| KEY_LABELS 扩充后仍有遗漏 key | 设定 camelCase → 空格标题化兜底已在 `toLabel()` 中实现 |
| StockPosition 表变更影响现有数据 | 纯新增表，不改已有表，安全 |
| Prompt 注入持仓后 token 超限 | 持仓摘要控制在 50 字以内 |
| handleNextAction 的 actionType 不可穷举 | 未识别类型降级为 toast 提示 |

---

## 交付物清单

1. 计划报告：本文件
2. 任务台账：`.automation/tasks.json` 新增 `GOAL-AGENT-NEW-004`
3. 运行状态：`.automation/state.json` 切换到本任务
4. 后续开发报告：待开发完成后补充 `GOAL-AGENT-NEW-004-DEV-*.md`
