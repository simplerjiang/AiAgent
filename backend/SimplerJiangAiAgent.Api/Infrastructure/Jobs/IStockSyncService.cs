namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public interface IStockSyncService
{
    Task SyncOnceAsync(CancellationToken cancellationToken = default);

    Task SaveDetailAsync(SimplerJiangAiAgent.Api.Modules.Stocks.Models.StockDetailDto detail, string interval, CancellationToken cancellationToken = default);
}
