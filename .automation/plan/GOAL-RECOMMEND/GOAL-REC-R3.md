# GOAL-REC-R3: 11 角色 Prompt 实现

> **前置**: R1 (WebSearchMcp 可用) + R2 (Runner + RoleExecutor 骨架可用)
> **交付**: 全部角色 Prompt 模板 + 输出 JSON Schema + function calling 工具注册 + 集成测试

## 任务清单

### R3-1: 角色合约注册表
**位置**: `backend/.../Services/Recommend/RecommendRoleContractRegistry.cs`

```csharp
public interface IRecommendRoleContractRegistry
{
    RecommendRoleContract GetContract(string roleId);
    IReadOnlyList<string> GetStageRoleIds(RecommendStageType stage);
}
```

每个合约包含:
- `RoleId`: 角色标识
- `DisplayName`: 中文显示名
- `SystemPrompt`: 完整 Prompt 模板（含 function calling schema）
- `OutputSchema`: 期望的 JSON 输出结构描述
- `ToolHints`: 建议优先使用的工具列表
- `MaxToolCalls`: 工具调用预算（默认 5）

### R3-2: Stage 1 — 市场扫描团队 (3 角色)

**宏观环境分析师** (`recommend_macro_analyst`):
- 输入: 用户意图 + 当前日期
- 工具偏好: `web_search`（宏观政策）, `market_context`, `stock_news(level=market)`
- 输出 Schema: `{ sentiment: "bullish"|"neutral"|"cautious", keyDrivers: [{event, impact, source, publishedAt}], globalContext: string, policySignals: string[] }`

**热点板块猎手** (`recommend_sector_hunter`):
- 输入: 用户意图 + 当前日期
- 工具偏好: `web_search_news`（板块热点）, `sector_realtime`, `market_context`
- 输出 Schema: `{ candidateSectors: [{name, code, changePercent, netInflow, catalysts: string[], reason}] }` (5-8 个)

**资金流向分析师** (`recommend_smart_money`):
- 输入: 用户意图 + 当前日期
- 工具偏好: `market_context`（资金流）, `web_search`（北向资金）
- 输出 Schema: `{ mainCapitalFlow: {...}, northboundFlow: {...}, resonanceSectors: [{name, reason}], anomalies: [{description, severity}] }`

### R3-3: Stage 2 — 板块聚焦辩论 (3 角色)

**板块多头** (`recommend_sector_bull`):
- 输入: Stage 1 全部输出
- 工具偏好: `web_search`（利好新闻）
- 输出 Schema: `{ sectorClaims: [{sectorName, bullPoints: [{claim, evidence, source}]}] }`

**板块空头** (`recommend_sector_bear`):
- 输入: Stage 1 输出 + Bull Claims
- 工具偏好: `web_search`（利空新闻、风险）
- 输出 Schema: `{ sectorRisks: [{sectorName, bearPoints: [{rebuttal, evidence, source}], riskRating: "high"|"medium"|"low"}] }`

**板块裁决官** (`recommend_sector_judge`):
- 输入: Bull Claims + Bear Risks
- 工具偏好: 无（纯消费辩论记录）
- 输出 Schema: `{ selectedSectors: [{name, code, reason, keyRisk}], eliminatedSectors: [{name, reason}] }` (选 2-3 个)

### R3-4: Stage 3 — 选股精选团队 (3 角色)

**龙头猎手** (`recommend_leader_picker`):
- 输入: 裁决板块列表
- 工具偏好: `stock_search`, `stock_kline`, `stock_fundamentals`, `web_search`
- 输出 Schema: `{ picks: [{symbol, name, sectorName, pickType: "leader", reason, metrics: {...}}] }` (每板块 1-2 只)

**潜力股猎手** (`recommend_growth_picker`):
- 输入: 裁决板块列表
- 工具偏好: `stock_search`, `stock_kline`, `stock_fundamentals`, `web_search_news`, `web_read_url`
- 输出 Schema: `{ picks: [{symbol, name, sectorName, pickType: "growth", triggerCondition, reason}] }` (每板块 1-2 只)

