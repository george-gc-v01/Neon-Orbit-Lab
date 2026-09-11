# Architecture and implementation decisions

Copyright © 2026 George Culache (george-gc-v01).

## Components

| Component | Responsibility |
|---|---|
| `NeonOrbit.Core` | Distribution validation, random streams, single-threaded state model, orbit geometry, settings and reports |
| `NeonOrbit.App` | WPF UI, native mouse observation/movement, tray, Windows power requests, session events and adaptive timers |
| `NeonOrbit.Tests` | Dependency-free executable assertion suite, deterministic/statistical checks and file/report tests |
| `scripts/create-icon.mjs` | Reproducible original vector N converted to a multi-size ICO without packages |
| `installer/NeonOrbit.iss` | Per-user Inno Setup package, no startup persistence |
| `.github/workflows/windows.yml` | Windows compilation, tests, preview render, portable publish, installer and checksums |

## State ownership

All state mutations run on the WPF dispatcher thread. A monotonic Stopwatch supplies seconds. The pure core takes explicit timestamps and has no timer, input hook or UI dependencies. The adapter schedules a deadline; motion ticks at approximately 30 Hz only while high owns the pointer. Visible resting UI updates at 1 Hz. Hidden low sleeps until a deadline. The event-driven hook is retained in low to honour input priority.

When physical input arrives, wow blocks movement immediately and records only the beginning/end of the overlay. Each input event replaces the recovery sample. A held button prevents recovery; a release hook and a 250 ms release check handle stale async button state. A secondary pointer-ownership check yields if the actual position differs from the last generated target. That check is a fallback, not proof that all physical/input races are eliminated: validate dragging and rapid movement on Windows.

The application never suppresses physical input in its hook. Self-tagged generated input is ignored by the observer. Other injected input is treated conservatively as competing input and pauses the experiment.

## Resolved brief differences

1. The latest consolidated prompt overrides earlier wording that always returned wow to high. Recovery returns to the current underlying high/low phase.
2. Effective DPI gives approximate millimetres; the explicit pixels/mm calibration supports measured size. This corrects the earlier assumption that Windows scaling directly measures physical size.
3. Minimise is not a pause. Low, Stop, lock, suspend, shutdown, held buttons and emergency stop still block motion as appropriate.
4. Timer-free low cannot provide a continuously changing tray countdown. A phase deadline is shown while resting/minimised; the open window shows a countdown.
5. The blue text tint is lightened to #529AFF for readability; the icon gradient retains #287CFF at its blue endpoint.
6. Exact replay means random-stream replay from seed + entropy draw events. Full interactive-session replay would require input timing retention and is not claimed.
7. Hybrid secure injection occurs only on the phase stream. It is not made more frequent by a person's mouse activity.
8. Calibration is monitor-specific. A monitor change resets the calibration and reports it to the user; mixed physical density still requires manual validation.
9. Optional linear variants and virtual hover demonstrations are deferred. Their omission is explicit in the README.

## Failure behaviour

Failure to install the input observer or receive session notifications prevents motion. Native movement failure stops the experiment. A power-request failure produces a visible message instead of claiming Windows is awake. Logging/settings permission failures are surfaced. Exit releases the hook, timer, tray icon, hotkey, notification registration and power handle. A crash terminates OS-owned handles; dispatcher exceptions also invoke cleanup. Lock/suspend requires explicit Start on resume.

The maximum session limit stops motion, while the independently selected Windows Awake toggle remains under user control. Emergency stop releases both. Closing saves a report only if requested and logging was enabled; shutdown does not block on a save prompt.

## Extensibility

Keep native interactions in `Native.cs` and the window adapter. Add new sampling tests before changing distributions. Preserve the random algorithm/version for replay compatibility. Add monitor identifiers to a calibration-only settings map if multi-monitor calibration persistence is implemented; do not add application-content monitoring or stored physical trajectories.
