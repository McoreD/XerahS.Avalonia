using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace XerahS.RegionCapture.Services;

/// <summary>
/// Normalizes coordinates between Win32 device pixels, Avalonia logical pixels, and the virtual screen.
/// </summary>
public class ScreenMappingService
{
    public record ScreenSnapshot(Screen Screen, PixelRect PhysicalBounds, Rect LogicalBounds, double Scaling);

    private readonly IReadOnlyList<ScreenSnapshot> _screens;

    public ScreenMappingService(Screens screens)
    {
        _screens = screens.All.Select(s =>
        {
            var scaling = GetScalingForScreen(s);
            var physical = s.Bounds;
            var logical = new Rect(
                physical.X / scaling,
                physical.Y / scaling,
                physical.Width / scaling,
                physical.Height / scaling);
            return new ScreenSnapshot(s, physical, logical, scaling);
        }).ToList();
    }

    public IReadOnlyList<ScreenSnapshot> Screens => _screens;

    public ScreenSnapshot GetScreenForLogicalPoint(Point logicalPoint)
    {
        return _screens.FirstOrDefault(s => s.LogicalBounds.Contains(logicalPoint))
               ?? _screens.First();
    }

    public PixelRect ToPhysicalBounds(Rect logicalRect, Screen screen)
    {
        var snapshot = _screens.First(s => s.Screen == screen);
        return new PixelRect(
            snapshot.PhysicalBounds.X + (int)Math.Round(logicalRect.X * snapshot.Scaling),
            snapshot.PhysicalBounds.Y + (int)Math.Round(logicalRect.Y * snapshot.Scaling),
            (int)Math.Round(logicalRect.Width * snapshot.Scaling),
            (int)Math.Round(logicalRect.Height * snapshot.Scaling));
    }

    public Rect ToLogicalRect(PixelRect physicalRect, Screen screen)
    {
        var snapshot = _screens.First(s => s.Screen == screen);
        return new Rect(
            (physicalRect.X - snapshot.PhysicalBounds.X) / snapshot.Scaling,
            (physicalRect.Y - snapshot.PhysicalBounds.Y) / snapshot.Scaling,
            physicalRect.Width / snapshot.Scaling,
            physicalRect.Height / snapshot.Scaling);
    }

    public PixelRect LogicalToVirtualPhysical(Rect logicalRect, Screen screen)
    {
        var snapshot = _screens.First(s => s.Screen == screen);
        return new PixelRect(
            snapshot.PhysicalBounds.X + (int)Math.Round(logicalRect.X * snapshot.Scaling),
            snapshot.PhysicalBounds.Y + (int)Math.Round(logicalRect.Y * snapshot.Scaling),
            (int)Math.Round(logicalRect.Width * snapshot.Scaling),
            (int)Math.Round(logicalRect.Height * snapshot.Scaling));
    }

    private static double GetScalingForScreen(Screen screen)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (TryGetDpiForMonitor(screen.Bounds, out var dpi))
            {
                return dpi / 96.0;
            }
        }

        return screen.Scaling;
    }

    private static bool TryGetDpiForMonitor(PixelRect physicalBounds, out double dpi)
    {
        dpi = 96.0;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        try
        {
            var center = new POINT
            {
                X = physicalBounds.X + physicalBounds.Width / 2,
                Y = physicalBounds.Y + physicalBounds.Height / 2
            };

            var monitor = MonitorFromPoint(center, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero &&
                GetDpiForMonitor(monitor, DpiType.MDT_EFFECTIVE_DPI, out var dpiX, out _).Succeeded)
            {
                dpi = dpiX;
                return true;
            }
        }
        catch
        {
            // Fall through to Avalonia scaling.
        }

        return false;
    }

    private enum DpiType
    {
        MDT_EFFECTIVE_DPI = 0
    }

    private enum MonitorOptions : uint
    {
        MONITOR_DEFAULTTONULL = 0x00000000,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002
    }

    [DllImport("Shcore.dll")]
    private static extern HRESULT GetDpiForMonitor(IntPtr hmonitor, DpiType dpiType, out uint dpiX, out uint dpiY);

    [DllImport("User32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, MonitorOptions dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HRESULT
    {
        private readonly int Value;

        public bool Succeeded => Value >= 0;
    }
}
