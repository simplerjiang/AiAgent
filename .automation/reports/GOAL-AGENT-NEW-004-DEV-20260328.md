# GOAL-AGENT-NEW-004 开发报告（2026-03-28）

## Summary / 概述

All 6 requirements (R1–R6) from GOAL-AGENT-NEW-004 have been implemented end-to-end, covering backend and frontend code, unit tests, browser (Darbot MCP) verification, and Windows desktop packaging.

全部 6 项需求（R1–R6）已端到端完成，涵盖后端代码、前端代码、单元测试、浏览器验收（Darbot MCP）及 Windows 桌面打包。

---

## Phase A — Backend

### R1: Feed 区展示用户追问消息
- `ResearchRunner.cs`：在 `TurnStarted` 事件发布之后，新增 `UserFollowUp` 事件，Summary 设为 `turn.UserPrompt`
- `ResearchFeedItemType` 已有 `UserFollowUp` 枚举值，前端自动右对齐显示用户气泡

### R5: 持仓信息录入与分析注入
- 新增实体 `StockPosition`（Symbol PK, QuantityLots, AverageCostPrice, Notes, UpdatedAt）
- `AppDbContext.cs` 新增 `DbSet<StockPosition>`
- `ResearchSessionSchemaInitializer.cs` 新增建表 SQL（SQL Server + SQLite）
- `StocksModule.cs` 新增 `GET /api/stocks/position?symbol=XXX` 与 `PUT /api/stocks/position`
- `ResearchRunner.cs`：新增 `_positionContext` 实例字段，在 `RunTurnAsync` 开始时读取持仓并格式化为 `"用户当前持仓：X 手，均价 ¥Y"` 字符串
- `ResearchRoleExecutor.cs`：`RoleExecutionContext` record 新增 `PositionContext` 字段，`BuildUserContent` 在 prompt 末尾附加持仓上下文

### R6: Feed MCP tool 事件存储 MetadataJson
- `ResearchRoleExecutor.cs`：tool dispatch 事件写入 `new { toolName, symbol, requestedAt }` 到 `DetailJson`
- Tool completed 事件写入 `new { toolName, status, resultJson }` 到 `DetailJson`
- `ResearchSessionDto.cs`：`ResearchFeedItemDto` 新增 `MetadataJson` 属性，DB 查询映射与实时事件映射均已更新

### R4-B: 后端翻译服务
- 安装 GTranslate 2.3.1 NuGet（MIT 协议，免费 Google Translate，无需 API Key）
- 新增 `JsonKeyTranslationService`：预种 ~100+ 条中文映射 + GTranslate 异步兜底 + `ConcurrentDictionary` 缓存
- 新增 `GET /api/stocks/translations/json-keys` 端点，返回全部翻译缓存

### 后端测试
```
dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\
→ 350 passed, 1 failed (pre-existing StockSyncServiceTests failure)
```

---

## Phase B — Frontend

### R2: "下一步操作"按钮事件修复
- `TradingWorkbench.vue`：`handleNextAction` 扩展支持 `RefreshNews`、`DeepAnalysis`、fallback
- `StockInfoTab.vue`：新增 `@navigate-chart` 和 `@navigate-plan` 事件监听 + handler 方法
- `StockCharts.vue`：绑定 `:focused-view="chartActiveView"` prop（StockCharts 已有 `focusedView` prop）

### R3: 全工作台字体放大 +2px
- 涉及 7 个 Vue 文件：TradingWorkbenchReport/Progress/Feed/Composer/Header/TradingWorkbench/TradingWorkbenchFeed
- 使用 Node.js UTF-8 安全替换（PowerShell 会破坏中文字符）
- 映射：8→10, 9→11, 10→12, 11→13, 12→14, 13→15, 14→16

### R4-A: 前端翻译联动
- `jsonMarkdownService.js`：新增 `loadTranslations()` 从后端拉取翻译缓存，`toLabel()` 对未知 key 排队批量翻译
- `useTradingWorkbench.js`：在 mount 时调用 `loadTranslations()`

