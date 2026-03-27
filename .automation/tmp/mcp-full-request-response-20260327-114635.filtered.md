# MCP 请求与返回（过滤版）

> 来源：`.automation/tmp/mcp-full-request-response-20260327-114635.json`
> 说明：保留请求参数、状态、traceId、关键指标与证据样本；省略超长 K 线/分时点位明细。

总计工具数：**11**

## CompanyOverviewMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/company-overview?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327","symbol":"sh600000"}`

### 返回
 - statusCode: `200`
 - traceId: `ff5da9df1f7b404e924dbcb131b4cae9`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `9841`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "peRatio":  6.65,
    "symbol":  "sh600000",
    "changePercent":  -0.70,
    "price":  9.99,
    "fundamentalFactCount":  33,
    "name":  "浦发银行",
    "quoteTimestamp":  "2026-03-27T03:46:13.7651006Z",
    "businessScope":  "吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。",
    "shareholderCount":  119099,
    "fundamentalUpdatedAt":  "2026-03-27T03:46:23.2211667Z",
    "floatMarketCap":  332725324617.0,
    "sectorName":  "银行"
}
```

### 证据样本（最多 3 条）
1. `浦发银行` | 来源：公司画像缓存 | 时间：2026-03-27T03:46:13.7651006Z
   - 摘要：所属板块=银行; 股东户数=119099; 现价=9.99; 经营范围=吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存...
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockProductMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/product?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327","symbol":"sh600000"}`

### 返回
 - statusCode: `200`
 - traceId: `7b2576dd40dc43008701f3c4f1349c6b`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `414`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol":  "sh600000",
    "industry":  "银行",
    "region":  "上海",
    "sourceSummary":  "东方财富公司概况",
    "factCount":  4,
    "updatedAt":  "2026-03-27T03:46:24.1456703Z",
    "businessScope":  "吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。",
    "csrcIndustry":  "金融业-货币金融服务"
}
```

### 证据样本（最多 3 条）
1. `产品业务概览` | 来源：东方财富公司概况 | 时间：2026-03-27T03:46:24.1456703Z
   - 摘要：经营范围=吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇...
1. `所属行业` | 来源：东方财富公司概况 | 时间：2026-03-27T03:46:24.1456703Z
   - 摘要：银行
1. `证监会行业` | 来源：东方财富公司概况 | 时间：2026-03-27T03:46:24.1456703Z
   - 摘要：金融业-货币金融服务

---

## StockFundamentalsMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/fundamentals?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327","symbol":"sh600000"}`

### 返回
 - statusCode: `200`
 - traceId: `750065ebf467492d95a096f5ce01ec9e`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `420`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "updatedAt":  "2026-03-27T03:46:24.6723384Z",
    "factCount":  28,
    "symbol":  "sh600000"
}
```

### 证据样本（最多 3 条）
1. `公司全称` | 来源：东方财富公司概况 | 时间：2026-03-27T03:46:24.6723384Z
   - 摘要：上海浦东发展银行股份有限公司
1. `英文名称` | 来源：东方财富公司概况 | 时间：2026-03-27T03:46:24.6723384Z
   - 摘要：Shanghai Pudong Development Bank Co.,Ltd.
1. `证券类别` | 来源：东方财富公司概况 | 时间：2026-03-27T03:46:24.6723384Z
   - 摘要：上交所主板A股

---

## StockShareholderMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/shareholder?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327","symbol":"sh600000"}`

### 返回
 - statusCode: `200`
 - traceId: `192d1bf5c59843788cd0565b76a75f8b`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `426`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "updatedAt":  "2026-03-27T03:46:25.1758731Z",
    "shareholderCount":  119099,
    "symbol":  "sh600000",
    "factCount":  5
}
```

### 证据样本（最多 3 条）
1. `股东户数` | 来源：东方财富股东研究 | 时间：2026-03-27T03:46:25.1758731Z
   - 摘要：119099
1. `股东户数统计截止` | 来源：东方财富股东研究 | 时间：2026-03-27T03:46:25.1758731Z
   - 摘要：2025-09-30 00:00:00
1. `股权集中度` | 来源：东方财富股东研究 | 时间：2026-03-27T03:46:25.1758731Z
   - 摘要：非常分散

---

## MarketContextMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/market-context?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327","symbol":"sh600000"}`

