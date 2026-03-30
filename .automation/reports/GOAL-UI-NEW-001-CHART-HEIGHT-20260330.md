# GOAL-UI-NEW-001 Chart Height Fix Report
**Date**: 2026-03-30

---

## Summary (EN)

### Problem
The professional chart terminal ("专业看盘终端") displayed charts with insufficient height, causing both the minute chart (分时图) and K-line chart (日K图) to be clipped/truncated.

### Root Cause Analysis
Five compounding CSS issues created the clipping:
1. `.terminal-view` had `position: sticky; top: 1.5rem;` — prevented vertical growth
2. `.terminal-view` had `height: max-content;` — conflicted with grid layout
3. `.terminal-view-chart` had `min-height: 0` — allowed chart container to shrink to zero
4. `.sc-workspace__left` had `overflow: hidden` — clipped terminal content at parent level
5. `.stock-chart-section` used inline `height` (not `minHeight`) set to 520px with `overflow: hidden` — hard-clipped the chart-shell which required ~612px (from `min(68vh, 760px)`)

### Changes Made

**TerminalView.vue**:
- Removed `position: sticky; top: 1.5rem;` from `.terminal-view`
- Removed `height: max-content;` from `.terminal-view`
- Changed `.terminal-view-chart` from `min-height: 0` to `min-height: 400px`

**StockInfoTab.vue**:
- Changed `.sc-workspace__left` from `overflow: hidden` to `overflow: visible`
- Removed `overflow: hidden` from `.stock-chart-section` CSS
- Changed inline style from `height: chartHeight + 'px'` to `minHeight: chartHeight + 'px'`
- Increased `useResizable` defaults: min 320→420, max 800→900, default 520→620

### Verification
- **Unit tests**: 124 passed, 0 failed, 2 skipped ✅
- **Build**: `npm run build` succeeded ✅
- **K-line chart**: Fully visible — candlesticks, MA5/MA10, volume bars, date axis ✅
- **Minute chart**: Fully visible — price line, VWAP, volume bars, time axis ✅
- **Month/Year K-line**: Verified via User Rep Agent ✅
- **Indicator stacking (MACD, RSI)**: No clipping after adding panels ✅
- **All 6 tabs**: Rendering correctly ✅
- **User Representative**: PASSED ✅

### Commands & Results
```
npm --prefix .\frontend run test:unit     → 124 passed, 0 failed, 2 skipped
npm --prefix .\frontend run build         → succeeded
node frontend/check-minute-chart.cjs      → 分时图 tab active, chart-shell 612px, no overflow
```

---

## 摘要 (ZH)

### 问题
专业看盘终端中的分时图和K线图显示高度不足，图表被截断显示不全。

### 根因分析
五个叠加的CSS问题造成了截断：
1. `.terminal-view` 的 `position: sticky` 阻止了垂直增长
2. `.terminal-view` 的 `height: max-content` 与 grid 布局冲突
3. `.terminal-view-chart` 的 `min-height: 0` 允许图表容器缩小到零
4. `.sc-workspace__left` 的 `overflow: hidden` 在父级裁切了内容
5. `.stock-chart-section` 使用固定 `height: 520px` 加 `overflow: hidden`，硬性裁切了需要约612px的图表壳体

### 修改内容
- **TerminalView.vue**: 移除 sticky 定位和 max-content 高度，设置图表区最小高度400px
- **StockInfoTab.vue**: 改 overflow 为 visible，改固定 height 为 minHeight，默认图表高度520→620px

### 验证结果
- 124/124 前端单测通过 ✅
- K线图完整显示（蜡烛图、均线、成交量、日期轴）✅
- 分时图完整显示（价格线、VWAP、昨收基线、成交量、时间轴）✅
- 月K图、年K图正常显示 ✅
- MACD/RSI等指标叠加后不截断 ✅
- User Representative 验收通过 ✅

### User Representative 反馈
- **通过**：图表高度修复完全达标
- 非阻塞建议：
  - `/api/stocks/detail/cache` 404 错误（已知既有问题）
  - 选股后自动滚动到图表区域（后续UX优化）
