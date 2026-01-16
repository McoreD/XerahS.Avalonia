using System;
using System.Runtime.InteropServices;
using Avalonia;

namespace XerahS.RegionCapture.Services;

/// <summary>
/// Detects native windows under the cursor and provides their physical bounds for snapping.
/// </summary>
public class Win32WindowSnappingService
{
    public bool TryGetWindowUnderCursor(out PixelRect rect)
    {
        rect = default;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        if (!GetCursorPos(out var point))
        {
            return false;
        }

        var hwnd = WindowFromPoint(point);
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        if (DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.ExtendedFrameBounds, out RECT frameRect) != 0)
        {
            if (!GetWindowRect(hwnd, out frameRect))
            {
                return false;
            }
        }

        rect = new PixelRect(frameRect.Left, frameRect.Top, frameRect.Right - frameRect.Left, frameRect.Bottom - frameRect.Top);
        return true;
    }

    #region Win32 Interop

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private enum DWMWINDOWATTRIBUTE
    {
        ExtendedFrameBounds = 9
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out RECT pvAttribute, int cbAttribute = 16);

    #endregion
}
