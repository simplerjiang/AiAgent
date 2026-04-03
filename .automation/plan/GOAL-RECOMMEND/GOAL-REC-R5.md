# GOAL-REC-R5: 前端推荐工作台 UI

> **前置**: R2 (API 端点可调)
> **交付**: 推荐报告卡片 + 辩论 Feed + 进度条 + 追问输入框 + SSE 集成

## 任务清单

### R5-1: KEY_LABELS 扩展 + jsonMarkdownService 复用验证
**位置**: `frontend/src/utils/jsonMarkdownService.js`

新增推荐专用 KEY_LABELS:
```javascript
sectorName, sectorCode, mainNetInflow, verdictReason, riskNotes,
pickType, technicalScore, supportLevel, resistanceLevel,
triggerCondition, invalidCondition, bullCase, bearCase,
validityWindow, overallConfidence, marketSentiment, toolUsageSummary,
candidateSectors, catalysts, resonanceSectors, bullPoints, bearPoints,
selectedSectors, eliminatedSectors, buyLogic, riskLevel
```

验证: `ensureMarkdown(sampleRecommendJSON)` 输出可读中文 Markdown。

### R5-2: StockRecommendTab.vue 整体重构
**位置**: `frontend/src/modules/stocks/StockRecommendTab.vue`

从 ChatWindow 模式 → 推荐工作台模式:
```
┌──────────────┬───────────────────────────────────────────┐
│  市场快照     │   Tab Bar: [推荐报告] [辩论过程] [团队进度] │
│  (保留侧边栏) ├───────────────────────────────────────────┤
│  指数/资金    │   主内容区（按选中 Tab 切换组件）            │
│  板块 Top 6  │                                           │
│              │                                           │
│  ─────────── │                                           │
│  历史推荐列表 │                                           │
│  · 会话 1    │                                           │
│  · 会话 2    ├───────────────────────────────────────────┤
│              │   追问输入框 + 快捷按钮                     │
│  [新建推荐]   │   [板块深挖] [换方向] [重新推荐]            │
└──────────────┴───────────────────────────────────────────┘
```

核心状态:
- `activeSession`: 当前推荐会话
- `activeTurn`: 当前执行轮次
- `activeTab`: 'report' | 'debate' | 'progress'
- `sessionHistory`: 历史会话列表
- `sseConnection`: EventSource 实例
- `isRunning`: 是否正在执行

### R5-3: RecommendReportCard.vue — 推荐报告卡片
**位置**: `frontend/src/modules/stocks/recommend/RecommendReportCard.vue`

渲染 RecommendationReport JSON:
- 顶部: 市场情绪 + 总体置信度 + 有效期
- 板块卡片列表: 板块名 + 涨跌幅 + 资金流入 + 裁决理由
- 每板块下的个股卡片: 龙头/潜力标签 + 价格 + 技术评分 + 支撑压力位 + 触发/失效条件
- 风险提示区
- 操作按钮: 看K线(跳转股票终端) / 查新闻 / 深度分析(进 Workbench)
- 使用 `valueToSafeHtml()` 渲染所有 Agent 输出文本（防 XSS）

### R5-4: RecommendFeed.vue — 辩论过程
**位置**: `frontend/src/modules/stocks/recommend/RecommendFeed.vue`

- 按时间线展示所有角色的发言
- 角色头像/颜色区分（Bull=绿, Bear=红, Judge=蓝, Scanner=灰...）
- Stage 分隔线
- 工具调用事件显示（哪个 Agent 调了 web_search）
- 使用 `ensureMarkdown()` + `markdownToSafeHtml()` 渲染 Agent 输出
- 支持折叠/展开完整内容

### R5-5: RecommendProgress.vue — 团队进度
**位置**: `frontend/src/modules/stocks/recommend/RecommendProgress.vue`

- 5 阶段进度条（● 完成 / ◐ 进行中 / ○ 待执行）
- 每阶段展开: 角色列表 + 状态 + 工具调用次数 + 耗时
- 实时更新（通过 SSE 事件驱动）
- 降级/失败角色红色标记

### R5-6: SSE 事件流集成
- 新建推荐会话后立即建立 EventSource 连接
- 事件类型映射:
  - `StageStarted` → 进度条更新
  - `RoleStarted/Completed` → 进度 + Feed 更新
  - `ToolDispatched/Completed` → Feed 工具事件
  - `TurnCompleted` → 加载最终报告
  - `DegradedNotice` → 显示降级告警
- 连接断开自动重连（3 次）

### R5-7: 追问对话框
- 底部输入框 + 快捷按钮
- 快捷: [板块深挖] [换方向] [重新推荐]
- 输入后 POST follow-up → 收到 FollowUpPlan → 根据 strategy 处理:
  - `partial_rerun` / `full_rerun`: 显示"正在重新分析..." + 重建 SSE
  - `workbench_handoff`: 弹出确认对话框 → 跳转到 Trading Workbench 页签
  - `direct_answer`: 直接在 Feed 中显示回答

### R5-8: 历史会话列表
- 左侧边栏下方
- 调用 GET `/api/recommend/sessions` 加载
- 点击加载历史会话的报告/辩论/进度
- 显示: 日期 + 推荐板块摘要 + 状态

### R5-9: 前端单元测试
- RecommendReportCard 渲染: mock RecommendationReport JSON → 验证板块卡片和个股卡片
- RecommendFeed 渲染: mock FeedItem 列表 → 验证角色发言和工具事件
- RecommendProgress 渲染: mock StageSnapshot 列表 → 验证进度条状态
- SSE 事件驱动 UI 更新测试
- 追问快捷按钮触发正确 API 调用测试
- StockRecommendTab 整体 mount 测试

## 验收标准
- [ ] 推荐报告卡片正确渲染板块+个股+风险
- [ ] 辩论过程按时间线正确展示角色发言
- [ ] 团队进度实时反映执行状态
- [ ] SSE 事件驱动 UI 实时更新
- [ ] 追问输入框 + 快捷按钮功能正常
- [ ] Workbench 交接跳转正确
- [ ] 历史会话可加载
- [ ] 所有 Agent 输出经 XSS 安全渲染
- [ ] 前端单元测试全部通过
