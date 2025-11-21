using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System;
using System.Timers;
using VideoVault.Controls;
using VideoVault.Services;

namespace VideoVault.Views;

public partial class FullscreenVideoWindow : Window
{
    private readonly LoggingService _logger;
    private readonly DispatcherTimer _hideControlsTimer;
    private VideoPlayerControl? _videoPlayer;

    public FullscreenVideoWindow()
    {
        InitializeComponent();
        _logger = LoggingService.Instance;

        // Set up timer - not needed as VideoPlayerControl handles its own controls
        _hideControlsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        // Add key handler for ESC
        this.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };

        // Add double-click handler to exit fullscreen
        if (VideoArea != null)
        {
            VideoArea.DoubleTapped += (s, e) => Close();
        }
    }

    /// <summary>
    /// Set the video player control
    /// </summary>
    public void SetVideoPlayer(VideoPlayerControl player, string videoName)
    {
        if (player == null) return;

        _videoPlayer = player;

        // Remove from current parent
        if (player.Parent is Panel parentPanel)
        {
            parentPanel.Children.Remove(player);
        }

        // Add to fullscreen window
        if (VideoArea != null && VideoArea is Border border)
        {
            border.Child = player;
        }

        _logger.LogInfo($"Fullscreen window pointer moved - showing controls");
        _logger.LogInfo("Calling ShowControlsWithAutoHide");

        // Tell the video player to show its controls
        player.ShowControlsWithAutoHide();
    }

    /// <summary>
    /// Return the video player control
    /// </summary>
    public VideoPlayerControl? GetVideoPlayer()
    {
        if (VideoArea is Border border)
        {
            var player = border.Child as VideoPlayerControl;
            border.Child = null;
            return player;
        }
        return null;
    }

    /// <summary>
    /// Handle pointer moved to show/hide controls
    /// </summary>
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        // VideoPlayerControl now handles its own controls display
        // Just show cursor
        Cursor = new Cursor(StandardCursorType.Arrow);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        _hideControlsTimer?.Stop();
    }
}
