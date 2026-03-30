# Phase 2 — 股票信息页结构重构 · UI 设计规格文档

> UI Designer Agent 产出 · 2026-03-29
> 状态：待 PM Agent 审批

---

## 一、改造后页面整体线框图

### 1.1 全页三段式布局（桌面端 ≥1180px）

```
┌──────────────────────────────────────────────────────────────────────┐
│  ① 顶部操作带（TopBar）                                              │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │  StockSearchToolbar            [彩色/黑白]                     │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │  StockTopMarketOverview (折叠条)       [▼ 展开 / ▲ 收起]       │  │
│  └────────────────────────────────────────────────────────────────┘  │
├──────────────────────────────────────────────────────────────────────┤
│  ② 主工作区（拖拽分栏）                                              │
│                                                                      │
│  ┌──────────────────────────┐║┌─────────────────────────────────┐   │
│  │                          │║│  右栏 SidebarTabs               │   │
│  │  TerminalView (sticky)   │║│  ┌─────┬─────┬──────┬────────┐ │   │
│  │  ┌────────────────────┐  │║│  │计划 │新闻 │AI分析│全局总览│ │   │
│  │  │ StockTerminalSumm- │  │║│  └─┬───┴─────┴──────┴────────┘ │   │
│  │  │ ary                │  │║│    │                             │   │
│  │  └────────────────────┘  │║│    ▼ Tab 内容区                  │   │
│  │  ═══════ 🔽 ═══════════  │║│  ┌─────────────────────────────┐│   │
│  │  ┌────────────────────┐  │║│  │                             ││   │
│  │  │                    │  │║│  │  (活跃 Tab 的组件内容)       ││   │
│  │  │   StockCharts ⛔   │  │║│  │  内部 overflow-y: auto      ││   │
│  │  │   (禁改区域)       │  │║│  │                             ││   │
│  │  │                    │  │║│  │                             ││   │
│  │  │                    │  │║│  │                             ││   │
│  │  └────────────────────┘  │║│  └─────────────────────────────┘│   │
│  │                          │║│                                 │   │
│  └──────────────────────────┘║└─────────────────────────────────┘   │
│                              ║                                       │
│                         Splitter                                     │
│                       (可左右拖拽)                                    │
│                                                                      │
├──────────────────────────────────────────────────────────────────────┤
│  ③ 模态层                                                            │
│  StockTradingPlanModal（覆盖弹窗，不在文档流中）                      │
│  StockMarketNewsPanel 模态展开（全屏模态）                            │
└──────────────────────────────────────────────────────────────────────┘
```

### 1.2 左栏内部细节（TerminalView 内部）

```
┌───────────────────────────────────────────────────┐
│ header: 股票名称（600519）           ¥1,823.00    │
│                                     +12.50 +0.69% │
├───────────────────────────────────────────────────┤
│ StockTerminalSummary                              │
│ ┌─────────────────────────────────────────────┐   │
│ │ 加载进度 / 基本面摘要 / 数据源状态         │   │
│ └─────────────────────────────────────────────┘   │
├═══════════════ chart-resize-handle ═══════════════┤  ← 图表高度拖拽条
│ StockCharts ⛔                                    │
│ ┌─────────────────────────────────────────────┐   │
│ │                                             │   │
│ │         K线图 / 分时图                      │   │
│ │         (禁改，只调容器高度)                │   │
│ │                                             │   │
│ └─────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────┘
```

### 1.3 窄屏降级布局（<1180px）

