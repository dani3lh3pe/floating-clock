using System;
using System.Threading;
using System.Windows;
using System.Windows.Forms.Integration;
using Forms = System.Windows.Forms;

namespace FloatingClock;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Single instance: a second start brings the running clock to the front and exits.
        using var showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\FloatingClock.Show", out var isFirst);
        if (!isFirst)
        {
            showSignal.Set();
            return;
        }

        Forms.Application.EnableVisualStyles();
        // Routes messages through WinForms filters so the WinForms menu handles Esc, arrows and Enter.
        WindowsFormsHost.EnableWindowsFormsInterop();
        var app = new Application();
        var window = new ClockWindow();
        ThreadPool.RegisterWaitForSingleObject(showSignal,
            (_, _) => window.Dispatcher.BeginInvoke(window.BringToFront), null, Timeout.Infinite, executeOnlyOnce: false);
        app.Run(window);
    }
}
