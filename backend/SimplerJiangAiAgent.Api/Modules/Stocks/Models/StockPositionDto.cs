namespace SimplerJiangAiAgent.Api.Modules.Stocks.Models;

public sealed record StockPositionUpsertDto(
    string Symbol,
    int QuantityLots,
    decimal AverageCostPrice,
    string? Notes);
