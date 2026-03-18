# GOAL-AGENT-001 Stock Copilot Tool Catalog v1 Interface Design (2026-03-17)

## EN
### 1. Objective
This document refines the tool layer proposed in the Stock Copilot design draft into an implementation-grade interface contract.

Tool Catalog v1 is designed for three goals:
1. Give the planner a small, explicit, auditable action space.
2. Let the governor validate tool calls deterministically.
3. Produce normalized outputs that can be merged into evidence, features, and task-state snapshots without prompt-specific parsing hacks.

This is not yet a public MCP protocol document. It is the internal contract that should exist before MCP packaging.

### 2. Design Principles
1. Local-First by default.
2. One tool should do one bounded thing.
3. Tool names should reflect domain meaning, not storage details.
4. Every tool must return machine-usable metadata for governor and replay.
5. Deterministic features must come from code, not LLM arithmetic.
6. External-web tools are gated fallbacks, not first-class primary sources.

### 3. Standard Invocation Envelope
Every tool call should use the same outer request/response shape.

#### 3.1 Request Envelope
```json
{
  "traceId": "string",
  "taskId": "string",
  "taskType": "stock_analysis | sector_analysis | market_analysis | cross_stock_scan | overnight_selection | strategy_draft | trade_plan_review | evidence_verification | dev_trace_review",
  "toolName": "string",
  "mode": "fast | standard | deep",
  "requestedBy": "minimum_loader | planner | governor_override | preset_workflow",
  "asOf": "2026-03-17T15:00:00+08:00",
  "arguments": {},
  "budgetHints": {
    "remainingSteps": 0,
    "remainingExternalCalls": 0,
    "remainingArticleReads": 0,
    "timeBudgetMs": 0
  },
  "scope": {
    "symbols": [],
    "sectorCodes": [],
    "market": "CN-A"
  }
}
```

#### 3.2 Response Envelope
```json
{
  "ok": true,
  "toolName": "string",
  "traceId": "string",
  "taskId": "string",
  "latencyMs": 0,
  "cache": {
    "hit": false,
    "source": "live | db | cache | synthesized",
    "generatedAt": "2026-03-17T15:00:00+08:00"
  },
  "warnings": [],
  "degradedFlags": [],
  "data": {},
  "evidence": [],
  "features": [],
  "meta": {
    "version": "tool-catalog-v1",
    "requiresNormalization": true
  }
}
```

#### 3.3 Error Envelope
```json
{
  "ok": false,
  "toolName": "string",
  "traceId": "string",
  "taskId": "string",
  "latencyMs": 0,
  "error": {
    "code": "not_found | timeout | invalid_args | source_unavailable | policy_denied | parse_failed | degraded_only",
    "message": "string",
    "retryable": false,
    "recommendedFallback": "string|null"
  },
  "warnings": [],
  "degradedFlags": []
}
```

### 4. Canonical Shared Schemas
#### 4.1 Evidence Schema
```json
{
  "source": "Eastmoney",
  "publishedAt": "2026-03-17T09:15:00+08:00",
  "url": "https://...",
  "title": "string",
  "excerpt": "string",
  "readMode": "metadata_only | summary_only | full_text_read",
  "readStatus": "verified | partial | fetch_failed | unverified",
  "ingestedAt": "2026-03-17T09:20:00+08:00",
  "localFactId": 0,
  "sourceRecordId": "string",
  "articleSummary": "string|null",
  "fullTextHash": "string|null",
  "relevanceScore": 0.0,
  "trustTier": "official | research | mainstream | community | unknown"
}
```

#### 4.2 Feature Schema
```json
{
  "featureName": "macd_state",
  "value": "bullish_cross",
  "unit": "enum",
  "computedAt": "2026-03-17T14:59:00+08:00",
  "sourceWindow": "20d",
  "quality": "high | medium | low"
}
```

#### 4.3 Tool Trace Schema
```json
{
  "step": 3,
  "toolName": "GetTrendFeatures",
  "argumentsDigest": "sha256...",
  "requestedBy": "planner",
  "approvedByGovernor": true,
  "startedAt": "2026-03-17T15:00:00+08:00",
  "finishedAt": "2026-03-17T15:00:01+08:00",
  "status": "ok | degraded | failed | denied",
  "cacheHit": false,
  "degradedFlags": [],
  "summary": "Computed MA slope, MACD, RSI, ATR and key levels for 002594"
}
```

