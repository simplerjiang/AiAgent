namespace SimplerJiangAiAgent.Api.Services;

public sealed class TradingCalendarService : ITradingCalendarService
{
    private volatile HashSet<DateOnly> _tradingDays = new();
    private readonly IBaostockClientFactory _clientFactory;
    private readonly ILogger<TradingCalendarService> _logger;
    private bool _loaded;

    public bool IsLoaded => _loaded;

    public TradingCalendarService(IBaostockClientFactory clientFactory, ILogger<TradingCalendarService> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public bool IsTradingDay(DateOnly date)
    {
        if (!_loaded)
        {
            // Fallback: weekend-only check
            return date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
        }
        return _tradingDays.Contains(date);
    }

    public DateOnly GetPreviousTradingDay(DateOnly date)
    {
        var d = date.AddDays(-1);
        while (!IsTradingDay(d)) d = d.AddDays(-1);
        return d;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            var now = DateOnly.FromDateTime(DateTime.Now);
            var startDate = new DateOnly(now.Year - 1, 1, 1);
            var endDate = new DateOnly(now.Year + 1, 12, 31);

            var newSet = new HashSet<DateOnly>();
            await using var lease = await _clientFactory.GetClientAsync(ct);
            await foreach (var row in lease.Client.QueryTradeDatesAsync(
                startDate.ToString("yyyy-MM-dd"),
                endDate.ToString("yyyy-MM-dd"), ct))
            {
                if (row.IsTrading)
                    newSet.Add(row.Date);
            }

            if (newSet.Count > 0)
            {
                _tradingDays = newSet;
                _loaded = true;
                _logger.LogInformation("Trading calendar loaded: {Count} trading days ({Start} ~ {End})",
                    newSet.Count, startDate, endDate);
            }
            else
            {
                _logger.LogWarning("Baostock returned 0 trading days, keeping existing data");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load trading calendar from Baostock, using fallback");
        }
    }
}
