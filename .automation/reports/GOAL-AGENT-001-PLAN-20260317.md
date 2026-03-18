# GOAL-AGENT-001 Planning Report (2026-03-17)

## EN
### Summary
Planned a dedicated redesign goal for the stock multi-agent analysis chain. The purpose is to move the current system from a structured report generator toward a more traceable, less polluted, and better-calibrated decision-assist engine.

### Planning Conclusions
- The current architecture direction is broadly correct, but real LLM audit logs show recurring weaknesses in evidence grounding, role overlap, context pollution, and confidence calibration.
- Human-facing evidence should be URL-first, not evidenceId-first.
- URL alone is insufficient. The backend should provide a traceable evidence object plus article-read status, instead of trusting the model to freely browse and self-assert that it read the article.
- Commander should become a constrained synthesizer, not a second news-generation layer.

### Full Problem Statement From Discussion
1. Evidence binding is too weak. The prompts require `source` and `publishedAt`, but they do not force the model to cite only facts that actually exist in context, and they do not require a traceable fact/news/source record. Real log samples show plausible-but-unverifiable phrases such as `华尔街日报`, `路透社`, `北向资金连续净流入`, and `社交媒体综合统计`, for example around `llm-requests.txt:36513`, `llm-requests.txt:38138`, and `llm-requests.txt:40320`.
2. Sub-agent roles are not specialized enough. The stock-news, sector-news, financial, and trend agents all emit `signals`, `triggers`, `invalidations`, and `riskLimits`, so each of them is acting like half a commander instead of a narrow analyst. This can be seen in the prompt contracts around `StockAgentOrchestrator.cs:886`, `StockAgentOrchestrator.cs:940`, `StockAgentOrchestrator.cs:988`, and `StockAgentOrchestrator.cs:1040`.
3. The upstream context is itself polluted. Both stock-news and sector-news prompts consume `localFacts.marketReports`, while the market-report stream contains unrelated Seeking Alpha, CoinTelegraph, crypto, and overseas single-stock content, visible in log regions such as `llm-requests.txt:68459`, `llm-requests.txt:70199`, and `llm-requests.txt:77700`.
4. Probabilities and confidence values are not calibrated enough. Repeated confidence values like `0.88` and standardized bullish/bearish probability wording appear in logs such as `llm-requests.txt:40320`, `llm-requests.txt:40573`, and `llm-requests.txt:6564`, which looks more like model habit than measured calibration.
5. Failure and missing-data paths are not conservative enough. The logs contain total agent failures, timeouts, HTML responses, and non-JSON reasoning leakage, for example around `llm-requests.txt:351`, `llm-requests.txt:490`, `llm-requests.txt:2026`, and `llm-requests.txt:3590`, yet the system can still produce a complete-looking final conclusion.

### Revised Evidence Decision
- The initial idea was to make evidence ID binding a hard constraint.
- The discussion refined this: the real hard constraint should be a traceable evidence object, not a user-facing evidenceId.
- URL should be the primary external field because it is intuitive for human review.
- URL alone is still insufficient because it does not prove which part of the article was used, whether the article was fully read, or whether the content came from stable Local-First ingestion.
- The evidence object must therefore carry both reviewability and read provenance: `source`, `publishedAt`, `url`, `title`, `excerpt`, `readMode`, `readStatus`, and optionally an internal local record key.
- High-confidence commander judgments should only rely on evidence that is both traceable and marked as actually read via local full text or local summary ingestion.
- Full-text reading should be selective rather than universal. It is most justified for announcements, earnings, regulatory filings, major contracts, earnings pre-announcements, and news that can invalidate an active trading plan.

