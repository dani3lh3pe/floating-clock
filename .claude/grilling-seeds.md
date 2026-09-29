# Grilling seed questions

Read by the `grilling` skill before round 1. A Windows desktop overlay has a handful of areas
where a missed question costs a rework round later. Use them to grow the design tree when the
change touches the area; they are prompts, not a questionnaire to read out.

- **What time means**: does elapsed time keep counting through sleep, hibernate, screen lock,
  app restart, reboot? What happens on a clock or time-zone change? Which clock source advances
  during sleep is a fact to verify, not assume.
- **Destructive actions**: reset throws away the measured time. Confirmation, undo, or neither?
- **Always on top**: over fullscreen apps, presentations, the taskbar? Toggleable?
- **No window chrome**: how is it moved, resized, closed, quit? Taskbar button, tray icon,
  Alt+Tab entry: which of them, if any?
- **Controls**: click, double-click, context menu, global hotkeys? What stops an accidental click?
- **Appearance**: which colours, fonts, formats (hh:mm:ss or hh:mm), sizes, opacity? Is
  click-through wanted? Settings UI or config file?
- **Monitors and DPI**: is the position remembered? What happens when that monitor is
  disconnected and the window would open off-screen? Per-monitor DPI scaling?
- **Stack and build**: development happens on Linux, the app runs on Windows. Which stack builds
  there, and how does a build reach the Windows machine for testing?
- **Where it runs**: a managed work machine with application control? Unsigned binaries were
  blocked there by Defender ASR (field-confirmed 2026-08-14 in entra-pim-manager). Signing,
  portable exe, or installer?
- **Explicit no's**: session history, daily totals, several timers? Naming what is out is as
  valuable as naming what is in.
