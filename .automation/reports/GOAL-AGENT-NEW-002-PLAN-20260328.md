# GOAL-AGENT-NEW-002 优化规划（2026-03-28）

## 来源
用户在 GOAL-AGENT-NEW-001 验收测试中发现 4 个体验问题，本文档对每个问题进行根因分析并制定优化方案。

---

## 问题 1：各阶段获取结果与讨论过程不可见（黑盒问题）

### 用户描述
无法看到"公司概览"、"分析师团队"、"研究辩论"等所有阶段的获取结果和讨论过程，过于黑盒，无法判断 agent 分析内容是否可靠、是否正确。

### 根因分析

**前端层面：**
1. `TradingWorkbench.vue` 使用 Tab 切换（研究报告 / 团队进度 / 讨论动态），默认 Tab 是"研究报告"。用户必须手动切换到"讨论动态"才能看到过程信息。
2. `TradingWorkbenchProgress.vue` 只显示阶段状态标签（执行中/完成/失败）和角色名称，**不展示任何角色输出内容**。
3. `TradingWorkbenchFeed.vue` 的 feed 内容被 CSS `-webkit-line-clamp: 4` 截断到最多 4 行，长内容不可展开。
4. Feed item 只展示 `summary || message || content`，没有"查看完整输出"的交互。

**后端层面：**
5. `ResearchRunner.cs` 在执行期间通过 `ResearchEventBus` 发布大量事件（RoleStarted, ToolDispatched, ToolCompleted, RoleSummaryReady 等），但 Feed 持久化只在 turn 结束时批量写入（`PersistFeedItemsAsync` 在 finally 块中）。运行期间前端 polling 拿到的 feedItems 为空或不完整。
6. `ResearchRoleExecutor.cs` 的角色输出写入 `ResearchRoleState.OutputContentJson`，但这个字段不在 feed 中直接暴露。前端需要额外调 `/turns/{turnId}/artifacts` 才能看到结构化输出，且该端点不在默认轮询路径中。

### 优化方案

#### P1-1：阶段输出透明化（前端）
- Progress 面板的每个角色行增加"展开/收起"按钮，展开后显示该角色的 `OutputContentJson` 摘要（取前 500 字 + "查看完整"链接）。
- 点击"查看完整"弹出 Drawer / Modal 显示角色完整输出，包含 Markdown 渲染。
- 当角色状态为 `Running` 时，显示实时工具调用信息（"正在获取近 30 天日 K..."）。

#### P1-2：Feed 实时推送（后端 + 前端）
- 后端：将 Feed 持久化从"turn 结束后批量写"改为"事件发生时立即写"（每个 EventBus 事件在分发时同步写一条 `ResearchFeedItem`）。
- 前端：轮询间隔从 3 秒缩短到 1.5 秒（运行期间），并切换到增量拉取（`GET /research/sessions/{id}/feed?after={lastTimestamp}`）。
- 中期目标：用 SSE（Server-Sent Events）替代轮询，实现真正的流式推送。

#### P1-3：默认展示策略调整
- 运行期间默认 Tab 自动切换到"讨论动态"（让用户看到过程），完成后自动切换到"研究报告"。
- 在"研究报告"Tab 的每个 block 下方增加"查看该阶段讨论记录"折叠区域，直接内联 feed 子集。

---

## 问题 2：无法单独重启阶段 + 无自重试机制

### 用户描述
Stock Copilot 每个阶段都无法单独重启，如果遇到错误只能整个询问重新启动，并且没有自重试机制，失败了就一直卡住。

### 根因分析

**后端层面：**
1. `ResearchRunner.RunStageAsync` 中，如果阶段返回 `Failed`，立即退出 turn（`break`），整个 turn 标记为 Failed，session 也标记为 Failed。没有任何重试逻辑。
2. `ResearchRoleExecutor.ExecuteRoleAsync` 中，MCP 工具失败会添加降级标记（如果工具是可选的），但 LLM 调用失败直接返回 `RoleExecutionResult.Failed`，不重试。
3. `ResearchContinuationMode` 枚举虽然定义了 `PartialRerun` 和 `FullRerun`，但 ResearchRunner 完全没有实现这两种模式的逻辑。`ReuseScope` 和 `RerunScope` 字段始终为空。
4. 没有任何 API 端点支持单阶段重启（如 `POST /research/stages/{stageId}/retry`）。

**前端层面：**
5. `TradingWorkbenchComposer.vue` 虽然有 `continuationMode` 选择器（延续当前会话 / 新建会话 / 仅刷新新闻 / 重跑风险评估），但后端不处理 `RefreshNews` 和 `RerunRisk`，它们实际上等同于 `ContinueSession`。
6. `TradingWorkbenchProgress.vue` 中 Failed 阶段只显示红色标记，没有任何"重试"按钮。
7. Header 中的错误提示只显示错误文本，没有"重试"或"从当前阶段继续"的操作入口。