### 5. Policy Classes
Each tool must declare a policy class so the governor can enforce it uniformly.

1. `local_required`: must be attempted before any external fallback.
2. `local_preferred`: local first, but external fallback may be allowed if readiness is still insufficient.
3. `external_gated`: only callable after explicit insufficiency and allowlist validation.
4. `workflow_mutating`: changes todo, strategy, or plan state and requires stronger audit fields.
5. `developer_only`: available only in developer/admin flows.

### 6. Tool Families
#### 6.1 Market Tools
##### `GetMarketSnapshot`
Purpose:
Return current market regime, breadth, turnover, stage, and key index conditions.

Arguments:
```json
{
  "market": "CN-A",
  "session": "latest | intraday | close_only"
}
```

Response `data`:
```json
{
  "market": "CN-A",
  "regime": "risk_on | selective | defensive",
  "stage": "early | middle | late | breakdown",
  "breadth": {
    "advancers": 0,
    "decliners": 0,
    "limitUps": 0,
    "limitDowns": 0
  },
  "turnover": 0,
  "indices": [],
  "riskAppetite": "high | medium | low"
}
```

Policy:
`local_required`

##### `GetMarketHistory`
Arguments:
```json
{
  "market": "CN-A",
  "lookbackDays": 20
}
```

Response `data`:
```json
{
  "market": "CN-A",
  "snapshots": []
}
```

Policy:
`local_required`

##### `GetMainlineSectors`
Arguments:
```json
{
  "market": "CN-A",
  "topN": 10,
  "lookbackDays": 5
}
```

Response `data`:
```json
{
  "leaders": [
    {
      "sectorCode": "BK1234",
      "sectorName": "AI算力",
      "rank": 1,
      "continuationScore": 0.0,
      "diffusionScore": 0.0,
      "rankChange": 0
    }
  ]
}
```

Policy:
`local_required`

##### `GetMarketNews`
Arguments:
```json
{
  "lookbackHours": 72,
  "maxItems": 20,
  "trustedOnly": true
}
```

Response `data`:
```json
{
  "items": []
}
```

Output requirements:
`evidence[]` must be populated.

Policy:
`local_required`

#### 6.2 Sector Tools
##### `GetSectorRotation`
Arguments:
```json
{
  "sectorCode": "BK1234",
  "lookbackDays": 10
}
```

Response `data`:
```json
{
  "sectorCode": "BK1234",
  "rotationState": "accelerating | continuing | weakening | exhausted",
  "relativeRankSeries": []
}
```

Policy:
`local_required`

##### `GetSectorTrend`
Arguments:
```json
{
  "sectorCode": "BK1234",
  "interval": "day",
  "lookbackBars": 60
}
```

Response `data`:
```json
{
  "sectorCode": "BK1234",
  "trendDirection": "up | flat | down",
  "trendStrength": 0.0,
  "supportLevels": [],
  "resistanceLevels": []
}
```

Policy:
`local_required`

##### `GetSectorNews`
Arguments:
```json
{
  "sectorCode": "BK1234",
  "lookbackHours": 72,
  "maxItems": 15
}
```

Response `data`:
```json
{
  "sectorCode": "BK1234",
  "items": []
}
```

Policy:
`local_required`

##### `GetSectorLeaders`
Arguments:
```json
{
  "sectorCode": "BK1234",
  "topN": 10,
  "includeSecondLine": true
}
```

Response `data`:
```json
{
  "sectorCode": "BK1234",
  "leaders": [],
  "secondLineCandidates": []
}
```

Policy:
`local_required`

##### `ScanSectorCandidates`
Arguments:
```json
{
  "sectorCodes": ["BK1234"],
  "maxCandidatesPerSector": 5,
  "objective": "overnight | continuation | catchup"
}
```

Response `data`:
```json
{
  "candidates": []
}
```

Policy:
`local_required`

#### 6.3 Stock Data Tools
##### `GetStockQuote`
Arguments:
```json
{
  "symbol": "002594",
  "includeRealtime": true
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "name": "比亚迪",
  "lastPrice": 0.0,
  "changePercent": 0.0,
  "volume": 0,
  "turnover": 0,
  "high": 0.0,
  "low": 0.0,
  "open": 0.0,
  "prevClose": 0.0,
  "quoteTime": "2026-03-17T14:59:57+08:00"
}
```

