# MANUAL Execution Report (20260215-DB-SCHEMA)

## EN - Planning
- Objective:
  - Fix EF Core decimal precision warnings (silent truncation risk).
  - Reduce startup DB connection failures in development environment.
  - Add a mandatory testing rule for database schema completeness checks using SQLCMD workflow.
- Scope:
  - `backend/SimplerJiangAiAgent.Api/Data/AppDbContext.cs`
  - `backend/SimplerJiangAiAgent.Api/appsettings.Development.json`
  - `backend/SimplerJiangAiAgent.Api.Tests/AppDbContextDecimalPrecisionTests.cs`
  - `.github/copilot-instructions.md`

## ZH - 规划阶段
- 目标：
  - 修复 EF Core `decimal` 精度告警（避免静默截断风险）。
  - 降低开发环境启动时数据库连接失败概率。
  - 新增数据库结构完整性检查规则（SQLCMD 流程）。
- 范围：
  - `backend/SimplerJiangAiAgent.Api/Data/AppDbContext.cs`
  - `backend/SimplerJiangAiAgent.Api/appsettings.Development.json`
  - `backend/SimplerJiangAiAgent.Api.Tests/AppDbContextDecimalPrecisionTests.cs`
  - `.github/copilot-instructions.md`

## EN - Development
- Added global decimal precision/scale enforcement in `AppDbContext.OnModelCreating`:
  - all `decimal`/`decimal?` mapped to precision `18`, scale `2`.
- Added regression test `AppDbContextDecimalPrecisionTests`:
  - verifies every decimal property in model has `(18,2)`.
- Updated development DB defaults in `appsettings.Development.json`:
  - set SQL Server Express connection string for local dev startup.
- Added new continuous rule (EN+ZH) in `.github/copilot-instructions.md`:
  - testing must verify DB schema completeness for touched features and fix mismatches before completion (SQLCMD workflow).

## ZH - 开发记录
- 在 `AppDbContext.OnModelCreating` 中增加全局 `decimal` 精度设置：
  - 所有 `decimal/decimal?` 统一为 `(18,2)`。
- 新增回归单测 `AppDbContextDecimalPrecisionTests`：
  - 校验模型中每个 decimal 字段都包含 `(18,2)` 元数据。
- 更新 `appsettings.Development.json` 开发默认连接：
  - 使用本地 SQL Server Express 连接串，减少启动连接失败。
- 在 `.github/copilot-instructions.md` 新增连续规则（中英双语）：
  - 测试阶段必须做数据库结构完整性校验，若不匹配需先修复再结束任务（SQLCMD 流程）。

## EN - Test Commands & Results
1) Backend unit tests
- Command: `dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj`
- Result: PASS (`Total: 31, Failed: 0, Passed: 31, Skipped: 0`).

2) DB schema check (SQL command workflow)
- Intended SQLCMD tool check:
  - `sqlcmd -?` -> not available in current shell environment.
- Executed equivalent SQL query via ADO.NET (same SQL text) against SQL Server:
  - verified touched decimal columns in `StockQuoteSnapshots/MarketIndexSnapshots/KLinePoints/MinuteLinePoints/StockQueryHistories` are all `decimal(18,2)`.
- Result: PASS (schema for touched decimal columns is complete and consistent).

## ZH - 测试命令与结果
1) 后端单元测试
- 命令：`dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj`
- 结果：通过（`总计 31，失败 0，通过 31，跳过 0`）。

2) 数据库结构检查（SQL命令流）
- 预期 SQLCMD 工具检查：
  - `sqlcmd -?` -> 当前 shell 环境不可用。
- 已执行等价 ADO.NET SQL 查询（同一 SQL 语句）进行校验：
  - 已确认 `StockQuoteSnapshots/MarketIndexSnapshots/KLinePoints/MinuteLinePoints/StockQueryHistories` 相关 decimal 字段均为 `decimal(18,2)`。
- 结果：通过（本次涉及字段结构完整且一致）。
