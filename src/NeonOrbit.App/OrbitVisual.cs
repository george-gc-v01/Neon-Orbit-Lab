// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
using System.Globalization;

namespace NeonOrbit.App;

/// <summary>Original vector artwork. No downloaded assets or scene-dependent user data.</summary>
public sealed class OrbitVisual : FrameworkElement
{
    public double Time { get; set; }
    public bool Low { get; set; }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var center = new System.Windows.Point(ActualWidth / 2, ActualHeight / 2 + 6);
        var green = SystemParameters.HighContrast ? SystemColors.HighlightBrush : new SolidColorBrush(Color.FromRgb(102,255,153));
        var blue = SystemParameters.HighContrast ? SystemColors.HighlightBrush : new SolidColorBrush(Color.FromRgb(82,154,255));
        var line = new Pen(new SolidColorBrush(Color.FromArgb(65,120,177,193)), 1);
        for (int i = 0; i < 27; i++)
        {
            double x = (i * 137 + 21) % Math.Max(1, ActualWidth), y = (i * 53 + 19) % Math.Max(1, ActualHeight);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(55,151,205,226)), null, new(x,y), 1,1);
        }
        dc.PushTransform(new RotateTransform(-18, center.X, center.Y));
        foreach (var r in new[] { 45.0, 77.0, 105.0 }) dc.DrawEllipse(null, line, center, r * 1.5, r * .59);
        for (int i = 0; i < 3; i++)
        {
            var r = new[] {45.0,77.0,105.0}[i]; var a = Time * (.24 - i * .04) + i * 2.2;
            var p = new System.Windows.Point(center.X + Math.Cos(a) * r * 1.5, center.Y + Math.Sin(a) * r * .59);
            dc.DrawEllipse(i == 1 ? blue : green, null, p, i == 1 ? 5 : 3.5, i == 1 ? 5 : 3.5);
        }
        dc.Pop();
        double pulse = 1 + .07 * Math.Sin(Time * 1.4);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(18,102,255,153)), null, center, 27 * pulse,27 * pulse);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(40,102,255,153)), null, center, 17 * pulse,17 * pulse);
        dc.DrawEllipse(Low ? blue : green, null, center, 8,8);
        dc.DrawLine(line, new(22,25),new(50,25)); dc.DrawLine(line,new(50,25),new(62,37));
        dc.DrawLine(line,new(ActualWidth-22,ActualHeight-24),new(ActualWidth-51,ActualHeight-24));
        dc.DrawText(new FormattedText("N / 026", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Consolas"), 10, blue, VisualTreeHelper.GetDpi(this).PixelsPerDip), new(22,ActualHeight-33));
    }
}
