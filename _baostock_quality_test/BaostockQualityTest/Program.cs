using System.Diagnostics;
using System.Globalization;
using Baostock.NET;
using Baostock.NET.Client;
using Baostock.NET.Models;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== Baostock.NET Data Quality Test ===");
Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine();

await using var client = await BaostockClient.CreateAndLoginAsync();
Console.WriteLine("Connected.\n");

var results = new List<(string api, string status, string details)>();

// Helper
async Task TestApi(string name, Func<Task<string>> action)
{
    Console.Write($"Testing {name,-35}... ");
    var sw = Stopwatch.StartNew();
    try
    {
        var detail = await action();
        sw.Stop();
        Console.WriteLine($"OK ({sw.ElapsedMilliseconds}ms) {detail}");
        results.Add((name, "OK", detail));
    }
    catch (Exception ex)
    {
        sw.Stop();
        Console.WriteLine($"FAIL ({sw.ElapsedMilliseconds}ms) {ex.GetType().Name}: {ex.Message}");
        results.Add((name, "FAIL", $"{ex.GetType().Name}: {ex.Message}"));
    }
}

// === 1. Trade Dates ===
await TestApi("QueryTradeDatesAsync(2024)", async () =>
{
    var dates = new List<TradeDateRow>();
    await foreach (var r in client.QueryTradeDatesAsync("2024")) dates.Add(r);
    var tradingDays = dates.Count(d => d.IsTrading);
    return $"total={dates.Count}, tradingDays={tradingDays}";
});

await TestApi("QueryTradeDatesAsync(2025)", async () =>
{
    var dates = new List<TradeDateRow>();
    await foreach (var r in client.QueryTradeDatesAsync("2025")) dates.Add(r);
    var tradingDays = dates.Count(d => d.IsTrading);
    return $"total={dates.Count}, tradingDays={tradingDays}";
});

// === 2. All Stock ===
await TestApi("QueryAllStockAsync(2024-06-28)", async () =>
{
    int count = 0;
    string? first = null, last = null;
    await foreach (var r in client.QueryAllStockAsync("2024-06-28"))
    {
        count++;
        if (first == null) first = r.Code;
        last = r.Code;
    }
    return $"count={count}, first={first}, last={last}";
});

// === 3. Stock Basic ===
await TestApi("QueryStockBasicAsync(sh.600000)", async () =>
{
    var items = new List<StockBasicRow>();
    await foreach (var r in client.QueryStockBasicAsync("sh.600000")) items.Add(r);
    var item = items.FirstOrDefault();
    return item != null ? $"name={item.CodeName}, status={item.Status}, ipoDate={item.IpoDate}" : "no data";
});

await TestApi("QueryStockBasicAsync(sz.000001)", async () =>
{
    var items = new List<StockBasicRow>();
    await foreach (var r in client.QueryStockBasicAsync("sz.000001")) items.Add(r);
    var item = items.FirstOrDefault();
    return item != null ? $"name={item.CodeName}, status={item.Status}" : "no data";
});

// === 4. Stock Industry ===
await TestApi("QueryStockIndustryAsync(sh.600000)", async () =>
{
    var items = new List<StockIndustryRow>();
    await foreach (var r in client.QueryStockIndustryAsync("sh.600000", "2024")) items.Add(r);
    var item = items.FirstOrDefault();
    return item != null ? $"industry={item.Industry}, industryClassification={item.IndustryClassification}" : "no data";
});

// === 5. K-Line (known bug - catch and report) ===
await TestApi("QueryHistoryKDataPlusAsync(sh.600519)", async () =>
{
    var rows = new List<KLineRow>();
    await foreach (var r in client.QueryHistoryKDataPlusAsync("sh.600519", startDate: "2024-06-01", endDate: "2024-06-30"))
        rows.Add(r);
    var first = rows.FirstOrDefault();
    return first != null ? $"rows={rows.Count}, firstDate={first.Date}, close={first.Close}" : "no data";
});

await TestApi("QueryHistoryKDataPlusAsync(sh.600000)", async () =>
{
    var rows = new List<KLineRow>();
    await foreach (var r in client.QueryHistoryKDataPlusAsync("sh.600000", startDate: "2024-01-01", endDate: "2024-01-31"))
        rows.Add(r);
    return $"rows={rows.Count}";
});

