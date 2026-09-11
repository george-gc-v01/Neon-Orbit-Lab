# Neon Orbit

## Application Development Plan

**Subtitle:** Process Lab v2026  
**Status:** Proposed educational and experimental beta  
**Platform:** Windows 10 and Windows 11, x64  
**Recommended stack:** C#, .NET 10 LTS, WPF  
**GitHub repository name:** `Neon-Orbit-Lab`  
**Repository visibility during development:** Private  
**Licence selected:** GNU General Public License v3.0 (`GPL-3.0`)

## 1. Purpose

Neon Orbit is a transparent Windows desktop research and educational utility. Its main window presents the subtitle **Process Lab v2026**. The application demonstrates:

- DPI-aware cursor motion;
- genuine user-input priority;
- active and passive experimental scheduling;
- Windows keep-awake behaviour;
- observable low-resource application states; and
- local, privacy-preserving experiment logging.

Neon Orbit is intended to be lightweight, approachable and enjoyable for a wide audience. Its low-key footprint means efficient, quiet and standards-based operation: one stable process, no unnecessary services, no network requirement, no busy-wait loops and no concealed or misleading behaviour.

The application will remain visibly identifiable through its process metadata, window title, tray icon, About panel and documentation. It will not include concealment, misleading process names, hidden persistence, monitoring bypasses or undisclosed data collection.

## 2. Core Behaviour

### 2.1 Start-up

- Begin a newly sampled Neon high phase every time the application is launched.
- Begin the configured cursor-motion experiment automatically.
- Minimise the main window automatically after displaying its state briefly.
- Keep a clearly labelled notification-area icon available at all times.
- Provide Start, Stop, Minimise and Close App controls.
- Enable the clearly labelled `Keep Windows awake` toggle by default while Neon Orbit is running.
- Keep `Keep display awake` off by default, allowing the monitor to turn off normally.

### 2.2 Cursor-motion experiment

- Move the pointer in a smooth circular path approximately 20 mm in diameter.
- Convert millimetres to pixels using the active monitor's effective DPI.
- Keep the path within the usable bounds of the current monitor.
- Return to the original anchor point after each complete revolution.
- Complete each normal-mode revolution in approximately seven seconds.
- Do not generate clicks, wheel input or keyboard events.
- Stop motion immediately while a physical mouse button is held.

### 2.3 Genuine user-input priority and Neon wow

- Genuine mouse movement immediately overrides and suspends generated movement and displays the state `Neon wow`.
- Neon wow remains active while genuine physical movement continues.
- After the last genuine movement, sample a recovery delay from a truncated normal distribution with a mean of 8 seconds, standard deviation of 4 seconds and absolute limits of 1 to 25 seconds.
- Further genuine movement cancels the current recovery delay and starts a newly sampled delay after that movement ends.
- At the end of the recovery delay, Neon wow changes to Neon high and generated movement may resume, subject to the underlying scheduler and all safety rules.
- The underlying Neon high/Neon low schedule continues to use elapsed wall-clock time during Neon wow.
- Each Neon wow interval is logged as a user-generated exception and analysed separately from the scheduled high/low durations.
- The application must distinguish its own generated movement from genuine physical input.
- Locking Windows, suspending the session or closing the application stops generated movement.

## 3. Statistical Neon High and Neon Low Scheduler

The scheduler alternates between Neon high and Neon low phases. Each duration is sampled independently at the start of its phase. Neon wow is a user-input overlay state and does not replace the underlying scheduled phase.

### 3.1 Neon high phase

- Typical duration: 20 to 30 minutes.
- Occasional duration: below 20 minutes or above 30 minutes.
- Absolute permitted range: 15 to 50 minutes.
- Recommended distribution: truncated normal distribution with mean 25 minutes and standard deviation 5 minutes, sampled only within 15 to 50 minutes.
- Recommended display precision: duration generated to the nearest second but displayed as minutes and seconds.

During Neon high, the cursor-motion experiment is enabled, subject to physical-input priority and all safety rules. The interface displays `Neon high MM:SS` without the word `remaining`.

### 3.2 Neon low phase

- Typical duration: 5 to 10 minutes.
- Occasional duration: below 5 minutes or above 10 minutes.
- Absolute permitted range: 3 to 15 minutes.
- Recommended distribution: truncated normal distribution with mean 7.5 minutes and standard deviation 2 minutes, sampled only within 3 to 15 minutes.
- Recommended display precision: duration generated to the nearest second but displayed as minutes and seconds.

