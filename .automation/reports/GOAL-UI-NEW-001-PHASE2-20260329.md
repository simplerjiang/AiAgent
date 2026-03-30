# GOAL-UI-NEW-001 Phase 2 Completion Report

**Date**: 2026-03-29
**Phase**: 2 — Stock Info Page Structure Refactoring
**Status**: ✅ COMPLETED

---

## EN: Summary

### What Was Done

Phase 2 restructured the Stock Info page from a two-column flat layout to a three-column layout with a draggable splitter and tabbed sidebar:

1. **New Composables** (3 files):
   - `useLayoutPersistence.js` — localStorage read/write with `sc_` prefix, reactive `usePersistedRef`
   - `useCollapsible.js` — collapse/expand state with persistence
   - `useResizable.js` — generic drag-resize composable for splitter and chart height

2. **New Vue Components** (2 files):
   - `SidebarTabs.vue` — 4-tab container (plans/news/AI/board) with ARIA, v-show switching, localStorage persistence
   - `ResizeSplitter.vue` — drag handle with grip dots, horizontal/vertical variants, 18px interact area

3. **StockInfoTab.vue Restructuring**:
   - Removed `.panel-header` with GOAL-012 label, internal descriptions
   - Added `.sc-workspace` 3-column CSS grid: left (65%) + splitter (auto) + right (1fr)
   - Left: TerminalView (summary + chart with height drag)
   - Right: SidebarTabs with 4 slots (plans, news, AI, board)
   - MarketNewsPanel moved from top-level to "news" tab
   - All styles tokenized with design-tokens

4. **Bug Fixes** (from User Rep review):
   - Hidden dev-facing labels (TerminalView, Top Market Tape, Market Wire) via CSS `:deep()`
   - Fixed splitter drag: `overflow: hidden` on left column, 18px splitter width, z-index 10
   - Right panel `overflow-y: auto` for scrollable tab content
   - Hidden description texts from search toolbar, market overview, terminal empty state

### Files Modified
| File | Change |
|------|--------|
| `frontend/src/modules/stocks/useLayoutPersistence.js` | NEW — persistence composable |
| `frontend/src/modules/stocks/useCollapsible.js` | NEW — collapse composable |
| `frontend/src/modules/stocks/useResizable.js` | NEW — drag-resize composable |
| `frontend/src/modules/stocks/SidebarTabs.vue` | NEW — 4-tab sidebar component |
| `frontend/src/modules/stocks/ResizeSplitter.vue` | NEW — splitter handle component |
| `frontend/src/modules/stocks/StockInfoTab.vue` | MAJOR — template + styles restructured |
| `frontend/src/modules/stocks/StockInfoTab.panel-ui.cases.js` | Selector update |

### Test Results
- **Unit Tests**: 124 passed, 0 failed, 2 skipped (16 test files)
- **Build**: Successful — 101.13 kB CSS, 618.92 kB JS
- **Browser Validation**: All 4 tabs click/switch correctly, splitter drag works, labels hidden

### Known Remaining Items (P2, deferred)
- `StockTradingPlanBoard` "全局总览" shows "Failed to fetch" — pre-existing API/data issue
- TerminalView heading "专业看盘终端" still visible in empty state — template is restricted
- 404 errors for `/api/stocks/detail/cache` and `/api/stocks/research/active-session` — backend endpoints not yet implemented
- Market overview takes significant vertical space — User Rep suggested default collapse (future Phase)
- Tab-switching friction for power users wanting to see news+AI+chart simultaneously (future UX consideration)

---

## ZH: 摘要

### 完成内容

Phase 2 将股票信息页从两列平铺布局重构为三列布局（左侧终端+图表 | 拖拽分隔条 | 右侧Tab面板）：

1. **新增3个组合式函数**：布局持久化、折叠控制、拖拽调整大小
2. **新增2个Vue组件**：SidebarTabs（4标签页容器）、ResizeSplitter（拖拽手柄）
3. **StockInfoTab.vue 大规模重构**：移除开发标签，新增CSS Grid三列布局，MarketNewsPanel移至"新闻影响"标签页
4. **User Rep 反馈修复**：隐藏所有开发标签和说明文字，修复分隔条拖拽功能，修复右侧面板高度

### 测试结果
- 单元测试：124通过，0失败
- 构建：成功
- 浏览器验证：4个Tab正常切换，分隔条拖拽正常，开发标签已隐藏

### 遗留项（P2，后续处理）
- 全局总览Tab的"Failed to fetch"是已有的API问题
- 市场总览带占用较多垂直空间，建议后续默认折叠
- 404错误来自未实现的后端接口
