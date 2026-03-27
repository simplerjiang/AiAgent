# MCP 请求与返回（过滤版）

> 来源：`D:/SimplerJiangAiAgent/.automation/tmp/mcp-full-request-response-20260327-142231.json`
> 说明：保留请求参数、状态、traceId、关键指标与证据样本；省略超长 K 线/分时点位明细。

总计工具数：**11**

## CompanyOverviewMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/company-overview?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"symbol":"sh600000","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `3599d9e5dbdb45faab0d5709a479589c`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `14506`
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
    "price": 10.03,
    "changePercent": -0.3,
    "floatMarketCap": 334057558149.0,
    "peRatio": 6.68,
    "shareholderCount": 119099,
    "quoteTimestamp": "2026-03-27T22:22:35.9282756+08:00",
    "fundamentalUpdatedAt": "2026-03-27T22:22:46.5292066+08:00",
    "fundamentalFactCount": 33,
    "mainBusiness": null,
    "businessScope": "吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。"
}
```

### 证据样本（最多 3 条）
1. `浦发银行` | 来源：公司画像缓存 | 时间：2026-03-27T14:22:35.9282756+08:00
   - 摘要：所属板块=银行; 股东户数=119099; 现价=10.03; 经营范围=吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-21T01:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-27T02:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockProductMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/product?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"symbol":"sh600000","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `999070dfbcc945f1962712a2b69ac45f`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `3735`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "updatedAt": "2026-03-27T22:22:50.3119409+08:00",
    "mainBusiness": null,
    "businessScope": "吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。",
    "industry": "银行",
    "csrcIndustry": "金融业-货币金融服务",
    "region": "上海",
    "factCount": 4,
    "sourceSummary": "东方财富公司概况"
}
```

### 证据样本（最多 3 条）
1. `产品业务概览` | 来源：东方财富公司概况 | 时间：2026-03-27T14:22:50.3119409+08:00
   - 摘要：经营范围=吸收公众存款;发放短期、中期和长期贷款;办理结算;办理票据贴现;发行金融债券;代理发行、代理兑付、承销政府债券,买卖政府债券;同业拆借;提供信用证服务及担保;代理收付款项及代理保险业务;提供保管箱服务;外汇存款;外汇贷款;外汇汇款;外币兑换;国际结算;同业外汇拆借;外汇票据的承兑和贴现;外汇借款;外汇担保;结汇、售汇;买卖和代理买卖股票以外的外币有价证券;自营外汇买卖;代客外汇买卖;银行卡业务;资信调查、咨询、见证业务;离岸银行业务;证券投资基金托管业务;公募证券投资基金销售;经批准的其它业务。; 所属行业=银行; 所属地区=上海
1. `所属行业` | 来源：东方财富公司概况 | 时间：2026-03-27T22:22:50.3119409+08:00
   - 摘要：银行
1. `证监会行业` | 来源：东方财富公司概况 | 时间：2026-03-27T22:22:50.3119409+08:00
   - 摘要：金融业-货币金融服务

---

## StockFundamentalsMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/fundamentals?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"symbol":"sh600000","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `118c428ad0874aa58d37e9dc34f2b871`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `3752`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "updatedAt": "2026-03-27T22:22:54.0902996+08:00",
    "factCount": 28
}
```

### 证据样本（最多 3 条）
1. `公司全称` | 来源：东方财富公司概况 | 时间：2026-03-27T22:22:54.0902996+08:00
   - 摘要：上海浦东发展银行股份有限公司
1. `英文名称` | 来源：东方财富公司概况 | 时间：2026-03-27T22:22:54.0902996+08:00
   - 摘要：Shanghai Pudong Development Bank Co.,Ltd.
1. `证券类别` | 来源：东方财富公司概况 | 时间：2026-03-27T22:22:54.0902996+08:00
   - 摘要：上交所主板A股

---

## StockShareholderMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/shareholder?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"symbol":"sh600000","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `9efe5519286e42c5b13f858e6b905385`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `10200`
 - freshnessTag: `fresh`
 - sourceTier: `local`
 - cache.hit: `False`
 - cache.source: `live`

### 关键数据（过滤）
```json
{
    "symbol": "sh600000",
    "shareholderCount": 119099,
    "updatedAt": "2026-03-27T22:23:04.3166013+08:00",
    "factCount": 5
}
```

### 证据样本（最多 3 条）
1. `股东户数` | 来源：东方财富股东研究 | 时间：2026-03-27T22:23:04.3166013+08:00
   - 摘要：119099
1. `股东户数统计截止` | 来源：东方财富股东研究 | 时间：2026-03-27T22:23:04.3166013+08:00
   - 摘要：2025-09-30 00:00:00
1. `股权集中度` | 来源：东方财富股东研究 | 时间：2026-03-27T22:23:04.3166013+08:00
   - 摘要：非常分散

---

## MarketContextMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/market-context?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"symbol":"sh600000","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `89b9dd3b30cb4b87855e706b44642198`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `14`
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

### 证据样本（最多 3 条）
1. `板块对齐` | 来源：IStockMarketContextService | 时间：
   - 摘要：个股行业=银行，主线=阿兹海默，主线对齐=否。

---

## SocialSentimentMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/social-sentiment?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"symbol":"sh600000","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `a1e55e8e927740388b7b98bd16235bd6`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `3483`
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
    "latestEvidenceAt": "2026-03-28T06:23:04.4444571+08:00"
}
```

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-21T01:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-27T02:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-13T01:19:17.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockKlineMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/kline?symbol=sh600000&interval=day&count=60&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"symbol":"sh600000","interval":"day","count":"60","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `586de3dc0af04e6581db90398f30ada3`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `13635`
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
    "return5dPercent": 1.72,
    "return20dPercent": 3.62,
    "atrPercent": 1.97,
    "breakoutDistancePercent": 4.29
}
```

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-21T01:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-27T02:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-13T01:19:17.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockMinuteMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/minute?symbol=sh600000&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"symbol":"sh600000","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `fb2673c3a05e480eb06e7e558b6eae88`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `13890`
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
    "sessionPhase": "afternoon_session",
    "windowSize": 220,
    "vwap": 10.027,
    "openingDrivePercent": 0.6,
    "afternoonDriftPercent": 0.3,
    "intradayRangePercent": 1.5
}
```

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-21T01:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-27T02:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-13T01:19:17.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockStrategyMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/strategy?symbol=sh600000&interval=day&count=60&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"symbol":"sh600000","interval":"day","count":"60","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `7bef3a48c9fd4572b181cd61a58d05a8`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `15564`
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
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-21T01:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-27T02:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `国有大型银行Ⅲ板块最新轮动排名第14，主线分数44.4，扩散度50` | 来源：本地板块轮动快照 | 时间：2026-03-17T22:41:50.1026352+08:00
   - 摘要：国有大型银行Ⅲ板块轮动快照。

---

## StockNewsMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/news?symbol=sh600000&level=stock&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"symbol":"sh600000","level":"stock","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `da62a5ae15ce4a80a8c452765f846d79`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `6195`
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
    "latestPublishedAt": "2026-03-21T01:47:45.0000000+08:00"
}
```

### 证据样本（最多 3 条）
1. `浦发银行:上海浦东发展银行股份有限公司关于召开2025年度业绩说明会的公告` | 来源：东方财富公告 | 时间：2026-03-21T01:47:45.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司优先股二期股息发放实施公告` | 来源：东方财富公告 | 时间：2026-02-27T02:29:33.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...
1. `浦发银行:上海浦东发展银行股份有限公司董事会2026年第三次会议决议公告` | 来源：东方财富公告 | 时间：2026-02-13T01:19:17.0000000+08:00
   - 摘要：银行 基金 理财 保险 债券 视频 股吧 基金吧 博客 搜索 数据中心 全球财经快讯 行情中心 Choice数据 妙想大模型...

---

## StockSearchMcp

### 请求
 - method: `GET`
 - url: `http://localhost:5119/api/stocks/mcp/search?query=%E6%B5%A6%E5%8F%91%E9%93%B6%E8%A1%8C&trustedOnly=true&taskId=GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - query: `{"query":"浦发银行","trustedOnly":"true","taskId":"GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT"}`

### 返回
 - statusCode: `200`
 - traceId: `54f0608f75c64a40ad8f9bea31701540`
 - taskId: `GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
 - latencyMs: `0`
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

---

## 2026-03-27 修复增量说明

> 说明：本节基于本次重新抓取的**当前在线真实返回**整理，不再引用修复前样本。

- 本次重跑 TaskId：`GOAL-AGENT-NEW-001-P0-PRE-FULL-MCP-REQRESP-RERUN-20260327-TESTAGENT`
- 定向单测命令：`dotnet test .\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~StockMcpGatewayPhaseATests|FullyQualifiedName~StockCopilotMcpServiceTests|FullyQualifiedName~StockMcpEndpointExecutorTests`
- 单测结果：**41/41 通过，0 失败**

### 已在最新返回中确认的修复结果

1. `MarketContextMcp.data.stageLabel = null`，`executionFrequencyLabel = null`。
2. `StockSearchMcp` 当前 warning 已更新为：`外部搜索未启用，StockSearchMcp 当前只返回空结果。请在 LLM 设置中配置 Tavily API Key。`。
3. 本报告上文全部 11 个 MCP 样本均来自本次在线重抓，已与当前代码行为同步。
