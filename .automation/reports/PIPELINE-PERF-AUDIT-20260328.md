# LLM / MCP Pipeline Performance Audit — 2026-03-28

**数据来源**: `App_Data/logs/llm-requests.txt` 尾部 3/26 18:11—18:25 会话  
**分析范围**: Live Gate 全链路、Research Pipeline 、Source Governance、News AI Enrichment

---

## 一、会话时间线还原 (3/26 18:11–18:25, ~14m40s)

| 时间段 | 耗时 | 操作 | 模型 | 状态 |
|--------|------|------|------|------|
| 18:11:02→18:11:17 | 15.9s | SOURCE-GOV discover | gemini-thinking-high + GoogleSearch | **失败** JsonReaderException |
| 18:11:47→18:12:01 | 13.6s | Live Gate Planner #1 | gemini-thinking-high | OK |
| 18:12:10→18:12:14 | 3.9s | News Cleaner batch 1 | gpt-4.1-nano | OK |
| 18:12:14→18:12:17 | 2.7s | News Cleaner batch 2 | gpt-4.1-nano | OK |
| 18:12:20→18:12:24 | 3.7s | News Cleaner batch 3 | gpt-4.1-nano | OK |
| 18:12:24→18:12:29 | 5.1s | News Cleaner batch 4 | gpt-4.1-nano | OK |
| 18:12:29→18:12:32 | 2.9s | News Cleaner batch 5 | gpt-4.1-nano | OK |
| 18:12:32→18:14:20 | **1m48s** | MCP Tool 执行 (无 LLM 日志) | — | 瓶颈 |
| 18:14:20→18:14:34 | 14.7s | Live Gate Planner #2 | gemini-thinking-high | OK (疑似重复) |
| 18:14:41→18:14:42 | 1.6s | News Cleaner batch 6 | gpt-4.1-nano | OK |
| 18:14:46→18:14:47 | 1.5s | News Cleaner batch 7 | gpt-4.1-nano | OK |
| 18:14:47→18:22:47 | **8m00s** | 等待/MCP执行 (无日志) | — | 长空白 |
| 18:22:47→18:23:00 | 13.4s | SOURCE-GOV discover #2 | gemini-thinking-high + GoogleSearch | **失败** JsonReaderException |
| 18:23:44→18:25:24 | **100.0s** | Live Gate Planner #3 | gemini-thinking-high | **超时** 100s |
| 18:25:26→18:25:42 | 15.7s | Live Gate Planner #4 (重试) | gemini-thinking-high | OK |

---

## 二、发现的问题清单

### 🔴 P0 — 严重性能问题

#### P0-1: Live Gate MCP 工具按顺序串行执行
- **位置**: `StockCopilotLiveGateService.cs` → `ExecuteApprovedToolsAsync()`
- **现状**: `foreach (var approvedCall in approvedCalls) { await ExecuteApprovedToolAsync(...) }` — 所有 approved 工具调用 **逐个串行等待**
- **影响**: planner 计划4个工具 (StockKlineMcp, MarketContextMcp, StockNewsMcp, CompanyOverviewMcp)，每个可能 2-8s，串行总计 8-32s
- **建议**: 将无依赖的 local_required 工具改为 `Task.WhenAll` 并行执行，仅 external_gated 保持串行（需先有 local 结果）

#### P0-2: Research Pipeline 角色内 MCP 工具串行执行
- **位置**: `ResearchRoleExecutor.cs` → `ExecuteRoleAsync()` Phase 1
- **现状**: `foreach (var toolName in contract.PreferredMcpSequence)` — 每个 analyst 角色内的工具也是串行调度
- **影响**: market_analyst 有 4 个工具 (MarketContextMcp, StockKlineMcp, StockMinuteMcp, StockStrategyMcp)，串行 = 4×工具耗时
- **说明**: 虽然 `RunParallelAsync` 已实现角色级并行 (`Task.WhenAll`)，但角色内部的工具仍然串行，是主要瓶颈
- **建议**: 角色内工具也改为 `Task.WhenAll` 并行（它们互不依赖）