```
┌─────────────────────────────────────┐
│  StockSearchToolbar                 │
├─────────────────────────────────────┤
│  StockTopMarketOverview (折叠)      │
├─────────────────────────────────────┤
│  TerminalView                       │
│  (宽度 100%，无 sticky)             │
│  ┌─────────────────────────────┐    │
│  │ Summary                     │    │
│  ├─────────────────────────────┤    │
│  │ StockCharts ⛔               │    │
│  └─────────────────────────────┘    │
├─────────────────────────────────────┤
│  SidebarTabs（底部全宽 Tab 切换）   │
│  ┌──────┬──────┬──────┬────────┐   │
│  │ 计划 │ 新闻 │AI分析│全局总览│   │
│  └──┬───┴──────┴──────┴────────┘   │
│     ▼ Tab 内容                      │
│  ┌─────────────────────────────┐    │
│  │ max-height: 50vh            │    │
│  │ overflow-y: auto            │    │
│  └─────────────────────────────┘    │
└─────────────────────────────────────┘
```

---

## 二、右栏 Tab 设计

### 2.1 Tab 列表

| 序号 | Tab 标签 | Tab Key | 包含组件 | 默认活跃 |
|------|----------|---------|----------|----------|
| 1 | 📋 交易计划 | `plans` | `StockTradingPlanSection` | ✅ 是 |
| 2 | 📰 新闻影响 | `news` | `StockNewsImpactPanel` + 内嵌 `StockMarketNewsPanel`（精简版） | |
| 3 | 🤖 AI 分析 | `ai` | `TradingWorkbench` | |
| 4 | 🌐 全局总览 | `board` | `StockTradingPlanBoard` | |

### 2.2 Tab 交互规格

```
┌─────────────────────────────────────────────────────┐
│  Tab Bar                                            │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────────┐   │
│  │ 交易   │ │  新闻  │ │AI 分析 │ │ 全局总览   │   │
│  │ 计划   │ │  影响  │ │        │ │            │   │
│  └────────┘ └────────┘ └────────┘ └────────────┘   │
│  ▔▔▔▔▔▔▔▔                                          │  ← 2px 活跃指示线
├─────────────────────────────────────────────────────┤
│                                                     │
│  Tab 面板内容                                       │
│  height: calc(100vh - 200px)                        │
│  overflow-y: auto                                   │
│  padding: var(--space-3)                            │
│                                                     │
└─────────────────────────────────────────────────────┘
```

**Tab Bar 样式规格：**

| 属性 | 值 |
|------|----|
| 容器高度 | 40px |
| Tab 字号 | `var(--text-md)` (14px) |
| Tab 间距 | `var(--space-0)` (紧贴) |
| Tab padding | `0 var(--space-4)` |
| 默认色 | `var(--color-text-secondary)` |
| 活跃色 | `var(--color-accent)` |
| 活跃指示线 | 底部 2px, `var(--color-accent)`, `border-radius: 1px` |
| Hover 背景 | `var(--color-accent-subtle)` |
| 过渡 | `var(--transition-fast)` |
| 边框 | 底部 `1px solid var(--color-border-light)` |

**Tab 切换行为：**
- 点击切换，无动画过渡（内容直接替换，避免卡顿感）
- 活跃 Tab 持久化到 `localStorage`
- 当有新的计划告警时，"交易计划" Tab 标签右上角显示红色圆点（未读提示）
- 当个股无计划时，Tab 仍然可见但内容区显示空态引导

### 2.3 市场新闻安置方案

**方案：将 `StockMarketNewsPanel` 移入右栏"新闻影响" Tab 内，作为该 Tab 的第一个子区块。**

当前布局中 `StockMarketNewsPanel` 独占 grid 外一整行，导致图表被大幅下压。移入右栏 Tab 后：
- 不再占用主轴垂直空间
- 与个股新闻 (`StockNewsImpactPanel`) 合并为一个信息域，减少认知切换
- 展开为模态框的功能保留不变

Tab "新闻影响" 内部结构：

```
┌─────────────────────────────────────────────┐
│  📰 新闻影响                                │
│                                             │
│  ┌─ 市场要闻 ─────────────────────────────┐ │
│  │ StockMarketNewsPanel (精简嵌入版)      │ │
│  │ 默认显示 3 条，[查看更多] → 模态展开   │ │
│  └────────────────────────────────────────┘ │
│                                             │
│  ┌─ 个股新闻 & AI 影响分析 ──────────────┐ │
│  │ StockNewsImpactPanel                   │ │
│  │ (完整保留现有内容)                     │ │
│  └────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘
```