Policy:
`local_required`

##### `GetStockDetailCache`
Arguments:
```json
{
  "symbol": "002594",
  "preferCache": true
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "name": "比亚迪",
  "companyProfile": {},
  "fundamentalSnapshot": {},
  "latestQuote": {},
  "updatedAt": "2026-03-17T14:30:00+08:00"
}
```

Policy:
`local_required`

##### `GetStockKLines`
Arguments:
```json
{
  "symbol": "002594",
  "interval": "day | week | month",
  "lookbackBars": 120,
  "adjusted": true
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "interval": "day",
  "bars": []
}
```

Policy:
`local_required`

##### `GetStockMinuteLines`
Arguments:
```json
{
  "symbol": "002594",
  "session": "latest",
  "maxBars": 240
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "sessionDate": "2026-03-17",
  "bars": []
}
```

Policy:
`local_required`

##### `GetStockMessages`
Arguments:
```json
{
  "symbol": "002594",
  "lookbackHours": 24,
  "maxItems": 20
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "messages": []
}
```

Policy:
`local_required`

##### `GetStockCompanyProfile`
Arguments:
```json
{
  "symbol": "002594",
  "includeFundamentalFacts": true
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "companyName": "比亚迪",
  "industry": "汽车整车",
  "businessSummary": "string",
  "fundamentalFacts": []
}
```

Policy:
`local_required`

##### `GetStockSignals`
Arguments:
```json
{
  "symbol": "002594",
  "view": "day | minute",
  "families": ["td", "macd_cross", "kdj_cross", "volume_breakout"]
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "signals": []
}
```

Policy:
`local_required`

##### `GetPositionGuidance`
Arguments:
```json
{
  "symbol": "002594",
  "riskProfile": "conservative | balanced | aggressive",
  "holdingPeriod": "intraday | swing | position"
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "maxPosition": 0.0,
  "initialPosition": 0.0,
  "stopLoss": 0.0,
  "takeProfit": 0.0,
  "drawdownLimit": 0.0
}
```

Policy:
`local_required`

#### 6.4 Indicator / Feature Tools
##### `GetTrendFeatures`
Arguments:
```json
{
  "symbol": "002594",
  "interval": "day",
  "lookbackBars": 120
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "interval": "day",
  "maSlope": {},
  "macd": {},
  "rsi": {},
  "atr": {},
  "breakoutStructure": {},
  "supportLevels": [],
  "resistanceLevels": []
}
```

Output requirements:
`features[]` must be populated.

Policy:
`local_required`

##### `GetMomentumFeatures`
Arguments:
```json
{
  "symbol": "002594",
  "lookbackDays": 20
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "relativeMomentum": 0.0,
  "volumeExpansion": 0.0,
  "turnoverAcceleration": 0.0
}
```

Policy:
`local_required`

##### `GetValuationFeatures`
Arguments:
```json
{
  "symbol": "002594",
  "benchmark": "sector | market | history"
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "peTtm": 0.0,
  "pb": 0.0,
  "valuationPercentile": 0.0,
  "benchmarkType": "sector"
}
```

Policy:
`local_required`

##### `GetFinancialFeatures`
Arguments:
```json
{
  "symbol": "002594",
  "period": "latest_quarter | ttm"
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "revenueGrowth": 0.0,
  "profitGrowth": 0.0,
  "grossMargin": 0.0,
  "roe": 0.0,
  "cashFlowQuality": 0.0
}
```

Policy:
`local_required`

##### `GetSectorRelativeStrength`
Arguments:
```json
{
  "symbol": "002594",
  "sectorCode": "BK1234",
  "lookbackDays": 20
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "sectorCode": "BK1234",
  "relativeStrengthScore": 0.0,
  "rankWithinSector": 0
}
```

Policy:
`local_required`

##### `GetHistoricalOutcomeFeatures`
Arguments:
```json
{
  "symbol": "002594",
  "patternKey": "macd_bull_cross_with_sector_strength",
  "lookbackSamples": 100
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "patternKey": "string",
  "sampleCount": 0,
  "winRate1D": 0.0,
  "winRate5D": 0.0,
  "avgReturn5D": 0.0
}
```

