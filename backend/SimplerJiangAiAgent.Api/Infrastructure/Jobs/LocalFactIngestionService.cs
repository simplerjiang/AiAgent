using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public interface ILocalFactIngestionService
{
    Task SyncAsync(CancellationToken cancellationToken = default);
    Task EnsureFreshAsync(string symbol, CancellationToken cancellationToken = default);
}

public sealed class LocalFactIngestionService : ILocalFactIngestionService
{
    private const string SinaRollUrl = "https://feed.mix.sina.com.cn/api/roll/get?pageid=155&lid=1686&num=60&versionNumber=1.2.8.1";
    private static readonly SemaphoreSlim MarketRefreshGate = new(1, 1);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SymbolRefreshGates = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] MarketKeywords =
    {
        "A股", "大盘", "沪指", "深成指", "创业板", "科创板", "两市", "收盘", "午评", "早评", "北向资金", "指数"
    };

    private readonly AppDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly StockSyncOptions _options;
    private readonly ILogger<LocalFactIngestionService> _logger;

    public LocalFactIngestionService(
        AppDbContext dbContext,
        HttpClient httpClient,
        IOptions<StockSyncOptions> options,
        ILogger<LocalFactIngestionService> logger)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        var symbols = await GetTrackedSymbolsAsync(cancellationToken);
        if (symbols.Count == 0)
        {
            return;
        }

        var crawledAt = DateTime.UtcNow;
        var rollMessages = await FetchRollMessagesAsync(cancellationToken);

        await MarketRefreshGate.WaitAsync(cancellationToken);
        try
        {
            await UpsertMarketReportsAsync(rollMessages, crawledAt, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            MarketRefreshGate.Release();
        }

        foreach (var symbol in symbols)
        {
            var symbolGate = GetSymbolGate(symbol);
            await symbolGate.WaitAsync(cancellationToken);
            try
            {
                await SyncSymbolAsync(symbol, rollMessages, crawledAt, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步本地事实失败: {Symbol}", symbol);
            }
            finally
            {
                symbolGate.Release();
            }
        }
    }

    public async Task EnsureFreshAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var normalized = StockSymbolNormalizer.Normalize(symbol);
        var freshCutoff = DateTime.UtcNow.AddMinutes(-30);
        IReadOnlyList<IntradayMessageDto>? rollMessages = null;

        await MarketRefreshGate.WaitAsync(cancellationToken);
        try
        {
            var hasFreshMarket = await _dbContext.LocalSectorReports
                .AnyAsync(item => item.Level == "market" && item.CrawledAt >= freshCutoff, cancellationToken);

            if (!hasFreshMarket)
            {
                var crawledAt = DateTime.UtcNow;
                rollMessages = await FetchRollMessagesAsync(cancellationToken);
                await UpsertMarketReportsAsync(rollMessages, crawledAt, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        finally
        {
            MarketRefreshGate.Release();
        }

        var symbolGate = GetSymbolGate(normalized);
        await symbolGate.WaitAsync(cancellationToken);
        try
        {
            var hasFreshStockNews = await _dbContext.LocalStockNews
                .AnyAsync(item => item.Symbol == normalized && item.CrawledAt >= freshCutoff, cancellationToken);

            var hasFreshSector = await _dbContext.LocalSectorReports
                .AnyAsync(item => item.Symbol == normalized && item.Level == "sector" && item.CrawledAt >= freshCutoff, cancellationToken);

            if (hasFreshStockNews && hasFreshSector)
            {
                return;
            }

            var crawledAt = DateTime.UtcNow;
            rollMessages ??= await FetchRollMessagesAsync(cancellationToken);
            await SyncSymbolAsync(normalized, rollMessages, crawledAt, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            symbolGate.Release();
        }
    }

    private static SemaphoreSlim GetSymbolGate(string symbol)
    {
        return SymbolRefreshGates.GetOrAdd(symbol, _ => new SemaphoreSlim(1, 1));
    }

    private async Task<IReadOnlyList<string>> GetTrackedSymbolsAsync(CancellationToken cancellationToken)
    {
        var configured = _options.Symbols
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => StockSymbolNormalizer.Normalize(item.Trim()));

        var recent = await _dbContext.StockQueryHistories
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => item.Symbol)
            .Take(20)
            .ToListAsync(cancellationToken);

        return configured
            .Concat(recent.Select(StockSymbolNormalizer.Normalize))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task SyncSymbolAsync(
        string symbol,
        IReadOnlyList<IntradayMessageDto> rollMessages,
        DateTime crawledAt,
        CancellationToken cancellationToken)
    {
        var profile = await FetchCompanyProfileAsync(symbol, cancellationToken);
        var announcementJson = await _httpClient.GetStringAsync(BuildAnnouncementUrl(symbol), cancellationToken);
        var announcementItems = EastmoneyAnnouncementParser.Parse(symbol, profile.Name, profile.SectorName, announcementJson, crawledAt);

        var companyHtml = await _httpClient.GetStringAsync($"https://finance.sina.com.cn/realstock/company/{symbol}/nc.shtml", cancellationToken);
        var companyNews = SinaCompanyNewsParser.ParseCompanyNews(companyHtml)
            .Select(item => new LocalStockNewsSeed(
                symbol,
                profile.Name,
                profile.SectorName,
                item.Title,
                "company_news",
                item.Source,
                "sina-company-news",
                item.Url,
                item.PublishedAt,
                crawledAt,
                item.Url))
            .ToArray();

        await UpsertStockNewsAsync(symbol, announcementItems.Concat(companyNews).ToArray(), cancellationToken);
        await UpsertSectorReportsAsync(symbol, profile.SectorName, rollMessages, crawledAt, cancellationToken);
    }

    private async Task<EastmoneyCompanyProfileDto> FetchCompanyProfileAsync(string symbol, CancellationToken cancellationToken)
    {
        var marketPrefix = symbol.StartsWith("sh", StringComparison.OrdinalIgnoreCase) ? "SH" : "SZ";
        var code = symbol[2..];
        var url = $"https://emweb.securities.eastmoney.com/PC_HSF10/CompanySurvey/CompanySurveyAjax?code={marketPrefix}{code}";
        var json = await _httpClient.GetStringAsync(url, cancellationToken);
        return EastmoneyCompanyProfileParser.Parse(symbol, json);
    }

    private async Task<IReadOnlyList<IntradayMessageDto>> FetchRollMessagesAsync(CancellationToken cancellationToken)
    {
        var json = await _httpClient.GetStringAsync(SinaRollUrl, cancellationToken);
        return SinaRollParser.ParseRollMessages(json, string.Empty)
            .OrderByDescending(item => item.PublishedAt)
            .ToArray();
    }

    private static string BuildAnnouncementUrl(string symbol)
    {
        var code = symbol[2..];
        return $"https://np-anotice-stock.eastmoney.com/api/security/ann?page_size=30&page_index=1&ann_type=A&client_source=web&stock_list={code}";
    }

    private async Task UpsertStockNewsAsync(string symbol, IReadOnlyList<LocalStockNewsSeed> items, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.LocalStockNews
            .Where(item => item.Symbol == symbol)
            .ToListAsync(cancellationToken);

        _dbContext.LocalStockNews.RemoveRange(existing);
        _dbContext.LocalStockNews.AddRange(items
            .OrderByDescending(item => item.PublishTime)
            .Take(40)
            .Select(item => new LocalStockNews
            {
                Symbol = item.Symbol,
                Name = item.Name,
                SectorName = item.SectorName,
                Title = item.Title,
                Category = item.Category,
                Source = item.Source,
                SourceTag = item.SourceTag,
                ExternalId = item.ExternalId,
                PublishTime = item.PublishTime,
                CrawledAt = item.CrawledAt,
                Url = item.Url
            }));
    }

    private async Task UpsertSectorReportsAsync(
        string symbol,
        string? sectorName,
        IReadOnlyList<IntradayMessageDto> rollMessages,
        DateTime crawledAt,
        CancellationToken cancellationToken)
    {
        var sectorReports = BuildSectorReports(symbol, sectorName, rollMessages, crawledAt);
        var existing = await _dbContext.LocalSectorReports
            .Where(item => item.Symbol == symbol && item.Level == "sector")
            .ToListAsync(cancellationToken);

        _dbContext.LocalSectorReports.RemoveRange(existing);
        _dbContext.LocalSectorReports.AddRange(sectorReports.Select(item => new LocalSectorReport
        {
            Symbol = item.Symbol,
            SectorName = item.SectorName,
            Level = item.Level,
            Title = item.Title,
            Source = item.Source,
            SourceTag = item.SourceTag,
            ExternalId = item.ExternalId,
            PublishTime = item.PublishTime,
            CrawledAt = item.CrawledAt,
            Url = item.Url
        }));
    }

    private async Task UpsertMarketReportsAsync(
        IReadOnlyList<IntradayMessageDto> rollMessages,
        DateTime crawledAt,
        CancellationToken cancellationToken)
    {
        var reports = BuildMarketReports(rollMessages, crawledAt);

        var existing = await _dbContext.LocalSectorReports
            .Where(item => item.Level == "market")
            .ToListAsync(cancellationToken);

        _dbContext.LocalSectorReports.RemoveRange(existing);
        _dbContext.LocalSectorReports.AddRange(reports.Select(item => new LocalSectorReport
        {
            Symbol = item.Symbol,
            SectorName = item.SectorName,
            Level = item.Level,
            Title = item.Title,
            Source = item.Source,
            SourceTag = item.SourceTag,
            ExternalId = item.ExternalId,
            PublishTime = item.PublishTime,
            CrawledAt = item.CrawledAt,
            Url = item.Url
        }));
    }

    internal static IReadOnlyList<LocalSectorReportSeed> BuildSectorReports(
        string symbol,
        string? sectorName,
        IReadOnlyList<IntradayMessageDto> rollMessages,
        DateTime crawledAt)
    {
        if (string.IsNullOrWhiteSpace(sectorName))
        {
            return Array.Empty<LocalSectorReportSeed>();
        }

        var sectorKeywords = BuildSectorKeywords(sectorName);

        return rollMessages
            .Where(item => sectorKeywords.Any(keyword => item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(item => item.PublishedAt)
            .Take(12)
            .Select(item => new LocalSectorReportSeed(
                symbol,
                sectorName,
                "sector",
                item.Title,
                item.Source,
                "sina-roll-sector",
                item.Url,
                item.PublishedAt,
                crawledAt,
                item.Url))
            .ToArray();
    }

    internal static IReadOnlyList<LocalSectorReportSeed> BuildMarketReports(
        IReadOnlyList<IntradayMessageDto> rollMessages,
        DateTime crawledAt)
    {
        var matched = rollMessages
            .Where(item => MarketKeywords.Any(keyword => item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(item => item.PublishedAt)
            .Take(12)
            .ToArray();

        var source = matched.Length > 0
            ? matched
            : rollMessages.OrderByDescending(item => item.PublishedAt).Take(12).ToArray();

        return source
            .Select(item => new LocalSectorReportSeed(
                null,
                "大盘环境",
                "market",
                item.Title,
                item.Source,
                "sina-roll-market",
                item.Url,
                item.PublishedAt,
                crawledAt,
                item.Url))
            .ToArray();
    }

    internal static IReadOnlyList<string> BuildSectorKeywords(string sectorName)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            sectorName.Trim()
        };

        foreach (var raw in sectorName.Split(new[] { '/', '、', '（', '）', '(', ')', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            tokens.Add(raw);
        }

        foreach (var token in tokens.ToArray())
        {
            var compact = token
                .Replace("行业", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("板块", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("概念", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Ⅱ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Ⅲ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (!string.IsNullOrWhiteSpace(compact))
            {
                tokens.Add(compact);
                tokens.Add($"{compact}板块");
                tokens.Add($"{compact}行业");
                tokens.Add($"{compact}概念");
            }
        }

        return tokens.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
    }
}