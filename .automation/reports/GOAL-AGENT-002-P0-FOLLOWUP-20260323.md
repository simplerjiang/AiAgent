# GOAL-AGENT-002-P0 Follow-up Dev Report (2026-03-23)

## EN

### Scope

- Continue buglist follow-up under the existing GOAL-AGENT-002-P0 stability/output-safety line.
- Focus only on the still-open reasoning-leak class affecting:
  - stock recommendation output,
  - stock assistant chat output,
  - source-governance developer-mode log summaries.

### Actions

- Added shared frontend sanitizer: `frontend/src/utils/reasoningSanitizer.js`.
- Rewired `frontend/src/components/ChatWindow.vue` to use the shared sanitizer for final and streaming assistant content.
- Rewired `frontend/src/modules/admin/SourceGovernanceDeveloperMode.vue` to use the same shared summarizer for list/raw previews.
- Expanded frontend reasoning-scaffold coverage to include:
  - `Simulating Information Retrieval`
  - `Interpreting the Data`
  - `Formulating the Response`
  - `Analyzing the Data`
  - existing `Defining the Scope` / `Assessing Risk Elements` / `Synthesizing Risk Insights`
- Expanded backend governance-log sanitizer in `backend/SimplerJiangAiAgent.Api/Infrastructure/Jobs/SourceGovernanceReadService.cs` with the same additional title phrases.
- Added regression coverage for the new phrase family in frontend and backend tests.
- Updated `.automation/buglist.md` with today’s fix and validation notes for Bug 4, Bug 5, and Bug 11.
- Added a second hardening wave for both frontend and backend sanitizers: detection is no longer limited to bold English title scaffolds and now also strips leading English introspection prose such as `I'm currently dissecting...`, `Here's how I'm approaching this...`, and `The user ... needs a JSON array ...`.
- Aligned packaged startup health handling in `desktop/SimplerJiangAiAgent.Desktop/Form1.cs` and `start-all.bat` so both the desktop host and the launcher now wait on fixed port `5119` with a 90-second backend-start timeout.
- Re-ran Browser MCP validation on fresh runtime routes:
  - stock recommendation: passed for the reasoning-leak symptom,
  - stock assistant: passed for the reasoning-leak symptom,
  - source-governance Developer Mode: new log rows are redacted, and older dirty non-JSON rows are now collapsed to a safe placeholder while extracted JSON remains available in the JSON view.
- Added a final frontend-only safety layer in `frontend/src/modules/admin/SourceGovernanceDeveloperMode.vue` to collapse suspicious mixed-language non-JSON historical responses into a safe summary placeholder without hiding extractable JSON payloads.

### Test Commands And Results

- Command: `npm --prefix .\frontend run test:unit -- src/modules/stocks/StockRecommendTab.spec.js src/modules/stocks/StockInfoTab.spec.js src/modules/admin/SourceGovernanceDeveloperMode.spec.js`
- Result: passed, 63/63.

- Command: `dotnet build .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --no-restore -p:OutDir=D:\SimplerJiangAiAgent\.automation\tmp\source-gov-sanitize\`
- Result: passed.

- Command: `dotnet vstest .\.automation\tmp\source-gov-sanitize\SimplerJiangAiAgent.Api.Tests.dll --Tests:SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldStripReasoningTitleScaffold,SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldStripExtendedReasoningTitleScaffold`
- Result: passed, 2/2.

- Command: `./start-all.bat`
- Result: passed. Frontend build, backend publish, desktop publish, packaged desktop startup, and `http://localhost:5119/api/health` all succeeded.

- Command: `npm --prefix .\frontend run test:unit -- src/modules/stocks/StockRecommendTab.spec.js`
- Result: passed, 6/6 after adding the English introspection narrative streaming regression.

- Command: `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldStripLiveBoldReasoningTitleSequence|SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldRedactEnglishReasoningNarrativePreamble|SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldKeepJsonAfterEnglishReasoningNarrative|SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldRedactUserTaskEnglishNarrative"`
- Result: passed, 4/4.

- Command: `./start-all.bat`
- Result: passed again after the final backend sanitizer update; packaged desktop startup remained healthy on `http://localhost:5119`.

- Command: `npm --prefix .\frontend run test:unit -- src/modules/admin/SourceGovernanceDeveloperMode.spec.js`
- Result: passed, 8/8, including the new regression that hides historical mixed-language non-JSON dirty output in both the list view and the detail modal.

### Issues

- A direct Browser MCP page snapshot after rebuild still showed stale social-tab labels not present in source or packaged assets. Grep against `frontend/dist/**` and `artifacts/windows-package/**` found no `社媒优化` or `社媒爬虫` strings, so this currently looks like browser-side residual state rather than a source regression.
- Because of that page-state noise, this round does not mark Bug 4 / Bug 5 / Bug 11 fully closed at Browser-MCP level yet, even though code and targeted tests are now aligned.
- A direct `dotnet test` against the default backend output path was blocked by a running API process locking `backend/SimplerJiangAiAgent.Api/bin/Debug/net8.0/SimplerJiangAiAgent.Api.exe`; the isolated output-directory flow was used instead.
- Browser MCP fresh-session rechecks now close the user-facing reasoning-leak symptom for Bug 4 and Bug 11, and confirm the startup mismatch fix for Bug 12 via repeated packaged launches.
- Bug 5 is now closed: fresh Developer Mode rows redact raw reasoning, and older historical mixed-language non-JSON rows are collapsed to a safe placeholder while extracted JSON remains accessible in the dedicated JSON view.

