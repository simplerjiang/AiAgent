# 2026-03-23 已解决 Bug 归档

- 说明：本文件归档 2026-03-23 从 `.automation/buglist.md` 迁出的已解决 Bug 历史记录。
- 当前主清单：见 `.automation/buglist.md`。

## Bug 1: 情绪轮动页不可用，后端板块接口稳定 500

- 严重级别：高
- 复现步骤：
	1. 打开首页。
	2. 点击顶部 `情绪轮动`。
	3. 页面立即显示 `情绪轮动数据加载失败`。
- 实际结果：
	- 页面顶部快照全部是 `0` / `暂无快照`。
	- Browser console 报错：`GET /api/market/sectors?boardType=concept&page=1&pageSize=12&sort=strength => 500`。
	- 命令行复测 `GET http://localhost:5119/api/market/sectors?page=1&pageSize=3` 稳定返回 500。
- 预期结果：
	- `情绪轮动` 应能正常展示板块榜、阶段快照、比较窗口数据。
- 用户指导意见（来自人）：
	- 纳入 `GOAL-AGENT-002-P0` 运行态稳定性收口。
- 修复结果：
	- 已在 `MarketSentimentSchemaInitializer` 增补 SQLite 幂等补列与索引补齐，开发/本地 SQLite 回退场景下不再因旧表缺列导致 `/api/market/sectors` 500。
	- 2026-03-22 二次修复：定位到 `SectorRotationQueryService` 在 SQLite 上对 `decimal` 字段执行 `ORDER BY / THEN BY` 会触发 `System.NotSupportedException`；现已改为“先取最新快照，再在内存排序后分页/截断”，并新增 SQLite 回归测试锁定 `/api/market/sectors` 与 `/api/market/mainline`。
- 复测结果：
	- 2026-03-22 重新打开后仍可稳定复现，当前未通过。
	- Browser MCP：页面仍显示 `情绪轮动数据加载失败`，顶部快照回落为 `0 / 暂无快照`，实时总览区域停在 `加载中...`。
	- 命令行/API：`/api/market/sentiment/latest`、`/api/market/sentiment/history?days=10`、`/api/market/realtime/overview` 为 200，但 `/api/market/sectors?boardType=concept&page=1&pageSize=3&sort=strength` 仍为 500；`/api/market/mainline?boardType=concept&window=10d&take=6` 也返回 500；`/api/market/sectors/realtime?...` 为 200 但 `items=[]`。
	- 当前判断：Bug 1 持续存在，而且已扩大为“板块分页/主线接口异常 + 页面整屏失败态”。
	- 2026-03-22 本轮二次修复后已通过：命令行复测 `GET /api/market/sectors?boardType=concept&page=1&pageSize=3&sort=strength` 与 `GET /api/market/mainline?boardType=concept&window=10d&take=3` 均返回 200 且正文包含板块数据；Browser MCP 刷新进入 `情绪轮动` 后已看到板块榜、详情侧栏和顶部快照，不再出现 `情绪轮动数据加载失败`。

## Bug 2: 股票图表终端空白，切换周期也没有真正走轻量图表接口

- 严重级别：高
- 复现步骤：
	1. 打开 `股票信息`。
	2. 查询或点击最近查询中的 `浦发银行 sh600000`。
	3. 观察 `专业图表终端`，再切换 `日K图 / 月K图 / 年K图`。
- 实际结果：
	- 图表区域持续显示 `暂无 K 线数据`。
	- Browser network 中没有看到 `/api/stocks/chart?...` 请求。
	- 页面改为重复请求 `/api/stocks/detail/cache?...interval=day|month` 和 `/api/stocks/detail?...interval=day|month`。
	- 后端命令行直测 `GET /api/stocks/chart?symbol=sh600000&interval=day&count=60` 是 200 且返回正文，说明不是源数据缺失，而是前端链路没有把图表接口真正用起来。
- 预期结果：
	- 选股后应展示实际分时/K线图。
	- 周期切换应走 README 声明的 `/api/stocks/chart` 轻量链路，而不是详情聚合链路。
- 用户指导意见（来自人）：
	- 作为股票终端轻链路回归项持续复核。
- 修复结果：
	- 已由 `MANUAL-20260319-CHART-PERF` 收口：前端切换图表周期只请求 `/api/stocks/chart`，不再退回 `/api/stocks/detail` 聚合链路。
