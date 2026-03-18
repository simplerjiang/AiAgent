using SimplerJiangAiAgent.Api.Modules.Stocks.Models;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

internal static class StockRealtimeKLineMerge
{
    public static IReadOnlyList<KLinePointDto> MergeDailyFromMinuteLines(
        IReadOnlyList<KLinePointDto> kLines,
        IReadOnlyList<MinuteLinePointDto> minuteLines,
        int? take = null)
    {
        var baseLines = (kLines ?? Array.Empty<KLinePointDto>())
            .OrderBy(item => item.Date)
            .ToList();

        var orderedMinutes = (minuteLines ?? Array.Empty<MinuteLinePointDto>())
            .Where(item => item.Price > 0m)
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Time)
            .ToList();

        if (orderedMinutes.Count == 0)
        {
            return ApplyTake(baseLines, take);
        }

        var latestMinuteDate = orderedMinutes[^1].Date;
        var latestKLineDate = baseLines.Count == 0
            ? (DateOnly?)null
            : DateOnly.FromDateTime(baseLines[^1].Date);

        if (latestKLineDate.HasValue && latestMinuteDate < latestKLineDate.Value)
        {
            return ApplyTake(baseLines, take);
        }

        var dayMinutes = orderedMinutes
            .Where(item => item.Date == latestMinuteDate)
            .ToList();

        if (dayMinutes.Count == 0)
        {
            return ApplyTake(baseLines, take);
        }

        var sameDayLine = baseLines.FirstOrDefault(item => DateOnly.FromDateTime(item.Date) == latestMinuteDate);
        var open = sameDayLine?.Open > 0m ? sameDayLine.Open : dayMinutes[0].Price;
        var close = dayMinutes[^1].Price;
        var high = new[]
        {
            open,
            close,
            dayMinutes.Max(item => item.Price),
            sameDayLine?.High ?? decimal.MinValue
        }.Max();
        var low = new[]
        {
            open,
            close,
            dayMinutes.Min(item => item.Price),
            sameDayLine?.Low ?? decimal.MaxValue
        }.Min();
        var volume = new[]
        {
            sameDayLine?.Volume ?? 0m,
            dayMinutes[^1].Volume,
            dayMinutes.Max(item => item.Volume)
        }.Max();

        var mergedLine = new KLinePointDto(latestMinuteDate.ToDateTime(TimeOnly.MinValue), open, close, high, low, volume);

        var merged = baseLines
            .Where(item => DateOnly.FromDateTime(item.Date) != latestMinuteDate)
            .Append(mergedLine)
            .OrderBy(item => item.Date)
            .ToList();

        return ApplyTake(merged, take);
    }

    private static IReadOnlyList<KLinePointDto> ApplyTake(IReadOnlyList<KLinePointDto> lines, int? take)
    {
        if (!take.HasValue || take.Value <= 0 || lines.Count <= take.Value)
        {
            return lines.ToArray();
        }

        return lines
            .TakeLast(take.Value)
            .ToArray();
    }
}