# Dual-Source Market Data Strategy Report / 双源行情策略报告

## Plan (EN)
- Objective: formally encapsulate Tencent and Eastmoney stock endpoints in the backend with a default dual-source policy.
- Strategy: use Tencent first for minute-line speed, Eastmoney first for K-line completeness, and automatically fall back when the preferred source is empty or throws.
- Scope: implement Eastmoney K-line retrieval, move routing policy into `StockDataService`, preserve explicit `source` override behavior, add regression tests, and document the new default behavior.

## 计划（ZH）
- 目标：把腾讯和东方财富两套行情接口正式收口到后端默认策略里。
- 策略：分时优先腾讯以压低时延，K 线优先东方财富以提高完整度；当优先源为空或异常时自动回退。
- 范围：补齐东方财富 K 线实现，把默认路由策略放入 `StockDataService`，保留显式 `source` 强制指定行为，并补充回归测试与文档说明。

## Development (EN)
- Implemented Eastmoney K-line retrieval in `EastmoneyStockCrawler`, covering day/week/month directly and year via monthly aggregation.
- Added ordered source routing in `StockDataService`:
  - minute: Tencent -> Eastmoney -> remaining sources -> default crawler
  - kline: Eastmoney -> Tencent -> remaining sources -> default crawler
- Kept explicit `source` requests deterministic; when callers pass a source name, only that source is used.
- Added regression tests to lock preferred-source behavior, fallback behavior, and explicit-source override behavior.
- Updated `README.md` and `.automation/tasks.json` to record the new backend default strategy.

## 开发结果（ZH）
- 在 `EastmoneyStockCrawler` 中补齐了东方财富 K 线抓取：日/周/月直接取，年线通过月线聚合得到。
- 在 `StockDataService` 中新增默认来源顺序：
  - 分时：腾讯 -> 东方财富 -> 其他来源 -> 默认聚合
  - K线：东方财富 -> 腾讯 -> 其他来源 -> 默认聚合
- 保留显式 `source` 的确定性；调用方传了来源名时，只使用该来源，不做隐式切换。
- 新增回归测试，锁定优先源命中、自动回退和显式来源覆盖行为。
- 已同步更新 `README.md` 与 `.automation/tasks.json`，记录新的后端默认策略。

## Validation (EN)
- Test command: `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "StockDataServiceSourceRoutingTests|StockRealtimeKLineMergeTests|StockSyncServiceTests|StockDetailCacheQueriesTests"`
- Result: passed, `12/12`.
- Live verification command: `Invoke-RestMethod http://localhost:5119/api/stocks/kline?symbol=sh600000&interval=day&count=5` and `Invoke-RestMethod http://localhost:5119/api/stocks/minute?symbol=sh600000`
- Result: default K-line returned `LastDate=2026-03-18T00:00:00`, while default minute returned `242` rows ending at `2026-03-18 15:00:00`, matching the expected Eastmoney-first K-line and Tencent-first minute behavior.

## 验证（ZH）
- 测试命令：`dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "StockDataServiceSourceRoutingTests|StockRealtimeKLineMergeTests|StockSyncServiceTests|StockDetailCacheQueriesTests"`
- 结果：通过，`12/12`。
- 运行时验证命令：`Invoke-RestMethod http://localhost:5119/api/stocks/kline?symbol=sh600000&interval=day&count=5` 与 `Invoke-RestMethod http://localhost:5119/api/stocks/minute?symbol=sh600000`
- 结果：默认 K 线返回的最后日期为 `2026-03-18T00:00:00`，默认分时返回 `242` 条并结束于 `2026-03-18 15:00:00`，与“日 K 东财优先、分时腾讯优先”的默认策略一致。