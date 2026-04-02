# SimplerJiangAiAgent Repository Rules

This file is the repository-specific layer. Keep agent identity in `.github/copilot-instructions.md` and detailed PM behavior in `.github/agents/pm.agent.md`. Keep engineering, validation, browser, data, and domain rules here.

## Repository Scope

- This repository is split into frontend, backend, and desktop.
- Follow existing project conventions and keep changes minimal and reviewable.
- Keep single files reasonably sized. Target under 1000 lines per file when practical, and split into focused modules when behavior can stay unchanged.

## Mandatory Workflow

1. Analyze task scope first and avoid speculative refactors.
2. If frontend and backend are both involved, start backend first and confirm health before frontend work.
3. Run validations in this order: unit tests first, then Browser MCP checks when UI is involved.
4. If new features are proposed or accepted, update `README.md` and `.automation/tasks.json` immediately with clear scope descriptions.
5. If new work breaks old behavior, fix both in the same task.
6. Complete only when new and existing behavior work together.

## Automation And Reporting

- Follow `.automation/README.md` and use `.automation/prompts` for plan, dev, and test flows.
- Keep `.automation/tasks.json` and `.automation/state.json` aligned with actual progress.
- After planning and after development, write a bilingual report in `.automation/reports`: English for agents, Chinese for the user, including actions, test commands, results, and issues.
- When a roadmap item is pulled into current scope, create a dedicated remaining-scope task ID such as `*-R1`, and sync README, tasks, state, and reports in the same change.

## Testing And Verification

- Validate every change with a relevant unit test or script. If no direct test exists, run the closest relevant verification script.
- If no backend code changed, backend tests are optional. If no frontend code changed, frontend tests are optional.
- Before any push to GitHub, verify the packaged Windows desktop chain at least once: run `scripts\publish-windows-package.ps1`, confirm `artifacts\windows-package\SimplerJiangAiAgent.Desktop.exe` exists, and record the result.
- If the change affects desktop startup, packaging, installer, runtime paths, or launch behavior, also launch the packaged desktop EXE once and verify the bundled app actually comes up.
- After tests pass, create a focused commit, include report updates, and push. If no remote is configured, ask the user for the remote URL before pushing.
- After each commit, remove useless local modifications such as caches, temp outputs, and browser profile artifacts. Keep useful artifacts only via ignore rules.
- If OpenCode performs development, it must still be re-tested and accepted through the existing review flow before replying to the user.

## Browser And UI Validation

- For UI tasks, verify rendering, clickability, and absence of backend or frontend runtime errors.
- Standardize Browser MCP selection to avoid trial-and-error: default to CopilotBrowser MCP first, fallback 1 to Playwright Edge, fallback 2 to the VS Code integrated browser tools only when CopilotBrowser is unavailable and chat browser tools are enabled.
- Prefer CopilotBrowser MCP for backend-served pages because it is the fastest and lowest-friction path in this repo: first check browser status, install only if missing, then reuse a live tab or navigate directly to `http://localhost:<port>`.
- For CopilotBrowser MCP, the default fast-start sequence is: status check, tab list or select, direct navigate, page snapshot or targeted click flow, console messages, network requests.
- Use CopilotBrowser MCP as long as the browser context is alive, tabs are controllable, and the target page can be reached. Prefer `http://localhost:<port>` over `http://127.0.0.1:<port>`, and prefer the backend-served frontend over Vite whenever both are available.
- Fallback 1 is Playwright Edge. Use it when CopilotBrowser MCP is unavailable, when trace or video capture is required, when persistent-profile behavior matters, when a specific browser channel is required, or when CopilotBrowser element targeting is unstable on a dense canvas-heavy page.
- For Playwright Edge fallback, prefer existing repo scripts under `frontend/scripts/edge-check-*.mjs` or `.automation/scripts/edge-check-*.mjs` before inventing a new flow. Keep `channel: 'msedge'` when available, fall back to bundled Chromium only if Edge launch fails, and use `.automation/edge-profile` as the dedicated user-data-dir whenever the default profile is locked or polluted.
- Fallback 2 is the VS Code integrated browser path. Use it only as a last resort when CopilotBrowser MCP is unavailable and `workbench.browser.enableChatTools` is enabled. If that setting is off and the page can only be opened without page-content access, treat it as a manual visibility aid rather than acceptance evidence.
- Browser validation must go beyond static existence checks. Click key actions, wait for visible state changes, and inspect both backend logs and frontend console logs for runtime errors.
- Prefer backend-served frontend for Browser MCP. Use Vite dev server only when `/api` proxying is configured, and set explicit backend URLs to avoid port conflicts.
- After a restart, prefer `http://localhost:<port>` over `http://127.0.0.1:<port>` so stale browser-session errors do not pollute diagnosis.
- Reuse the port already exposed by the running backend whenever possible. When no port is known yet, inspect the backend startup log first. Many validation scripts assume `http://localhost:5119/`, but the live backend log still has priority over any hard-coded default.
- For Playwright Edge fallback, use `.automation/edge-profile` when the default profile is locked. On dense dashboards, use resilient click strategies such as forced clicks and tolerate valid empty states.
- Insert new browser interaction and assertion blocks before any `browser.close()` or page teardown call.

