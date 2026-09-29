namespace FloatingClock.Core;

public enum ClockFormat { HoursMinutesSeconds, HoursMinutes }

public sealed class WorkTimer
{
    private readonly TimeProvider clock;
    private TimeSpan lastReminderCheck;

    public WorkTimer(TimeProvider clock, TimeSpan accumulated = default, DateTimeOffset? startedAtUtc = null)
    {
        this.clock = clock;
        Accumulated = accumulated;
        StartedAtUtc = startedAtUtc;
        // Starting up already past the threshold must not notify again, only colour the digits.
        lastReminderCheck = Elapsed;
    }

    public TimeSpan Accumulated { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public bool IsRunning => StartedAtUtc is not null;

    // ADR 0001: wall-clock based so it counts through sleep, app restarts and reboots. Clamped
    // because a manual clock change can put the start in the future.
    public TimeSpan Elapsed
    {
        get
        {
            var elapsed = StartedAtUtc is { } start ? Accumulated + (clock.GetUtcNow() - start) : Accumulated;
            return elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        }
    }

    public void Toggle()
    {
        if (IsRunning)
        {
            Accumulated = Elapsed;
            StartedAtUtc = null;
        }
        else
        {
            StartedAtUtc = clock.GetUtcNow();
        }
    }

    public void Reset()
    {
        Accumulated = TimeSpan.Zero;
        StartedAtUtc = null;
        lastReminderCheck = TimeSpan.Zero;
    }

    public static bool IsPastReminder(TimeSpan elapsed, int reminderHours) =>
        reminderHours > 0 && elapsed >= TimeSpan.FromHours(reminderHours);

    /// <summary>True exactly once when elapsed crosses the threshold since the previous call.</summary>
    public bool ReminderDue(int reminderHours)
    {
        var elapsed = Elapsed;
        var due = !IsPastReminder(lastReminderCheck, reminderHours) && IsPastReminder(elapsed, reminderHours);
        lastReminderCheck = elapsed;
        return due;
    }

    // Hours never roll over into days: 64 h shows as 64:00:00.
    public static string Format(TimeSpan elapsed, ClockFormat format) => format == ClockFormat.HoursMinutes
        ? $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}"
        : $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
}