**技术面验证师** (`recommend_chart_validator`):
- 输入: LeaderPicker + GrowthPicker 选出的个股列表
- 工具偏好: `stock_kline`, `stock_minute`, `stock_strategy`
- 输出 Schema: `{ validations: [{symbol, name, technicalScore: 0-100, supportLevel, resistanceLevel, volumeAssessment, trendState, strategySignals: [...], verdict: "pass"|"caution"|"fail"}] }`

### R3-5: Stage 4 — 个股辩论 & 风控 (3 角色)

**推荐多头** (`recommend_stock_bull`):
- 输入: Stage 3 全部输出（选股 + 验证结果）
- 工具偏好: `web_search`（个股利好）
- 输出 Schema: `{ bullCases: [{symbol, name, buyLogic, catalysts: [{event, timeline}], evidenceSources: [...]}] }`

**推荐空头** (`recommend_stock_bear`):
- 输入: Stage 3 输出 + Bull Cases
- 工具偏好: `web_search`（利空、负面）
- 输出 Schema: `{ bearCases: [{symbol, name, risks: [{risk, severity, evidence}], counterArguments: string[]}] }`

**风控审查员** (`recommend_risk_reviewer`):
- 输入: Bull + Bear 全部辩论
- 工具偏好: 无（纯消费）
- 输出 Schema: `{ assessments: [{symbol, name, riskLevel: "high"|"medium"|"low", invalidConditions: string[], maxLossEstimate, recommendation: "approve"|"conditional"|"reject"}] }`

### R3-6: Stage 5 — 推荐决策 (1 角色)

**推荐总监** (`recommend_director`):
- 输入: 全流水线输出（S1-S4 汇总）
- 工具偏好: 无
- 输出 Schema: 完整 `RecommendationReport`（见设计文档第九节）
- 要求: 生成板块卡片 + 个股卡片 + 风险提示 + 置信度 + 有效期 + 工具调用统计

### R3-7: 全局工具 Function Calling Schema
每个角色 Prompt 中嵌入可用工具列表:
```json
{
  "tools": [
    {"name": "web_search", "description": "搜索互联网", "parameters": {"query": "string", "time_range": "day|week|month", "max_results": "int"}},
    {"name": "web_search_news", "description": "搜索新闻", "parameters": {...}},
    {"name": "web_read_url", "description": "读取URL内容", "parameters": {"url": "string"}},
    {"name": "market_context", "description": "获取大盘行情", "parameters": {}},
    {"name": "sector_realtime", "description": "获取板块排行", "parameters": {}},
    {"name": "stock_search", "description": "搜索股票", "parameters": {"query": "string"}},
    {"name": "stock_news", "description": "获取新闻事实", "parameters": {"symbol": "string", "level": "stock|sector|market"}},
    {"name": "stock_kline", "description": "获取K线", "parameters": {"symbol": "string", "interval": "day|week|month", "count": "int"}},
    {"name": "stock_minute", "description": "获取分时", "parameters": {"symbol": "string"}},
    {"name": "stock_fundamentals", "description": "获取基本面", "parameters": {"symbol": "string"}},
    {"name": "stock_strategy", "description": "获取策略信号", "parameters": {"symbol": "string", "interval": "string"}}
  ]
}
```

### R3-8: 单元测试
- 每个角色合约的 Prompt 非空、Schema 可解析测试
- 工具注册表完整性测试（所有工具在 McpToolGateway 中有对应实现）
- Stage 1 集成测试（mock LLM + mock 工具返回 → 验证输出 schema 合规）
- 辩论阶段 Bull→Bear→Judge 的 artifact 传递测试
- 推荐总监输出 RecommendationReport schema 验证测试

## 验收标准
- [ ] 11 个角色合约全部注册且 Prompt 完整
- [ ] 每个角色有明确的输出 JSON Schema
- [ ] 工具 function calling schema 嵌入每个角色 Prompt
- [ ] 至少 3 个阶段有集成测试（mock LLM）
- [ ] 推荐总监输出符合 RecommendationReport 结构