## Terminal And Startup Safety

- When reusing a terminal session, confirm `cwd` before running relative-path commands.
- For repo startup scripts, prefer absolute script paths or verify `cwd` immediately before invocation.
- For frontend npm commands in shared terminals, prefer `npm --prefix .\frontend ...` or verify `cwd` first.
- Local startup scripts on fixed ports must be idempotent: if health is already good, skip restart; if the same app owns the port, stop it first; if another process owns it, fail fast with a clear message.
- If required ports are occupied, either stop the conflicting process or choose a free port and record the chosen ports in the report.

## Security And Secrets

- Never commit API keys, tokens, passwords, or other credentials.
- Store third-party secrets only in environment variables or ignored local secret files, never in tracked repository files.
- Avoid writing secrets into terminal history, logs, or reports.
- Keep tracked `App_Data/llm-settings.json` free of plaintext secrets.

## Database And Backend Reliability

- Before adding or changing tables or columns, validate SQL locally with `sqlcmd`, then verify the resulting schema is correct.
- During testing, verify touched schema objects with `sqlcmd`, including tables, columns, and indexes. If schema mismatches exist, fix them before concluding the task.
- After schema initializer changes add columns to existing tables, verify them via `sys.columns` and apply local `ALTER` fixes if needed.
- Before backend `dotnet test`, stop any running API process that locks `backend/SimplerJiangAiAgent.Api/bin/Debug/net8.0/SimplerJiangAiAgent.Api.exe`.
- For local startup stability, keep `appsettings.Development.json` connection strings pointed at a verified reachable SQL instance and verify connectivity with `sqlcmd` before launch.
- On Windows, when `sqlcmd` targets a named-pipe instance containing `$`, wrap the `-S` value in single quotes so PowerShell does not corrupt it.
- For legacy local tables that persist enums as strings, use tolerant parsing that maps known historical values and degrades unknown values to a safe status instead of crashing APIs.
- For `TradingPlanEvents`, assume legacy local tables may still require non-null fields such as `Strategy`, `Reason`, and `CreatedAt`. Map and fill them, then verify against the real local schema.
- For `TradingPlans`, assume legacy local tables may still require `PlanKey`, mirrored `Title`, and a unique `PlanKey` index. Explicitly fill those values.
- For `TradingPlans` list surfaces, filter out legacy placeholder rows missing `Symbol`, `Name`, or a valid `AnalysisHistoryId` at the backend query boundary.

## AI, LLM, And News Rules

- Any LLM-generated stock suggestion must use a structured schema with evidence sources, confidence score, trigger conditions, invalidation conditions, and explicit risk limits.
- For news-driven output, enforce a strict recency window and require source plus published timestamp on every key evidence item. If timestamps are missing, downgrade the conclusion to neutral or insufficient-data.
- For multi-agent news context, default to a 72-hour trusted-source window and expand to 7 days only when evidence count is insufficient, while marking the expansion explicitly.
- For prompt changes involving news analysis, define source tiers explicitly and downgrade to neutral when only blocked or untimestamped evidence is available.
- Maintain a dynamic news-source registry with automated health scoring. LLM-discovered sources may enter production only after programmatic verification and quarantine safeguards.
- For GOAL-013 sector-context ingestion, use a targeted Eastmoney source for `level=sector`, aggregate verified global business RSS sources for `level=market`, and keep a deterministic fallback when an upstream response is invalid or times out.
- For local fact refresh flows, never let freshness short-circuits skip pending `IsAiProcessed = false` rows. Market, stock, and sector rows must still be AI-enriched before returning.
- For GOAL-013 stock news pages, render local fact buckets independently from slower `/api/stocks/news/impact` analysis and protect `/api/news` refreshes with per-symbol plus market-level locks.
- For any LLM logging enhancement, keep a single sink file with stable `traceId` request, response, and error records plus content truncation.
- For local LLM provider settings, separate provider key from provider type, route model choice from backend business logic using explicit `IsPro`, and never trust the caller to force a Pro model for non-Pro traffic.
- For `HttpRequestException` diagnostics, keep both outer and inner exception messages because actionable gateway detail may exist only on the outer message.
- For Gemini or OpenAI JSON parsing, always validate `JsonElement.ValueKind` before `GetArrayLength` or `EnumerateArray`, and treat `null` or non-array nodes as empty data.
- For frontend LLM audit views, never pair raw log lines on the client. Aggregate backend records by `traceId` first.

