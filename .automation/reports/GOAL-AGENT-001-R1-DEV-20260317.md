# GOAL-AGENT-001-R1 Development Report

## English

### Scope
- Implement the backend-first evidence traceability foundation for R1.
- Keep the solution additive and backward-compatible while the repo still contains unrelated in-flight edits.
- Validate the new evidence contract on the backend before moving to frontend/browser acceptance.

### Actions
- Added additive article-read cache fields to `LocalStockNews` and `LocalSectorReport`: `ArticleExcerpt`, `ArticleSummary`, `ReadMode`, `ReadStatus`, `IngestedAt`.
- Updated `AppDbContext` and `LocalFactSchemaInitializer` so the new fields are configured in EF and added through the runtime SQL initializer.
- Added `LocalFactArticleReadService` and registered `ILocalFactArticleReadService` in `StocksModule`.
- Extended local-fact DTOs and stock-agent projection DTOs with `LocalFactId`, `SourceRecordId`, `Excerpt`, `Summary`, `ReadMode`, `ReadStatus`, and `IngestedAt`.
- Updated `QueryLocalFactDatabaseTool` to prepare tracked local-fact rows through the article-read service, persist cache changes, and propagate the new traceability fields across stock / sector / market / archive paths.
- Updated `StockAgentOrchestrator` prompt contracts and repair schemas so all agents now request the richer evidence object with traceability and read-quality metadata.
- Updated `StockAgentResultNormalizer` to normalize the richer evidence shape and treat `metadata_only`, `unverified`, and `fetch_failed` as insufficient for high-confidence news conclusions.
- Updated `StockAgentCommanderConsistencyGuardrails` so commander confidence is capped when evidence quality is only metadata-level or otherwise weak.
- Updated backend regression tests for prompt builder, result normalizer, commander guardrails, local-fact projection, and query-tool behavior.
- Updated `frontend/src/modules/stocks/StockAgentPanels.vue` and its unit test so evidence now renders as clickable cards with source, published time, excerpt, read-status, read-mode, ingest time, and local trace IDs while remaining compatible with older point-only history payloads.
- During Browser MCP validation, identified a runtime concurrency bug caused by destructive local-fact refreshes deleting and recreating rows while the new article-read cache path was persisting evidence metadata.
- Reworked `LocalFactIngestionService` stock/sector/market upserts to merge in place instead of delete-and-reinsert, preserving stable row identities and cached evidence fields (`ArticleExcerpt`, `ArticleSummary`, `ReadMode`, `ReadStatus`, `IngestedAt`).
- Added a regression test to lock the merge behavior so matched local-stock-news rows keep their cached evidence metadata during refresh.
- Re-ran backend-served Browser MCP validation on `http://localhost:5119/` and confirmed evidence cards render in the stock-agent UI with clickable URLs, read-status/read-mode pills, ingest timestamps, and local trace identifiers.

### Test Commands And Results
- `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~StockAgentPromptBuilderTests|FullyQualifiedName~StockAgentResultNormalizerTests|FullyQualifiedName~StockAgentCommanderConsistencyGuardrailsTests|FullyQualifiedName~StockAgentLocalFactProjectionTests|FullyQualifiedName~QueryLocalFactDatabaseToolTests"`
  - Passed: 23/23
- `npm --prefix .\frontend run test:unit -- src/modules/stocks/StockAgentPanels.spec.js`
  - Passed
- `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "LocalFactIngestionServiceTests|QueryLocalFactDatabaseToolTests|StockAgentResultNormalizerTests|StockAgentPromptBuilderTests|StockAgentCommanderConsistencyGuardrailsTests|StockAgentLocalFactProjectionTests"`
  - Passed: 30/30
- Browser MCP acceptance on `http://localhost:5119/`
  - Confirmed backend-served frontend loads and `/api/stocks/detail`, `/api/stocks/news/impact`, `/api/news?level=stock`, `/api/news?level=sector`, and `/api/stocks/agents/single` recover to `200 OK` after the merge-fix restart.
  - Confirmed evidence cards show clickable URLs, source, publish time, excerpt, `readStatus`, `readMode`, ingest time, and `事实# / 源#` trace identifiers in the live stock-agent panel.

### Issues / Remaining Work
- R1 scope is complete for Dev1.
- Browser console and network history still contain stale pre-restart `ERR_CONNECTION_REFUSED` / `500` entries from the earlier broken server session, but the latest post-fix requests observed during validation returned `200 OK`.

## 中文

