using FloatingClock.Core;

namespace FloatingClock.Tests;

public class WorkTimerTests
{
    private sealed class FakeClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 29, 8, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance(TimeSpan by) => Now += by;
    }

    private readonly FakeClock clock = new();

    [Fact]
    public void Elapsed_adds_up_across_stop_start_cycles()
    {
        var timer = new WorkTimer(clock);
        timer.Toggle();
        clock.Advance(TimeSpan.FromMinutes(10));
        timer.Toggle();
        clock.Advance(TimeSpan.FromHours(1)); // stopped: must not count
        timer.Toggle();
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.True(timer.IsRunning);
        Assert.Equal(TimeSpan.FromMinutes(15), timer.Elapsed);
    }

    [Fact]
    public void Elapsed_is_clamped_at_zero_when_the_clock_was_set_back()
    {
        var timer = new WorkTimer(clock, startedAtUtc: clock.Now.AddHours(1));

        Assert.Equal(TimeSpan.Zero, timer.Elapsed);
    }

    [Fact]
    public void Running_timer_survives_a_save_load_round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "settings.json");
        var timer = new WorkTimer(clock, TimeSpan.FromMinutes(30));
        timer.Toggle();
        SettingsStore.Save(path, new Settings { Accumulated = timer.Accumulated, StartedAtUtc = timer.StartedAtUtc });

        clock.Advance(TimeSpan.FromHours(2)); // app closed, machine asleep or rebooted
        var loaded = SettingsStore.Load(path);
        var restored = new WorkTimer(clock, loaded.Accumulated, loaded.StartedAtUtc);

        Assert.True(restored.IsRunning);
        Assert.Equal(TimeSpan.FromMinutes(150), restored.Elapsed);
    }

    [Fact]
    public void Reset_while_running_ends_stopped_at_zero()
    {
        var timer = new WorkTimer(clock, TimeSpan.FromHours(3));
        timer.Toggle();
        clock.Advance(TimeSpan.FromMinutes(1));

        timer.Reset();
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.False(timer.IsRunning);
        Assert.Equal(TimeSpan.Zero, timer.Elapsed);
    }

    [Fact]
    public void Reminder_colour_applies_from_the_threshold_on_and_never_when_off()
    {
        Assert.False(WorkTimer.IsPastReminder(new TimeSpan(7, 59, 59), 8));
        Assert.True(WorkTimer.IsPastReminder(TimeSpan.FromHours(8), 8));
        Assert.False(WorkTimer.IsPastReminder(TimeSpan.FromHours(20), 0));
    }

    [Fact]
    public void Reminder_is_due_exactly_once_when_crossing()
    {
        var timer = new WorkTimer(clock, new TimeSpan(7, 59, 59));
        timer.Toggle();

        Assert.False(timer.ReminderDue(8));
        clock.Advance(TimeSpan.FromSeconds(2));
        Assert.True(timer.ReminderDue(8));
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(timer.ReminderDue(8));
    }

    [Fact]
    public void Reminder_is_not_due_when_loaded_already_past_it()
    {
        var timer = new WorkTimer(clock, TimeSpan.FromHours(9));
        timer.Toggle();
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.False(timer.ReminderDue(8));
    }

    [Fact]
    public void Reminder_is_rearmed_after_reset()
    {
        var timer = new WorkTimer(clock, TimeSpan.FromHours(9));
        timer.Reset();
        timer.Toggle();
        clock.Advance(TimeSpan.FromHours(8));

        Assert.True(timer.ReminderDue(8));
    }

    [Fact]
    public void Reminder_is_never_due_when_off()
    {
        var timer = new WorkTimer(clock);
        timer.Toggle();
        clock.Advance(TimeSpan.FromHours(12));

        Assert.False(timer.ReminderDue(0));
    }

    [Theory]
    [InlineData(0, 0, 0, ClockFormat.HoursMinutesSeconds, "00:00:00")]
    [InlineData(3, 12, 5, ClockFormat.HoursMinutesSeconds, "03:12:05")]
    [InlineData(3, 12, 59, ClockFormat.HoursMinutes, "03:12")]
    [InlineData(64, 3, 12, ClockFormat.HoursMinutesSeconds, "64:03:12")]
    [InlineData(123, 4, 5, ClockFormat.HoursMinutesSeconds, "123:04:05")]
    [InlineData(123, 4, 5, ClockFormat.HoursMinutes, "123:04")]
    public void Format_never_rolls_hours_into_days(int h, int m, int s, ClockFormat format, string expected)
    {
        Assert.Equal(expected, WorkTimer.Format(new TimeSpan(h, m, s), format));
    }
}
