# Windows acceptance and resource profiling

Copyright © 2026 George Culache (george-gc-v01).

CI and manual tests are separate evidence. A successful compile or static WPF render does not establish physical-input, multi-monitor, installer or power-policy behaviour on the user's desktop.

## Automated checks

Run `dotnet run --project tests/NeonOrbit.Tests -c Release`. The runner writes `artifacts/core-tests.txt`, returns nonzero on failure and covers sampler limits/moments/tails, seeded/hybrid reproducibility, state deadlines, input priority, held-button recovery, low overlap, session limits, Test Mode isolation, geometry, calibration, settings and privacy/report handling.

Run the app with `--smoke-test` to render its own WPF window to `artifacts/neon-orbit-preview.png` and exit. This mode makes no pointer or power requests. This render is generated exclusively from the application's own visual tree, not a screenshot of the desktop.

## Home-PC acceptance

| Test | Procedure | Pass condition |
|---|---|---|
| Install / uninstall | Install per user; launch; uninstall after exit | No SDK required, correct name/icon, no remaining process or startup item; user logs retained |
| Startup | Launch normal mode | Fresh high; visible N tray; minimises after ~3 seconds |
| Override | Move mouse and drag with each mouse button | Automatic motion yields; drag uninterrupted; Wow shown |
| Recovery | Stop input, inspect recovery | Delay is 1–25 seconds; low is respected if scheduled |
| Mixed monitors | Test 100%, 125%, 150%, 200%, negative monitor coordinates | Orbit stays inside current work area; reanchors after input |
| Physical diameter | Measure calibration line, enter pixels/mm, measure orbit | Approximately 20 mm after calibration |
| Edges | Start near all four edges/corners | Smooth safe entry; no off-screen movement |
| Test Mode | Enable and Apply | High 15–50 s, low 3–15 s, wow 1–25 s; clear Test label |
| Normal isolation | Exit Test Mode and relaunch | Normal minute-based defaults restored |
| Emergency | Press Ctrl+Alt+F12 | Motion and power request stop; user cursor remains where left |
| Close paths | X, Close App, tray Exit | Clean exit, optional summary, no leftover process |
| Windows Awake | Toggle on/off with `powercfg /requests` in a diagnostic terminal where permitted | Request appears/disappears; display request independent; manual sleep still works |
| Lock/sleep | Lock/unlock, sleep/resume | No motion while unavailable; explicit Start after resumption |
| Privacy | Disable logging; inspect file size after more activity | No further log writes; no save-summary prompt |
| Reports | Export after multiple complete cycles and Wow exceptions | Non-overlapping actual state totals; separate samples and exceptions |
| Accessibility | Keyboard navigation, high contrast, reduced animation | All controls reachable; visible focus; readable status without relying on colour |

## Resource measurements

Use Task Manager Details or Process Explorer, observe `NeonOrbitProcess.v2026.exe` only. Record OS/build, CPU, monitor scaling and power plan. Measure each state for 60 seconds, after a 10-second warm-up: high visible, high minimised, wow, low visible, low minimised, stopped. Record average CPU, working set, private bytes, handles and threads. Compare before/after 20 Test Mode cycles to detect growth.

Use Windows Performance Recorder when timer/wake-up evidence is needed. Confirm hidden low has no periodic motion/render tick. Physical input may still deliver event notifications. Do not claim a fixed MB or CPU target from CI alone.

## Current limitations

Physical Windows acceptance and hardware profiling require a local interactive machine. The beta is unsigned. A static tray N is deliberately used to avoid animation wake-ups. Log deletion is explicit and permanent for the current session. Other saved reports and prior sessions are preserved. Optional virtual hovering and non-circular variations are not part of this first beta.