### 本次范围
- 落地 R1 的后端优先 evidence traceability 基础能力。
- 在仓库仍有其他进行中改动的前提下，保持增量、兼容式修改。
- 先把后端 evidence contract 打通并验证，再继续前端与 Browser MCP 验收。

### 已完成动作
- 为 `LocalStockNews` 与 `LocalSectorReport` 增加正文缓存相关字段：`ArticleExcerpt`、`ArticleSummary`、`ReadMode`、`ReadStatus`、`IngestedAt`。
- 更新 `AppDbContext` 与 `LocalFactSchemaInitializer`，让新字段同时进入 EF 配置和运行时 SQL 初始化流程。
- 新增 `LocalFactArticleReadService`，并在 `StocksModule` 中注册 `ILocalFactArticleReadService`。
- 扩展本地事实 DTO 与 stock-agent projection DTO，补齐 `LocalFactId`、`SourceRecordId`、`Excerpt`、`Summary`、`ReadMode`、`ReadStatus`、`IngestedAt`。
- 修改 `QueryLocalFactDatabaseTool`：对跟踪态本地事实行执行正文准备与缓存回写，并在 stock / sector / market / archive 各条路径透传新的可追溯字段。
- 修改 `StockAgentOrchestrator` 的 prompt contract 与 repair schema，让各个 Agent 都按新的 evidence object 输出 traceability / read-quality 信息。
- 修改 `StockAgentResultNormalizer`：归一化新的 evidence shape，并把 `metadata_only`、`unverified`、`fetch_failed` 视为不能支撑高置信结论的弱证据。
- 修改 `StockAgentCommanderConsistencyGuardrails`：当 commander 证据只有元数据级或其它弱证据时，自动压低置信度上限。
- 更新后端回归测试，覆盖 prompt builder、result normalizer、commander guardrails、local-fact projection、query tool。
- 修改 `frontend/src/modules/stocks/StockAgentPanels.vue` 及其单测：evidence 改为卡片式展示，补齐可点击 URL、来源、发布时间、摘录、`readStatus`、`readMode`、入库时间，以及 `事实# / 源#` 本地追踪字段，并兼容旧历史里的 point-only evidence。
- 在 Browser MCP 验收时发现新的运行时并发问题：本地事实刷新仍然采用“整批删除再重建”，会与 article-read 缓存写回发生主键失效冲突。
- 修改 `LocalFactIngestionService` 的 stock / sector / market 刷新逻辑为按业务键原位 merge，不再 delete-and-reinsert，从而保留稳定主键和已缓存的 `ArticleExcerpt`、`ArticleSummary`、`ReadMode`、`ReadStatus`、`IngestedAt`。
- 新增回归测试，锁定“命中同一条本地个股资讯时，刷新不会抹掉 cached evidence metadata”的行为。
- 重新执行 backend-served Browser MCP 验收，确认 stock-agent 实时页面能展示新的 evidence 卡片，并能看到可点击链接、阅读状态/阅读模式、入库时间与本地追踪标识。

### 测试命令与结果
- `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~StockAgentPromptBuilderTests|FullyQualifiedName~StockAgentResultNormalizerTests|FullyQualifiedName~StockAgentCommanderConsistencyGuardrailsTests|FullyQualifiedName~StockAgentLocalFactProjectionTests|FullyQualifiedName~QueryLocalFactDatabaseToolTests"`
  - 通过：23/23
- `npm --prefix .\frontend run test:unit -- src/modules/stocks/StockAgentPanels.spec.js`
  - 通过
- `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "LocalFactIngestionServiceTests|QueryLocalFactDatabaseToolTests|StockAgentResultNormalizerTests|StockAgentPromptBuilderTests|StockAgentCommanderConsistencyGuardrailsTests|StockAgentLocalFactProjectionTests"`
  - 通过：30/30
- Browser MCP 验收：`http://localhost:5119/`
  - 修复重启后，`/api/stocks/detail`、`/api/stocks/news/impact`、`/api/news?level=stock`、`/api/news?level=sector`、`/api/stocks/agents/single` 这几条关键请求恢复为 `200 OK`。
  - 实际页面中已验证 evidence 卡片展示链接、来源、发布时间、摘录、`readStatus`、`readMode`、入库时间，以及 `事实# / 源#` 追踪标识。

### 当前问题 / 剩余工作
- Dev1 负责的 R1 范围已经收口完成。
- Browser MCP 的 console / network 历史里仍保留重启前旧进程留下的 `ERR_CONNECTION_REFUSED` / `500` 记录，但修复后的最新请求链路已经恢复正常并完成实时页面验收。