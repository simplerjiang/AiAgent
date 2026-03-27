# MCP 请求与返回（过滤版）

> 来源：`.automation/tmp/mcp-full-request-response-20260327-150832.json`
> 说明：保留请求参数、状态、traceId、关键指标与证据样本；省略超长 K 线/分时点位明细。

## 通用返回字段说明

### 返回区块字段

- `statusCode`：HTTP 返回状态码；`200` 表示接口调用成功。
- `traceId`：本次后端处理请求的唯一追踪标识，用于日志排查与链路审计。
- `taskId`：本轮测试或调用任务的业务标识，便于把一组 MCP 请求串起来看。
- `latencyMs`：接口总耗时，单位毫秒；数值越大说明这次处理越慢。
- `errorCode`：降级或异常的机器可读错误码；为空表示没有显式错误码。
- `freshnessTag`：数据新鲜度标签，常见如 `fresh`、`stale`、`no_data`。
- `sourceTier`：数据来源层级；`local` 表示主要来自本地缓存/本地事实库，`external` 表示依赖外部搜索或外部源。
- `cache.hit`：是否命中缓存；`False` 表示本次不是直接命中已有缓存结果。
- `cache.source`：本次结果的生成来源；`live` 表示在线即时生成。
- `warnings`：非阻塞提醒，说明结果虽然可用，但存在限制或注意事项。
- `degradedFlags`：降级标记，说明结果走了降级路径、能力受限，或部分信号来源不可用。

### 证据样本字段怎么看

- `标题`：证据条目的标题或主题。
- `来源`：证据来自哪个数据源或系统模块。
- `时间`：该证据的发布时间或系统记录时间。
- `摘要`：证据的压缩说明，用于快速理解这条证据讲了什么。

总计工具数：**11**

## CompanyOverviewMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/company-overview?symbol=sh600000&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"symbol":"sh600000","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `d91ff0bb2a3240dfb0dd5ff4daf000be`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `14693`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "name": "浦发银行",
    "sectorName": "银行",
    "price": 10.02,
    "changePercent": -0.4,
    "floatMarketCap": 333724499766.0,
    "peRatio": 6.67,
    "shareholderCount": 119099,
    "quoteTimestamp": "2026-03-27T15:08:37.1949003+08:00",
    "fundamentalUpdatedAt": "2026-03-27T15:08:47.7192268+08:00",
    "fundamentalFactCount": 33,
    "mainBusiness": null,
    "businessScope": "吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。"
}
```

### 数据字段说明

- `symbol`：股票代码，本例为 `sh600000`。
- `name`：股票名称，即公司简称。
- `sectorName`：股票所属板块或行业名称。
- `price`：最新价格。
- `changePercent`：相对前一交易基准的涨跌幅，单位 `%`。
- `floatMarketCap`：流通市值，反映市场上可流通部分的总市值规模。
- `peRatio`：市盈率，用于衡量估值水平。
- `shareholderCount`：股东户数。
- `quoteTimestamp`：这份报价数据的时间戳。
- `fundamentalUpdatedAt`：基本面快照的更新时间。
- `fundamentalFactCount`：本次返回中纳入的结构化基本面事实条数。
- `mainBusiness`：主营业务摘要；`null` 表示当前没有稳定提取到可用主营摘要。
- `businessScope`：经营范围原文或压缩结果，通常比主营业务更长、更偏注册口径。

### 证据样本（最多 3 条）
1. `浦发银行` | 来源：公司画像缓存 | 时间：2026-03-27T15:08:37.1949003+08:00
   - 摘要：所属板块=银行; 股东户数=119099; 现价=10.02; 经营范围=吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockProductMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/product?symbol=sh600000&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"symbol":"sh600000","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `f997d9e2876d46ad9613d3f629b42f6d`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `3680`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "updatedAt": "2026-03-27T15:08:51.4744572+08:00",
    "mainBusiness": null,
    "businessScope": "吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。",
    "industry": "银行",
    "csrcIndustry": "金融业-货币金融服务",
    "region": "上海",
    "factCount": 4,
    "sourceSummary": "东方财富公司概况"
}
```

### 数据字段说明

