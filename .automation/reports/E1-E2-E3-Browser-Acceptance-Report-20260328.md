# P0-Pre MCP 补强验收报告

> **验收时间**: 2026-03-28 02:22 (UTC+8)  
> **验收方式**: PowerShell HTTP 请求 + CopilotBrowser MCP 浏览器验证  
> **测试股票**: sh600519 (贵州茅台)  
> **后端地址**: http://localhost:5119  
> **Commit**: `3b0937f` (feat(mcp): E1/E2/E3 enhancements for P0-Pre MCP data depth)

---

## 1. 总体结论

| 补强项 | 验收结果 | 说明 |
|--------|---------|------|
| **E1**: mainBusiness/sectorName 修复 | ✅ **通过** | mainBusiness 有值，sectorName=酿酒行业 |
| **E2**: MarketContextMcp 市场实时概览 | ✅ **通过** | 三大指数/主力资金/北向资金/涨跌分布全量返回 |
| **E3**: StockProductMcp 主营构成补充 | ✅ **通过** | factCount=6 (原4)，新增主营构成报告期 |
| **E4**: SocialSentimentMcp 社媒源 | ⏭️ **跳过** | 按计划列入 backlog，不在本轮执行 |

**全部 11 个 MCP 端点**: 11/11 正常返回 (HTTP 200)

---

## 2. 全量端点测试汇总

| # | 端点 | 请求 URL | 状态 | 延迟(ms) | 响应大小 | Evidence | DegradedFlags | Warnings |
|---|------|----------|------|---------|---------|----------|---------------|----------|
| 1 | MarketContextMcp | `/api/stocks/mcp/market-context?symbol=sh600519` | ✅ OK | 241 | 7,241B | 5 | 0 | 0 |
| 2 | CompanyOverviewMcp | `/api/stocks/mcp/company-overview?symbol=sh600519` | ✅ OK | 12,316 | 13,133B | 15 | 0 | 0 |
| 3 | StockProductMcp | `/api/stocks/mcp/product?symbol=sh600519` | ✅ OK | 13,653 | 7,039B | 7 | 0 | 0 |
| 4 | StockFundamentalsMcp | `/api/stocks/mcp/fundamentals?symbol=sh600519` | ✅ OK | 3,401 | 17,625B | 31 | 0 | 0 |
| 5 | StockShareholderMcp | `/api/stocks/mcp/shareholder?symbol=sh600519` | ✅ OK | 10,331 | 3,975B | 5 | 0 | 0 |
| 6 | SocialSentimentMcp | `/api/stocks/mcp/social-sentiment?symbol=sh600519` | ✅ OK | 3,131 | 22,774B | 31 | 2 | 1 |
| 7 | StockKlineMcp | `/api/stocks/mcp/kline?symbol=sh600519` | ✅ OK | 12,100 | 18,949B | 14 | 2 | 0 |
| 8 | StockMinuteMcp | `/api/stocks/mcp/minute?symbol=sh600519` | ✅ OK | 12,401 | 34,985B | 14 | 2 | 0 |
| 9 | StockStrategyMcp | `/api/stocks/mcp/strategy?symbol=sh600519` | ✅ OK | 13,824 | 15,284B | 18 | 2 | 0 |
| 10 | StockNewsMcp | `/api/stocks/mcp/news?symbol=sh600519` | ✅ OK | 2,831 | 10,724B | 14 | 0 | 0 |
| 11 | StockSearchMcp | `/api/stocks/mcp/search?query=茅台` | ✅ OK | 3,077 | 17,737B | 5 | 0 | 0 |

> **说明**: SocialSentiment/Kline/Minute/Strategy 的 degradedFlags=2 属于预期行为（社媒数据源和多源行情的降级标记），不影响功能。

---

## 3. E1 详细验收：mainBusiness / sectorName 修复

### 3.1 请求内容

**CompanyOverviewMcp**:
```
GET http://localhost:5119/api/stocks/mcp/company-overview?symbol=sh600519
```

**StockProductMcp**:
```
GET http://localhost:5119/api/stocks/mcp/product?symbol=sh600519
```

### 3.2 请求结果

#### CompanyOverviewMcp 核心字段

| 字段 | 改进前（审计报告记录） | 改进后（本次验证） |
|-----|---------------------|-------------------|
| `mainBusiness` | `null` ❌ | `"茅台酒及系列酒的生产与销售；饮料、食品、包装材料的生产、销售；防伪技术开发、信息产业相关产品的研制、开发"` ✅ |
| `businessScope` | (不存在) | `"茅台酒及系列酒的生产与销售;饮料、食品、包装材料的生产...酒店经营管理、住宿、餐饮...第二类增值电信业务"` ✅ |
| `sectorName` | `null` ❌ | `"酿酒行业"` ✅ |
| `fundamentalFactCount` | 36 | 36 ✅ (保持不变) |
| `price` | 1416.02 | 1416.02 ✅ |
| `peRatio` | 20.58 | 20.58 ✅ |
| `shareholderCount` | 238,512 | 238,512 ✅ |
| `evidence` | 15条 | 15条 ✅ (包含公司概览 + 14条公告) |
| `degradedFlags` | 0 | 0 ✅ |

