# GOAL-UI-NEW-001 Phase 0 — 页面级任务清单
> 逐文件 · 逐组件 · 逐验收点
> 生成日期：2026-03-28 | 状态：READY-TO-EXECUTE

---

## 0. Phase 0 目标定义

Phase 0 = **审计 + 冻结设计基线**，不写任何新功能代码。
产出物：
1. 设计 Token 文件（CSS variables）草稿
2. 每个页面的"当前状态快照 + 改造意图"文档（各 1 张表）
3. 组件可触边界白名单（告诉 Dev Agent 哪些文件能改、哪些禁改）
4. 全局布局草图（文字版线框图）

---

## 1. 全局 Shell & 样式层（Phase 0）

### 文件清单

| 文件 | 当前职责 | Phase 0 任务 | 是否禁改 |
|------|----------|--------------|---------|
| `frontend/src/style.css` | 全局基础样式 | 提取所有颜色/字号/间距硬编码值，列成 `--token-*` 草案表 | 可改（仅提取，不重写） |
| `frontend/src/App.vue` | 根组件，含 Tab 导航 | 记录：现有 6 个 Tab 的名称、顺序、对应组件；标记"无路由"问题 | 只读审计 |
| `frontend/index.html` | HTML 入口 | 检查 `<meta viewport>`、`<title>`，记录缺失 | 只读审计 |
| `frontend/vite.config.js` | 构建配置 | 检查是否有 CSS `preprocessorOptions`，是否可接入 css vars | 只读审计 |

### 验收点

- [ ] A0-1：`style.css` 中的所有颜色值被整理为 `colors-audit.md` 表格（hex + 用途 + 出现次数）
- [ ] A0-2：字号层级（h1/h2/p/small/tiny）被列出，标记缺失的层级
- [ ] A0-3：间距 padding/margin 被整理出现有值域（4px/8px/12px/16px/24px/32px？）
- [ ] A0-4：`App.vue` Tab 列表被记录为文字，确认是否需要增减 Tab
- [ ] A0-5：`index.html` viewport meta 验证通过

---

## 2. 股票信息页（StockInfoTab）审计

### 文件清单

| 文件 | 当前职责 | Phase 0 任务 | 是否禁改 |
|------|----------|--------------|---------|
| `frontend/src/modules/stocks/StockInfoTab.vue` | 股票信息页根组件，>1200 行 | 画出当前模板树（缩进列表），标记每个区块的 z-index/sticky 层级 | 只读审计 |
| `frontend/src/modules/stocks/TerminalView.vue` | K线区 sticky 容器 | 记录：`position:sticky`、`top`值、`min-height`，确认不改 | 禁改 |
| `frontend/src/modules/stocks/StockCharts.vue` | K线+分时+策略图表逻辑 | 只记录对外 Props/Emit 接口，不改任何代码 | **严格禁改** |
| `frontend/src/modules/stocks/charting/**` | 图表策略注册/适配器 | 不读、不碰 | **严格禁改** |
| `frontend/src/modules/stocks/StockSearchToolbar.vue` | 顶部搜索框 | 记录现有尺寸约束，标记改造意图（加固定宽度或响应式） | 审计 |
| `frontend/src/modules/stocks/StockTopMarketOverview.vue` | 大盘指数顶部栏 | 记录现有高度/布局方式，标记"能否折叠"意图 | 审计 |
| `frontend/src/modules/stocks/StockTerminalSummary.vue` | K线区右侧摘要 | 记录现有 props，标记是否应移位 | 审计 |
| `frontend/src/modules/stocks/StockMarketNewsPanel.vue` | 本地新闻区 | 记录现有区域高度/滚动策略 | 审计 |
| `frontend/src/modules/stocks/StockNewsImpactPanel.vue` | 新闻影响评估 | 同上 | 审计 |
| `frontend/src/modules/stocks/StockTradingPlanSection.vue` | 当前股票交易计划列 | 记录现有 Props/Emit，标记无"新建"入口 → 重构目标 | **登记为重构对象** |
| `frontend/src/modules/stocks/StockTradingPlanModal.vue` | 交易计划表单模态框 | 记录现有字段列表，标记无分组问题 → 重构目标 | **登记为重构对象** |
| `frontend/src/modules/stocks/StockTradingPlanBoard.vue` | 全局交易计划总览 | 同上，无新建入口 | **登记为重构对象** |
| `frontend/src/modules/stocks/stockInfoTabTradingPlans.js` | 交易计划 composable | 记录现有函数签名列表 | **登记为重构对象** |

### 当前模板树（审计输出目标）

```
StockInfoTab.vue
├── StockSearchToolbar               ← 搜索区
├── StockTopMarketOverview           ← 大盘条
├── TerminalView (sticky)            ← ⛔ 禁改
│   ├── StockCharts                  ← ⛔⛔ 严格禁改
│   └── StockTerminalSummary         ← 审计
├── StockTradingPlanSection          ← 🔴 重构对象
│   └── StockTradingPlanModal        ← 🔴 重构对象
├── StockMarketNewsPanel             ← 布局审计
├── StockNewsImpactPanel             ← 布局审计
└── StockTradingPlanBoard            ← 🔴 重构对象
```

### 验收点

- [ ] A1-1：`StockInfoTab.vue` 模板树被完整记录（含 v-if/v-show 条件）
- [ ] A1-2：现有 sticky 区域层级（z-index + top 值）被列出，确认至少有 2 层冲突问题
- [ ] A1-3：`StockCharts.vue` 对外接口（Props + Emits 清单）被记录，确认：`@view-change`, `@strategy-visibility-change`, `@fullscreen-change` 三个 emit 存在且绑定位置被记录
- [ ] A1-4：三个重构对象文件的"现有 Props 表 + 现有 Emit 表"被整理
- [ ] A1-5：新闻区与交易计划区当前竞争高度的问题被描述（可测量：滚动到计划区需要多少像素）

