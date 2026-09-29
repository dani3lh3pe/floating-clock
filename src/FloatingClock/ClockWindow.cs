using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FloatingClock.Core;
using Microsoft.VisualBasic;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace FloatingClock;

sealed class ClockWindow : Window
{
    const int ScreenMargin = 16;
    static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FloatingClock", "settings.json");

    readonly TextBlock text = new() { Typography = { NumeralAlignment = FontNumeralAlignment.Tabular } };
    readonly Border border;
    readonly Forms.ContextMenuStrip menu = new();
    readonly Forms.NotifyIcon tray;
    readonly WorkTimer timer;
    Settings settings;
    Brush textBrush = Brushes.White, reminderBrush = Brushes.Red;
    int wheelDelta;

    public ClockWindow()
    {
        settings = SettingsStore.Load(SettingsPath);
        timer = new WorkTimer(TimeProvider.System, settings.Accumulated, settings.StartedAtUtc);

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        Content = border = new Border { Padding = new Thickness(12, 4, 12, 4), CornerRadius = new CornerRadius(6), Child = text };

        tray = new Forms.NotifyIcon
        {
            Icon = new Drawing.Icon(typeof(ClockWindow).Assembly.GetManifestResourceStream("app.ico")!, Forms.SystemInformation.SmallIconSize),
            ContextMenuStrip = menu,
            Visible = true,
        };
        tray.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) ToggleVisible(); };
        BuildMenu();

        MouseLeftButtonDown += OnLeftButtonDown;
        MouseRightButtonUp += (_, _) => menu.Show(Forms.Cursor.Position);
        MouseWheel += OnMouseWheel;
        SourceInitialized += (_, _) => HideFromAltTab();
        Loaded += (_, _) => PlaceOnScreen();
        Closed += (_, _) => tray.Dispose();

        Apply();
        // 200 ms so the seconds flip on time; this constructor starts the timer itself.
        _ = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Normal, (_, _) => Tick(), Dispatcher);
    }

    public void BringToFront()
    {
        Show();
        Activate();
        if (!Topmost)
        {
            Topmost = true; // raises it above normal windows without pinning it there
            Topmost = false;
        }
    }

    nint Handle => new WindowInteropHelper(this).Handle;

    void Tick()
    {
        var elapsed = timer.Elapsed;
        text.Text = WorkTimer.Format(elapsed, settings.Format);
        text.Opacity = timer.IsRunning ? 1 : 0.5;
        text.Foreground = WorkTimer.IsPastReminder(elapsed, settings.ReminderHours) ? reminderBrush : textBrush;

        tray.Text = $"Floating Clock – {text.Text} ({(timer.IsRunning ? "running" : "stopped")})";
        if (timer.ReminderDue(settings.ReminderHours))
            tray.ShowBalloonTip(10_000, "Floating Clock", $"{settings.ReminderHours} h reached.", Forms.ToolTipIcon.Info);
    }

    void Apply()
    {
        text.FontFamily = new FontFamily(settings.FontFamily);
        text.FontSize = settings.FontSizePt * 96 / 72; // WPF sizes are in 1/96 inch
        text.FontWeight = settings.Bold ? FontWeights.Bold : FontWeights.Normal;
        text.FontStyle = settings.Italic ? FontStyles.Italic : FontStyles.Normal;
        var background = ParseColor(settings.BackgroundColor);
        background.A = (byte)Math.Round(settings.BackgroundOpacity * 2.55);
        border.Background = new SolidColorBrush(background);
        textBrush = new SolidColorBrush(ParseColor(settings.TextColor));
        reminderBrush = new SolidColorBrush(ParseColor(settings.ReminderColor));
        Topmost = settings.AlwaysOnTop;
        Tick();
    }

    void Change(Settings next)
    {
        settings = next;
        Save();
        Apply();
    }

    void Save()
    {
        // ponytail: a locked file or full disk skips this save instead of crashing; the next change saves again.
        try
        {
            SettingsStore.Save(SettingsPath, settings with { Accumulated = timer.Accumulated, StartedAtUtc = timer.StartedAtUtc });
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    void ToggleTimer()
    {
        timer.Toggle();
        Save();
        Tick();
    }

    void ResetTimer()
    {
        timer.Reset();
        Save();
        Tick();
    }

    void SetTime()
    {
        var prompt = "Elapsed time (h:mm):";
        var prefill = WorkTimer.Format(timer.Elapsed, ClockFormat.HoursMinutes);
        var input = prefill;
        Activate(); // InputBox takes the active window as owner: modal to the clock and above it
        while (true)
        {
            input = Interaction.InputBox(prompt, "Set time", input);
            // Cancel, empty, or OK on the prefill (which is cut to minutes): unchanged
            if (string.IsNullOrWhiteSpace(input) || input.Trim() == prefill) return;
            if (WorkTimer.ParseHoursMinutes(input) is { } elapsed)
            {
                timer.SetElapsed(elapsed);
                Save();
                Tick();
                return;
            }
            prompt = "Use h:mm, e.g. 2:15";
        }
    }

    void ToggleVisible()
    {
        if (IsVisible) Hide();
        else BringToFront();
    }

    void BuildMenu()
    {
        var startStop = new Forms.ToolStripMenuItem("Start", null, (_, _) => ToggleTimer());
        var alwaysOnTop = new Forms.ToolStripMenuItem("Always on top", null, (_, _) => Change(settings with { AlwaysOnTop = !settings.AlwaysOnTop }));
        menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            startStop,
            new Forms.ToolStripMenuItem("Reset", null, (_, _) => ResetTimer()),
            new Forms.ToolStripMenuItem("Set time…", null, (_, _) => SetTime()),
            alwaysOnTop,
            new Forms.ToolStripSeparator(),
            ColorItem("Text colour…", () => settings.TextColor, c => settings with { TextColor = c }),
            ColorItem("Background colour…", () => settings.BackgroundColor, c => settings with { BackgroundColor = c }),
            Choice("Background opacity", Settings.OpacitySteps, o => $"{o} %", () => settings.BackgroundOpacity, o => settings with { BackgroundOpacity = o }),
            new Forms.ToolStripMenuItem("Font…", null, (_, _) => ChooseFont()),
            Choice("Format", Enum.GetValues<ClockFormat>(), f => f == ClockFormat.HoursMinutes ? "hh:mm" : "hh:mm:ss", () => settings.Format, f => settings with { Format = f }),
            new Forms.ToolStripSeparator(),
            Choice("Reminder", Settings.ReminderSteps, h => h == 0 ? "Off" : $"{h} h", () => settings.ReminderHours, h => settings with { ReminderHours = h }),
            ColorItem("Reminder colour…", () => settings.ReminderColor, c => settings with { ReminderColor = c }),
            new Forms.ToolStripSeparator(),
            new Forms.ToolStripMenuItem("Quit", null, (_, _) => Close()),
        });
        menu.Opening += (_, _) =>
        {
            startStop.Text = timer.IsRunning ? "Stop" : "Start";
            alwaysOnTop.Checked = settings.AlwaysOnTop;
        };
    }

    Forms.ToolStripMenuItem Choice<T>(string title, T[] values, Func<T, string> label, Func<T> current, Func<T, Settings> select)
    {
        var item = new Forms.ToolStripMenuItem(title);
        foreach (var value in values)
            item.DropDownItems.Add(new Forms.ToolStripMenuItem(label(value), null, (_, _) => Change(select(value))) { Tag = value });
        item.DropDownOpening += (_, _) =>
        {
            foreach (Forms.ToolStripMenuItem option in item.DropDownItems) option.Checked = Equals(option.Tag, current());
        };
        return item;
    }

    Forms.ToolStripMenuItem ColorItem(string title, Func<string> current, Func<string, Settings> select) =>
        new(title, null, (_, _) =>
        {
            using var dialog = new Forms.ColorDialog { Color = Drawing.ColorTranslator.FromHtml(current()), FullOpen = true };
            if (dialog.ShowDialog(new Win32Owner(Handle)) != Forms.DialogResult.OK) return;
            // Not ColorTranslator.ToHtml: it returns names like "White" for known colours.
            var c = dialog.Color;
            Change(select($"#{c.R:X2}{c.G:X2}{c.B:X2}"));
        });

    void ChooseFont()
    {
        var style = (settings.Bold ? Drawing.FontStyle.Bold : Drawing.FontStyle.Regular)
            | (settings.Italic ? Drawing.FontStyle.Italic : Drawing.FontStyle.Regular);
        using var current = new Drawing.Font(settings.FontFamily, (float)settings.FontSizePt, style);
        using var dialog = new Forms.FontDialog
        {
            Font = current,
            ShowEffects = false,
            AllowVerticalFonts = false,
            FontMustExist = true,
            MinSize = (int)Settings.MinFontSizePt,
            MaxSize = (int)Settings.MaxFontSizePt,
        };
        if (dialog.ShowDialog(new Win32Owner(Handle)) != Forms.DialogResult.OK) return;
        var font = dialog.Font;
        Change(settings with
        {
            FontFamily = font.FontFamily.Name,
            FontSizePt = Math.Clamp(Math.Round(font.SizeInPoints, 1), Settings.MinFontSizePt, Settings.MaxFontSizePt),
            Bold = font.Bold,
            Italic = font.Italic,
        });
    }

    void OnLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleTimer();
            return;
        }
        if (Mouse.LeftButton != MouseButtonState.Pressed) return; // DragMove throws otherwise (touch, very fast clicks)
        GetWindowRect(Handle, out var before);
        DragMove();
        GetWindowRect(Handle, out var r);
        if (r.Left != before.Left || r.Top != before.Top) Change(settings with { X = r.Left, Y = r.Top });
    }

    void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Precision touchpads send fractions of a notch; collect them into whole notches.
        wheelDelta += e.Delta;
        var notches = wheelDelta / Mouse.MouseWheelDeltaForOneLine;
        wheelDelta %= Mouse.MouseWheelDeltaForOneLine;
        var size = Settings.StepFontSize(settings.FontSizePt, notches);
        if (size != settings.FontSizePt) Change(settings with { FontSizePt = size });
    }

    void HideFromAltTab()
    {
        // ShowInTaskbar = false alone still leaves an Alt+Tab entry; a tool window has neither.
        SetWindowLongPtr(Handle, GWL_EXSTYLE, GetWindowLongPtr(Handle, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
    }

    // Physical pixels throughout: WPF's DIP-based Left/Top are unreliable across monitors with
    // different scaling, and SetWindowPos lets Windows rescale the window for the target monitor.
    void PlaceOnScreen()
    {
        GetWindowRect(Handle, out var r);
        int width = r.Right - r.Left, height = r.Bottom - r.Top;
        var primary = Forms.Screen.PrimaryScreen!.WorkingArea;
        Drawing.Point target;
        if (settings.X is int x && settings.Y is int y)
        {
            // The centre must be on a monitor, so at least a grabbable part of the clock is visible.
            target = Forms.Screen.AllScreens.Any(s => s.Bounds.Contains(x + width / 2, y + height / 2))
                ? new(x, y)
                : new(primary.Left + (primary.Width - width) / 2, primary.Top + (primary.Height - height) / 2);
        }
        else
        {
            target = new(primary.Right - width - ScreenMargin, primary.Top + ScreenMargin);
        }
        SetWindowPos(Handle, 0, target.X, target.Y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    static Color ParseColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    sealed class Win32Owner(nint handle) : Forms.IWin32Window
    {
        public nint Handle => handle;
    }

    const int GWL_EXSTYLE = -20;
    const nint WS_EX_TOOLWINDOW = 0x80;
    const uint SWP_NOSIZE = 0x1, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;

    [StructLayout(LayoutKind.Sequential)]
    struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] static extern nint GetWindowLongPtr(nint hWnd, int index);
    [DllImport("user32.dll")] static extern nint SetWindowLongPtr(nint hWnd, int index, nint value);
    [DllImport("user32.dll")] static extern bool GetWindowRect(nint hWnd, out Rect rect);
    [DllImport("user32.dll")] static extern bool SetWindowPos(nint hWnd, nint insertAfter, int x, int y, int cx, int cy, uint flags);
}
