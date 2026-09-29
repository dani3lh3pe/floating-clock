using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FloatingClock.Core;

/// <summary>Everything that survives a restart: timer state and appearance, in one file.</summary>
public sealed record Settings
{
    public const double MinFontSizePt = 8, MaxFontSizePt = 200;
    public static readonly int[] OpacitySteps = [100, 80, 60, 40, 0];
    public static readonly int[] ReminderSteps = [0, 6, 7, 8, 9, 10];

    public TimeSpan Accumulated { get; init; }
    public DateTimeOffset? StartedAtUtc { get; init; }

    /// <summary>Window top-left in physical pixels; null until the user moves it.</summary>
    public int? X { get; init; }
    public int? Y { get; init; }

    public string FontFamily { get; init; } = "Segoe UI";
    public double FontSizePt { get; init; } = 24;
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public string TextColor { get; init; } = "#FFFFFF";
    public string BackgroundColor { get; init; } = "#1E1E1E";
    public int BackgroundOpacity { get; init; } = 80;
    public ClockFormat Format { get; init; }
    public bool AlwaysOnTop { get; init; } = true;
    public int ReminderHours { get; init; } = 8;
    public string ReminderColor { get; init; } = "#FF4D4D";

    /// <summary>One mouse-wheel step: 10 % per notch, at least 1 pt, within the bounds.</summary>
    public static double StepFontSize(double sizePt, int notches)
    {
        var next = sizePt * Math.Pow(1.1, notches);
        if (Math.Abs(next - sizePt) < 1) next = sizePt + Math.Sign(notches);
        return Math.Clamp(Math.Round(next, 1), MinFontSizePt, MaxFontSizePt);
    }
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Missing, unreadable or invalid file means defaults.</summary>
    public static Settings Load(string path)
    {
        Settings? settings;
        try
        {
            settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), Options);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new Settings();
        }

        // Invalid values count as unreadable, so a broken colour cannot crash every start.
        return settings is not null && IsValid(settings) ? settings : new Settings();
    }

    /// <summary>Temp file, flushed, then renamed, so a crash mid-write cannot lose the timer.</summary>
    public static void Save(string path, Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        using (var stream = File.Create(temp))
        {
            JsonSerializer.Serialize(stream, settings, Options);
            stream.Flush(flushToDisk: true); // power loss right after the rename must not leave an empty file
        }
        File.Move(temp, path, overwrite: true);
    }

    private static bool IsValid(Settings s) =>
        IsColor(s.TextColor) && IsColor(s.BackgroundColor) && IsColor(s.ReminderColor)
        && !string.IsNullOrWhiteSpace(s.FontFamily)
        && s.FontSizePt is >= Settings.MinFontSizePt and <= Settings.MaxFontSizePt
        && s.BackgroundOpacity is >= 0 and <= 100
        && s.ReminderHours >= 0;

    private static bool IsColor(string? value) => value is not null && Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$");
}
