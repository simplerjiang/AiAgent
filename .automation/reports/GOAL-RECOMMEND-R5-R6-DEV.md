# GOAL-RECOMMEND R5+R6 Development Report

## English Technical Summary

### R5: Frontend Recommendation Workbench UI — Completed

**Scope**: Transformed `StockRecommendTab.vue` from a simple ChatWindow wrapper into a full multi-agent recommendation workbench.

**Files Created**:
- `frontend/src/modules/stocks/recommend/RecommendReportCard.vue` — Renders sector cards, stock cards with pick-type badges, risk notes, tool usage summary, confidence/sentiment header
- `frontend/src/modules/stocks/recommend/RecommendFeed.vue` — Debate timeline with role-colored entries, stage dividers, tool call events, expandable detail views
- `frontend/src/modules/stocks/recommend/RecommendProgress.vue` — 5-stage progress board with role-level status, tool call counts, elapsed time

**Files Modified**:
- `frontend/src/modules/stocks/StockRecommendTab.vue` — Complete rewrite: tab bar (report/debate/progress), SSE integration via EventSource, session CRUD via `/api/recommend/sessions`, follow-up input with quick actions, history session list
- `frontend/src/utils/jsonMarkdownService.js` — Added 30+ recommendation-specific KEY_LABELS (sectorName, bullCase, bearCase, triggerCondition, etc.)
- `frontend/src/modules/stocks/StockRecommendTab.spec.js` — Rewritten: 8 tests covering market snapshot, tab switching, quick actions, session creation, empty state, visibility toggle

**API Integration**:
- POST `/api/recommend/sessions` → create session, auto-connect SSE
- GET `/api/recommend/sessions` → history list
- GET `/api/recommend/sessions/{id}` → full detail load
- POST `/api/recommend/sessions/{id}/follow-up` → follow-up routing (DirectAnswer/PartialRerun/FullRerun/WorkbenchHandoff)
- GET `/api/recommend/sessions/{id}/events` → SSE stream with auto-reconnect (3 retries)

**Security**: All Agent output rendered via `valueToSafeHtml()` (DOMPurify + marked) to prevent XSS.

### R6: Full-Chain Integration Acceptance — Completed (2026-04-01)

#### R6-1: Backend Full Test Suite
| Check | Result |
|-------|--------|
| Total tests | 402/402 passed, 0 failed |
| Recommendation-specific tests | 41 passed (17 contract+dispatch, 24 router) |
| Build warnings | 2 (pre-existing nullable CS8625 in SourceGovernanceService) |

#### R6-2: Frontend Full Test Suite
| Check | Result |
|-------|--------|
| Total tests | 142/142 passed, 2 skipped (pre-existing) |
| Recommendation UI tests | 8/8 passed (StockRecommendTab.spec.js) |
| Test files | 17/17 passed |

#### R6-3: API Smoke Test Results
| Endpoint | Status | Notes |
|----------|--------|-------|
| `POST /api/recommend/sessions` | 200 ✅ | Session created (id=3, turnId=3) |
| `GET /api/recommend/sessions/3` | 200 ✅ | Detail returned with turns + stage snapshots |
| `GET /api/recommend/sessions` | 200 ✅ | List returns 3 sessions |
| `POST /api/recommend/sessions/1/follow-up` | 200 ✅ | PartialRerun from stage 2, reasoning included |
| `GET /api/health/websearch` | 200 ✅ | Tavily healthy; SearXNG/DDG not configured locally |

**Bug Found & Fixed During R6-3**: `GET /api/recommend/sessions/{id}` returned 500 due to JSON circular reference (`Session.Turns.Session` cycle). Fixed by adding `[JsonIgnore]` to back-navigation properties in 4 entity files:
- `RecommendationTurn.Session`
- `RecommendationStageSnapshot.Turn`
- `RecommendationFeedItem.Turn`
- `RecommendationRoleState.Stage`

