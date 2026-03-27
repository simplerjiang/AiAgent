# GOAL-AGENT-NEW-001-P0-PRE-PHASE-F-R2 直观结果展示

> 基于真实回执文件：`.automation/tmp/live-gate-visual.json`
> 
> 本文只做人工可读整理，不补写、不猜测、不编造底层字段。真正可采信事实以后端 tool results 与 traceId 为准。

## 总览

- **用户问题**：看下浦发银行日线结构和本地新闻证据
- **Symbol**：`sh600000`
- **live gate traceId**：`00906074eef44eeea3c9770a6f2e5b29`
- **tool 数量**：`4`
- **最终状态**：`done`
- **执行概况**：4 个已批准 MCP 全部执行完成，`allowExternalSearch=False`，本轮保持 Local-First，没有触发外部搜索。

## 为什么这里只有 4 个 MCP

你记得没错：**P0-PRE 里可用的 MCP 不止 4 个**。  
这份报告之所以只展示 4 个，是因为它展示的是**这一轮 live gate 真实执行结果**，不是“全量 MCP 清单”。

### P0-PRE 当前可用的查询型 MCP（按 live gate tool registry）

1. `CompanyOverviewMcp`
2. `StockProductMcp`
3. `StockFundamentalsMcp`
4. `StockShareholderMcp`
5. `MarketContextMcp`
6. `SocialSentimentMcp`
7. `StockKlineMcp`
8. `StockMinuteMcp`
9. `StockStrategyMcp`
10. `StockNewsMcp`
11. `StockSearchMcp`（`external_gated`）

### 为什么这次只执行了 4 个

因为这次用户问题非常具体：

> **看下浦发银行日线结构和本地新闻证据**

在这个问题下，planner 实际上只需要补齐 4 类信息：

- 公司基本背景 → `CompanyOverviewMcp`
- 市场环境上下文 → `MarketContextMcp`
- 日线结构 → `StockKlineMcp`
- 本地新闻证据 → `StockNewsMcp`

也就是说：

- 这次不是“系统只能跑 4 个 MCP”
- 而是“**这次问题只需要 4 个 MCP 就够完成最小闭环**”

### 另外几个 MCP 为什么没被调用

- `StockProductMcp`：更偏**主营业务 / 产品结构 / 行业 / 地区**，这次问题没直接问产品面。
- `StockFundamentalsMcp`：更偏**财务基本面**，例如营收、利润、估值因子；这次问题重点不是基本面拆解。
- `StockShareholderMcp`：更偏**股东结构 / 持股变化**，本轮问题没有触发。
- `SocialSentimentMcp`：更偏**情绪代理 / 社交情绪降级契约**，本轮已有 `StockNewsMcp`，且问题重点是“本地新闻证据”，不是社交情绪。
- `StockMinuteMcp`：更偏**分时结构**，但这次问题明确是“日线结构”，所以没必要上分时。
- `StockStrategyMcp`：更偏**策略信号**，例如策略标签或技术信号；这次只要求先看日线结构和新闻证据。
- `StockSearchMcp`：这是 `external_gated`，而本轮 `allowExternalSearch=False`，所以直接禁止。

### 还有两个限制也决定了不会把 MCP 全跑一遍

1. **live gate 不是“把所有 MCP 都扫一遍”的模式**  
  它是根据问题生成一个“最小必要工具计划”。

2. **有预算限制**  
  live gate 规则里明确写了：
  - 最多 `6` 个 `toolCalls`
  - 最多 `1` 个 `StockSearchMcp`

所以从设计上，它就不会为了“看起来全面”而把 11 个 MCP 全部打一遍。

### 一句话理解

> **P0-PRE 有很多 MCP；这份报告只出现 4 个，是因为它展示的是“这一次问题实际命中的 MCP 子集”，不是“全部 MCP 功能表”。**

## 一眼看懂

这次 live gate 不是在“直接给投资建议”，而是在做一次**受控 MCP 回执验收**。实际执行的 4 个 MCP 分别覆盖：

1. 公司概况
2. 市场环境
3. 日线 K 线结构
4. 本地新闻证据

从回执看，这一轮已经把用户问题需要的两个核心面向都覆盖到了：
- **日线结构**：已拿到 `StockKlineMcp` 结果
- **本地新闻证据**：已拿到 `StockNewsMcp` 结果

