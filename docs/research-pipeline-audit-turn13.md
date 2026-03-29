# 多Agent研究管线 - 完整链路审计报告

> **审计目标**: Session 9 · Turn 13（追问轮）  
> **标的**: SH600036 招商银行  
> **用户追问**: "帮我解释一下为什么这个估值相对偏低"  
> **时间窗口**: 2026-03-28 23:06:20 → 23:11:41（**5分21秒**）  
> **最终评级**: **Hold**  
> **Session 状态**: Degraded（product_analyst 工具失败）

---

## 1. 管线架构总览

```
用户追问
    │
    ▼
┌─────────────────────────────────────┐
│   Follow-up 路由组合经理 (LLM)       │ ← 9.1s
│   决策: ContinueSession              │
│   置信度: 1.0                         │
└─────────────┬───────────────────────┘
              │
    ▼ 全链路重跑 (stageIndex=0)
              │
┌─────────────┴───────────────────────┐
│ Stage 0: CompanyOverviewPreflight    │ 23:06:20 → 23:07:23 (63s)
│  └─ company_overview_analyst         │ Sequential
└─────────────┬───────────────────────┘
              │
┌─────────────┴───────────────────────┐
│ Stage 1: AnalystTeam                 │ 23:07:23 → 23:08:42 (79s)
│  ├─ market_analyst          ─┐       │
│  ├─ fundamentals_analyst     │       │
│  ├─ news_analyst             │ 并行  │
│  ├─ social_sentiment_analyst │       │
│  ├─ shareholder_analyst      │       │
│  └─ product_analyst ⚠️      ─┘       │ Degraded
└─────────────┬───────────────────────┘
              │
┌─────────────┴───────────────────────┐
│ Stage 2: ResearchDebate              │ 23:08:42 → 23:09:51 (69s)
│  ├─ bull_researcher    ─┐            │
│  ├─ bear_researcher     │ 3 Rounds  │
│  └─ research_manager   ─┘            │
└─────────────┬───────────────────────┘
              │
┌─────────────┴───────────────────────┐
│ Stage 3: TraderProposal              │ 23:09:51 → 23:10:04 (13s)
│  └─ trader                           │ Sequential
└─────────────┬───────────────────────┘
              │
┌─────────────┴───────────────────────┐
│ Stage 4: RiskDebate                  │ 23:10:04 → 23:11:21 (77s)
│  ├─ aggressive_risk_analyst  ─┐      │
│  ├─ neutral_risk_analyst      │ 3R   │
│  └─ conservative_risk_analyst─┘      │
└─────────────┬───────────────────────┘
              │
┌─────────────┴───────────────────────┐
│ Stage 5: PortfolioDecision           │ 23:11:21 → 23:11:41 (20s)
│  └─ portfolio_manager                │ Sequential
└─────────────────────────────────────┘
```

---

## 2. Follow-up 路由决策

### 2.1 路由 LLM 请求

| 字段 | 值 |
|---|---|
| **traceId** | `c72e4922622d4be99006f84ff43caf58` |
| **时间** | 23:06:11.365 |
| **模型** | gemini-3.1-flash-lite-preview-thinking-high |
| **温度** | 0.1 |
| **promptChars** | 2,252 |
| **耗时** | 9,082ms |

### 2.2 路由 Prompt（完整）