- 复测结果：
	- 2026-03-22 本轮仍未通过，但当前形态与最初记录不同。
	- Browser MCP：`sh600000` 的 `专业图表终端` 仍显示 `暂无 K 线数据`；切到 `月K图` 后仍为空白。
	- Browser network：本轮已经真实请求 `/api/stocks/chart?symbol=sh600000&interval=day&includeQuote=true&includeMinute=true` 和 `/api/stocks/chart?symbol=sh600000&interval=month&includeQuote=false&includeMinute=false`，说明“没走轻量链路”的旧问题已不成立。
	- 命令行/API：`/api/stocks/chart?symbol=sh600000&interval=day&includeQuote=true&includeMinute=true` 返回 200，且 payload 含 `kLines=60`、`minuteLines=256`；前端仍显示无数据，当前更像是图表渲染/字段消费不一致，而不是接口未请求。
	- 2026-03-22 本轮追加修复后，`StockInfoTab.vue` 已对股票页关键 GET 请求补上短时重试，避免瞬时 `Failed to fetch / ERR_CONNECTION_REFUSED` 直接把图表区打成永久失败态；新增前端回归测试覆盖“首轮图表请求短暂失败后自动重试成功”。
	- 2026-03-22 打包复测：重新执行 `start-all.bat` 后，Browser MCP 进入 `股票信息 -> 浦发银行 sh600000`，页面已显示 `浦发银行（sh600000）` 与 `专业图表终端`，且页面内不再出现 `暂无 K 线数据` / `暂无分时数据`。
	- 当前判断：Bug 2 以“运行态短时连接波动导致图表空白”的形态已被本轮前端重试修复并通过打包复测。

## Bug 3: 顶部导航暴露了两个纯占位模块，没有任何实际功能

- 严重级别：中
- 复现步骤：
	1. 打开首页。
	2. 点击 `社媒优化`。
	3. 点击 `社媒爬虫`。
- 实际结果：
	- `社媒优化` 页面只显示：`占位模块：后续提供文案改写、标题优化、风格适配等功能。`
	- `社媒爬虫` 页面只显示：`占位模块：后续接入爬虫任务、账号池与采集调度。`
	- 没有任何可执行能力或后端联动。
- 预期结果：
	- 未完成模块不应作为正式导航入口暴露；或者至少应有明确的禁用态/开发中标识，而不是可点击后进入空壳页。
- 用户指导意见（来自人）：
	- 纳入 `GOAL-AGENT-002-P0` 输出安全与占位功能清理范围。
- 修复结果：
	- 已从 `frontend/src/App.vue` 顶部导航移除 `社媒优化` 与 `社媒爬虫` 两个占位模块入口。
- 复测结果：
	- 2026-03-22 Browser MCP 已确认顶部导航不再出现这两个页签；当前代码复核同样已移除。

## Bug 4: 股票推荐输出格式失控，直接把模型推理过程暴露到最终界面

- 严重级别：高
- 复现步骤：
	1. 打开 `股票推荐`。
	2. 点击 `当日股票推荐`。
	3. 等待返回。
- 实际结果：
	- 返回内容直接出现 `Analyzing the Request`、`Refining the Approach`、`Simulating the Search` 等推理式文本。
	- 输出混入英文、Markdown 粗体和长篇自由发挥，不是面向用户的受控结果。
	- 内容主体偏向全球市场/美股风险叙事，不像本项目的 A 股本地优先推荐流。
- 预期结果：
	- 结果应是面向用户的结构化推荐，不应暴露原始推理过程。
	- 推荐内容应遵守本项目的 A 股、本地事实优先和前端展示约束。
- 用户指导意见（来自人）：
	- 纳入 `GOAL-AGENT-002-P0` 用户面向结果脱敏收口。
