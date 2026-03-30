# GOAL-AGENT-NEW-001-R8 P0: MCP Reliability Bug Fixes

**Date**: 2026-03-30  
**Scope**: A1 (KlineMcp/MinuteMcp timeout optimization) + A2 (Fundamentals summary parse fix)  
**Status**: P0 COMPLETE — 361/361 backend tests passing

---

## English Summary

### A1: MCP Timeout Optimization (3 changes)

**Root cause analysis**: `FetchSymbolDataBundleAsync` was already parallel via `Task.WhenAll`. The actual bottlenecks were:
1. `LocalFactIngestionService.EnsureFreshAsync` called multiple times for the same symbol within a single MCP request pipeline (from `EnsureSymbolFactsRefreshedAsync` AND from `StockDataService.GetIntradayMessagesAsync`)
2. `EnsureMarketFreshAsync` called on every `EnsureFreshAsync` with no skip window
3. `ResolveMcpMarketContextAsync` redundantly re-fetched the quote just for `sectorNameHint` even though the quote was already available from `FetchSymbolDataBundleAsync`

**Changes made**:

| File | Change | Impact |
|------|--------|--------|
| `LocalFactIngestionService.cs` | Added 2-min `SymbolCrawlTimestamps` skip window for `SyncSymbolAsync` crawl, always preserving `ProcessSymbolPendingAsync` | Eliminates redundant network crawls within MCP pipeline |
| `LocalFactIngestionService.cs` | Added 2-min `_lastMarketCrawlTicks` skip window (via `Interlocked`) for `FetchMarketReportsAsync`, always preserving `ProcessMarketPendingAsync` | Eliminates redundant market report crawls |
| `LocalFactIngestionService.cs` | Market skip check moved inside `MarketRefreshGate` semaphore to prevent TOCTOU race (caught in code review) | Thread-safe concurrent access |
| `StockCopilotMcpService.cs` | Added `ResolveMcpMarketContextAsync(symbol, sectorNameHint, ct)` overload | Eliminates redundant quote network call |
| `StockCopilotMcpService.cs` | Updated `GetKlineAsync`, `GetMinuteAsync`, `GetStrategyAsync` to use `bundle.Quote.SectorName` as hint | 3 MCP tools optimized |

**Expected performance improvement**: For `market_analyst` role calling 4 MCP tools sequentially:
- Before: Each tool did `EnsureRefresh(symbol+market network crawl) + FetchBundle + Prepare + MarketContext(redundant quote fetch)`
- After: First tool crawls, remaining 3 skip crawls (2-min window). All skip redundant quote fetch in market context resolution.
- Estimated improvement: ~3-8s saved per tool call, ~12-24s total for `market_analyst`

### A2: Fundamentals Summary Parse Fix (1 change)

**Bug**: `ParseAnalystBlock` crashed with `InvalidOperationException` when the fundamentals analyst returned `valuationView` without `qualityView`. The `qv` out-variable was a default `JsonElement` and `.GetString()` threw.

**Fix**: Changed `qv.GetString()` to `null` in the fallback path. When only `valuationView` exists, quality is correctly set to null and the summary synthesizes from valuationView alone.

### Tests Added

| Test | Validates |
|------|-----------|
| `EnsureFreshAsync_SecondCallWithinSkipWindow_ShouldSkipCrawl` | Symbol crawl skip + AI enrichment still runs |
| `EnsureMarketFreshAsync_SecondCallWithinSkipWindow_ShouldSkipCrawl` | Market crawl skip + AI enrichment still runs |
| `GenerateBlocks_FundamentalsWithOnlyValuationView_ShouldNotCrash` | No crash, summary contains "估值判断", evidence/counter mapped |
| `GenerateBlocks_FundamentalsWithBothViews_SynthesizesSummary` | Both quality and valuation synthesized |

### Verification

```
dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj
# 通过! - 失败: 0, 通过: 361, 已跳过: 0, 总计: 361
```

---

## 中文摘要

### A1: MCP 超时优化（3 项改动）

**根因分析**：`FetchSymbolDataBundleAsync` 已经通过 `Task.WhenAll` 并行化，不是瓶颈。实际瓶颈是：
1. `EnsureFreshAsync` 在同一次 MCP 请求中被多次调用（来自 `EnsureSymbolFactsRefreshedAsync` 和 `GetIntradayMessagesAsync`），每次都触发网络爬取
2. `EnsureMarketFreshAsync` 在每次 `EnsureFreshAsync` 中被调用且无跳过窗口
3. `ResolveMcpMarketContextAsync` 冗余地重新获取行情报价仅为了取得 `sectorNameHint`

**改动**：
- `LocalFactIngestionService` 增加 2 分钟爬取跳过窗口（符号级 + 市场级），始终保留 AI 富化处理
- 市场跳过检查移入信号量内部，消除 TOCTOU 竞态（代码审查中发现）
- `StockCopilotMcpService` 新增 `ResolveMcpMarketContextAsync` 重载，接受已知的行业名称提示，跳过冗余报价获取
- `GetKlineAsync`、`GetMinuteAsync`、`GetStrategyAsync` 使用 bundle 中已有的 `Quote.SectorName`

**预期性能提升**：`market_analyst` 角色调用 4 个 MCP 工具时，预计节省 12-24 秒。

### A2: 基本面摘要解析修复

**Bug**：当基本面分析师仅返回 `valuationView` 而无 `qualityView` 时，`ParseAnalystBlock` 因访问默认 `JsonElement` 而崩溃。

**修复**：将 fallback 路径中的 `qv.GetString()` 改为 `null`。

### 测试验证

全量后端测试 361/361 通过，新增 4 个测试覆盖跳过窗口和基本面解析。

### 后续工作（R8 P1+）

| 优先级 | 任务 | 状态 |
|--------|------|------|
| P1 | B1: 多源 fallback 机制 (新浪/腾讯备用源) | 未开始 |
| P1 | C2: 研究管线并发控制 (SemaphoreSlim + RateLimiter) | 未开始 |
| P2 | B2: Fundamental 快照多源冗余 | 未开始 |
| P2 | C1: LLM 响应韧性增强 | 未开始 |
| P2 | C3: 报告快照聚合 | 未开始 |
| P3 | B3: SocialSentiment 真实数据源 | 未开始 |