```
你是研究工作台中的 follow-up 路由组合经理。你的职责不是重新分析股票，
而是判断这次追问应该如何复用已有研究。只输出 JSON，不要输出 Markdown。

可选 route：ContinueSession | PartialRerun | FullRerun | NewSession
stageIndex 含义：0=公司概览，1=分析师团队，2=研究辩论，3=交易方案，
                 4=风险评估，5=投资决策。

规则：
1. 如果追问只是澄清、解释、补充已有结论，优先 ContinueSession。
2. 如果只需要重做某一后段链路，优先 PartialRerun，并给出最早需要重跑的 stageIndex。
3. 只有当前分析框架整体失效时才选择 FullRerun。
4. 只有用户明确要求开启新的独立分析主题时才选择 NewSession。
5. 输出 JSON 字段：route, fromStageIndex, reuseScope, changeSummary, reasoning, confidence。

symbol: sh600036
sessionName: 分析一下招商银行
latestPrompt: 分析一下招商银行
latestContinuationMode: ContinueSession
latestRating: 
latestExecutiveSummary: 
latestRiskConsensus: 
latestBlocks:
- CompanyOverview: 招商银行 公司概览 | 招商银行股份有限公司是中国领先的股份制商业银行...
- Fundamentals:  | {"content":"{\n  \"qualityView\": \"平稳\",\n  \"valuationView\": \"低估\"...
- Market:  | {"content":"{\n  \"trendState\": \"盘整\",\n  \"keyLevels\": {...
- News:  | {"content":"{\n  \"eventBias\": \"利好\",\n  \"impactScore\": 7...
- PortfolioDecision: 投资组合决策 | 招商银行作为中国零售银行与财富管理的行业标杆...
- Product:  | 吸收公众存款；发放短期、中期和长期贷款...
- ResearchDebate: 研究辩论裁决 | 招商银行目前处于"基本面稳健"与"技术面回调"的博弈期...
- RiskReview: 风险审查 | 有条件通过。建议将初始仓位下调至15%...

userFollowUp: 帮我解释一下为什么这个估值相对偏低

请使用中文回答。
```

### 2.3 路由 LLM 响应

```json
{
  "route": "ContinueSession",
  "fromStageIndex": 0,
  "reuseScope": "Fundamentals",
  "changeSummary": "解释招商银行估值偏低的原因",
  "reasoning": "用户针对已有研究结论（估值低估）进行追问，属于对现有分析结果的
                深化解释，无需重新运行分析链路，直接基于现有基本面数据进行逻辑阐述即可。",
  "confidence": 1.0
}
```

### 2.4 🔴 改进点：路由结果与实际执行严重矛盾

| 问题 | 描述 |
|---|---|
| **路由说** | `ContinueSession`（继续会话，无需重跑） |
| **实际执行** | 从 Stage 0 开始全链路重跑了所有 6 个阶段，包括 3 轮辩论 |
| **浪费** | ~5 分钟的完整管线运行，27+ 次 LLM 调用，~30 次 MCP 工具调用 |
| **根本原因** | `fromStageIndex=0` 导致引擎将其解释为"从头开始"，与 `ContinueSession` 语义矛盾 |

**建议**：
1. 当路由返回 `ContinueSession` 时，应直接让 `portfolio_manager` 基于已有 blocks 回答追问，**跳过全部分析阶段**。
2. `fromStageIndex` 字段在 `ContinueSession` 模式下应被忽略。
3. 路由 prompt 应明确说明：*"ContinueSession 意味着不重跑任何阶段，仅生成追问回复。"*

---

## 3. Stage 0: CompanyOverviewPreflight

> 23:06:20 → 23:07:23 · 63 秒 · 1 个角色 (Sequential)

### 3.1 company_overview_analyst

#### 系统提示词模板（AnalystSystemShell）

```
你是一个多 Agent 股票研究管线中的专业分析师角色。请严格遵守以下规则：
1. 你有权使用分配给你的 MCP 工具（Tool）查询实时数据，优先使用工具获取数据而非凭记忆回答。
2. 你已获得前序角色的研究报告作为协作上下文，但你必须独立完成自己的分析。
3. 所有分析结论必须有数据证据支撑，禁止无根据推测。
4. 涉及 MCP 工具参数时，一律使用英文拼写（如 symbol, period）。
5. 请按照你的角色定义输出结构化分析报告。
```

#### 角色任务提示词

```
## 角色：公司概览分析师

### 职责
提供公司的完整基础画像、当前市场定位和近期动态概览，为后续分析师团队提供背景参考。

### 工具
- CompanyOverviewMcp：获取公司基本信息
- MarketContextMcp：获取市场环境数据
- StockNewsMcp：获取近期新闻
- StockSearchMcp：搜索相关信息

### 输出格式
以 JSON 结构输出：
- headline: 一句话概要
- summary: 2-3 段综合画像
- companyName / industry / listingDate 等基础字段
```

#### MCP 工具调用链