Policy:
`local_preferred`

#### 6.5 Local Fact Tools
##### `GetLocalStockNews`
Arguments:
```json
{
  "symbol": "002594",
  "lookbackHours": 72,
  "maxItems": 20,
  "includeUnprocessed": false
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "items": []
}
```

Output requirements:
`evidence[]` must be populated.

Policy:
`local_required`

##### `GetLocalSectorReports`
Arguments:
```json
{
  "sectorCode": "BK1234",
  "lookbackHours": 72,
  "maxItems": 15
}
```

Response `data`:
```json
{
  "sectorCode": "BK1234",
  "items": []
}
```

Policy:
`local_required`

##### `GetLocalMarketReports`
Arguments:
```json
{
  "lookbackHours": 48,
  "maxItems": 15,
  "strictChinaAOnly": true
}
```

Response `data`:
```json
{
  "items": []
}
```

Policy notes:
Must apply anti-contamination filtering before returning items.

Policy:
`local_required`

##### `GetAnnouncements`
Arguments:
```json
{
  "symbol": "002594",
  "lookbackDays": 30,
  "types": ["earnings", "guidance", "major_contract", "shareholder_change"]
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "items": []
}
```

Policy:
`local_required`

##### `GetResearchReports`
Arguments:
```json
{
  "symbol": "002594",
  "lookbackDays": 30,
  "maxItems": 10
}
```

Response `data`:
```json
{
  "symbol": "002594",
  "items": []
}
```

Policy:
`local_preferred`

##### `GetLocalFactPackage`
Arguments:
```json
{
  "symbol": "002594",
  "sectorCode": "BK1234",
  "includeMarket": true,
  "includeSector": true,
  "includeStock": true,
  "lookbackHours": 72
}
```

Response `data`:
```json
{
  "stockFacts": [],
  "sectorFacts": [],
  "marketFacts": []
}
```

Policy:
`local_required`

#### 6.6 Article Reading Tools
##### `FetchArticleMetadata`
Arguments:
```json
{
  "url": "https://...",
  "sourceHint": "Eastmoney"
}
```

Response `data`:
```json
{
  "url": "https://...",
  "title": "string",
  "publishedAt": "2026-03-17T09:15:00+08:00",
  "author": "string|null",
  "source": "Eastmoney"
}
```

Policy:
`local_preferred`

##### `FetchArticleFullText`
Arguments:
```json
{
  "url": "https://...",
  "sourceHint": "Eastmoney",
  "maxChars": 12000
}
```

Response `data`:
```json
{
  "url": "https://...",
  "fullText": "string",
  "contentHash": "sha256...",
  "paragraphCount": 0
}
```

Policy notes:
Counts against article-read budget.

Policy:
`local_preferred`

##### `GetArticleSummary`
Arguments:
```json
{
  "url": "https://...",
  "summaryMode": "deterministic_extract | llm_summary",
  "maxSentences": 5
}
```

Response `data`:
```json
{
  "url": "https://...",
  "summary": "string",
  "summaryMode": "deterministic_extract | llm_summary"
}
```

Policy notes:
Preferred path is deterministic extract when possible.

Policy:
`local_preferred`

##### `ExtractKeyParagraphs`
Arguments:
```json
{
  "url": "https://...",
  "question": "What paragraph mentions earnings guidance?",
  "maxParagraphs": 3
}
```

Response `data`:
```json
{
  "url": "https://...",
  "paragraphs": []
}
```

Policy:
`local_preferred`

#### 6.7 External Web Tools
##### `SearchExternalWeb`
Arguments:
```json
{
  "query": "比亚迪 最新 海外 销量 指引",
  "maxResults": 5,
  "language": "zh-CN"
}
```

Response `data`:
```json
{
  "query": "string",
  "results": []
}
```

Policy notes:
Only allowed after governor records local insufficiency reason.

Policy:
`external_gated`

##### `FetchExternalPage`
Arguments:
```json
{
  "url": "https://...",
  "extractMode": "readable | raw_text",
  "maxChars": 10000
}
```

Response `data`:
```json
{
  "url": "https://...",
  "content": "string",
  "contentHash": "sha256...",
  "title": "string|null"
}
```

Policy:
`external_gated`

