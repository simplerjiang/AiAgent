using System.Diagnostics;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public interface IMcpToolGateway
{
    Task<StockCopilotMcpEnvelopeDto<StockCopilotCompanyOverviewDataDto>> GetCompanyOverviewAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default);
    Task<StockCopilotMcpEnvelopeDto<StockCopilotProductDataDto>> GetProductAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default);
    Task<StockCopilotMcpEnvelopeDto<StockCopilotFundamentalsDataDto>> GetFundamentalsAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default);
    Task<StockCopilotMcpEnvelopeDto<StockCopilotShareholderDataDto>> GetShareholderAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default);
    Task<StockCopilotMcpEnvelopeDto<StockCopilotMarketContextDataDto>> GetMarketContextAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default);
    Task<StockCopilotMcpEnvelopeDto<StockCopilotSocialSentimentDataDto>> GetSocialSentimentAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default);
    Task<StockCopilotMcpEnvelopeDto<StockCopilotKlineDataDto>> GetKlineAsync(string symbol, string interval, int count, string? source, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default);
    Task<StockCopilotMcpEnvelopeDto<StockCopilotMinuteDataDto>> GetMinuteAsync(string symbol, string? source, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default);
    Task<StockCopilotMcpEnvelopeDto<StockCopilotStrategyDataDto>> GetStrategyAsync(string symbol, string interval, int count, string? source, IReadOnlyList<string>? strategies, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default);
    Task<StockCopilotMcpEnvelopeDto<StockCopilotNewsDataDto>> GetNewsAsync(string symbol, string level, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default);
    Task<StockCopilotMcpEnvelopeDto<StockCopilotSearchDataDto>> SearchAsync(string query, bool trustedOnly, string? taskId, CancellationToken cancellationToken = default);
}

public sealed class McpToolGateway : IMcpToolGateway
{
    private readonly IStockCopilotMcpService _stockCopilotMcpService;
    private readonly IMcpServiceRegistry _registry;
    private readonly IRoleToolPolicyService _roleToolPolicyService;
    private readonly ILogger<McpToolGateway> _logger;

    public McpToolGateway(
        IStockCopilotMcpService stockCopilotMcpService,
        IMcpServiceRegistry registry,
        IRoleToolPolicyService roleToolPolicyService,
        ILogger<McpToolGateway> logger)
    {
        _stockCopilotMcpService = stockCopilotMcpService;
        _registry = registry;
        _roleToolPolicyService = roleToolPolicyService;
        _logger = logger;
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotCompanyOverviewDataDto>> GetCompanyOverviewAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.CompanyOverview);
        return ExecuteWithLoggingAsync(StockMcpToolNames.CompanyOverview, symbol,
            () => _stockCopilotMcpService.GetCompanyOverviewAsync(symbol, taskId, window, cancellationToken));
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotProductDataDto>> GetProductAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.Product);
        return ExecuteWithLoggingAsync(StockMcpToolNames.Product, symbol,
            () => _stockCopilotMcpService.GetProductAsync(symbol, taskId, window, cancellationToken));
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotFundamentalsDataDto>> GetFundamentalsAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.Fundamentals);
        return ExecuteWithLoggingAsync(StockMcpToolNames.Fundamentals, symbol,
            () => _stockCopilotMcpService.GetFundamentalsAsync(symbol, taskId, window, cancellationToken));
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotShareholderDataDto>> GetShareholderAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.Shareholder);
        return ExecuteWithLoggingAsync(StockMcpToolNames.Shareholder, symbol,
            () => _stockCopilotMcpService.GetShareholderAsync(symbol, taskId, window, cancellationToken));
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotMarketContextDataDto>> GetMarketContextAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.MarketContext);
        return ExecuteWithLoggingAsync(StockMcpToolNames.MarketContext, symbol,
            () => _stockCopilotMcpService.GetMarketContextAsync(symbol, taskId, window, cancellationToken));
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotSocialSentimentDataDto>> GetSocialSentimentAsync(string symbol, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.SocialSentiment);
        return ExecuteWithLoggingAsync(StockMcpToolNames.SocialSentiment, symbol,
            () => _stockCopilotMcpService.GetSocialSentimentAsync(symbol, taskId, window, cancellationToken));
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotKlineDataDto>> GetKlineAsync(string symbol, string interval, int count, string? source, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.Kline);
        return ExecuteWithLoggingAsync(StockMcpToolNames.Kline, symbol,
            () => _stockCopilotMcpService.GetKlineAsync(symbol, interval, count, source, taskId, window, cancellationToken));
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotMinuteDataDto>> GetMinuteAsync(string symbol, string? source, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.Minute);
        return ExecuteWithLoggingAsync(StockMcpToolNames.Minute, symbol,
            () => _stockCopilotMcpService.GetMinuteAsync(symbol, source, taskId, window, cancellationToken));
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotStrategyDataDto>> GetStrategyAsync(string symbol, string interval, int count, string? source, IReadOnlyList<string>? strategies, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.Strategy);
        return ExecuteWithLoggingAsync(StockMcpToolNames.Strategy, symbol,
            () => _stockCopilotMcpService.GetStrategyAsync(symbol, interval, count, source, strategies, taskId, window, cancellationToken));
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotNewsDataDto>> GetNewsAsync(string symbol, string level, string? taskId, StockCopilotMcpWindowOptions? window = null, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.News);
        return ExecuteWithLoggingAsync(StockMcpToolNames.News, symbol,
            () => _stockCopilotMcpService.GetNewsAsync(symbol, level, taskId, window, cancellationToken));
    }

    public Task<StockCopilotMcpEnvelopeDto<StockCopilotSearchDataDto>> SearchAsync(string query, bool trustedOnly, string? taskId, CancellationToken cancellationToken = default)
    {
        EnsureSystemToolAccess(StockMcpToolNames.Search);
        return ExecuteWithLoggingAsync(StockMcpToolNames.Search, query,
            () => _stockCopilotMcpService.SearchAsync(query, trustedOnly, taskId, cancellationToken));
    }

    private async Task<T> ExecuteWithLoggingAsync<T>(string toolName, string key, Func<Task<T>> action)
    {
        _logger.LogDebug("MCP tool {Tool} starting for {Key}", toolName, key);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            sw.Stop();
            _logger.LogInformation("MCP tool {Tool} completed for {Key} in {ElapsedMs}ms", toolName, key, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "MCP tool {Tool} failed for {Key} after {ElapsedMs}ms", toolName, key, sw.ElapsedMilliseconds);
            throw;
        }
    }

    private void EnsureSystemToolAccess(string toolName)
    {
        _registry.GetRequired(toolName);
        var result = _roleToolPolicyService.AuthorizeSystemEndpoint(toolName);
        if (!result.IsAllowed)
        {
            throw new InvalidOperationException(result.ErrorCode ?? McpErrorCodes.SystemEndpointNotAuthorized);
        }
    }
}