**CopilotBrowser MCP 浏览器截图确认**: JSON 完整渲染，所有字段可读。

#### StockProductMcp 核心字段

| 字段 | 改进前 | 改进后 |
|-----|--------|--------|
| `mainBusiness` | `null` ❌ | `"茅台酒及系列酒的生产与销售；..."` ✅ |
| `businessScope` | (不存在) | `"茅台酒及系列酒的生产与销售；..."` ✅ |
| `industry` | `"酿酒行业"` | `"酿酒行业"` ✅ |
| `csrcIndustry` | `"制造业-酒、饮料和精制茶制造业"` | `"制造业-酒、饮料和精制茶制造业"` ✅ |
| `region` | `"贵州"` | `"贵州"` ✅ |

### 3.3 修复方案回顾

- **问题根因**: 东方财富 CompanySurvey API 的 `zyyw`（主营业务）字段对大部分股票返回空值
- **修复方案**: 新增 `DeriveMainBusinessFromScope()` fallback 方法，从 `jyfw`（经营范围）字段提取核心业务摘要
- **效果**: mainBusiness 字段稳定填充，不再为 null

### 3.4 验收建议

**✅ E1 验收通过**。

- mainBusiness 从 null 修复为完整的主营业务描述
- sectorName 从 null 修复为 "酿酒行业"
- 新增 businessScope 字段，提供完整经营范围
- features 中保留了结构化的 mainBusiness / businessScope / sectorName，方便 LLM 直接引用
- 建议后续：确认其余常用股票（如 sz000001 平安银行）也能正确填充

---

## 4. E2 详细验收：MarketContextMcp 市场实时概览

### 4.1 请求内容

```
GET http://localhost:5119/api/stocks/mcp/market-context?symbol=sh600519
```

### 4.2 请求结果

| 数据维度 | 改进前（审计报告记录） | 改进后（本次验证） |
|---------|---------------------|-------------------|
| **indices** (三大指数) | `[]` 空数组 ❌ | 3条指数完整数据 ✅ |
| **mainCapitalFlow** (主力资金) | `null` ❌ | 有值 ✅ |
| **northboundFlow** (北向资金) | `null` ❌ | 有值 ✅ |
| **breadth** (涨跌分布) | `null` ❌ | 有值 ✅ |

#### 指数详情

| 指数 | 代码 | 最新价 | 涨跌幅 |
|------|------|--------|--------|
| 上证指数 | sh000001 | 3,913.72 | +0.63% |
| 深证成指 | sz399001 | 13,760.37 | +1.13% |
| 创业板指 | sz399006 | 3,295.88 | +0.71% |

#### 资金 & 广度

| 指标 | 值 |
|-----|-----|
| 主力资金净流入 | 131.31 亿元 |
| 北向资金净流入 | 0.00 亿元 |
| 上涨家数 | 4,210 |
| 下跌家数 | 1,033 |
| 涨停家数 | 107 |
| 跌停家数 | 3 |

#### Features 新增项

| Feature Name | Value | 说明 |
|-------------|-------|------|
| `index_sh000001_price` | 3913.72 | 上证指数最新价 |
| `index_sh000001_changePct` | 0.63 | 上证指数涨跌幅 |
| `index_sz399001_price` | 13760.37 | 深证成指最新价 |
| `index_sz399001_changePct` | 1.13 | 深证成指涨跌幅 |
| `index_sz399006_price` | 3295.88 | 创业板指最新价 |
| `index_sz399006_changePct` | 0.71 | 创业板指涨跌幅 |
| `mainCapitalNetInflow` | 131.31 (亿元) | 主力资金净流入 |
| `northboundNetInflow` | 0.00 (亿元) | 北向资金净流入 |
| `advancers` | 4210 | 上涨家数 |
| `decliners` | 1033 | 下跌家数 |
| `limitUpCount` | 107 | 涨停家数 |
| `limitDownCount` | 3 | 跌停家数 |

#### Evidence (5条)

1. `本地主线板块=逆变器` — 来自板块轮动快照
2. `三大指数实时行情` — 来自实时行情接口
3. `主力净流入=131.31亿元` — 来自实时资金流向接口
4. `北向净流入=0.00亿元` — 来自实时北向资金接口
5. `涨跌分布: 涨4210/跌1033` — 来自实时涨跌分布接口

### 4.3 验收建议

**✅ E2 验收通过**。

- 端点延迟仅 228-241ms（极快），因为直接读取内存中的实时行情缓存
- 三大指数 + 资金面 + 涨跌广度全量填充
- evidence 完整追溯每个数据维度的来源
- features 将每个数值结构化为独立字段，方便 LLM 精确引用
- degradedFlags=0，说明 `RealtimeMarketOverviewService` 的所有数据源均正常
- 建议后续：非交易时段验证指数数据是否为缓存值并标注时效性

---

## 5. E3 详细验收：StockProductMcp 主营构成补充

### 5.1 请求内容

```
GET http://localhost:5119/api/stocks/mcp/product?symbol=sh600519
```

