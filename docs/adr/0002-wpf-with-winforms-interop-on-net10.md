# WPF with WinForms interop on .NET 10, built on Linux

The app is Windows-only and is developed on Linux. It uses WPF for the window (per-pixel
transparency, so anti-aliased digits look clean on a transparent background) and pulls three
pieces from WinForms instead of writing them: `NotifyIcon` for the tray, `ColorDialog` and
`FontDialog` for appearance settings. Target is .NET 10 LTS (support until Nov 2028; .NET 8 ends
2026-11-10). It builds on Linux with `EnableWindowsTargeting` (verified for WPF, including XAML,
single-file and ReadyToRun, on SDK 8; trimming and NativeAOT are unsupported).

Considered options:
- WinForms alone: least code, but a transparent background only works via `TransparencyKey`,
  which leaves colour fringes around anti-aliased text.
- Avalonia (used in entra-pim-manager): runs on Linux for quick checks, but has no native
  colour or font dialog, so both would have to be built.
- Tauri 2: small binary, but cross-compiling from Linux is "possible with caveats", and the
  dialogs would again be custom.

Consequences: the app cannot be run on the Linux dev machine; every visual check happens on
Windows. Releases are self-contained so users need no .NET install, and since WPF/WinForms
cannot be trimmed, the output is large (~150 MB, ~70 MB compressed). Framework-dependent would be
a few hundred KB but excludes everyone who cannot install the .NET Desktop Runtime.
