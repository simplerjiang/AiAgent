using SimplerJiangAiAgent.Api.Services;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public static class ChinaAStockMarketClock
{
    private static readonly TimeZoneInfo ChinaTimeZone = ResolveChinaTimeZone();
    private static readonly TimeSpan MorningStart = new(9, 30, 0);
    private static readonly TimeSpan MorningEnd = new(11, 30, 0);
    private static readonly TimeSpan AfternoonStart = new(13, 0, 0);
    private static readonly TimeSpan AfternoonEnd = new(15, 0, 0);

    public static bool IsTradingSession(DateTimeOffset utcNow, ITradingCalendarService calendar)
    {
        var local = TimeZoneInfo.ConvertTime(utcNow, ChinaTimeZone);
        if (!calendar.IsTradingDay(DateOnly.FromDateTime(local.DateTime)))
        {
            return false;
        }

        var time = local.TimeOfDay;
        return (time >= MorningStart && time < MorningEnd)
            || (time >= AfternoonStart && time < AfternoonEnd);
    }

    private static TimeZoneInfo ResolveChinaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("China Standard Time", TimeSpan.FromHours(8), "China Standard Time", "China Standard Time");
        }
    }
}