// === 6. Profit Data ===
await TestApi("QueryProfitDataAsync(sh.600000,2023,4)", async () =>
{
    var items = new List<ProfitRow>();
    await foreach (var r in client.QueryProfitDataAsync("sh.600000", 2023, 4)) items.Add(r);
    var item = items.FirstOrDefault();
    return item != null ? $"roeAvg={item.RoeAvg}, netProfit={item.NetProfit}" : "no data";
});

await TestApi("QueryProfitDataAsync(sz.000858,2023,4)", async () =>
{
    var items = new List<ProfitRow>();
    await foreach (var r in client.QueryProfitDataAsync("sz.000858", 2023, 4)) items.Add(r);
    var item = items.FirstOrDefault();
    return item != null ? $"roeAvg={item.RoeAvg}, netProfit={item.NetProfit}" : "no data";
});

// === 7. Operation Data ===
await TestApi("QueryOperationDataAsync(sh.600000,2023,4)", async () =>
{
    var items = new List<OperationRow>();
    await foreach (var r in client.QueryOperationDataAsync("sh.600000", 2023, 4)) items.Add(r);
    var item = items.FirstOrDefault();
    return item != null ? $"fields={typeof(OperationRow).GetProperties().Length}" : "no data";
});

// === 8. Growth Data ===
await TestApi("QueryGrowthDataAsync(sh.600000,2023,4)", async () =>
{
    var items = new List<GrowthRow>();
    await foreach (var r in client.QueryGrowthDataAsync("sh.600000", 2023, 4)) items.Add(r);
    var item = items.FirstOrDefault();
    return item != null ? $"has data" : "no data";
});

// === 9. DuPont Data ===
await TestApi("QueryDupontDataAsync(sh.600000,2023,4)", async () =>
{
    var items = new List<DupontRow>();
    await foreach (var r in client.QueryDupontDataAsync("sh.600000", 2023, 4)) items.Add(r);
    var item = items.FirstOrDefault();
    return item != null ? $"dupontRoe={item.DupontRoe}, dupontAssetStoEquity={item.DupontAssetStoEquity}" : "no data";
});

// === 10. Balance Data ===
await TestApi("QueryBalanceDataAsync(sh.600000,2023,4)", async () =>
{
    var items = new List<BalanceRow>();
    await foreach (var r in client.QueryBalanceDataAsync("sh.600000", 2023, 4)) items.Add(r);
    var item = items.FirstOrDefault();
    return item != null ? $"liabilityToAsset={item.LiabilityToAsset}, assetToEquity={item.AssetToEquity}" : "no data";
});

// === 11. Cash Flow Data ===
await TestApi("QueryCashFlowDataAsync(sh.600000,2023,4)", async () =>
{
    var items = new List<CashFlowRow>();
    await foreach (var r in client.QueryCashFlowDataAsync("sh.600000", 2023, 4)) items.Add(r);
    var item = items.FirstOrDefault();
    return item != null ? $"has data" : "no data";
});

// === 12. Dividend Data ===
await TestApi("QueryDividendDataAsync(sh.600000,2024)", async () =>
{
    var items = new List<DividendRow>();
    await foreach (var r in client.QueryDividendDataAsync("sh.600000", "2024")) items.Add(r);
    return $"records={items.Count}";
});

await TestApi("QueryDividendDataAsync(sh.600519,2024)", async () =>
{
    var items = new List<DividendRow>();
    await foreach (var r in client.QueryDividendDataAsync("sh.600519", "2024")) items.Add(r);
    return $"records={items.Count}";
});

// === 13. Adjust Factor ===
await TestApi("QueryAdjustFactorAsync(sh.600000)", async () =>
{
    int count = 0;
    await foreach (var r in client.QueryAdjustFactorAsync("sh.600000", "2024-01-01", "2024-12-31"))
        count++;
    return $"records={count}";
});

// === 14. Performance Express Report ===
await TestApi("QueryPerformanceExpressReportAsync(sh.600000)", async () =>
{
    int count = 0;
    await foreach (var r in client.QueryPerformanceExpressReportAsync("sh.600000", "2023-01-01", "2024-12-31"))
        count++;
    return $"records={count}";
});

// === 15. Forecast Report ===
await TestApi("QueryForecastReportAsync(sh.600000)", async () =>
{
    int count = 0;
    await foreach (var r in client.QueryForecastReportAsync("sh.600000", "2023-01-01", "2024-12-31"))
        count++;
    return $"records={count}";
});