## Stock Terminal And Charting

- For GOAL-012 and related stock-terminal refinements, keep symbol query and history controls compact and prioritize vertical space for K-line and minute charts.
- For fast symbol switching, load `/api/stocks/detail/cache` first, protect detail-state writes with a per-request token, keep the live request as a background refresh, and preserve per-symbol workspace state instead of resetting it.
- Stock-detail cache endpoints that feed charts must return only the latest trading-session minute-line data plus the most recent K-line window, never multi-day minute history in the fast path.
- For chart overlays, render only numeric support or resistance values, and use deterministic fallback precedence: commander recommendation first, then trend extremes.
- For multi-line indicators and overlays, always expose `color -> line name -> meaning` in the UI help layer.
- If a beta chart engine's built-in multi-line rendering is ambiguous, replace it with a controlled custom definition and validate actual canvas output.
- Keep chart-engine experiments behind `frontend/src/modules/stocks/charting/**`. Reject widget-wrapper or template-style engines as the terminal core when first-party `minuteLines` and `kLines` plus future overlays are required.
- For beta chart-engine replacements, pin the exact package version in both `package.json` and the lockfile. When the replacement is accepted, remove the superseded package in the same change and validate on a fresh backend-served page.
- For `klinecharts`, always use Unix-millisecond timestamps and normalize cumulative minute volumes into per-bar hand counts before rendering volume panes.
- Never globally sort or deduplicate render-plan `calcParams`. Only merge truly shared indicator params such as MA.
- Each new chart-registry indicator must land with pane constants, per-view removal filters, and regression tests in the same change.
- Each new chart signal strategy must wire the renderer output path first so computed signals are actually visible.
- For TD Sequential and similar sequence signals, lock counting logic with synthetic K-line tests. When full sequence labels are too noisy, keep the late-stage `6-9` setup visible, style `6-7` as weaker warnings and `8-9` as stronger cues, and verify both tests and browser-visible help text.
- For MACD cross signals, compute markers directly from DIFF-versus-DEA crossovers and lock one bullish plus one bearish fixture before treating the feature as done.
- For multi-signal Phase C batches, cover both day-view and minute-view marker families with synthetic fixtures rather than relying on live samples.
- For chart legend controls, drive active state from one per-view visibility model and update both runtime chart styles and registry-managed overlays together. Cover active and inactive classes in tests.
- For chart fullscreen controls, bind button state to the browser `fullscreenchange` lifecycle and cover both enter and exit paths in tests.

## Frontend Test Specifics

- In Vue component tests, assert rendered copy from the mounted wrapper such as `wrapper.text()` or scoped locators rather than `document.body.textContent`.
- For `StockInfoTab` UI tests, scope duplicate action labels such as `刷新` to the intended card container.
- When `StockInfoTab` behavior adds new `/api/stocks/**` reads, update shared fetch mock defaults before asserting older UI surfaces.
- For `StockInfoTab` polling tests that use fake timers, flush mount-triggered async work with `await Promise.resolve()` plus `await vi.advanceTimersByTimeAsync(0)` before asserting initial request counts.

## Goal-Specific Delivery Rules

- For GOAL-007 optimization work, prefer in-place upgrades to existing multi-agent prompts and displays. Do not introduce new modules when current panels can be extended.
- For GOAL-008 Step 4.1 quote polling, source symbols from `ActiveWatchlist` and gate the worker by China A-share trading sessions.
- For GOAL-008 Step 4.3 acceptance, gate execution by `ActiveWatchlist`, deduplicate warning events by ongoing condition rather than raw metadata equality, and short-poll both the current-stock card and the global plan board.
- For P0-R1 developer mode delivery, verify the backend-served frontend with a flow covering admin login, developer-mode toggle, trace search, and API status checks before marking testing complete.
- For GOAL-013 local-fact UX, load `level=market` independently of stock selection and never hide query history with hard `slice(...)` limits. Use a scrollable layout instead.
- For GOAL-013 browser validation after market-news moved to root scope, treat `level=market` as an initial page-load dependency rather than requiring a symbol search to retrigger it.
- For GOAL-013 database work, trim accidental EF migrations back to feature tables when schema-initializer-managed legacy tables are pulled in.
- For commander consistency upgrades, inject 3-7 days of history into Commander only, require a structured revision block for rating or direction changes, and run deterministic guardrail tests for divergence tagging, low-confidence hysteresis suppression, and strong-counter-evidence override.
- For commander schema migrations, update backend history and guardrails, frontend overlay parsing, and unit tests in the same change.