| # | 工具 | 时间 | 耗时 | 结果 |
|---|---|---|---|---|
| 1 | **CompanyOverviewMcp** | 23:06:21 → 23:06:31 | ~10s | ✅ |
| 2 | **MarketContextMcp** | 23:06:31 → 23:06:31 | ~0.2s | ✅ (缓存) |
| 3 | **StockNewsMcp** | 23:06:31 → 23:06:42 | ~10s | ✅ |
| 4 | **StockSearchMcp** | 23:06:42 → 23:07:09 | ~27s | ✅ |

> MCP 工具调用格式: `GET /api/stocks/mcp/{tool}?symbol=sh600036`

#### LLM 调用详情

| 字段 | 值 |
|---|---|
| **traceId** | `cac505681cb54f26985e749d287e835d` |
| **模型** | gemini-3.1-flash-lite-preview-thinking-high |
| **promptChars** | 33,610 |
| **systemChars** | 164 |
| **耗时** | 13,969ms |

#### Agent 输出摘要

```
招商银行作为国内领先的股份制商业银行，当前市盈率（PE）约为6.62倍，
估值处于市场较低水平。其低估值特征主要源于银行板块的防御性属性、
市场对净息差收窄的担忧以及对未来盈利增速的保守预期。同时，公司具备
高股息率（TTM约7.64%）特征，在当前市场环境下被视为稳健的红利资产，
而非高成长性资产。
```

---

## 4. Stage 1: AnalystTeam

> 23:07:23 → 23:08:42 · 79 秒 · 6 个角色 (Parallel)

### 4.1 各分析师 MCP 工具调用汇总

| 角色 | 工具调用链 | 异常 |
|---|---|---|
| **market_analyst** | MarketContextMcp → StockKlineMcp(27s) → StockMinuteMcp(16s) → StockStrategyMcp(18s) | — |
| **fundamentals_analyst** | StockFundamentalsMcp(15s) → CompanyOverviewMcp ⚠️retry → MarketContextMcp | CompanyOverviewMcp 重试1次 |
| **news_analyst** | StockNewsMcp(12s) → MarketContextMcp → StockSearchMcp | — |
| **social_sentiment_analyst** | SocialSentimentMcp(4s) → StockNewsMcp(12s) → MarketContextMcp → StockSearchMcp | — |
| **shareholder_analyst** | StockShareholderMcp ⚠️retry(28s) → CompanyOverviewMcp(6s) → StockNewsMcp ⚠️retry(15s) | 两个工具各重试1次 |
| **product_analyst** | StockProductMcp ❌fail(30s) → CompanyOverviewMcp ⚠️retry → MarketContextMcp → StockNewsMcp | **StockProductMcp 重试后彻底失败** |

### 4.2 fundamentals_analyst 详情（代表性样本）

#### LLM 调用

| 字段 | 值 |
|---|---|
| **traceId** | `a8bf5ff0f4ea4d45b66bcde6e45cff5e` |
| **promptChars** | 34,045 |
| **耗时** | 12,518ms |

#### LLM 输出（格式化 JSON）

```json
{
  "qualityView": "平稳",
  "valuationView": "低估",
  "metrics": {
    "revenue": "3375.32亿元",
    "revenueYoY": "0.01%",
    "netProfit": "1501.81亿元",
    "netProfitYoY": "1.21%",
    "eps": "5.7元",
    "roe": "13.44%",
    "debtRatio": "90.2%",
    "peRatio": 6.62
  },
  "highlights": [
    "盈利规模庞大，2025年归母净利润达1501.81亿元，展现极强的经营底盘。",
    "具备高股息特征，TTM股息率约7.64%，在当前市场环境下具备较强的防御性资产属性。",
    "ROE保持在13.44%的稳健水平，体现了公司在行业内的资本回报能力。"
  ],
  "risks": [
    "成长性明显不足，营收同比仅增长0.01%，净利同比仅增长1.21%，缺乏估值溢价的成长动力。",
    "市场风格偏好压制，当前资金主要流向逆变器等高成长板块，银行股缺乏估值修复的流动性支持。",
    "高资产负债率（90.2%）虽为银行行业特性，但需持续关注宏观经济环境下的资产质量与净息差压力。"
  ],
  "evidenceTable": [
    {
      "metric": "市盈率(PE)",
      "value": "6.62",
      "period": "2026-03-28",
      "source": "CompanyOverviewMcp",
      "assessment": "低估"
    }
    // ... 更多指标
  ]
}
```

