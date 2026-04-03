# GOAL-RECOMMEND 多 Agent 推荐系统 — 总需求与约束

## 文档目的
1. 本文件是 `GOAL-RECOMMEND` 的唯一总入口，定义总需求、总约束、执行顺序、测试门禁和分任务链接。
2. 具体实现任务拆到独立任务文件，便于分工执行和逐项验收。
3. 设计计划书位于 `docs/GOAL-RECOMMEND-multi-agent-design-plan.md`（v3.0）。

## 总目标
1. 将当前「推荐助手」从单 LLM 问答升级为 **11 Agent + 5 阶段辩论** 的推荐系统。
2. 流水线：**市场扫描 → 板块辩论 → 选股精选 → 个股辩论 → 推荐决策**。
3. Agent 自主决策调用工具（Web 搜索 + 本地 MCP），不硬编码权限矩阵。
4. 追问由 Router LLM 动态派遣，支持部分重跑、全量重跑、交接至 Trading Workbench。
5. 全程结构化、可回溯、可追问，输出 JSON→Markdown 安全渲染。

## 顶层约束
1. Web 搜索三链路（Tavily 主 → SearXNG 降级 → DuckDuckGo 兜底）必须都实现且通过健康检查。
2. WebSearchMcp 是共享基础设施，推荐系统与 Trading Workbench 使用同一实现。
3. 每个 Agent 单次最多 5 次工具调用，超限强制输出。
4. 会话内相同 query 缓存 TTL 5 分钟。
5. 所有面向用户的内容默认中文。
6. JSON 输出必须经 `ensureMarkdown → markdownToSafeHtml → v-html` 安全渲染。
7. 所有推荐必须基于 72 小时内的事件证据。
8. 复用现有 ResearchEventBus 架构、McpToolGateway、ILlmService。

## 当前状态（2026-04-01）
1. **项目无法编译**：AppDbContext 已声明 5 个 Recommendation 实体 DbSet，但实体文件未创建。
2. Program.cs 引用 `RecommendSessionSchemaInitializer.EnsureAsync`，该类也不存在。
3. `Modules/Stocks/Services/Recommend/` 目录为空。
4. `StockRecommendTab.vue` 是一个简单的 ChatWindow + 市场快照侧边栏。
5. Tavily 集成已存在，SearXNG 和 DuckDuckGo 尚未实现。

## 执行阶段

| 阶段 | ID | 名称 | 前置依赖 | 预期交付 |
|------|----|------|----------|----------|
| P0 | GOAL-REC-P0 | 编译恢复 + 规格冻结 | 无 | 项目可编译 + 实体/枚举/Schema 初始化器 + 角色 ID 常量 |
| R1 | GOAL-REC-R1 | Web 搜索基础设施 | P0 | WebSearchMcp 三链路 + 健康检查 + Workbench 同步 |
| R2 | GOAL-REC-R2 | 后端编排引擎 | P0 | RecommendationRunner + RoleExecutor + EventBus + API 端点 |
| R3 | GOAL-REC-R3 | 11 角色 Prompt + 工具调度 | R1 + R2 | 全部角色 Prompt 模板 + function calling schema + 单元测试 |
| R4 | GOAL-REC-R4 | 追问路由器 | R2 + R3 | FollowUpRouter + 部分重跑 / 全量重跑 / Workbench 交接 |
| R5 | GOAL-REC-R5 | 前端推荐工作台 UI | R2（API 可调） | 报告卡片 + 辩论 Feed + 进度条 + 追问框 |
| R6 | GOAL-REC-R6 | 全链路集成验收 | R3 + R4 + R5 | 单测 + Browser MCP + 桌面打包 |

## 任务文件索引
- [P0 编译恢复与规格冻结](./GOAL-REC-P0.md)
- [R1 Web 搜索基础设施](./GOAL-REC-R1.md)
- [R2 后端编排引擎](./GOAL-REC-R2.md)
- [R3 角色 Prompt 实现](./GOAL-REC-R3.md)
- [R4 追问路由器](./GOAL-REC-R4.md)
- [R5 前端推荐工作台](./GOAL-REC-R5.md)
- [R6 全链路验收](./GOAL-REC-R6.md)
