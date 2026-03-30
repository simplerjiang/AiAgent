# GOAL-AGENT-NEW-001-R8: MCP 可靠性优化与数据源冗余计划

## 1. 背景与现状分析

### 1.1 分析依据
基于 Session 12 (sz000021, Turn 18) 的完整运行日志和实时端点测试，发现以下问题：

### 1.2 发现的 Bug

| 编号 | 严重度 | 问题描述 | 影响范围 |
|------|--------|----------|----------|
| BUG-1 | **高** | StockKlineMcp / StockMinuteMcp 在研究管线中超时失败 | market_analyst 降级，缺失 K线+分时数据，技术分析基础不足 |
| BUG-2 | **中** | Fundamentals 报告块 summary 字段存储为原始 JSON wrapper `{"content":"..."}` 而非提取后的文本 | UI 渲染基本面摘要为 JSON 结构而非可读文本 |
| BUG-3 | **低** | ResearchReportSnapshots 表为空 (Reports=0) | 块已生成但未聚合为快照，影响历史查看 |

### 1.3 数据源可靠性现状

| MCP 工具 | 数据源 | 备用源 | 当前状态 |
|-----------|--------|--------|----------|
| CompanyOverviewMcp | Eastmoney快照 + 本地facts | 无 | ✅ 正常 |
| StockProductMcp | 本地facts + Eastmoney公告 | 无 | ✅ 正常 |
| StockFundamentalsMcp | 本地facts + Eastmoney财报快照 | 无 | ✅ 正常 (31 facts) |
| StockShareholderMcp | 本地facts + Eastmoney股东数据 | 无 | ✅ 正常 |
| MarketContextMcp | IStockMarketContextService | 无 | ✅ 正常 |
| StockKlineMcp | Eastmoney K线 (IStockDataService) | 无 | ❌ **超时** |
| StockMinuteMcp | Eastmoney 分时 (IStockDataService) | 无 | ❌ **超时** |
| StockStrategyMcp | 本地计算引擎 | 无 | ⚠️ evidence=0 |
| StockNewsMcp | 本地facts + Eastmoney新闻 | 无 | ✅ 正常 |
| SocialSentimentMcp | 降级至 local_news_and_market_proxy | 无真实社交数据源 | ⚠️ 永久降级 |
| StockSearchMcp | Tavily API (external_gated) | 无 | ✅ 正常 |

### 1.4 关键根因分析

**StockKlineMcp/StockMinuteMcp 超时根因**：
两个端点内部各调用 5+ 个串行数据请求：
```
EnsureSymbolFactsRefreshedAsync (可能触发爬虫刷新)
→ GetQuoteAsync
→ GetKLineAsync
→ GetMinuteLineAsync / GetIntradayMessagesAsync
→ QueryLocalFactDatabaseTool.QueryAsync
→ FeatureEngineeringService.Prepare
→ ResolveMcpMarketContextAsync
```
在研究管线 AnalystTeam 阶段，6个 analyst 并行执行，其中 market_analyst 同时调用 MarketContextMcp + StockKlineMcp + StockMinuteMcp + StockStrategyMcp，造成对 Eastmoney API 的大量并发请求。当单个请求延迟 >15s 且管道超时为 ~30s 时，StockKlineMcp（最重端点）容易超过阈值。

**Fundamentals 报告块 summary 双层 JSON 根因**：
`ParseAnalystBlock` 的 summary 提取逻辑使用字段优先级 `summary → analysis → businessScope → industryPosition → institutionActivity → content(原始输入)`。但 fundamentals_analyst 的 LLM 输出使用 `qualityView`、`valuationView`、`metrics`、`highlights`、`risks` 等字段，均不在 fallback 链中，导致整个 `{"content":"..."}` 原始 wrapper 被存为 summary。

---

## 2. 优化方案

### 2.1 Phase A - Bug 修复 (必做, 预计 1-2 个任务)

#### A1: 修复 StockKlineMcp/StockMinuteMcp 超时
**目标**：将研究管线中的 K线/分时 MCP 调用成功率从 ~30% 提升至 >95%