### Planned Scope
1. Redesign the evidence schema around a URL-first traceable evidence object with `source`, `publishedAt`, `url`, `title`, `excerpt`, `readMode`, `readStatus`, and `ingestedAt`, while keeping any internal DB key optional and non-user-facing.
2. Add a backend-first article ingestion/read pipeline so the system fetches and cleans article content, then passes actual readable content or extracted summaries to the LLM. Metadata-only or fetch-failed evidence must be marked explicitly and down-weighted.
3. Re-split sub-agent responsibilities so `stock_news`, `sector_news`, `financial_analysis`, and `trend_analysis` stop overlapping, and `commander` can only synthesize upstream evidence instead of inventing new claims.
4. Tighten Local-First context hygiene for stock/sector/market evidence pools, and isolate or downgrade overseas/crypto/noisy macro feeds that should not dominate A-share stock judgment.
5. Precompute deterministic features in code before prompting the model, including freshness, coverage, conflict counts, trend state, volatility, valuation drift, and revision deltas.
6. Redesign `confidence_score` into a semi-rule-based output constrained by evidence coverage, evidence conflict, evidence freshness, and degraded-path signals such as JSON repair or article-fetch failure.
7. Redesign the final commander contract around opinion plus conditions: multi-horizon direction, probability distribution, key drivers, counter-evidence, trigger conditions, invalidation conditions, risk cap, key price levels, and clickable evidence URLs.
8. Build a replay and calibration baseline from `llm-requests.txt`, historical commander records, and local facts so future development can be validated against traceability, repair rate, contamination rate, and explanation completeness.

### Direct Assessment
- The current multi-agent system is already good enough for structured information integration and UI presentation.
- It is not yet strong enough to be treated as a high-confidence trading judgment engine.
- The main weakness is not that it cannot speak; it is that it speaks too fluently, too completely, and too uniformly under weak constraints.

### Planned Validation
1. Parse `.automation/tasks.json` and `.automation/state.json` as JSON after the planning update.
2. Run focused stock-agent backend tests to keep the current prompt/guardrail baseline visible before implementation work starts.
3. Check diagnostics on the changed planning files.

### Validation Run & Results
1. Command: `node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('.\\.automation\\tasks.json','utf8')); JSON.parse(fs.readFileSync('.\\.automation\\state.json','utf8')); console.log('TASKS_JSON_OK');"`
	- Result: PASS (`TASKS_JSON_OK`)
2. Command: `dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~StockAgentPromptBuilderTests|FullyQualifiedName~StockAgentResultNormalizerTests|FullyQualifiedName~StockAgentCommanderConsistencyGuardrailsTests|FullyQualifiedName~StockAgentNewsContextPolicyTests|FullyQualifiedName~StockAgentLocalFactProjectionTests"`
	- Result: PASS (`Total: 16, Failed: 0, Passed: 16, Skipped: 0`, duration ~1.5s)
3. Validation note: changed files were also checked with editor diagnostics and returned no errors.

### Issues Observed During Validation
- PowerShell `Get-Content ... | ConvertFrom-Json` produced a false negative on the UTF-8 planning JSON file in the current shell, while Node UTF-8 parsing succeeded. The JSON files themselves are valid.
- The first `dotnet test` attempt failed because `SimplerJiangAiAgent.Api.exe` was locked by a running backend process. After stopping the process by name, the same test command passed.

### Risks Noted
- If URL-first evidence is implemented without backend reading status, the system may look more transparent while still remaining weakly grounded.
- If commander is allowed to continue introducing new evidence, prompt/schema cleanup alone will not stop hallucinated synthesis.
- If market/sector context hygiene is not fixed, better prompt wording will still inherit noisy or irrelevant upstream facts.

## ZH
### 摘要
本轮先把“多 Agent 分析链路重构”单独立项为 GOAL-AGENT-001。目标不是立刻改代码，而是先把问题、步骤、验收口径写清楚，让后续开发从“结构化研报生成”转向“证据可追溯、上下文更干净、置信度可校准、结果可回放”的决策辅助引擎。

