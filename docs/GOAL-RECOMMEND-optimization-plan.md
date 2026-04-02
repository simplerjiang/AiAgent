# GOAL-RECOMMEND 推荐系统优化计划书

> **版本**: v1.0  
> **日期**: 2026-04-01  
> **评估来源**: PM Agent 代码审查 + Test Agent 技术评估 + User Representative Agent 用户验收 + LLM日志分析 + 数据库审查  
> **当前状态**: ❌ 未通过验收（用户评分 3.0/10，技术评分 7.0/10）

---

## 一、综合评估总结

### 1.1 三方评估结论

| 评估方 | 结论 | 核心问题 |
|--------|------|----------|
| **Test Agent** (技术) | 7.0/10 — 架构清晰但关键路径缺测试 | DbContext 并发 Bug、SSE 断连丢事件、EventBus 内存泄漏 |
| **User Representative** (用户) | 3.0/10 — 拒绝验收 | 零个完整推荐成功、推荐报告永远为空、中文内容乱码 |
| **PM Agent** (LLM日志) | 核心流水线阻断 | LLM 100秒超时、无重试机制、工具失败不透明 |

### 1.2 关键数据

- **推荐完成率**: 0%（10个会话中0个成功到达 FinalDecision）
- **角色执行失败率**: >70%（主要集中在 StockPicking 阶段的 LLM 超时）
- **数据库表状态**: 推荐相关表尚未在本地创建（Schema Initializer 未触发）
- **前端测试**: 144 pass / 0 fail（但覆盖率不足，核心组件缺专项测试）
- **后端推荐测试**: 49 pass / 0 fail（但缺 Runner 完整流水线测试）

---

## 二、发现的问题清单

### 2.1 P0 — 阻断性问题（必须立即修复）

#### P0-1: DbContext 并发线程安全
- **位置**: `RecommendationRunner.ExecuteParallelAsync`
- **问题**: MarketScan（3角色并行）和 StockPicking（2角色并行）使用 `Task.WhenAll` 并发执行，多个 Task 共享同一个 `AppDbContext` 实例并发调用 `SaveChangesAsync()`
- **影响**: `InvalidOperationException`（"A second operation was started on this context instance before a previous operation completed"），直接导致 Turn 崩溃
- **修复方向**: 为每个并行角色创建独立的 DI scope + DbContext，或使用 `IDbContextFactory<AppDbContext>`

#### P0-2: LLM 请求超时无重试
- **位置**: `LlmService.ChatAsync` → `RecommendationRoleExecutor.ExecuteAsync`
- **问题**: LLM Provider（api.bltcy.ai 网关）100秒硬超时后立即抛出 `InvalidOperationException`，无任何重试逻辑
- **日志证据**: `traceId=52da468d... stage=error elapsedMs=100011 type=InvalidOperationException message="OpenAI 请求超时"`
- **影响**: 单个 LLM 超时 → 角色失败 → 若同阶段全部超时 → 流水线中止
- **修复方向**: 在 RoleExecutor 层添加指数退避重试（最多2次），或在 LlmService 层添加 Polly 重试策略

#### P0-3: 推荐报告数据源查找逻辑错误
- **位置**: `RecommendReportCard.vue` → `latestTurn` computed
- **问题**: 只查看最后一个 Turn 的 stageSnapshots。当用户追问后产生的 DirectAnswer Turn 没有 stageSnapshots，导致报告永远显示"尚未生成"
- **影响**: 即使之前有成功的推荐数据，追问后报告也会消失
- **修复方向**: 遍历所有 Turns，从最近向前搜索包含 FinalDecision + `recommend_director` 输出的 stageSnapshot

#### P0-4: 中文内容编码损坏
- **位置**: 后端 API 返回的 `outputContentJson` / `summary` 字段
- **问题**: 用户代表报告部分会话数据中中文显示为乱码（如"鍒涙柊鑽"而非"创新药"）
- **影响**: 辩论过程对中文用户完全不可读
- **修复方向**: 检查 JsonSerializer 的 Encoder 配置，确认数据库存储和读取均使用 NVARCHAR + UTF-8

### 2.2 P1 — 严重影响体验