- `symbol`：股票代码。
- `updatedAt`：产品/业务信息快照更新时间。
- `mainBusiness`：主营业务摘要；`null` 表示当前上游没有稳定返回可用主营摘要。
- `businessScope`：经营范围，偏工商/公司概况口径。
- `industry`：所属行业。
- `csrcIndustry`：证监会行业分类，更适合做监管口径或标准分类对照。
- `region`：公司所属地区或注册地区。
- `factCount`：该 MCP 当前保留并输出的产品/业务相关事实数量。
- `sourceSummary`：这组产品/业务信息的主要来源摘要。

### 证据样本（最多 3 条）
1. `产品业务概览` | 来源：东方财富公司概况 | 时间：2026-03-27T15:08:51.4744572+08:00
   - 摘要：经营范围=吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。; 所属行业=银行; 所属地区=上海
1. `所属行业` | 来源：东方财富公司概况 | 时间：2026-03-27T15:08:51.4744572+08:00
   - 摘要：银行
1. `证监会行业` | 来源：东方财富公司概况 | 时间：2026-03-27T15:08:51.4744572+08:00
   - 摘要：金融业-货币金融服务

---

## StockFundamentalsMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/fundamentals?symbol=sh600000&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"symbol":"sh600000","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `8687af9917684aa3b4d2aeccc17fcf48`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `3706`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "updatedAt": "2026-03-27T15:08:55.2097646+08:00",
    "factCount": 28
}
```

### 数据字段说明

- `symbol`：股票代码。
- `updatedAt`：这份基本面快照的更新时间。
- `factCount`：纳入返回的基本面事实总数，例如公司全称、财报项、估值项等。

### 证据样本（最多 3 条）
1. `公司全称` | 来源：东方财富公司概况 | 时间：2026-03-27T15:08:55.2097646+08:00
   - 摘要：上海浦东发展银行股份有限公司
1. `英文名称` | 来源：东方财富公司概况 | 时间：2026-03-27T15:08:55.2097646+08:00
   - 摘要：Shanghai Pudong Development Bank Co.,Ltd.
1. `证券类别` | 来源：东方财富公司概况 | 时间：2026-03-27T15:08:55.2097646+08:00
   - 摘要：上交所主板A股

---

## StockShareholderMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/shareholder?symbol=sh600000&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"symbol":"sh600000","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `032838c76dfe47fcb1a21f1982fbde66`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `10098`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "shareholderCount": 119099,
    "updatedAt": "2026-03-27T15:09:05.3378282+08:00",
    "factCount": 5
}
```

### 数据字段说明

- `symbol`：股票代码。
- `shareholderCount`：股东户数。
- `updatedAt`：股东相关信息的更新时间。
- `factCount`：股东维度输出的结构化事实条数。

### 证据样本（最多 3 条）
1. `股东户数` | 来源：东方财富股东研究 | 时间：2026-03-27T15:09:05.3378282+08:00
   - 摘要：119099
1. `股东户数统计截止` | 来源：东方财富股东研究 | 时间：2026-03-27T15:09:05.3378282+08:00
   - 摘要：2025-09-30 00:00:00
1. `股权集中度` | 来源：东方财富股东研究 | 时间：2026-03-27T15:09:05.3378282+08:00
   - 摘要：非常分散

---

## MarketContextMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/market-context?symbol=sh600000&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"symbol":"sh600000","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `a8f026cb009b42bead02cec2d1fc35cb`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `16`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "available": true,
    "stageLabel": null,
    "stageConfidence": 60.35,
    "stockSectorName": "银行",
    "mainlineSectorName": "阿兹海默",
    "sectorCode": null,
    "mainlineScore": 84.91,
    "suggestedPositionScale": 0.5718,
    "executionFrequencyLabel": null,
    "counterTrendWarning": false,
    "isMainlineAligned": false
}
```

### 数据字段说明

- `symbol`：股票代码。
- `available`：市场环境上下文是否可用；`true` 表示当前成功生成。
- `stageLabel`：市场阶段标签，如分歧、发酵、退潮等；`null` 表示当前没有稳定可输出的阶段文案。
- `stageConfidence`：市场阶段判断的置信度。
- `stockSectorName`：该股票所属行业/板块。
- `mainlineSectorName`：当前系统判断的市场主线板块。
- `sectorCode`：主线板块代码；`null` 表示当前无稳定代码。
- `mainlineScore`：主线强度或主线评分。
- `suggestedPositionScale`：建议仓位系数，用于表达当前市场环境下的仓位放大或收缩建议。
- `executionFrequencyLabel`：建议交易频率标签；`null` 表示当前没有稳定频率文案。
- `counterTrendWarning`：是否存在逆势交易警告。
- `isMainlineAligned`：个股所属板块是否与当前市场主线一致。

### 证据样本（最多 3 条）
1. `板块对齐` | 来源：IStockMarketContextService | 时间：
   - 摘要：个股行业=银行，主线=阿兹海默，主线对齐=否。

---

## SocialSentimentMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/social-sentiment?symbol=sh600000&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"symbol":"sh600000","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `97f2313061ac4ac8a352986794e8ffa7`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `3118`
 - errorCode: `no_live_social_source`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`
 - warnings:
   - SocialSentimentMcp v1 仅基于本地新闻情绪与市场代理情绪，不代表真实社媒情绪覆盖。
 - degradedFlags:
   - no_live_social_source
   - degraded.local_news_and_market_proxy

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "status": "degraded",
    "blocked": false,
    "blockedReason": null,
    "approximationMode": "local_news_and_market_proxy",
    "overallSentiment": "中性",
    "evidenceCount": 9,
    "latestEvidenceAt": "2026-03-27T15:09:05.5016595+08:00"
}
```

### 数据字段说明

- `symbol`：股票代码。
- `status`：当前社交情绪契约状态；`degraded` 表示可用但走了降级路径。
- `blocked`：是否被阻断；`false` 表示虽然降级，但仍能返回结果。
- `blockedReason`：若被阻断，这里会给出原因；当前为 `null`。
- `approximationMode`：情绪近似模式；这里的 `local_news_and_market_proxy` 表示结果由本地新闻情绪和市场代理情绪共同近似得出。
- `overallSentiment`：综合情绪结论。
- `evidenceCount`：用于支撑该情绪结论的证据数量。
- `latestEvidenceAt`：这批证据中最新一条证据的时间。

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-12T17:19:17.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockKlineMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/kline?symbol=sh600000&interval=day&count=60&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"symbol":"sh600000","interval":"day","count":"60","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `88613b289d6e49318b94e0e8b7baebec`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `13311`
 - errorCode: `market_noise_filtered`
 - freshnessTag: `stale`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`
 - degradedFlags:
   - market_noise_filtered
   - expanded_news_window

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "interval": "day",
    "windowSize": 60,
    "trendState": "盘整",
    "return5dPercent": 1.62,
    "return20dPercent": 3.51,
    "atrPercent": 1.97,
    "breakoutDistancePercent": 4.39
}
```

### 数据字段说明

- `symbol`：股票代码。
- `interval`：K 线周期；`day` 表示日线。
- `windowSize`：本次分析使用的 K 线窗口数量。
- `trendState`：趋势状态，如上涨、下跌、盘整。
- `return5dPercent`：近 5 个交易日收益率，单位 `%`。
- `return20dPercent`：近 20 个交易日收益率，单位 `%`。
- `atrPercent`：ATR 波动率占价格的百分比，用来衡量波动强弱。
- `breakoutDistancePercent`：当前价格距离关键突破位的百分比距离。

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-12T17:19:17.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockMinuteMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/minute?symbol=sh600000&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"symbol":"sh600000","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `b3b8b0b4d0f24976b3ea72ab5125c8c1`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `12779`
 - errorCode: `market_noise_filtered`
 - freshnessTag: `stale`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`
 - degradedFlags:
   - market_noise_filtered
   - expanded_news_window

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "sessionPhase": "post_market",
    "windowSize": 256,
    "vwap": 10.0258,
    "openingDrivePercent": 0.6,
    "afternoonDriftPercent": 0.3,
    "intradayRangePercent": 1.5
}
```

### 数据字段说明

- `symbol`：股票代码。
- `sessionPhase`：当前分时所处交易阶段，如盘中、午间、收盘后等；`post_market` 表示当前属于收盘后阶段。
- `windowSize`：本次返回的分时点数量。
- `vwap`：成交量加权平均价。
- `openingDrivePercent`：开盘后价格向上或向下第一段驱动的幅度。
- `afternoonDriftPercent`：午后阶段相对午间锚点的漂移幅度。
- `intradayRangePercent`：日内最高价与最低价之间的振幅，单位 `%`。

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-12T17:19:17.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockStrategyMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/strategy?symbol=sh600000&interval=day&count=60&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"symbol":"sh600000","interval":"day","count":"60","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `aab2359db51144d1ab1ef7676833e3c0`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `42603`
 - errorCode: `market_noise_filtered`
 - freshnessTag: `stale`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`
 - degradedFlags:
   - market_noise_filtered
   - expanded_news_window

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "interval": "day"
}
```

### 数据字段说明

- `symbol`：股票代码。
- `interval`：策略分析所基于的周期；这里是 `day`，表示按日线策略信号计算。

> 说明：过滤版这里只保留了最顶层标量字段；更完整的策略返回通常还会包含具体信号列表，如 MA、MACD、RSI、TD 等。

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `国有大型银行Ⅲ板块最新轮动排名第14，主线分数44.4，扩散度50` | 来源：本地板块轮动快照 | 时间：2026-03-17T14:41:50.1026352+08:00
   - 摘要：国有大型银行Ⅲ板块轮动快照。

---

## StockNewsMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/news?symbol=sh600000&level=stock&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"symbol":"sh600000","level":"stock","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `07c562edaa6e44dba0a9e015ef32bd40`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `3498`
 - freshnessTag: `stale`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "level": "stock",
    "itemCount": 20,
    "latestPublishedAt": "2026-03-20T17:47:45.0000000+08:00"
}
```

