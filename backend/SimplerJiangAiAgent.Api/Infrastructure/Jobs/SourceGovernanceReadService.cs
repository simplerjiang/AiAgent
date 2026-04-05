using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using SimplerJiangAiAgent.Api.Data.Entities;
using SimplerJiangAiAgent.Api.Infrastructure.Storage;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public interface ISourceGovernanceReadService
{
    Task<SourceGovernanceOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<SourceRegistryListItemDto>> GetSourcesAsync(string? status, string? tier, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<SourceCandidateListItemDto>> GetCandidatesAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<CrawlerChangeListItemDto>> GetChangesAsync(string? status, string? domain, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CrawlerChangeDetailDto?> GetChangeDetailAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GovernanceErrorSnapshotDto>> GetErrorSnapshotsAsync(int take, CancellationToken cancellationToken = default);
    Task<TraceSearchResultDto> SearchTraceAsync(string traceId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LlmConversationLogItemDto>> GetLlmConversationLogsAsync(int take, string? keyword, CancellationToken cancellationToken = default);
}

public sealed class SourceGovernanceReadService : ISourceGovernanceReadService
{
    private readonly AppDbContext _dbContext;
    private readonly string _logPath;
    private static readonly Regex TraceRegex = new("traceId=([^\\s]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex StageRegex = new("stage=([^\\s]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ProviderRegex = new("provider=([^\\s]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ModelRegex = new("model=([^\\s]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private const string EnglishTitleWordPattern = "(?:[A-Z][A-Za-z'&/-]*|the|and|of|to|for|in|on|with|from|a|an)";
    private static readonly Regex LeadingReasoningTitleBlockRegex = new(
        $"^(?:\\s|[*#>`\"'_\\-])*(?:(?:\\*{{0,2}}{EnglishTitleWordPattern}(?:\\s+{EnglishTitleWordPattern}){{0,7}}\\*{{0,2}})(?:[:：-]?\\s*)){{2,}}",
        RegexOptions.Compiled);
    private static readonly Regex LeadingEmphasizedTitleSequenceRegex = new(
        "^(?:\\s|[*#>`\"'_\\-])*(?:\\*{1,2}[A-Z][A-Za-z'&/-]*(?:\\s+[A-Z][A-Za-z'&/-]*){1,7}\\*{1,2}(?:\\s+|[:：-]?\\s*)){2,}",
        RegexOptions.Compiled);
    private static readonly string[] LeadingEnglishReasoningNarrativeMarkers =
    {
        "i'm currently dissecting",
        "i am currently dissecting",
        "i'm now focusing on",
        "i am now focusing on",
        "here's how i'm approaching this",
        "here is how i'm approaching this",
        "the role is clear",
        "the task is clear",
        "the task is straightforward",
        "the core objective is",
        "i'll need to consider",
        "i will need to consider",
        "my analysis centers on",
        "focusing on the core task",
        "focusing on the core objective"
    };
    private static readonly string[] LeadingEnglishReasoningNarrativeActorMarkers =
    {
        "i'm",
        "i am",
        "i've",
        "i have",
        "i'll",
        "i will",
        "my ",
        "here's",
        "here is",
        "let's",
        "the user",
        "the prompt",
        "the task",
        "the role",
        "the goal"
    };
    private static readonly string[] LeadingEnglishReasoningNarrativeMetaMarkers =
    {
        "json array",
        "structured json",
        "prompt",
        "task",
        "objective",
        "constraint",
        "approach",
        "analysis",
        "analyzing",
        "dissecting",
        "focus",
        "focusing",
        "consider",
        "generate",
        "delivering",
        "adhere"
    };

    public SourceGovernanceReadService(AppDbContext dbContext, AppRuntimePaths runtimePaths)
    {
        _dbContext = dbContext;
        _logPath = Path.Combine(runtimePaths.LogsPath, "llm-requests.txt");
    }

    public SourceGovernanceReadService(AppDbContext dbContext, IHostEnvironment environment)
    {
        _dbContext = dbContext;
        _logPath = Path.Combine(environment.ContentRootPath, "App_Data", "logs", "llm-requests.txt");
    }

    public async Task<SourceGovernanceOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var activeSources = await _dbContext.NewsSourceRegistries.CountAsync(x => x.Status == NewsSourceStatus.Active, cancellationToken);
        var quarantinedSources = await _dbContext.NewsSourceRegistries.CountAsync(x => x.Status == NewsSourceStatus.Quarantine, cancellationToken);
        var pendingCandidates = await _dbContext.NewsSourceCandidates.CountAsync(x => x.Status == NewsSourceStatus.Pending, cancellationToken);
        var pendingChanges = await _dbContext.CrawlerChangeQueues.CountAsync(x => x.Status == CrawlerChangeStatus.Pending || x.Status == CrawlerChangeStatus.Generated || x.Status == CrawlerChangeStatus.Validated, cancellationToken);
        var deployedChanges = await _dbContext.CrawlerChangeQueues.CountAsync(x => x.Status == CrawlerChangeStatus.Deployed, cancellationToken);

        var rollbackSince = DateTime.UtcNow.AddDays(-7);
        var rollbackCount7d = await _dbContext.CrawlerChangeRuns.CountAsync(x => x.Result == CrawlerChangeStatus.RolledBack && x.ExecutedAt >= rollbackSince, cancellationToken);

        var latestVerification = await _dbContext.NewsSourceVerificationRuns
            .OrderByDescending(x => x.ExecutedAt)
            .Select(x => (DateTime?)x.ExecutedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var latestChangeRun = await _dbContext.CrawlerChangeRuns
            .OrderByDescending(x => x.ExecutedAt)
            .Select(x => (DateTime?)x.ExecutedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var latestRunAt = new[] { latestVerification, latestChangeRun }.Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty().Max();

        var errorSince = DateTime.UtcNow.AddHours(-24);
        var verificationErrors = await _dbContext.NewsSourceVerificationRuns.CountAsync(x => !x.Success && x.ExecutedAt >= errorSince, cancellationToken);
        var crawlerErrors = await _dbContext.CrawlerChangeRuns.CountAsync(x => x.Result == CrawlerChangeStatus.Rejected && x.ExecutedAt >= errorSince, cancellationToken);

        return new SourceGovernanceOverviewDto(
            activeSources,
            quarantinedSources,
            pendingCandidates,
            pendingChanges,
            deployedChanges,
            rollbackCount7d,
            latestRunAt == default ? null : latestRunAt,
            verificationErrors + crawlerErrors);
    }

    public async Task<PagedResult<SourceRegistryListItemDto>> GetSourcesAsync(string? status, string? tier, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedStatus = status?.Trim();
        var normalizedTier = tier?.Trim();
        var query = _dbContext.NewsSourceRegistries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(normalizedTier))
        {
            query = query.Where(x => x.Tier == normalizedTier);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SourceRegistryListItemDto(
                x.Id,
                x.Domain,
                x.BaseUrl,
                x.Tier,
                x.Status,
                x.QualityScore,
                x.ParseSuccessRate,
                x.TimestampCoverage,
                x.FreshnessLagMinutes,
                x.ConsecutiveFailures,
                x.LastStatusReason,
                x.LastCheckedAt,
                x.UpdatedAt,
                _dbContext.NewsSourceVerificationRuns
                    .Where(v => v.Domain == x.Domain)
                    .OrderByDescending(v => v.ExecutedAt)
                    .Select(v => v.TraceId)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<SourceRegistryListItemDto>(total, page, pageSize, items);
    }

    public async Task<PagedResult<SourceCandidateListItemDto>> GetCandidatesAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedStatus = status?.Trim();
        var query = _dbContext.NewsSourceCandidates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            query = query.Where(x => x.Status == normalizedStatus);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.DiscoveredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SourceCandidateListItemDto(
                x.Id,
                x.Domain,
                x.HomepageUrl,
                x.ProposedTier,
                x.Status,
                x.DiscoveryReason,
                x.FetchStrategy,
                x.VerificationScore,
                x.ParseSuccessRate,
                x.TimestampCoverage,
                x.FreshnessLagMinutes,
                x.DiscoveredAt,
                x.VerifiedAt,
                _dbContext.NewsSourceVerificationRuns
                    .Where(v => v.CandidateId == x.Id)
                    .OrderByDescending(v => v.ExecutedAt)
                    .Select(v => v.TraceId)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<SourceCandidateListItemDto>(total, page, pageSize, items);
    }

    public async Task<PagedResult<CrawlerChangeListItemDto>> GetChangesAsync(string? status, string? domain, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedStatus = status?.Trim();
        var normalizedDomain = domain?.Trim();
        var query = _dbContext.CrawlerChangeQueues.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(normalizedDomain))
        {
            query = query.Where(x => x.Domain.Contains(normalizedDomain));
        }

        var total = await query.CountAsync(cancellationToken);
        var pageItems = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var queueIds = pageItems.Select(x => x.Id).ToArray();
        var latestRuns = await _dbContext.CrawlerChangeRuns
            .AsNoTracking()
            .Where(x => queueIds.Contains(x.QueueId))
            .OrderByDescending(x => x.ExecutedAt)
            .GroupBy(x => x.QueueId)
            .Select(x => x.First())
            .ToDictionaryAsync(x => x.QueueId, cancellationToken);

        var items = pageItems.Select(x =>
        {
            latestRuns.TryGetValue(x.Id, out var latestRun);
            return new CrawlerChangeListItemDto(
                x.Id,
                x.TraceId,
                x.SourceId,
                x.Domain,
                x.Status,
                x.TriggerReason,
                x.ProposedPatchSummary,
                x.ValidationNote,
                x.CreatedAt,
                x.UpdatedAt,
                latestRun?.Result,
                latestRun?.ExecutedAt,
                latestRun?.Note,
                latestRun?.TraceId);
        }).ToList();

        return new PagedResult<CrawlerChangeListItemDto>(total, page, pageSize, items);
    }

    public async Task<CrawlerChangeDetailDto?> GetChangeDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        var queue = await _dbContext.CrawlerChangeQueues
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (queue is null)
        {
            return null;
        }

        var targetFiles = DeserializeStringArray(queue.ProposedFilesJson);
        var patchCount = DeserializeObjectArrayLength(queue.ProposedPatchJson);

        var runItems = await _dbContext.CrawlerChangeRuns
            .AsNoTracking()
            .Where(x => x.QueueId == queue.Id)
            .OrderByDescending(x => x.ExecutedAt)
            .Take(20)
            .Select(x => new CrawlerChangeRunItemDto(
                x.Id,
                x.TraceId,
                x.Result,
                x.Note,
                x.ExecutedAt))
            .ToListAsync(cancellationToken);

        return new CrawlerChangeDetailDto(
            queue.Id,
            queue.TraceId,
            queue.Domain,
            queue.Status,
            queue.TriggerReason,
            queue.ProposedPatchSummary,
            queue.ValidationNote,
            queue.ProposedTestCommand,
            queue.ProposedReplayCommand,
            targetFiles,
            patchCount,
            queue.CreatedAt,
            queue.UpdatedAt,
            runItems);
    }

    public async Task<IReadOnlyList<GovernanceErrorSnapshotDto>> GetErrorSnapshotsAsync(int take, CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 100);

        var verificationErrors = await _dbContext.NewsSourceVerificationRuns
            .AsNoTracking()
            .Where(x => !x.Success)
            .OrderByDescending(x => x.ExecutedAt)
            .Take(normalizedTake)
            .Select(x => new GovernanceErrorSnapshotDto(
                "verification",
                x.Domain,
                x.FailureReason ?? "verify_failed",
                x.ExecutedAt,
                x.VerificationScore,
                x.HttpStatusCode,
                null,
                x.TraceId))
            .ToListAsync(cancellationToken);

        var changeErrors = await _dbContext.CrawlerChangeRuns
            .AsNoTracking()
            .Where(x => x.Result == CrawlerChangeStatus.Rejected || x.Result == CrawlerChangeStatus.RolledBack)
            .OrderByDescending(x => x.ExecutedAt)
            .Take(normalizedTake)
            .Select(x => new GovernanceErrorSnapshotDto(
                "crawler",
                x.Domain,
                x.Note ?? x.Result,
                x.ExecutedAt,
                null,
                null,
                x.QueueId,
                x.TraceId))
            .ToListAsync(cancellationToken);

        return verificationErrors
            .Concat(changeErrors)
            .OrderByDescending(x => x.OccurredAt)
            .Take(normalizedTake)
            .ToList();
    }

    public async Task<TraceSearchResultDto> SearchTraceAsync(string traceId, int take, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(traceId) || traceId.Length < 8)
        {
            return new TraceSearchResultDto(traceId, Array.Empty<string>(), Array.Empty<TraceTimelineItemDto>());
        }

        var timeline = await BuildTraceTimelineAsync(traceId, Math.Clamp(take, 1, 200), cancellationToken);

        var lines = await FindTraceLinesAsync(traceId, take, cancellationToken);
        return new TraceSearchResultDto(traceId, lines, timeline);
    }

    public async Task<IReadOnlyList<LlmConversationLogItemDto>> GetLlmConversationLogsAsync(int take, string? keyword, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_logPath))
        {
            return Array.Empty<LlmConversationLogItemDto>();
        }

        var normalizedTake = Math.Clamp(take, 1, 1000);
        var normalizedKeyword = keyword?.Trim();

        var sessions = new Dictionary<string, LlmConversationLogSessionBuilder>(StringComparer.OrdinalIgnoreCase);
        await using var stream = File.OpenRead(_logPath);
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (!line.Contains("[LLM", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parsed = ParseLlmLogLine(line);
            if (string.IsNullOrWhiteSpace(parsed.TraceId))
            {
                continue;
            }

            if (!sessions.TryGetValue(parsed.TraceId!, out var session))
            {
                session = new LlmConversationLogSessionBuilder(parsed.TraceId!);
                sessions[parsed.TraceId!] = session;
            }

            session.Add(parsed);
        }

        var matched = sessions.Values
            .Select(session => session.Build())
            .Where(item => MatchesKeyword(item, normalizedKeyword))
            .OrderByDescending(item => item.LastSeenAt ?? item.Timestamp)
            .Take(normalizedTake)
            .ToList();

        return matched;
    }

    private async Task<IReadOnlyList<TraceTimelineItemDto>> BuildTraceTimelineAsync(string traceId, int take, CancellationToken cancellationToken)
    {
        var verificationItems = await _dbContext.NewsSourceVerificationRuns
            .AsNoTracking()
            .Where(x => x.TraceId == traceId)
            .OrderByDescending(x => x.ExecutedAt)
            .Take(take)
            .Select(x => new TraceTimelineItemDto(
                "verification",
                x.Domain,
                x.Success ? "success" : "failed",
                x.FailureReason,
                x.ExecutedAt,
                x.TraceId,
                null))
            .ToListAsync(cancellationToken);

        var queueItems = await _dbContext.CrawlerChangeQueues
            .AsNoTracking()
            .Where(x => x.TraceId == traceId)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Take(take)
            .Select(x => new TraceTimelineItemDto(
                "queue",
                x.Domain,
                x.Status,
                x.ValidationNote,
                x.UpdatedAt ?? x.CreatedAt,
                x.TraceId,
                x.Id))
            .ToListAsync(cancellationToken);

        var runItems = await _dbContext.CrawlerChangeRuns
            .AsNoTracking()
            .Where(x => x.TraceId == traceId)
            .OrderByDescending(x => x.ExecutedAt)
            .Take(take)
            .Select(x => new TraceTimelineItemDto(
                "run",
                x.Domain,
                x.Result,
                x.Note,
                x.ExecutedAt,
                x.TraceId,
                x.QueueId))
            .ToListAsync(cancellationToken);

        return verificationItems
            .Concat(queueItems)
            .Concat(runItems)
            .OrderByDescending(x => x.OccurredAt)
            .Take(take)
            .ToList();
    }

    private async Task<IReadOnlyList<string>> FindTraceLinesAsync(string traceId, int take, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(traceId) || traceId.Length < 8)
        {
            return Array.Empty<string>();
        }

        if (!File.Exists(_logPath))
        {
            return Array.Empty<string>();
        }

        var normalizedTake = Math.Clamp(take, 1, 200);
        var matches = new List<string>(normalizedTake);
        var file = new FileInfo(_logPath);

        if (file.Length <= 0)
        {
            return matches;
        }

        // Stream lines and keep only the latest N matches for developer diagnostics.
        await using var logStream = File.OpenRead(_logPath);
        using var reader = new StreamReader(logStream);
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (!line.Contains(traceId, StringComparison.OrdinalIgnoreCase))
                continue;

            matches.Add(line);
        }

        // Keep only trailing N matches
        if (matches.Count > normalizedTake)
        {
            matches.RemoveRange(0, matches.Count - normalizedTake);
        }

        return matches;
    }

    private static IReadOnlyList<string> DeserializeStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            var values = JsonSerializer.Deserialize<List<string>>(json);
            return values is null ? Array.Empty<string>() : values;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static int DeserializeObjectArrayLength(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return 0;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.GetArrayLength() : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static LlmConversationLogLineDto ParseLlmLogLine(string line)
    {
        var level = line.Contains("[LLM-AUDIT]", StringComparison.OrdinalIgnoreCase) ? "LLM-AUDIT" : "LLM";
        var timestamp = string.Empty;
        if (line.Length >= 23)
        {
            timestamp = line[..23];
        }

        var traceId = MatchValue(TraceRegex, line);
        var stage = MatchValue(StageRegex, line);
        var provider = MatchValue(ProviderRegex, line);
        var model = MatchValue(ModelRegex, line);

        return new LlmConversationLogLineDto(
            timestamp,
            level,
            traceId,
            stage,
            provider,
            model,
            line);
    }

    private static bool MatchesKeyword(LlmConversationLogItemDto item, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return new[]
        {
            item.TraceId,
            item.Provider,
            item.Model,
            item.Status,
            item.RequestText,
            item.ResponseText,
            item.ErrorText,
            item.Raw,
            item.RequestRaw,
            item.ResponseRaw,
            item.ErrorRaw
        }.Any(value => !string.IsNullOrWhiteSpace(value) && value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class LlmConversationLogSessionBuilder
    {
        private readonly string _traceId;
        private readonly List<string> _lines = new();
        private readonly HashSet<string> _stages = new(StringComparer.OrdinalIgnoreCase);
        private string _timestamp = string.Empty;
        private string _level = "LLM-AUDIT";
        private string? _provider;
        private string? _model;
        private string? _requestRaw;
        private string? _responseRaw;
        private string? _errorRaw;
        private string? _requestText;
        private string? _responseText;
        private string? _errorText;
        private string? _status;
        private string? _lastSeenAt;

        public LlmConversationLogSessionBuilder(string traceId)
        {
            _traceId = traceId;
        }

        public void Add(LlmConversationLogLineDto item)
        {
            if (string.IsNullOrWhiteSpace(_timestamp))
            {
                _timestamp = item.Timestamp;
            }

            _lastSeenAt = item.Timestamp;
            _level = _level == "LLM-AUDIT" || item.Level != "LLM-AUDIT" ? _level : item.Level;
            _provider ??= item.Provider;
            _model ??= item.Model;
            _lines.Add(item.Raw);

            if (!string.IsNullOrWhiteSpace(item.Stage))
            {
                _stages.Add(item.Stage!);
            }

            var extracted = ExtractLlmLogField(item.Raw, item.Stage);
            if (IsRequestStage(item.Stage))
            {
                _requestRaw ??= item.Raw;
                _requestText ??= extracted;
                _status ??= "request";
                return;
            }

            if (IsResponseStage(item.Stage))
            {
                _responseRaw ??= item.Raw;
                _responseText ??= extracted;
                _status = "response";
                return;
            }

            if (IsErrorStage(item.Stage))
            {
                _errorRaw ??= item.Raw;
                _errorText ??= extracted;
                _status = "error";
            }
        }

        public LlmConversationLogItemDto Build()
        {
            var status = _status;
            if (string.IsNullOrWhiteSpace(status))
            {
                status = _responseRaw is not null ? "response" : _errorRaw is not null ? "error" : "request";
            }

            return new LlmConversationLogItemDto(
                _timestamp,
                _level,
                _traceId,
                status,
                _provider,
                _model,
                _requestRaw,
                _responseRaw,
                _errorRaw,
                _requestText,
                _responseText,
                _errorText,
                _lastSeenAt,
                _lines.AsReadOnly(),
                _lines.Count > 0 ? string.Join(Environment.NewLine, _lines) : string.Empty,
                _stages.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray());
        }

        private static bool IsRequestStage(string? stage)
        {
            return stage is not null && stage.StartsWith("request", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsResponseStage(string? stage)
        {
            return stage is not null && stage.StartsWith("response", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsErrorStage(string? stage)
        {
            return stage is not null && stage.StartsWith("error", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ExtractLlmLogField(string raw, string? stage)
    {
        var decoded = raw
            .Replace("\\r", string.Empty, StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\t", "\t", StringComparison.Ordinal);

        if (stage is not null && stage.StartsWith("request", StringComparison.OrdinalIgnoreCase))
        {
            return "请求内容已脱敏；界面仅保留必要元数据与结构化 JSON。";
        }

        foreach (var marker in new[] { "prompt=", "userPrompt=", "content=", "payload=", "message=" })
        {
            var index = decoded.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return SanitizeLlmDisplayText(decoded[(index + marker.Length)..].Trim());
            }
        }

        return SanitizeLlmDisplayText(decoded);
    }

    private static string SanitizeLlmDisplayText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = Regex.Replace(value, "<think>[\\s\\S]*?</think>", string.Empty, RegexOptions.IgnoreCase)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Trim();

        var hadReasoningScaffold = ContainsReasoningScaffold(sanitized);

        sanitized = Regex.Replace(
            sanitized,
            "(^|\\n)#{0,6}\\s*(思考过程|推理过程|reasoning|analysis|chain of thought|chain-of-thought)[^\\n]*(\\n[\\s\\S]*)?$",
            string.Empty,
            RegexOptions.IgnoreCase);

        sanitized = Regex.Replace(
            sanitized,
            "(\\*{0,2}\\s*)?(initiating market analysis|refining search strategies|adapting query approach|analyzing current context|reviewing market trends|considering the request|analyzing the request|analyzing the scenario|analyzing the data|refining the strategy|refining the approach|simulating the search|simulating information retrieval|defining the scope|interpreting the data|formulating the response|assessing risk elements|synthesizing risk insights|my thought process|thought process|let's break this down before answering|let's break this down|before answering|i need to understand|i'm zeroing in on)(\\*{0,2}\\s*)?[:：-]?\\s*",
            string.Empty,
            RegexOptions.IgnoreCase);

        while (true)
        {
            var nextValue = LeadingEmphasizedTitleSequenceRegex.Replace(sanitized, string.Empty);
            nextValue = LeadingReasoningTitleBlockRegex.Replace(nextValue, string.Empty);
            nextValue = StripLeadingEnglishReasoningNarrative(nextValue).TrimStart();
            if (ReferenceEquals(nextValue, sanitized) || nextValue == sanitized)
            {
                break;
            }

            sanitized = nextValue;
        }

        sanitized = sanitized.Trim();

        if (hadReasoningScaffold && (string.IsNullOrWhiteSpace(sanitized) || Regex.IsMatch(sanitized, "^[\\p{P}\\p{S}\\s]+$")))
        {
            return "返回内容包含中间推理，已脱敏。";
        }

        if (ContainsReasoningScaffold(sanitized))
        {
            var jsonCandidate = TryExtractJsonCandidate(sanitized);
            if (!string.IsNullOrWhiteSpace(jsonCandidate))
            {
                sanitized = jsonCandidate;
            }
            else
            {
                return "返回内容包含中间推理，已脱敏。";
            }
        }

        if (sanitized.Length > 2000)
        {
            sanitized = sanitized[..2000] + "...";
        }

        return sanitized.Trim();
    }

    private static bool ContainsReasoningScaffold(string value)
    {
        return LooksLikeLeadingEnglishReasoningNarrative(value)
            || value.Contains("my thought process", StringComparison.OrdinalIgnoreCase)
            || value.Contains("thought process", StringComparison.OrdinalIgnoreCase)
            || value.Contains("initiating market analysis", StringComparison.OrdinalIgnoreCase)
            || value.Contains("refining search strategies", StringComparison.OrdinalIgnoreCase)
            || value.Contains("adapting query approach", StringComparison.OrdinalIgnoreCase)
            || value.Contains("analyzing current context", StringComparison.OrdinalIgnoreCase)
            || value.Contains("reviewing market trends", StringComparison.OrdinalIgnoreCase)
            || value.Contains("defining the scope", StringComparison.OrdinalIgnoreCase)
            || value.Contains("considering the request", StringComparison.OrdinalIgnoreCase)
            || value.Contains("analyzing the request", StringComparison.OrdinalIgnoreCase)
            || value.Contains("analyzing the scenario", StringComparison.OrdinalIgnoreCase)
            || value.Contains("analyzing the data", StringComparison.OrdinalIgnoreCase)
            || value.Contains("refining the strategy", StringComparison.OrdinalIgnoreCase)
            || value.Contains("refining the approach", StringComparison.OrdinalIgnoreCase)
            || value.Contains("simulating the search", StringComparison.OrdinalIgnoreCase)
            || value.Contains("simulating information retrieval", StringComparison.OrdinalIgnoreCase)
            || value.Contains("interpreting the data", StringComparison.OrdinalIgnoreCase)
            || value.Contains("formulating the response", StringComparison.OrdinalIgnoreCase)
            || value.Contains("assessing risk elements", StringComparison.OrdinalIgnoreCase)
            || value.Contains("synthesizing risk insights", StringComparison.OrdinalIgnoreCase)
            || value.Contains("before answering", StringComparison.OrdinalIgnoreCase)
            || value.Contains("let's break this down", StringComparison.OrdinalIgnoreCase)
            || value.Contains("i need to understand", StringComparison.OrdinalIgnoreCase)
            || value.Contains("i'm zeroing in on", StringComparison.OrdinalIgnoreCase)
            || value.Contains("i'm currently dissecting", StringComparison.OrdinalIgnoreCase)
            || value.Contains("i am currently dissecting", StringComparison.OrdinalIgnoreCase)
            || value.Contains("i'm now focusing on", StringComparison.OrdinalIgnoreCase)
            || value.Contains("i am now focusing on", StringComparison.OrdinalIgnoreCase)
            || value.Contains("here's how i'm approaching this", StringComparison.OrdinalIgnoreCase)
            || value.Contains("here is how i'm approaching this", StringComparison.OrdinalIgnoreCase)
            || value.Contains("the role is clear", StringComparison.OrdinalIgnoreCase)
            || value.Contains("the task is clear", StringComparison.OrdinalIgnoreCase)
            || value.Contains("the task is straightforward", StringComparison.OrdinalIgnoreCase)
            || value.Contains("the core objective is", StringComparison.OrdinalIgnoreCase)
            || value.Contains("i'll need to consider", StringComparison.OrdinalIgnoreCase)
            || value.Contains("i will need to consider", StringComparison.OrdinalIgnoreCase)
            || value.Contains("my analysis centers on", StringComparison.OrdinalIgnoreCase)
            || value.Contains("focusing on the core task", StringComparison.OrdinalIgnoreCase)
            || value.Contains("focusing on the core objective", StringComparison.OrdinalIgnoreCase)
            || value.Contains("思考过程", StringComparison.OrdinalIgnoreCase)
            || value.Contains("推理过程", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripLeadingEnglishReasoningNarrative(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cutoff = FindFirstStructuredOrCjkIndex(value);
        var prefix = value[..cutoff];
        if (!LooksLikeLeadingEnglishReasoningNarrative(prefix))
        {
            return value;
        }

        return value[cutoff..].TrimStart();
    }

    private static bool LooksLikeLeadingEnglishReasoningNarrative(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = Regex.Replace(value, "\\s+", " ").Trim();
        if (normalized.Length < 24)
        {
            return false;
        }

        if (normalized.Any(IsCjkCharacter))
        {
            return false;
        }

        if (LeadingEnglishReasoningNarrativeMarkers.Any(marker => normalized.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return LeadingEnglishReasoningNarrativeActorMarkers.Any(marker => normalized.Contains(marker, StringComparison.OrdinalIgnoreCase))
            && LeadingEnglishReasoningNarrativeMetaMarkers.Any(marker => normalized.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static int FindFirstStructuredOrCjkIndex(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            if (ch == '[' || ch == '{' || IsCjkCharacter(ch))
            {
                return index;
            }
        }

        return value.Length;
    }

    private static bool IsCjkCharacter(char ch)
        => ch >= '\u3400' && ch <= '\u9fff';

    private static string TryExtractJsonCandidate(string value)
    {
        var firstBrace = value.IndexOf('{');
        var firstBracket = value.IndexOf('[');
        var candidates = new[] { firstBrace, firstBracket }.Where(index => index >= 0).ToArray();
        if (candidates.Length == 0)
        {
            return string.Empty;
        }

        var start = candidates.Min();
        var end = Math.Max(value.LastIndexOf('}'), value.LastIndexOf(']'));
        if (end <= start)
        {
            return string.Empty;
        }

        var candidate = value[start..(end + 1)].Trim();
        try
        {
            using var document = JsonDocument.Parse(candidate);
            return candidate;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? MatchValue(Regex regex, string input)
    {
        var match = regex.Match(input);
        return match.Success ? match.Groups[1].Value : null;
    }
}

public sealed record SourceGovernanceOverviewDto(
    int ActiveSources,
    int QuarantinedSources,
    int PendingCandidates,
    int PendingChanges,
    int DeployedChanges,
    int RollbackCount7d,
    DateTime? LatestRunAt,
    int RecentErrorCount24h);

public sealed record SourceRegistryListItemDto(
    long Id,
    string Domain,
    string BaseUrl,
    string Tier,
    string Status,
    decimal? QualityScore,
    decimal? ParseSuccessRate,
    decimal? TimestampCoverage,
    int? FreshnessLagMinutes,
    int ConsecutiveFailures,
    string? LastStatusReason,
    DateTime? LastCheckedAt,
    DateTime UpdatedAt,
    string? TraceId);

public sealed record SourceCandidateListItemDto(
    long Id,
    string Domain,
    string HomepageUrl,
    string ProposedTier,
    string Status,
    string DiscoveryReason,
    string FetchStrategy,
    decimal? VerificationScore,
    decimal? ParseSuccessRate,
    decimal? TimestampCoverage,
    int? FreshnessLagMinutes,
    DateTime DiscoveredAt,
    DateTime? VerifiedAt,
    string? TraceId);

public sealed record CrawlerChangeListItemDto(
    long Id,
    string? TraceId,
    long SourceId,
    string Domain,
    string Status,
    string TriggerReason,
    string? ProposedPatchSummary,
    string? ValidationNote,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? LatestRunResult,
    DateTime? LatestRunAt,
    string? LatestRunNote,
    string? LatestRunTraceId);

public sealed record CrawlerChangeDetailDto(
    long Id,
    string? TraceId,
    string Domain,
    string Status,
    string TriggerReason,
    string? ProposedPatchSummary,
    string? ValidationNote,
    string? ProposedTestCommand,
    string? ProposedReplayCommand,
    IReadOnlyList<string> TargetFiles,
    int PatchCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<CrawlerChangeRunItemDto> Runs);

public sealed record CrawlerChangeRunItemDto(
    long Id,
    string? TraceId,
    string Result,
    string? Note,
    DateTime ExecutedAt);

public sealed record GovernanceErrorSnapshotDto(
    string ErrorType,
    string Domain,
    string Message,
    DateTime OccurredAt,
    decimal? VerificationScore,
    int? HttpStatusCode,
    long? QueueId,
    string? TraceId);

public sealed record TraceSearchResultDto(
    string TraceId,
    IReadOnlyList<string> Lines,
    IReadOnlyList<TraceTimelineItemDto> Timeline);

public sealed record TraceTimelineItemDto(
    string Stage,
    string Domain,
    string Status,
    string? Note,
    DateTime OccurredAt,
    string? TraceId,
    long? QueueId);

public sealed record LlmConversationLogLineDto(
    string Timestamp,
    string Level,
    string? TraceId,
    string? Stage,
    string? Provider,
    string? Model,
    string Raw);

public sealed record LlmConversationLogItemDto(
    string Timestamp,
    string Level,
    string? TraceId,
    string Status,
    string? Provider,
    string? Model,
    string? RequestRaw,
    string? ResponseRaw,
    string? ErrorRaw,
    string? RequestText,
    string? ResponseText,
    string? ErrorText,
    string? LastSeenAt,
    IReadOnlyList<string> Lines,
    string Raw,
    IReadOnlyList<string> Stages);

public sealed record PagedResult<T>(
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<T> Items);