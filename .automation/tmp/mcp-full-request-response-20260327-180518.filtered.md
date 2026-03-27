# MCP 请求与返回（过滤版）

> 来源：`.automation/tmp/mcp-full-request-response-20260327-180518.json`
> 说明：保留请求参数、状态、traceId、关键指标与证据样本；省略超长 K 线/分时点位明细。

总计工具数：**11**

## CompanyOverviewMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/company-overview?symbol=sh600000&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"symbol":"sh600000","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `6f3525865dce4e8d921299d157e96002`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `17621`
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
    "quoteTimestamp": "2026-03-27T18:05:25.6695899+08:00",
    "fundamentalUpdatedAt": "2026-03-27T18:05:36.7045114+08:00",
    "fundamentalFactCount": 33,
    "mainBusiness": null,
    "businessScope": "吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。"
}
```

### 证据样本（最多 3 条）
1. `浦发银行` | 来源：公司画像缓存 | 时间：2026-03-27T18:05:25.6695899+08:00
   - 摘要：所属板块=银行; 股东户数=119099; 现价=10.02; 经营范围=吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockProductMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/product?symbol=sh600000&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"symbol":"sh600000","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `fd8ad46b200848cf90997af536620f25`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `6058`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "updatedAt": "2026-03-27T18:05:40.3714756+08:00",
    "mainBusiness": null,
    "businessScope": "以银行相关业务为主",
    "industry": "银行",
    "csrcIndustry": "金融业-货币金融服务",
    "region": "上海",
    "factCount": 4,
    "sourceSummary": "东方财富公司概况"
}
```

### 证据样本（最多 3 条）
1. `产品业务概览` | 来源：东方财富公司概况 + 市场归纳LLM | 时间：2026-03-27T18:05:40.3714756+08:00
   - 摘要：市场认可方向=银行股稳健估值提升 / 金融板块景气度改善; 业务摘要=以银行相关业务为主; 所属行业=银行; 所属地区=上海
1. `经营范围` | 来源：东方财富公司概况 | 时间：2026-03-27T18:05:40.3714756+08:00
   - 摘要：吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。
1. `所属行业` | 来源：东方财富公司概况 | 时间：2026-03-27T18:05:40.3714756+08:00
   - 摘要：银行

---

## StockFundamentalsMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/fundamentals?symbol=sh600000&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"symbol":"sh600000","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `9d8a3a00a50846a1b5fa70d455a2c7b8`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `3679`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "updatedAt": "2026-03-27T18:05:46.5586689+08:00",
    "factCount": 28
}
```

### 证据样本（最多 3 条）
1. `最新财报期` | 来源：东方财富最新财报 | 时间：2026-03-27T18:05:46.5586689+08:00
   - 摘要：2025三季报
1. `营业收入` | 来源：东方财富最新财报 | 时间：2026-03-27T18:05:46.5586689+08:00
   - 摘要：1322.8亿元
1. `归属净利润` | 来源：东方财富最新财报 | 时间：2026-03-27T18:05:46.5586689+08:00
   - 摘要：388.19亿元

---

## StockShareholderMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/shareholder?symbol=sh600000&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"symbol":"sh600000","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `333377736d1c40dcac22fe2c5b4dbd59`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `10371`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "shareholderCount": 119099,
    "updatedAt": "2026-03-27T18:05:56.9506134+08:00",
    "factCount": 5
}
```

### 证据样本（最多 3 条）
1. `股东户数` | 来源：东方财富股东研究 | 时间：2026-03-27T18:05:56.9506134+08:00
   - 摘要：119099
1. `股东户数统计截止` | 来源：东方财富股东研究 | 时间：2026-03-27T18:05:56.9506134+08:00
   - 摘要：2025-09-30 00:00:00
1. `股权集中度` | 来源：东方财富股东研究 | 时间：2026-03-27T18:05:56.9506134+08:00
   - 摘要：非常分散

---

## MarketContextMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/market-context?symbol=sh600000&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"symbol":"sh600000","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `186768e1f35f4dff9f759bf3bcfac873`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `6`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "available": true,
    "stageConfidence": 57.23,
    "stockSectorName": "银行",
    "mainlineSectorName": "东数西算",
    "sectorCode": null,
    "mainlineScore": 86.24
}
```

### 证据样本（最多 3 条）
1. `本地个股行业上下文` | 来源：本地市场上下文 | 时间：
   - 摘要：该字段来自本地市场上下文，不是东方财富公司概况字段。个股行业=银行，行业代码=未知。
1. `本地主线板块轮动` | 来源：本地市场上下文/板块轮动 | 时间：
   - 摘要：该字段来自本地市场上下文/板块轮动快照，不是东方财富字段。当前主线=东数西算，主线强度=86.24，阶段置信度=57.23。

---

## SocialSentimentMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/social-sentiment?symbol=sh600000&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"symbol":"sh600000","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `7932c3046a1248f98727afd837d1a737`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `3478`
 - errorCode: `no_live_social_source`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`
 - warnings:
   - SocialSentimentMcp v1 是本地情绪相关证据聚合工具，仅汇总本地新闻与市场代理快照，不会自行给出社交情绪结论。
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
    "evidenceCount": 34,
    "latestEvidenceAt": "2026-03-27T18:05:57.0721425+08:00"
}
```

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
 - url: `http://localhost:5119/api/stocks/mcp/kline?symbol=sh600000&interval=day&count=60&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"symbol":"sh600000","interval":"day","count":"60","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `1667e28598484b94ae4292455652d73e`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `13481`
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
 - url: `http://localhost:5119/api/stocks/mcp/minute?symbol=sh600000&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"symbol":"sh600000","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `e5d4e5c2637544c2b4a95ee553168638`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `12823`
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
    "vwap": 10.0256,
    "openingDrivePercent": 0.6,
    "afternoonDriftPercent": 0.2,
    "intradayRangePercent": 1.5
}
```

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
 - url: `http://localhost:5119/api/stocks/mcp/strategy?symbol=sh600000&interval=day&count=60&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"symbol":"sh600000","interval":"day","count":"60","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `1fa97047300549afb8a3a0a7e5b96b9d`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `13356`
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

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-20T17:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-26T18:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-12T17:19:17.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockNewsMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/news?symbol=sh600000&level=stock&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"symbol":"sh600000","level":"stock","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `021a5f9a38504088bd822615d3c139a4`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `3865`
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
 - url: `http://localhost:5119/api/stocks/mcp/search?query=%E6%B5%A6%E5%8F%91%E9%93%B6%E8%A1%8C&trustedOnly=true&taskId=MANUAL-CHECK-20260327-180518`
 - query: `{"query":"浦发银行","trustedOnly":"true","taskId":"MANUAL-CHECK-20260327-180518"}`

### 返回
 - statusCode: `200`
 - traceId: `8a8958766ec045dabf063cad6393b968`
 - taskId: `MANUAL-CHECK-20260327-180518`
 - latencyMs: `3542`
 - freshnessTag: `fresh`
 - sourceTier: `external`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "query": "浦发银行",
    "provider": "tavily",
    "trustedOnly": true,
    "resultCount": 5
}
```

### 证据样本（最多 3 条）
1. `上海浦东发展银行股份有限公司_百度百科` | 来源：baike.baidu.com | 时间：
   - 摘要：2007年11月25日首次由中国境内媒体和学术研究机构联手进行的“2006亚洲银行竞争力排名”在京揭晓，该评选活动由香港中文大学工商管理学院、北京大学光华管理学院和南方报业传媒集团共同推出。浦发银行名列五十强之列。 2007年10月30日...
1. `上海浦東發展銀行- 維基百科` | 来源：zh.wikipedia.org | 时间：
   - 摘要：位于上海浦东新区浦发大厦的上海分行营业部 .jpg) 位于香港灣仔浦發銀行大廈的香港分行 深圳分行 上海浦东发展银行股份有限公司（上交所：600000，简称：浦发银行）是中华人民共和国的一家全国性股份制商业银行，创立于1992年8月28日...
1. `网上银行- 浦发银行官网` | 来源：per.spdb.com.cn | 时间：
   - 摘要：大额汇款安全便捷，网上银行为您护航！上班理财两不误，大屏幕阅读更舒适，交易记录可打印，网上银行带您赚钱带您飞！

---

## 2026-03-27 修复增量说明

> 说明：本节基于本次重新抓取的**当前在线真实返回**整理，不再引用修复前样本。

- 本次重跑 TaskId：`MANUAL-CHECK-20260327-180518`
- 定向单测命令：`dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --filter FullyQualifiedName~StockCopilotMcpServiceTests (latest known)`
- 单测结果：**latest known: 41/41 passed**

### 已在最新返回中确认的修复结果

1. `MarketContextMcp.data.stageLabel = null`，`executionFrequencyLabel = null`。
2. `StockSearchMcp` 当前 warning 已更新为：``。
3. 本报告上文全部 11 个 MCP 样本均来自本次在线重抓，已与当前代码行为同步。
