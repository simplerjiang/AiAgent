# GOAL-REC-FEED-OPT: RecommendFeed 优化方案

## 问题总结

当前 `RecommendFeed.vue` 存在以下问题：

1. **前端卡住 / 无反馈**（已定位根因）
   - `expandedItems` 使用原始 `Set()` 而非 `ref(new Set())`，Vue 无法追踪其变化，导致展开/折叠按钮点击后模板不刷新。
   - 辩论 Tab 在流水线运行期间没有任何打字动画/加载指示器，用户无法感知后端正在工作。
   - 没有自动滚动到最新事件，新事件在视口外用户无法察觉。

2. **工具调用事件体验差**
   - 后端 `RecommendationRoleExecutor.cs` 中 `ToolDispatched` 和 `ToolCompleted` 事件的 `DetailJson` 始终传 `null`，前端无数据可展示。
   - 前端工具事件只显示一行文字 + emoji，没有可展开的请求/返回数据面板。

3. **布局简陋**
   - 没有聊天气泡式布局（TradingWorkbenchFeed 有完整的左对齐角色气泡 + 右对齐用户气泡）。
   - 角色事件使用普通 card 而非对话气泡，缺少头像、角色名颜色标注。
   - 长内容没有折叠/展开控制。

4. **缺少功能性细节**
   - MCP 工具名和角色 ID 未翻译为中文（TradingWorkbenchFeed 有完整的翻译映射）。
   - 没有 Turn 分组 / Stage 分割线样式。
   - 没有 lifecycle 事件压缩（RoleStarted/RoleCompleted 应作为低优先级 dim 展示）。

## 参考实现

`TradingWorkbenchFeed.vue` 已实现完整的聊天气泡体验，可作为 1:1 参考：
- 聊天气泡布局（role 左对齐 + user 右对齐）
- 角色头像 + 颜色 + 名称
- 工具事件可展开详情（toolName → 中文标签、status、symbol、resultPreview）
- 打字动画指示器（`.feed-typing` 三点动画）
- 自动滚动到底部（`watch items.length → scrollIntoView`）
- 长内容折叠（`isLongContent` + `collapsed` class）
- lifecycle 事件压缩为一行点
- Stage 分隔线

## 任务拆分

### T1: 后端 — ToolDispatched/ToolCompleted 填充 DetailJson

**文件**: `backend/.../Recommend/RecommendationRoleExecutor.cs`

**改动**:
1. `ToolDispatched` 事件：DetailJson 改为 `JsonSerializer.Serialize(new { toolName, args })`.
2. `ToolCompleted` 事件：DetailJson 改为 `JsonSerializer.Serialize(new { toolName, status = "Completed", resultPreview = Truncate(toolResult, 2000) })`.
3. 添加私有辅助方法 `Truncate(string, int)`.

**验收**: 单元测试断言 ToolDispatched/ToolCompleted 事件的 DetailJson 非 null，且可反序列化。

### T2: 前端 — 修复 expandedItems 的 reactivity bug

**文件**: `frontend/src/modules/stocks/recommend/RecommendFeed.vue`

**改动**:
```js
// 修改前（非 reactive）
const expandedItems = new Set()
const toggleExpand = id => {
  if (expandedItems.has(id)) expandedItems.delete(id)
  else expandedItems.add(id)
}

// 修改后（reactive）
const expandedItems = ref(new Set())
const toggleExpand = id => {
  const s = new Set(expandedItems.value)
  s.has(id) ? s.delete(id) : s.add(id)
  expandedItems.value = s
}
```

模板中 `expandedItems.has(...)` 改为 `expandedItems.value.has(...)`.

**验收**: 点击"展开详情"按钮后详情区域正确显示/隐藏。

### T3: 前端 — 添加自动滚动到最新事件

**文件**: `frontend/src/modules/stocks/recommend/RecommendFeed.vue`

**改动**:
1. 添加 `ref` 引用底部锚点元素。
2. `watch(feedItems.length)` 触发 `nextTick(() => feedEnd.scrollIntoView({ behavior: 'smooth' }))`.

**验收**: SSE 新事件到达时，Feed 自动滚动到底部。

### T4: 前端 — 添加打字动画指示器

**文件**: `frontend/src/modules/stocks/recommend/RecommendFeed.vue`

**改动**:
1. 接收新 prop `isRunning: Boolean`.
2. 在 feed 底部添加 `.feed-typing` 元素（复用 TradingWorkbenchFeed 的三点动画 CSS）。
3. `StockRecommendTab.vue` 传递 `isRunning` prop。

**验收**: 流水线运行期间底部显示"团队分析中..."动画。

### T5: 前端 — 聊天气泡式布局重构

**文件**: `frontend/src/modules/stocks/recommend/RecommendFeed.vue`

**改动**: 参照 TradingWorkbenchFeed，将整个模板重构为气泡布局：
1. `itemKind(item)` 分类函数：区分 user-query / hidden / divider / tool / system / lifecycle / role。
2. Role 事件 → 左对齐气泡（avatar + name + bubble）。
3. User 事件 → 右对齐气泡。
4. Stage 事件 → 居中分割线。
5. Tool 事件 → 可展开详情行（读取 DetailJson 解析为 sections）。
6. Lifecycle 事件 → 灰色压缩行。
7. 长内容折叠/展开。

**CSS**: 复用 TradingWorkbenchFeed 的 `.feed-msg` / `.feed-bubble` / `.feed-avatar` 样式体系。

**验收**: 辩论过程视觉效果与 TradingWorkbenchFeed 一致。

### T6: 前端 — MCP 工具名 & 角色 ID 中文翻译

**文件**: `frontend/src/modules/stocks/recommend/RecommendFeed.vue`

**改动**:
1. 添加推荐系统专用角色翻译映射（11 个 recommend_ 系角色 → 中文标签 + 颜色 + icon）。
2. 添加工具名翻译映射（web_search → "网页搜索"等推荐系统工具 + 复用已有 MCP 翻译）。
3. Summary 文本中的英文角色 ID 和工具名替换为中文。

**验收**: Feed 中所有角色名和工具名显示中文。

### T7: 集成测试 & 回归

1. 确认 `StockRecommendTab.spec.js` 现有测试仍通过。
2. 补充 RecommendFeed 组件测试：展开/折叠、空列表占位、typing indicator。
3. 运行 `npm --prefix frontend test` 全量通过。
4. Browser MCP 验证：启动后端 → 创建推荐 → 观察辩论 Tab 气泡布局、工具展开、自动滚动、打字动画。

## 执行顺序

```
T1 (backend DetailJson)
  ↓
T2 (fix reactivity) → T3 (auto-scroll) → T4 (typing indicator)
  ↓
T5 (bubble layout refactor) → T6 (translations)
  ↓
T7 (test & validate)
```

T1 独立于前端，可先行。T2-T4 是快速修复，可并行。T5 是最大变更，依赖 T2 的修复。T6 在 T5 之后微调。T7 收尾。

## 风险与注意事项

1. T5 重构幅度大，需确保持久化的 feedItems（从 session.turns 加载）和 SSE live events 两个数据源都正确渲染。
2. 后端 DetailJson 中的 `resultPreview` 截断长度需设上限（建议 2000 字符），避免 SSE 单消息过大导致前端解析卡顿。
3. TradingWorkbenchFeed 的 CSS 使用了 scoped 样式和 CSS 变量，RecommendFeed 需确保使用相同的变量体系，避免重复定义。
