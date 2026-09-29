using FloatingClock.Core;

namespace FloatingClock.Tests;

public class SettingsTests
{
    private readonly string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "settings.json");

    [Fact]
    public void Every_field_survives_a_round_trip()
    {
        var settings = new Settings
        {
            Accumulated = new TimeSpan(5, 4, 3),
            StartedAtUtc = new DateTimeOffset(2026, 9, 29, 7, 30, 0, TimeSpan.Zero),
            X = -1200,
            Y = 40,
            FontFamily = "Consolas",
            FontSizePt = 42.5,
            Bold = true,
            Italic = true,
            TextColor = "#112233",
            BackgroundColor = "#445566",
            BackgroundOpacity = 0,
            Format = ClockFormat.HoursMinutes,
            AlwaysOnTop = false,
            ReminderHours = 10,
            ReminderColor = "#778899",
        };

        SettingsStore.Save(path, settings);

        Assert.Equal(settings, SettingsStore.Load(path));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Missing_file_means_defaults()
    {
        Assert.Equal(new Settings(), SettingsStore.Load(path));
    }

    [Fact]
    public void Corrupt_file_means_defaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ \"Accumulated\": ");

        Assert.Equal(new Settings(), SettingsStore.Load(path));
    }

    [Fact]
    public void Invalid_values_mean_defaults()
    {
        SettingsStore.Save(path, new Settings { Accumulated = TimeSpan.FromHours(2), TextColor = "red" });

        Assert.Equal(new Settings(), SettingsStore.Load(path));
    }

    [Theory]
    [InlineData(24, 1, 26.4)]
    [InlineData(24, -1, 21.8)]
    [InlineData(8, 1, 9)]       // 10 % would be 0.8 pt: at least 1 pt
    [InlineData(8.5, -1, 8)]    // clamped at the minimum
    [InlineData(195, 1, 200)]   // clamped at the maximum
    public void Wheel_step_is_ten_percent_at_least_one_point_within_bounds(double size, int notches, double expected)
    {
        Assert.Equal(expected, Settings.StepFontSize(size, notches));
    }
}
