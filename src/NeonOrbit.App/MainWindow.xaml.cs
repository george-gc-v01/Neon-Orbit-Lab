// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace NeonOrbit.App;

public partial class MainWindow : Window
{
    private readonly bool preview, smoke;
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly DispatcherTimer timer = new(DispatcherPriority.Background);
    private readonly DispatcherTimer minimiseTimer = new();
    private readonly PowerService power = new();
    private InputObserver? observer;
    private Forms.NotifyIcon? tray;
    private System.Drawing.Icon? trayIcon;
    private HwndSource? source;
    private LabSettings settings = new();
    private LabEngine engine = null!;
    private SessionLog log = null!;
    private Orbit? orbit;
    private double orbitAt;
    private PointD approach;
    private PointD? lastGenerated;
    private nint monitor;
    private nint handle;
    private uint dpi = 96;
    private long movements;
    private bool ready, cleaned, closing, sessionUnavailable;
    private string? warning;
    private readonly string dataRoot;
    private string SettingsPath => System.IO.Path.Combine(dataRoot, "settings.json");
    private double Now => clock.Elapsed.TotalSeconds;

    public MainWindow(bool preview, bool smoke)
    {
        this.preview = preview; this.smoke = smoke;
        dataRoot = preview ? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NeonOrbitPreview", Guid.NewGuid().ToString("N")) :
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeonOrbit");
        InitializeComponent();
        // Keep controls reachable on laptop screens and at 150–200% Windows scaling.
        var available = SystemParameters.WorkArea;
        MinHeight = Math.Min(MinHeight, Math.Max(300, available.Height - 24));
        MinWidth = Math.Min(MinWidth, Math.Max(320, available.Width - 24));
        Height = Math.Min(Height, Math.Max(MinHeight, available.Height - 24));
        Width = Math.Min(Width, Math.Max(MinWidth, available.Width - 24));
        settings = preview ? new LabSettings { MinimiseOnLaunch = false, Awake = false, Logging = false } : LabSettings.Load(SettingsPath, out warning);
        FillSettings();
        log = new(System.IO.Path.Combine(dataRoot, "logs", $"session-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.jsonl"));
        try { log.Enable(settings.Logging); } catch (IOException ex) { warning = ex.Message; LogToggle.IsChecked = false; }
        BuildEngine(false);
        timer.Tick += OnTimer;
        minimiseTimer.Interval = TimeSpan.FromSeconds(3);
        minimiseTimer.Tick += (_, _) => { minimiseTimer.Stop(); if (!closing && settings.MinimiseOnLaunch) Minimise(); };
        SourceInitialized += OnSource;
        Loaded += OnLoaded;
        StateChanged += (_, _) => { if (ready) { if (WindowState == WindowState.Minimized) ShowInTaskbar = false; Schedule(); } };
        Closing += OnClosing;
    }
    private void FillSettings()
    {
        AwakeToggle.IsChecked = settings.Awake; DisplayToggle.IsChecked = settings.DisplayAwake;
        LogToggle.IsChecked = settings.Logging; MotionToggle.IsChecked = settings.ReducedMotion;
        MinimiseToggle.IsChecked = settings.MinimiseOnLaunch; ReplayToggle.IsChecked = settings.ExactReplayMetadata;
        RandomChoice.SelectedIndex = (int)settings.RandomMode;
        SeedBox.Text = settings.Seed.ToString(CultureInfo.InvariantCulture);
        DiameterBox.Text = settings.DiameterMm.ToString(CultureInfo.InvariantCulture);
        PeriodBox.Text = settings.RevolutionSeconds.ToString(CultureInfo.InvariantCulture);
        CalibrationBox.Text = settings.PixelsPerMm.ToString(CultureInfo.InvariantCulture);
        MaxBox.Text = settings.MaximumSessionMinutes.ToString(CultureInfo.InvariantCulture);
        static string D(Distribution d) => FormattableString.Invariant($"{d.Mean} / {d.Sigma} / {d.Min} / {d.Max}");
        HighBox.Text = D(settings.High); LowBox.Text = D(settings.Low); WowBox.Text = D(settings.Wow);
    }
    private void BuildEngine(bool test)
    {
        engine = new(settings, test);
        engine.Recorded += e => { Record(e); orbit = null; lastGenerated = null; };
        orbit = null;
    }
    private void Record(LabEvent e)
    {
        try { log.Add(e); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { log.Enable(false); LogToggle.IsChecked = false; NoticeLabel.Text = "Log disabled: " + ex.Message; }
    }
    private void OnSource(object? sender, EventArgs e)
    {
        handle = new WindowInteropHelper(this).Handle;
        source = HwndSource.FromHwnd(handle); source?.AddHook(WindowMessage);
        if (preview) return;
        if (!Native.RegisterHotKey(handle, 2026, 0x4000 | 0x2 | 0x1, 0x7B))
            warning = "Emergency shortcut unavailable. Use the tray Stop/Exit controls.";
        if (!Native.WTSRegisterSessionNotification(handle, 0))
        { sessionUnavailable = true; warning = "Session notifications unavailable; cursor experiment disabled."; }
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!preview)
        {
            CreateTray();
            try { observer = new(OnInput); } catch (Win32Exception ex) { warning = ex.Message; }
        }
        ready = true;
        if (preview) ModeLabel.Text = "PREVIEW · NO INPUT";
        if (warning != null) NoticeLabel.Text = warning;
        StartExperiment();
        if (!preview && settings.MinimiseOnLaunch) minimiseTimer.Start();
        if (smoke)
        {
            // Render only our own window. Never capture the desktop or other applications.
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                UpdateLayout();
                if (Background is not SolidColorBrush background || background.Color.R > 40)
                    throw new InvalidOperationException("Preview default theme is not dark.");
                if (observer != null || movements != 0) throw new InvalidOperationException("Preview unexpectedly activated input.");
                // Exercise UI-controlled Test Mode and stop/start without native cursor generation.
                TestToggle.IsChecked = true; ApplyClick(this, new RoutedEventArgs());
                if (!engine.TestMode || engine.SampledSeconds > 50) throw new InvalidOperationException("Test Mode UI failed.");
                StopExperiment(); if (engine.CanMove) throw new InvalidOperationException("Stop control failed.");
                TestToggle.IsChecked = false; ApplyClick(this, new RoutedEventArgs());
                if (engine.SampledSeconds < 900) throw new InvalidOperationException("Normal timing not restored.");
                UpdateLayout();
                var content = (FrameworkElement)Content;
                var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(content);
                Directory.CreateDirectory("artifacts");
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var stream = File.Create("artifacts/neon-orbit-preview.png")) encoder.Save(stream);
                File.WriteAllText("artifacts/ui-smoke.txt", "PASS: WPF dark theme, Test Mode controls, Stop/Start, normal timing restoration, preview isolation and own-window rendering. No input or power requests.");
                Close();
            }));
        }
    }
    private void CreateTray()
    {
        trayIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application;
        tray = new Forms.NotifyIcon { Icon = trayIcon, Text = "Neon Orbit · Process Lab v2026", Visible = true };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Neon Orbit", null, (_, _) => OpenWindow());
        menu.Items.Add("Start", null, (_, _) => StartExperiment());
        menu.Items.Add("Stop motion", null, (_, _) => StopExperiment());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Emergency stop · Ctrl+Alt+F12", null, (_, _) => EmergencyStop());
        menu.Items.Add("Exit", null, (_, _) => Close());
        tray.ContextMenuStrip = menu; tray.DoubleClick += (_, _) => OpenWindow();
    }
    private nint WindowMessage(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == 0x312 && wParam == 2026) { EmergencyStop(); handled = true; }
        if ((msg == 0x2B1 && wParam == 7) || (msg == 0x218 && wParam == 4))
        { sessionUnavailable = true; StopExperiment("Windows lock or suspend"); power.Dispose(); }
        if ((msg == 0x2B1 && wParam == 8) || (msg == 0x218 && (wParam == 7 || wParam == 18)))
        { sessionUnavailable = false; NoticeLabel.Text = "Session resumed. Press Start when ready."; }
        if (msg == 0x11) { closing = true; EmergencyCleanup(); } // Windows shutdown, no blocking save dialog.
        if (msg == 0x7E || msg == 0x2E0) { orbit = null; }
        return 0;
    }
    private void StartExperiment()
    {
        if (!ready || cleaned || sessionUnavailable) return;
        if (!preview && observer == null) { NoticeLabel.Text = warning ?? "Input observer is unavailable."; return; }
        if (!preview) ApplyPower();
        engine.Start(Now);
        if (!preview && Native.ButtonHeld) engine.Input(Now, true);
        Record(new(Now, "configuration", engine.State.ToString(), 0,
            $"mode={settings.RandomMode};test={engine.TestMode};diameter={settings.DiameterMm};period={settings.RevolutionSeconds};seed={(settings.RandomMode == RandomMode.Secure ? "n/a" : settings.Seed.ToString(CultureInfo.InvariantCulture))}"));
        RenderStatus(); Schedule();
    }
    private void StopExperiment(string reason = "User stop")
    {
        if (!ready || cleaned) return;
        engine.Stop(Now, reason); orbit = null; RenderStatus(); Schedule();
    }
    private void OnInput(bool held)
    {
        if (!ready || cleaned || closing || !engine.Running) return;
        engine.Input(Now, held); orbit = null; lastGenerated = null; Schedule();
    }
    private void OnTimer(object? sender, EventArgs e)
    {
        timer.Stop();
        if (cleaned || closing) return;
        try
        {
            var now = Now; engine.Tick(now);
            if (!preview && engine.Running && engine.ButtonHeld && !Native.ButtonHeld) engine.Input(now, false);
            if (!preview && engine.CanMove)
            {
                if (Native.ButtonHeld) engine.Input(now, true);
                else MovePointer(now);
            }
            RenderStatus();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException)
        { StopExperiment("Motion error"); NoticeLabel.Text = ex.Message; }
        Schedule();
    }
    private void MovePointer(double now)
    {
        if (!Native.GetCursorPos(out var point)) throw new Win32Exception("Cursor unavailable.");
        if (lastGenerated is PointD expected && (Math.Abs(expected.X - point.X) > 3 || Math.Abs(expected.Y - point.Y) > 3))
        {
            // Secondary ownership check if Windows drops a slow hook or another utility moves the pointer.
            engine.Input(now, Native.ButtonHeld); orbit = null; lastGenerated = null; return;
        }
        var activeMonitor = Native.MonitorFromPoint(point, 2);
        if (orbit == null || monitor != activeMonitor)
        {
            if (monitor != 0 && monitor != activeMonitor && settings.PixelsPerMm > 0)
            { settings = settings with { PixelsPerMm = 0 }; CalibrationBox.Text = "0"; NoticeLabel.Text = "Monitor changed; calibration reset to DPI estimate."; }
            monitor = activeMonitor;
            var info = new Native.MonitorInfo { Size = Marshal.SizeOf<Native.MonitorInfo>() };
            if (!Native.GetMonitorInfo(monitor, ref info)) throw new Win32Exception("Monitor unavailable.");
            if (Native.GetDpiForMonitor(monitor, 0, out dpi, out _) != 0) dpi = 96;
            var w = info.Work;
            approach = new(point.X, point.Y);
            orbit = Orbit.At(approach, new(w.Left, w.Top, w.Right, w.Bottom), Orbit.DiameterPixels(settings.DiameterMm, dpi, settings.PixelsPerMm));
            orbitAt = now;
            Record(new(now, "display", engine.State.ToString(), dpi, $"diameter_px={orbit.Radius * 2:F3};calibrated={settings.PixelsPerMm > 0}"));
        }
        var target = orbit.Position(Math.Max(0, now - orbitAt - .5), settings.RevolutionSeconds);
        var blend = Math.Clamp((now - orbitAt) / .5, 0, 1); blend = blend * blend * (3 - 2 * blend);
        target = new(approach.X + blend * (target.X - approach.X), approach.Y + blend * (target.Y - approach.Y));
        Native.Move(target); lastGenerated = target; movements++;
    }
    private void Schedule()
    {
        timer.Stop();
        if (!ready || cleaned || closing) return;
        double now = Now, delay = engine.NextDeadline(now) - now;
        if (engine.CanMove && !preview) delay = Math.Min(delay, 1.0 / 30);
        bool visible = IsVisible && WindowState != WindowState.Minimized;
        if (visible) delay = Math.Min(delay, engine.CanMove && !settings.ReducedMotion && SystemParameters.ClientAreaAnimation ? 1.0 / 30 : 1);
        if (engine.ButtonHeld) delay = Math.Min(delay, .25); // button release fail-safe, no pointer coordinates collected
        if (double.IsPositiveInfinity(delay)) return;
        timer.Interval = TimeSpan.FromSeconds(Math.Clamp(delay, .005, 86400)); timer.Start();
    }
    private void RenderStatus()
    {
        double now = Now;
        var state = engine.State;
        double seconds = state == NeonState.Wow ? Math.Max(0, engine.RecoveryEnd - now) : Math.Max(0, engine.PhaseEnd - now);
        var span = TimeSpan.FromSeconds(seconds);
        string countdown = $"{(int)span.TotalMinutes:00}:{span.Seconds:00}";
        StateLabel.Text = state == NeonState.Stopped ? "Stopped" : $"Neon {state.ToString().ToLowerInvariant()} " + (engine.ButtonHeld ? "· your input" : countdown);
        StateLabel.Foreground = (Brush)FindResource(state == NeonState.Low ? "Blue" : "Green");
        PhaseProgress.Value = engine.Running ? Math.Clamp((engine.PhaseEnd - now) / engine.SampledSeconds, 0, 1) : 0;
        PhaseLabel.Text = state == NeonState.Wow ? $"Your input · {(now - engine.WowStart):F0} s elapsed · scheduled Neon {engine.Phase.ToString().ToLowerInvariant()}" :
            state == NeonState.Low ? "Pointer resting · low resource interval" : state == NeonState.Stopped ? "Press Start to begin a new interval." : $"Sampled {engine.SampledSeconds:F1} s · your input takes priority";
        DiameterLabel.Text = $"{settings.DiameterMm:g} mm · {settings.RevolutionSeconds:g} s";
        DpiLabel.Text = $"{dpi} DPI · {Orbit.DiameterPixels(settings.DiameterMm, dpi, settings.PixelsPerMm):F0} px\n{(settings.PixelsPerMm > 0 ? "calibrated" : "size estimate")}";
        CountLabel.Text = movements.ToString("N0", CultureInfo.CurrentCulture);
        ModeLabel.Text = preview ? "PREVIEW · NO INPUT" : engine.TestMode ? "TEST · SECONDS" : "EDUCATIONAL BETA";
        if (IsVisible && WindowState != WindowState.Minimized)
        {
            if (engine.CanMove && !settings.ReducedMotion && SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast) OrbitArt.Time = now;
            OrbitArt.Low = state == NeonState.Low; OrbitArt.InvalidateVisual();
            CalibrationLine.Width = 100 / VisualTreeHelper.GetDpi(this).DpiScaleX;
        }
        if (tray != null)
        {
            // Absolute deadline stays accurate without periodic updates in hidden low.
            string tooltip = state == NeonState.Low ? $"Neon Orbit · Neon low until {DateTime.Now.AddSeconds(Math.Max(0, engine.PhaseEnd-now)):HH:mm:ss}" : "Neon Orbit · " + StateLabel.Text;
            if (tray.Text != tooltip) tray.Text = tooltip;
        }
    }
    private void ApplyPower()
    {
        if (preview) return;
        try { power.Set(settings.Awake, settings.DisplayAwake); AwakeHint.Text = settings.Awake ? "Windows awake · independent of motion" : "Windows follows normal sleep settings"; }
        catch (Win32Exception ex) { AwakeHint.Text = "Awake request failed: " + ex.Message; }
    }
    private void Persist()
    {
        if (preview) return;
        try { settings.Save(SettingsPath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { NoticeLabel.Text = "Settings not saved: " + ex.Message; }
    }
    private void PowerChanged(object sender, RoutedEventArgs e)
    { if (!ready) return; settings = settings with { Awake = AwakeToggle.IsChecked == true, DisplayAwake = DisplayToggle.IsChecked == true }; ApplyPower(); Persist(); }
    private void LoggingChanged(object sender, RoutedEventArgs e)
    {
        if (!ready) return;
        bool enabled = LogToggle.IsChecked == true;
        try { log.Enable(enabled); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { LogToggle.IsChecked = false; enabled = false; NoticeLabel.Text = ex.Message; }
        settings = settings with { Logging = enabled }; Persist();
        if (enabled) { StopExperiment("Logging enabled; begin a complete report interval"); StartExperiment(); }
    }
    private void ApplyClick(object sender, RoutedEventArgs e)
    {
        try
        {
            static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);
            static Distribution Parse(string text)
            { var v = text.Split('/').Select(x => double.Parse(x.Trim(), CultureInfo.InvariantCulture)).ToArray(); if (v.Length != 4) throw new FormatException("Use mean / sigma / min / max."); return new(v[0],v[1],v[2],v[3]); }
            var next = settings with {
                High = Parse(HighBox.Text), Low = Parse(LowBox.Text), Wow = Parse(WowBox.Text),
                DiameterMm = Number(DiameterBox.Text), RevolutionSeconds = Number(PeriodBox.Text), PixelsPerMm = Number(CalibrationBox.Text),
                MaximumSessionMinutes = int.Parse(MaxBox.Text, CultureInfo.InvariantCulture), Seed = ulong.Parse(SeedBox.Text, CultureInfo.InvariantCulture),
                RandomMode = (RandomMode)RandomChoice.SelectedIndex, ExactReplayMetadata = ReplayToggle.IsChecked == true,
                ReducedMotion = MotionToggle.IsChecked == true, MinimiseOnLaunch = MinimiseToggle.IsChecked == true
            };
            next.Validate(); StopExperiment("Settings changed"); settings = next; Persist(); BuildEngine(TestToggle.IsChecked == true); StartExperiment();
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
        { MessageBox.Show(this, ex.Message, "Check Lab settings"); }
    }
    private void EmergencyStop()
    { StopExperiment("Emergency stop"); power.Dispose(); AwakeToggle.IsChecked = false; settings = settings with { Awake = false }; NoticeLabel.Text = "Emergency stop · motion and Windows Awake released"; }
    private void StartClick(object sender, RoutedEventArgs e) => StartExperiment();
    private void StopClick(object sender, RoutedEventArgs e) => StopExperiment();
    private void CloseClick(object sender, RoutedEventArgs e) => Close();
    private void MinimiseClick(object sender, RoutedEventArgs e) => Minimise();
    private void Minimise() { WindowState = WindowState.Minimized; ShowInTaskbar = false; }
    private void OpenWindow() { ShowInTaskbar = true; Show(); WindowState = WindowState.Normal; Activate(); Schedule(); }
    private void ExportClick(object sender, RoutedEventArgs e) => Export();
    private void Export()
    {
        if (!log.Enabled || log.Events.Count == 0) { if (!closing) MessageBox.Show(this,"No retained session events to export."); return; }
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "HTML report|*.html", FileName = $"Neon-Orbit-{DateTime.Now:yyyyMMdd-HHmmss}.html" };
        if (dialog.ShowDialog(this) != true) return;
        try { log.Export(dialog.FileName, Now, movements, engine.TestMode ? "Accelerated Test Mode" : "Normal Mode"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { MessageBox.Show(this, "Export failed: " + ex.Message); }
    }
    private void OpenLogsClick(object sender, RoutedEventArgs e)
    {
        var directory = System.IO.Path.Combine(dataRoot, "logs"); Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo("explorer.exe", directory) { UseShellExecute = true });
    }
    private void DeleteLogClick(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this,"Delete this session's log permanently and disable logging?", "Delete session log",MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        try { log.Delete(); LogToggle.IsChecked = false; settings = settings with { Logging = false }; Persist(); NoticeLabel.Text = "Current session log deleted; logging disabled."; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { NoticeLabel.Text = ex.Message; }
    }
    private void AboutClick(object sender, RoutedEventArgs e) => MessageBox.Show(this,
        "Neon Orbit — Process Lab v2026\nCopyright © 2026 George Culache (george-gc-v01)\nSoftware: GPL-3.0. The Neon Orbit name and logo are not licensed for use in derivative products.\n\nLocal educational experiment. No clicks, keylogging, screenshots, window-title collection, network calls or stored personal pointer trajectories. Windows Awake is independent of motion.\n\nNormal samples: High 25±5 min (15–50); Low 7.5±2 min (3–15); Wow 8±4 s (1–25). Values are rejected outside bounds.\n\nDPI estimates physical size; use monitor calibration for measured millimetres. No guarantee of Teams or other third-party presence behaviour.", "About Neon Orbit");
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (cleaned) return;
        var wasClosing = closing;
        StopExperiment("App closed"); closing = true; timer.Stop(); minimiseTimer.Stop(); power.Dispose(); observer?.Dispose(); observer = null;
        Record(new(Now,"movement_count","Stopped",movements));
        if (!preview && !wasClosing && log.Enabled && log.Events.Count > 0 && MessageBox.Show(this,"Save a summary of this Neon session?","Neon Orbit",MessageBoxButton.YesNo) == MessageBoxResult.Yes) Export();
        EmergencyCleanup();
    }
    public void EmergencyCleanup()
    {
        if (cleaned) return; cleaned = true;
        timer.Stop(); minimiseTimer.Stop(); power.Dispose(); observer?.Dispose(); observer = null;
        if (handle != 0 && !preview) { Native.UnregisterHotKey(handle,2026); Native.WTSUnRegisterSessionNotification(handle); }
        source?.RemoveHook(WindowMessage);
        if (tray != null) { tray.Visible = false; tray.ContextMenuStrip?.Dispose(); tray.Dispose(); }
        trayIcon?.Dispose(); log?.Dispose();
    }
}