### 优化方案

#### P2-1：角色级自动重试（后端）
- 在 `ResearchRoleExecutor.ExecuteRoleAsync` 中增加自动重试逻辑：
  - LLM 调用失败：最多重试 2 次，间隔 2s / 5s（指数退避）。
  - MCP 工具调用失败（非降级型）：最多重试 1 次。
  - 重试时发布 `RetryAttempt` feed 事件，让前端实时看到重试状态。
- 重试次数和间隔可在 `appsettings` 中配置。

#### P2-2：阶段级手动重启（后端 API + 前端交互）
- 后端新增端点：`POST /research/turns/{turnId}/stages/{stageType}/retry`
  - 仅允许对 `Failed` 或 `Degraded` 状态的阶段重启。
  - 创建新的 `ResearchStageSnapshot`（StageRunIndex + 1），重跑该阶段的所有角色。
  - 该阶段之后的阶段自动标记为 `Pending` 并按原流水线继续执行。
  - 复用该阶段之前所有已完成阶段的输出。
- 前端：`TradingWorkbenchProgress.vue` 中 `Failed` 和 `Degraded` 阶段显示"重试该阶段"按钮。

#### P2-3：PartialRerun 模式实现（后端）
- 实现 `ResearchContinuationMode.PartialRerun`：
  - 用户指定 `RerunScope`（如 `["RiskDebate", "PortfolioDecision"]`）。
  - ResearchRunner 在 turn 开始时，将 RerunScope 之外的阶段标记为 `Reused`，直接复制上一轮的 StageSnapshot 和 RoleState。
  - 只重跑 RerunScope 中的阶段。
- 前端 Composer 的 `RefreshNews` 映射为 `PartialRerun + RerunScope = ["AnalystTeam(NewsAnalyst)", "ResearchDebate", ...]`，`RerunRisk` 映射为 `PartialRerun + RerunScope = ["RiskDebate", "PortfolioDecision"]`。

#### P2-4：失败会话恢复
- 当 session 因某个阶段失败而整体 Failed 时，Header 区域显示"从失败阶段继续"按钮。
- 点击后创建新 turn（ContinuationMode = PartialRerun），自动设置 RerunScope 为失败阶段及其后续阶段。

---

## 问题 3：UI 太小，没有放大功能

### 用户描述
GOAL-AGENT-NEW-001 开发的 UI 太小了，应该允许点击按钮放大。

### 根因分析

1. `TradingWorkbench.vue` 的 CSS 限制了 `max-height: calc(100vh - 260px)`，在 `StockInfoTab.vue` 的多面板页面中实际可用高度较小。
2. 工作台嵌入在股票详情页的右侧扩展区，宽度受限于页面布局分栏。
3. 没有任何全屏/放大按钮。当前没有 fullscreen 相关的 CSS 或交互逻辑。
4. Tab 切换意味着同一时间只能看到一个面板内容（报告 / 进度 / 动态），进一步压缩了信息密度。

### 优化方案

#### P3-1：全屏/放大模式
- Header 右侧（刷新按钮旁）增加"全屏"按钮（`⛶` / `全屏`）。
- 点击后 `TradingWorkbench` 切换为 `position: fixed; inset: 0; z-index: 9999`，覆盖整个视口。
- 全屏模式下自动切换为桌面端双栏布局：
  - 左侧 30%：团队进度（Progress）
  - 右侧 70% 上半部分：讨论动态（Feed）
  - 右侧 70% 下半部分：研究报告（Report）
- 用 `Escape` 键或右上角关闭按钮退出全屏。
- 绑定浏览器 `fullscreenchange` 事件同步按钮状态。

#### P3-2：面板可拖拽调整大小
- 非全屏模式下去掉 `max-height` 硬限制，改为用户可拖拽底边调整高度。
- 保存用户调整后的高度到 localStorage，下次打开时恢复。

---

## 问题 4：讨论动态过于教条化/表格化，希望像聊天框一样

### 用户描述
讨论动态太教条化、表格化，希望像 GitHub Copilot Chat 一样，做成聊天框形式。

### 根因分析

1. 当前 `TradingWorkbenchFeed.vue` 使用的是信息卡片列表样式：每条 feed item 是 `icon + metadata + content` 的扁平卡片，左边框颜色区分类型。
2. 所有角色的消息在视觉上完全一致——都是同样大小的灰色卡片。无法直观区分"谁在说话"。
3. 没有头像 / 角色名突出显示 / 消息气泡的视觉差异。
4. 内容被 `-webkit-line-clamp: 4` 强制截断，看不到完整讨论内容。
5. 缺少消息之间的对话流感——没有引用/回复指示，没有时间间隔标记，没有"正在输入"动画。

