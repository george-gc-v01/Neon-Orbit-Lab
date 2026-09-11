# Neon Orbit

## Agreed Decisions, Comments and Recommendations

**Application:** Neon Orbit  
**Window subtitle:** Process Lab v2026  
**Executable:** `NeonOrbitProcess.v2026.exe`  
**GitHub repository:** `Neon-Orbit-Lab`  
**Development model:** Astra with High reasoning

Copyright © 2026 George Culache (`george-gc-v01`).

## 1. Purpose and identity

- Neon Orbit is a transparent Windows educational and experimental application.
- It demonstrates DPI-aware cursor motion, statistical phase scheduling, physical-input priority, low-resource application states and privacy-preserving local experiment logging.
- The application and its process remain clearly identifiable.
- Low-key operation means lightweight, efficient and visually unobtrusive—not hidden, disguised or misleading.
- The application should be approachable, useful and enjoyable for a broad range of users.

## 2. Visual identity

- Futuristic, minimal and science-fiction inspired.
- Dark charcoal background with bright neon green as the primary colour and bright, slightly darker neon blue as the secondary colour.
- Suggested colours: neon green `#66FF99` and neon blue `#287CFF`.
- Subtle pulse animations rather than excessive constant animation.
- Minimal circuit traces around framing important controls.
- Small planets orbiting a sun in the main window.
- The tray icon is a single capital `N`, transitioning from neon green to neon blue along its legs and diagonal.
- Advanced settings remain inside a collapsed `LAB SETTINGS` panel.
- Reduced-motion preferences and accessibility requirements are respected.

## 3. Main controls

- Start, Stop, Minimise and Close App controls.
- The standard top-right Windows `X` closes the application completely.
- Minimising is performed deliberately through the Minimise control or tray menu.
- The tray menu displays the current state and countdown and provides Start, Stop, Open, Emergency Stop and Exit actions.

## 4. Neon states

### Neon high

- Displayed in bright neon green as `Neon high MM:SS`, without the word `remaining`.
- Typical scheduled duration: 20–30 minutes.
- Absolute range: 15–50 minutes.
- Truncated normal distribution: mean 25 minutes, standard deviation 5 minutes.
- Cursor follows an approximately 20 mm circular path.
- One full revolution takes approximately seven seconds in Normal Mode.

### Neon low

- Displayed in bright, slightly darker neon blue as `Neon low MM:SS`, without the word `remaining`.
- Typical scheduled duration: 5–10 minutes.
- Absolute range: 3–15 minutes.
- Truncated normal distribution: mean 7.5 minutes, standard deviation 2 minutes.
- Generated pointer movement stops completely.
- Motion-specific observers and repeated polling are suspended where technically possible.
- Only the transition timer, tray state and enabled logging remain active.

### Neon wow

- Triggered immediately by genuine physical mouse movement.
- Displayed in the same neon green as Neon high.
- Remains active while genuine movement continues.
- After the last movement, a recovery delay is sampled from a truncated normal distribution with mean 8 seconds, standard deviation 4 seconds and limits of 1–25 seconds.
- Further physical movement cancels the current recovery and starts a newly sampled delay afterward.
- The underlying Neon high/low wall-clock schedule continues during Neon wow.
- Neon wow is logged separately as a user-generated exception.

## 5. Randomness

- Normal scheduling uses a properly sampled truncated normal distribution rather than hard clamping.
- Developer Mode may use a known base seed for reproducibility.
- Hybrid Experiment Mode may occasionally inject fresh secure randomness into the seeded sequence.
- Random injections are clearly logged.
- Exact replay records the effective seed and injection values when deliberately enabled.

## 6. Logging and summaries

- Experimental logging is enabled by default.
- The user can turn logging off.
- Logs remain local and human-readable.
- No screenshots, keylogging, clipboard data, window titles or application content are collected.
- On closing, ask whether the user wants to save a session summary.
- If logging is disabled, do not display the save-summary question.
- CSV and readable HTML summaries may include intervals, sampled and actual durations, state transitions, movement counts, exceptions, mean, minimum, maximum, standard deviation and a compact distribution histogram.

## 7. Test Mode

- Accelerated Test Mode uses seconds instead of minutes.
- It enables several complete Neon cycles to be verified quickly.
- Test settings remain separate from Normal Mode defaults.
- Normal Mode must never inherit compressed Test Mode timing accidentally.

## 8. Motion and application boundaries

- Genuine input always overrides generated motion.
- Generated motion never creates mouse clicks, wheel activity or keyboard input.
- Neon Orbit will not switch to, focus or click other applications.
- It will not store detailed personal pointer coordinates or trajectories.
- It will not replay half, or any other proportion, of an individual's mouse movements.
- Neon high may include newly generated, non-personal linear motion variations mixed with its orbiting pattern.
- A virtual cursor may demonstrate hovering entirely inside the Neon Orbit window without moving the real pointer or activating controls.

## 9. Resource footprint

- Prefer one stable application process.
- No unnecessary background services or helper processes.
- No network access required for core operation.
- No busy-wait loops.
- Suspend unnecessary timers, hooks and animation resources during Neon low.
- Measure and document CPU, memory, timer wake-ups and state-transition behaviour.
- Use consistent, transparent executable and publisher metadata.

## 10. Development and release

- C#, .NET 10 LTS and WPF.
- Windows x64 initial target with Per-Monitor V2 DPI awareness.
- GitHub Actions provides an independent Windows build and test environment.
- Home machine provides local visual and pointer-behaviour testing.
- Produce a self-contained portable executable and a per-user installer.
- End users do not require Git or the .NET SDK.
- Include automated unit, integration, state-machine, probability, DPI, packaging and regression tests.
- Publish checksums and clearly labelled beta releases.
- Keep the repository private during initial development.
- Apply GNU General Public License v3.0 to the software.
- Put `Copyright © 2026 George Culache (george-gc-v01)` in the README and original source-file headers.
- State separately: `The Neon Orbit name and logo are not licensed for use in derivative products.`

## 11. Windows Awake

- `Keep Windows awake` is a visible, independently controllable toggle and defaults to On.
- `Keep display awake` defaults to Off.
- Windows Awake remains active while the app is minimised and across Neon high, Neon low and Neon wow.
- Neon wow pauses generated pointer motion, not the Windows Awake request.
- Closing the application or disabling the toggle releases the awake request.
- Manual lock, sleep, shutdown and enforced Windows or administrator policies remain available.

## 12. Machine readiness

- Home machine: Git 2.55.0.windows.5, WinGet 1.29.290 and Microsoft .NET SDK 10.0.401 are installed; ready for local builds and testing.
- Work machine: .NET 10 Desktop Runtime is present, but no SDK or Git command was detected; a self-contained approved release requires neither.

## 13. Model recommendation

- Use Astra with High reasoning for the first end-to-end implementation.
- Use Terra with High reasoning for focused subsequent corrections and iterations.
- Use Sol with High reasoning for an optional independent architecture, security and release-readiness review.
- Prioritise meaningful testing and defect correction; do not create artificial work merely to consume usage allowance.

## 14. Final safety and privacy boundary

- No external-application switching, focusing, hovering or clicking.
- No keyboard, wheel or button simulation.
- No detailed physical-pointer trajectory storage or replay.
- No process-name randomisation, impersonation, concealment or monitoring bypass.
- Logging remains local, transparent, optional and enabled by default.
- The application is an educational and experimental cursor-motion and Windows power-management laboratory; it makes no promise that third-party communication or monitoring products will classify activity in any particular way.

