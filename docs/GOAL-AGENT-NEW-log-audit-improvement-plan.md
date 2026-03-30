# GOAL-AGENT-NEW 日志审计与改进计划

> **审计日期**: 2026-03-31（v2.0 更新于 2026-03-30）  
> **审计数据**: 开发日志 `App_Data/logs/llm-requests.txt` (38.9 MB) + 打包运行日志 `%LOCALAPPDATA%\SimplerJiangAiAgent\App_Data\logs\llm-requests.txt` (4.1 MB) + backend stdout log  
> **样本区间**: 2026-03-20 (旧流水线) + 2026-03-26 (新 MCP 流水线) + **2026-03-30 (生产打包环境)**  
> **文档版本**: v2.0

---

## 一、审计概览

### 1.1 数据统计

| 指标 | 3 月 20 日 (旧流水线) | 3 月 26 日 (新 MCP 流水线) | **3 月 30 日 (生产打包环境)** |
|------|----------------------|--------------------------|-------------------------------|
| AUDIT 条目总数 | 51 | 26 | **440** |
| 唯一 traceId | 26 | 13 | ~110 |
| request 条目 | 25 | 13 | ~220 |
| response 条目 | 22 | 12 | ~153 (仅 nano) + **0 (gemini, ⚠️)** |
| error 条目 | 4 | 1 | **3** |
| SOURCE-GOV 失败 | 0 | 2 | 1 (thinking bleed) |
| MCP 工具失败 | N/A | N/A | **31** |
| SQLite 锁失败 | N/A | N/A | **3 turns 完全失败** |
| 研究 Turn 数 | 0 | 0 | **5 (Turn 23-27)** |
| 成功完成 Turn | 0 | 0 | **2 (Turn 24, 25)** |
| 错误率 | 16% (4/25) | 7.7% (1/13) | 1.4% (3/~220) |
| 研究 Turn 失败率 | N/A | N/A | **60% (3/5)** |

