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
    /// Set video player control for fullscreen display
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

        // Show controls initially
        player.ShowControlsWithAutoHide();
    }

    /// <summary>
    /// Return video player control to normal window
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
    /// Show cursor when pointer moves
    /// </summary>
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        Cursor = new Cursor(StandardCursorType.Arrow);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        _hideControlsTimer?.Stop();
    }
}