#### P1-1: SSE 断连后事件丢失
- **位置**: SSE 端点 `/api/recommend/sessions/{id}/events` + `RecommendEventBus.Drain()`
- **问题**: `Drain()` 会从队列移除事件，客户端断连重连后已消费的事件不会再发送
- **前端影响**: 重连时 `sseEvents` 被清空，事件链断裂
- **修复方向**: SSE 端点改用 `Snapshot()` + 客户端游标（lastTimestamp），或实现 SSE `id:` 字段 + offset 恢复

#### P1-2: EventBus 内存泄漏
- **位置**: `RecommendEventBus._history` 字典
- **问题**: `Cleanup(turnId)` 仅在 SSE 端点收到终端事件时调用。无 SSE 客户端连接的推荐、客户端提前断开的推荐，`_history` 永远不被清理
- **修复方向**: 在 `PersistFeedItemsAsync` 完成后调用 `Cleanup()`，或设置 TTL 定时清理（如15分钟）

#### P1-3: 僵尸 Running 会话
- **位置**: 缺乏超时自动终结机制
- **问题**: 10个会话中至少5个永远卡在 Running 状态，无法自动转为 Failed/TimedOut
- **影响**: 历史列表充斥无法区分的 Running 记录
- **修复方向**: 后台定时任务扫描 Running>10min 的 Turn，标记为 TimedOut

#### P1-4: 缺少核心路径测试
- **测试缺失**: 
  - `RecommendationRunner.RunTurnAsync` 完整流水线集成测试
  - `ExecuteParallelAsync` 并发行为
  - `ExecuteDebateAsync` 多轮辩论 + CONSENSUS_REACHED 退出
  - `RunPartialTurnAsync` 部分重跑
  - `RecommendProgress.vue` + `RecommendReportCard.vue` 专项前端测试

#### P1-5: 进度面板不显示失败原因
- **位置**: `RecommendProgress.vue`
- **问题**: 角色失败只有 ❌ 图标，看不到具体错误信息（如"OpenAI请求超时"）
- **修复方向**: role state 的 errorMessage 展示为 tooltip 或可展开详情

### 2.3 P2 — 用户体验优化

#### P2-1: 辩论过程信息过载
- **问题**: 13角色 × 多次工具调用 = 50+ 条消息，缺乏过滤机制
- **建议**: 增加"只看结论"过滤按钮，折叠 lifecycle/tool 事件，只展示 RoleSummaryReady

#### P2-2: 历史列表缺少摘要
- **问题**: 只显示日期+状态，无 userPrompt 预览
- **建议**: 显示 userPrompt 前20字 + turn 数量

#### P2-3: 快捷追问按钮静态
- **问题**: "板块深挖"等固定文案无法匹配实际推荐上下文
- **建议**: 根据当前 selectedSectors 动态生成，如"创新药深挖"

#### P2-4: SSE 心跳缺失
- **问题**: 无 `:keepalive\n\n` 帧，代理环境下连接可能被超时关闭
- **建议**: 每15秒发送心跳

#### P2-5: SSE 重连无手动按钮
- **问题**: 3次自动重试失败后只提示"刷新页面"
- **建议**: 增加手动重连按钮 + 从 API 恢复历史事件

---

## 三、LLM Prompt 优化发现

### 3.1 日志中的具体问题

| 问题 | 日志证据 | 影响 |
|------|----------|------|
| **100秒超时阻断** | `traceId=52da468d elapsedMs=100011 type=InvalidOperationException` | 角色执行100%失败 |
| **连接被远程关闭** | `traceId=48652cdf message="远程主机强迫关闭了一个现有的连接"` | 部分流水线失败 |
| **巨量 Prompt** | Commander Agent 单次 prompt 126,316 chars | Token 超限风险 |
| **工具失败不透明** | ToolDispatcher 返回 `{"error":"..."}` 被当做正常内容 | LLM 无法区分工具错误与正常结果 |

### 3.2 Prompt 模板优化建议

#### A. 工具调用指令不够明确
**当前**:
```
当你需要获取外部数据时，请输出如下 JSON（一次只能调用一个工具，等待结果后再继续）：
{"tool_call":{"name":"工具名","args":{参数}}}
```

