# CLAUDE.md

Floating Clock: a minimal, chromeless, always-on-top stopwatch for Windows that shows how long
I have been working.

## Stack and commands

.NET 10 (on the Linux dev machine the SDK is in `~/.dotnet`, not on `PATH`) · WPF plus WinForms `NotifyIcon`/`ColorDialog`/
`FontDialog` ([ADR 0002](docs/adr/0002-wpf-with-winforms-interop-on-net10.md)) · xUnit.

```sh
dotnet test                                                # Core tests, run on Linux
dotnet publish src/FloatingClock -c Release -o publish     # one self-contained win-x64 exe
git tag vX.Y.Z && git push origin vX.Y.Z                   # CI tests, builds, publishes the release
```

The release version comes from the tag (`.github/workflows/release.yml`); the `<Version>` in the
csproj is only the fallback for local builds.

## Architecture

```text
src/FloatingClock.Core  →  timer, formatting, reminder, settings file   (net10.0, no UI deps)
src/FloatingClock       →  the WPF window, tray, menu, dialogs           (net10.0-windows)
tests/FloatingClock.Tests → xUnit, tests Core only
```

`Core` must not reference WPF or WinForms: that is what keeps the tests runnable on Linux. The app
cannot run on Linux, so anything visual is checked by hand on Windows. Elapsed time is wall-clock
based and persisted ([ADR 0001](docs/adr/0001-elapsed-time-from-persisted-utc-start.md)); do not
switch it to `Stopwatch`.

## Workflow

- New feature: `grilling` → `spec` → implement → spec-check → `/ponytail-review` → `/code-review`
  → commit. Bug fixes and small, fully specified changes skip grilling and spec.
- Decisions that are hard to reverse, surprising, and a real trade-off go to `docs/adr/`
  (rules and format: the ADR section of the `grilling` skill). Deferred feature ideas go to
  `docs/backlog.md`.
- All files (code, comments, ADRs, specs) are English.