- 修复结果：
	- 已在前端聊天渲染与保存链路增加 `<think>` / reasoning scaffold 清洗，并补齐 `StockRecommendTab` 定向测试，阻断原始推理式标题直接落到最终界面。
	- 2026-03-22 本轮继续扩展 reasoning scaffold 识别，新增 `Considering the Request`、`Analyzing the Scenario`、`Refining the Strategy`、`before answering` 等标题式脚手架清洗，前端流式推荐单测已覆盖该类标题前缀。
	- 2026-03-23 再次补强前端共享清洗器：`ChatWindow.vue` 与推荐页已统一复用 `frontend/src/utils/reasoningSanitizer.js`，新增覆盖 `Simulating Information Retrieval`、`Interpreting the Data`、`Formulating the Response` 等本轮真实泄露标题，避免推荐流式输出只清掉旧词表、漏掉新标题。
	- 2026-03-23 本轮后续又把共享清洗器从“固定标题词表”提升为“英文元叙事前缀”启发式，新增处理 `I'm currently dissecting...`、`The task is clear...` 一类非标题式推理开场；`StockRecommendTab.spec.js` 已补入对应流式回归样本。
- 复测结果：
	- 2026-03-22 Browser MCP 仍可直接复现，当前未通过。
	- 返回文本首段继续出现 `Considering the Request`、`Analyzing the Scenario`、`Refining the Strategy` 等推理式标题。
	- 输出仍以全球市场/AI 算力/生物医药等泛化叙述为主，并明确写出“2026年3月22日（星期日），全球主要证券交易所均处于休市状态”，不符合本项目 A 股、本地事实优先的受控推荐预期。
	- 2026-03-22 本轮代码与单测已更新，但尚未在稳定浏览器会话中完成同路径复测；后续应在 bug 8 运行态掉线问题稳定后再次走 `股票推荐 -> 当日股票推荐` 路径确认真实 UI 输出。
	- 2026-03-23 代码级复测通过：`npm --prefix .\frontend run test:unit -- src/modules/stocks/StockRecommendTab.spec.js` 已确认流式返回中即使连续出现 `Considering the Request -> Simulating Information Retrieval -> Interpreting the Data -> Formulating the Response`，最终助手内容仍只保留 `你好世界`。
	- 2026-03-23 Browser MCP 新鲜会话复测通过：使用带时间戳的新页面 `?tab=stock-recommend&ts=...` 重新进入推荐页、创建新会话并触发真实请求后，回答已直接从中文正文开始，未再出现 `Initiating Market Analysis` / `Refining Search Strategies` / `Analyzing Current Context` 等英文推理标题。
	- 当前判断：就“股票推荐最终界面泄露推理式标题/脚手架”这一主症状，本轮 Browser MCP 已通过；推荐内容的题材选择与 A 股本地优先度仍值得继续单独跟踪，但不再作为本 bug 的继续阻塞项。

## Bug 5: Developer Mode 直接展示原始 LLM 推理/脏输出，审计日志未做安全收口

- 严重级别：中
- 复现步骤：
	1. 打开 `LLM 设置`，用 `admin / admin123` 登录。
	2. 切到 `治理开发者模式`。
	3. 勾选 `开启 Developer Mode（只读诊断）`。
	4. 查看 `LLM 对话过程日志` 列表。
- 实际结果：
	- 日志列表直接显示类似 `**Analyzing the Request**` 的原始模型推理式输出。
	- 还可见本应只返回 JSON 的任务返回了非 JSON 文案。
	- 当前展示没有对这类脏输出做聚合后的安全收口。
- 预期结果：
	- 开发者日志可以保留审计信息，但不应直接把原始推理文本作为主展示内容泄露到 UI。
	- 应优先展示结构化 request/response/error 摘要，而不是未收口的模型思维流。
- 用户指导意见（来自人）：
	- 纳入 `GOAL-AGENT-002-P0` 开发者模式安全收口。
