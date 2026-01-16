#if WINDOWS
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using XerahS.Avalonia.RegionCapture.Models;

namespace XerahS.Avalonia.RegionCapture.Platform.Windows;

/// <summary>
/// Native Windows monitor enumeration using direct P/Invoke for predictable DPI behavior.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class NativeMonitorService
{
    public static IReadOnlyList<MonitorInfo> EnumerateMonitors()
    {
        var monitors = new List<MonitorInfo>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr lParam) =>
        {
            var info = GetMonitorInfoEx(hMonitor);
            if (info is { } monitor)
            {
                monitors.Add(monitor);
            }
            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    private static MonitorInfo? GetMonitorInfoEx(IntPtr hMonitor)
    {
        var info = new MONITORINFOEX();
        info.cbSize = Marshal.SizeOf<MONITORINFOEX>();

        if (!GetMonitorInfo(hMonitor, ref info))
        {
            return null;
        }

        // DPI
        double scaleFactor = 1.0;
        if (GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out var dpiY) == 0)
        {
            scaleFactor = dpiX / 96.0;
        }

        var rcMon = info.rcMonitor;
        var rcWork = info.rcWork;

        var deviceName = info.szDevice.TrimEnd('\0');

        return new MonitorInfo(
            DeviceName: deviceName,
            PhysicalBounds: new PixelRect(rcMon.Left, rcMon.Top, rcMon.Width, rcMon.Height),
            WorkArea: new PixelRect(rcWork.Left, rcWork.Top, rcWork.Width, rcWork.Height),
            ScaleFactor: scaleFactor,
            IsPrimary: (info.dwFlags & 0x1) != 0);
    }

    /// <summary>
    /// Gets the physical cursor position (not affected by DPI virtualization).
    /// </summary>
    public static PixelPoint GetPhysicalCursorPosition()
    {
        if (GetPhysicalCursorPos(out var point))
        {
            return new PixelPoint(point.X, point.Y);
        }

        if (GetCursorPos(out point))
        {
            return new PixelPoint(point.X, point.Y);
        }

        return PixelPoint.Origin;
    }

    #region PInvoke
    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll")]
    private static extern bool GetPhysicalCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    private enum MONITOR_DPI_TYPE
    {
        MDT_EFFECTIVE_DPI = 0
    }
    #endregion
}
#endif
