namespace SimplerJiangAiAgent.Api.Services;

public interface ITradingCalendarService
{
    /// <summary>Check if a date is a trading day (uses cached data).</summary>
    bool IsTradingDay(DateOnly date);

    /// <summary>Get the previous trading day before the given date.</summary>
    DateOnly GetPreviousTradingDay(DateOnly date);

    /// <summary>Reload calendar data from Baostock.</summary>
    Task RefreshAsync(CancellationToken ct = default);

    /// <summary>Whether the calendar has been loaded.</summary>
    bool IsLoaded { get; }
}