### 4.3 market_analyst 输出

```
成交量表现平稳，缺乏放量突破迹象，反映出市场对该股作为防御性资产的
存量博弈特征，主力资金未见明显介入，导致股价难以脱离盘整区间。
```

> traceId: `5ea77aa0df0f49f0b1581457dab75871`

### 4.4 product_analyst 输出 (⚠️ Degraded)

```
吸收公众存款；发放短期、中期和长期贷款；办理结算；办理票据贴现；
发行金融债券；代理发行、代理兑付、承销政府债券...（经营范围枚举）
```

> traceId: `d1454a1646d14332b8f0b29ee479e7b0`  
> 状态: **Degraded** — StockProductMcp 工具在重试后失败，回退到 CompanyOverview 数据

### 4.5 🔴 改进点

| # | 问题 | 建议 |
|---|---|---|
| 1 | **Fundamentals 输出包含嵌套 JSON 包装**: Block summary 字段为 `{"content":"..."}` 格式，前端需要额外解析 | 在 Block 写入层自动展平 `content` 字段 |
| 2 | **Market/News Block 同样有嵌套 JSON**: 标题为空，summary 是原始 JSON 字符串 | Block 生成时应提取 headline 作为标题 |
| 3 | **StockSearchMcp 耗时过长**: CompanyOverview 的 StockSearchMcp 调用耗时 27 秒 | 考虑并行化或设置更短的超时 |
| 4 | **product_analyst 价值极低**: 输出仅为经营范围枚举，无实质分析 | 重新设计 prompt，或在追问轮跳过该角色 |
| 5 | **MCP 重试缺乏指数退避**: 多个工具重试，但日志未显示退避策略 | 添加指数退避 + 抖动 |

---

## 5. Stage 2: ResearchDebate

> 23:08:42 → 23:09:51 · 69 秒 · 3 角色 × 3 轮

### 5.1 辩论结构

```
Round 1:  research_manager → bull_researcher → bear_researcher
Round 2:  bull_researcher → research_manager → bear_researcher
Round 3:  bear_researcher → research_manager → bull_researcher
```

> 每轮 ~23 秒，3 个角色并行启动但因上下文依赖实际近串行。

### 5.2 research_manager 第一轮裁决（代表性样本）

#### LLM 调用

| 字段 | 值 |
|---|---|
| **traceId** | `d4df9b45e09a40ae8f75b10b024076ee` |
| **promptChars** | ~35,000 (estimated) |
| **耗时** | ~21s |

#### 输出

```
招商银行当前估值（PE 6.62倍）处于低位，并非因为公司基本面出现恶化，
而是市场定价逻辑与公司资产属性错配的必然结果。

首先，从Bull（看多）视角看，招商银行拥有极高的股息率（TTM约7.64%）和
稳健的ROE（13.44%），这构成了极强的安全边际。然而，Bear（看空/谨慎）
论点在此刻更具解释力：公司营收同比仅增长0.01%，净利润增长1.21%，
这种近乎停滞的增长速度直接导致了市场无法给予其成长性溢价。

综合来看，招商银行目前处于"价值陷阱"与"防御性资产"的模糊地带。
```

### 5.3 bull_researcher 第一轮论点

```
招商银行当前6.62倍的市盈率（PE）严重低估了其作为国内零售金融龙头的
核心资产价值。市场因短期营收增长放缓（0.01%）而将其简单归类为
"无成长性"资产，这完全忽视了其在宏观波动环境下的极高经营韧性。
招行高达7.64%的TTM股息率构成了极强的安全边际，在当前利率下行周期中，
这种高分红属性具备极高的配置性价比。
```

### 5.4 bear_researcher 第一轮论点

