namespace TaskFlow.Api.Tests.TestInfrastructure;

/// <summary>
/// A clock that only moves when a test moves it. Replaces TimeProvider.System in
/// TaskFlowApiFactory so "today" (and therefore the due-date rules) is predictable.
/// Its local time zone defaults to UTC (local and UTC dates are the same day);
/// set <see cref="LocalZone"/> to test a machine whose local date differs.
/// </summary>
public class FixedTimeProvider : TimeProvider
{
    /// <summary>The current instant; set it to move the clock.</summary>
    public DateTimeOffset UtcNow { get; set; } = new(2026, 6, 15, 9, 30, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => UtcNow;

    /// <summary>
    /// The zone LocalTimeZone reports. A separate property because C# can't add a
    /// setter when overriding the get-only TimeProvider.LocalTimeZone.
    /// </summary>
    public TimeZoneInfo LocalZone { get; set; } = TimeZoneInfo.Utc;

    public override TimeZoneInfo LocalTimeZone => LocalZone;

    /// <summary>Today's date as the API sees it, for building due dates in tests.</summary>
    public DateOnly Today => DateOnly.FromDateTime(GetLocalNow().DateTime);
}