##### `SearchTrustedSourcesOnly`
Arguments:
```json
{
  "query": "半导体 行业 景气度",
  "sourceAllowlist": ["cs.com.cn", "eastmoney.com"],
  "maxResults": 5
}
```

Response `data`:
```json
{
  "query": "string",
  "results": []
}
```

Policy:
`external_gated`

##### `ValidateExternalSource`
Arguments:
```json
{
  "url": "https://...",
  "requireTimestamp": true,
  "requireReadableContent": true
}
```

Response `data`:
```json
{
  "url": "https://...",
  "isTrusted": false,
  "trustTier": "unknown",
  "timestampFound": true,
  "readable": true,
  "validationReasons": []
}
```

Policy:
`external_gated`

#### 6.8 Workflow / Productivity Tools
##### `CreateTodo`
Arguments:
```json
{
  "title": "Read latest earnings preview",
  "owner": "system | user | planner",
  "sourceTaskId": "task-001",
  "priority": "low | normal | high"
}
```

Response `data`:
```json
{
  "id": "todo-001",
  "status": "open",
  "createdAt": "2026-03-17T15:10:00+08:00"
}
```

Policy:
`workflow_mutating`

##### `UpdateTodo`
Arguments:
```json
{
  "id": "todo-001",
  "status": "open | in_progress | done | cancelled",
  "note": "string"
}
```

Response `data`:
```json
{
  "id": "todo-001",
  "status": "done",
  "updatedAt": "2026-03-17T15:12:00+08:00"
}
```

Policy:
`workflow_mutating`

##### `DraftTradingPlan`
Arguments:
```json
{
  "symbol": "002594",
  "strategyType": "swing_continuation",
  "entryIdea": "pullback_to_ma10",
  "riskProfile": "balanced"
}
```

Response `data`:
```json
{
  "planId": "plan-001",
  "draft": {},
  "createdAt": "2026-03-17T15:13:00+08:00"
}
```

Policy:
`workflow_mutating`

##### `CreateStrategySuggestion`
Arguments:
```json
{
  "taskId": "task-001",
  "suggestionType": "overnight_watch | breakout | mean_reversion | earnings_rebound",
  "symbols": ["002594"]
}
```

Response `data`:
```json
{
  "suggestionId": "strategy-001",
  "draft": {}
}
```

Policy:
`workflow_mutating`

##### `ListOpenPlans`
Arguments:
```json
{
  "symbol": "002594",
  "status": "open | pending | all"
}
```

Response `data`:
```json
{
  "plans": []
}
```

Policy:
`local_required`

##### `ReviewExistingPlan`
Arguments:
```json
{
  "planId": "plan-001",
  "includeLatestEvidence": true
}
```

Response `data`:
```json
{
  "planId": "plan-001",
  "status": "needs_review | still_valid | invalidated",
  "revisionHints": []
}
```

Policy:
`workflow_mutating`

#### 6.9 Review / Developer Tools
##### `GetAnalysisHistory`
Arguments:
```json
{
  "symbol": "002594",
  "limit": 20
}
```

Response `data`:
```json
{
  "items": []
}
```

Policy:
`developer_only`

##### `GetCommanderHistory`
Arguments:
```json
{
  "symbol": "002594",
  "limit": 20,
  "includeEvidenceDigest": true
}
```

Response `data`:
```json
{
  "items": []
}
```

Policy:
`developer_only`

##### `GetReplaySample`
Arguments:
```json
{
  "sampleId": "replay-001"
}
```

Response `data`:
```json
{
  "sample": {}
}
```

Policy:
`developer_only`

##### `GetCalibrationMetrics`
Arguments:
```json
{
  "groupBy": "model | prompt | strategy | date",
  "windowDays": 30
}
```

Response `data`:
```json
{
  "metrics": {
    "hitRate": 0.0,
    "brierScore": 0.0,
    "traceabilityRate": 0.0,
    "repairRate": 0.0,
    "contaminationRate": 0.0
  }
}
```

Policy:
`developer_only`

##### `GetAgentTrace`
Arguments:
```json
{
  "traceId": "trace-001",
  "includePromptDigest": true,
  "includeToolOutputs": true
}
```

Response `data`:
```json
{
  "trace": {
    "request": {},
    "toolTrace": [],
    "finalResult": {}
  }
}
```