**方案**：
1. **并行化内部数据调用**：将 `GetKlineAsync` 和 `GetMinuteAsync` 内部的 5+ 个串行数据请求改为并行 `Task.WhenAll`：
   ```
   Before: quote → kline → minute → messages → facts (串行 ~15s)
   After: [quote | kline | minute | messages | facts] (并行 ~3-5s)
   ```
2. **分离 EnsureSymbolFactsRefreshedAsync**：对研究管线内的 MCP 调用，使用已有的 facts 缓存而非每次强制刷新。增加 `skipRefresh` 参数或基于最新刷新时间戳的短路逻辑。
3. **增加 MCP 级超时保护**：在 `DispatchToolAsync` 中增加 `CancellationTokenSource` 超时（如 25s），避免单个工具拖垮整个角色。
4. **MCP 级轻量模式**：为研究管线场景增加 `lightweight=true` 参数，跳过不必要的 feature engineering 计算（如完整的策略信号和 market context 重复解析）。

**验证标准**：
- StockKlineMcp 响应时间 < 8s (P95)
- StockMinuteMcp 响应时间 < 6s (P95)
- 研究管线 AnalystTeam 阶段 market_analyst 不再因超时降级

#### A2: 修复 Fundamentals 报告块 summary 解析
**目标**：Fundamentals 块显示可读的基本面摘要，而非 raw JSON

**方案**：
在 `ParseAnalystBlock` 的 summary fallback 链中增加 fundamentals 特有字段：
```csharp
// 现有 fallback 链:
summary → analysis → businessScope → industryPosition → institutionActivity → content

// 新增:
summary → analysis → qualityView+valuationView(合成) → businessScope → ... → content
```

具体实现：当 root 包含 `qualityView` 或 `valuationView` 时，合成摘要文本：
```
"质量评价: {qualityView}, 估值评价: {valuationView}。" + highlights[0] (如果存在)
```

同时将 `highlights` 映射到 `key_points`，将 `risks` 映射到 `counter_evidence`，将 `evidenceTable` 映射到 `evidence_refs`。

**验证标准**：
- `ResearchReportTests` 新增测试用例覆盖 fundamentals 输出格式
- UI 渲染 Fundamentals 块时显示清晰的中文摘要

### 2.2 Phase B - 数据源冗余 (推荐, 预计 2-3 个任务)

#### B1: IStockDataService 多源 fallback 机制
**目标**：为 K线、分时、行情三类核心数据增加备用源

**方案**：
在 `IStockDataService` 层增加 fallback 策略接口，当主数据源 (Eastmoney) 失败时自动切换：

| 数据类型 | 主源 | 备用源 1 | 备用源 2 |
|----------|------|----------|----------|
| K线历史 | Eastmoney K线 API | **新浪财经 K线 API** | 本地缓存最后 N 条 |
| 分时线 | Eastmoney 分时 API | **腾讯财经分时 API** | 空数组（降级） |
| 实时行情 | Eastmoney 行情 | **新浪财经行情** | 本地最新缓存 |

实现模式：
```csharp
public class FallbackStockDataService : IStockDataService
{
    private readonly IStockDataSource[] _sources; // [Eastmoney, Sina, ...]
    
    public async Task<IReadOnlyList<KLinePointDto>> GetKLineAsync(...)
    {
        foreach (var source in _sources)
        {
            try { return await source.GetKLineAsync(...); }
            catch (Exception ex) { _logger.LogWarning(ex, "Source {Name} failed", source.Name); }
        }
        return Array.Empty<KLinePointDto>(); // 最终降级
    }
}
```

**新增文件**：
- `IStockDataSource.cs` - 统一数据源接口
- `SinaStockDataSource.cs` - 新浪财经实现
- `TencentStockDataSource.cs` - 腾讯财经实现
- `FallbackStockDataService.cs` - fallback 编排器

**验证标准**：
- 主源断开时，备用源在 <5s 内接管
- 新增 fallback 单元测试
- `CompositeStockCrawlerTests` 扩展覆盖 fallback 场景

#### B2: Fundamental 快照多源冗余
**目标**：为财务指标获取增加备用源