---

## 三、Splitter（左右分栏拖拽）组件设计

### 3.1 交互规格

```
左栏                    Splitter                    右栏
┌──────────────────────┐║┌─────────────────────────┐
│                      │║│                         │
│   TerminalView       │║│   SidebarTabs           │
│                      │║│                         │
│                      │║│                         │
│                      │║│                         │
│                      │║│                         │
└──────────────────────┘║└─────────────────────────┘
                        ▲
                    Splitter 手柄
                    宽度: 6px
```

### 3.2 尺寸约束

| 参数 | 值 | 说明 |
|------|----|------|
| 默认左栏比例 | 65% | 初始分配 |
| 左栏最小宽度 | 55% | 保证图表可读 |
| 左栏最大宽度 | 75% | 保证右栏可用 |
| 右栏最小宽度 | 320px | 硬约束，保证表单字段完整 |
| Splitter 手柄宽度 | 6px | hover时伸展到 8px |

### 3.3 交互行为

| 操作 | 行为 |
|------|------|
| 鼠标悬停(hover) | 手柄变为 `var(--color-accent)` + `cursor: col-resize` |
| 鼠标拖拽(drag) | 实时调整左右栏宽度，图表自适应 `ResizeObserver` |
| 双击(dblclick) | 重置为默认 65/35 比例 |
| 触摸拖拽(touch) | 同鼠标拖拽，使用 `touchstart/touchmove/touchend` |
| 释放(mouseup) | 将当前比例持久化到 `localStorage` |
| 键盘(a11y) | Splitter 可聚焦，左右方向键可微调（步长 2%） |

### 3.4 Splitter 视觉规格

```
默认态                   Hover态                   拖拽态
┃                        ┃                         ┃
┃  宽: 6px               ┃  宽: 8px                ┃  宽: 8px
┃  色: border-light      ┃  色: accent (蓝)        ┃  色: accent-active
┃  圆角: 3px             ┃  圆角: 4px              ┃  + 散射阴影
┃  opacity: 0.6          ┃  opacity: 1.0           ┃  opacity: 1.0
┃                        ┃                         ┃
```

中部可放一个 grip 图案（3 个横线点）以暗示可拖拽：

```
        ┃
     ···┃···
     ···┃···
     ···┃···
        ┃
```

### 3.5 localStorage Key

```
Key:   sc_splitter_ratio
Value: 0.65  (浮点数，左栏比例)
```

---

## 四、图表高度拖拽规格

### 4.1 handle 区域定义

在 `StockTerminalSummary` 与 `StockCharts` 之间（即 TerminalView 内 summary slot 和 chart slot 的分界处），放置一个水平 resize-handle。

```
┌──────────────────────────────────────┐
│ StockTerminalSummary                 │
│ (摘要区域)                           │
├══════════════════════════════════════┤  ← chart-resize-handle
│  ═══ 🔽 ═══                          │     高度: 8px, hover: 12px
├──────────────────────────────────────┤
│ StockCharts ⛔                       │
│ (容器高度可调)                       │
└──────────────────────────────────────┘
```

### 4.2 实现位置

由于 TerminalView 的 `<script>` 禁改，此 handle 应在 **StockInfoTab.vue 的 `#chart` slot 内**实现，即包裹 `StockCharts` 的 `.stock-chart-section` div 上方。

```vue
<!-- 在 StockInfoTab.vue 中 -->
<template #chart>
  <div class="chart-resize-wrapper">
    <div class="chart-resize-handle" @mousedown="startChartResize" />
    <div class="stock-chart-section" :style="{ height: chartHeight + 'px' }">
      <StockCharts ... />   <!-- 绑定不变 -->
    </div>
  </div>
</template>
```

### 4.3 尺寸约束

