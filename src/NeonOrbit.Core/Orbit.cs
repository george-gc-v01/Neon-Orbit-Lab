// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
namespace NeonOrbit.Core;

public readonly record struct PointD(double X, double Y);
public readonly record struct RectD(double Left, double Top, double Right, double Bottom);
public sealed record Orbit(PointD Center, double Radius, double InitialAngle)
{
    public static double DiameterPixels(double millimetres, double effectiveDpi, double calibratedPixelsPerMm = 0)
    {
        if (millimetres <= 0 || effectiveDpi <= 0 || !double.IsFinite(millimetres + effectiveDpi + calibratedPixelsPerMm))
            throw new ArgumentException("Invalid size or DPI.");
        return millimetres * (calibratedPixelsPerMm > 0 ? calibratedPixelsPerMm : effectiveDpi / 25.4);
    }
    public static Orbit At(PointD anchor, RectD area, double diameterPixels)
    {
        var radius = Math.Min(diameterPixels / 2, Math.Min(area.Right - area.Left - 2, area.Bottom - area.Top - 2) / 2);
        if (radius <= 0) throw new ArgumentException("Monitor has no usable orbit area.");
        // Near an edge, find a bounded circle passing through the existing pointer when possible.
        for (int i = 0; i < 72; i++)
        {
            var angle = 2 * Math.PI * i / 72;
            var c = new PointD(anchor.X - radius * Math.Cos(angle), anchor.Y - radius * Math.Sin(angle));
            if (c.X - radius >= area.Left && c.X + radius < area.Right && c.Y - radius >= area.Top && c.Y + radius < area.Bottom)
                return new(c, radius, angle);
        }
        // Exact corners cannot fit a circle through the anchor. Adapter smoothly approaches this safe orbit.
        return new(new(Math.Clamp(anchor.X, area.Left + radius, area.Right - radius - 1),
            Math.Clamp(anchor.Y, area.Top + radius, area.Bottom - radius - 1)), radius, Math.PI);
    }
    public PointD Position(double elapsed, double period)
    {
        if (period <= 0) throw new ArgumentException("Period must be positive.");
        var a = InitialAngle + elapsed / period * 2 * Math.PI;
        return new(Center.X + Radius * Math.Cos(a), Center.Y + Radius * Math.Sin(a));
    }
}