```
招商银行当前6.62倍的市盈率（PE）并非市场给予的"低估值修复"机会，
而是对其长期增长停滞的理性定价。根据2025年报数据，公司营收同比增速
仅为0.01%，净利润同比增速仅为1.21%，这种近乎停滞的增长曲线，
使得任何估值溢价都缺乏基本面支撑。筹码结构极度分散（股东户数624,961户），
缺乏主力资金锁仓迹象。
```

### 5.5 🔴 改进点：辩论轮次产生高度重复内容

| 问题 | 描述 |
|---|---|
| **3 轮内容几乎一致** | bull_researcher 三轮论点的核心论据（PE=6.62、ROE=13.44%、股息率7.64%）完全相同，仅措辞微调 |
| **bear_researcher 同样高度重复** | 三轮均围绕"增长停滞+价值陷阱+死叉共振"，无新增论据或论点深化 |
| **research_manager 几乎复制粘贴** | 三轮裁决结论、措辞高度相似，缺少"回应对方反驳"的真正辩论交锋 |
| **Token 浪费严重** | 3 轮 × 3 角色 = 9 次 LLM 调用产出几乎相同内容 |

**建议**：
1. 辩论 prompt 应强制要求**引用并反驳**对方上一轮的具体论据，而非重申自己的观点。
2. 增加"新论据要求"：每轮必须提出至少 1 个未在前轮出现的论据或数据点。
3. 设置"收敛检测"：如果连续两轮的核心论点变化率 <20%，自动终止辩论。
4. 考虑对追问场景（ContinueSession）减少辩论轮次到 1 轮。

---

## 6. Stage 3: TraderProposal

> 23:09:51 → 23:10:04 · 13 秒 · 1 个角色 (Sequential)

### 6.1 trader

#### LLM 调用

| 字段 | 值 |
|---|---|
| **traceId** | `89b2d20cb191417882dddc3312e85ec1` |
| **promptChars** | 34,981 |
| **耗时** | 12,864ms |

#### LLM 输出（格式化 JSON）

```json
{
  "proposal": "基于 Research Manager 的观望决策，当前招商银行（SH600036）处于
               技术性弱势与市场风格错配的叠加期。虽然具备高股息防御属性，但缺乏
               短期上涨动能。策略上维持空仓或轻仓观望。",
  "action": "HOLD",
  "entryConditions": [
    "股价有效突破40.36元阻力位",
    "成交量显著放大（至少超过近5日均量1.5倍），确认突破有效性",
    "技术指标（MACD/MA）出现金叉信号，确认短期趋势反转"
  ],
  "positionSizing": "当前仓位：0%。若触发进场条件，建议初始仓位控制在5%-10%。",
  "stopLoss": "有效跌破38.01元支撑位，必须无条件止损离场。",
  "takeProfit": "目标价位43.00元附近，或当股息率因股价上涨回落至6%以下时分批止盈。",
  "timeframe": "3-6个月",
  "executionNotes": "严禁在38.01-40.36元区间内进行高频交易。",
  "marketTimingAssessment": "当前市场时机不佳。银行板块在市场风格偏向成长板块时，
                             流动性被虹吸，处于明显的盘整弱势期。"
}
```

### 6.2 🟡 改进点

| 问题 | 建议 |
|---|---|
| `positionSizing` 建议 0% 但又说 5%-10% | 措辞矛盾，应区分"当前建议"与"条件触发后建议" |

---

## 7. Stage 4: RiskDebate

> 23:10:04 → 23:11:21 · 77 秒 · 3 角色 × 3 轮

### 7.1 风险分析师共识总结

| 角色 | 立场 | 核心意见 |
|---|---|---|
| **conservative_risk_analyst** | 完全支持 HOLD | 视为"观察池"非"交易池"；若必须配置，仓位≤5%，硬性止损 5% |
| **neutral_risk_analyst** | 支持 HOLD | 38.01-40.36 区间策略，突破 40.36 再介入 |
| **aggressive_risk_analyst** | 反对纯观望 | 建议分批建仓，38-39 区间左侧布局，目标 45 元+ |

### 7.2 🔴 改进点：与辩论阶段同样的重复问题