> **关键发现**: 3 月 30 日是首次在生产打包环境下运行完整研究流水线的日期。日志来自 `%LOCALAPPDATA%\SimplerJiangAiAgent\` 而非源码树。

### 1.2 流水线架构对比

**旧流水线 (3 月 20 日)**:
- 固定 multi-agent 链: 个股资讯 Agent → 板块资讯 Agent → 个股分析 Agent → 走势分析 Agent → 指挥 Agent
- 所有分析角色使用 `gemini-3.1-flash-lite-preview-thinking-high`（13-17s/调用）
- 新闻清洗使用 `gpt-4.1-nano`（1-5s/调用）

**新 MCP 流水线 (3 月 26 日)**:
- Live LLM Gate Planner → Tool Calls → Role Executor
- Gate Planner 使用 `gemini-3.1-flash-lite-preview-thinking-high`
- 新闻清洗使用 `gpt-4.1-nano`
- 6 阶段固定 Pipeline: CompanyOverviewPreflight → AnalystTeam (并行) → ResearchDebate → TraderProposal → RiskDebate → PortfolioDecision

**生产环境 (3 月 30 日)**:
- 打包运行环境，SQLite 数据库，完整 6 阶段 Pipeline 首次真实运行
- 5 个研究 Turn（跨 2 只股票 sh605196、sh601100），2 个用户追问会话
- 大量 MCP 工具调用失败（31 次），导致 AnalystTeam 阶段严重降级
- 3 个 Turn 因 SQLite 表锁失败

### 1.3 模型使用分布

| 模型 | 3 月 20 日 | 3 月 26 日 | **3 月 30 日** | 典型耗时 | 用途 |
|------|-----------|-----------|---------------|---------|------|
| `gemini-3.1-flash-lite-preview-thinking-high` | 1 | 4 | **66** | 11-57s (avg 21s) | 全部研究角色 + 源发现 |
| `gpt-4.1-nano` | 24 | 9 | **153** | 1-5s | 财经新闻清洗 |

---

## 二、已知问题日志验证

### 问题 1: "单独重跑"机制按钮逻辑异常

**代码确认**: ✅ 已在代码中确认  
**日志证据**: ⚠️ 日志中未直接记录 UI 操作，但 `ResearchRunner` 的 `effectiveRerunFrom` 逻辑证实——  
- `rerunFromStage(stageIndex)` 前端发送 `continuationMode: 'PartialRerun'`，后端从 `stageIndex` 运行到流水线末尾
- 用户期望"单独重跑某个阶段"，实际执行"从该阶段跑到末尾"
- 对 Completed 状态的阶段也展示重跑按钮，容易误操作

**根因位置**:
- 前端: `useTradingWorkbench.js` → `rerunFromStage()` 总是发送 `PartialRerun`
- 后端: `ResearchRunner.cs` L94-106 → `effectiveRerunFrom` 只截断起点，不截断终点
- 前端: `TradingWorkbenchProgress.vue` → 按钮在 `Completed | Failed | Degraded` 状态均展示

**修复方案**:
1. 后端增加 `SingleStageRerun` 模式，仅重跑目标阶段
2. 前端只在 `Failed | Degraded` 状态展示重跑按钮
3. 或：保留 `PartialRerun` 语义但添加确认对话框说明"将从此阶段重跑至末尾"

### 问题 2: 追问清空页面内容

**代码确认**: ✅ 已在代码中确认  
**日志证据**: ⚠️ 日志无直接 UI 状态记录，但代码路径清晰  

**根因位置**:
- 前端: `useTradingWorkbench.js` → `loadSessionDetail()` 在新 Turn 创建后被调用
- `loadTurnReport(newTurnId)` 用新 Turn（无 reportBlocks）替换当前 report
- 结果：用户看到空白页面，需等新 Turn 完成后才能看到内容

**修复方案**:
1. 采用 chat-style 多轮布局：每个 Turn 的 report 作为独立区块累积展示
2. 新 Turn 运行期间保留上一轮 report 可见，新内容在上方追加
3. 或：新 Turn 开始时先展示 skeleton + 进度，不清除旧 report

### 问题 3: 最终回答不正面回答用户问题 + 分析流程固定

**代码确认**: ✅ 已在代码中确认  
**日志证据**: ✅ 3 月 20 日日志确认旧流水线固定 5 Agent 链执行；3 月 26 日确认新流水线 6 阶段也是固定执行

**根因分析**:

**(A) 流程固定**:
- `ResearchRunner.cs` L16-32 硬编码 6 阶段 Pipeline，Turn 0 总是全量执行
- 用户问"浦发银行明天涨还是跌？"和用户问"分析浦发银行基本面"走完全相同的 6 阶段
- 仅 Turn 1+ 的 follow-up 经 `ResearchFollowUpRoutingService` LLM 路由

**(B) 最终回答不答问题**:
- `PortfolioManagerTask` prompt 定义为"做出最终投资决策和评级"
- 输出要求为 JSON 结构（rating, executiveSummary, investmentThesis...）
- **缺失**: prompt 中未要求"必须首先正面回答用户的原始问题"
- 结果：用户问"明天涨还是跌？" → 得到的是通用投资报告而非针对性回答

**修复方案**:
1. 在 `PortfolioManagerTask` prompt 中增加强制规则: "你必须在 executiveSummary 的第一句话直接回答用户的原始问题"
2. Turn 0 增加意图分类预处理：简单问题直接回答 vs 深度分析问题走全流水线
3. Gate Planner 决策层增加流水线裁剪能力：根据用户意图决定跳过不必要的阶段

---

## 三、新发现问题

### 问题 4 [P0/Critical]: SOURCE-GOV JSON 解析失败（Thinking Model 输出非 JSON）

**日志证据**:
```
2026-03-26 18:11:17 [SOURCE-GOV] stage=discover error=JsonReaderException message='*' is an invalid start of a value
2026-03-26 18:23:00 [SOURCE-GOV] stage=discover error=JsonReaderException message='*' is an invalid start of a value
```

**根因**:
- Source Discovery 调用 `gemini-3.1-flash-lite-preview-thinking-high`（一个 thinking model）
- Thinking model 的输出以 `**My Thought Process:...` 散文开头，然后才输出 JSON
- 代码直接对 LLM 返回的全文做 `JsonDocument.Parse()`，遇到 `*` 字符立即抛异常
- **100% 失败率**: 3 月 26 日 2 次源发现调用全部失败

**影响**: 新闻源动态发现功能完全失效，系统退化到仅使用硬编码源列表

**修复方案**:
1. **方案 A（推荐）**: Source Discovery 不使用 thinking model，改用 `gpt-4.1-nano` 或非 thinking 版 gemini
2. **方案 B**: 在 JSON 解析前增加 thinking 输出剥离逻辑（regex 匹配 `[\[{]` 找到 JSON 起始位置）
3. **方案 C**: 在 LLM 调用时设置 `response_format: json_object` 强制 JSON 输出（需验证 thinking model 是否支持）

### 问题 5 [P1/High]: LLM 调用超时无断路器/退避策略

**日志证据**:
```
2026-03-20 13:59:07 traceId=52da468d error 100011ms  → 超时（100s）
2026-03-20 14:16:40 traceId=48652cdf error 19454ms   → 连接被远端强制关闭
2026-03-20 14:55:52 traceId=932cef0a error 20153ms   → 连接被远端强制关闭
2026-03-20 15:09:19 traceId=a2679ca7 error 20050ms   → 连接被远端强制关闭
2026-03-26 18:25:24 traceId=c318cd5f error 100018ms  → 超时（100s）
```

**统计**: 2 天合计 5 次 LLM 调用失败（2 次 100s 超时 + 3 次 ~20s 连接断开）

**根因**:
- `ResearchRoleExecutor` 有 LLM 重试机制（MaxLlmRetries=2，延迟 2s/5s），**但仅限单次角色执行内部**
- 无全局断路器：网关 `api.bltcy.ai` 连续 3 次请求失败后仍继续发送请求
- 3 月 26 日日志：`c318cd5f`（Gate Planner）100s 超时后，**2 秒内** `02537f7e` 立即重试且成功——说明超时是偶发性网关问题，但缺少对连续失败的保护

**影响**: 用户等待时间长（100s 超时才失败）+ 连续失败会扩散到整个 Pipeline

**修复方案**:
1. 实现轻量级断路器（Circuit Breaker）: 连续 N 次失败后短暂熔断，快速返回错误而非继续等待
2. Gate Planner 超时从 100s 降至 30-45s（thinking model 正常响应在 13-17s）
3. 新闻清洗超时保持 20-30s（正常响应在 1-5s）
4. 增加 exponential backoff（指数退避），当前 2s/5s 太激进

### 问题 6 [P1/High]: Gate Planner 超时后立即无退避重试

**日志证据**:
```
2026-03-26 18:25:24 c318cd5f error 100018ms   ← Gate Planner 超时
2026-03-26 18:25:26 02537f7e request           ← 仅隔 2 秒立即重试
2026-03-26 18:25:42 02537f7e response 15682ms  ← 重试成功
```

**根因**: Gate Planner 调用层面可能缺少显式退避，100s 超时失败后仅 2s 就发起新请求。虽然本次重试成功，但在网关过载场景下会加剧拥塞。

**修复方案**: Gate Planner 层面增加退避策略（建议首次 5s，第二次 15s）

### 问题 7 [P2/Medium]: 连接被远端强制关闭（TCP Reset）

**日志证据**（3 月 20 日 3 次）:
```
message=OpenAI 请求发送失败
inner=Unable to read data from the transport connection: 远程主机强迫关闭了一个现有的连接。
uri=https://api.bltcy.ai/v1/chat/completions
耗时: 19454ms / 20153ms / 20050ms
```

**分析**:
- 均发生在 `gpt-4.1-nano` 新闻清洗调用（正常 1-5s 的任务耗时 ~20s 后断开）
- 网关 `api.bltcy.ai` 可能存在 20s idle timeout 或连接池耗尽
- 与 100s 超时不同，这是 TCP RST（连接被对端重置），暗示网关过载或限流

**修复方案**:
1. HttpClient 启用连接池复用 + keep-alive 心跳
2. 考虑网关层面的 rate limiting 自适应（每分钟不超过 N 并发）
3. 新闻清洗可批量处理多条新闻减少请求次数

### 问题 8 [P2/Medium]: 新闻清洗标签质量问题

**日志证据**（3 月 26 日 `gpt-4.1-nano` 清洗结果采样）:
- 常规董事会公告被标记为 `"突发事件"`
- 出现非标准标签如 `"重要消息"` (`tags` 字段未受枚举约束)
- 分类粒度不统一：有的标记 `"利好信号"`，有的标记 `"中性"`

**根因**: 清洗 prompt 未定义标签枚举白名单，`gpt-4.1-nano` 自由发挥

**修复方案**:
1. 在新闻清洗 prompt 中定义严格的标签枚举: `["重大利好", "中性", "利空", "公司公告", "行业动态", "宏观政策"]`
2. 后端增加标签校验逻辑，未命中枚举的标签映射到最近似类别
3. 考虑 structured output (JSON mode) 强制返回约束格式

### 问题 9 [P3/Low]: Source Discovery 请求的 model 字段为空

**日志证据**:
```
2026-03-26 18:11:02 traceId=2db0ec29 stage=request model=
2026-03-26 18:22:47 traceId=bb2c8ab6 stage=request model=
```

**分析**: Source Discovery 调用没有在 AUDIT 日志中记录使用的模型名。从响应内容（thinking 输出）推断实际使用了 gemini thinking model，但日志缺失 model 字段说明日志记录逻辑不完整。

**修复方案**: 确保 LLM audit logger 在所有路径上正确记录 model 字段

### 问题 10 [P2/Medium]: 6 阶段固定 Pipeline 的 Token 和时间成本过高

**推算**:
- 每个角色 LLM 调用使用 thinking model: ~15s
- 6 阶段包含 15 个角色: CompanyOverviewAnalyst, 6x Analyst (并行), Bull/Bear/ResearchManager (Debate)×N轮, Trader, 3x RiskAnalyst, PortfolioManager
- 估算单次完整请求: 15+ 次 LLM 调用 × 13-17s ≈ **最少 60-90s**（并行优化后），**最坏情况 200s+**
- 每次调用的 prompt 包含完整 systemPrompt + 上游全部 artifacts，Token 消耗随阶段累积

**日志间接证据**: 
- 3 月 20 日旧流水线: 1 次完整请求的时间跨度 09:12-09:17 (5 分钟) 和 13:55-13:57 (2 分钟)
- 3 月 26 日新流水线仅触发了 Gate Planner + 新闻清洗阶段，未见完整 15 角色执行

**修复方案**: 见问题 3 的修复方案——根据用户意图动态裁剪 Pipeline

---

## 三-B、3 月 30 日生产环境新发现问题

### 问题 11 [P0/Critical]: SQLite 表锁导致研究 Turn 直接失败

**日志证据**:
```
09:36:00 Turn 23 failed: SQLite Error 6: 'database table is locked'
10:12:30 Turn 26 failed: SQLite Error 6: 'database table is locked'
10:14:55 Turn 27 failed: SQLite Error 6: 'database table is locked: ResearchRoleStates'
```

**统计**: 5 个 Turn 中 3 个（60%）因 SQLite 锁失败

**根因分析**:
- 生产环境使用 SQLite（非 SQL Server），单文件数据库不支持高并发写入
- AnalystTeam 阶段 6 个角色**并行**执行，同时向 `ResearchRoleStates` 表写入状态
- SQLite 的 WAL 模式允许并发读，但写入仍需排队，并行角色写入时互相阻塞
- 后台新闻清洗等其他写操作加剧锁竞争

**影响**: 研究功能在打包部署（SQLite）模式下基本不可用，60% 失败率

**修复方案**:
1. **方案 A（推荐）**: 研究 Pipeline 中所有 DB 写操作集中到 Turn 完成后批量提交，阶段执行期间仅在内存中维护状态
2. **方案 B**: 为 SQLite 连接配置 `PRAGMA busy_timeout = 5000;`，让被阻塞的写操作等待而非立即失败
3. **方案 C**: 并行角色的状态写操作通过串行化队列（Channel/SemaphoreSlim）避免并发冲突
4. **方案 D**: 生产环境改用 SQL Server Express 或 PostgreSQL

### 问题 12 [P0/Critical]: MCP 工具大面积失败导致 AnalystTeam 严重降级

**日志证据**:
```
Turn 24: AnalystTeam 6 outputs, 11 degraded
Turn 25: AnalystTeam 6 outputs, 15 degraded  
Turn 26: AnalystTeam 6 outputs, 11 degraded
```

**工具失败统计**:
| 工具 | 失败次数 | 影响角色 |
|------|---------|---------|
| StockNewsMcp | 11 | news_analyst, social_sentiment_analyst, product_analyst, shareholder_analyst |
| CompanyOverviewMcp | 9 | company_overview_analyst, fundamentals_analyst, product_analyst, shareholder_analyst |
| StockFundamentalsMcp | 3 | fundamentals_analyst |
| StockShareholderMcp | 3 | shareholder_analyst |
| StockProductMcp | 2 | product_analyst |
| StockKlineMcp | 1 | market_analyst |
| StockStrategyMcp | 1 | market_analyst |
| StockMinuteMcp | 1 | market_analyst |

**模式**: 每个角色的工具调用先尝试一次（attempt 1）→ 2 秒后重试（attempt 2）→ 失败标记 degraded

**根因推测**:
- MCP 工具内部访问东方财富等外部 API，可能因网络/频率限制导致批量失败
- 并行 6 角色同时发起 MCP 工具调用，瞬时并发请求过多触发限流
- 工具重试间隔为 ~2s，太短无法绕过频率限制

**影响**: 
- 几乎所有分析角色（除 market_analyst 的部分调用）无法获取数据
- 最终决策（PortfolioDecision）基于不完整的分析数据做出
- 用户收到的报告质量低于预期（Session 19 的 Buy 建议基于降级数据）

**修复方案**:
1. MCP 工具增加指数退避重试（2s → 5s → 15s），MaxRetries 从 2 提升到 3
2. 并行 AnalystTeam 增加最大并发限制（如 SemaphoreSlim(3)），避免 6 角色同时并发调用外部 API
3. MCP 工具层实现请求合并/缓存：多角色请求同一股票的 News/Overview 数据时共享一次实际 HTTP 调用
4. 工具失败时记录具体 HTTP 状态码和原因（当前日志仅显示 "tool X failed"，缺少根因信息）

### 问题 13 [P1/High]: Gemini 模型 LLM-AUDIT response 日志缺失

**日志证据**:
```
March 30 LLM-AUDIT entries:
- stage=request (gemini model): 66 条
- stage=response (gemini model): 0 条  ← 全部缺失!
- stage=response (gpt-4.1-nano): 153 条 ← 正常
```

**根因分析**:
- Gemini 模型通过 OpenAI 兼容网关（`api.bltcy.ai/v1`）调用，`providerType=openai`
- 但 Gemini 的 response 路径可能走了 native Gemini provider 的分支（`provider=gemini`），绕过了 LLM-AUDIT 的 response 记录逻辑
- 证据：首个调用 `a51c7efc` 的 request 同时被记录为 `provider=openai` 和 `provider=gemini`，但 response 仅在 `[LLM]` 级别出现，未到 `[LLM-AUDIT]`

**影响**: 
- 66 个 Gemini 研究调用的 elapsed time 未被审计系统记录
- 无法通过审计日志监控 Gemini 调用的成功率、延迟分布和 token 消耗
- 运维和调试时缺失关键遥测数据

**修复方案**:
1. 在 Gemini native provider 的 response 路径上补充 LLM-AUDIT stage=response 日志记录
2. 确保双 provider（OpenAI compat + Gemini native）的 AUDIT 日志格式一致

### 问题 14 [P2/Medium]: Google Translate API 连接超时阻塞键翻译

**日志证据**:
```
09:28:58 Failed to translate key 'productPortfolio' → SocketException 10060: 连接超时 (translate.googleapis.com:443)
09:29:20 Failed to translate key 'revenueContribution' → 同样超时
09:29:41 Failed to translate key 'growthTrend' → 同样超时
```

**根因**: `JsonKeyTranslationService` 在无代理或代理不通的情况下尝试调用 `translate.googleapis.com`，每次超时等待 21 秒

**影响**: 股票详情页的关键财务指标字段名显示为英文原始 key，用户体验下降

**修复方案**:
1. **方案 A（推荐）**: 预置常用财务字段的中文翻译映射表，不依赖在线翻译 API
2. **方案 B**: Google Translate 调用增加快速超时（3s），并缓存失败结果避免重复尝试

### 问题 15 [P2/Medium]: Gemini 响应延迟波动大

**统计**:
```
64 次成功的 Gemini 调用:
- 平均耗时: 21,385ms (21.4s)
- 最小耗时: 11,071ms (11.1s)
- 最大耗时: 56,694ms (56.7s)
- >30s 的调用: 7 次 (11%)
- >60s 的调用: 0 次
```

**对比 3/20 和 3/26 数据**: 之前典型 13-17s，现在平均 21.4s 且波动更大（max 57s）

**根因推测**: 
- Thinking model 的"思考"时间不可控，输入 token 越多思考越久
- 中间阶段（Debate、RiskDebate）的 prompt 包含前序所有 artifacts，token 累积导致延迟增长

**修复方案**: 
1. 中间阶段 prompt 仅引用上游关键结论而非全量 artifacts
2. 考虑为延迟敏感阶段（如 Trader、PortfolioDecision）使用非 thinking model

---

## 三-C、3 月 30 日研究会话质量分析

### 研究 Turn 执行概览

| Turn | Session | 股票 | 用户问题 | 状态 | 阶段进度 | 耗时 |
|------|---------|------|---------|------|---------|------|
| 23 | 18 | sh605196 | 为什么这只股票老是涨? | **Failed** | CompanyOverviewPreflight (SQLite锁) | 64s |
| 24 | 19 | sh605196 | 你如何看今天华通线大涨？ | **Closed** | ✅ 全 6 阶段完成 (11 degraded) | 220s |
| 25 | 20 | sh605196 | 那你建议我现在买入吗？(追问) | **Degraded** | ✅ 全 6 阶段完成 (15 degraded) | 424s |
| 26 | 21 | sh601100 | 这只股票目前适合买入吗？ | **Failed** | RiskDebate (SQLite锁) | 312s |
| 27 | 22 | sh601100 | (PartialRerun from stage 3) | **Failed** | CompanyOverviewPreflight (SQLite锁) | 114s |

### 决策质量评估

**Session 19 → 20（sh605196 华通线缆追问链）**:
- Turn 24 决策: **Buy**（rating=Buy, confidence=高）
- Turn 25 追问后: **Hold**（从 Buy 下调到观望）
- 评估: ✅ 追问导致决策修正是合理行为——第二轮考虑了更多风险因素（估值泡沫 PE72、RSI超买、市场大盘流出）
- 问题: 两次决策基于严重降级的数据（11/15 degraded roles），决策可信度存疑

**Session 21-22（sh601100）**:
- Turn 26 走到 RiskDebate 后 SQLite 锁失败
- Turn 27 尝试 PartialRerun 从 Stage 3 起跑，但在 CompanyOverviewPreflight 就锁失败
- 评估: ❌ 用户完全无法获得结果

---

## 四、问题优先级汇总

| 优先级 | 问题 # | 标题 | 影响范围 | 预估复杂度 | 来源日期 |
|--------|-------|------|---------|-----------|---------|
| **P0** | 11 | SQLite 表锁致 60% Turn 失败 | 生产环境研究不可用 | 中 | 3/30 |
| **P0** | 12 | MCP 工具大面积失败 | 分析数据严重不完整 | 中 | 3/30 |
| **P0** | 4 | SOURCE-GOV JSON 解析失败 | 新闻源发现完全失效 | 低 | 3/26 |
| **P0** | 3-B | PortfolioManager 不回答用户问题 | 用户核心体验 | 低 | 3/20 |
| **P1** | 13 | Gemini AUDIT response 日志缺失 | 可观测性盲区 | 低 | 3/30 |
| **P1** | 2 | 追问清空页面 | 多轮对话体验 | 中 | 3/20 |
| **P1** | 5 | LLM 调用无断路器 | 系统稳定性 | 中 | 3/20 |
| **P1** | 6 | Gate Planner 无退避重试 | 网关过载保护 | 低 | 3/26 |
| **P2** | 14 | Google Translate 连接超时 | 股票详情翻译 | 低 | 3/30 |
| **P2** | 15 | Gemini 响应延迟波动大 | 用户等待时间 | 中 | 3/30 |
| **P2** | 1 | 单独重跑逻辑错误 | 用户操作准确性 | 中 | 3/20 |
| **P2** | 7 | TCP 连接被远端重置 | 请求可靠性 | 中 | 3/20 |
| **P2** | 8 | 新闻清洗标签质量 | 分析准确性 | 低 | 3/26 |
| **P2** | 10 | Pipeline Token 成本过高 | 性能和费用 | 高 | 3/20 |
| **P3** | 3-A | Turn 0 流程固定无意图路由 | 灵活性 | 高 | 3/20 |
| **P3** | 9 | Source Discovery model 字段为空 | 可观测性 | 低 | 3/26 |

---

## 五、改进计划

### Phase 1: 紧急修复（P0，1-2 天）

#### Task 1.1: SQLite 并发写入锁修复 ★ 3/30 新增
- 文件: `AppDbContext` 配置 / `ResearchRoleExecutor.cs`
- 问题: AnalystTeam 6 角色并行写 `ResearchRoleStates`，触发 "database table is locked"，3/5 Turn 崩溃
- 方案 A: 配置 `busy_timeout=5000`（SQLite pragma），让并发写自动等待而非立即失败
- 方案 B: 将 AnalystTeam 角色状态写入改为批量单次提交（收集所有角色结果后统一 SaveChanges）
- 方案 C: 使用 WAL 模式 + busy_timeout 组合提升并发写吞吐
- 验证: 连续运行 5 个 Turn，0 次锁失败

#### Task 1.2: MCP 工具可靠性修复 ★ 3/30 新增
- 文件: MCP 工具调用层（StockNewsMcp, CompanyOverviewMcp 等）
- 问题: 31 次工具失败（StockNewsMcp 11 次、CompanyOverviewMcp 9 次），导致 AnalystTeam 11-15 个角色降级
- 方案: (a) 增加 MCP 调用超时和重试配置 (b) 对外部 API 增加连接池预热 (c) 降级时保留缓存数据而非返回空
- 验证: MCP 工具成功率 > 90%

#### Task 1.3: 修复 SOURCE-GOV JSON 解析
- 文件: Source Discovery 相关 service
- 方案: 剥离 thinking model 的 prose 前缀，或换用非 thinking model
- 验证: Source Discovery 调用成功率 100%

#### Task 1.4: PortfolioManager Prompt 增加用户问题直答规则
- 文件: `TradingWorkbenchPromptTemplates.cs` → `PortfolioManagerTask`
- 修改: 在 prompt 中增加规则——"在 executiveSummary 的第一句话必须直接回答用户原始提问"
- 验证: 用不同类型的用户问题测试 PortfolioManager 输出

### Phase 2: 核心体验与可观测性修复（P1，3-5 天）

#### Task 2.1: 追问不清空页面
- 前端重构: `useTradingWorkbench.js` — 多轮 report 累积展示
- 方案: 保留前一轮 report，新 Turn 的进度和 report 在页面上方追加
- 验证: 多轮追问场景下所有 Turn 的 report 均可见

#### Task 2.2: LLM 断路器和退避
- 文件: LLM 基础设施层（ILlmService 实现）
- 新增: 轻量级 Circuit Breaker（连续 3 次失败熔断 30s）
- 修改: Gate Planner 超时 100s → 40s，新闻清洗超时 → 15s
- 新增: Exponential backoff（2s → 5s → 15s）
- 验证: 模拟网关故障场景验证熔断行为

#### Task 2.3: Gemini AUDIT response 日志补全 ★ 3/30 新增
- 文件: `LlmAuditLogger.cs` 或 OpenAI provider 层
- 问题: Gemini 走 OpenAI 兼容路由时 66 次请求仅记录 request，0 条 response AUDIT
- 方案: 在 OpenAI provider 的 response 路径补充 `stage=response` AUDIT 日志
- 验证: 每条 Gemini request AUDIT 都有对应的 response AUDIT

### Phase 3: 质量与效率提升（P2，5-7 天）

#### Task 3.1: 单独重跑逻辑修正
- 后端: `ResearchRunner.cs` 增加 `SingleStageRerun` continuation mode
- 前端: 重跑按钮仅在 `Failed | Degraded` 展示，或给 Completed 按钮添加确认框
- 验证: 重跑单个阶段不影响其他阶段

#### Task 3.2: 新闻清洗标签白名单
- 修改: 新闻清洗 prompt 增加标签枚举约束
- 后端: 增加标签映射/校验逻辑
- 验证: 清洗结果标签全部命中白名单

#### Task 3.3: HttpClient 连接稳定性
- 配置: HttpClient 连接池、keep-alive、自适应并发限制
- 验证: 20s 连接断开频率下降

#### Task 3.4: Google Translate 超时降级 ★ 3/30 新增
- 文件: 翻译服务层
- 问题: `translate.googleapis.com:443` SocketException 10060 阻塞财务字段翻译
- 方案: (a) 翻译超时 5s 后降级为原文 (b) 缓存已翻译结果 (c) 备选翻译 API
- 验证: 翻译超时不阻塞主流程

#### Task 3.5: Gemini 延迟监控与优化 ★ 3/30 新增
- 问题: 均值 21.4s（3/26 为 13-17s），最大 56.7s，11% 超 30s
- 方案: (a) 请求添加延迟指标追踪 (b) 超时 30s 的请求自动重试到备用模型 (c) 考虑模型降级策略
- 验证: P95 延迟 < 30s

### Phase 4: 架构优化（P3，长期）

#### Task 4.1: Turn 0 意图路由
- 新增: Turn 0 提交前的意图分类（简单问答 / 深度分析 / 特定阶段分析）
- 根据意图裁剪 Pipeline：简单问答跳过 Debate+Risk，仅执行 Analyst+PortfolioDecision
- 验证: 不同类型问题的 Pipeline 执行阶段数和总时间

#### Task 4.2: Pipeline 动态裁剪
- Gate Planner 输出中包含"推荐执行阶段列表"
- ResearchRunner 根据 Gate Planner 建议跳过非必要阶段
- 验证: Token 消耗和响应时间显著降低

---

## 六、日志样本附录

### 3 月 26 日完整 LLM 调用时间线

```
18:11:02  2db0ec29  request  (model=空)     → Source Discovery
18:11:17  2db0ec29  response 15878ms        → 返回 thinking prose（非 JSON）
          [SOURCE-GOV] discover error=JsonReaderException '*' is invalid

18:11:47  4a13f3ea  request  gemini-thinking → Gate Planner (第1次)
18:12:01  4a13f3ea  response 13596ms        → toolCalls JSON 成功

18:12:10  79fae4f3  request  gpt-4.1-nano   → 新闻清洗 #1
18:12:14  79fae4f3  response 3944ms
18:12:14  874e5d76  request  gpt-4.1-nano   → 新闻清洗 #2
18:12:17  874e5d76  response 2720ms
18:12:20  5d3d87f1  request  gpt-4.1-nano   → 新闻清洗 #3
18:12:24  5d3d87f1  response 3668ms
18:12:24  50bb24c4  request  gpt-4.1-nano   → 新闻清洗 #4
18:12:29  50bb24c4  response 5051ms
18:12:29  e3f136f0  request  gpt-4.1-nano   → 新闻清洗 #5
18:12:32  e3f136f0  response 2938ms

18:14:20  34a032ba  request  gemini-thinking → Gate Planner (第2次)
18:14:34  34a032ba  response 14660ms

18:14:41  2a3874b7  request  gpt-4.1-nano   → 新闻清洗 #6
18:14:42  2a3874b7  response 1590ms
18:14:46  8e03369d  request  gpt-4.1-nano   → 新闻清洗 #7
18:14:47  8e03369d  response 1510ms

--- 8 分钟间隔 ---

18:22:47  bb2c8ab6  request  (model=空)     → Source Discovery (第2次)
18:23:00  bb2c8ab6  response 13436ms
          [SOURCE-GOV] discover error=JsonReaderException '*' is invalid

18:23:44  c318cd5f  request  gemini-thinking → Gate Planner (第3次)
18:25:24  c318cd5f  error    100018ms       → ★ 超时!

18:25:26  02537f7e  request  gemini-thinking → Gate Planner (第4次, 立即重试)
18:25:42  02537f7e  response 15682ms        → 成功
```

### 关键错误模式摘要

| 错误类型 | 3/20 次数 | 3/26 次数 | 3/30 次数 | 最大耗时 | 根因 |
|---------|----------|----------|----------|---------|------|
| SQLite 表锁 | 0 | 0 | 3 | N/A | 并行写 ResearchRoleStates |
| MCP 工具失败 | 0 | 0 | 31 | N/A | 外部 API 超时/不可达 |
| LLM 超时 (100s) | 1 | 1 | 2 | 100018ms | 网关偶发超时 |
| TCP 连接重置 (~20s) | 3 | 0 | 1 | 20153ms | 网关连接池/限流 |
| SOURCE-GOV JSON 解析 | 0 | 2 | 0 | N/A | Thinking model 输出非 JSON |
| Google Translate 超时 | 0 | 0 | 1+ | N/A | translate.googleapis.com 不可达 |

---

## 七、中文摘要

本次审计基于 2026 年 3 月 20 日、26 日、30 日的 LLM 请求日志与生产环境数据库，验证了 3 个已知问题并发现了 12 个新问题（共计 15 个）：

**已知问题全部确认**:
1. "单独重跑"实为"从此阶段重跑到末尾"，按钮在 Completed 状态也展示
2. 追问创建新 Turn 时清空当前页面内容
3. PortfolioManager 不正面回答用户问题 + Pipeline 对 Turn 0 固定

**3/20 + 3/26 发现（7 个问题）**:
4. **[P0]** SOURCE-GOV 源发现 100% 失败——Thinking model 输出散文前缀导致 JSON 解析失败
5. **[P1]** LLM 调用无全局断路器，100s 超时才失败，用户等待过久
6. **[P1]** Gate Planner 超时后 2 秒立即重试，无退避策略
7. **[P2]** TCP 连接被远端重置（3 次 ~20s 断开）
8. **[P2]** 新闻清洗标签质量不可控（无枚举约束）
9. **[P3]** Source Discovery 日志 model 字段为空
10. **[P2]** 全量 Pipeline 成本过高（15+ 角色 LLM 调用）

**3/30 生产环境新发现（5 个问题）★**:
11. **[P0]** SQLite 表锁导致 60% 研究 Turn 失败——AnalystTeam 并行写入 ResearchRoleStates 触发 "database table is locked"，5 个 Turn 中 3 个崩溃
12. **[P0]** MCP 工具大面积失败——31 次工具调用失败，StockNewsMcp (11 次) 和 CompanyOverviewMcp (9 次) 最严重，导致 AnalystTeam 11-15 个角色降级
13. **[P1]** Gemini AUDIT response 日志缺失——66 次 Gemini 请求只有 request 没有 response AUDIT 记录，可观测性存在盲区
14. **[P2]** Google Translate API 连接超时——SocketException 10060 阻塞财务字段翻译
15. **[P2]** Gemini 响应延迟波动大——均值 21.4s（历史 13-17s），最大 56.7s，11% 超过 30s

**改进优先级**: P0 紧急修复（1-2 天，SQLite 锁 + MCP 可靠性 + SOURCE-GOV + PM 直答） → P1 核心体验与可观测性（3-5 天） → P2 质量提升（5-7 天） → P3 架构优化（长期）

**3/30 关键结论**: 生产环境的首要瓶颈已从 LLM 超时转移到 **SQLite 并发锁** 和 **MCP 工具可靠性**。即使 LLM 调用全部成功，角色因数据源失败而大面积降级也会导致分析质量严重下降。
