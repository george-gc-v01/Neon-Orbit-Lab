// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
namespace NeonOrbit.Core;

public enum NeonState { Stopped, High, Low, Wow }
public sealed record LabEvent(double At, string Kind, string State, double Value = 0, string Detail = "");

/// <summary>Single-threaded deterministic state model. All time is monotonic session seconds.</summary>
public sealed class LabEngine
{
    private readonly LabSettings settings;
    private readonly RandomStream phaseRng, inputRng;
    private double maximumAt, wowUntil, lastTime;
    private bool wow, held;
    public bool Running { get; private set; }
    public bool TestMode { get; }
    public NeonState Phase { get; private set; } = NeonState.High;
    public NeonState State => !Running ? NeonState.Stopped : wow ? NeonState.Wow : Phase;
    public bool CanMove => Running && !wow && !held && Phase == NeonState.High;
    public double PhaseEnd { get; private set; }
    public double SampledSeconds { get; private set; }
    public double RecoveryEnd => wowUntil;
    public double WowStart { get; private set; }
    public bool ButtonHeld => held;
    public event Action<LabEvent>? Recorded;

    public LabEngine(LabSettings settings, bool testMode = false)
    {
        settings.Validate(); this.settings = settings; TestMode = testMode;
        phaseRng = new(settings.RandomMode, settings.Seed);
        // Hybrid injection is confined to phase draws, never proportional to physical mouse event rate.
        inputRng = new(settings.RandomMode == RandomMode.Hybrid ? RandomMode.Seeded : settings.RandomMode, settings.Seed ^ 0xA5A5A5A5UL);
        phaseRng.Injected += e => Entropy("phase", e);
        inputRng.Injected += e => Entropy("input", e);
    }
    private void Entropy(string stream, EntropyEvent e) => Recorded?.Invoke(new(lastTime,
        "entropy", State.ToString(), e.Draw, settings.ExactReplayMetadata ? $"{stream}:seed={e.Seed}" : stream));
    public void Start(double now)
    {
        CheckTime(now);
        if (Running) return;
        Running = true; wow = false; held = false;
        maximumAt = now + settings.MaximumSessionMinutes * 60;
        Recorded?.Invoke(new(now, "start", "High", 0, TestMode ? "Test" : "Normal"));
        BeginPhase(NeonState.High, now);
    }
    public void Stop(double now, string reason = "User stop")
    {
        CheckTime(now);
        if (!Running) return;
        if (wow) Recorded?.Invoke(new(now, "wow_end", "Wow", now - WowStart, "interrupted"));
        Running = false; wow = false; held = false;
        Recorded?.Invoke(new(now, "stop", "Stopped", 0, reason));
    }
    public void Tick(double now)
    {
        CheckTime(now);
        if (!Running) return;
        if (now >= maximumAt) { Stop(now, "Session limit"); return; }
        while (now >= PhaseEnd)
            BeginPhase(Phase == NeonState.High ? NeonState.Low : NeonState.High, PhaseEnd);
        if (wow && !held && now >= wowUntil)
        {
            wow = false;
            Recorded?.Invoke(new(now, "wow_end", "Wow", now - WowStart, "completed"));
        }
    }
    public void Input(double now, bool anyButtonHeld)
    {
        Tick(now);
        if (!Running) return;
        if (!wow) { wow = true; WowStart = now; Recorded?.Invoke(new(now, "wow_start", "Wow")); }
        held = anyButtonHeld;
        var delay = settings.Wow.Sample(inputRng);
        wowUntil = now + delay;
        // No event/coordinate per pixel: only retained final delay when wow completes is needed for duration analysis.
    }
    public double NextDeadline(double now)
    {
        if (!Running) return double.PositiveInfinity;
        var next = Math.Min(PhaseEnd, maximumAt);
        if (wow && !held) next = Math.Min(next, wowUntil);
        return Math.Max(now + .001, next);
    }
    private void BeginPhase(NeonState phase, double at)
    {
        Phase = phase;
        var minutes = (phase == NeonState.High ? settings.High : settings.Low).Sample(phaseRng);
        SampledSeconds = Math.Round(minutes * (TestMode ? 1 : 60), 3);
        PhaseEnd = at + SampledSeconds;
        Recorded?.Invoke(new(at, "phase", phase.ToString(), SampledSeconds));
    }
    private void CheckTime(double now)
    {
        if (!double.IsFinite(now) || now < lastTime) throw new ArgumentOutOfRangeException(nameof(now));
        lastTime = now;
    }
}
