using Microsoft.Extensions.Options;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public sealed class SourceGovernanceWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SourceGovernanceWorker> _logger;
    private readonly SourceGovernanceOptions _options;

    public SourceGovernanceWorker(
        IServiceProvider serviceProvider,
        ILogger<SourceGovernanceWorker> logger,
        IOptions<SourceGovernanceOptions> options)
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
                var service = scope.ServiceProvider.GetRequiredService<ISourceGovernanceService>();
                await service.RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "来源治理任务执行失败");
            }

            var delay = TimeSpan.FromSeconds(Math.Max(300, _options.IntervalSeconds));
            await Task.Delay(delay, stoppingToken);
        }
    }
}