三轮风险辩论的内容几乎完全一致：
- conservative 三轮均为 "观察池而非交易池，严禁建仓"
- neutral 三轮均为 "区间策略，突破 40.36 再考虑"
- aggressive 三轮均为 "分批建仓，38 元加大力度"

**与 Stage 2 改进建议相同**——需要强制新论据要求和收敛检测。

---

## 8. Stage 5: PortfolioDecision

> 23:11:21 → 23:11:41 · 20 秒 · 1 个角色 (Sequential)

### 8.1 portfolio_manager

#### LLM 调用

| 字段 | 值 |
|---|---|
| **traceId** | `4cf18bcc4d63440d9837c6088d256ca9` |
| **promptChars** | 49,098 |
| **耗时** | 20,009ms |

#### 角色任务提示词（BackOffice 前缀）

```
你没有直接查询数据的权限。你必须仅基于前序角色已提供的研究报告、
分析数据和讨论记录来完成你的任务。

## 角色：投资组合经理

### 职责
做出最终投资决策和评级，综合所有研究、交易和风险意见形成权威结论。

### 任务
1. 审阅 Trader 的交易提案和三位风险分析师的审查意见。
2. 综合评估研究质量、交易可行性和风险控制。
3. 做出最终投资评级和交易决策。
4. 形成权威投资报告。

### 输出格式
以 JSON 结构输出：
- rating: "Buy" / "Overweight" / "Hold" / "Underweight" / "Sell"
- executiveSummary / investmentThesis / finalDecision / riskConsensus
- dissent / confidence / nextActions / invalidationConditions
```

#### LLM 输出（格式化 JSON）

```json
{
  "rating": "Hold",
  "executiveSummary": "招商银行当前6.62倍的市盈率（PE）并非市场定价错误，
    而是市场对公司进入成熟期、营收增长停滞（0.01%）的理性定价。
    市场已将其从'成长型资产'重估为'类债券型资产'。低估值的核心原因：
    1. 基本面缺乏增长催化剂，市场无法给予成长溢价；
    2. 市场风格偏好虹吸效应，资金流向高弹性成长板块（如逆变器）；
    3. 技术面处于盘整弱势，缺乏上涨动能。",

  "investmentThesis": "招商银行的投资逻辑在于'防御性'而非'进攻性'。
    其7.64%的高股息率构成了极强的安全边际，使其在低利率环境下具备
    类债券的配置价值。然而，在营收与净利润增长近乎停滞的背景下，
    若无宏观利率环境改善或市场风格从成长向价值切换，资本利得空间极为有限。
    在缺乏明确的右侧突破信号（如有效突破40.36元）之前，
    盲目抄底可能陷入'价值陷阱'。",

  "finalDecision": {
    "action": "Hold",
    "targetPrice": 43.00,
    "stopLoss": 38.01,
    "takeProfit": 43.00,
    "positionSize": "0% (建议维持现有仓位，不宜新增)",
    "timeHorizon": "3-6个月"
  },

  "riskConsensus": "主要风险：1. 价值陷阱风险；2. 市场风格持续偏向成长板块
    导致的流动性折价；3. 技术面死叉共振带来的短期下行压力；
    4. 宏观经济下行对净息差的潜在冲击。",

  "dissent": "激进风险分析师建议采取左侧布局策略，认为当前低估值是折价入场券，
    应分批建仓以获取长期股息复利，与当前观望共识存在分歧。",

  "confidence": "高",

  "nextActions": [
    { "action": "监测", "priority": "高",
      "description": "密切关注股价是否有效突破40.36元阻力位" },
    { "action": "评估", "priority": "中",
      "description": "持续跟踪净息差（NIM）变化及宏观利率政策" },
    { "action": "风控", "priority": "高",
      "description": "若股价有效跌破38.01元支撑位，需重新评估防御逻辑" }
  ],

  "invalidationConditions": [
    "股价有效跌破38.01元支撑位，且伴随放量下跌。",
    "公司出现分红政策变更或资产质量恶化公告。",
    "宏观利率环境发生剧烈不利变化，导致银行净息差大幅收窄。"
  ]
}
```

### 8.2 评价

