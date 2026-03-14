using Microsoft.Extensions.Options;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public sealed class TradingPlanTriggerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TradingPlanTriggerWorker> _logger;
    private readonly TradingPlanTriggerOptions _options;

    public TradingPlanTriggerWorker(
        IServiceProvider serviceProvider,
        ILogger<TradingPlanTriggerWorker> logger,
        IOptions<TradingPlanTriggerOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ITradingPlanTriggerService>();
                await service.EvaluateAsync(DateTimeOffset.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "交易计划触发引擎执行失败");
            }

            var delay = TimeSpan.FromSeconds(Math.Max(15, _options.IntervalSeconds));
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
}