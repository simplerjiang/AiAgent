using Microsoft.Extensions.Options;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public sealed class TradingPlanReviewWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TradingPlanReviewWorker> _logger;
    private readonly TradingPlanReviewOptions _options;

    public TradingPlanReviewWorker(
        IServiceProvider serviceProvider,
        ILogger<TradingPlanReviewWorker> logger,
        IOptions<TradingPlanReviewOptions> options)
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
                var reviewService = scope.ServiceProvider.GetRequiredService<ITradingPlanReviewService>();
                await reviewService.EvaluateAsync(DateTimeOffset.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "交易计划语义复核执行失败");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(15, _options.IntervalSeconds)), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}