### 返回
 - statusCode: `200`
 - traceId: `0748c61e0fb34b499200db8b0651ed15`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `27`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol":  "sh600000",
    "mainlineSectorName":  "锂",
    "suggestedPositionScale":  0.5718,
    "stockSectorName":  "银行",
    "stageConfidence":  60.35,
    "executionFrequencyLabel":  "降低频率",
    "stageLabel":  "分歧",
    "counterTrendWarning":  false,
    "isMainlineAligned":  false,
    "mainlineScore":  63.12,
    "available":  true
}
```

### 证据样本（最多 3 条）
1. `市场阶段` | 来源：IStockMarketContextService | 时间：
   - 摘要：阶段=分歧，置信度=60.35。
1. `板块对齐` | 来源：IStockMarketContextService | 时间：
   - 摘要：个股行业=银行，主线=锂，主线对齐=否。

---

## SocialSentimentMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/social-sentiment?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327","symbol":"sh600000"}`

### 返回
 - statusCode: `200`
 - traceId: `a4d6d5f9094e4422ac5929e48b06269e`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `56`
 - errorCode: `no_live_social_source`
 - freshnessTag: `recent`
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
    "symbol":  "sh600000",
    "latestEvidenceAt":  "2026-03-27T03:46:08.4515587",
    "overallSentiment":  "中性",
    "approximationMode":  "local_news_and_market_proxy",
    "evidenceCount":  9,
    "status":  "degraded",
    "blocked":  false
}
```

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-12T17:19:17
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockKlineMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/kline?symbol=sh600000&interval=day&count=60&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"interval":"day","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327","symbol":"sh600000","count":"60"}`

### 返回
 - statusCode: `200`
 - traceId: `76d94f9138004ab5951c20e57ba04201`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `7889`
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
    "windowSize":  60,
    "symbol":  "sh600000",
    "trendState":  "盘整",
    "interval":  "day",
    "return5dPercent":  1.32,
    "breakoutDistancePercent":  4.70,
    "atrPercent":  1.98,
    "return20dPercent":  3.20
}
```

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-12T17:19:17+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockMinuteMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/minute?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327","symbol":"sh600000"}`

### 返回
 - statusCode: `200`
 - traceId: `f536f9c558294d769426c687fabf9c4d`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `967`
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
    "windowSize":  136,
    "symbol":  "sh600000",
    "intradayRangePercent":  1.50,
    "afternoonDriftPercent":  0,
    "sessionPhase":  "midday_break",
    "openingDrivePercent":  0.60,
    "vwap":  10.0284
}
```

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-12T17:19:17+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockStrategyMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/strategy?symbol=sh600000&interval=day&count=60&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"interval":"day","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327","symbol":"sh600000","count":"60"}`

### 返回
 - statusCode: `200`
 - traceId: `5660ae95e0fd445895cd9755f9c0c5d4`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `993`
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
    "interval":  "day",
    "symbol":  "sh600000"
}
```

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `国有大型银行Ⅲ板块最新轮动排名第14，主线分数44.4，扩散度50` | 来源：本地板块轮动快照 | 时间：2026-03-17T06:41:50.1026352
   - 摘要：国有大型银行Ⅲ板块轮动快照。

---

## StockNewsMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/news?symbol=sh600000&level=stock&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"level":"stock","symbol":"sh600000","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327"}`

### 返回
 - statusCode: `200`
 - traceId: `0f6024256b154e198994fda5efaf3fff`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `10`
 - freshnessTag: `stale`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "latestPublishedAt":  "2026-03-20T17:47:45",
    "level":  "stock",
    "symbol":  "sh600000",
    "itemCount":  20
}
```

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-12T17:19:17
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockSearchMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/search?query=%E6%B5%A6%E5%8F%91%E9%93%B6%E8%A1%8C&trustedOnly=true&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - query: `{"query":"浦发银行","trustedOnly":"true","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327"}`

### 返回
 - statusCode: `200`
 - traceId: `34a3ea50bfb2454592e300eb39ff9319`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-20260327`
 - latencyMs: `0`
 - errorCode: `external_search_unavailable`
 - freshnessTag: `no_data`
 - sourceTier: `external`
 - cache.hit: `False`
 - cache.source: `live`
 - warnings:
   - 外部搜索未启用，StockSearchMcp 当前只返回空结果。请配置 Tavily API Key。
 - degradedFlags:
   - external_search_unavailable

### 关键数据（过滤）
```json
{
    "query":  "浦发银行",
    "provider":  "tavily",
    "trustedOnly":  true,
    "resultCount":  0
}
```

---