Policy:
`developer_only`

### 7. Mandatory Metadata by Tool Type
1. Data retrieval tools must fill `cache`, `latencyMs`, and freshness markers.
2. Evidence tools must populate `evidence[]` with `readMode` and `readStatus`.
3. Feature tools must populate `features[]` with `computedAt` and `sourceWindow`.
4. Mutating tools must record actor, reason, and resulting object id.
5. Developer tools must redact secrets and large prompt bodies by default.

### 8. Duplicate Detection Rules
The governor should treat two tool calls as duplicates when all of the following match:
1. Same `toolName`.
2. Same normalized arguments after default expansion.
3. Same `asOf` bucket or freshness window.
4. No relevant degraded flag that justifies retry.

If duplicate and prior result is fresh enough, return cached result or deny the call.

### 9. Freshness Defaults
1. Market snapshot: 5 minutes intraday, 1 trading day after close.
2. Stock quote: 30 seconds intraday, 1 trading day after close.
3. Minute lines: latest session only.
4. K-lines: 1 trading day.
5. Local stock/sector/market news: 72 hours default analysis window.
6. Announcements: 30 calendar days unless the task says otherwise.
7. Research reports: 30 calendar days.
8. External pages: no reuse without re-validation unless cached within the same session.

### 10. Security and Boundary Requirements
1. No tool may expose secrets, raw provider keys, or internal credentials.
2. External page fetch must go through backend sanitization.
3. Developer tools are admin-gated and should be hidden from normal user workflows.
4. `DraftTradingPlan` and `CreateStrategySuggestion` may assist decisions but must not place orders.
5. Article-reading tools must record source URL and content hash for replayability.

### 11. Suggested v1 Delivery Order
1. Core local retrieval tools.
2. Deterministic feature tools.
3. Evidence article-reading tools.
4. Workflow mutating tools.
5. Developer/replay tools.
6. External gated tools.

This order keeps the first usable agent runtime local-first and testable.

### 12. Validation Notes
Actions performed for this draft:
1. Reused the previously accepted Stock Copilot architecture terms.
2. Expanded each named tool into request/response/policy shape.
3. Kept contracts compatible with the previously proposed evidence and feature objects.

Validation command to run after writing:
1. Editor diagnostics on this markdown file.

Known limitation:
1. This is an interface design draft, not yet bound to concrete backend class names or DTO files.

## ZH
### 1. 目标
这份文档把上一版 Stock Copilot 总体设计里的“工具层”进一步收敛成可落地的接口合同。

Tool Catalog v1 的目标有三个：
1. 给 planner 一个小而明确、可审计的动作空间。
2. 让 governor 能用确定性规则审核工具调用。
3. 让工具输出无需依赖 prompt 特例修补，就能统一并入 evidence、feature 与 task state。

它还不是对外公开的 MCP 协议文档，而是正式做 MCP 之前，内部必须先稳定下来的工具接口底稿。

### 2. 设计原则
1. 默认 Local-First。
2. 一个工具只做一个有边界的事情。
3. 工具命名表达业务含义，而不是底层存储实现。
4. 每个工具都必须返回可供 governor 和 replay 使用的机器字段。
5. 确定性特征由代码计算，不让 LLM 临场算数。
6. 外网工具是 fallback，不是主链。

### 3. 统一调用外壳
所有工具调用都应复用同一套 request/response/error envelope。

#### 3.1 Request Envelope
保留以下核心字段：
1. `traceId`：整条分析链路统一追踪。
2. `taskId`：任务实例 ID。
3. `taskType`：任务类型。
4. `toolName`：工具名。
5. `mode`：`fast | standard | deep`。
6. `requestedBy`：由最小上下文加载器、planner、governor override 还是 preset workflow 发起。
7. `asOf`：当前分析时间点。
8. `arguments`：工具入参。
9. `budgetHints`：剩余步数、外网次数、正文阅读次数、时间预算。
10. `scope`：symbols、sectorCodes、market。

#### 3.2 Response Envelope
统一返回：
1. `ok`
2. `toolName`
3. `traceId`
4. `taskId`
5. `latencyMs`
6. `cache`
7. `warnings`
8. `degradedFlags`
9. `data`
10. `evidence`
11. `features`
12. `meta`