### 🟠 P1 — 重要问题

#### P1-1: SOURCE-GOV discover 每次都失败 (JsonReaderException)
- **位置**: `SourceGovernanceService.cs` → `DiscoverCandidatesFromLlmAsync()`
- **现状**: 用 `gemini-3.1-flash-lite-preview-thinking-high` + `useInternet=True`，模型返回 Markdown thinking trace (`**My Thought Process:...`) 而非 JSON
- **影响**: 每次白白浪费 ~15s LLM 调用 + token 成本，且永远不会成功
- **原因**: thinking-high 模型会先输出思考过程，`ParseLlmCandidates` 遇到 `*` 开头直接 JSON 解析失败
- **建议**: 
  - (A) 换用非 thinking 模型如 `gpt-4.1-nano` 或 `gemini-2.0-flash`
  - (B) 在 `ParseLlmCandidates` 中先剥离 Markdown/thinking 前缀，定位首个 `[` 字符

#### P1-2: Live Gate Planner 使用 thinking-high 模型导致延迟过高
- **位置**: `StockCopilotLiveGateService.cs` → `RunAsync()` 中的 `_llmService.ChatAsync(...)`
- **现状**: 每次 planner 调用耗时 13-16s (正常) 或 100s+ (超时)
- **影响**: planner 只需返回一个 JSON 工具计划，thinking 模型的思考链是纯浪费
- **建议**: planner 使用快速模型 (如 `gpt-4.1-mini` 或 `gemini-2.0-flash`)，只在 analyst/researcher LLM 推理阶段用 thinking 模型

#### P1-3: 100 秒 LLM 超时后无重试策略
- **位置**: 日志 traceId=c318cd5f — `elapsedMs=100018` timeout
- **现状**: planner 超时后系统自动发起了重试 (traceId=02537f7e)，但这是在 live gate 外层还是内层？
- **建议**: 确认重试逻辑位置和次数，添加指数退避；对 planner 单独设置更短的超时 (如 30s)

#### P1-4: News Cleaner 批次串行发送
- **位置**: `LocalFactAiEnrichmentService.cs` → `ProcessBatchesAsync()`
- **现状**: 7 个 batch 依次发送给 gpt-4.1-nano，每 batch 2-5s，总计 ~22s
- **影响**: 所有 batch 互不依赖，可并行
- **建议**: 使用 `Task.WhenAll` + 并发限制 (SemaphoreSlim, 3-5 并发) 并行执行批次

### 🟡 P2 — 中等问题

#### P2-1: 重复的 Live Gate Planner 调用
- **现状**: 同一会话中 planner 被调用了 4 次 (18:11:47, 18:14:20, 18:23:44, 18:25:26)
- **影响**: 每次调用浪费 14-16s + token 成本
- **可能原因**: 前端重复提交、定时器重触发、或 Source-Gov + Live Gate 被不同后台任务触发
- **建议**: 检查触发逻辑，添加去重 (debounce/锁)，避免相同 symbol+question 重复规划

#### P2-2: SOURCE-GOV discover 在每次 live gate 流程中被触发
- **现状**: 每次请求都会走 `SourceGovernanceService.DiscoverCandidatesFromLlmAsync()`，16s 白等
- **建议**: SOURCE-GOV discover 不应在用户请求路径上同步执行，应为后台定时任务 (每小时/每天一次)

#### P2-3: MCP 工具执行期间无 LLM 日志记录
- **现状**: 18:12:32→18:14:20 有 1m48s 无日志的空白段
- **影响**: 无法判断 MCP 工具 (KLine, MarketContext, News, CompanyOverview) 各自耗时多少
- **建议**: 在 `McpToolGateway` 或 `ExecuteApprovedToolAsync` 中添加每个工具的开始/完成/耗时日志