portfolio_manager 的输出是整个管线中质量最高的环节：
- ✅ 直接回答了用户追问（"为什么估值偏低"）
- ✅ 结构化 JSON 输出完整
- ✅ 综合了多方观点并保留了 dissent

---

## 9. MCP 工具调用全局统计

### 9.1 Turn 13 工具调用汇总

| MCP 工具 | API 端点 | 调用次数 | 重试次数 | 失败次数 |
|---|---|---|---|---|
| CompanyOverviewMcp | `/api/stocks/mcp/company-overview` | 4 | 2 | 0 |
| MarketContextMcp | `/api/stocks/mcp/market-context` | 5 | 0 | 0 |
| StockNewsMcp | `/api/stocks/mcp/news` | 5 | 1 | 0 |
| StockSearchMcp | `/api/stocks/mcp/search` | 3 | 0 | 0 |
| StockFundamentalsMcp | `/api/stocks/mcp/fundamentals` | 1 | 0 | 0 |
| SocialSentimentMcp | `/api/stocks/mcp/social-sentiment` | 1 | 0 | 0 |
| StockShareholderMcp | `/api/stocks/mcp/shareholder` | 1 | 1 | 0 |
| **StockProductMcp** | `/api/stocks/mcp/product` | 1 | 1 | **1** |
| StockKlineMcp | `/api/stocks/mcp/kline` | 1 | 0 | 0 |
| StockMinuteMcp | `/api/stocks/mcp/minute` | 1 | 0 | 0 |
| StockStrategyMcp | `/api/stocks/mcp/strategy` | 1 | 0 | 0 |
| **合计** | | **24** | **5** | **1** |

### 9.2 🔴 改进点

| # | 问题 | 建议 |
|---|---|---|
| 1 | **MCP 工具大量重复调用**: CompanyOverviewMcp 被调用 4 次，MarketContextMcp 被调用 5 次 | 在 Stage 1 并行分析师之间共享 MCP 结果缓存 |
| 2 | **StockProductMcp 不稳定**: 重试后仍失败 | 增加熔断器机制；对非核心工具允许优雅降级 |
| 3 | **StockSearchMcp 极慢(27s)**: 显著拖长 Stage 0 耗时 | 单独分析该端点性能瓶颈；考虑设置更短超时 |

---

## 10. LLM 调用全局统计

### 10.1 统计总结

| 指标 | 值 |
|---|---|
| **LLM 总调用次数** | ~27 次 |
| **总耗时** | ~5 分 21 秒 |
| **模型** | gemini-3.1-flash-lite-preview-thinking-high |
| **温度** | 0.3（分析角色）/ 0.1（路由） |
| **providerType** | openai (via api.bltcy.ai 代理) |
| **平均每次 LLM 调用** | promptChars ≈ 35,000 · 耗时 ≈ 12-20s |

### 10.2 Token 消耗估算

| 阶段 | LLM 调用数 | 估计 Prompt Token | 估计 Completion Token |
|---|---|---|---|
| 路由 | 1 | ~2,500 | ~200 |
| Stage 0 | 1 | ~34,000 | ~500 |
| Stage 1 | 6 | ~200,000 | ~4,000 |
| Stage 2 | 9 (3×3) | ~315,000 | ~9,000 |
| Stage 3 | 1 | ~35,000 | ~800 |
| Stage 4 | 9 (3×3) | ~315,000 | ~5,000 |
| Stage 5 | 1 | ~49,000 | ~1,500 |
| **合计** | **~28** | **~950,000** | **~21,000** |

> ⚠️ **近 100 万 prompt token 消耗在一个追问轮**——而路由已判定该追问 "无需重新运行分析链路"。

---

## 11. 综合改进建议

### 🔴 P0 — 关键（严重影响效率与成本）

| # | 问题 | 影响 | 建议 |
|---|---|---|---|
| **P0-1** | ContinueSession 路由被忽略，全链路重跑 | ~100 万 token 浪费，5 分钟不必要等待 | 路由返回 ContinueSession 时，直接由 portfolio_manager 基于已有 blocks + 用户追问生成回复，跳过 Stage 0-4 |
| **P0-2** | 辩论轮次产生高度重复内容 | 3 轮 × 3 角色 = 9 次 LLM 调用输出几乎相同 | 增加论据去重检测 + 收敛终止机制 |
| **P0-3** | MCP 工具重复调用无跨角色缓存 | MarketContextMcp 5 次调用 3 次完全冗余 | 在 pipeline 层实现 MCP result cache per-session |