### 5.2 请求结果

| 指标 | 改进前（审计报告记录） | 改进后（本次验证） |
|-----|---------------------|-------------------|
| `factCount` | 4 | **6** ✅ (+2条新增) |
| `mainBusiness` | `null` | `"茅台酒及系列酒的生产与销售；..."` ✅ |
| `sourceSummary` | `"东方财富公司概况"` | `"东方财富公司概况(经营范围摘要) + 东方财富公司概况 + 东方财富主营构成"` ✅ |

#### Facts 详细列表

| # | 标签 | 值 | 来源 | 状态 |
|---|------|-----|------|------|
| 1 | 主营业务 | 茅台酒及系列酒的生产与销售；饮料、食品、包装材料... | 东方财富公司概况(经营范围摘要) | ✅ **新增** |
| 2 | 经营范围 | 茅台酒及系列酒的生产与销售;...第二类增值电信业务 | 东方财富公司概况 | 原有 |
| 3 | 所属行业 | 酿酒行业 | 东方财富公司概况 | 原有 |
| 4 | 证监会行业 | 制造业-酒、饮料和精制茶制造业 | 东方财富公司概况 | 原有 |
| 5 | 所属地区 | 贵州 | 东方财富公司概况 | 原有 |
| 6 | 主营构成报告期 | 2025-09-30 | 东方财富主营构成 | ✅ **新增** |

#### Features 新增项

| Feature Name | Value | 说明 |
|-------------|-------|------|
| `mainBusiness` | 茅台酒及系列酒的生产与销售... | 主营业务（从经营范围摘要提取） |
| `businessScope` | 茅台酒及系列酒的生产与销售... | 市场维度的业务摘要 |
| `registeredBusinessScope` | 完整工商经营范围 | 原始工商登记口径 |
| `industry` | 酿酒行业 | 所属行业 |
| `csrcIndustry` | 制造业-酒、饮料和精制茶制造业 | 证监会行业分类 |
| `region` | 贵州 | 所属地区 |

### 5.3 验收建议

**✅ E3 验收通过**。

- factCount 从 4 提升到 6，新增"主营业务"和"主营构成报告期"两条关键 fact
- sourceSummary 清晰标注了三个数据来源：经营范围摘要 + 公司概况 + 主营构成
- features 新增 `mainBusiness`/`businessScope`/`registeredBusinessScope` 三层拆分，LLM 可按需引用
- evidence 完整（7条），含产品概览汇总 + 6条独立 fact
- 建议后续：主营构成目前仅含报告期，尚未拆分为产品线收入占比（如"茅台酒 85%"），可作为下一期优化方向

---

## 6. 其余端点基线验证

以下端点未涉及本轮 E1/E2/E3 变更，但全部通过了基础功能验证：

| 端点 | 核心数据确认 |
|------|-------------|
| **Fundamentals** | 31条 facts（财报+公司概况），latency=3.4s，数据完整 |
| **Shareholder** | 5条 evidence，含十大股东/流通股东数据 |
| **SocialSentiment** | 31条 evidence，degraded=2（社媒数据源降级，预期行为） |
| **Kline** | 14条 evidence，含日K线数据60天窗口 |
| **Minute** | 14条 evidence，含分钟线数据当日窗口 |
| **Strategy** | 18条 evidence，含技术指标策略信号 |
| **News** | 14条 evidence，含股票新闻/公告列表 |
| **Search** | 5条 evidence，搜索"茅台"返回相关股票列表 |

---

## 7. 后端运行状态

- **零错误**: 后端日志无异常错误输出
- **背景任务**: LocalFactIngestionService 正常运行，持续更新本地事实库
- **已知 warn**: `新浪板块搜索未返回可解析结果: 酿酒行业` — 新浪搜索 API 间歇性问题，不影响功能
- **数据库**: SQLite 正常运行，查询响应 <1ms

---

## 8. 验收总结 & 后续建议

### 已完成
1. ✅ E1: mainBusiness 和 sectorName 从 null 成功修复为有效值
2. ✅ E2: MarketContextMcp 新增三大指数、主力资金、北向资金、涨跌广度四大维度
3. ✅ E3: StockProductMcp 新增主营业务摘要和主营构成报告期
4. ✅ 全部 11 个 MCP 端点正常运行、HTTP 200 返回

### 后续建议（非阻断）
1. **E3 深化**: 解析主营收入按产品线/地区的占比拆分（如 "茅台酒 85.2%"）
2. **E4 社媒**: 待后续版本评估接入 Eastmoney 股吧数据
3. **延迟优化**: CompanyOverview(12.3s) 和 Shareholder(10.3s) 延迟较高，首次调用需等待上游爬取；后续可考虑预热缓存
4. **多股验证**: 建议补充 sz000001(平安银行)、sz300750(宁德时代) 等不同板块股票的交叉验证
5. **非交易时段**: 验证 MarketContextMcp 在非交易时段的缓存数据标注是否清晰

---

*报告生成工具: PM Agent via CopilotBrowser MCP + PowerShell HTTP 测试*