---

## 1. CompanyOverviewMcp

### MCP 名称

`CompanyOverviewMcp`

### 模拟输入请求

```json
{
  "symbol": "sh600000"
}
```

### 实际输出结果

- **summary**：公司概况：浦发银行，主营=`缺失`，经营范围=吸收公众存款、发放短中长期贷款、办理结算、票据贴现、发行金融债券、外汇业务、银行卡业务、基金托管等一长串银行业务。
- **关键 evidence / feature**：
  - 公司标题为 **浦发银行**，来源是“公司画像缓存”。
  - 回执明确体现：**主营字段缺失**，这也是这次公司概览里最值得人工注意的缺口。
  - 所属板块为 **银行**，股东户数为 **119099**，现价为 **10.04**。
  - **经营范围非常长**，更像完整经营项目列表，而不是压缩后的主营摘要；可读性强，但不够“精炼主营”。

### traceId

`442e365bae064b848eb0f5ee6ef99e8e`

---

## 2. MarketContextMcp

### MCP 名称

`MarketContextMcp`

### 模拟输入请求

```json
{
  "symbol": "sh600000"
}
```

### 实际输出结果

- **summary**：市场阶段=`分歧`，主线=`单抗概念`。
- **关键 evidence / feature**：
  - 阶段 evidence：**阶段=分歧，置信度=60.35**。
  - 板块对齐 evidence：个股行业是 **银行**，当前主线是 **单抗概念**。
  - 回执明确写出：**主线对齐=否**，也就是浦发银行所在行业并不在当前主线方向上。

### traceId

`173c5555484548488ecbc540a34efc6b`

---

## 3. StockKlineMcp

### MCP 名称

`StockKlineMcp`

### 模拟输入请求

```json
{
  "period": "daily",
  "symbol": "sh600000"
}
```

### 实际输出结果

- **summary**：**K 线窗口=60，趋势=盘整，5D=1.83%。**
- **关键 evidence / feature**：
  - 这次回执里最核心的结构结论就是：**窗口=60**。
  - 趋势判断不是单边上攻或单边下跌，而是：**趋势=盘整**。
  - 近 5 日变化直接写在 summary 中：**5D=1.83%**。
  - 该 MCP 结果带有一个 degraded flag：`expanded_news_window`，说明它在收集辅助证据时存在“新闻时间窗扩展”的痕迹，但主结论仍然正常返回。

### traceId

`7adfec8c29b94c59abf9be97d9782dcb`

---

## 4. StockNewsMcp

### MCP 名称

`StockNewsMcp`

### 模拟输入请求

```json
{
  "symbol": "sh600000"
}
```

### 实际输出结果

- **summary**：本地新闻 **20 条**，最新时间=`2026-03-20 17:47:45`。
- **关键 evidence / feature（只挑代表性 4 条，不把 20 条全部铺开）**：
  1. **2026-03-20｜关于召开 2025 年度业绩说明会的公告**  
     - 来源：东方财富公告  
     - 情绪：中性  
     - 标签：`财报业绩`、`政策红利`
  2. **2026-02-26｜优先股二期股息发放实施公告**  
     - 来源：东方财富公告  
     - 情绪：利好  
     - 标签：`财报业绩`
  3. **2026-01-13｜2025 年度业绩快报公告**  
     - 来源：东方财富公告  
     - 情绪：中性  
     - 标签：`财报业绩`
  4. **2025-12-29｜关于公司章程修订获核准及不再设立监事会的公告**  
     - 来源：东方财富公告  
     - 情绪：中性  
     - 标签：`监管政策`

### traceId

`709999b4fe344db7a0ecce7e9f40b3ff`

---

## 人工可直接看到的结论

这次 live gate 的真实回执，已经把“**浦发银行日线结构**”和“**浦发银行本地新闻证据**”两部分都实际跑出来了，而且是 **4 个本地 MCP 全部执行完成** 的状态。

人工复核时，最值得直接记住的是下面几点：