| 参数 | 值 |
|------|----|
| 默认图表高度 | 520px |
| 最小高度 | 320px (保证 K 线可读) |
| 最大高度 | `calc(100vh - 280px)` (不超出视口) |
| Handle 高度 | 8px (hover时12px) |
| Handle 外观 | 居中短横线 (40px宽), `var(--color-border-medium)` |

### 4.4 交互行为

| 操作 | 行为 |
|------|------|
| hover | handle 区域变色 + `cursor: row-resize` |
| drag | 实时调整 `.stock-chart-section` 的 height |
| dblclick | 重置为默认 520px |
| release | 持久化到 localStorage |

### 4.5 localStorage Key

```
Key:   sc_chart_height
Value: 520  (整数，像素值)
```

---

## 五、折叠/展开规格

### 5.1 可折叠模块一览

| 模块 | localStorage Key | 默认状态 | 折叠后展示 |
|------|-----------------|----------|-----------|
| StockTopMarketOverview | `sc_collapse_market_overview` | 展开 | 单行摘要：`上证 3,250.12 ↑0.35% │ 深证 ↓0.12% │ 创业板 ↑0.58%` |
| StockTerminalSummary | `sc_collapse_terminal_summary` | 展开 | 单行：`基本面：PE 32.4 │ PB 8.2 │ 市值 2.3万亿` |
| StockMarketNewsPanel (在 Tab 内) | `sc_collapse_market_news` | 展开 | 单行：`市场要闻（12条）` |
| StockNewsImpactPanel (在 Tab 内) | `sc_collapse_news_impact` | 展开 | 单行：`个股新闻影响（5条）` |

### 5.2 折叠按钮设计

```
展开态：
┌──────────────────────────────────────────────────────┐
│  ▼ 大盘指数概览                          [刷新] [─]  │
│  ┌────────────────────────────────────────────────┐   │
│  │  完整内容区                                   │   │
│  └────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────┘

折叠态：
┌──────────────────────────────────────────────────────┐
│  ▶ 大盘指数概览    上证 3,250 ↑0.35%  │ 深证 ↓0.12% │
└──────────────────────────────────────────────────────┘
```

**折叠按钮规格：**

| 属性 | 值 |
|------|----|
| 图标 | ▼（展开）/ ▶（折叠），使用 CSS `transform: rotate` 过渡 |
| 图标尺寸 | 10px |
| 图标色 | `var(--color-text-muted)` |
| 过渡动画 | `max-height` + `opacity` 双重过渡, `var(--transition-normal)` |
| 折叠态行高 | 36px |
| 折叠态字号 | `var(--text-sm)` |
| 折叠态文字色 | `var(--color-text-secondary)` |

### 5.3 展开/折叠交互

| 操作 | 行为 |
|------|------|
| 点击标题行 | 切换折叠/展开 |
| 点击折叠图标 | 切换折叠/展开 |
| 首次加载 | 读取 localStorage，恢复上次状态 |
| 状态变化 | 立即写入 localStorage |

---

## 六、清除内部开发标识

### 6.1 需移除的元素

| 元素 | 位置 | 处理 |
|------|------|------|
| `<p class="panel-kicker">GOAL-012</p>` | StockInfoTab.vue | 直接删除 |
| `<h2>股票信息终端</h2>` | StockInfoTab.vue | 改为不显示或替换为品牌名 |
| `<p class="muted panel-subtitle">左侧聚焦行情与图表…</p>` | StockInfoTab.vue | 直接删除 |
| `<p class="terminal-view-label">TerminalView</p>` | TerminalView.vue | 该文件 template 禁改，保留但通过 CSS `display: none` 隐藏 |

### 6.2 改造后的 panel-header

```
改造后（干净的工具栏风格）：
┌──────────────────────────────────────────────────────────────┐
│  StockSearchToolbar 直接占据顶部                              │
│  (无 GOAL-012 标识、无内部描述文字)                           │
│  搜索框已包含品牌识别，无需额外标题                           │
│                                      [彩色/黑白模式切换按钮]  │
└──────────────────────────────────────────────────────────────┘
```

