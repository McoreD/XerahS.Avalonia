#if WINDOWS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using XerahS.Avalonia.RegionCapture.Models;

namespace XerahS.Avalonia.RegionCapture.Platform.Windows;

/// <summary>
/// Native window enumeration and geometry helpers using direct P/Invoke.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class NativeWindowService
{
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    public static IReadOnlyList<WindowInfo> EnumerateWindows()
    {
        var results = new List<WindowInfo>();
        EnumWindows((hWnd, lParam) =>
        {
            if (TryGetWindowInfo(hWnd, results.Count, out var info))
            {
                results.Add(info);
            }
            return true;
        }, IntPtr.Zero);
        return results;
    }

    public static IReadOnlyList<WindowInfo> EnumerateVisibleWindows() => EnumerateWindows();

    public static WindowInfo? GetWindowAtPoint(PixelPoint physicalPoint)
    {
        // Enumerate and pick first containing the point
        foreach (var win in EnumerateWindows())
        {
            if (win.SnapBounds.Contains(physicalPoint.X, physicalPoint.Y))
            {
                return win;
            }
        }
        return null;
    }

    private static bool TryGetWindowInfo(IntPtr hWnd, int zOrder, out WindowInfo info)
    {
        info = null!;

        var style = GetWindowLongAuto(hWnd, GWL_STYLE);
        if ((style & WS_VISIBLE) == 0)
            return false;

        var exStyle = GetWindowLongAuto(hWnd, GWL_EXSTYLE);
        if ((exStyle & WS_EX_TOOLWINDOW) != 0)
            return false;

        if (!IsWindowVisible(hWnd))
            return false;

        var title = GetWindowTitle(hWnd);
        var className = GetClassName(hWnd);

        var bounds = GetWindowRect(hWnd);
        if (bounds is null || bounds.Value.Width <= 0 || bounds.Value.Height <= 0)
            return false;

        var visualBounds = GetExtendedFrameBounds(hWnd) ?? bounds.Value;
        var isMinimized = IsIconic(hWnd);

        info = new WindowInfo(
            Handle: hWnd,
            Title: title,
            ClassName: className,
            Bounds: bounds.Value,
            VisualBounds: visualBounds,
            IsMinimized: isMinimized,
            ZOrder: zOrder);
        return true;
    }

    private static PixelRect? GetExtendedFrameBounds(IntPtr hWnd)
    {
        if (DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rect, (uint)Marshal.SizeOf<RECT>()) != 0)
        {
            return null;
        }

        return new PixelRect(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    private static PixelRect? GetWindowRect(IntPtr hWnd)
    {
        if (!GetWindowRect(hWnd, out RECT rect))
            return null;
        return new PixelRect(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    private static nint GetWindowLongAuto(IntPtr hWnd, int index)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr(hWnd, index)
            : GetWindowLong(hWnd, index);
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length == 0)
            return string.Empty;

        var sb = new StringBuilder(length + 1);
        _ = GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string GetClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        _ = GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    #region PInvoke
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, uint cbAttribute);

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
    #endregion
}
#endif