- **K 线结构端**：回执明确给出 **窗口=60、趋势=盘整、5D=1.83%**，所以这不是一个“强趋势已经拍板”的回执，更像是**整理中的日线状态**。
- **新闻证据端**：本地新闻共 **20 条**，最近一条时间是 **2026-03-20 17:47:45**，以**公告型证据**为主，重点集中在**业绩说明会、股息发放、业绩快报、治理/监管事项**。
- **公司概况端**：公司识别本身没问题，但 **主营字段缺失**，同时 **经营范围非常长**，说明当前 company overview 更像“完整经营项目展开”，不够像“精炼主营画像”。
- **市场环境端**：市场阶段是 **分歧**，而浦发银行所属 **银行** 行业与当前主线 **单抗概念** **不对齐**。

综合来看，这份回执说明的不是“系统已经给出最终投资结论”，而是：

> live gate 已经用真实本地 MCP 把相关证据链拉齐，且结果可追溯；其中最清晰的结构信号是 **日线盘整 + 5D 小幅正收益**，最清晰的新闻画像是 **以本地公告型证据为主**，最明显的信息缺口则是 **CompanyOverview 的主营缺失**。

---

## 补充验收：按文档逻辑执行“全量可用 MCP”实测（11/11）

> 时间：2026-03-27（本机）
>
> 目标：不是让 planner 一次性调用全部 MCP（live gate 有预算上限），而是按文档列出的“可用 MCP 清单”逐个走真实后端接口，确认每个 MCP 都能返回有效回执与 traceId。

### 先跑的单测（unit first）

已先执行 MCP 相关单测，再做接口实测：

- `dotnet test backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter "FullyQualifiedName~StockMcpGatewayPhaseATests|FullyQualifiedName~StockCopilotMcpServiceTests|FullyQualifiedName~StockMcpEndpointExecutorTests"`
- 结果：**41/41 通过，0 失败**

### 全量 11 个 MCP 逐项接口实测结果

测试基线：

- BaseUrl：`http://localhost:5119`
- Symbol：`sh600000`
- TaskId：`GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-20260327`

1. `CompanyOverviewMcp` → 200，traceId=`03559f8aeb694bf785b44154d5d80513`
2. `StockProductMcp` → 200，traceId=`662488f52cb44fef8a805d177ca775bc`
3. `StockFundamentalsMcp` → 200，traceId=`35856a11ca5446a1b16e69a276ad85f9`
4. `StockShareholderMcp` → 200，traceId=`dc854eaefb43426f8ba9b9acac701370`
5. `MarketContextMcp` → 200，traceId=`93ca7226a0e1401aaf0e8a28968fdb4f`
6. `SocialSentimentMcp` → 200，traceId=`7b2874ad524241439387726ab69a79ab`
7. `StockKlineMcp` → 200，traceId=`8d1b691b15194e3fa1ab5c26443617db`
8. `StockMinuteMcp` → 200，traceId=`dc33fd2f892f4399bb829ff06309638e`
9. `StockStrategyMcp` → 200，traceId=`44beb8069c914abd80a7f0c4f9d00486`
10. `StockNewsMcp` → 200，traceId=`767cae82863a4762aacd98b4d78c6340`
11. `StockSearchMcp`（`external_gated`）→ 200，traceId=`db313ffbd0f74110b85fb6a9f29dc33f`

### 产物文件

- `.automation/tmp/mcp-full-test-part1-20260327-113849.json`
- `.automation/tmp/mcp-full-test-part2-20260327-113751.json`
- `.automation/tmp/mcp-full-request-response-20260327-114635.json`（**11 个 MCP 的完整请求与完整返回正文**）
- `.automation/tmp/mcp-full-request-response-20260327-114635.md`（快速索引版）
- `.automation/reports/GOAL-AGENT-NEW-001-P0-PRE-PHASE-F-R2-REQRESP-FILTERED-20260327.md`（**过滤后的可读版，请求 + 返回已结构化**）

> 注：`mcp-full-request-response-20260327-114330.json` 是后端不在线时的失败采样（保留用于排障对照），本轮有效正文证据以 `114635` 版本为准。

### 本轮结论

- 按文档清单可用的 **11 个 MCP 已全部完成真实接口验证（11/11，HTTP 200）**。
- `StockSearchMcp` 虽然在 live gate 规划层是 `external_gated`，但其 MCP 接口本身可正常工作；是否在某轮会话里允许执行，仍由 live gate 的审批与 `allowExternalSearch` 控制。