During Neon low:

- all generated pointer movement stops;
- cursor-motion timers and input-monitoring components used only by the motion engine are suspended;
- continuous polling is avoided;
- a single low-cost transition timer remains active;
- the main window remains minimised unless opened by the user;
- the tray tooltip clearly displays `Neon low MM:SS` without the word `remaining`;
- local experimental logging records the phase transition unless the user has disabled logging; and
- the process remains openly identifiable as Neon Orbit.

This is a low-resource operating state, not a hidden or disguised state.

### 3.3 Sampling rules

- Use a proper truncated-normal sampler based on rejection sampling.
- Do not simply clamp out-of-range results to the nearest limit, because this would create artificial concentrations at the minimum and maximum values.
- Use the operating system's secure random-number generator as the source of randomness.
- Store the sampled phase duration in the experiment log when logging is enabled.
- Permit a reproducible base seed in Developer Mode.
- Support a Hybrid Experiment option in which rare, clearly logged injections of fresh secure randomness perturb the seeded sequence.
- Record the effective seed and secure-random injection values when exact replay has been requested; otherwise record the injection event without exposing unnecessary internal values.
- Validate all sampled values before starting a phase.

## 4. User Interface

### 4.1 Visual direction

The interface should feel futuristic and science-fiction inspired while remaining minimal and easy to read:

- dark charcoal background;
- bright neon green as the primary colour, suggested `#66FF99`;
- bright neon blue, only slightly darker than the green, as the secondary colour, suggested `#287CFF`;
- subtle glow and pulse animation rather than constant decorative movement;
- minimal circuit traces framing key controls;
- a small, elegant orbital animation showing planets moving around a sun in the main window;
- reduced animation in Neon low and when Windows reduced-motion preferences are enabled; and
- sufficient text contrast and non-colour labels for accessibility.

### 4.2 Main window

The compact main window should show:

- application name and `Educational Beta` label;
- current state: Neon high, Neon low, Neon wow, Stopped or Closing;
- the state and countdown as `Neon high MM:SS` or `Neon low MM:SS`, without the word `remaining`;
- Neon wow elapsed time while genuine input is active, followed by its sampled recovery countdown;
- sampled duration for the current phase;
- current monitor DPI;
- calculated circle diameter in millimetres and pixels;
- movement-event count;
- Start, Stop, Minimise and Close App buttons; and
- a link to About, Privacy and Experiment Log information.

Keep advanced controls inside a collapsed `LAB SETTINGS` panel. This panel contains distribution parameters, random-seed controls, logging, Test Mode and other research settings, leaving the normal interface minimal.

The notification-area menu should provide:

- Open Neon Orbit;
- Start or Resume;
- Stop;
- current phase and countdown;
- Emergency Stop; and
- Exit.

The standard top-right Windows `X` closes the application completely. Minimising is performed only through the dedicated Minimise control or tray action.

### 4.3 Tray icon

- Use a single capital `N` as the tray icon.
- Apply a continuous transition from bright neon green to slightly darker neon blue along the two legs and diagonal of the N.
- Keep both colours bright enough to remain legible at 16, 20, 24 and 32 pixel tray sizes.
- Use a simplified silhouette with no fine circuitry that would disappear at tray scale.
- Apply a subtle state pulse only where Windows permits animated tray updates without excessive resource use.
- Provide static high-contrast icon variants as a fallback and for reduced-motion mode.

### 4.4 Session summary

When the application closes, ask `Save a summary of this Neon session?` The optional CSV and human-readable HTML summary should include:

- session start, finish and total duration;
- Neon high, Neon low and Neon wow intervals;
- sampled and actual durations;
- minimum, maximum, mean and standard deviation;
- a compact distribution histogram;
- user-generated Neon wow exceptions;
- logged hybrid-randomness injection events;
- a state-transition timeline; and
- generated cursor-movement count.

If experimental logging has been disabled, do not show the close-time save-summary prompt because no retained session record is available to summarise.

## 5. Transparency, Privacy and Resource Use