- 修复结果：
	- 已在 `SourceGovernanceReadService` 做请求脱敏与 reasoning 输出收口；开发者模式界面改为展示请求摘要、返回摘要和原始日志摘要，不再直接外露原始推理文本。
	- 2026-03-22 本轮继续补强：后端新增标题式 reasoning 脚手架识别与标点残留兜底，前端 `SourceGovernanceDeveloperMode.vue` 的日志摘要也同步二次清洗 `Considering the Request` 等标题，避免摘要列表直接透传原始推理开场白。
	- 2026-03-23 再次补强前后端词表：前端开发者模式摘要与后端治理日志摘要现已同步覆盖 `Interpreting the Data`、`Formulating the Response`、`Simulating Information Retrieval` 等新增标题式脚手架，且前端列表摘要改为复用共享清洗器，避免聊天与治理页再次各自漂移。
	- 2026-03-23 本轮后续继续把治理日志摘要从“标题词表”升级为“英文元叙事前缀”收口：后端 `SourceGovernanceReadService` 与前端共享清洗器都新增对 `I'm currently dissecting...`、`Here's how I'm approaching this...`、`The user ... needs a JSON array ...` 这类英文自述前缀的识别与脱敏，后端新增对应回归测试。
	- 2026-03-23 本轮继续补上 Developer Mode 的展示层安全摘要策略：`SourceGovernanceDeveloperMode.vue` 现在会对“非 JSON 且中英混杂的历史脏输出”直接显示 `返回内容不是结构化 JSON，已按安全摘要收口。`，同时若能从原文中提取到合法 JSON，仍保留 JSON 美化视图供审计查看。
- 复测结果：
	- 2026-03-22 本轮部分改善，但仍未通过。
	- 界面文案已经改成“原始 prompt 与推理文本不在界面直接展示”，多数条目也会显示“返回内容包含中间推理，已脱敏。”。
	- 但日志列表首条仍直接显示：`返回：**Considering the Request** Okay, I'm now zeroing in on the core of this request...`，说明审计列表摘要仍在泄露原始模型输出。
	- 前端单测同步出现回归：`SourceGovernanceDeveloperMode.spec.js` 期望 `请求内容/返回内容`，实际仍为 `请求摘要...`。
	- 2026-03-22 本轮代码级复测通过：`dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~SourceGovernanceReadServiceTests"` 12/12 通过，新增覆盖 `**Considering the Request** 最终建议...` 这类标题式泄露；前端 `npm --prefix .\frontend run test:unit -- src/modules/admin/SourceGovernanceDeveloperMode.spec.js` 7/7 通过。Browser MCP 尚未在稳定运行态下重走 Developer Mode 页面，因此本 bug 暂不直接关闭。
	- 2026-03-23 代码级复测继续通过：前端 `SourceGovernanceDeveloperMode.spec.js` 已新增 `Interpreting the Data` / `Formulating the Response` 样本，确认列表摘要不再显示这些标题；后端隔离输出目录下的 `SourceGovernanceReadServiceTests` 也通过新增 `Simulating Information Retrieval -> Interpreting the Data -> Formulating the Response` 回归测试。
	- 2026-03-23 Browser MCP 新鲜会话复测部分通过：重新登录 `治理开发者模式` 并开启 Developer Mode 后，最新日志条目已显示 `返回内容包含中间推理，已脱敏。`，说明新产生的英文元推理输出不再直接暴露到 UI。
	- 2026-03-23 本轮追加前端安全摘要后，Browser MCP 再次复测：历史中英混杂非 JSON 条目在列表中已显示为 `返回内容不是结构化 JSON，已按安全摘要收口。`；点击进入详情弹层时，`返回摘要` 也保持该安全摘要，而合法 JSON 仍通过 `返回 JSON 美化视图` 展示。
	- 当前判断：Bug 5 本轮已通过，可关闭。

## Bug 9: LLM 设置可写但不可清空，空值保存没有实际生效

- 严重级别：高
- 复现步骤：
	1. 打开 `LLM 设置`，用 `admin / admin123` 登录。
	2. 在 `default` provider 中把 `Project` 改成任意非空值，例如 `write-test-20260322`，点击 `保存设置`。
	3. 再把同一字段清空，继续点击 `保存设置`。
	4. 命令行复测 `GET /api/admin/llm/settings/default`，或直接调用 `PUT /api/admin/llm/settings/default` 传入 `"project":""`。
- 实际结果：
	- UI 会提示 `已保存`，看起来像成功了。
	- 但后端读回仍然保留旧值 `write-test-20260322`，空值没有真正落库。
	- 直接走 admin `PUT` 接口传空字符串也同样无效，不是单纯前端问题。
	- 本轮只能通过手工修改运行时文件 `%LOCALAPPDATA%\SimplerJiangAiAgent\App_Data\llm-settings.json` 才把该值恢复为空。
- 预期结果：
	- 可选字段应支持被用户清空；UI 保存成功必须与后端持久化结果一致。
- 用户指导意见（来自人）：
	- 纳入 `GOAL-AGENT-002-P0` 设置持久化修复范围。
