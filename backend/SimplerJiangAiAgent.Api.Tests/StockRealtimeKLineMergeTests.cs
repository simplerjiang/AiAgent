using SimplerJiangAiAgent.Api.Modules.Stocks.Models;
using SimplerJiangAiAgent.Api.Modules.Stocks.Services;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class StockRealtimeKLineMergeTests
{
    [Fact]
    public void MergeDailyFromMinuteLines_AppendsMissingTradingDayBar()
    {
        var kLines = new[]
        {
            new KLinePointDto(new DateTime(2026, 3, 17), 10.1m, 10.2m, 10.3m, 10.0m, 1000m)
        };
        var minuteLines = new[]
        {
            new MinuteLinePointDto(new DateOnly(2026, 3, 18), new TimeSpan(9, 30, 0), 10.4m, 10.4m, 100m),
            new MinuteLinePointDto(new DateOnly(2026, 3, 18), new TimeSpan(9, 31, 0), 10.6m, 10.5m, 300m),
            new MinuteLinePointDto(new DateOnly(2026, 3, 18), new TimeSpan(9, 32, 0), 10.3m, 10.43m, 450m)
        };

        var result = StockRealtimeKLineMerge.MergeDailyFromMinuteLines(kLines, minuteLines, 2);

        Assert.Equal(2, result.Count);
        Assert.Collection(
            result,
            item => Assert.Equal(new DateTime(2026, 3, 17), item.Date),
            item =>
            {
                Assert.Equal(new DateTime(2026, 3, 18), item.Date);
                Assert.Equal(10.4m, item.Open);
                Assert.Equal(10.3m, item.Close);
                Assert.Equal(10.6m, item.High);
                Assert.Equal(10.3m, item.Low);
                Assert.Equal(450m, item.Volume);
            });
    }

    [Fact]
    public void MergeDailyFromMinuteLines_ReplacesSameDayBarWithLatestMinuteClose()
    {
        var kLines = new[]
        {
            new KLinePointDto(new DateTime(2026, 3, 18), 10.2m, 10.25m, 10.28m, 10.18m, 200m)
        };
        var minuteLines = new[]
        {
            new MinuteLinePointDto(new DateOnly(2026, 3, 18), new TimeSpan(9, 30, 0), 10.2m, 10.2m, 100m),
            new MinuteLinePointDto(new DateOnly(2026, 3, 18), new TimeSpan(9, 31, 0), 10.35m, 10.28m, 220m),
            new MinuteLinePointDto(new DateOnly(2026, 3, 18), new TimeSpan(9, 32, 0), 10.3m, 10.29m, 260m)
        };

        var result = StockRealtimeKLineMerge.MergeDailyFromMinuteLines(kLines, minuteLines, 1);

        var item = Assert.Single(result);
        Assert.Equal(new DateTime(2026, 3, 18), item.Date);
        Assert.Equal(10.2m, item.Open);
        Assert.Equal(10.3m, item.Close);
        Assert.Equal(10.35m, item.High);
        Assert.Equal(10.18m, item.Low);
        Assert.Equal(260m, item.Volume);
    }
}