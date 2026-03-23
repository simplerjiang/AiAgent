# MANUAL-20260323 v0.0.3 RELEASE DEV

## EN

### Scope

- Prepare the repository state for release `v0.0.3`.
- Validate the current uncommitted frontend, backend, desktop, packaging, and release-metadata changes before pushing.

### Actions

- Bumped the live release metadata to `0.0.3` in:
  - `Directory.Build.props`
  - `frontend/package.json`
  - `frontend/package-lock.json`
  - `scripts/build-windows-installer.ps1`
  - `scripts/windows-installer.iss`
  - `README.md`
- Kept the already prepared product/dev reports for the Copilot R2/R3 and Bug 8 follow-up work in the repo.
- Revalidated the currently modified frontend Stock Copilot and Source Governance UI areas.
- Revalidated the currently modified backend Source Governance sanitizer and stock HTTP timeout registration areas.
- Rebuilt the Windows package with `scripts/publish-windows-package.ps1`.
- Launched the packaged desktop EXE from `artifacts/windows-package/SimplerJiangAiAgent.Desktop.exe` and verified bundled backend and UI startup.
- Built the Windows installer and portable zip for `0.0.3`.

### Test Commands And Results

- Command: `npm --prefix .\frontend run test:unit -- src/modules/stocks/StockInfoTab.spec.js src/modules/stocks/StockRecommendTab.spec.js src/modules/admin/SourceGovernanceDeveloperMode.spec.js`
- Result: passed, 70/70.

- Command: `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~SourceGovernanceReadServiceTests|FullyQualifiedName~StocksModuleHttpClientTests"`
- Result: passed, 15/15.

- Command: `& .\scripts\publish-windows-package.ps1`
- Result: passed after clearing old packaged process locks; rebuilt `artifacts/windows-package` successfully.

- Packaged EXE launch validation:
  - Launch command: `Start-Process .\artifacts\windows-package\SimplerJiangAiAgent.Desktop.exe -PassThru`
  - Health check: `Invoke-RestMethod http://localhost:5119/api/health`
  - Version check: `Invoke-RestMethod http://localhost:5119/api/app/version`
  - UI check: browser-rendered fetch on `http://localhost:5119/?tab=stock-info`
- Result: passed. Health returned `{"status":"ok"}`, app version returned `0.0.3`, and the rendered page showed the `v0.0.3` badge plus the `股票信息` terminal surface.

- Command: `& .\scripts\build-windows-installer.ps1 -SkipPackagePublish -AppVersion 0.0.3 -SelfContained:$true`
- Result: passed; generated `artifacts/installer/SimplerJiangAiAgent-Setup-0.0.3.exe`.

- Command: `Compress-Archive -Path .\artifacts\windows-package\* -DestinationPath .\artifacts\SimplerJiangAiAgent-portable-0.0.3.zip -Force`
- Result: passed; generated `artifacts/SimplerJiangAiAgent-portable-0.0.3.zip`.

### Artifacts

- Installer: `artifacts/installer/SimplerJiangAiAgent-Setup-0.0.3.exe`
- Portable zip: `artifacts/SimplerJiangAiAgent-portable-0.0.3.zip`
- Packaged EXE: `artifacts/windows-package/SimplerJiangAiAgent.Desktop.exe`

### Issues

- Initial package rebuild failed because old packaged desktop/backend processes were still locking files under `artifacts/windows-package`; resolved by stopping the stale packaged processes and rerunning the script.
- Initial portable-zip attempt failed because a residual packaged backend process still held package files open; resolved by stopping the remaining packaged API process and rerunning the archive command.
- Frontend build still emits a Vite chunk-size warning for the main bundle, but the build completes successfully and this is not a release blocker for `v0.0.3`.

## ZH

### 范围

- 为 `v0.0.3` 做发版准备。
- 在推送前，把当前未提交的前端、后端、桌面端、打包链路和版本元数据改动按仓库规则完整验收一遍。

### 本轮动作

- 已将在线生效的版本元数据统一升级到 `0.0.3`，覆盖：
  - `Directory.Build.props`
  - `frontend/package.json`
  - `frontend/package-lock.json`
  - `scripts/build-windows-installer.ps1`
  - `scripts/windows-installer.iss`
  - `README.md`
- 保留并纳入当前仓库中的 Copilot R2/R3 与 Bug 8 follow-up 开发报告。
- 对当前改动过的 Stock Copilot 与治理页前端区域重新跑了定向单测。
- 对当前改动过的 Source Governance 脱敏链路和股票 HTTP 超时注册链路重新跑了后端定向单测。
- 使用 `scripts/publish-windows-package.ps1` 重新构建了 Windows 打包目录。
- 实际启动了 `artifacts/windows-package/SimplerJiangAiAgent.Desktop.exe`，确认打包后的后端和前端页面都能起来。
- 生成了 `0.0.3` 的安装包和便携包。

### 测试命令与结果

- 命令：`npm --prefix .\frontend run test:unit -- src/modules/stocks/StockInfoTab.spec.js src/modules/stocks/StockRecommendTab.spec.js src/modules/admin/SourceGovernanceDeveloperMode.spec.js`
- 结果：通过，70/70。

- 命令：`dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~SourceGovernanceReadServiceTests|FullyQualifiedName~StocksModuleHttpClientTests"`
- 结果：通过，15/15。

- 命令：`& .\scripts\publish-windows-package.ps1`
- 结果：在清理旧的打包进程占用后通过，成功重建 `artifacts/windows-package`。

- 打包 EXE 启动验收：
  - 启动命令：`Start-Process .\artifacts\windows-package\SimplerJiangAiAgent.Desktop.exe -PassThru`
  - 健康检查：`Invoke-RestMethod http://localhost:5119/api/health`
  - 版本检查：`Invoke-RestMethod http://localhost:5119/api/app/version`
  - UI 检查：浏览器渲染抓取 `http://localhost:5119/?tab=stock-info`
- 结果：通过。健康接口返回 `{"status":"ok"}`，版本接口返回 `0.0.3`，页面抓取到 `v0.0.3` 徽标和 `股票信息` 终端页面。

- 命令：`& .\scripts\build-windows-installer.ps1 -SkipPackagePublish -AppVersion 0.0.3 -SelfContained:$true`
- 结果：通过；成功生成 `artifacts/installer/SimplerJiangAiAgent-Setup-0.0.3.exe`。

- 命令：`Compress-Archive -Path .\artifacts\windows-package\* -DestinationPath .\artifacts\SimplerJiangAiAgent-portable-0.0.3.zip -Force`
- 结果：通过；成功生成 `artifacts/SimplerJiangAiAgent-portable-0.0.3.zip`。

### 产物

- 安装包：`artifacts/installer/SimplerJiangAiAgent-Setup-0.0.3.exe`
- 便携包：`artifacts/SimplerJiangAiAgent-portable-0.0.3.zip`
- 打包 EXE：`artifacts/windows-package/SimplerJiangAiAgent.Desktop.exe`

### 当前问题

- 初次重跑打包时失败，不是构建问题，而是旧的打包桌面端/后端进程还占着 `artifacts/windows-package` 下的文件；清理进程后已解决。
- 初次生成便携包时失败，是因为还有残留的打包后端进程持有 DLL 文件；停止该进程后已解决。
- 前端 build 仍然会报 Vite 主包 chunk 过大的 warning，但构建本身成功，这一项不阻塞 `v0.0.3` 发布。