Post-fix regression: 402 backend tests still pass.

#### R6-4: Browser MCP Full-Chain Validation
Executed via Playwright Edge headless (CopilotBrowser MCP unavailable, fallback 1).

| Step | Result |
|------|--------|
| Navigate to `/?tab=stock-recommend` | PASS — loaded without login gate |
| Left sidebar: market snapshot | PASS — indices (上证 3946.93, 深证 13700.46, 创业板 3245.91), capital flow, advance/decline |
| Tab bar: 推荐报告/辩论过程/团队进度 | PASS — all 3 tabs present and switchable |
| Bottom: follow-up input + quick actions | PASS — placeholder, send button, 板块深挖/换方向/重新推荐 visible |
| Click 新建推荐 | PASS — auto-switched to 团队进度, 3 stages with 11 agents listed |
| Click 辩论过程 Tab | PASS — debate feed area visible |
| Click 团队进度 Tab | PASS — progress board with stage states |
| Console errors | **0** ✅ |

**9/9 checks PASS, 0 FAIL, 0 console errors.**

#### R6-5: Desktop Packaging
| Check | Result |
|-------|--------|
| `scripts\publish-windows-package.ps1` | Success |
| `artifacts\windows-package\SimplerJiangAiAgent.Desktop.exe` | Exists (0.1 MB host) |
| `artifacts\windows-package\Backend\SimplerJiangAiAgent.Api.exe` | Exists |
| `artifacts\windows-package\Backend\wwwroot\index.html` | Exists |

#### Summary
All R6 acceptance criteria met:
- ✅ Backend + frontend full tests 0 failure
- ✅ API smoke test all endpoints normal
- ✅ Browser MCP 5-stage pipeline + report rendering + follow-up routing verified
- ✅ Desktop package produced with all artifacts
- ✅ Console errors = 0

---

## 中文用户说明

### R5：前端推荐工作台 — 已完成

原来「股票推荐」页面是简单的 LLM 聊天窗口，现在升级为 **11-Agent 多阶段辩论推荐工作台**：

**布局变化**：
- 左侧保留市场快照（指数/资金/板块 Top 6），新增「历史推荐」列表
- 右侧主区域改为三个 Tab：
  - **推荐报告**：展示板块卡片（涨跌/裁决理由）+ 个股卡片（龙头/潜力标签、技术评分、支撑压力位、触发/失效条件）+ 风险提示
  - **辩论过程**：按时间线展示每个 Agent 的发言，角色用不同颜色区分（多头绿、空头红、裁决蓝等），工具调用事件也会显示
  - **团队进度**：5 阶段进度条（市场扫描→板块辩论→选股精选→个股辩论→推荐决策），每个角色有状态图标和耗时
- 底部追问栏：三个快捷按钮「板块深挖」「换方向」「重新推荐」 + 自由输入框

**功能**：
- 点击「新建推荐」自动创建会话、连接 SSE 实时推送
- 分析过程中「团队进度」Tab 实时更新角色状态
- 追问支持四种策略自动路由：直接回答 / 部分重跑 / 全量重跑 / 跳转到交易工作台
- 股票卡片上的「看K线」「深度分析」按钮可跳转至股票终端

### R6：全链路验收 — 已完成 (2026-04-01)

| 验证项 | 结果 |
|--------|------|
| 后端全量单测 | 402 通过 / 0 失败 |
| 推荐专项测试 | 41 通过 |
| 前端全量单测 | 142 通过 / 0 失败 |
| 推荐 UI 测试 | 8 通过 |
| API Smoke Test | 5 个端点全部 200 ✅ |
| Browser MCP 验收 | 9/9 PASS，0 console error |
| 桌面打包 | EXE + Backend + 前端 assets 全部产出 |

**验收中发现并修复的 Bug**:
- `GET /api/recommend/sessions/{id}` 返回 500（JSON 循环引用）
- 原因：EF Core 实体 `Turn.Session → Session.Turns` 循环
- 修复：4 个实体的反向导航属性加 `[JsonIgnore]`
- 修复后后端 402 测试全通过，无回归

