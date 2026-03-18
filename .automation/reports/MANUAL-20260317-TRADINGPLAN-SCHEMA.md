# MANUAL Execution Report (20260317-TRADINGPLAN-SCHEMA)

## EN - Planning
- Objective:
  - Investigate why trading plans were not loading.
  - Compare trading-plan code paths with the live SQL Server schema.
  - Patch missing database columns and indexes with `sqlcmd` and make the startup initializer consistent.
- Scope:
  - `backend/SimplerJiangAiAgent.Api/Infrastructure/Jobs/TradingPlanSchemaInitializer.cs`
  - SQL Server database `SimplerJiangAiAgent`

## ZH - 规划阶段
- 目标：
  - 排查交易计划为什么没有加载出来。
  - 对照代码与当前 SQL Server 实库结构，找出交易计划相关缺失项。
  - 使用 `sqlcmd` 补齐缺失字段和索引，并同步修正启动时的 schema initializer。
- 范围：
  - `backend/SimplerJiangAiAgent.Api/Infrastructure/Jobs/TradingPlanSchemaInitializer.cs`
  - SQL Server 数据库 `SimplerJiangAiAgent`

## EN - Development
- Verified `/api/stocks/plans` reads only from `TradingPlans` and filters to renderable rows (`AnalysisHistoryId > 0`, `Symbol`/`Name` required).
- Found schema drift in live table `dbo.TradingPlans`:
  - missing `PlanKey`
  - missing `Title`
  - missing unique index `IX_TradingPlans_PlanKey`
- Updated `TradingPlanSchemaInitializer` so new environments add the missing columns and unique index automatically.
- Patched the live SQL Server database with `sqlcmd` to add the missing columns/defaults/index immediately.
- Confirmed supporting data exists in `StockAgentAnalysisHistories` but `TradingPlans` currently contains zero rows, so the endpoint correctly returns an empty list after schema repair.

## ZH - 开发记录
- 已确认 `/api/stocks/plans` 只查询 `TradingPlans`，并且只返回可渲染记录（要求 `AnalysisHistoryId > 0` 且 `Symbol/Name` 有效）。
- 在真实库 `dbo.TradingPlans` 中发现 schema 漂移：
  - 缺少 `PlanKey`
  - 缺少 `Title`
  - 缺少唯一索引 `IX_TradingPlans_PlanKey`
- 已更新 `TradingPlanSchemaInitializer`，保证新环境启动时会自动补齐这些结构。
- 已使用 `sqlcmd` 立即修补当前 SQL Server 实库，补上缺失字段、默认约束和唯一索引。
- 已确认 `StockAgentAnalysisHistories` 中有数据，但 `TradingPlans` 当前确实为 0 行，因此修复后接口返回空数组属于正确结果，不是新的报错。

## EN - Test Commands & Results
1) SQL schema/data verification
- Command:
  - `sqlcmd -C -S localhost -U sa -P 123456 -d SimplerJiangAiAgent -h -1 -W -Q "SET NOCOUNT ON; ..."`
- Result:
  - PASS
  - confirmed `TradingPlans` now contains `PlanKey`, `Title`, and `IX_TradingPlans_PlanKey`
  - confirmed `TradingPlans` row count = `0`
  - confirmed `StockAgentAnalysisHistories` row count = `129`

2) API verification
- Command:
  - `Invoke-WebRequest http://localhost:5119/api/stocks/plans`
- Result:
  - PASS, response `[]`

3) Draft flow verification
- Command:
  - `Invoke-WebRequest http://localhost:5119/api/stocks/plans/draft -Method Post -ContentType 'application/json' -Body '{"symbol":"sh603259","analysisHistoryId":129}'`
- Result:
  - PASS, draft payload returned successfully

4) Backend build verification
- Command:
  - `dotnet build backend/SimplerJiangAiAgent.Api/SimplerJiangAiAgent.Api.csproj`
- Result:
  - PASS after stopping the running API process that was locking `SimplerJiangAiAgent.Api.exe`

## ZH - 测试命令与结果
1) SQL 结构/数据校验
- 命令：
  - `sqlcmd -C -S localhost -U sa -P 123456 -d SimplerJiangAiAgent -h -1 -W -Q "SET NOCOUNT ON; ..."`
- 结果：
  - 通过
  - 已确认 `TradingPlans` 现在包含 `PlanKey`、`Title` 和 `IX_TradingPlans_PlanKey`
  - 已确认 `TradingPlans` 当前记录数为 `0`
  - 已确认 `StockAgentAnalysisHistories` 当前记录数为 `129`

2) API 校验
- 命令：
  - `Invoke-WebRequest http://localhost:5119/api/stocks/plans`
- 结果：
  - 通过，返回 `[]`

3) 草稿链路校验
- 命令：
  - `Invoke-WebRequest http://localhost:5119/api/stocks/plans/draft -Method Post -ContentType 'application/json' -Body '{"symbol":"sh603259","analysisHistoryId":129}'`
- 结果：
  - 通过，成功返回交易计划草稿

4) 后端编译校验
- 命令：
  - `dotnet build backend/SimplerJiangAiAgent.Api/SimplerJiangAiAgent.Api.csproj`
- 结果：
  - 在先停止占用 `SimplerJiangAiAgent.Api.exe` 的运行中 API 进程后编译通过