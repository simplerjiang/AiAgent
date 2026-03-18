# Current-Day Daily K-Line Fix Report / 当日日K线修复报告

## Plan (EN)
- Objective: make the daily K-line include the current trading day even when the upstream daily endpoint lags behind intraday data.
- Root cause: the Tencent daily K-line endpoint was returning data only through `2026-03-17`, while minute-line data already contained `2026-03-18`, so the stock detail response and cached detail could miss the current trading day bar.
- Approach: synthesize or refresh the latest daily bar from the newest minute-line series and apply that merge consistently in live detail, cache detail, and persistence paths.

## 计划（ZH）
- 目标：即使上游日线接口盘中滞后，也要让日 K 线显示当前交易日。
- 根因：腾讯日线接口当时只返回到 `2026-03-17`，但分时数据已经包含 `2026-03-18`，导致详情接口和缓存详情都可能缺少当天日线。
- 做法：用最新分时序列合成或刷新最新一根日线，并在实时详情、缓存详情、持久化链路统一应用。

## Development (EN)
- Added `StockRealtimeKLineMerge` to merge the latest daily bar from minute data.
- Wired the merge into `StocksModule` for `/api/stocks/detail`, `/api/stocks/detail/cache`, `/api/stocks/signals`, and `/api/stocks/position-guidance`.
- Wired the same merge into `StockSyncService.SyncOnceAsync` and `StockSyncService.SaveDetailAsync` so cached K-line rows can also contain the current trading day.
- Added regression tests for both the merge helper and the realtime-payload persistence path.

## 开发结果（ZH）
- 新增 `StockRealtimeKLineMerge`，负责用最新分时补齐或刷新最新一根日线。
- 在 `StocksModule` 中把该逻辑接入 `/api/stocks/detail`、`/api/stocks/detail/cache`、`/api/stocks/signals`、`/api/stocks/position-guidance`。
- 在 `StockSyncService.SyncOnceAsync` 和 `StockSyncService.SaveDetailAsync` 中接入相同逻辑，使数据库缓存的 K 线也能带上当前交易日。
- 新增回归测试，覆盖合并器行为与“只有分时没有日线时的持久化”场景。

## Validation (EN)
- Upstream check command: `Invoke-WebRequest https://web.ifzq.gtimg.cn/appstock/app/fqkline/get?...`
- Result: upstream daily K-line still ended at `2026-03-17`, confirming source lag.
- Test command: `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "StockRealtimeKLineMergeTests|StockSyncServiceTests|StockDetailCacheQueriesTests|StockSignalAndGuidanceTests"`
- Result: passed, `9/9` tests.
- Live endpoint validation command: `Invoke-RestMethod http://localhost:5119/api/stocks/detail?symbol=sh600000&interval=day&count=5&persist=false`
- Result: `LastKLineDate=2026-03-18T00:00:00`, `LastMinuteDate=2026-03-18`, so the returned daily K-line now includes the current trading day.

## 验证（ZH）
- 上游检查命令：`Invoke-WebRequest https://web.ifzq.gtimg.cn/appstock/app/fqkline/get?...`
- 结果：上游日线仍只到 `2026-03-17`，确认问题来自源数据盘中滞后。
- 测试命令：`dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "StockRealtimeKLineMergeTests|StockSyncServiceTests|StockDetailCacheQueriesTests|StockSignalAndGuidanceTests"`
- 结果：通过，`9/9`。
- 本地接口验证命令：`Invoke-RestMethod http://localhost:5119/api/stocks/detail?symbol=sh600000&interval=day&count=5&persist=false`
- 结果：`LastKLineDate=2026-03-18T00:00:00`、`LastMinuteDate=2026-03-18`，说明返回的日 K 线已经补上当前交易日。