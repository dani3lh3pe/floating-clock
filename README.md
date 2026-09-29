# Floating Clock

A minimal stopwatch for Windows that shows how long you have been working. Only the digits are
on screen: no title bar, no border, no taskbar button. It can stay on top of every other window.

## Use

| Action | How |
| --- | --- |
| Start / stop | Double-click the clock |
| Move | Drag the clock |
| Resize | Mouse wheel over the clock |
| Everything else | Right-click the clock or the tray icon |
| Hide / show | Left-click the tray icon |

The menu has Reset, Always on top, text/background/reminder colour, background opacity
(0 % = transparent), font, format (`hh:mm:ss` or `hh:mm`), the reminder (off or 6–10 h,
default 8 h) and Quit.

The time keeps counting through sleep, screen lock, quitting the app and reboots until you stop
it. When it passes the reminder threshold, the digits turn red and one notification appears.
While stopped, the digits are dimmed.

Settings and timer state live in `%LocalAppData%\FloatingClock\settings.json`. Delete that file
to start over. To start the clock with Windows, put a shortcut to the exe in `shell:startup`.

## Install

Download `FloatingClock.exe` and run it. It is a single portable file with .NET included; nothing
to install. Builds are not code-signed, so SmartScreen may warn on first start, and managed
machines with Defender ASR may block a new version for a few hours.

## Build

Requires the .NET 10 SDK. Builds on Windows or Linux.

```sh
dotnet test                                          # logic tests, run on any OS
dotnet publish src/FloatingClock -c Release -o publish   # publish/FloatingClock.exe
```

## License

[MIT](LICENSE)