#### 3.3 Error Envelope
错误码建议统一为：
1. `not_found`
2. `timeout`
3. `invalid_args`
4. `source_unavailable`
5. `policy_denied`
6. `parse_failed`
7. `degraded_only`

### 4. 核心共享结构
#### 4.1 Evidence Schema
必须包含：
1. `source`
2. `publishedAt`
3. `url`
4. `title`
5. `excerpt`
6. `readMode`
7. `readStatus`
8. `ingestedAt`

可选但推荐：
1. `localFactId`
2. `sourceRecordId`
3. `articleSummary`
4. `fullTextHash`
5. `relevanceScore`
6. `trustTier`

#### 4.2 Feature Schema
建议固定：
1. `featureName`
2. `value`
3. `unit`
4. `computedAt`
5. `sourceWindow`
6. `quality`

#### 4.3 Tool Trace Schema
每次调用至少记录：
1. 步号
2. 工具名
3. 参数摘要
4. 发起方
5. governor 是否批准
6. 开始/结束时间
7. 状态
8. 是否命中缓存
9. 降级标记
10. 人类可读摘要

### 5. 策略分级
每个工具都必须申明自己的 policy class：
1. `local_required`：必须先查本地。
2. `local_preferred`：优先本地，但在证据不足时可以继续 fallback。
3. `external_gated`：只有本地不足并通过 governor 审核才允许调用。
4. `workflow_mutating`：会改 todo / 策略 / plan，需要更强审计。
5. `developer_only`：仅开发者或管理员可用。

### 6. 工具家族与接口要求
#### 6.1 大盘工具
1. `GetMarketSnapshot`
   - 目标：返回当前市场风格、阶段、广度、成交、关键指数状态。
   - 入参：`market`、`session`。
   - 出参：`regime`、`stage`、`breadth`、`turnover`、`indices`、`riskAppetite`。
   - 策略：`local_required`。
2. `GetMarketHistory`
   - 目标：返回最近 N 天市场快照序列。
   - 入参：`market`、`lookbackDays`。
   - 出参：`snapshots[]`。
3. `GetMainlineSectors`
   - 目标：返回当前主线板块、延续分、扩散分、排名变化。
4. `GetMarketNews`
   - 目标：返回经过本地治理后的大盘资讯 evidence。
   - 要求：必须填充 `evidence[]`。

#### 6.2 板块工具
1. `GetSectorRotation`
   - 返回板块轮动状态与相对排名序列。
2. `GetSectorTrend`
   - 返回板块趋势方向、趋势强度、支撑阻力。
3. `GetSectorNews`
   - 返回板块资讯 evidence。
4. `GetSectorLeaders`
   - 返回龙头与补涨候选。
5. `ScanSectorCandidates`
   - 用于隔夜或延续型板块候选扫描。

#### 6.3 个股数据工具
1. `GetStockQuote`
   - 返回最新价格、涨跌幅、量能、高低开收与时间戳。
2. `GetStockDetailCache`
   - 返回公司概况、基本面快照、最新行情、缓存更新时间。
3. `GetStockKLines`
   - 返回日/周/月 K 线序列。
4. `GetStockMinuteLines`
   - 返回最新交易日分时线。
5. `GetStockMessages`
   - 返回盘中消息或短讯。
6. `GetStockCompanyProfile`
   - 返回业务概况与基本面事实。
7. `GetStockSignals`
   - 返回 TD、MACD 金叉死叉、KDJ 金叉死叉、放量突破等代码信号。
8. `GetPositionGuidance`
   - 返回风险档位下的初始仓位、最大仓位、止损、止盈、回撤上限。

#### 6.4 指标 / 特征工具
1. `GetTrendFeatures`
   - 返回 MA 斜率、MACD、RSI、ATR、突破结构、支撑阻力。
   - 要求：必须填充 `features[]`。
2. `GetMomentumFeatures`
   - 返回动量、量能扩张、换手加速。
3. `GetValuationFeatures`
   - 返回估值水平与所处分位。
4. `GetFinancialFeatures`
   - 返回营收增速、利润增速、毛利率、ROE、现金流质量。
5. `GetSectorRelativeStrength`
   - 返回个股相对板块强弱与板块内排名。
6. `GetHistoricalOutcomeFeatures`
   - 返回某类历史模式在 1D/5D 等窗口的样本数、胜率、均值收益。

