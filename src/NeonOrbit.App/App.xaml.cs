// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
using System.Threading;

namespace NeonOrbit.App;

public partial class App : System.Windows.Application
{
    private Mutex? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--native-smoke")) { Shutdown(NativeSmoke.Run()); return; }
        bool preview = e.Args.Contains("--preview") || e.Args.Contains("--smoke-test");
        instance = new Mutex(true, "Local\\NeonOrbitProcess.v2026" + (preview ? ".Preview" : ""), out var created);
        if (!created) { MessageBox.Show("Neon Orbit is already running. Open it from the N tray icon."); Shutdown(); return; }
        if (SystemParameters.HighContrast)
        {
            Resources["Surface"] = SystemColors.WindowBrush; Resources["Panel"] = SystemColors.ControlBrush;
            Resources["Ink"] = SystemColors.WindowTextBrush; Resources["Muted"] = SystemColors.WindowTextBrush;
            Resources["Green"] = SystemColors.HighlightBrush; Resources["Blue"] = SystemColors.HighlightBrush;
        }
        DispatcherUnhandledException += (_, args) =>
        {
            (MainWindow as MainWindow)?.EmergencyCleanup();
            MessageBox.Show("Neon Orbit stopped after an error. " + args.Exception.Message, "Neon Orbit");
            args.Handled = true; Shutdown(1);
        };
        var window = new MainWindow(preview, e.Args.Contains("--smoke-test"));
        MainWindow = window; window.Show();
    }
    protected override void OnExit(ExitEventArgs e)
    { (MainWindow as MainWindow)?.EmergencyCleanup(); instance?.Dispose(); base.OnExit(e); }
}
