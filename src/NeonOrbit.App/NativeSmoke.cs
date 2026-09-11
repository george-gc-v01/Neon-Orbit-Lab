// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace NeonOrbit.App;

internal static class NativeSmoke
{
    internal static int Run()
    {
        Directory.CreateDirectory("artifacts");
        List<string> lines = [];
        try
        {
            if (IntPtr.Size != 8 || Marshal.SizeOf<Native.Input>() != 40 || Marshal.SizeOf<Native.MouseHook>() != 32)
                throw new InvalidOperationException("Native x64 ABI mismatch.");
            lines.Add("PASS: x64 INPUT and MSLLHOOKSTRUCT sizes.");
            using var window = new HwndSource(new HwndSourceParameters("Neon Orbit native test") { Width = 1, Height = 1, WindowStyle = 0 });
            if (!Native.WTSRegisterSessionNotification(window.Handle,0)) throw new InvalidOperationException("WTS registration failed.");
            Native.WTSUnRegisterSessionNotification(window.Handle);
            lines.Add("PASS: session registration and release.");
            if (!Native.RegisterHotKey(window.Handle,2026,0x4003,0x7B)) throw new InvalidOperationException("Hotkey registration failed.");
            Native.UnregisterHotKey(window.Handle,2026);
            lines.Add("PASS: emergency hotkey registration and release.");
            using (var observer = new InputObserver(_=>{})) { }
            lines.Add("PASS: mouse observer installation and release; no generated movement.");
            using (var request = new PowerService()) { request.Set(true,false); request.Set(true,true); request.Set(false,false); }
            lines.Add("PASS: system/display power requests acquired and released.");
            using var process = Process.GetCurrentProcess(); process.Refresh(); var initial = process.HandleCount;
            for(int i=0;i<100;i++) { using var request = new PowerService(); request.Set(true,false); request.Set(false,false); }
            process.Refresh(); var delta = process.HandleCount-initial;
            if (delta > 8) throw new InvalidOperationException($"Power handle growth: {delta}");
            lines.Add($"PASS: 100 power-request lifecycles; handle delta={delta}.");
            File.WriteAllLines("artifacts/native-smoke.txt",lines); return 0;
        }
        catch(Exception ex) { lines.Add("FAIL: " + ex); File.WriteAllLines("artifacts/native-smoke.txt",lines); return 1; }
    }
}
