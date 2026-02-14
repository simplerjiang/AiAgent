namespace SimplerJiangAiAgent.Api.Modules.Stocks.Models;

public sealed record StockSearchResultDto(
    string Symbol,
    string Name,
    string Code,
    string Market
);