panel-header 区域缩减为仅保留右上角功能按钮（模式切换），与 SearchToolbar 合并为一行。

---

## 七、CSS 类名规范

### 7.1 命名约定

所有 Phase 2 新增类名使用 `sc-` 前缀（StockCopilot），采用 BEM 变体命名：

```
sc-{block}__{element}--{modifier}
```

### 7.2 需要的新类名清单

| 类名 | 引用的 Token | 用途 |
|------|-------------|------|
| `.sc-workspace` | -- | 替代 `.workspace-grid`，外层容器 |
| `.sc-workspace__left` | -- | 左栏 |
| `.sc-workspace__right` | -- | 右栏 |
| `.sc-splitter` | `--color-border-light` | 分栏拖拽手柄 |
| `.sc-splitter--active` | `--color-accent` | 拖拽中 |
| `.sc-splitter--hover` | `--color-accent`, `--shadow-sm` | 悬停态 |
| `.sc-splitter__grip` | `--color-text-muted` | 中部 grip 图案 |
| `.sc-tabs` | `--color-border-light` (底边) | Tab 容器 |
| `.sc-tabs__item` | `--color-text-secondary`, `--text-md` | Tab 按钮 |
| `.sc-tabs__item--active` | `--color-accent`, `--color-accent-subtle` | 活跃 Tab |
| `.sc-tabs__indicator` | `--color-accent` | 底部指示线 |
| `.sc-tabs__panel` | -- | Tab 内容区 |
| `.sc-tabs__badge` | `--color-danger` | 未读红点 |
| `.sc-chart-handle` | `--color-border-medium` | 图表高度拖拽手柄 |
| `.sc-chart-handle--active` | `--color-accent` | 拖拽中 |
| `.sc-collapsible` | -- | 可折叠区块容器 |
| `.sc-collapsible__header` | `--text-lg`, `--color-text-primary` | 折叠标题行 |
| `.sc-collapsible__toggle` | `--color-text-muted` | 折叠图标 |
| `.sc-collapsible__toggle--open` | `transform: rotate(90deg)` | 展开态图标 |
| `.sc-collapsible__body` | `--transition-normal` | 折叠内容区 |
| `.sc-collapsible__summary` | `--text-sm`, `--color-text-secondary` | 折叠态摘要文字 |

### 7.3 需要替换的硬编码值（StockInfoTab.vue `<style>` 中）

| 当前硬编码 | 替换为 Token |
|-----------|-------------|
| `border-radius: 16px` | `var(--radius-xl)` |
| `padding: 1.5rem` | `var(--space-6)` |
| `gap: 1rem` | `var(--space-4)` |
| `color: #0f172a` | `var(--color-text-primary)` |
| `font-size: 0.72rem` | `var(--text-xs)` |
| `color: #2563eb` | `var(--color-accent)` |
| `background: #e2e8f0` | `var(--color-bg-inset)` |
| `color: #b91c1c` | `var(--color-danger)` |
| `color: #ef4444` | `var(--color-market-rise)` |
| `color: #22c55e` / `#16a34a` | `var(--color-market-fall)` |
| `color: #475569` | `var(--color-text-secondary)` |
| `color: #cbd5e1` | `var(--color-text-disabled)` |
| `border-bottom: 1px solid rgba(226, 232, 240, 0.9)` | `1px solid var(--color-border-light)` |
| `background: rgba(241, 245, 249, 0.9)` | `var(--color-bg-surface-alt)` |
| `border-radius: 999px` | `var(--radius-full)` |
| `border-radius: 18px` | `var(--radius-xl)` |

---

## 八、响应式规格

### 8.1 断点定义

| 断点 | 宽度范围 | 布局模式 |
|------|----------|----------|
| Desktop | ≥1180px | 左右双栏 + Splitter |
| Tablet | 720-1179px | 单栏堆叠，右栏变底部 Tab |
| Mobile | <720px | 单栏精简，Tab 标签缩为图标 |

