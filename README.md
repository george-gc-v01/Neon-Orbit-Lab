# Neon-Orbit-Lab

A futuristic Windows educational lab exploring DPI-aware cursor motion, statistical Neon states, user-input priority and transparent low-resource process behaviour.

**Neon Orbit · Process Lab v2026** is a C#/.NET 10 WPF desktop beta. It combines a bounded statistical experiment, a visible mouse orbit, Windows Awake controls and local reports in a compact neon interface. The repository remains private during development.

## Download and run

Open the repository's **Actions → Windows build and verification → latest successful run → Artifacts**. Download `Neon-Orbit-Windows-Beta`, extract it, and choose either:

- `Neon-Orbit-v0.1.0-beta-Setup.exe`: per-user installer; no administrator account normally required.
- `Neon-Orbit-win-x64-portable.zip`: extract, then run `NeonOrbitProcess.v2026.exe`.

Both packages include the .NET runtime; end users need neither Git nor the .NET SDK. These initial builds are unsigned. Windows may show an unknown-publisher warning; verify the private repository source and accompanying SHA-256 checksums. No public release has been created.

The app starts a new experiment and minimises after three seconds by default. Find the gradient **N** in the notification area. Physical mouse input pauses automatic movement. **Ctrl+Alt+F12** stops both movement and the power request. **Stop** stops movement only. **X** or **Close App** exits completely and offers an optional local report.

For a visual preview without pointer or power requests:

```powershell
.\NeonOrbitProcess.v2026.exe --preview
```

## Statistical Neon states

| State | Untruncated normal mean / sigma | Hard bounds | Behaviour |
|---|---|---|---|
| Neon high | 25 / 5 minutes | 15–50 minutes | Circular motion enabled, subject to input priority |
| Neon low | 7.5 / 2 minutes | 3–15 minutes | Generated pointer movement completely stopped |
| Neon wow recovery | 8 / 4 seconds | 1–25 seconds | Physical input owns the pointer; resumption waits for recovery |

Durations use Box–Muller normal sampling with rejection outside the bounds. Values are not clamped to the endpoints. Mean and sigma describe the underlying normal before truncation; the truncated distribution's observed mean and standard deviation differ. High is usually around 20–30 minutes and low around 5–10 minutes, but tails are allowed. A hard upper bound does not imply that durations close to that bound occur frequently.

High/low scheduling continues during Neon wow. When wow ends, the app exposes the **current underlying phase**, including low if low is in progress. Starting a new launch or explicitly restarting a stopped experiment samples a new high period. The default maximum motion session is four hours and is configurable.

**Accelerated Test Mode** changes scheduled minutes into seconds; the seven-second orbit and wow recovery remain unchanged. Test Mode is conspicuously labelled and is never persisted to normal settings.

## Randomness and reproducibility

- **Secure:** operating-system cryptographic randomness supplies the sampler.
- **Seeded:** a versioned SplitMix64 stream gives repeatable sampling from an explicit seed.
- **Hybrid:** the seeded phase stream has a 1% opportunity for fresh entropy per uniform draw (normally two draws per sampled normal). Each injection is logged. This is a developer experiment; it is not an attempt to disguise automation.
- Phase and input streams are separate. Hybrid entropy is limited to phase sampling, so moving the mouse more frequently does not cause more secure injections.
- **Sampler replay metadata** records the phase-stream draw index and seed of each injection. The test suite verifies replay. This reproduces random streams, not a person's mouse path or a complete interactive session. There is no session-replay UI in this beta.

## Motion and physical size

The default orbit is approximately 20 mm across with a seven-second revolution. The motion adapter uses monitor work-area bounds, negative desktop coordinates and Per-Monitor V2 awareness. It eases into a safe circle near corners and stops immediately when the user takes control; it never snaps the cursor back after an interruption.

