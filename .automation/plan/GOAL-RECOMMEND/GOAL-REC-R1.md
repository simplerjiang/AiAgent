# GOAL-REC-R1: Web 搜索基础设施

> **前置**: P0 完成
> **交付**: WebSearchMcp 三链路 + 健康检查 + 同步到 Trading Workbench

## 任务清单

### R1-1: WebSearchMcp 核心抽象
**位置**: `backend/SimplerJiangAiAgent.Api/Infrastructure/Mcp/WebSearchMcp.cs`

接口设计:
```csharp
public interface IWebSearchService
{
    Task<WebSearchResult> SearchAsync(string query, SearchType type, WebSearchOptions? options, CancellationToken ct);
    Task<WebReadResult> ReadUrlAsync(string url, int maxChars = 8000, CancellationToken ct = default);
    WebSearchHealthStatus GetHealthStatus();
}

public enum SearchType { Web, News, Finance }
public record WebSearchOptions(string? TimeRange, int MaxResults = 10, string? Language = "zh");
public record WebSearchResult(IReadOnlyList<WebSearchItem> Items, string Provider, bool IsDegraded);
public record WebSearchItem(string Title, string Url, string Snippet, DateTime? PublishedAt, string Source);
public record WebReadResult(string Content, string Url, int OriginalLength, bool Truncated);
public record WebSearchHealthStatus(string ActiveProvider, bool TavilyHealthy, bool SearxngHealthy, bool DuckDuckGoHealthy);
```

### R1-2: Tavily 降级逻辑
- 在现有 Tavily 调用上包裹 try/catch
- HTTP 4xx/5xx 或 `quota_exceeded` → 标记降级 → 切换到 SearXNG
- 降级事件写入 ResearchEventBus
- 健康探针: 启动时 + 每 5 分钟 probe

### R1-3: SearXNG Docker 部署 + 客户端
- 新增 `docker-compose.searxng.yml`（项目根目录）
- SearXNG 客户端 `SearxngSearchClient.cs`
- `GET /search?q={query}&format=json&time_range={range}&language=zh`
- 引擎白名单: google, bing, baidu, sogou
- 连接超时 3s，返回 0 结果 → 降级到 DuckDuckGo

### R1-4: DuckDuckGo 兜底链路
- C# HTTP 直接调用 DuckDuckGo HTML API 或 lite API
- 或委托 Python 脚本 `ddgs` 库（`scripts/ddg_search.py`）
- 支持 `text()` 和 `news()` 两种搜索类型
- 有 `timelimit` 参数: d/w/m
- 最终兜底，失败返回空 + 告警

### R1-5: 统一降级编排
- `WebSearchService.cs` 实现 `IWebSearchService`
- 内部持有三个 client 实例
- 策略: Tavily → SearXNG → DuckDuckGo
- 配置来源: `appsettings.json` → `WebSearch:PrimaryProvider` / `FallbackOrder`
- 降级事件记录到日志 + EventBus

### R1-6: 健康检查端点
- `GET /api/health/websearch` 返回三链路状态
- 每 5 分钟后台探活
- 前端可查询当前活跃的搜索链路

### R1-7: 同步到 Trading Workbench
- 在 `RoleToolPolicyService` 注册 `web_search` / `web_search_news` / `web_read_url`
- McpToolGateway 扩展 Web 搜索工具
- Trading Workbench 的 ResearchRoleExecutor 可调用 WebSearchMcp

### R1-8: 单元测试
- Tavily 降级到 SearXNG 的 mock 测试
- SearXNG 降级到 DuckDuckGo 的 mock 测试
- 三链路全挂时返回空 + 告警的测试
- 健康检查状态准确性测试

## 验收标准
- [ ] 三链路各自可独立调用
- [ ] 降级链自动切换，无需人工干预
- [ ] `/api/health/websearch` 返回准确状态
- [ ] Trading Workbench agent 可调用 `web_search`
- [ ] 单元测试覆盖降级 + 健康检查
- [ ] SearXNG Docker 配置可用
