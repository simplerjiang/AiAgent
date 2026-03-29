# 股票信息页 — "只改布局不改逻辑"实施单
> GOAL-UI-NEW-001 · Phase 2 先决文档
> 生成日期：2026-03-28 | 状态：待 Phase 0 审计完成后执行

---

## 一、文件可触边界表（File Touch Policy）

### 🔴 严格禁改文件（任何人、任何时候都不得修改）

| 文件路径 | 禁改理由 |
|----------|----------|
| `frontend/src/modules/stocks/StockCharts.vue` | K线/分时/策略图表全部渲染逻辑，改坏无法恢复 |
| `frontend/src/modules/stocks/charting/chartStrategyRegistry.js` | 策略注册表，改动会使所有策略信号消失 |
| `frontend/src/modules/stocks/charting/useStockChartAdapter.js` | 图表适配器，与 klinecharts 强绑定 |
| `frontend/src/modules/stocks/charting/chartRegistry.js` | 图表注册，含 pane/indicator 定义 |
| `frontend/src/modules/stocks/charting/chartIndicators.js` | 自定义指标计算逻辑 |
| `frontend/src/modules/stocks/charting/**` (全目录) | 所有 charting 子文件，一律禁改 |
| `frontend/src/modules/stocks/TerminalView.vue` (逻辑部分) | sticky 容器的 `<script>` 逻辑禁改，仅允许改 `<style>` 中的尺寸参数 |

> **⚠️ 违规处理**：Dev Agent 若修改上述文件，PM Agent 将在代码审查时强制回滚，并要求重新执行所有图表回归测试。

---

### 🟡 限制修改文件（仅可改指定类型的内容）

| 文件路径 | 允许改的内容 | 禁止改的内容 |
|----------|-------------|-------------|
| `frontend/src/modules/stocks/TerminalView.vue` | `<style>` 中的 `min-height`、`top`（sticky 偏移量）、容器背景渐变 | `<script>` 中所有逻辑，`<template>` 中的 slot 绑定名称 |
| `frontend/src/modules/stocks/StockInfoTab.vue` | `<template>` 中区块的顺序/包裹 div/class 名称/v-show 条件、`<style>` 全部 | `<script>` 中与 StockCharts 相关的 `emit` 处理函数（`handleViewChange`、`handleStrategyVisibilityChange`、`handleFullscreenChange`）、对 `StockCharts` 的 `ref` 引用及 `chart.xxx()` 调用 |
| `frontend/src/modules/stocks/StockTerminalSummary.vue` | 布局 CSS（display/flex/gap）、字体大小、颜色 Token 替换 | Props 接口定义（不能改接收的字段名），与父组件的 emit |
| `frontend/src/modules/stocks/StockSearchToolbar.vue` | 容器宽度、padding、响应式断点 CSS | 搜索请求逻辑、防抖设置 |
| `frontend/src/modules/stocks/StockTopMarketOverview.vue` | 整体高度、折叠/展开动画 | 行情数据轮询逻辑 |
| `frontend/src/modules/stocks/StockMarketNewsPanel.vue` | 区域最大高度、滚动方向 | 新闻拉取 API 调用 |
| `frontend/src/modules/stocks/StockNewsImpactPanel.vue` | 同上 | 同上 |

---

### 🟢 可自由修改文件（布局+逻辑均可，但需保持接口向后兼容）

| 文件路径 | 改造目标 |
|----------|----------|
| `frontend/src/modules/stocks/StockTradingPlanSection.vue` | 完全重写（详见交易计划重构专项计划） |
| `frontend/src/modules/stocks/StockTradingPlanModal.vue` | 完全重写 |
| `frontend/src/modules/stocks/StockTradingPlanBoard.vue` | 完全重写 |
| `frontend/src/modules/stocks/stockInfoTabTradingPlans.js` | 完全重写（保留原有函数名，向后兼容） |
| `frontend/src/style.css` | 引入 design-tokens.css（Phase 1 完成后），替换硬编码值 |

---

### 🆕 新建文件（允许在以下目录下创建）