### 8.2 各断点行为

**Desktop (≥1180px):**
- 双栏布局，Splitter 可拖拽
- TerminalView `position: sticky`
- Tab 全文字展示

**Tablet (720-1179px):**
- 单栏堆叠
- TerminalView 100% 宽度，取消 sticky
- Splitter 隐藏
- SidebarTabs 移到主区下方，全宽显示
- Tab 面板 `max-height: 50vh`，内部滚动
- 图表高度拖拽手柄保留

**Mobile (<720px):**
- 同 Tablet 但 Tab 标签简化
- Tab 标签用 图标 + 短标签：`📋` `📰` `🤖` `🌐`
- panel-header 按钮栏换行
- 折叠模块默认收起

### 8.3 CSS media query 骨架

```css
/* Desktop: 默认（双栏） */
.sc-workspace {
  display: grid;
  grid-template-columns: var(--sc-left-ratio, 65%) 6px 1fr;
  gap: 0;
  min-height: calc(100vh - 200px);
}

/* Tablet: 单栏 */
@media (max-width: 1179px) {
  .sc-workspace {
    grid-template-columns: 1fr;
  }
  .sc-splitter { display: none; }
  .sc-workspace__left .terminal-view {
    position: static;
    min-height: auto;
  }
}

/* Mobile: 紧凑 */
@media (max-width: 719px) {
  .sc-tabs__item { font-size: var(--text-sm); padding: 0 var(--space-2); }
}
```

---

## 九、新文件清单

### 9.1 必须新建的文件

| 文件路径 | 类型 | 职责 |
|----------|------|------|
| `frontend/src/modules/stocks/SidebarTabs.vue` | Vue SFC | 右栏 Tab 容器组件，管理 Tab 切换和面板渲染 |
| `frontend/src/modules/stocks/ResizeSplitter.vue` | Vue SFC | 通用左右分栏 Splitter 组件，支持拖拽、双击重置、键盘控制 |
| `frontend/src/modules/stocks/useResizable.js` | JS Composable | 拖拽逻辑钩子，封装 mousedown/mousemove/mouseup 和 touch 事件，供 Splitter 和图表高度拖拽复用 |
| `frontend/src/modules/stocks/useCollapsible.js` | JS Composable | 折叠/展开逻辑钩子，封装 localStorage 持久化与开关状态管理 |
| `frontend/src/modules/stocks/useLayoutPersistence.js` | JS Composable | 统一管理所有布局偏好的 localStorage 读写（splitter ratio、chart height、collapse states、active tab） |

### 9.2 可选新建的文件（建议但非必须）

| 文件路径 | 类型 | 职责 |
|----------|------|------|
| `frontend/src/modules/stocks/SidebarTabs.spec.js` | Test | SidebarTabs 组件的 Vitest 测试 |
| `frontend/src/modules/stocks/ResizeSplitter.spec.js` | Test | ResizeSplitter 组件的测试 |

### 9.3 需要修改的现有文件

| 文件路径 | 改动范围 |
|----------|----------|
| `frontend/src/modules/stocks/StockInfoTab.vue` | `<template>` 区块重排(移除开发标识、引入 SidebarTabs 和 ResizeSplitter、移动 MarketNews); `<style>` 全量 token 替换 |
| `frontend/src/modules/stocks/TerminalView.vue` | 仅 `<style>`: 隐藏 `.terminal-view-label`, 尺寸参数微调 |
| `frontend/src/modules/stocks/StockMarketNewsPanel.vue` | 仅 `<style>`: 嵌入侧栏需要的紧凑尺寸适配 |

---

## 十、组件接口设计

### 10.1 SidebarTabs.vue

