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

    [Fact]
    public void Set_elapsed_while_running_counts_on_from_the_new_value()
    {
        var timer = new WorkTimer(clock);
        timer.Toggle();
        clock.Advance(new TimeSpan(0, 10, 37));

        timer.SetElapsed(TimeSpan.FromHours(2));
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.True(timer.IsRunning);
        Assert.Equal(new TimeSpan(2, 1, 0), timer.Elapsed);
    }

    [Fact]
    public void Set_elapsed_while_stopped_stays_stopped()
    {
        var timer = new WorkTimer(clock, TimeSpan.FromMinutes(10));

        timer.SetElapsed(new TimeSpan(1, 30, 0));
        clock.Advance(TimeSpan.FromHours(1));

        Assert.False(timer.IsRunning);
        Assert.Equal(new TimeSpan(1, 30, 0), timer.Elapsed);
    }

    [Fact]
    public void Setting_past_the_reminder_does_not_notify_but_setting_below_rearms_it()
    {
        var timer = new WorkTimer(clock, TimeSpan.FromHours(7));
        timer.Toggle();

        timer.SetElapsed(TimeSpan.FromHours(9));
        Assert.False(timer.ReminderDue(8));

        timer.SetElapsed(new TimeSpan(7, 59, 0));
        Assert.False(timer.ReminderDue(8));
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.True(timer.ReminderDue(8));
    }

    [Theory]
    [InlineData("2:00", 2, 0)]
    [InlineData("10:30", 10, 30)]
    [InlineData("0:00", 0, 0)]
    [InlineData("02:00", 2, 0)]
    [InlineData("123:05", 123, 5)]
    [InlineData(" 2:00 ", 2, 0)]
    public void Parse_accepts_h_mm(string input, int hours, int minutes)
    {
        Assert.Equal(new TimeSpan(hours, minutes, 0), WorkTimer.ParseHoursMinutes(input));
    }

    [Fact]
    public void Prefill_from_the_hh_mm_format_parses_back()
    {
        var elapsed = new TimeSpan(123, 5, 0);

        Assert.Equal(elapsed, WorkTimer.ParseHoursMinutes(WorkTimer.Format(elapsed, ClockFormat.HoursMinutes)));
    }

    [Theory]
    [InlineData("2h")]
    [InlineData("1:75")]
    [InlineData("2:5")]
    [InlineData("2:005")]
    [InlineData("-1:00")]
    [InlineData(":30")]
    [InlineData("2")]
    [InlineData("2:00:00")]
    [InlineData("")]
    [InlineData("9999999:00")]
    public void Parse_rejects_anything_else(string input)
    {
        Assert.Null(WorkTimer.ParseHoursMinutes(input));
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