## ZH

### 范围

- 继续处理现有 GOAL-AGENT-002-P0 稳定性/输出安全线下的 buglist follow-up。
- 本轮只收口仍未关闭的 reasoning 泄露类问题，覆盖：
  - 股票推荐输出，
  - 股票助手聊天输出，
  - 来源治理 Developer Mode 日志摘要。

### 本轮动作

- 新增前端共享清洗器：`frontend/src/utils/reasoningSanitizer.js`。
- `frontend/src/components/ChatWindow.vue` 已改为统一复用共享清洗器，覆盖流式与最终助手内容。
- `frontend/src/modules/admin/SourceGovernanceDeveloperMode.vue` 已改为统一复用共享摘要清洗逻辑，避免治理页与聊天页词表漂移。
- 前端新增并统一覆盖以下标题式脚手架：
  - `Simulating Information Retrieval`
  - `Interpreting the Data`
  - `Formulating the Response`
  - `Analyzing the Data`
  - 以及既有 `Defining the Scope` / `Assessing Risk Elements` / `Synthesizing Risk Insights`
- 后端 `backend/SimplerJiangAiAgent.Api/Infrastructure/Jobs/SourceGovernanceReadService.cs` 也同步补齐同批词表。
- 前后端都新增了对应回归测试。
- `.automation/buglist.md` 已写回今天对 Bug 4、Bug 5、Bug 11 的修复与验证记录。
- 本轮后续又做了第二波加固：前后端清洗器不再只识别粗体英文标题，而是开始收口 `I'm currently dissecting...`、`Here's how I'm approaching this...`、`The user ... needs a JSON array ...` 这类英文元叙事前缀。
- `desktop/SimplerJiangAiAgent.Desktop/Form1.cs` 与 `start-all.bat` 已同步对齐 packaged backend 启动判定：固定使用 `5119`，并将后端健康等待窗口统一到 90 秒。
- Browser MCP 已在新鲜运行态下重新验证：
  - 股票推荐：推理泄露主症状通过；
  - 股票助手：推理泄露主症状通过；
  - 治理开发者模式：新日志条目已正确脱敏，历史非 JSON 脏输出也已被安全摘要替换，同时合法 JSON 仍可在 JSON 视图查看。

### 测试命令与结果

- 命令：`npm --prefix .\frontend run test:unit -- src/modules/stocks/StockRecommendTab.spec.js src/modules/stocks/StockInfoTab.spec.js src/modules/admin/SourceGovernanceDeveloperMode.spec.js`
- 结果：通过，63/63。

- 命令：`dotnet build .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --no-restore -p:OutDir=D:\SimplerJiangAiAgent\.automation\tmp\source-gov-sanitize\`
- 结果：通过。

- 命令：`dotnet vstest .\.automation\tmp\source-gov-sanitize\SimplerJiangAiAgent.Api.Tests.dll --Tests:SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldStripReasoningTitleScaffold,SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldStripExtendedReasoningTitleScaffold`
- 结果：通过，2/2。

- 命令：`./start-all.bat`
- 结果：通过；已成功完成 frontend build、backend publish、desktop publish、打包版桌面启动和 `http://localhost:5119/api/health` 健康检查。

- 命令：`npm --prefix .\frontend run test:unit -- src/modules/stocks/StockRecommendTab.spec.js`
- 结果：通过，6/6；新增“英文元叙事前缀”流式样本后仍全部通过。

- 命令：`dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldStripLiveBoldReasoningTitleSequence|SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldRedactEnglishReasoningNarrativePreamble|SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldKeepJsonAfterEnglishReasoningNarrative|SourceGovernanceReadServiceTests.GetLlmConversationLogsAsync_ShouldRedactUserTaskEnglishNarrative"`
- 结果：通过，4/4。

- 命令：`./start-all.bat`
- 结果：最终后端加固后再次通过；打包版桌面仍稳定在 `http://localhost:5119` 健康起来。

- 命令：`npm --prefix .\frontend run test:unit -- src/modules/admin/SourceGovernanceDeveloperMode.spec.js`
- 结果：通过，8/8；已覆盖“历史中英混杂非 JSON 脏输出”的列表与详情弹层安全摘要回归。

### 剩余问题

- 重建后直接做 Browser MCP 页面快照时，仍看到了源码和产物里都不存在的社媒导航文案；对 `frontend/dist/**` 与 `artifacts/windows-package/**` 的搜索均未命中 `社媒优化` / `社媒爬虫`，当前更像是浏览器侧残留状态，而不是源码回归。
- 因为这个页面状态噪声，本轮没有把 Bug 4 / Bug 5 / Bug 11 在 Browser MCP 层面直接关闭；但代码和定向测试层面已经对齐。
- 直接对默认输出目录跑 `dotnet test` 时，运行中的 API 进程锁住了 `backend/SimplerJiangAiAgent.Api/bin/Debug/net8.0/SimplerJiangAiAgent.Api.exe`；本轮已按隔离输出目录流程完成验证。
- 现在可以把 Bug 4、Bug 11 的“用户界面推理泄露”视为已通过 fresh Browser MCP；Bug 12 的 packaged startup mismatch 也已经通过多次 `start-all.bat` 验证。
- Bug 5 已完成关闭：Developer Mode 新生成日志和历史坏样本都已通过 Browser MCP 复核，前者会显示推理脱敏占位，后者会显示非 JSON 安全摘要；若原文中仍可提取合法 JSON，界面会单独保留 JSON 美化视图。