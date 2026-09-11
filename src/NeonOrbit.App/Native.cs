// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NeonOrbit.App;

internal static class Native
{
    internal const nuint Marker = 0x4E454F4E;
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] internal struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseHook { public Point Pt; public uint MouseData, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseInput { public int X, Y; public uint Data, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Explicit)] internal struct InputUnion { [FieldOffset(0)] public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] internal struct Input { public uint Type; public InputUnion Data; }
    internal delegate nint HookProc(int code, nint message, nint data);
    [DllImport("user32.dll", SetLastError = true)] internal static extern nint SetWindowsHookEx(int id, HookProc callback, nint module, uint thread);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern nint GetModuleHandle(string? name);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] internal static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] internal static extern nint MonitorFromPoint(Point point, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("shcore.dll")] internal static extern int GetDpiForMonitor(nint monitor, int type, out uint x, out uint y);
    [DllImport("user32.dll")] internal static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(nint window, int id);
    [DllImport("user32.dll")] internal static extern bool DestroyIcon(nint icon);
    [DllImport("wtsapi32.dll", SetLastError = true)] internal static extern bool WTSRegisterSessionNotification(nint window, uint flags);
    [DllImport("wtsapi32.dll")] internal static extern bool WTSUnRegisterSessionNotification(nint window);

    internal static bool ButtonHeld => new[] { 1, 2, 4, 5, 6 }.Any(k => (GetAsyncKeyState(k) & 0x8000) != 0);
    internal static void Move(PointD point)
    {
        int x = GetSystemMetrics(76), y = GetSystemMetrics(77), width = GetSystemMetrics(78), height = GetSystemMetrics(79);
        if (width <= 1 || height <= 1) throw new InvalidOperationException("Desktop dimensions are unavailable.");
        var input = new Input { Type = 0, Data = new InputUnion { Mouse = new MouseInput {
            X = (int)Math.Round((point.X - x) * 65535 / (width - 1)),
            Y = (int)Math.Round((point.Y - y) * 65535 / (height - 1)),
            Flags = 0x8000 | 0x4000 | 1, Extra = Marker
        } } };
        if (SendInput(1, [input], Marshal.SizeOf<Input>()) != 1)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows did not accept cursor movement.");
    }
}

internal sealed class InputObserver : IDisposable
{
    private readonly Native.HookProc callback;
    private readonly nint handle;
    private readonly Action<bool> input;
    internal InputObserver(Action<bool> input)
    {
        this.input = input; callback = Observe;
        handle = Native.SetWindowsHookEx(14, callback, Native.GetModuleHandle(null), 0);
        if (handle == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "Input observer unavailable; motion disabled.");
    }
    private nint Observe(int code, nint message, nint data)
    {
        if (code >= 0)
        {
            var info = Marshal.PtrToStructure<Native.MouseHook>(data);
            if (info.Extra != Native.Marker)
            {
                // Foreign synthetic input also pauses us. Never fight another accessibility utility.
                var msg = (int)message;
                bool down = msg is 0x201 or 0x204 or 0x207 or 0x20B;
                int released = msg switch { 0x202 => 1, 0x205 => 2, 0x208 => 4,
                    0x20C => (info.MouseData >> 16) == 1 ? 5 : 6, _ => 0 };
                bool othersHeld = new[] {1,2,4,5,6}.Any(k => k != released && (Native.GetAsyncKeyState(k) & 0x8000) != 0);
                input(down || othersHeld);
            }
        }
        return Native.CallNextHookEx(handle, code, message, data);
    }
    public void Dispose() { if (handle != 0) Native.UnhookWindowsHookEx(handle); }
}

internal sealed class PowerService : IDisposable
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Reason { public uint Version, Flags; [MarshalAs(UnmanagedType.LPWStr)] public string Text; }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern nint PowerCreateRequest(ref Reason reason);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool PowerSetRequest(nint request, int type);
    [DllImport("kernel32.dll")] private static extern bool PowerClearRequest(nint request, int type);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    private nint handle;
    private bool system, display;
    internal void Set(bool awake, bool keepDisplay)
    {
        if (awake == system && (awake && keepDisplay) == display) return;
        Dispose();
        if (!awake) return;
        var reason = new Reason { Flags = 1, Text = "Neon Orbit — user-enabled educational Windows Awake" };
        handle = PowerCreateRequest(ref reason);
        if (handle == 0 || handle == -1) { handle = 0; throw new Win32Exception(Marshal.GetLastWin32Error()); }
        try
        {
            if (!PowerSetRequest(handle, 1)) throw new Win32Exception(Marshal.GetLastWin32Error());
            system = true;
            if (keepDisplay)
            { if (!PowerSetRequest(handle, 0)) throw new Win32Exception(Marshal.GetLastWin32Error()); display = true; }
        }
        catch { Dispose(); throw; }
    }
    public void Dispose()
    {
        if (handle != 0)
        { if (display) PowerClearRequest(handle, 0); if (system) PowerClearRequest(handle, 1); CloseHandle(handle); }
        handle = 0; system = display = false;
    }
}
