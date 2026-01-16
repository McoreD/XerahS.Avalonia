using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using XerahS.RegionCapture;

namespace RegionCaptureHost;

public partial class MainWindow : Window
{
    private readonly RegionCaptureController _controller;
    private TextBlock? _statusText;
    private Button? _startButton;

    public MainWindow()
    {
        InitializeComponent();
        _controller = new RegionCaptureController(Screens);
        _statusText = this.FindControl<TextBlock>("StatusText");
        _startButton = this.FindControl<Button>("StartCaptureButton");
        if (_startButton is not null)
        {
            _startButton.Click += OnStartCapture;
        }
    }

    private async void OnStartCapture(object? sender, RoutedEventArgs e)
    {
        if (_startButton is not null)
        {
            _startButton.IsEnabled = false;
        }

        SetStatus("Capturing... Press Esc to cancel.");
        try
        {
            var region = await _controller.StartAsync();
            if (region is { } rect)
            {
                SetStatus($"Captured: ({rect.X},{rect.Y}) {rect.Width}x{rect.Height}px");
            }
            else
            {
                SetStatus("Capture canceled.");
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            if (_startButton is not null)
            {
                _startButton.IsEnabled = true;
            }
        }
    }

    private void SetStatus(string text)
    {
        if (_statusText is not null)
        {
            _statusText.Text = text;
        }
    }
}
