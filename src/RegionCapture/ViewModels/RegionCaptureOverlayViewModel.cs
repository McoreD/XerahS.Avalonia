using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using XerahS.RegionCapture.Models;

namespace XerahS.RegionCapture.ViewModels;

public class RegionCaptureOverlayViewModel : INotifyPropertyChanged
{
    private Rect _selectionRect;
    private Rect? _hoverRect;
    private RegionCaptureState _state = RegionCaptureState.Idle;
    private StreamGeometry? _overlayGeometry;
    private Size _overlaySize;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Rect SelectionRect
    {
        get => _selectionRect;
        set
        {
            if (_selectionRect != value)
            {
                _selectionRect = value;
                OnPropertyChanged();
                UpdateOverlayGeometry();
            }
        }
    }

    public Rect? HoverRect
    {
        get => _hoverRect;
        set
        {
            if (_hoverRect != value)
            {
                _hoverRect = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasHover));
            }
        }
    }

    public bool HasHover => _hoverRect.HasValue;

    public RegionCaptureState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged();
            }
        }
    }

    public StreamGeometry? OverlayGeometry
    {
        get => _overlayGeometry;
        private set
        {
            if (_overlayGeometry != value)
            {
                _overlayGeometry = value;
                OnPropertyChanged();
            }
        }
    }

    public Size OverlaySize
    {
        get => _overlaySize;
        set
        {
            if (_overlaySize != value)
            {
                _overlaySize = value;
                OnPropertyChanged();
                UpdateOverlayGeometry();
            }
        }
    }

    public void NudgeSelection(Vector delta)
    {
        SelectionRect = new Rect(SelectionRect.Position + delta, SelectionRect.Size);
    }

    public void SetSelectionFromDrag(Point anchor, Point current)
    {
        var x1 = Math.Min(anchor.X, current.X);
        var y1 = Math.Min(anchor.Y, current.Y);
        var x2 = Math.Max(anchor.X, current.X);
        var y2 = Math.Max(anchor.Y, current.Y);
        SelectionRect = new Rect(new Point(x1, y1), new Point(x2, y2));
    }

    public void Reset()
    {
        SelectionRect = default;
        HoverRect = null;
        State = RegionCaptureState.Idle;
    }

    private void UpdateOverlayGeometry()
    {
        if (_overlaySize.Width <= 0 || _overlaySize.Height <= 0)
        {
            OverlayGeometry = null;
            return;
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.SetFillRule(FillRule.EvenOdd);
            ctx.BeginFigure(new Point(0, 0), true);
            ctx.LineTo(new Point(_overlaySize.Width, 0));
            ctx.LineTo(new Point(_overlaySize.Width, _overlaySize.Height));
            ctx.LineTo(new Point(0, _overlaySize.Height));
            ctx.EndFigure(true);

            if (HasSelection(SelectionRect))
            {
                ctx.BeginFigure(SelectionRect.TopLeft, true);
                ctx.LineTo(SelectionRect.TopRight);
                ctx.LineTo(SelectionRect.BottomRight);
                ctx.LineTo(SelectionRect.BottomLeft);
                ctx.EndFigure(true);
            }
        }

        OverlayGeometry = geometry;
    }

    private static bool HasSelection(Rect rect) => rect.Width > 0 && rect.Height > 0;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