// === 16. Macro: Deposit Rate (use correct date format) ===
await TestApi("QueryDepositRateDataAsync(correct format)", async () =>
{
    var items = new List<DepositRateRow>();
    await foreach (var r in client.QueryDepositRateDataAsync("2020-01-01", "2024-12-31")) items.Add(r);
    var last = items.LastOrDefault();
    return $"records={items.Count}, last rate info available";
});

// === 17. Macro: Loan Rate ===
await TestApi("QueryLoanRateDataAsync(correct format)", async () =>
{
    var items = new List<LoanRateRow>();
    await foreach (var r in client.QueryLoanRateDataAsync("2020-01-01", "2024-12-31")) items.Add(r);
    return $"records={items.Count}";
});

// === 18. Macro: Reserve Ratio ===
await TestApi("QueryRequiredReserveRatioDataAsync", async () =>
{
    var items = new List<ReserveRatioRow>();
    await foreach (var r in client.QueryRequiredReserveRatioDataAsync("2020-01-01", "2024-12-31")) items.Add(r);
    return $"records={items.Count}";
});

// === 19. Macro: Money Supply Month ===
await TestApi("QueryMoneySupplyDataMonthAsync", async () =>
{
    var items = new List<MoneySupplyMonthRow>();
    await foreach (var r in client.QueryMoneySupplyDataMonthAsync("2020-01-01", "2024-12-31")) items.Add(r);
    var last = items.LastOrDefault();
    return $"records={items.Count}";
});

// === 20. Macro: Money Supply Year ===
await TestApi("QueryMoneySupplyDataYearAsync", async () =>
{
    var items = new List<MoneySupplyYearRow>();
    await foreach (var r in client.QueryMoneySupplyDataYearAsync("2020-01-01", "2024-12-31")) items.Add(r);
    return $"records={items.Count}";
});

// === 21. Index: HS300 ===
await TestApi("QueryHs300StocksAsync(2024)", async () =>
{
    var items = new List<IndexConstituentRow>();
    await foreach (var r in client.QueryHs300StocksAsync("2024")) items.Add(r);
    return $"constituents={items.Count}";
});

// === 22. Index: SZ50 ===
await TestApi("QuerySz50StocksAsync(2024)", async () =>
{
    var items = new List<IndexConstituentRow>();
    await foreach (var r in client.QuerySz50StocksAsync("2024")) items.Add(r);
    return $"constituents={items.Count}";
});

// === 23. Index: ZZ500 ===
await TestApi("QueryZz500StocksAsync(2024)", async () =>
{
    var items = new List<IndexConstituentRow>();
    await foreach (var r in client.QueryZz500StocksAsync("2024")) items.Add(r);
    return $"constituents={items.Count}";
});

// === 24. Special: ST Stocks ===
await TestApi("QueryStStocksAsync(2024-06-28)", async () =>
{
    int count = 0;
    await foreach (var r in client.QueryStStocksAsync("2024-06-28")) count++;
    return $"count={count}";
});

// === 25. Special: *ST Stocks ===
await TestApi("QueryStarStStocksAsync(2024-06-28)", async () =>
{
    int count = 0;
    await foreach (var r in client.QueryStarStStocksAsync("2024-06-28")) count++;
    return $"count={count}";
});

// === 26. Special: Suspended ===
await TestApi("QuerySuspendedStocksAsync(2024-06-28)", async () =>
{
    int count = 0;
    await foreach (var r in client.QuerySuspendedStocksAsync("2024-06-28")) count++;
    return $"count={count}";
});

// === 27. Special: Terminated ===
await TestApi("QueryTerminatedStocksAsync(2024-06-28)", async () =>
{
    int count = 0;
    await foreach (var r in client.QueryTerminatedStocksAsync("2024-06-28")) count++;
    return $"count={count}";
});

// === Summary ===
Console.WriteLine("\n=== DATA QUALITY SUMMARY ===\n");
Console.WriteLine($"Total APIs tested: {results.Count}");
Console.WriteLine($"Passed: {results.Count(r => r.status == "OK")}");
Console.WriteLine($"Failed: {results.Count(r => r.status == "FAIL")}");

Console.WriteLine("\n--- Results Table ---");
Console.WriteLine($"{"API",-40} {"Status",-6} Details");
Console.WriteLine(new string('-', 100));
foreach (var (api, status, details) in results)
{
    Console.WriteLine($"{api,-40} {status,-6} {details}");
}