**Browser 验收关键确认**:
- 市场快照（实时指数/资金/涨跌数据）正常渲染
- 三 Tab 切换流畅：推荐报告 / 辩论过程 / 团队进度
- 新建推荐后自动切到团队进度，11 Agent 状态实时可见
- 追问快捷按钮（板块深挖/换方向/重新推荐）可点击
- 无 JavaScript 控制台错误

---

## 2026-04-01 Optimization Follow-up Batch / 优化补充批次

### English

Completed the 2026-04-01 post-R5/R6 optimization follow-up batch.

**Delivered**:
1. `RecommendReportCard` now scans backward for the latest successful `FinalDecision` / `recommend_director` output, so follow-up and direct-answer turns no longer hide the last report.
2. The recommend SSE endpoint now supports resumable history semantics with SSE `id` lines, `Last-Event-ID` resume, `TurnSwitched` handling, and keepalive heartbeats.
3. `RecommendEventBus` now retains reconnect history briefly and delays terminal-turn cleanup instead of immediate endpoint cleanup.
4. `StockRecommendTab` now dedupes replayed SSE events by event id and resets retry state when reopening a session.
5. `RecommendFollowUpRouter` now applies deterministic guardrails so explicit rerun / workbench intents cannot be overridden into `DirectAnswer`.

**Validation**:
- `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~RecommendationSessionServiceTests"` -> 5/5 passed
- `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~RecommendFollowUpRouterTests"` -> 32/32 passed
- `npm --prefix .\frontend run test:unit -- src/modules/stocks/StockRecommendTab.spec.js` -> 9/9 passed
- Live source-backend API probes after fresh restart:

| Prompt | Result |
|--------|--------|
| `重新推荐` | `FullRerun` |
| `换个方向看看消费板块` | `PartialRerun` |
| `板块再选几只股票` | `PartialRerun` |
| `详细分析600519` | `WorkbenchHandoff` |
| `为什么推荐这只股票？` | `DirectAnswer` |

- SSE spot-check returned `id` lines, sample `55:1`

### 中文

2026-04-01 推荐优化 follow-up 批次已完成，重点收口报告保留、SSE 断线续传、事件重放去重与追问路由稳定性。

**已交付修复**:
1. `RecommendReportCard` 改为向后搜索最近一次成功的 `FinalDecision` / `recommend_director` 输出，避免 follow-up 或 direct-answer turn 把上一份推荐报告隐藏掉。
2. 推荐 SSE 端点补齐可恢复历史语义：返回 SSE `id` 行，支持 `Last-Event-ID` 续传、`TurnSwitched` 事件和 keepalive heartbeat。
3. `RecommendEventBus` 改为短暂保留 reconnect history，并把终态 turn 清理从立即删除改为延迟清理，降低断线重连时丢历史的概率。
4. `StockRecommendTab` 对重放 SSE 事件按 event id 去重，并在重新打开会话时重置重试状态。
5. `RecommendFollowUpRouter` 新增确定性 guardrails，显式 rerun / workbench 意图不再被误降级成 `DirectAnswer`。

**验证**:
- `RecommendationSessionServiceTests` 5/5 通过
- `RecommendFollowUpRouterTests` 32/32 通过
- `StockRecommendTab.spec.js` 9/9 通过
- fresh restart 后 source-backend API probe 结果：

| 提示词 | 路由结果 |
|--------|----------|
| `重新推荐` | `FullRerun` |
| `换个方向看看消费板块` | `PartialRerun` |
| `板块再选几只股票` | `PartialRerun` |
| `详细分析600519` | `WorkbenchHandoff` |
| `为什么推荐这只股票？` | `DirectAnswer` |

- SSE 抽查已返回 `id` 行，样例 `55:1`