#### 6.5 本地事实工具
1. `GetLocalStockNews`
2. `GetLocalSectorReports`
3. `GetLocalMarketReports`
4. `GetAnnouncements`
5. `GetResearchReports`
6. `GetLocalFactPackage`

其中：
1. `GetLocalMarketReports` 必须先做 A 股场景抗污染过滤。
2. `GetAnnouncements` 应支持公告类型筛选。
3. `GetLocalFactPackage` 适合 preset workflow 首轮加载使用。

#### 6.6 正文阅读工具
1. `FetchArticleMetadata`
   - 只抓元信息。
2. `FetchArticleFullText`
   - 抓取正文并输出 `contentHash`。
   - 会消耗 article-read budget。
3. `GetArticleSummary`
   - 优先确定性抽取，必要时才走 LLM 摘要。
4. `ExtractKeyParagraphs`
   - 根据问题提取关键段落，用于“哪一段支持这个结论”。

#### 6.7 外网工具
1. `SearchExternalWeb`
2. `FetchExternalPage`
3. `SearchTrustedSourcesOnly`
4. `ValidateExternalSource`

约束：
1. 只有 governor 记录了“本地证据不足原因”后才能放行。
2. 必须经后端抓取和清洗。
3. 进入最终结论前必须归一化为 evidence object。

#### 6.8 工作流 / 效率工具
1. `CreateTodo`
2. `UpdateTodo`
3. `DraftTradingPlan`
4. `CreateStrategySuggestion`
5. `ListOpenPlans`
6. `ReviewExistingPlan`

其中变更型工具都属于 `workflow_mutating`。

#### 6.9 开发 / 复盘工具
1. `GetAnalysisHistory`
2. `GetCommanderHistory`
3. `GetReplaySample`
4. `GetCalibrationMetrics`
5. `GetAgentTrace`

这些工具默认 `developer_only`，且输出需做敏感信息裁剪。

### 7. 不同工具的必备元数据
1. 数据拉取类：必须填 `cache`、`latencyMs`、freshness。
2. evidence 类：必须输出 `evidence[]`，且有 `readMode/readStatus`。
3. feature 类：必须输出 `features[]`，且有 `computedAt/sourceWindow`。
4. 变更类：必须记录 actor、reason、对象 ID。
5. 开发类：默认隐藏敏感 prompt 与密钥内容。

### 8. 重复调用判定
Governor 可按以下条件判重：
1. `toolName` 相同。
2. 归一化参数相同。
3. `asOf` 落在同一 freshness bucket。
4. 上一结果没有出现必须重试的降级标记。

若命中判重且结果仍新鲜，则直接复用缓存或拒绝调用。

### 9. 默认 freshness 窗口
1. 大盘快照：盘中 5 分钟，盘后 1 个交易日。
2. 个股行情：盘中 30 秒，盘后 1 个交易日。
3. 分时：仅最新交易日。
4. K 线：1 个交易日。
5. 本地资讯：默认 72 小时。
6. 公告：默认 30 天。
7. 研报：默认 30 天。
8. 外网页面：除同一会话内，否则不直接复用，需再次校验。

### 10. 安全边界
1. 工具不能暴露密钥、token、内部凭据。
2. 外网抓取必须经过后端清洗。
3. 开发类工具只开放给管理员/开发者模式。
4. `DraftTradingPlan` 与 `CreateStrategySuggestion` 只辅助，不下单。
5. 正文阅读工具必须记录 URL 与内容 hash，方便 replay。

### 11. v1 推荐落地顺序
1. 核心本地查询工具。
2. 确定性特征工具。
3. 正文阅读与摘要工具。
4. workflow 工具。
5. developer / replay 工具。
6. 外网 gated 工具。

这样可以先做出一个 Local-First、可测试的内部 Agent Runtime。

### 12. 本稿校验说明
本次动作：
1. 复用已确认的 Stock Copilot 总体术语。
2. 将上一版设计中列出的工具逐一扩成 request / response / policy contract。
3. 保持与 evidence object、feature object 的字段设计一致。

本稿建议校验：
1. 编辑器 markdown diagnostics。

当前限制：
1. 这是接口设计稿，尚未绑定到真实后端 DTO / service / controller 命名。