### 规划结论
- 现有总体架构方向没有错，但从真实 LLM 审计日志看，主要短板在证据绑定弱、角色重叠、上下文污染、置信度缺少硬约束。
- 面向人的证据展示应该以 URL 为主，而不是以 evidenceId 为主。
- 但 URL 也不能单独解决问题，必须补上“后端是否真的抓到了正文、LLM 实际读到了什么”的 read status/read mode 链路。
- commander 必须收口成“受约束的综合判断层”，不能继续作为第二个自由发挥的新闻生成层。

### 来自原始沟通的完整问题定义
1. 证据绑定太弱。当前提示词虽然要求 `source` 和 `publishedAt`，但没有强制模型只能引用上下文里真实存在的事实项，也没有强制返回可回溯的 fact/news/source record。真实日志里已经出现过很像研报的话术，例如 `华尔街日报`、`路透社`、`北向资金连续净流入`、`社交媒体综合统计`，可见于 `llm-requests.txt:36513`、`llm-requests.txt:38138`、`llm-requests.txt:40320` 等位置。
2. 子 Agent 分工不够专。个股资讯、板块资讯、基本面、走势 4 个 Agent 都在输出 `signals`、`triggers`、`invalidations`、`riskLimits`，实际上都在做半个 commander，可见于 `StockAgentOrchestrator.cs:886`、`StockAgentOrchestrator.cs:940`、`StockAgentOrchestrator.cs:988`、`StockAgentOrchestrator.cs:1040` 附近的 prompt contract。
3. 上下文本身有污染。`stock_news` 与 `sector_news` 都允许直接吃 `localFacts.marketReports`，但当前大盘环境中已经混进不少 Seeking Alpha、CoinTelegraph、加密资产和海外个股内容，可见于 `llm-requests.txt:68459`、`llm-requests.txt:70199`、`llm-requests.txt:77700` 一带，这会把 A 股单票分析拉向泛宏观杂讯。
4. 概率和置信度不够校准。日志中多次出现机械化 `0.88` 置信度和模板化上涨/下跌/震荡概率表达，例如 `llm-requests.txt:40320`、`llm-requests.txt:40573`、`llm-requests.txt:6564`，更像模型表达习惯，而不是历史回归后的校准结果。
5. 异常和缺失场景还不够保守。日志中已经出现子 Agent 全部失败、超时、HTML 返回页、非 JSON 思维泄漏等 degraded path，例如 `llm-requests.txt:351`、`llm-requests.txt:490`、`llm-requests.txt:2026`、`llm-requests.txt:3590`，但系统仍可能产出“格式完整但过度自信”的最终结论。

### 证据约束的修正版决策
- 最初的直觉是把 evidenceId 绑定做成硬约束。
- 这次沟通后的修正版是：真正应该做成硬约束的，不是面向用户暴露的 evidenceId，而是“可回溯证据对象”。
- URL 应该成为外显主字段，因为它最适合人回看和点击核查。
- 但 URL 单独存在并不能证明模型读了哪一段、是否读完全文、以及内容是否来自稳定的 Local-First 抓取链路。
- 因此 evidence object 至少要同时携带可审查字段和已读证明：`source`、`publishedAt`、`url`、`title`、`excerpt`、`readMode`、`readStatus`，必要时再补内部 local record key。
- commander 的高置信度判断，只能采信既可追溯、又带本地正文或本地摘要已读状态的证据。
- “要求阅读全文”应是选择性触发，而不是默认全量触发。更适合全文抓取的内容包括公告、财报、监管文件、重大合同、业绩预告，以及会直接影响交易计划失效条件的新闻。

