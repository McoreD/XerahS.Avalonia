using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using XerahS.RegionCapture.Services;
using XerahS.RegionCapture.ViewModels;

namespace XerahS.RegionCapture;

/// <summary>
/// Manages the lifecycle of per-monitor overlay windows and returns the selected physical region.
/// </summary>
public class RegionCaptureController
{
    private readonly Screens _screens;
    private readonly ScreenMappingService _mappingService;
    private readonly List<Views.RegionCaptureOverlayWindow> _overlays = new();
    private TaskCompletionSource<PixelRect?>? _selectionTcs;

    public RegionCaptureController(Screens screens)
    {
        _screens = screens;
        _mappingService = new ScreenMappingService(screens);
    }

    public Task<PixelRect?> StartAsync()
    {
        _selectionTcs = new TaskCompletionSource<PixelRect?>();
        foreach (var screenSnapshot in _mappingService.Screens)
        {
            var viewModel = new RegionCaptureViewModelForScreen(screenSnapshot.Screen);
            var window = new Views.RegionCaptureOverlayWindow(screenSnapshot.Screen, _mappingService, viewModel.OverlayViewModel);
            window.SelectionConfirmed += OnSelectionConfirmed;
            window.SelectionCanceled += OnSelectionCanceled;
            _overlays.Add(window);
            window.Show();
        }

        return _selectionTcs.Task;
    }

    private void OnSelectionCanceled(object? sender, EventArgs e)
    {
        _selectionTcs?.TrySetResult(null);
        CloseAll();
    }

    private void OnSelectionConfirmed(object? sender, PixelRect e)
    {
        _selectionTcs?.TrySetResult(e);
        CloseAll();
    }

    private void CloseAll()
    {
        foreach (var overlay in _overlays)
        {
            overlay.SelectionConfirmed -= OnSelectionConfirmed;
            overlay.SelectionCanceled -= OnSelectionCanceled;
            overlay.Close();
        }

        _overlays.Clear();
    }
}

internal sealed class RegionCaptureViewModelForScreen
{
    public RegionCaptureOverlayViewModel OverlayViewModel { get; }
    public Screen Screen { get; }

    public RegionCaptureViewModelForScreen(Screen screen)
    {
        Screen = screen;
        OverlayViewModel = new RegionCaptureOverlayViewModel();
    }
}