| 新建路径 | 用途 |
|----------|------|
| `frontend/src/design-tokens.css` | 设计 Token（Phase 0 产出） |
| `frontend/src/modules/stocks/TradingPlanDrawer.vue` | 交易计划创建/编辑抽屉（替代旧 Modal） |
| `frontend/src/modules/stocks/TradingPlanWorkbench.spec.js` | 交易计划重构后的 Vitest 测试 |

---

## 二、布局改造目标（Layout Transformation）

### 2.1 当前布局问题（基于审计）

```
当前页面 StockInfoTab（竖向，从上到下）：
┌─────────────────────────────────────┐
│ StockSearchToolbar                  │ ← 高度 ~60px
├─────────────────────────────────────┤
│ StockTopMarketOverview              │ ← 高度 ~80px
├─────────────────────────────────────┤
│ TerminalView (sticky)               │ ← min-height: calc(100vh-238px)
│  ┌─────────────────┬──────────────┐│
│  │  StockCharts    │TerminalSum-  ││  ← 图表占主要空间
│  │  ⛔禁改         │mary 📋       ││
│  └─────────────────┴──────────────┘│
├─────────────────────────────────────┤  ← ⬇ 以下区域需要大量滚动才能到达
│ StockTradingPlanSection             │ ← 问题：无新建入口，黑暗区
├─────────────────────────────────────┤
│ StockMarketNewsPanel                │ ← 新闻挤在计划下方
├─────────────────────────────────────┤
│ StockNewsImpactPanel                │
├─────────────────────────────────────┤
│ StockTradingPlanBoard               │ ← 总览更深处，基本无人使用
└─────────────────────────────────────┘
```

**核心问题**：交易计划和新闻区被埋在 sticky 图表下方深处，用户不知道它们存在。

### 2.2 改造后目标布局

```
改造后（双栏或 Tab 切换方案）：

方案 A — 右侧信息面板（推荐）:
┌────────────────────────┬──────────────────────┐
│ StockSearchToolbar     │                      │
├────────────────────────┤                      │
│ StockTopMarketOverview │  右栏固定（可拖拽调宽）│
├────────────────────────┤                      │
│ TerminalView (sticky) │  [Tabs]               │
│  StockCharts ⛔        │  ├ 交易计划 [+ 新建]  │
│  StockTerminalSummary  │  ├ 新闻 & 影响        │
│                        │  └ 基本面             │
└────────────────────────┴──────────────────────┘
 左栏宽度：可配置 55%-75%    右栏：余量

方案 B — 底部 Tab 分区（备选）:
┌─────────────────────────────────────┐
│ StockSearchToolbar + TopMarket      │
├─────────────────────────────────────┤
│ TerminalView (sticky)               │
│  StockCharts ⛔ | StockTerminalSum  │
├─────────────────────────────────────┤
│ [Tab: 交易计划 | 新闻 | 基本面]     │ ← Tab 切换，不滚动
│                                     │
│ Tab 内容区（固定高度，内部滚动）    │
└─────────────────────────────────────┘
```

**推荐方案 A**：右侧信息面板，与图表并排。这是 TradingView 式经典布局，信息密度高但不遮挡图表。

---

## 三、容器尺寸约束（必须遵守）

| 容器 | 约束 | 来源 |
|------|------|------|
| `TerminalView` 最小高度 | `min-height: calc(100vh - 238px)` | 现状（不得缩小） |
| 右栏最小宽度 | `320px` | 新增约束（保证计划表单可用） |
| 右栏默认宽度 | `380px`（可拖拽 320-580px） | 新增约束 |
| 右栏内 Tab 内容高度 | `calc(100vh - 180px)`，内部 overflow-y: auto | 新增约束 |
| `StockCharts` 宽度 | `左栏宽度 - 32px padding` | 由容器决定，图表自适应 |
| Drawer（交易计划表单） | `width: 480px`，`position: fixed; right: 0` | 新增 |

---

## 四、必须保留不变的 Props/Emit 接口