```
Props:
  activeTab: String           — 当前活跃 Tab key
  hasUnreadAlerts: Boolean    — 交易计划 Tab 是否有未读告警

Emits:
  update:activeTab(key)       — Tab 切换

Slots:
  plans                       — 交易计划 Tab 内容
  news                        — 新闻影响 Tab 内容
  ai                          — AI 分析 Tab 内容
  board                       — 全局总览 Tab 内容
```

### 10.2 ResizeSplitter.vue

```
Props:
  direction: 'horizontal' | 'vertical'  — 拖拽方向
  defaultRatio: Number                   — 默认比例 (0-1)
  minRatio: Number                       — 最小比例
  maxRatio: Number                       — 最大比例
  storageKey: String                     — localStorage key

Emits:
  update:ratio(value)                    — 比例变化
  reset                                  — 双击重置

Slots:
  default                                — grip 自定义
```

### 10.3 useCollapsible(storageKey, defaultOpen)

```
Returns:
  isOpen: Ref<Boolean>
  toggle: () => void
  open:   () => void
  close:  () => void
```

### 10.4 useResizable({ direction, min, max, default, storageKey })

```
Returns:
  size: Ref<Number>          — 当前尺寸(比例或像素)
  startResize: (event) => void
  resetSize: () => void
  handleRef: Ref<HTMLElement> — 绑定到 handle DOM
```

---

## 十一、视觉一致性 Checklist

改造完成后，以下视觉元素必须全部使用 design token：

- [ ] `.panel` 背景、圆角、阴影 → token
- [ ] 所有 `#xxxxxx` 硬编码颜色 → `var(--color-*)` token
- [ ] 所有 `Xrem` / `Xpx` 间距 → `var(--space-*)` token
- [ ] 所有 `border-radius` → `var(--radius-*)` token
- [ ] Tab 组件 → 使用 base-components 按钮/badge 风格
- [ ] 折叠组件 → 使用统一过渡 `var(--transition-*)`
- [ ] 空态/错误态 → 使用 base-components `.empty-state` / `.error-state`

---

## 十二、已有文档对照

本设计规格与以下文档协同：
- [GOAL-UI-NEW-001-PLAN](../../.automation/reports/GOAL-UI-NEW-001-PLAN-20260328.md) — 全局计划（Phase 分期）
- [GOAL-UI-NEW-001-STOCK-INFO-LAYOUT-SPEC](../../.automation/reports/GOAL-UI-NEW-001-STOCK-INFO-LAYOUT-SPEC-20260328.md) — 文件触边界与容器约束
- [design-tokens.css](../../frontend/src/design-tokens.css) — Phase 1 产出：全局变量
- [base-components.css](../../frontend/src/base-components.css) — Phase 1 产出：基础组件

---

## 十三、设计决策说明（UX 理由）

### Q: 为什么推荐右栏 Tab 而不是底部 Tab？
**A:** 右栏 Tab 保持图表始终全高可见，是 TradingView/同花顺/东方财富等专业工具的经典布局。底部 Tab 会额外挤压图表高度，且在 Desktop 宽屏上浪费水平空间。

### Q: 为什么把市场新闻移入 Tab 而不是保留在外？
**A:** 当前全宽市场新闻面板高约 80-120px，直接将图表入视口推下半屏。对于交易用户，图表和实时报价是第一注视区域，新闻属于解释层信息（二级），应按需查看而非始终占位。

### Q: 为什么图表高度拖拽 handle 放在 Summary 和 Chart 之间？
**A:** 这是 TerminalView 禁改约束下的最优位置。handle 放在 slot 内而非 TerminalView 组件内，完全不触碰 TerminalView 的 script 和 template。用户直觉上也会在 "文字信息" 与 "图表" 的分界处期望可以调整各自占比。

### Q: Splitter 默认 65/35 而不是 70/30？
**A:** 当前实测 `1.75fr / 0.95fr ≈ 65/35`，保持用户习惯。右栏 35% 在 1920px 屏幕上约 672px，足以展示计划表单和新闻列表。70/30 会让右栏仅 576px，表单内的价格输入框会过于拥挤。

---

*End of Spec*