### 🟡 P1 — 重要（影响质量）

| # | 问题 | 建议 |
|---|---|---|
| **P1-1** | Block summary 包含嵌套 JSON `{"content":"..."}` | Block 写入层自动展平；路由 prompt 中的 latestBlocks 因此可读性极差 |
| **P1-2** | product_analyst 输出价值极低 | 重新设计 prompt 使其产出产品竞争力分析而非经营范围枚举 |
| **P1-3** | 辩论 prompt 未要求反驳互动 | 强制引用对方上一轮论据再提出新反驳 |
| **P1-4** | Trader 的 positionSize 表述矛盾 | prompt 中明确区分"即时建议"和"条件触发后建议"的措辞格式 |

### 🟢 P2 — 优化

| # | 问题 | 建议 |
|---|---|---|
| **P2-1** | StockSearchMcp 太慢(27s) | 后端性能优化或拆分为异步预热 |
| **P2-2** | MCP 重试无指数退避 | 实现 exponential backoff + jitter |
| **P2-3** | portfolio_manager 的 49K prompt 输入过大 | 在传递辩论记录时只保留最后一轮总结 |
| **P2-4** | 路由 prompt 中 latestBlocks 截断为 180 字符 | 对 JSON-heavy blocks 截断效果差，考虑提取 headline + top-3 要点 |

---

## 12. 完整时间线（精简版）

| 时间 | 事件 |
|---|---|
| 23:06:11 | Follow-up 路由 LLM 请求发出 |
| 23:06:20 | 路由响应 → ContinueSession (9.1s) |
| 23:06:20 | Turn 1 started → Stage 0 |
| 23:06:21 | company_overview_analyst started → 4 MCP calls |
| 23:07:23 | company_overview_analyst completed (13.9s LLM) → Stage 1 |
| 23:07:23 | 6 个分析师并行启动，各自调用 MCP 工具 |
| 23:07:37 | ⚠️ product_analyst StockProductMcp retry #2 |
| 23:07:39 | ⚠️ shareholder_analyst StockShareholderMcp retry #2 |
| 23:07:53 | ❌ product_analyst StockProductMcp failed after retries |
| 23:08:42 | Stage 1 Degraded → Stage 2 ResearchDebate |
| 23:08:42 | 辩论 Round 1: RM/Bull/Bear 并行 |
| 23:09:06 | Round 2 |
| 23:09:29 | Round 3 |
| 23:09:51 | Stage 2 Complete → Stage 3 TraderProposal |
| 23:10:04 | Trader completed (12.9s LLM) → Stage 4 RiskDebate |
| 23:10:04 | 风险辩论 Round 1 |
| 23:10:24 | Round 2 |
| 23:11:01 | Round 3 |
| 23:11:21 | Stage 4 Complete → Stage 5 PortfolioDecision |
| 23:11:41 | portfolio_manager completed (20s LLM) — **rating: Hold** |
| 23:11:41 | Turn 1 completed |

---

## 13. 数据来源

| 数据 | 来源路径 |
|---|---|
| Session & Feed Data | `GET http://localhost:5119/api/research/sessions/9` |
| LLM Audit Log | `C:\Users\kong\AppData\Local\SimplerJiangAiAgent\App_Data\logs\llm-requests.txt` |
| Prompt Templates | `backend/.../Modules/Stocks/Prompts/TradingWorkbenchPromptTemplates.cs` |
| Role Contracts | `backend/.../Modules/Stocks/ResearchWorkbench/StockAgentRoleContractRegistry.cs` |
| Routing Logic | `backend/.../Modules/Stocks/ResearchWorkbench/ResearchFollowUpRoutingService.cs` |
| MCP Tool Registry | `backend/.../Modules/Stocks/StockMcpToolNames.cs` |
| MCP API Endpoints | `backend/.../Modules/Stocks/StocksModule.cs` |
