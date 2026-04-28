using Microsoft.Extensions.Options;
using SimplerJiangAiAgent.Api.Modules.Market.Models;
using SimplerJiangAiAgent.Api.Modules.Market.Services;
using SimplerJiangAiAgent.Api.Services;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public sealed class SectorRotationWorker : BackgroundService
{
    private static readonly TimeZoneInfo ChinaTimeZone = ResolveChinaTimeZone();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SectorRotationWorker> _logger;
    private readonly SectorRotationOptions _options;
    private readonly ITradingCalendarService _calendar;

    public SectorRotationWorker(
        IServiceProvider serviceProvider,
        ILogger<SectorRotationWorker> logger,
        IOptions<SectorRotationOptions> options,
        ITradingCalendarService calendar)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        _calendar = calendar;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                if (_options.Enabled && ShouldSync(now))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ISectorRotationIngestionService>();
                    await service.SyncAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SectorRotationWorker 执行失败");
            }

            var delay = ResolveDelay(DateTimeOffset.UtcNow);
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private TimeSpan ResolveDelay(DateTimeOffset utcNow)
    {
        if (!_options.Enabled)
        {
            return TimeSpan.FromMinutes(10);
        }

        if (ChinaAStockMarketClock.IsTradingSession(utcNow, _calendar))
        {
            return TimeSpan.FromSeconds(Math.Max(60, _options.IntervalSeconds));
        }

        var localNow = TimeZoneInfo.ConvertTime(utcNow, ChinaTimeZone);
        if (_calendar.IsTradingDay(DateOnly.FromDateTime(localNow.DateTime)) && localNow.TimeOfDay >= new TimeSpan(15, 5, 0) && localNow.TimeOfDay < new TimeSpan(16, 0, 0))
        {
            return TimeSpan.FromMinutes(15);
        }

        return TimeSpan.FromMinutes(30);
    }

    private bool ShouldSync(DateTimeOffset utcNow)
    {
        if (ChinaAStockMarketClock.IsTradingSession(utcNow, _calendar))
        {
            return true;
        }

        var localNow = TimeZoneInfo.ConvertTime(utcNow, ChinaTimeZone);
        return _calendar.IsTradingDay(DateOnly.FromDateTime(localNow.DateTime))
            && localNow.TimeOfDay >= new TimeSpan(15, 5, 0)
            && localNow.TimeOfDay < new TimeSpan(16, 0, 0);
    }

    private static TimeZoneInfo ResolveChinaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("China Standard Time", TimeSpan.FromHours(8), "China Standard Time", "China Standard Time");
        }
    }
}
