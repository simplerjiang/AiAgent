# GOAL-AGENT-002-P0 Bug 8 Follow-up Dev Report (2026-03-23)

## EN

### Scope

- Continue the GOAL-AGENT-002-P0 runtime-stability line for Bug 8 only.
- Target the packaged stock-terminal symptom where a fresh stock query can be followed by transient `ERR_CONNECTION_REFUSED` and temporary loss of `http://localhost:5119`.

### Actions

- Added `HttpTimeoutSeconds` to `backend/SimplerJiangAiAgent.Api/Modules/Stocks/Models/StockCrawlerOptions.cs`.
- Wired the configured timeout into stock-related HTTP clients in `backend/SimplerJiangAiAgent.Api/Modules/Stocks/StocksModule.cs` with a safe clamp.
- Added focused regression coverage in `backend/SimplerJiangAiAgent.Api.Tests/StocksModuleHttpClientTests.cs`.
- Updated stock crawler defaults in `backend/SimplerJiangAiAgent.Api/appsettings.json` and `backend/SimplerJiangAiAgent.Api/appsettings.Development.json`.
- Hardened `desktop/SimplerJiangAiAgent.Desktop/Form1.cs` so the desktop host now:
  - captures managed backend stdout/stderr to `%LOCALAPPDATA%\\SimplerJiangAiAgent\\logs`,
  - polls backend health,
  - restarts the owned backend on repeated health failure or process exit,
  - reloads the frontend after recovery,
  - triggers recovery on navigation failure,
  - uses a tighter 2-second probe interval with a 2-failure threshold.
- Fixed a self-introduced DI regression by restoring `BaiduStockCrawler` to non-typed registration after desktop startup logs exposed the constructor mismatch.

### Test Commands And Results

- Command: `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~StocksModuleHttpClientTests"`
- Result: passed, 2/2.

- Command: `dotnet build .\desktop\SimplerJiangAiAgent.Desktop\SimplerJiangAiAgent.Desktop.csproj`
- Result: passed.

- Command: `.\start-all.bat`
- Result: passed. Frontend build, backend publish, desktop publish, packaged EXE launch, and packaged backend health wait all succeeded.

- Command: `Invoke-RestMethod http://localhost:5119/api/health | ConvertTo-Json -Compress`
- Result: returned `{"status":"ok"}` before Browser MCP validation.

- Browser MCP: navigate to `http://localhost:5119/?tab=stock-info&ts=1742790600`, wait for `股票信息`
- Result: passed.

### Runtime Observations

- Fresh packaged validation still does not justify closure.
- The stock page loaded successfully and the backend health probe returned `{"status":"ok"}`.
- However, fresh Browser MCP network capture still recorded new failures for:
  - `/api/stocks/plans?take=20`
  - `/api/stocks/plans/alerts?take=20`
- Failure shape: `net::ERR_CONNECTION_REFUSED`.
- After those failures, the health endpoint returned `{"status":"ok"}` again, which means the current behavior is closer to a transient backend availability drop with recovery, not a permanently healthy runtime.
- The new desktop-managed backend logs under `%LOCALAPPDATA%\\SimplerJiangAiAgent\\logs` now provide recovery evidence and restart windows for follow-up diagnosis, including:
  - `desktop-backend-20260323-164113.stdout.log`
  - `desktop-backend-20260323-164621.stdout.log`

### Issues

- Bug 8 is improved but not closed.
- The current fix set adds observability, bounded crawler timeouts, and desktop-host self-healing, but the packaged runtime still shows transient connection-refused windows during fresh stock-terminal activity.
- The latest desktop stdout logs show startup success and request traffic, but they do not yet fully explain the momentary availability gap seen by Browser MCP.

## ZH

### 范围

- 继续处理 GOAL-AGENT-002-P0 运行稳定性线上的 Bug 8。
- 聚焦打包版股票终端中“fresh 查股后出现瞬时 `ERR_CONNECTION_REFUSED`、`http://localhost:5119` 短暂不可用”的问题。

### 本轮动作

- 在 `backend/SimplerJiangAiAgent.Api/Modules/Stocks/Models/StockCrawlerOptions.cs` 新增 `HttpTimeoutSeconds`。
- 在 `backend/SimplerJiangAiAgent.Api/Modules/Stocks/StocksModule.cs` 把超时真正落到股票相关 HTTP client，并加入安全夹取。
- 新增定向回归测试 `backend/SimplerJiangAiAgent.Api.Tests/StocksModuleHttpClientTests.cs`。
- 更新 `backend/SimplerJiangAiAgent.Api/appsettings.json` 与 `backend/SimplerJiangAiAgent.Api/appsettings.Development.json` 的股票爬虫默认超时配置。
- 加固 `desktop/SimplerJiangAiAgent.Desktop/Form1.cs`，让桌面宿主现在具备：
  - 将托管后端 stdout/stderr 写入 `%LOCALAPPDATA%\\SimplerJiangAiAgent\\logs`，
  - 定时探测后端健康，
  - 健康连续失败或进程退出后自动拉起后端，
  - 恢复后自动刷新前端，
  - 导航失败时主动触发恢复，
  - 2 秒探测间隔、连续 2 次失败即恢复的更紧恢复窗口。
- 在桌面启动日志暴露 `BaiduStockCrawler` 构造器不匹配后，已修复自引入的 DI 回归，把它恢复为非 typed client 注册。

### 测试命令与结果

- 命令：`dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~StocksModuleHttpClientTests"`
- 结果：通过，2/2。

- 命令：`dotnet build .\desktop\SimplerJiangAiAgent.Desktop\SimplerJiangAiAgent.Desktop.csproj`
- 结果：通过。

- 命令：`.\start-all.bat`
- 结果：通过；frontend build、backend publish、desktop publish、打包 EXE 启动、打包后端健康等待全部成功。

- 命令：`Invoke-RestMethod http://localhost:5119/api/health | ConvertTo-Json -Compress`
- 结果：在 Browser MCP 验证前返回 `{"status":"ok"}`。

- Browser MCP：导航到 `http://localhost:5119/?tab=stock-info&ts=1742790600` 并等待 `股票信息`
- 结果：通过。

### 运行态观察

- fresh 打包验证仍不足以关单。
- 股票页能够正常打开，健康探针也返回了 `{"status":"ok"}`。
- 但 fresh Browser MCP 的 network 仍抓到了新的失败请求：
  - `/api/stocks/plans?take=20`
  - `/api/stocks/plans/alerts?take=20`
- 失败形态：`net::ERR_CONNECTION_REFUSED`。
- 在这些失败之后，健康接口又恢复为 `{"status":"ok"}`，说明当前更像“后端出现瞬时不可用后自动恢复”，而不是已经完全稳定。
- 现在 `%LOCALAPPDATA%\\SimplerJiangAiAgent\\logs` 下已经有新的桌面托管后端日志可用于后续追踪恢复窗口，例如：
  - `desktop-backend-20260323-164113.stdout.log`
  - `desktop-backend-20260323-164621.stdout.log`

### 当前问题

- Bug 8 有缓解，但不能关闭。
- 本轮改动已经补齐观测能力、股票抓取超时边界和桌面宿主自愈，但打包运行态在 fresh 股票终端操作下仍会出现瞬时连接拒绝窗口。
- 最新桌面 stdout 日志说明启动和请求流量总体正常，但还不足以直接解释 Browser MCP 看到的瞬时不可用缺口。