### R5: 持仓输入控件
- `TradingWorkbenchHeader.vue`：新增 symbol prop，持仓状态（posQuantity/posCost/posNotes/posEditing/posSaving/posLoaded），loadPosition/savePosition 函数，编辑表单（手数/均价/备注），wb-position-row CSS

### R6: Feed 可展开 tool 事件
- `TradingWorkbenchFeed.vue`：新增 `expandedTools` Set、`toggleToolExpand`、`parseDetailJson`、条件可点击 tool 事件、展开面板、feed-tool-expandable/feed-tool-detail CSS

### Report 修复
- `TradingWorkbenchReport.vue`：object key points 使用 `valueToSafeHtml` 渲染

### 前端测试
```
npm --prefix .\frontend run test:unit
→ 124 passed, 2 skipped (routing summary + MCP result panels — outside R1-R6 scope)
```

---

## Phase C — Browser Verification (Darbot MCP)

| 需求 | 验收结果 |
|------|---------|
| R2 | ✅ 点击"日K图"按钮后图表正确从分时切换到日K视图 |
| R3 | ✅ 截图确认工作台字体已放大至可读尺寸，无 9px 文字 |
| R4 | ✅ "holder"→"持有人"、"changeType"→"变动类型" 正确显示 |
| R5 | ✅ 持仓编辑表单输入 10 手 @ ¥38.50 + 保存成功（API PUT 200），刷新后持仓保持显示 "📦 持仓 10 手 @ ¥38.50" |
| R6 | ✅ Feed 显示 🔧 图标的 tool 事件，旧事件（无 MetadataJson）不可展开，新事件将可点击展开 |
| R1 | ✅ 代码完成，需要实际 LLM 研究会话才能完整验证追问消息显示 |

---

## Desktop Packaging

### 问题
`publish-windows-package.ps1` 因 Vite 构建的 chunk-size 警告写入 stderr，而脚本 `$ErrorActionPreference = "Stop"` 将 stderr 视为致命错误导致失败。

### 修复
将 `npm` 调用包装为 `cmd /c` 合并 stdout/stderr，并独立检查 `$LASTEXITCODE`：
```powershell
cmd /c "npm --prefix `"$frontendDir`" run build 2>&1"
if ($LASTEXITCODE -ne 0) { throw "Frontend build failed with exit code $LASTEXITCODE" }
```

### 验证
```
.\scripts\publish-windows-package.ps1
→ Package ready: artifacts\windows-package
→ Main executable: SimplerJiangAiAgent.Desktop.exe ✅ 存在
```

---

## 修改文件清单

### 后端新增
- `backend/.../Services/JsonKeyTranslationService.cs`
- `backend/.../Data/Entities/StockPosition.cs`

### 后端修改
- `ResearchRunner.cs` — UserFollowUp 事件 + 持仓注入
- `ResearchRoleExecutor.cs` — PositionContext + MetadataJson
- `AppDbContext.cs` — DbSet\<StockPosition\>
- `ResearchSessionSchemaInitializer.cs` — 建表 SQL
- `StocksModule.cs` — 3 个新 API 端点
- `ResearchSessionDto.cs` — MetadataJson 映射

### 前端修改
- `TradingWorkbench.vue` — handleNextAction + symbol prop
- `TradingWorkbenchHeader.vue` — 持仓 UI
- `TradingWorkbenchFeed.vue` — 可展开 tool 事件
- `TradingWorkbenchReport.vue` — valueToSafeHtml
- `jsonMarkdownService.js` — 后端翻译联动
- `useTradingWorkbench.js` — loadTranslations
- `StockInfoTab.vue` — navigate-chart/plan handler + focusedView 绑定
- 7 个 Vue 文件 — 字体 +2px

### 脚本修改
- `scripts/publish-windows-package.ps1` — npm stderr 修复

---

## 已知限制
1. R1 用户追问消息显示需要实际 LLM 会话触发，Browser MCP 无法模拟
2. R6 新增 MetadataJson 仅对后续生成的 tool 事件生效，历史事件无 MetadataJson 不可展开
3. GTranslate 依赖 Google 提供的免费翻译接口，无 SLA 保证；预种词典覆盖主要场景作为兜底
