using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public sealed class ResearchZombieCleanupWorker : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ResearchZombieCleanupWorker> _logger;

    public ResearchZombieCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<ResearchZombieCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ScanInterval, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<ResearchZombieCleanupService>();
                await cleanupService.CleanupStaleRunningAsync(
                    ResearchZombieCleanupService.BackgroundStaleThreshold,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResearchZombieCleanupWorker scan failed");
            }
        }
    }
}