using SimplerJiangAiAgent.Api.Services;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public sealed class TradingCalendarWorker : BackgroundService
{
    private readonly ITradingCalendarService _calendarService;
    private readonly ILogger<TradingCalendarWorker> _logger;

    public TradingCalendarWorker(ITradingCalendarService calendarService, ILogger<TradingCalendarWorker> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _calendarService.RefreshAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1).AddHours(2);
            var delay = nextMonth - now;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            await _calendarService.RefreshAsync(stoppingToken);
        }
    }
}