- 修复结果：
	- 已修复 `JsonFileLlmSettingsStore`，显式空字符串会真正覆盖旧值，`Project` / `Organization` / `BaseUrl` / `Model` / `SystemPrompt` 均可被清空。
- 复测结果：
	- 2026-03-22 本轮命令行复测未复现。
	- 通过 `/api/admin/login` 登录后，先将 `default` provider 的 `project` 写入 `retest-clear-20260322`，再用 `PUT /api/admin/llm/settings/default` 提交空字符串，读回结果已为空；最后已恢复原值。
	- 当前判断：Bug 9 本轮通过，可维持关闭状态。

## Bug 10: 股票助手发送消息时，消息保存接口间歇性 500，但前端表面仍显示成功

- 严重级别：高
- 复现步骤：
	1. 打开 `股票信息`，选择 `sh600000`。
	2. 在右侧 `股票助手` 点击 `新建对话`。
	3. 输入 `请用一句话说明浦发银行当前最大的风险点。` 并点击 `发送`。
	4. 观察 browser network / console 中的 `/api/stocks/chat/sessions/{sessionKey}/messages` 请求。
- 实际结果：
	- 页面可以看到用户提问和助手回答，似乎发送成功。
	- 但同一次发送过程中，`PUT /api/stocks/chat/sessions/{sessionKey}/messages` 会出现多个请求，且其中部分稳定返回 `500 Internal Server Error`。
	- 这意味着聊天写入链路并不干净，属于“前端有显示，但底层保存过程在报错”的假成功。
	- 本轮实测中，最新会话 `sh600000-1774152043985` 已出现至少两次该 500。
- 预期结果：
	- 一次聊天发送不应伴随保存接口 500。
	- 聊天历史保存应稳定、幂等，不能靠后续重试把表面现象掩盖过去。
- 用户指导意见（来自人）：
	- 纳入 `GOAL-AGENT-002-P0` 聊天假成功修复范围。
- 修复结果：
	- 已将 `ChatWindow.vue` 历史保存改为串行保存队列，停止流式输出期间的重复 PUT 风暴，避免前端表面成功但底层保存过程报错。
- 复测结果：
	- 2026-03-22 本轮 Browser MCP 未再复现 500。
	- 新会话 `sh600000-1774167817597` 创建、回读与多次 `PUT /api/stocks/chat/sessions/{sessionKey}/messages` 均返回 200，未见 `500 Internal Server Error`。
	- 当前判断：Bug 10 本轮通过，但仍可观察到一次发送触发多次 PUT；虽然目前都成功，不再按高优先级故障计。

## Bug 11: 股票助手返回内容仍泄露原始推理式标题，不是收口后的用户答案

- 严重级别：中
- 复现步骤：
	1. 打开 `股票信息`，选择 `sh600000`。
	2. 在 `股票助手` 输入任意简短问题并点击 `发送`。
	3. 观察助手返回文本首段。
- 实际结果：
	- 返回内容前缀直接出现 `Defining the Scope****Analyzing the Data` 这类原始推理式标题。
	- 这是面向用户的聊天面板，不是开发日志或调试面板，但依然暴露了模型中间表达痕迹。
	- 该问题与 `股票推荐`、`Developer Mode` 中发现的同类问题一致，说明收口规则没有覆盖聊天发送链路。
- 预期结果：
	- `股票助手` 应只输出整理后的最终答案，不应出现推理标题、链路标记或脏 Markdown。
- 用户指导意见（来自人）：
	- 纳入 `GOAL-AGENT-002-P0` 用户聊天输出脱敏收口。
- 修复结果：
	- 已在 `ChatWindow.vue` 对流式与最终助手内容统一做 reasoning scaffold 清洗，阻断 `Defining the Scope`、`Analyzing the Data` 等原始推理式标题进入用户聊天面板。
	- 2026-03-23 已把股票助手与股票推荐复用的清洗逻辑抽到 `frontend/src/utils/reasoningSanitizer.js`，并补齐 `Interpreting the Data`、`Assessing Risk Elements`、`Synthesizing Risk Insights` 等仍在真实输出里出现的标题，避免股票助手和推荐页再次各自漏词。
