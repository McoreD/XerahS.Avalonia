using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Platform;
using XerahS.RegionCapture.Models;
using XerahS.RegionCapture.Services;
using XerahS.RegionCapture.ViewModels;

namespace XerahS.RegionCapture.Views;

public partial class RegionCaptureOverlayWindow : Window
{
    private readonly Screen _screen;
    private readonly ScreenMappingService _screenMapping;
    private readonly Win32WindowSnappingService _snappingService = new();
    private readonly double _scaling;
    private Point? _dragAnchor;
    public event EventHandler<PixelRect>? SelectionConfirmed;
    public event EventHandler? SelectionCanceled;

    private RegionCaptureOverlayViewModel ViewModel => (RegionCaptureOverlayViewModel)DataContext!;
    private Line? _crosshairHorizontal;
    private Line? _crosshairVertical;

    // Parameterless constructor exists for XAML loader; not intended for runtime use.
    public RegionCaptureOverlayWindow()
    {
        throw new NotSupportedException("Use the Screen + ScreenMappingService constructor for RegionCaptureOverlayWindow.");
    }

    public RegionCaptureOverlayWindow(Screen screen, ScreenMappingService screenMapping, RegionCaptureOverlayViewModel? viewModel = null)
    {
        InitializeComponent();
        _screen = screen;
        _screenMapping = screenMapping;
        _scaling = screen.Scaling;
        DataContext = viewModel ?? new RegionCaptureOverlayViewModel();
        Position = _screen.Bounds.Position;
        Width = _screen.Bounds.Width / _scaling;
        Height = _screen.Bounds.Height / _scaling;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        PointerMoved += OnPointerMoved;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        KeyDown += OnKeyDown;
        PropertyChanged += OnWindowPropertyChanged;
        Opened += (_, _) => ViewModel.OverlaySize = ClientSize;
        _crosshairHorizontal = this.FindControl<Line>("CrosshairHorizontal");
        _crosshairVertical = this.FindControl<Line>("CrosshairVertical");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragAnchor = e.GetPosition(this);
        ViewModel.State = RegionCaptureState.DraggingSelection;
        ViewModel.SelectionRect = default;
        ViewModel.HoverRect = null;
        e.Pointer.Capture(this);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragAnchor.HasValue)
        {
            var finalRect = _screenMapping.LogicalToVirtualPhysical(ViewModel.SelectionRect, _screen);
            SelectionConfirmed?.Invoke(this, finalRect);
        }

        _dragAnchor = null;
        e.Pointer.Capture(null);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(this);
        UpdateCrosshair(position);

        if (_dragAnchor.HasValue)
        {
            ViewModel.SetSelectionFromDrag(_dragAnchor.Value, position);
            return;
        }

        if (_snappingService.TryGetWindowUnderCursor(out var windowRect))
        {
            var logicalRect = _screenMapping.ToLogicalRect(windowRect, _screen);
            ViewModel.HoverRect = logicalRect;
            ViewModel.State = RegionCaptureState.HoveringWindow;
        }
        else
        {
            ViewModel.HoverRect = null;
            ViewModel.State = RegionCaptureState.Idle;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        const double arrowNudge = 1.0;
        switch (e.Key)
        {
            case Key.Escape:
                SelectionCanceled?.Invoke(this, EventArgs.Empty);
                Close();
                break;
            case Key.Enter:
                var finalRect = _screenMapping.LogicalToVirtualPhysical(ViewModel.SelectionRect, _screen);
                SelectionConfirmed?.Invoke(this, finalRect);
                break;
            case Key.Left:
                ViewModel.NudgeSelection(new Vector(-arrowNudge, 0));
                break;
            case Key.Right:
                ViewModel.NudgeSelection(new Vector(arrowNudge, 0));
                break;
            case Key.Up:
                ViewModel.NudgeSelection(new Vector(0, -arrowNudge));
                break;
            case Key.Down:
                ViewModel.NudgeSelection(new Vector(0, arrowNudge));
                break;
        }
    }

    private void UpdateCrosshair(Point position)
    {
        if (_crosshairHorizontal is { } horiz)
        {
            horiz.StartPoint = new Point(0, position.Y);
            horiz.EndPoint = new Point(ClientSize.Width, position.Y);
        }

        if (_crosshairVertical is { } vert)
        {
            vert.StartPoint = new Point(position.X, 0);
            vert.EndPoint = new Point(position.X, ClientSize.Height);
        }
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ClientSizeProperty)
        {
            ViewModel.OverlaySize = ClientSize;
        }
    }
}
