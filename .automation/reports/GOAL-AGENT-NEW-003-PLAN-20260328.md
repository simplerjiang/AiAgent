# GOAL-AGENT-NEW-003 任务计划书（2026-03-28）

## 目标摘要（中文）
本任务聚焦动态页面可解释性与会话追问编排，解决“看不全、看不懂、看不准、重复重跑”四类核心问题。

用户提出的 5 个需求全部纳入本次范围：
1. `company_overview_analyst` 不得只显示主营业务，必须完整展示其已获取到的公司基础面信息（如市盈率、量比、股东户数、流通市值等）。
2. MCP 不能只显示状态，需提供“可点击文本 + Collapse 折叠面板”，展示可读的 MCP 结果摘要（Markdown 形式，信息完整但不暴露 JSON 原文）。
3. `社交情绪` 板块禁止直接展示 JSON；所有板块统一输出可读 Markdown。
4. 一轮分析结束后，后续追问先由 `组合经理` 进行“问题路由决策”：判断是复用当前报告、局部补跑某些 Agent，还是启动新 turn；该选择权交给 LLM。
5. `研究报告` 板块中所有 JSON 结果统一处理为 Markdown，标题可由 key 生成并做中英文字段映射。

## Objective Summary (EN)
This goal improves dynamic-page transparency and follow-up orchestration quality.

All five requested items are in scope:
1. `company_overview_analyst` must expose all retrieved fundamentals instead of only main business text.
2. MCP UI must show not only status but also a clickable collapse panel with readable markdown summaries (no raw JSON dump).
3. `Social Sentiment` and all other sections must not render raw JSON to end users.
4. After one analysis turn completes, follow-up questions should first be routed by `portfolio manager` to decide reuse vs partial rerun vs new turn.
5. `Research Report` cards must convert JSON outputs into markdown blocks with key-to-Chinese label mapping.

---

## 范围边界
### In Scope
1. 动态页内容呈现层（report/feed/progress/card）中的 JSON-to-Markdown 转换。
2. MCP 工具结果展示层新增 Collapse 交互和摘要渲染。
3. `company_overview_analyst` 结果展示完整度提升（展示层 + contract 校验）。
4. follow-up 路由策略：先走组合经理判定，再执行局部/全量分析路径。
5. 对应的后端 contract、前端渲染器、回归测试与 Browser MCP 验收。

### Out of Scope
1. 新增第三方数据源。
2. 全量重写现有工作台布局。
3. 重做历史数据库结构（仅在必要时增量字段）。

---

## 问题分解与实施方案

## R1 公司概览完整展示（对应需求 1）
### 现状问题
`company_overview_analyst` 最终只露出“主营业务”片段，用户无法判断是否真的拿到更完整的基础面事实。

### 方案
1. 明确 company overview 的展示契约：
   - 经营概览
   - 市盈率（动态/静态，按可得字段）
   - 量比
   - 股东户数
   - 流通市值
   - 行业/地区/关键补充指标
2. 输出层新增“已获取字段清单 + 缺失字段说明”。
3. 若上游字段缺失，显示“数据缺失原因”而非静默省略。
4. 报告卡片强制显示“采集到 X 项，展示 X 项”，避免截断误判。

### 验收标准
1. 同一标的 company overview 卡片可见完整基础面块，不再仅一段主营业务。
2. 缺失字段有显式说明。
3. Browser MCP 可见字段总数与后端返回一致。

---

## R2 MCP 结果可读展开（对应需求 2）
### 现状问题
MCP 只显示状态，不显示结果内容，用户无法评估工具质量。

### 方案
1. 每个 MCP 结果行提供可点击“文字入口”（如“查看摘要”）。
2. 点击后展开 Collapse 面板，展示结构化 Markdown：
   - 关键结论
   - 关键数据点
   - 证据来源与时间
   - 降级或失败说明
3. 禁止 raw JSON 直出；由摘要转换器统一清洗。
4. 对超长内容支持折叠与“继续展开”。

### 验收标准
1. MCP 行具备展开交互，且展开后有可读摘要。
2. 不出现 JSON 原文块。
3. 在成功、降级、失败三种场景下均有可读输出。

---

## R3 全板块 JSON 禁止直出（对应需求 3 与 5）
### 现状问题
`社交情绪` 与 `研究报告` 部分卡片直接显示 JSON，阅读成本高且不专业。

### 方案
1. 建立统一 `JsonToMarkdownPresenter`（前端渲染层）与字段映射字典。
2. key 翻译策略：
   - 通用 key（summary/confidence/risk/trigger/invalidation）固定中译名。
   - 未命中 key 使用标题化 + 兜底中文映射。
3. 对数组、对象、数值、时间戳采用分类型渲染模板。
4. 社交情绪、研究报告、工具摘要三类入口复用同一渲染器。

### 验收标准
1. 目标页面中不再出现裸 JSON 文本输出。
2. 所有卡片标题和字段均为可读文本。
3. 回归测试覆盖对象/数组/空值三类输入。

---

## R4 Follow-up 智能路由（对应需求 4，重要）
### 现状问题
用户追问常常是“基于当前报告补充”，但系统倾向直接启动新一轮全量分析。

### 方案
1. 新增 `组合经理路由判定` 步骤（FollowUpRouting）：
   - `ReuseOnly`: 仅基于现有结论答复
   - `PartialRerun`: 仅调用必要 Agent
   - `FullTurn`: 启动完整新 turn
2. 路由决策由 LLM 给出，并附理由与置信度。
3. 路由结果写入 turn 元数据，前端展示“本次采用策略”。
4. 当 LLM 判定不确定时，允许降级为 FullTurn（安全兜底）。

### 验收标准
1. 追问时可见路由决策结果与理由。
2. 局部补跑场景不再触发全链路无意义重算。
3. 历史 turn 可追溯每轮路由模式。

---

## 执行拆分与顺序
1. Phase A: 后端 contract 与路由决策对象（R4 基础）
2. Phase B: 前端统一 JSON-to-Markdown 渲染器（R3）
3. Phase C: MCP Collapse 展示与 company overview 完整化（R1 + R2）
4. Phase D: 联调、单测、Browser MCP 验收与报告

---

## 测试与验收计划
1. 后端单测：
   - Follow-up 路由判定（ReuseOnly/PartialRerun/FullTurn）
   - company overview 字段完整度与缺失说明
2. 前端单测：
   - JSON-to-Markdown 渲染快照
   - MCP Collapse 交互
   - 社交情绪/研究报告不再输出 JSON
3. Browser MCP：
   - 完整跑一次“首轮分析 + 追问”
   - 验证 MCP 展开内容与研究报告可读性
   - 检查 console/network 无新增错误

---

## 风险与缓解
1. 风险：字段映射不全导致 Markdown 文本不自然。
   - 缓解：维护 key 翻译字典 + 未知 key 兜底规则。
2. 风险：局部补跑的依赖边界不清。
   - 缓解：先实现路由策略，再用白名单控制可局部重跑角色。
3. 风险：不同板块重复实现转换逻辑。
   - 缓解：强制复用统一渲染器与 contract。

---

## 交付物清单
1. 计划报告：本文件。
2. 任务台账：`.automation/tasks.json` 已新增 `GOAL-AGENT-NEW-003`。
3. 运行状态：`.automation/state.json` 已切换到本任务。
4. 后续开发报告：待开发阶段完成后补充 `GOAL-AGENT-NEW-003-DEV-*.md` 与测试报告。