**方案**：
当前 `ResolveFundamentalSnapshotAsync` 仅使用 Eastmoney 作为快照源。增加：
1. **新浪财经财务数据 API** 作为备用
2. **本地历史快照缓存** 作为终极降级（返回上次成功获取的数据，并标记 `stale` 标志）

注意：财务数据相对稳定（季报更新频率低），本地缓存命中率会很高。

#### B3: SocialSentiment 真实数据源接入
**目标**：至少有一个真实的社交 / 舆情数据源

**方案**（3选1）：
1. **东方财富股吧帖子爬取**：解析个股讨论区最新帖子情绪
2. **雪球讨论区 API**：获取个股帖子和用户观点
3. **百度指数/微博指数**：获取搜索热度趋势

推荐选项 1（东方财富股吧），原因：已有 Eastmoney 基础，可复用 HTTP 客户端和解析框架。

### 2.3 Phase C - 管线鲁棒性 (可选, 预计 1-2 个任务)

#### C1: LLM 响应韧性增强
- JSON parse 失败时的结构化降级（当前 Gemini 返回 markdown preamble 已有 retry，但可优化为提取 JSON 块而非直接重试）
- 对 thinking model 的 `*` 前缀输出增加 JSON 提取逻辑（`FindFirstJsonBlock`）
- LLM timeout 从 100s 降至 60s，但 retry 延迟从 [2s, 5s] 改为 [3s, 8s]

#### C2: 研究管线并发控制
- 增加 `SemaphoreSlim` 限制同一 symbol 的并发 MCP 调用数量（如 max=3）
- 对 Eastmoney API 增加全局限流器（`RateLimiter`），防止在并行 analyst 阶段触发反爬

#### C3: 报告快照聚合
- 在 `RunTurnAsync` 完成后，聚合所有 report blocks 为一个 `ResearchReportSnapshot`
- 使其出现在 session detail 的 `Reports` 列表中

---

## 3. 优先级与执行计划

| 优先级 | 任务 | 依赖 | 预计工作量 |
|--------|------|------|-----------|
| P0 | A1: KlineMcp/MinuteMcp 超时修复 | 无 | 中 |
| P0 | A2: Fundamentals 报告摘要修复 | 无 | 小 |
| P1 | B1: 多源 fallback 机制 | 无 | 大 |
| P1 | C2: 并发控制 | 与 A1 互补 | 中 |
| P2 | B2: Fundamental 多源冗余 | B1 的模式 | 中 |
| P2 | C1: LLM 韧性增强 | 无 | 小 |
| P2 | C3: 报告快照聚合 | 无 | 小 |
| P3 | B3: SocialSentiment 真实源 | 无 | 大 |

**建议执行顺序**：A1 + A2 → C2 → B1 → C3 → B2 → C1 → B3

---

## 4. 测试策略

### 4.1 单元测试
- A1: 在 `StockCopilotMcpServiceTests` 中测试 KlineMcp/MinuteMcp 超时场景
- A2: 在 `ResearchReportTests` 中测试 fundamentals JSON 格式解析
- B1: 新增 `FallbackStockDataServiceTests` 测试 source 切换
- C2: 新增限流器测试

### 4.2 集成测试
- 启动后端，触发一次完整研究管线（sz000021 或其他 A 股标的）
- 验证所有 6 个阶段完成，market_analyst 不降级
- 验证 report blocks 全部生成且 summary 可读
- 验证 report turn endpoint 返回完整数据

### 4.3 浏览器验证
- 打开 Trading Workbench 页面
- 触发研究分析
- 验证 Current Report 渲染：
  - 公司概览有完整数据
  - 基本面摘要是可读文本（非 JSON）
  - 有 K线/分时数据支撑的市场分析（非降级）
  - 投资决策和下一步操作正常显示

---

## 5. 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 新浪/腾讯 API 无稳定文档 | 中 | B1 延期 | 先验证 API 可用性再开发 |
| Eastmoney 增加反爬策略 | 低 | 全链路受影响 | C2 限流 + B1 fallback 双保险 |
| MCP 内部并行化引入竞态 | 低 | 数据不一致 | 无共享状态，各请求独立 |
| LLM provider 突然变更格式 | 低 | JSON 解析失败 | C1 增加容错提取 |