### 规划范围
1. 重构证据 schema：以 URL-first 的 evidence object 为核心，至少包含 `source`、`publishedAt`、`url`、`title`、`excerpt`、`readMode`、`readStatus`、`ingestedAt`，内部数据库关联键如有需要可保留，但不作为用户主视图。
2. 增加后端优先的正文获取链路：由后端抓取并清洗原文正文，再抽摘要或关键段落交给 LLM；如果只能拿到元数据或抓取失败，必须显式标记并自动降权。
3. 重切子 Agent 职责：`stock_news` 负责个股事件事实，`sector_news` 负责板块/市场 regime，`financial_analysis` 负责慢变量基本面和估值锚，`trend_analysis` 负责多周期趋势和量价结构，`commander` 只能综合上游证据，不能额外发明新事实。
4. 强化 Local-First 上下文净化：继续分离 stock/sector/market 三层事实池，对 A 股场景中不相关的海外宏观、加密货币、泛美股噪音做隔离或降权。
5. 先在代码里算确定性特征：例如新鲜度、覆盖率、冲突数、趋势状态、波动风险、估值偏离、历史改判差异，再由 LLM 解释，不让模型“脑补数字”。
6. 重构 `confidence_score`：至少由证据覆盖度、证据冲突度、证据新鲜度、链路降级状态四类因素约束；一旦出现 JSON 修复、正文缺失、证据不可追溯等情况，要自动降级。
7. 重构 commander 输出 contract：围绕“观点 + 概率 + 条件 + 风险 + 证据”统一字段，包含多周期方向、概率分布、核心驱动、反证、触发条件、失效条件、风险上限、关键价位和可点击 URL。
8. 建立回放与校准基线：利用 `llm-requests.txt`、历史 commander 记录和本地事实库，统计证据可追溯率、解析修复率、污染混入率、置信度分布和改判解释完整率，作为后续开发验收门槛。

### 直接评价
- 当前这套多 Agent 在“可读性、结构化、可展示性”上已经过关。
- 在“证据约束、角色分工、概率校准、走势预测有效性”上还明显不够。
- 最值得优化的不是文案，而是让模型少猜、少编、少抢结论，把它变成受约束的分析组件。

### 计划校验
1. 规划更新后校验 `.automation/tasks.json` 与 `.automation/state.json` 的 JSON 可解析性。
2. 跑一组与 stock agent 相关的后端定向测试，确认当前 prompt/guardrail 基线仍然稳定。
3. 检查本次改动文件的诊断状态。

### 实际校验命令与结果
1. 命令：`node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('.\\.automation\\tasks.json','utf8')); JSON.parse(fs.readFileSync('.\\.automation\\state.json','utf8')); console.log('TASKS_JSON_OK');"`
	- 结果：通过（输出 `TASKS_JSON_OK`）
2. 命令：`dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~StockAgentPromptBuilderTests|FullyQualifiedName~StockAgentResultNormalizerTests|FullyQualifiedName~StockAgentCommanderConsistencyGuardrailsTests|FullyQualifiedName~StockAgentNewsContextPolicyTests|FullyQualifiedName~StockAgentLocalFactProjectionTests"`
	- 结果：通过（`总计 16，失败 0，通过 16，跳过 0`，测试耗时约 1.5 秒）
3. 说明：本次改动文件也已通过编辑器诊断检查，未发现错误。

### 校验过程中的问题记录
- 当前 PowerShell 会话里使用 `Get-Content ... | ConvertFrom-Json` 对 UTF-8 中文 JSON 做校验时出现了假失败，但 Node 的 UTF-8 解析通过，说明文件本身是合法 JSON。
- 第一次 `dotnet test` 失败不是代码问题，而是运行中的 `SimplerJiangAiAgent.Api.exe` 锁住了测试编译产物；按进程名停止后，同一命令复跑通过。

### 风险记录
- 如果只把 evidenceId 换成 URL，但没有 read status/fulltext provenance，系统只是“看起来更透明”，并没有真正变得更可验证。
- 如果 commander 继续允许自己引入上游没引用过的新证据，再严格的 schema 也压不住幻觉综合。
- 如果不先解决 market/sector 上下文污染，仅靠 prompt 文案优化，结论质量仍会被脏上游拖垮。
