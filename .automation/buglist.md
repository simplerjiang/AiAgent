# 2026-04-06 未解决 Bug 清单

- 说明：本文件只保留当前仍未完全解决、仍需继续验证或继续跟踪的 Bug。
- 已解决项归档：见 `.automation/buglist-resolved-20260323.md`。
- 当前开放项数量：5

## 当前开放项（2026-04-05 人工补录，2026-04-06 结构化整理）

### Bug 1: 全量资讯库批量清洗待处理不完整 

- Bug ID：BUG-20260405-01
- 来源：2026-04-05 人工补录
- 严重级别：高
- 当前状态：2026-04-06 本轮代码修复已完成，待 Test Agent 与 User Representative Agent 验证
- 复现摘要：进入“全量资讯库”点击“批量清洗待处理”后，界面很快提示完成，但库内仍有大量 `IsAiProcessed = false` 的本地事实记录残留；已确认旧接口只调用 `ProcessMarketPendingAsync()`，且单次只扫一部分 market 数据。
- 验收说明：按钮触发后必须覆盖 market、sector、stock 三类 pending 本地事实；只有在返回 `remaining = 0` 时才能提示“完成”，否则必须展示剩余数量或明确停止原因。

### Bug 2: 4月5日LLM日志与MCP失效排查

- Bug ID：BUG-20260405-02
- 来源：2026-04-05 人工补录
- 严重级别：高
- 当前状态：2026-04-08 source mode 已关闭；根因锁定为 Ollama keep_alive 与本地模型 JSON 契约问题，live-gate 与 recommendation 复测通过，剩余 latency / degraded 风险待单独跟踪
- 复现摘要：需要读取 4 月 5 日 LLM 日志，逐项检查近期 MCP 是否存在失效、退化或不可用情况，并形成明确问题清单。
- 验收说明：完成日志排查、逐个 MCP 可用性验证，并输出失效原因、修复建议与复测结果。

### Bug 4: 本地模型完整AI分析卡住

- Bug ID：BUG-20260405-04
- 来源：2026-04-05 人工补录
- 严重级别：高
- 当前状态：开放，待排查上下文长度与无效上下文注入
- 复现摘要：使用本地模型执行完整 AI 分析时流程卡住，怀疑与上下文过长或夹带垃圾上下文有关，需要结合 LLM 日志核对实际入参。
- 验收说明：定位卡住点并收敛根因；若为上下文过长，需补长度治理或垃圾上下文过滤，并验证完整分析可稳定完成。

### Bug 6: 讨论动态白色文字不可读

- Bug ID：BUG-20260405-06
- 来源：2026-04-05 人工补录
- 严重级别：中
- 当前状态：2026-04-07 source mode 已关闭；样式修复完成，Test Agent 自动化与 source-mode 浏览器验证通过，UI Designer Agent 视觉验收通过
- 复现摘要：“讨论动态”输出内容仍存在白色或浅色文字落在浅色气泡中的场景，导致文本可读性差。
- 验收说明：统一高对比度文本颜色，确保气泡内正文、强调和链接在当前主题下均可清晰阅读。

### Bug 7: 情绪轮动数据不更新/快照过旧

- Bug ID：BUG-20260405-07
- 来源：2026-04-05 人工补录
- 严重级别：高
- 当前状态：2026-04-07 source mode 已关闭；最新快照与 degraded/榜单回退口径修复完成，Test Agent、UI Designer Agent、User Representative Agent（scoped verdict）通过
- 复现摘要：“情绪轮动”右侧“分歧”长期不更新，“综合强度榜单”缺少时效信息，“快照”时间停留在 4 月 2 日。
- 验收说明：恢复右侧指标与榜单快照刷新，补清晰时效展示，并验证页面可反映最新数据时间戳。

## 历史备注

- 2026-04-08 Bug 2 已关闭：4 月 5 日日志复盘确认并非 MCP 整体失效，fresh source-mode 下 direct MCP 端点可正常返回；实际根因是 Ollama `keep_alive` 不兼容叠加本地模型下 live-gate / recommendation JSON 协议破裂。后端已补 `keep_alive` 规范化、JSON response-format 强制、live-gate bounded repair、recommendation bounded invalid-response 出口，并精简 live-gate prompt。Test Agent 在 `http://localhost:5128` 两轮复测通过：`/api/stocks/copilot/live-gate` 成功完成并执行工具，direct MCP samples 正常，`股票推荐` 不再卡在 `recommend_chart_validator`；剩余 latency / degraded 风险另行跟踪，不作为本 Bug 保持开放的理由。

- 2026-04-07 Bug 3 已关闭：`ResearchRoleExecutor` 已对 `MarketContextMcp` 单独放宽 timeout budget，其他工具保持默认预算；Test Agent 在 source mode 下复测 research-workbench 未再出现旧的 `MarketContextMcp` 重试风暴，日志已记录多次成功完成，包含 4000ms 完成样本，且未再出现 `Retrying MarketContextMcp` 模式。Round 2 暴露的是独立的 source-mode research-session 运行时不稳定问题，应另行跟踪，不作为本 Bug 保持开放的理由。

- 2026-04-07 Bug 8 已关闭：latest packaged validation 已确认 `财务数据测试` 管理页裸六码输入在 `600519`、`000858` 等样本上可成功采集，日志正确更新、worker 持续健康且无 frontend/page error；其余个别标的失败表现为显式上游耗尽或 `cninfo` 无可下载 PDF，不再视为本 Bug 未修复，后续如需扩覆盖率应单独跟踪。

- 2026-04-07 Bug 5 已关闭：后端 route/DI/symbol 修复、worker proxy timeout split、管理员日志契约修复、股票页 sparse-data 状态修复已在 fresh packaged rebuild 后完成复核；Test Agent packaged validation、UI Designer Agent 与 User Representative Agent 均通过。`财务数据测试` 裸码 `600519` -> `股票信息` 规范化 `sh600519` 回显链路已验证；对仅有稀疏 PDF 数据的样本，`财务报表` 明确显示“已通过/已获取 ... 期报表，但当前暂无可展示的结构化财务指标”视为通过。

- 2026-04-07 Bug 7 已关闭：`/api/market/sentiment/latest` fresh source-mode 复核已确认不再回退到 April-2 旧快照；上游不完整时页面进入 fresh degraded `同步不完整` 口径，`综合强度榜单` 不再回退旧 snapshot，而是明确显示榜单待补齐/暂不展示历史榜单；Test Agent、UI Designer Agent、User Representative Agent（scoped verdict）通过。`股票信息 -> 新建计划` 缺少市场上下文区块已确认是独立预存问题，不作为 Bug 7 保持开放的理由。

- 2026-03-24 归档备注：当时无开放项。Bug 6、Bug 7、Bug 8 已归档到 `.automation/buglist-resolved-20260323.md`。
	- 2026-03-22 本轮在 `sh600000` 的 `盘中消息带` 与右侧 `资讯影响` 中未再看到原记录里的错字/失真标题样例。
	- 当前样本下暂未复现，但仅覆盖了 `sh600000` 单一标的，建议后续在 `全量资讯库` 再抽样复核，不在本轮直接关闭。

## Bug 模板

### Bug X: 标题

- 严重级别：高 / 中 / 低
- 当前状态：开放 / 处理中 / 已修复待验证 / 已关闭
- 复现摘要：明确入口、操作、现象与已知根因
- 验收说明：明确完成标准、保留条件与复测口径


人工发现的bug:
- "财务报表" 功能前端报错："加载失败: Unexpected token '<', "<!doctype "... is not valid JSON"