- 复测结果：
	- 2026-03-22 Browser MCP 仍可稳定复现，当前未通过。
	- 新会话回答首段直接出现 `Defining the Scope`、`Interpreting the Data`、`Assessing Risk Elements`、`Synthesizing Risk Insights`，随后才进入中文答案。
	- 当前判断：Bug 11 仍然存在，且与 Bug 4、Bug 5 属于同一类输出收口失效。
	- 2026-03-23 代码级复测通过：`npm --prefix .\frontend run test:unit -- src/modules/stocks/StockInfoTab.spec.js` 已确认股票助手流式返回即使包含 `Defining the Scope****Interpreting the Data**` 与 `Assessing Risk Elements` / `Synthesizing Risk Insights`，保存与恢复后的助手内容仍只保留 `风险提示保持仓位纪律`。
	- 2026-03-23 Browser MCP 新鲜会话复测通过：在 `股票信息 -> sh600000 -> 股票助手` 新建对话并发送 `今天这只股票的风险点是什么？` 后，最新助手回答直接从中文正文开始，未再出现 `Defining the Scope`、`Interpreting the Data`、`Assessing Risk Elements` 等推理式标题。
	- 当前判断：就“股票助手对用户直接暴露推理式标题”这一主症状，本轮 Browser MCP 已通过，可从同类泄露问题中移出。

## Bug 12: `start-all.bat` 启动链误判失败，桌面打包链未按 5119 健康起来

- 严重级别：高
- 复现步骤：
	1. 在运行态异常后执行 `c:\Users\kong\AiAgent\start-all.bat`。
	2. 等待脚本完成打包、启动 packaged desktop 并做健康检查。
- 实际结果：
	- 脚本输出 `Packaged desktop backend did not become healthy in time.` 并以非零退出。
	- 现有日志 `.automation/tmp/backend-run.log` 中出现 `Now listening on: http://localhost:5000`，与脚本固定等待的 `http://localhost:5119/api/health` 不一致。
	- 这会导致启动链即使后端进程已启动，也被误判为失败。
- 预期结果：
	- `start-all.bat` 应与实际 packaged backend 端口保持一致，或从真实监听地址探测健康，不应产生假失败。
- 用户指导意见（来自人）：
	- 待补充
- 修复结果：
	- 2026-03-23 已对桌面打包启动链做端口与超时对齐：`desktop/SimplerJiangAiAgent.Desktop/Form1.cs` 不再在 packaged backend 启动时静默切换到 `5119-5139` 其他端口，而是固定使用 `http://localhost:5119`；桌面内部等待后端健康的超时也从 20 秒提升到 90 秒。
	- 2026-03-23 `start-all.bat` 对 `http://localhost:5119/api/health` 的等待窗口同步从 60 秒提升到 90 秒，避免脚本和桌面宿主对“启动完成”的判断不一致。
- 复测结果：
	- 2026-03-23 已多次重走 `start-all.bat` 打包启动链，frontend build、backend publish、desktop publish 均成功，脚本最终稳定输出 `Packaged desktop started successfully.`。
	- 2026-03-23 当前判断：Bug 12 的“5119 健康检查与真实 packaged backend 端口/超时不一致导致误判失败”已修复，本轮可关闭。

## Bug 13: 治理开发者模式前端单测回归，日志展示文案与用例不一致

- 严重级别：中
- 复现步骤：
	1. 运行 `npm --prefix .\frontend run test:unit`。
	2. 观察 `SourceGovernanceDeveloperMode.spec.js` 结果。
- 实际结果：
	- 用例 `shows paired request and response content with prettified json` 失败。
	- 断言期望首段包含 `请求内容`，实际拿到的是 `请求摘要{"symbol":"600000",...}`。
- 预期结果：
	- 单测断言和页面实际展示应一致；若页面已改成“摘要”模式，用例应同步更新；若页面仍要求展示“内容”，实现应修正。
- 用户指导意见（来自人）：
	- 待补充
- 修复结果：
	- 已将 `SourceGovernanceDeveloperMode.spec.js` 中该用例的期望文案从 `请求内容 / 返回内容` 同步为页面真实展示的 `请求摘要 / 返回摘要`。
- 复测结果：
	- 2026-03-22 已通过 `npm --prefix .\frontend run test:unit -- src/modules/admin/SourceGovernanceDeveloperMode.spec.js` 复测，7/7 通过；该回归项已消失。