### 优化方案

#### P4-1：聊天气泡式 Feed 重构
- 将 Feed 从卡片列表改为聊天气泡样式：
  - 每条消息包含：角色头像（圆形 icon）+ 角色名 + 时间戳 + 消息气泡。
  - 不同角色类型使用不同气泡颜色/方向：
    - Analyst 类：左对齐，蓝色系气泡
    - Researcher（Bull）：左对齐，绿色系气泡
    - Researcher（Bear）：左对齐，红色系气泡
    - Manager / Portfolio：左对齐，金色系气泡（权威角色突出）
    - Trader：左对齐，紫色系气泡
    - 工具事件：居中，灰色小字标签（类似聊天中的"系统消息"）
    - 用户 follow-up：右对齐，深色气泡
  - 气泡内容支持 Markdown 渲染（用 `marked` + `DOMPurify`，与 Report 一致）。
  - 取消 4 行截断限制，改为默认展开；超长内容（>800 字）显示"收起"按钮。

#### P4-2：对话流增强
- 阶段转换事件作为"分割线"插入（类似微信群聊的日期分隔线），显示阶段名和状态。
- 辩论消息支持"引用"指示：Bear 回复 Bull 时，气泡顶部显示小字"回复 @Bull Researcher"。
- 运行期间最后一条消息下方显示"正在分析..."动画（typing indicator），标注当前活跃角色名。
- Turn 分隔更加明显：每个 Turn 开头显示用户问题 + 执行模式的卡片。

#### P4-3：消息交互
- 每条消息支持"复制内容"按钮。
- Analyst 类消息支持"查看数据来源"展开面板（显示该角色使用的 MCP 工具调用列表和结果摘要）。
- Manager/Portfolio 决策消息支持"查看推理依据"展开面板。

---

## 开发优先级与排期建议

### Phase A（核心体验修复）—— 建议优先
| 编号 | 任务 | 涉及 | 预估复杂度 |
|------|------|------|-----------|
| P4-1 | 聊天气泡式 Feed 重构 | 前端 | 中 |
| P4-2 | 对话流增强（分割线、引用、typing） | 前端 | 中 |
| P1-3 | 默认展示策略调整（运行时切 Feed, 完成切 Report） | 前端 | 低 |
| P3-1 | 全屏/放大模式 | 前端 | 中 |
| P2-1 | 角色级自动重试（指数退避） | 后端 | 中 |

### Phase B（管道强化）
| 编号 | 任务 | 涉及 | 预估复杂度 |
|------|------|------|-----------|
| P1-2 | Feed 实时推送（立即写 + 增量拉取） | 后端 + 前端 | 中 |
| P1-1 | 阶段输出透明化（角色行展开） | 前端 | 中 |
| P2-2 | 阶段级手动重启 API + 前端按钮 | 后端 + 前端 | 高 |
| P2-4 | 失败会话恢复（"从失败阶段继续"） | 后端 + 前端 | 中 |

### Phase C（深度功能）
| 编号 | 任务 | 涉及 | 预估复杂度 |
|------|------|------|-----------|
| P2-3 | PartialRerun 模式完整实现 | 后端 | 高 |
| P1-2+ | SSE 替代轮询 | 后端 + 前端 | 高 |
| P3-2 | 面板可拖拽调整大小 | 前端 | 低 |
| P4-3 | 消息交互（复制、数据来源、推理依据） | 前端 | 中 |

---

## 验收标准

### Phase A 验收 Checklist
- [ ] 讨论动态展示为聊天气泡样式，不同角色有明确视觉区分
- [ ] 阶段转换显示为分割线，辩论消息有引用指示
- [ ] 运行期间默认展示"讨论动态"Tab，完成后自动切"研究报告"
- [ ] 全屏按钮可用，全屏模式下信息密度明显提升
- [ ] LLM 调用失败自动重试 2 次，前端可见重试状态
- [ ] 聊天气泡内容支持 Markdown 渲染，不再截断

### Phase B 验收 Checklist
- [ ] Feed 事件在发生时实时写入 DB，前端增量拉取间隔 ≤ 1.5s
- [ ] Progress 面板角色行可展开查看输出摘要
- [ ] Failed/Degraded 阶段显示"重试"按钮，点击后仅重跑该阶段
- [ ] 失败 session 显示"从失败阶段继续"入口

### Phase C 验收 Checklist
- [ ] PartialRerun 模式可复用未变阶段、仅重跑指定阶段
- [ ] SSE 推送替代轮询，Feed 实时刷新无感延迟
- [ ] 工作台高度可拖拽，设置自动保存
- [ ] 消息支持复制、查看数据来源、查看推理依据
