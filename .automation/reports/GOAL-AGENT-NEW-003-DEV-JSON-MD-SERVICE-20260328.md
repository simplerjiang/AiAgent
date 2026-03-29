# GOAL-AGENT-NEW-003 开发报告（JSON->Markdown 统一服务，2026-03-28）

## English (for agents)
### Scope delivered
1. Added a centralized frontend formatter service at `frontend/src/utils/jsonMarkdownService.js`.
2. Unified JSON parsing + readable markdown generation + safe HTML rendering in one place.
3. Integrated service into dynamic workbench surfaces:
   - `TradingWorkbenchReport.vue`
   - `TradingWorkbenchFeed.vue`
4. Removed direct `JSON.stringify(...)` user-facing rendering from report list/tag sections.
5. Added focused tests:
   - `frontend/src/utils/jsonMarkdownService.spec.js`
   - updated `frontend/src/modules/stocks/TradingWorkbench.spec.js`

### Key behaviors
1. JSON strings/objects/arrays are transformed into readable markdown instead of raw JSON text.
2. Key label localization is supported with baseline Chinese mappings (e.g., PE ratio, volume ratio, shareholder count, float market cap).
3. Numeric formatting for confidence/probability/market cap/shareholder count is normalized.
4. Existing plain markdown remains unchanged.

### Validation commands and results
1. Command:
   `npm --prefix .\\frontend run test:unit -- src/utils/jsonMarkdownService.spec.js src/modules/stocks/TradingWorkbench.spec.js`
2. Result:
   - Test Files: 2 passed
   - Tests: 31 passed

### Notes
1. This is an infrastructure-first slice for GOAL-AGENT-NEW-003.
2. Remaining feature tasks (MCP collapse detail panel, company overview completeness exposure, follow-up routing orchestration) are not part of this single slice yet.

---

## 中文（给用户）
### 本次已完成
1. 新增统一服务：`frontend/src/utils/jsonMarkdownService.js`。
2. 把 JSON 解析、字段中文化、Markdown 生成、安全 HTML 渲染统一收口。
3. 已接入动态工作台核心显示面：
   - `TradingWorkbenchReport.vue`
   - `TradingWorkbenchFeed.vue`
4. 报告中的关键要点/风险限制/失效条件/证据标签不再 `JSON.stringify` 直出。
5. 新增并更新单测，保证后续可持续迭代。

### 用户可见改进
1. 遇到对象/数组内容时，界面显示为可读文本或 Markdown，而不是原始 JSON。
2. 基本面的常见字段（市盈率、量比、股东户数、流通市值）会按中文字段名和可读格式展示。
3. 现有已是 Markdown 的内容不会被破坏。

### 验证命令
1. `npm --prefix .\\frontend run test:unit -- src/utils/jsonMarkdownService.spec.js src/modules/stocks/TradingWorkbench.spec.js`
2. 结果：`2` 个测试文件通过，`31` 条用例通过。

### 说明
这一步是“统一基础能力”先行，便于你后面提到的 MCP 展开面板、研究报告全板块去 JSON 化、社交情绪可读化等需求直接复用。