**建议优化**:
```
当你需要获取外部数据时，请输出如下 JSON（一次只能调用一个工具，等待结果后再继续）：
{"tool_call":{"name":"工具名","args":{参数}}}

重要规则：
1. 你最多可以调用 {MaxToolCalls} 次工具。达到限制后必须直接输出最终结果 JSON。
2. 如果工具返回包含 "error" 字段，说明调用失败。请尝试简化查询参数或使用备选策略，不要重复相同的失败调用。
3. 所有分析完成后，直接输出最终结果 JSON（不要包裹在 tool_call 中）。
```

#### B. 无失败恢复引导
**问题**: 当工具返回错误时，prompt 只是追加结果，没有引导 LLM 如何应对失败
**建议**: 在工具返回结果后追加的模板中区分成功和失败：
```
// 成功时:
"## 工具调用 {n}: {toolName}\n### 工具返回结果\n{result}\n\n请继续分析，或输出最终结果 JSON。"

// 失败时:
"## 工具调用 {n}: {toolName}\n### 工具调用失败\n{result}\n\n该工具暂时不可用。请基于已有信息继续分析，或换用其他工具补充数据。"
```

#### C. Director 角色 Prompt 过大
**问题**: Director（推荐总监）的 upstreamArtifacts JSON 可能包含所有前4个阶段的完整输出，导致 prompt 超长
**建议**: 在传递给 Director 之前，对 upstream artifacts 进行摘要压缩（如只保留 summary/verdict/picks 等关键字段）

#### D. 缺少时间上下文
**问题**: 角色 prompt 中没有明确告知当前日期和交易时段
**建议**: 在 BuildPrompt 时注入当前日期 + 交易时段状态：
```
## 当前时间上下文
- 日期: 2026-04-01 (星期三)
- A股交易状态: 午盘休市中 (11:30-13:00)
- 检索信息时请注意时效性，优先使用最近72小时内的数据
```

#### E. 缺少输出质量约束
**建议**: 在每个角色 prompt 的输出要求部分增加质量约束：
```
## 质量要求
- 每条证据必须标注来源(source)和时间(publishedAt)，缺少时间标注的信息降级为弱证据
- 数值型数据（涨跌幅、资金流向等）必须标注数据来源和时间点
- 不确定的判断请明确标注置信度
```

---

## 四、工具调用与错误处理优化

### 4.1 工具调用层问题

| 问题 | 位置 | 严重度 |
|------|------|--------|
| **工具无重试机制** | `RecommendToolDispatcher` | 高 |
| **参数缺失静默默认** | `GetRequired()` 默认 query="A股市场", symbol="000001" | 中 |
| **股票数据工具无降级** | kline/minute/fundamentals 失败直接异常 | 高 |
| **工具错误对LLM不透明** | 错误包在 JSON 里当正常结果传递 | 中 |
| **无全局超时** | 流水线可无限运行 | 中 |
| **无断路器** | MCP 服务宕机时持续打请求 | 中 |

### 4.2 修复计划

1. **工具重试**: ToolDispatcher 增加2次指数退避重试（1s → 3s），仅对网络异常/超时重试
2. **显式错误标记**: 工具失败时返回 `{"tool_error": true, "error": "..."}` 并在追加 prompt 中用不同模板
3. **参数验证**: 移除静默默认值，缺少必需参数时返回明确错误
4. **全局流水线超时**: 在 Runner 层设置 `CancellationTokenSource` 超时（如8分钟）
5. **断路器**: 对 MCP Gateway 增加简单断路器（连续3次失败 → 短路5分钟）

---

## 五、前端优化计划

### 5.1 报告展示修复

```
当前: latestTurn → stageSnapshots → FinalDecision → director output
优化: allTurns.reverse() → 找到第一个有 FinalDecision 的 turn → director output
```

### 5.2 辩论过程增强

- 增加过滤器栏：「全部」|「仅结论」|「仅工具」
- 长内容默认折叠（>200字），展开查看全部
- 失败角色的气泡增加 errorMessage tooltip

### 5.3 进度面板增强

- 角色状态增加 errorMessage 展示
- 失败角色可展开查看 traceId + 错误详情
- 阶段耗时统计

### 5.4 历史列表增强

- 显示 userPrompt 前缀 + turn 计数
- 状态用颜色编码（绿=完成、黄=降级、红=失败、灰=运行中）
- 长按/右键删除历史会话