### 4.1 StockCharts.vue 对外接口（**绝对不得改变**）

```javascript
// Props（不得增减）
props: {
  symbol: String,
  detail: Object,        // StockDetailDto
  viewMode: String,      // 'minute' | 'day' | 'week' | 'month'
  strategies: Array,
  // ... 其他 props — 由 Phase 0 审计记录完整列表
}

// Emits（不得增减，事件名不得改变）
emits: [
  'view-change',               // 切换分时/日K/周K/月K
  'strategy-visibility-change', // 策略显示/隐藏
  'fullscreen-change'           // 全屏切换
]
```

### 4.2 StockInfoTab.vue 中对 StockCharts 的绑定（禁改这一片段）

```vue
<!-- ⛔ 这段模板绑定 — 不得以任何形式修改 -->
<StockCharts
  :symbol="..."
  :detail="..."
  :viewMode="..."
  :strategies="..."
  @view-change="handleViewChange"
  @strategy-visibility-change="handleStrategyVisibilityChange"
  @fullscreen-change="handleFullscreenChange"
/>
```

> **规则**：以上 `@view-change` / `@strategy-visibility-change` / `@fullscreen-change` 的处理函数名称**可以保持不变**，函数的内部实现也**不得修改**。布局改造仅允许调整包裹这段代码的父容器 div。

---

## 五、防回归检查清单（每次提交前验证）

### 5.1 图表功能回归

| 检查项 | 验证方法 |
|--------|----------|
| 分时图渲染正常 | Browser MCP → 选股票 → 确认分时图有数据 |
| 日K图渲染正常 | 切换"日K" → 确认 K 线有颜色和 MA 线 |
| 策略信号显示正常 | 打开策略开关 → 确认指标出现 |
| 全屏切换正常 | 点击全屏按钮 → 图表铺满 → 再点退出 |
| K线窗口切换（周/月K） | 切换视图 → 确认数据重新加载 |

### 5.2 单元测试回归

```bash
# 必须全部通过
npm --prefix .\frontend run test:unit
```

重点关注：
- `StockInfoTab.spec.js`（如存在）
- `TradingWorkbench.spec.js`（开发完成后）
- `stockInfoTabTradingPlans.spec.js`（如存在）

### 5.3 后端回归

```bash
# 必须全部通过（如触动后端文件）
dotnet test backend/SimplerJiangAiAgent.Api.Tests
```

---

## 六、开发顺序建议（布局改造）

1. **先做 Phase 0 审计**（读文件，不写代码）
2. **做 design-tokens.css**（新文件，不影响任何组件）
3. **做交易计划 Drawer**（新建 `TradingPlanDrawer.vue`，完全新文件，零风险）
4. **改 `stockInfoTabTradingPlans.js`**（添加手动创建函数，不删现有函数）
5. **改 `StockTradingPlanSection.vue`**（增加新建按钮，连接 Drawer）
6. **改 `StockInfoTab.vue` 布局**（调整容器 div，引入右栏，图表绑定片段不动）
7. **最后改 style.css**（替换硬编码颜色为 token 引用）

> 规则：每步完成后必须运行 `npm run test:unit` 验证无回归。步骤 6 完成后必须执行图表功能回归清单 5.1。

---

## 七、Dev Agent 指令摘要

Dev Agent 收到此文档时，需遵守以下执行约束：

```
1. 在任何 git diff 中，charting/ 目录下的文件必须为空（零变更）
2. StockCharts.vue 的 git diff 必须为空
3. TerminalView.vue 的 <script> 块的 git diff 必须为空
4. StockInfoTab.vue 中 @view-change / @strategy-visibility-change / @fullscreen-change
   的相关处理函数（含函数体）的 git diff 必须为空
5. 如在实现中遇到需要修改上述文件的情况，必须停下来向 PM Agent 报告，
   而不是擅自修改后再汇报
```

---

## 八、执行日志

| 日期 | 操作 | 执行者 | 结果 |
|------|------|--------|------|
| 2026-03-28 | 产出布局改造实施单 | PM Agent | ✅ 完成 |