- Product name: `Neon Orbit`.
- Main-window subtitle: `Process Lab v2026`.
- Executable and Task Manager process name: `NeonOrbitProcess.v2026.exe`.
- File description: `Educational cursor-motion and process-state research utility`.
- Process, executable, installer and publisher metadata must use consistent names.
- No screenshots, keylogging, clipboard access, window-title collection or application-content inspection.
- No network access is required for core operation.
- Logging is local, human-readable and enabled by default, with a clear option to turn it off.
- The user can open, export and delete the log.
- When closing, ask whether the user would like to save a summary of the session's Neon states.
- Neon low should release unnecessary hooks, timers and animation resources.
- Resource use should be measurable in tests and documented in releases.
- No automatic Windows start-up by default.
- Use one stable process wherever technically practical and avoid unnecessary helper processes or background services.
- Profile CPU, memory, wake-ups and timer activity in every state, with Neon low designed to approach idle application behaviour.
- Keep executable metadata, process identity and behaviour consistent and transparent.

### 5.1 In-app motion demonstration

- At a randomly sampled interval of up to four minutes, Neon Orbit may show a virtual cursor demonstration inside its own window.
- The virtual cursor may hover over illustrated controls, orbit elements and circuit nodes without moving the real Windows pointer or activating controls.
- The demonstration must never click, focus or switch to another application.
- Neon wow may record duration and aggregate event counts, but must not store a person's detailed pointer coordinates or trajectory.
- Neon high may mix the seven-second orbit with newly generated, non-personal linear motion variations; these are statistically generated rather than copied from a user's movements.

## 6. Safety Controls

- Global emergency-stop keyboard shortcut.
- Tray-menu Stop and Exit commands.
- Immediate pause on genuine mouse input.
- Immediate stop while any mouse button is held.
- Pause when Windows is locked.
- Stop safely on session shutdown, sleep or application exit.
- Prevent cursor movement outside the current monitor.
- Maximum continuous session duration configurable by the user.
- Restore the cursor to a stable location when a generated revolution ends normally.

## 7. Technical Architecture

Recommended components:

1. **WPF presentation layer** — compact window, settings, status and accessibility.
2. **Application state machine** — Neon high, Neon low, Neon wow, Stopped and Closing states.
3. **Motion engine** — DPI-aware circle generation and monitor-boundary protection.
4. **Input observer** — detects genuine user input and ignores self-generated events.
5. **Phase scheduler** — truncated-normal sampling and safe phase transitions.
6. **Power-state service** — optional presentation keep-awake mode using documented Windows APIs.
7. **Local experiment logger** — privacy-limited structured CSV or JSON Lines output.
8. **Tray service** — visible status, countdown and exit controls.
9. **Visual system** — reusable neon palette, N icon assets, circuitry components and lightweight orbital animation.

Target configuration:

- `net10.0-windows`
- Windows x64
- per-monitor DPI awareness V2
- self-contained portable release
- optional per-user installer without administrator privileges

### 7.1 Windows Awake behaviour

- `Keep Windows awake` defaults to On and uses documented Windows power-request APIs.
- The awake request remains in effect during Neon high, Neon low and Neon wow while the application is running.
- Minimising the window does not pause or disable the awake request.
- Neon wow pauses generated cursor movement only; it does not disable Windows Awake.
- `Keep display awake` defaults to Off.
- Stopping the experiment may leave Windows Awake active if its separate toggle remains On; the interface must make this explicit.
- Closing the application, disabling the toggle or an abnormal-exit cleanup path releases the power request.
- Neon Orbit never prevents manual lock, sleep, shutdown or administrator policy enforcement.

## 8. Development Phases

### Phase 1 — Foundation

- Create the .NET 10 WPF solution.
- Configure repository metadata, licence, documentation and continuous integration.
- Implement the main window, tray icon and state machine skeleton.

### Phase 2 — Motion and DPI

- Implement physical-size conversion and circular movement.
- Add multiple-monitor and screen-edge handling.
- Add configurable movement speed and diameter limits.

### Phase 3 — Input priority and safety

- Implement genuine-input detection.
- Add Neon wow detection and the truncated-normal 1-to-25-second recovery delay.
- Add mouse-button, Windows lock, sleep and emergency-stop protections.

### Phase 4 — Statistical scheduler

- Implement truncated-normal sampling.
- Add Neon high and Neon low phase transitions.
- Add countdown display, validation and deterministic developer tests.
- Verify the observed sample distributions statistically.
- Add an accelerated Test Mode that uses seconds in place of minutes so complete Neon high/Neon low/Neon wow cycles can be verified quickly without changing Normal Mode defaults.

### Phase 5 — Neon low resource state

