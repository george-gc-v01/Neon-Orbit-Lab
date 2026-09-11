// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;

namespace NeonOrbit.Core;

public sealed record LabSettings
{
    public Distribution High { get; init; } = new(25, 5, 15, 50);
    public Distribution Low { get; init; } = new(7.5, 2, 3, 15);
    public Distribution Wow { get; init; } = new(8, 4, 1, 25);
    public double DiameterMm { get; init; } = 20;
    public double RevolutionSeconds { get; init; } = 7;
    // Effective DPI is a UI scale, not measured panel PPI. Calibration is explicit.
    public double PixelsPerMm { get; init; } = 0;
    public int MaximumSessionMinutes { get; init; } = 240;
    public RandomMode RandomMode { get; init; } = RandomMode.Secure;
    public ulong Seed { get; init; } = 2026;
    public bool ExactReplayMetadata { get; init; }
    public bool Logging { get; init; } = true;
    public bool Awake { get; init; } = true;
    public bool DisplayAwake { get; init; }
    public bool ReducedMotion { get; init; }
    public bool MinimiseOnLaunch { get; init; } = true;
    public void Validate()
    {
        High.Validate(); Low.Validate(); Wow.Validate();
        if (High.Min < 15 || High.Max > 50 || Low.Min < 3 || Low.Max > 15 || Wow.Min < 1 || Wow.Max > 25)
            throw new ArgumentException("Intervals exceed the agreed hard bounds.");
        if (!double.IsFinite(DiameterMm) || DiameterMm < 2 || DiameterMm > 60 ||
            !double.IsFinite(RevolutionSeconds) || RevolutionSeconds < 2 || RevolutionSeconds > 60 ||
            !double.IsFinite(PixelsPerMm) || PixelsPerMm < 0 || PixelsPerMm > 40 ||
            MaximumSessionMinutes < 1 || MaximumSessionMinutes > 1440 || !Enum.IsDefined(RandomMode))
            throw new ArgumentException("Invalid motion, calibration, session or randomness settings.");
    }
    public static LabSettings Load(string path, out string? warning)
    {
        warning = null;
        if (!File.Exists(path)) return new();
        try
        {
            var settings = JsonSerializer.Deserialize<LabSettings>(File.ReadAllText(path)) ?? throw new FormatException();
            settings.Validate(); return settings;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or FormatException or IOException or NullReferenceException)
        { warning = "Settings could not be loaded; defaults are in use. " + ex.Message; return new(); }
    }
    public void Save(string path)
    {
        Validate(); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(path + ".tmp", path, true);
    }
}