---

## 六、执行计划

### Phase 1: 地基修复（P0）— 预估 1-2 天

| 任务 | 涉及文件 | 验收标准 |
|------|----------|----------|
| 修复 DbContext 并发 | `RecommendationRunner.cs` | 3角色并行阶段不再抛出 InvalidOperationException |
| LLM 重试策略 | `RecommendationRoleExecutor.cs` | 单次超时后自动重试2次，指数退避 |
| 报告数据源修复 | `RecommendReportCard.vue` | 追问后仍能显示历史推荐报告 |
| 中文编码修复 | `RecommendationRunner.cs` / `RecommendationSessionService.cs` | 所有中文内容正确显示 |

### Phase 2: 稳定性增强（P1）— 预估 2-3 天

| 任务 | 涉及文件 | 验收标准 |
|------|----------|----------|
| SSE 断连事件恢复 | `StocksModule.cs` SSE 端点 + 前端 | 断线重连后事件不丢失 |
| EventBus 内存管理 | `RecommendEventBus.cs` + Runner | Turn 完成后15分钟内自动清理 |
| 僵尸会话终结 | 新增 BackgroundService | Running>10min 自动标记 TimedOut |
| 工具重试+断路器 | `RecommendToolDispatcher.cs` | 网络闪断不再直接失败 |
| 核心路径测试 | Tests 项目 | RunTurnAsync + ExecuteParallel + ExecuteDebate 覆盖 |

### Phase 3: 体验优化（P2）— 预估 2-3 天

| 任务 | 涉及文件 | 验收标准 |
|------|----------|----------|
| Prompt 优化（工具指令+失败引导+质量约束） | `RecommendPromptTemplates.cs` | LLM 能正确处理工具失败 |
| 辩论过程过滤器 | `RecommendFeed.vue` | 支持"只看结论"模式 |
| 历史列表摘要 | `StockRecommendTab.vue` | 显示 userPrompt 前缀 |
| 进度面板错误详情 | `RecommendProgress.vue` | 失败角色显示错误原因 |
| 快捷追问动态化 | `StockRecommendTab.vue` | 按钮内容匹配推荐上下文 |
| SSE 心跳+手动重连 | SSE 端点 + 前端 | 15秒心跳 + 手动重连按钮 |

### Phase 4: 高级功能（后续迭代）

| 任务 | 描述 |
|------|------|
| Director Prompt 摘要压缩 | upstream artifacts 超过阈值时自动摘要 |
| 推荐命中率回测 | 推荐发出后追踪 1/3/5 天涨跌对比 |
| 推荐结果汇总表 | 一页纵览所有推荐个股关键指标 |
| K线工作台联动 | 推荐的支撑位/压力位标记到K线图上 |

---

## 七、风险与依赖

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| api.bltcy.ai 网关不稳定 | LLM 请求持续超时 | 增加备用 Provider 切换 + 超时参数可配置 |
| Gemini Flash 模型不遵循 JSON 输出格式 | 角色输出解析失败 | 增加 JSON 修复重试 + 结构化输出校验 |
| SearXNG Docker 容器未部署 | Web 搜索降级到 DuckDuckGo（结果质量下降） | 部署 SearXNG 或确认 Tavily 额度充足 |
| 前端 SSE 在 WebView2 中的兼容性 | Desktop 版本 SSE 行为异常 | 需要 Desktop 专项测试 |

---

## 八、验收标准

### 最低可用标准（Phase 1 完成后）
- [ ] 至少 1 个完整推荐会话成功到达 FinalDecision 并展示报告
- [ ] 3 角色并行阶段不再有 DbContext 异常
- [ ] 所有中文内容正确编码显示
- [ ] 追问后历史报告不消失

### 生产可用标准（Phase 2 完成后）
- [ ] 推荐完成率 ≥ 70%
- [ ] SSE 断连后可恢复事件流
- [ ] 无僵尸 Running 会话
- [ ] Runner 完整流水线有集成测试覆盖

### 用户满意标准（Phase 3 完成后）
- [ ] User Representative 评分 ≥ 7.0/10
- [ ] 辩论过程支持过滤
- [ ] 历史列表有内容摘要
- [ ] 失败时用户能看到原因并知道下一步该做什么