- Suspend the motion engine and unnecessary observers.
- Replace repeated polling with a one-shot transition timer.
- Measure CPU and memory use during Neon high, Neon wow and Neon low states.
- Keep the passive state visible through the tray and log.

### Phase 6 — Logging, packaging and release

- Add default-enabled local experimental logging with a user-controlled off switch.
- Add the close-time session-summary prompt and CSV/HTML statistical report.
- Complete privacy, security and usage documentation.
- Create portable and installer builds.
- Run automated unit, integration and Windows UI tests.
- Publish checksums and a versioned beta release.

## 9. Verification

Automated tests should cover:

- state-machine transitions;
- genuine-input Neon wow activation and the 1-to-25-second recovery distribution;
- seven-second normal-mode circular revolutions;
- accelerated Test Mode timing and separation from Normal Mode settings;
- truncated-normal boundaries and approximate distribution properties;
- no movement during Neon low or Stopped states;
- timer cancellation and application closure;
- DPI conversion;
- monitor-boundary calculations; and
- corrupted or invalid settings recovery.

Manual Windows tests should cover:

- Windows 10 and Windows 11;
- 100%, 125%, 150% and 200% scaling;
- one monitor and mixed-DPI multiple monitors;
- screen edges and taskbar boundaries;
- mouse dragging and rapid physical movement;
- lock, unlock, sleep and resume;
- long Neon high/Neon low cycles; and
- CPU and memory use in Neon low.

## 10. GitHub Repository Settings

- Repository name: `Neon-Orbit-Lab`.
- Keep the repository private during initial development and intellectual-property assessment.
- GNU General Public License v3.0 selected. If the repository becomes public, distributed modified versions of GPL-covered code must remain available under GPL-3.0, subject to the licence terms.
- Add `Copyright © 2026 George Culache (george-gc-v01)` to the README and original source-file headers.
- Add this separate branding reservation: `The Neon Orbit name and logo are not licensed for use in derivative products.`
- Treat the name, logo and other brand identifiers as outside the GPL software grant to the extent permitted by law; obtain legal advice before relying on this statement commercially.
- Visual Studio `.gitignore`.
- Protected `main` branch with required build and test checks.
- Dependabot alerts and weekly dependency updates.
- Secret scanning and push protection where available.
- GitHub Actions on a Windows runner for restore, build, test and packaging checks.
- Issues enabled with bug-report and feature-request templates.
- Security policy explaining responsible vulnerability reporting.
- Tagged beta releases beginning with `v0.1.0-beta`.
- No signing certificates, private keys or credentials committed to the repository.

## 11. Confirmed Design Decisions

1. Time spent in Neon wow continues to count toward the underlying Neon high/Neon low wall-clock schedule and is also logged separately as a user-generated exception.
2. Every launch begins with a newly sampled Neon high phase rather than resuming an interrupted phase.
3. Experimental logging is enabled by default, with a clear user option to disable it.
4. Neon wow remains active while genuine input continues, then transitions to Neon high after a truncated-normal recovery delay between 1 and 25 seconds.
5. The public application identity is Neon Orbit; the main-window subtitle is Process Lab v2026; and the stable executable name is `NeonOrbitProcess.v2026.exe`.
6. Normal Mode uses approximately seven seconds per complete 20 mm revolution.
7. Accelerated Test Mode uses seconds instead of minutes for rapid cycle verification.
8. Advanced controls remain inside a collapsed `LAB SETTINGS` panel.
9. The standard top-right Windows `X` closes the application completely.
10. The GitHub repository is named `Neon-Orbit-Lab`; the application remains named Neon Orbit.
11. If experimental logging is disabled, the close-time `Save a summary?` prompt is also disabled.
12. Neon Orbit will not switch to or click other applications, store detailed personal pointer trajectories, or replay an individual's mouse movements. Any linear variation used by Neon high is newly generated, non-personal and statistically defined.
13. `Keep Windows awake` defaults to On; `Keep display awake` defaults to Off. Minimising does not stop the awake request, and only Neon wow pauses generated cursor movement.
14. The repository uses GPL-3.0, includes George Culache's copyright notice, and reserves the Neon Orbit name and logo from derivative-product branding.

## 12. Recommended Development Model

Use **Astra with High reasoning** for the full first implementation. This is preferred because the task combines Windows-native engineering, statistical state logic, polished visual design, accessibility, automated testing and release packaging. Use Terra with High reasoning for focused later iterations and Sol with High reasoning as an optional independent review of architecture, security and release readiness.

