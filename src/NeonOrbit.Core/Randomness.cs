// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
using System.Security.Cryptography;

namespace NeonOrbit.Core;

public enum RandomMode { Secure, Seeded, Hybrid }
public sealed record EntropyEvent(long Draw, ulong Seed);

/// <summary>Versioned SplitMix64 deterministic stream; crypto entropy only for Secure/Hybrid.</summary>
public sealed class RandomStream
{
    private ulong state;
    private readonly Func<ulong> entropy;
    private readonly Queue<EntropyEvent>? replay;
    public RandomMode Mode { get; }
    public long Draws { get; private set; }
    public event Action<EntropyEvent>? Injected;
    public RandomStream(RandomMode mode, ulong seed, Func<ulong>? entropy = null,
        IEnumerable<EntropyEvent>? replay = null)
    {
        Mode = mode; state = seed;
        this.entropy = entropy ?? (() => BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8)));
        this.replay = replay == null ? null : new Queue<EntropyEvent>(replay);
    }
    public double Next()
    {
        Draws++;
        if (Mode == RandomMode.Secure) return Unit(entropy());
        if (Mode == RandomMode.Hybrid)
        {
            // 1% chance at each logical sample draw. Replay never consults secure entropy.
            if (replay != null)
            {
                if (replay.TryPeek(out var e) && e.Draw == Draws) { state = e.Seed; replay.Dequeue(); }
            }
            else if (entropy() % 100 == 0)
            {
                state = entropy(); Injected?.Invoke(new(Draws, state));
            }
        }
        state = unchecked(state + 0x9E3779B97F4A7C15UL);
        var z = state;
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        return Unit(z ^ (z >> 31));
    }
    private static double Unit(ulong n) => ((n >> 11) + 0.5) * (1.0 / 9007199254740992.0);
}

public sealed record Distribution(double Mean, double Sigma, double Min, double Max)
{
    public void Validate()
    {
        if (!double.IsFinite(Mean) || !double.IsFinite(Sigma) || !double.IsFinite(Min) ||
            !double.IsFinite(Max) || Sigma <= 0 || Min <= 0 || Max <= Min || Mean < Min || Mean > Max)
            throw new ArgumentException("Distribution requires finite positive bounds, sigma and an in-range mean.");
    }
    public double Sample(RandomStream rng)
    {
        Validate();
        for (var i = 0; i < 10000; i++)
        {
            var u = Math.Clamp(rng.Next(), double.Epsilon, 1 - 1e-16);
            var z = Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * rng.Next());
            var x = Mean + Sigma * z;
            if (x >= Min && x <= Max) return x;
        }
        throw new InvalidOperationException("Sampling failed: check distribution parameters.");
    }
}