---

## 3. 情绪轮动页（MarketSentimentTab）审计

### 文件清单

| 文件 | Phase 0 任务 | 是否禁改 |
|------|--------------|---------|
| `frontend/src/modules/market/MarketSentimentTab.vue` | 记录现有区块分布（hero/toolbar/两列/指标/历史/实时），标记认知过载问题 | 只读审计 |
| `frontend/src/modules/market/SectorRotationChart.vue` | 记录图表类型（ECharts），标记是否受保护 | 只读审计，情绪图表不受保护，但注意不改数据逻辑 |

### 验收点

- [ ] A2-1：MarketSentimentTab 区块列表被记录（每个区块：名称/大概高度/用途）
- [ ] A2-2：现有"双列布局"在小窗口下的问题被描述（截图或文字描述均可）
- [ ] A2-3：建议的分 Tab（情绪总览 / 板块轮动 / 历史走势）改造方案被写入审计记录

---

## 4. 全量资讯库页（NewsArchiveTab）审计

### 文件清单

| 文件 | Phase 0 任务 | 是否禁改 |
|------|--------------|---------|
| `frontend/src/modules/stocks/NewsArchiveTab.vue` | 记录：现有过滤器数量、分页策略（Prev/Next）、卡片布局 | 只读审计 |

### 验收点

- [ ] A3-1：当前过滤器列表（keyword/level/sentiment）被记录
- [ ] A3-2：分页策略（Prev/Next）的问题被描述（无跳页、无 URL 参数化）
- [ ] A3-3：建议改造方向（无限滚动 vs 页码分页）被写入

---

## 5. 股票推荐页（StockRecommendTab）审计

### 文件清单

| 文件 | Phase 0 任务 | 是否禁改 |
|------|--------------|---------|
| `frontend/src/modules/stocks/StockRecommendTab.vue` | 记录：MarketSnapshot + ChatWindow 的布局关系 | 只读审计 |

### 验收点

- [ ] A4-1：ChatWindow 组件的 Props/Emit 接口被记录（供后续布局改造参考）
- [ ] A4-2：现有 ChatWindow 的高度策略（fixed? flex?) 被记录

---

## 6. LLM 设置页 & 管理员页审计

### 文件清单

| 文件 | Phase 0 任务 |
|------|--------------|
| `frontend/src/modules/settings/LlmSettingsTab.vue` (或对应文件) | 记录现有表单字段，标记可视化改进点 |
| `frontend/src/modules/admin/AdminTab.vue` (或对应文件) | 记录现有功能区块（开发者模式/trace等），标记布局问题 |

### 验收点

- [ ] A5-1：两个页面的 Form Field 清单被记录
- [ ] A5-2：操作按钮的视觉层级问题被描述

---

## 7. 设计 Token 草案（Phase 0 产出物）

Phase 0 结束时，必须产出 `frontend/src/design-tokens.css`（草案文件，仅变量，不引入任何样式）：

```css
/* 颜色 */
--color-bg-app: #eef1f5;
--color-bg-card: #ffffff;
--color-bg-header: #111827;
--color-text-primary: #1a202c;
--color-text-muted: #6b7280;
--color-text-accent: #3b82f6;
--color-border: #e5e7eb;
--color-danger: #ef4444;
--color-success: #10b981;
--color-warning: #f59e0b;

/* 间距 */
--space-1: 4px;
--space-2: 8px;
--space-3: 12px;
--space-4: 16px;
--space-5: 24px;
--space-6: 32px;

/* 字号 */
--text-xs: 11px;
--text-sm: 12px;
--text-base: 14px;
--text-md: 15px;
--text-lg: 16px;
--text-xl: 18px;
--text-2xl: 22px;

/* 圆角 */
--radius-sm: 4px;
--radius-md: 8px;
--radius-lg: 12px;
--radius-xl: 16px;

/* 阴影 */
--shadow-card: 0 1px 3px rgba(0,0,0,.08), 0 1px 2px rgba(0,0,0,.06);
--shadow-modal: 0 20px 60px rgba(0,0,0,.18);
```

> ⚠️ 注意：草案文件**不得被任何其他文件 import** — 它仅作为下一阶段的设计协议文档使用。

### 验收点

- [ ] A6-1：`design-tokens.css` 文件存在，仅含 CSS 变量声明，无选择器规则
- [ ] A6-2：变量值与 `style.css` 中整理出的实际值对齐（允许小幅优化）
- [ ] A6-3：有中文注释说明每个 token 的用途

---

## 8. Phase 0 整体验收门控 (Gate A)

**Gate A 通过条件（全部满足才允许进入 Phase 1）：**

| 门控项 | 标准 |
|--------|------|
| GA-1 | 设计 Token 草案文件存在且可被 Node 解析（无语法错误） |
| GA-2 | `StockCharts.vue` + `charting/**` 均无任何代码变更（git diff 为空） |
| GA-3 | 5 个页面的审计记录均已产出（可以是同一 Markdown 内分节） |
| GA-4 | 重构目标文件列表（含 4 个交易计划组件）已被 Dev Agent 和 Test Agent 双方确认 |
| GA-5 | 现有单元测试全部通过（`npm run test:unit` 退出码 0） |

---

## 9. 执行日志

| 日期 | 操作 | 执行者 | 结果 |
|------|------|--------|------|
| 2026-03-28 | 产出 Phase 0 任务清单 | PM Agent | ✅ 完成 |