#### P2-4: Planner Prompt 过大 (6976 chars)
- **现状**: `promptChars=6976` — 包含完整的角色清单、MCP 序列、fallback 规则等
- **影响**: 大 prompt = 更高 token 成本 + 更慢的首 token 时间
- **建议**: 精简 planner prompt，将静态元数据放到 system message 或 cache/prefix 中

#### P2-5: 每个 LLM 请求产生3条重复日志行 (request/prompt/request-http)
- **现状**: 每个 trace 写入 3 行 request 日志 + 2 行 response 日志 = 5 行/次
- **影响**: llm-requests.txt 已 40MB，增长过快
- **建议**: 合并为单行 request 和单行 response，减少日志膨胀

### 🟢 P3 — 改善建议

#### P3-1: FetchSymbolDataBundleAsync 并行化未在 Live Gate 路径使用
- **现状**: `FetchSymbolDataBundleAsync` 已在 `StockCopilotMcpService` 中实现 `Task.WhenAll`，但 Live Gate 调用路径是直接走 `McpToolGateway` 的独立方法
- **影响**: Live Gate 的工具调用无法享受到之前实现的数据并行化
- **建议**: Live Gate 路径也利用 bundle 并行化，或在 `ExecuteApprovedToolsAsync` 层面实现并行

#### P3-2: 缺少 LLM 调用缓存机制
- **建议**: 对于重复的 planner 请求 (相同 symbol + question)，在短时间窗口内 (~5min) 缓存 planner 结果

#### P3-3: News Cleaner batch size 可优化
- **现状**: `AiBatchSize` 配置为 5-20 条/batch，7 个 batch 说明有 35-140 条待处理
- **建议**: gpt-4.1-nano 可以处理更大 batch (20-30条)，减少请求次数

---

## 三、优化优先级排序

| 优先级 | ID | 预估收益 | 实现复杂度 |
|--------|-----|----------|-----------|
| 🔴 P0-1 | Live Gate MCP 并行化 | ↓60-75% 工具耗时 | 中 |
| 🔴 P0-2 | Role 内 MCP 并行化 | ↓50-70% 角色内工具耗时 | 中 |
| 🟠 P1-1 | SOURCE-GOV 修复 JSON 解析 | ↓15s 浪费 | 低 |
| 🟠 P1-2 | Planner 换快速模型 | ↓10-13s / 次 | 低 |
| 🟠 P1-4 | News Cleaner 批次并行 | ↓15-18s | 低 |
| 🟡 P2-1 | 去重重复 planner 调用 | ↓30-50s 浪费 | 中 |
| 🟡 P2-2 | SOURCE-GOV 移出请求路径 | ↓16s / 请求 | 低 |
| 🟡 P2-3 | MCP 工具执行日志 | 可观测性 | 低 |
| 🟡 P2-4 | 精简 Planner Prompt | ↓ token 成本 + ↓首 token 延迟 | 中 |
| 🟡 P2-5 | 合并重复日志行 | ↓日志膨胀 | 低 |
| 🟢 P3-1 | Live Gate 用 bundle 并行 | 与 P0-1 合并 | — |
| 🟢 P3-2 | Planner 缓存 | ↓重复调用 | 中 |
| 🟢 P3-3 | 增大 News batch size | ↓请求次数 | 低 |

---

## 四、预期优化效果

**当前**: 一次 live gate 请求总耗时 ~3-14 分钟（含重试和超时）  
**优化后 (P0+P1)**:
- Planner: 2-4s (换快速模型) vs 14-16s
- MCP 工具执行: 3-8s (并行) vs 8-32s (串行)
- News Cleaner: 5-8s (并行batch) vs 22s (串行)
- SOURCE-GOV: 0s (移出请求路径) vs 16s
- **预估总耗时: 10-20s** (↓80-90%)

---

*Report generated: 2026-03-28*
