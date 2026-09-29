# Elapsed time is computed from a persisted UTC start timestamp

The stopwatch measures real elapsed time: it keeps counting through sleep, hibernate, screen lock,
app exit and reboot until the user stops it. Quitting the app does not stop it. To survive a
reboot, the state is persisted as `accumulated` plus `startedAtUtc` (while running), and the
displayed value is `accumulated + (DateTime.UtcNow - startedAtUtc)`, clamped at zero.

Considered options:
- `Stopwatch` / QueryPerformanceCounter: monotonic and counts through sleep, but resets on reboot
  and lives only as long as the process, so it cannot carry a running timer across restarts.
- QueryUnbiasedInterruptTime: excludes sleep (the opposite of what is wanted) and also resets on
  reboot.
- Auto-pause on sleep/lock: rejected; the user stops breaks deliberately. Kept as a feature idea.

Consequences: a manual clock change or a Windows Time (NTP) correction shifts the displayed
value by the same amount. Time-zone and DST changes do not, because everything is UTC. A timer
left running over a weekend shows the whole weekend; that is intended.