### 数据字段说明

- `symbol`：股票代码。
- `level`：新闻层级；`stock` 表示个股级新闻。
- `itemCount`：本次保留并返回的新闻条数。
- `latestPublishedAt`：当前新闻结果里最新一条新闻的发布时间。

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-12T17:19:17.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockSearchMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/search?query=%E6%B5%A6%E5%8F%91%E9%93%B6%E8%A1%8C&trustedOnly=true&taskId=TESTAGENT-MCP-RERUN-20260327-150829`
 - query: `{"query":"浦发银行","trustedOnly":"true","taskId":"TESTAGENT-MCP-RERUN-20260327-150829"}`

### 返回
 - statusCode: `200`
 - traceId: `bc3c4961af1b4e9dafec009c1afb5229`
 - taskId: `TESTAGENT-MCP-RERUN-20260327-150829`
 - latencyMs: `2`
 - errorCode: `external_search_unavailable`
 - freshnessTag: `no_data`
 - sourceTier: `external`
 - cache.hit: `False`
 - cache.source: `live`
 - warnings:
   - 外部搜索未启用，StockSearchMcp 当前只返回空结果。请在 LLM 设置中配置 Tavily API Key。
 - degradedFlags:
   - external_search_unavailable

### 关键数据（过滤）
```json
{
    "query": "浦发银行",
    "provider": "tavily",
    "trustedOnly": true,
    "resultCount": 0
}
```

### 数据字段说明

- `query`：原始搜索词。
- `provider`：当前外部搜索提供方；这里是 `tavily`。
- `trustedOnly`：是否只保留可信来源结果。
- `resultCount`：搜索结果数量；`0` 表示本次没有返回任何外部搜索结果。

---

## 2026-03-27 修复增量说明

> 说明：本节基于本次重新抓取的**当前在线真实返回**整理，不再引用修复前样本。

- 本次重跑 TaskId：`TESTAGENT-MCP-RERUN-20260327-150829`
- 定向单测命令：`dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter FullyQualifiedName~StockCopilotMcpServiceTests`
- 单测结果：**33/33 通过，0 失败**

### 已在最新返回中确认的修复结果

1. `MarketContextMcp.data.stageLabel = null`，`executionFrequencyLabel = null`。
2. `StockSearchMcp` 当前 warning 已更新为：`外部搜索未启用，StockSearchMcp 当前只返回空结果。请在 LLM 设置中配置 Tavily API Key。`。
3. 本报告上文全部 11 个 MCP 样本均来自本次在线重抓，已与当前代码行为同步。