Windows **effective DPI is a scaling measurement, not measured monitor pixel density**. Therefore the default physical diameter is an estimate. Lab settings provide a 100-physical-pixel reference line and a pixels/mm calibration field. Measure the line with a ruler and enter `100 / measured_mm`. Calibration applies to the current monitor and resets if the motion engine detects a monitor change. Use individual physical-monitor testing before relying on the stated diameter.

## Windows Awake and controls

`Keep Windows awake` defaults to **On**; `Keep display awake` defaults to **Off**. The power request is independent of the motion schedule and survives minimise/high/low/wow. Stopping motion alone leaves a requested awake state in effect; the UI says so. Exit and emergency stop release the request. Lock/suspend interrupts the experiment and releases the request; resumption requires Start. Windows policies, manual sleep and shutdown remain authoritative.

## Privacy and resource use

Core operation is offline and uses one stable process. No clicks, keystrokes, wheel events, app switching, screenshots, window titles, clipboard data or personal cursor trajectories are generated/collected. The local input hook uses position transiently to yield control; logs never retain those coordinates. Other utilities' injected movement also pauses Neon Orbit. No Teams/presence outcome is promised.

Logs are enabled by default in `%LOCALAPPDATA%\NeonOrbit\logs`. They contain phase times, samples, configuration, aggregate movement totals and optional entropy events. Turning logging off clears in-memory events and stops new writes; previously written files remain available for inspection/deletion. **Delete session log** deletes only the current session after confirmation. Uninstall preserves user settings and logs.

HTML and CSV reports include sampled-duration statistics and histograms, state transitions and actual visible-state totals. Wow is analysed as an overlay without double-counting visible time. Logs are bounded to 20,000 events per session; errors disable logging with a visible message. Reports are user-selected local files.

The input observer stays installed during low so physical input can activate wow; it is event-driven. While low and minimised, there is no periodic motion or rendering loop: only the next transition deadline is scheduled. A held button uses a 250 ms release fail-safe. Visible countdowns update once per second. The tray tooltip shows a deadline in resting states to avoid repeated hidden-window updates. CPU and memory targets must be measured on real Windows hardware; no unmeasured performance claim is made.

## Build from source

Developers need a supported Windows installation, .NET 10 SDK, and Node.js only to regenerate the original icon. The icon is already committed. Git is useful for cloning; Inno Setup 6 is needed only for installer creation.

```powershell
git clone https://github.com/george-gc-v01/Neon-Orbit-Lab.git
cd Neon-Orbit-Lab
dotnet build NeonOrbit.slnx -c Release
dotnet run --project tests/NeonOrbit.Tests -c Release
dotnet run --project src/NeonOrbit.App -c Release -- --preview
dotnet publish src/NeonOrbit.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o artifacts/portable
```

To test actual pointer behaviour, launch without `--preview`. The dependency-free console test runner returns nonzero on failure and prints named assertions; it is intentionally run with `dotnet run`, not `dotnet test`.

## Documentation

- [Architecture and resolved specification details](docs/ARCHITECTURE.md)
- [Windows acceptance and profiling checklist](docs/WINDOWS_TESTING.md)
- [Original four brief documents](docs/brief/)
- [Security and privacy reporting](SECURITY.md)
- [Third-party notices](THIRD_PARTY_NOTICES.md)
- [Brand policy](BRANDING.md)

The combination of a statistical state model, calibrated physical orbit, reproducible experiments, input priority, local reporting and restrained visual design is the intended distinction from Mouse Jiggler, Move Mouse, PowerToys Awake and Caffeine. This beta is not a verified feature-for-feature replacement or universally superior product. Optional non-circular motion and virtual control-hover demonstrations are deferred; the first beta focuses on a reliable circular experiment.

## Licence and contribution

Copyright © 2026 George Culache (george-gc-v01).

Software is licensed under **GNU General Public License v3.0 only**, without warranty. See [LICENSE](LICENSE). The Neon Orbit name and logo are not licensed for use in derivative products. This statement reserves product branding; it does not remove GPL permissions to modify and redistribute covered software. Derivative products should use their own branding. See [BRANDING.md](BRANDING.md).

Suggestions or feedback would be greatly appreciated.
