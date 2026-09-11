// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NeonOrbit.Core;

public sealed record StateInterval(string State, double Start, double End)
{ public double Seconds => End - Start; }

public sealed class SessionLog : IDisposable
{
    private StreamWriter? writer;
    private readonly List<LabEvent> events = [];
    public IReadOnlyList<LabEvent> Events => events;
    public bool Enabled { get; private set; }
    public string Path { get; }
    public DateTimeOffset StartedUtc { get; } = DateTimeOffset.UtcNow;
    public SessionLog(string path) { Path = path; }
    public void Enable(bool enabled)
    {
        writer?.Dispose(); writer = null; Enabled = false;
        if (!enabled) { events.Clear(); return; }
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        writer = new(Path, true, Encoding.UTF8) { AutoFlush = true }; Enabled = true;
    }
    public void Add(LabEvent e)
    {
        if (!Enabled) return;
        if (events.Count >= 20000) throw new IOException("Session log limit reached; export and begin a new session.");
        events.Add(e); writer?.WriteLine(JsonSerializer.Serialize(e));
    }
    public void Delete()
    { Enable(false); if (File.Exists(Path)) File.Delete(Path); }
    public void Dispose() { writer?.Dispose(); writer = null; Enabled = false; }

    public static List<StateInterval> Timeline(IEnumerable<LabEvent> source, double end)
    {
        List<StateInterval> result = [];
        string phase = "Stopped", visible = "Stopped";
        bool wow = false; double start = 0;
        foreach (var e in source.OrderBy(e => e.At))
        {
            switch (e.Kind)
            {
                case "start": phase = "High"; wow = false; break;
                case "phase": phase = e.State; break;
                case "wow_start": wow = true; break;
                case "wow_end": wow = false; break;
                case "stop": phase = "Stopped"; wow = false; break;
                default: continue;
            }
            var next = wow ? "Wow" : phase;
            if (next == visible) continue;
            if (e.At > start) result.Add(new(visible, start, e.At));
            start = e.At; visible = next;
        }
        if (end > start) result.Add(new(visible, start, end));
        return result;
    }
    public void Export(string htmlPath, double end, long movements, string mode)
    {
        static string F(double d) => d.ToString("F3", CultureInfo.InvariantCulture);
        static string H(string s) => WebUtility.HtmlEncode(s);
        var csv = new StringBuilder("elapsed_seconds,kind,state,value,detail\r\n");
        foreach (var e in events)
            csv.AppendLine($"{F(e.At)},{Quote(e.Kind)},{Quote(e.State)},{F(e.Value)},{Quote(e.Detail)}");
        File.WriteAllText(System.IO.Path.ChangeExtension(htmlPath, ".csv"), csv.ToString());
        var body = new StringBuilder("<!doctype html><html lang='en'><meta charset='utf-8'><title>Neon Orbit session</title><style>body{background:#101820;color:#e1eee9;font:16px system-ui;max-width:960px;margin:40px auto;padding:20px}h1,h2{color:#66ff99}table{border-collapse:collapse;width:100%}td,th{border-bottom:1px solid #345;padding:8px;text-align:left}meter{width:220px}small{color:#b8c9d0}</style>");
        body.Append($"<h1>Neon Orbit · session report</h1><p>{H(mode)} · Start {StartedUtc:O}<br>Report generated {DateTimeOffset.UtcNow:O}<br>Elapsed {F(end)} s · Generated movement events {movements}</p>");
        body.Append("<p>Local experiment metadata only. No personal cursor trajectories. Timeline totals are mutually exclusive; scheduled samples and Wow overlays are analysed separately.</p>");
        body.Append("<h2>Scheduled durations</h2><table><tr><th>Phase</th><th>n</th><th>Mean (s)</th><th>SD (s)</th><th>Min / max (s)</th></tr>");
        foreach (var state in new[] { "High", "Low" })
        {
            var samples = events.Where(e => e.Kind == "phase" && e.State == state).Select(e => e.Value).ToArray();
            if (samples.Length == 0) continue;
            var mean = samples.Average();
            var sd = Math.Sqrt(samples.Sum(x => (x - mean) * (x - mean)) / samples.Length);
            body.Append($"<tr><td>Neon {state.ToLowerInvariant()}</td><td>{samples.Length}</td><td>{F(mean)}</td><td>{F(sd)}</td><td>{F(samples.Min())} / {F(samples.Max())}</td></tr>");
        }
        body.Append("</table><small>SD is the population statistic of this session's samples, not an estimate of the untruncated normal's sigma.</small><h2>Duration histogram</h2>");
        foreach (var state in new[] { "High", "Low" })
        {
            var samples = events.Where(e => e.Kind == "phase" && e.State == state).Select(e => e.Value).ToArray();
            if (samples.Length == 0) continue;
            body.Append($"<h3>Neon {state.ToLowerInvariant()}</h3>");
            var min = samples.Min(); var step = Math.Max(1, (samples.Max() - min) / 8);
            for (int i = 0; i < 8; i++)
            {
                var count = samples.Count(x => x >= min + i * step && (i == 7 || x < min + (i + 1) * step));
                body.Append($"<div>{F(min + i * step)}–{F(min + (i + 1) * step)} s <meter min='0' max='{samples.Length}' value='{count}'></meter> {count}</div>");
            }
        }
        var timeline = Timeline(events, end);
        body.Append("<h2>Actual visible-state totals</h2>");
        foreach (var group in timeline.GroupBy(t => t.State)) body.Append($"<p>{H(group.Key)}: {F(group.Sum(t => t.Seconds))} s</p>");
        body.Append("<h2>State timeline</h2><table><tr><th>State</th><th>Start (s)</th><th>End (s)</th><th>Actual (s)</th></tr>");
        foreach (var t in timeline) body.Append($"<tr><td>{H(t.State)}</td><td>{F(t.Start)}</td><td>{F(t.End)}</td><td>{F(t.Seconds)}</td></tr>");
        body.Append("</table><p>The companion CSV contains sampled durations, interruptions, configuration and entropy events.</p></html>");
        File.WriteAllText(htmlPath, body.ToString());
    }
    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
