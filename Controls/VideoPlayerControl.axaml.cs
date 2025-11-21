using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using VideoVault.Services;

namespace VideoVault.Controls;

public partial class VideoPlayerControl : UserControl
{
    private VideoPlayerService? _playerService;
    private Timer? _updateTimer;
    private bool _isUserSeeking = false;
    private bool _isTimerUpdatingSlider = false;
    private bool _isMuted = false;
    private int _volumeBeforeMute = 100;
    private bool _isFullscreen = false;
    private readonly LoggingService _logger;
    private IntPtr _videoHandle = IntPtr.Zero;
    private bool _handleReady = false;
    private VlcNativeControlHost? _vlcHost;
    private bool _isInitializing = false;
    private string? _pendingVideoPath = null;
    private string? _currentVideoName = null;
    private Timer? _controlsHideTimer;
    private bool _isInFullscreenMode = false;

    /// <summary>
    /// Event fired when video starts playing
    /// </summary>
    public event Action<string>? VideoStarted;

    public VideoPlayerControl()
    {
        InitializeComponent();

        _logger = LoggingService.Instance;

        // Set up update timer for UI updates
        _updateTimer = new Timer(100);
        _updateTimer.Elapsed += UpdateTimer_Elapsed;

        // Set up controls auto-hide timer (3 seconds)
        _controlsHideTimer = new Timer(3000);
        _controlsHideTimer.Elapsed += ControlsHideTimer_Elapsed;
        _controlsHideTimer.AutoReset = false;

        // Add double-click handler for fullscreen
        if (VideoContainer != null)
        {
            VideoContainer.DoubleTapped += VideoContainer_DoubleTapped;
        }

        // Add seek slider interaction handlers
        if (ProgressSlider != null)
        {
            ProgressSlider.PointerPressed += ProgressSlider_PointerPressed;
            ProgressSlider.PointerReleased += ProgressSlider_PointerReleased;
            ProgressSlider.PointerCaptureLost += ProgressSlider_PointerCaptureLost;
        }

        // Replace the NativeControlHost with our custom VLC host
        if (VideoHost != null)
        {
            _vlcHost = new VlcNativeControlHost();
            var parent = VideoHost.Parent;
            if (parent is Panel panel)
            {
                var index = panel.Children.IndexOf(VideoHost);
                panel.Children.RemoveAt(index);
                panel.Children.Insert(index, _vlcHost);
                _vlcHost.Name = "VideoHost";
                _vlcHost.IsVisible = true;
                // Preserve alignment to ensure video fills the space
                _vlcHost.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                _vlcHost.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            }

            _vlcHost.HandleCreated += OnHandleCreated;
        }
    }

    /// <summary>
    /// Handle when native control handle is created
    /// </summary>
    private void OnHandleCreated(IntPtr handle)
    {
        _videoHandle = handle;
        _handleReady = true;

        // If player service already exists, set the handle now
        if (_playerService?.MediaPlayer != null)
        {
            _playerService.MediaPlayer.Hwnd = _videoHandle;
        }
    }


    /// <summary>
    /// Event fired when fullscreen is toggled
    /// </summary>
    public event Action<bool>? FullscreenToggled;

    /// <summary>
    /// Property to check if video is loaded
    /// </summary>
    public bool IsVideoLoaded { get; private set; }

    /// <summary>
    /// Initialize the video player service
    /// </summary>
    public void InitializePlayer()
    {
        if (_isInitializing || _playerService != null)
        {
            return;
        }

        try
        {
            _isInitializing = true;

            _playerService = new VideoPlayerService();
            _playerService.Initialize();

            // Set the window handle if we have it
            if (_playerService.MediaPlayer != null && _handleReady && _videoHandle != IntPtr.Zero)
            {
                _playerService.MediaPlayer.Hwnd = _videoHandle;
            }

            // Subscribe to player events
            if (_playerService != null)
            {
                _playerService.EndReached += OnVideoEnded;
            }

            // Load pending video if there is one
            if (_pendingVideoPath != null)
            {
                var pendingPath = _pendingVideoPath;
                _pendingVideoPath = null;
                LoadVideo(pendingPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize video player", ex);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    /// <summary>
    /// Load and play a video file
    /// </summary>
    public void LoadAndPlay(string filePath)
    {
        LoadVideo(filePath);

        // Start playing automatically
        if (IsVideoLoaded && _playerService != null && !_playerService.IsPlaying)
        {
            _playerService.Play();
            UpdatePlayPauseButton();

            // Fire the VideoStarted event
            if (_currentVideoName != null)
            {
                VideoStarted?.Invoke(_currentVideoName);
            }
        }
    }

    /// <summary>
    /// Load a video file (does NOT auto-play)
    /// </summary>
    public void LoadVideo(string filePath)
    {
        try
        {
            // If initialization is in progress, queue this video
            if (_isInitializing)
            {
                _pendingVideoPath = filePath;
                return;
            }

            // Initialize player if not already initialized
            if (_playerService == null)
            {
                _pendingVideoPath = filePath;
                InitializePlayer();
                return;
            }

            // Verify MediaPlayer is ready
            if (_playerService.MediaPlayer == null)
            {
                _logger.LogError("MediaPlayer is null after initialization");
                return;
            }

            // Ensure handle is set before loading media
            if (_handleReady && _videoHandle != IntPtr.Zero)
            {
                _playerService.MediaPlayer.Hwnd = _videoHandle;
            }

            // Load the video
            _playerService.LoadVideo(filePath);

            // Store the video name
            _currentVideoName = System.IO.Path.GetFileName(filePath);

            IsVideoLoaded = true;

            if (NoVideoText != null)
            {
                NoVideoText.IsVisible = false;
            }

            _updateTimer?.Start();
            UpdatePlayPauseButton();

            // Fire the VideoStarted event immediately when video is loaded
            // This ensures the name is displayed even if auto-play happens
            if (_currentVideoName != null)
            {
                VideoStarted?.Invoke(_currentVideoName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load video", ex);
        }
    }

    /// <summary>
    /// Handle play/pause button click
    /// </summary>
    private void PlayPauseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_playerService == null)
        {
            _logger.LogError("Player service null");
            return;
        }

        // If no video is loaded, try to load the selected video from parent window
        if (!IsVideoLoaded)
        {
            // Try to get selected video from MainWindow by traversing visual tree
            var parent = this.Parent;
            Views.MainWindow? mainWindow = null;

            while (parent != null)
            {
                if (parent is Views.MainWindow window)
                {
                    mainWindow = window;
                    break;
                }
                parent = (parent as Control)?.Parent;
            }

            if (mainWindow?.DataContext is ViewModels.MainWindowViewModel viewModel && viewModel.SelectedVideo != null)
            {
                var filePath = viewModel.SelectedVideo.FilePath;
                if (System.IO.File.Exists(filePath))
                {
                    LoadAndPlay(filePath);
                    return;
                }
            }

            _logger.LogError("No video loaded or selected");
            return;
        }

        // Verify handle is set before playing
        if (_playerService.MediaPlayer != null && _handleReady && _videoHandle != IntPtr.Zero)
        {
            _playerService.MediaPlayer.Hwnd = _videoHandle;
        }

        bool wasPlaying = _playerService.IsPlaying;
        _playerService.TogglePlayPause();
        UpdatePlayPauseButton();

        // If we just started playing, fire the VideoStarted event
        if (!wasPlaying && _playerService.IsPlaying && _currentVideoName != null)
        {
            VideoStarted?.Invoke(_currentVideoName);
        }
    }

    /// <summary>
    /// Update play/pause button text
    /// </summary>
    private void UpdatePlayPauseButton()
    {
        if (PlayPauseButton.Content is TextBlock textBlock && _playerService != null)
        {
            textBlock.Text = _playerService.IsPlaying ? "⏸" : "▶";
        }
    }

    /// <summary>
    /// Handle progress slider pointer pressed
    /// </summary>
    private void ProgressSlider_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isUserSeeking = true;
    }

    /// <summary>
    /// Handle progress slider pointer released
    /// </summary>
    private void ProgressSlider_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isUserSeeking)
        {
            _isUserSeeking = false;

            // Seek to the new position when user releases
            if (_playerService != null && ProgressSlider != null)
            {
                float position = (float)(ProgressSlider.Value / 100.0);
                _playerService.SetPosition(position);
            }
        }
    }

    /// <summary>
    /// Handle progress slider pointer capture lost
    /// </summary>
    private void ProgressSlider_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isUserSeeking)
        {
            _isUserSeeking = false;

            // Seek to the new position when pointer is lost
            if (_playerService != null && ProgressSlider != null)
            {
                float position = (float)(ProgressSlider.Value / 100.0);
                _playerService.SetPosition(position);
            }
        }
    }

    /// <summary>
    /// Handle progress slider value changed
    /// </summary>
    private void ProgressSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        // If timer is updating, ignore
        if (_isTimerUpdatingSlider)
        {
            return;
        }

        // Otherwise, this is a user interaction - perform seek immediately
        if (_playerService != null && ProgressSlider != null)
        {
            float position = (float)(e.NewValue / 100.0);
            _playerService.SetPosition(position);

            // Set flag to prevent timer from updating slider briefly
            _isUserSeeking = true;
            Task.Delay(500).ContinueWith(_ => _isUserSeeking = false);
        }
    }

    /// <summary>
    /// Handle volume slider value changed
    /// </summary>
    private void VolumeSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_playerService != null)
        {
            int volume = (int)e.NewValue;
            _playerService.SetVolume(volume);
            UpdateVolumeButton();
        }
    }

    /// <summary>
    /// Handle volume button click
    /// </summary>
    private void VolumeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_playerService == null)
        {
            return;
        }

        if (_isMuted)
        {
            // Unmute
            _isMuted = false;
            _playerService.SetVolume(_volumeBeforeMute);
            VolumeSlider.Value = _volumeBeforeMute;
        }
        else
        {
            // Mute
            _volumeBeforeMute = _playerService.GetVolume();
            _isMuted = true;
            _playerService.SetVolume(0);
            VolumeSlider.Value = 0;
        }

        UpdateVolumeButton();
    }

    /// <summary>
    /// Update volume button icon
    /// </summary>
    private void UpdateVolumeButton()
    {
        if (VolumeButton.Content is TextBlock textBlock && VolumeSlider != null)
        {
            int volume = (int)VolumeSlider.Value;
            textBlock.Text = volume == 0 ? "🔇" : (volume < 50 ? "🔉" : "🔊");
        }
    }

    /// <summary>
    /// Handle fullscreen button click
    /// </summary>
    private void FullscreenButton_Click(object? sender, RoutedEventArgs e)
    {
        ToggleFullscreen();
    }

    /// <summary>
    /// Handle video container double tap for fullscreen
    /// </summary>
    private void VideoContainer_DoubleTapped(object? sender, TappedEventArgs e)
    {
        ToggleFullscreen();
    }

    /// <summary>
    /// Toggle fullscreen mode
    /// </summary>
    private void ToggleFullscreen()
    {
        _isFullscreen = !_isFullscreen;
        FullscreenToggled?.Invoke(_isFullscreen);
    }

    /// <summary>
    /// Reset fullscreen state (called by MainWindow to sync state)
    /// </summary>
    public void ResetFullscreenState()
    {
        _isFullscreen = false;
    }

    /// <summary>
    /// Set visibility of player controls
    /// </summary>
    public void SetControlsVisibility(bool visible)
    {
        if (PlayerControls != null)
        {
            PlayerControls.IsVisible = visible;
        }
    }

    /// <summary>
    /// Show controls and restart auto-hide timer (for fullscreen mode)
    /// </summary>
    public void ShowControlsWithAutoHide()
    {
        if (!_isInFullscreenMode)
        {
            _logger.LogInfo("ShowControlsWithAutoHide called but not in fullscreen mode");
            return;
        }

        _logger.LogInfo("ShowControlsWithAutoHide called - setting controls visible");

        SetControlsVisibility(true);

        _logger.LogInfo($"Controls visibility set, PlayerControls != null: {PlayerControls != null}");

        // Restart the hide timer
        _controlsHideTimer?.Stop();
        _controlsHideTimer?.Start();
    }

    /// <summary>
    /// Enable fullscreen mode with auto-hide controls
    /// </summary>
    public void EnableFullscreenMode(bool enable)
    {
        _isInFullscreenMode = enable;

        if (enable)
        {
            _logger.LogInfo($"Enabling fullscreen mode - VideoPlayerControl Bounds: {this.Bounds}");

            // Ensure video container is visible
            if (VideoContainer != null)
            {
                VideoContainer.IsVisible = true;
                VideoContainer.ZIndex = 0;

                // Force layout update on container
                VideoContainer.InvalidateMeasure();
                VideoContainer.InvalidateArrange();

                _logger.LogInfo($"VideoContainer visible: {VideoContainer.IsVisible}, Bounds: {VideoContainer.Bounds}");
            }

            // Ensure VLC host is visible and force layout update
            if (_vlcHost != null)
            {
                _vlcHost.IsVisible = true;
                _vlcHost.ZIndex = 0;

                // Force layout update and refresh VLC handle
                _vlcHost.InvalidateVisual();
                _vlcHost.InvalidateMeasure();
                _vlcHost.InvalidateArrange();

                _logger.LogInfo($"VlcHost visible: {_vlcHost.IsVisible}, Bounds: {_vlcHost.Bounds}");
            }

            // Re-set the VLC window handle after layout changes with a slight delay
            if (_playerService?.MediaPlayer != null && _handleReady && _videoHandle != IntPtr.Zero)
            {
                _logger.LogInfo("Re-setting VLC window handle for fullscreen");

                // Update handle immediately
                _playerService.MediaPlayer.Hwnd = _videoHandle;

                // Schedule another update after layout settles
                Task.Delay(100).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_playerService?.MediaPlayer != null && _handleReady && _videoHandle != IntPtr.Zero)
                        {
                            _logger.LogInfo("Re-setting VLC window handle after layout settled");
                            _playerService.MediaPlayer.Hwnd = _videoHandle;
                        }
                    });
                });
            }

            // Move controls to overlay position (Grid.Row=0 with VerticalAlignment=Bottom)
            if (PlayerControls != null)
            {
                PlayerControls.SetValue(Grid.RowProperty, 0);
                PlayerControls.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
                PlayerControls.ZIndex = 100; // Ensure controls are above video

                // Make controls semi-transparent for fullscreen
                PlayerControls.Background = new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Color.FromArgb(200, 40, 40, 40)); // Semi-transparent dark background
            }

            // Initially show controls in fullscreen (they will auto-hide after 3 seconds)
            SetControlsVisibility(true);

            // Ensure the control can capture pointer events by setting a transparent background
            this.Background = Avalonia.Media.Brushes.Transparent;

            // Add mouse move handler to the entire control
            this.PointerMoved += OnPointerMovedInFullscreen;

            // Add mouse move handler to video container as well (in case native control blocks events)
            if (VideoContainer != null)
            {
                VideoContainer.PointerMoved += OnPointerMovedInFullscreen;
            }

            // Add mouse enter/leave handlers to controls
            if (PlayerControls != null)
            {
                PlayerControls.PointerEntered += OnControlsPointerEntered;
                PlayerControls.PointerExited += OnControlsPointerExited;
            }

            // Start the auto-hide timer
            _controlsHideTimer?.Stop();
            _controlsHideTimer?.Start();
        }
        else
        {
            _logger.LogInfo("Disabling fullscreen mode");

            // Restore controls to normal position (Grid.Row=1)
            if (PlayerControls != null)
            {
                PlayerControls.SetValue(Grid.RowProperty, 1);
                PlayerControls.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
                PlayerControls.ZIndex = 0;

                // Restore normal controls background
                PlayerControls.Background = new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Color.FromRgb(240, 240, 240)); // Normal light background
            }

            // Remove handlers when exiting fullscreen
            this.PointerMoved -= OnPointerMovedInFullscreen;

            if (VideoContainer != null)
            {
                VideoContainer.PointerMoved -= OnPointerMovedInFullscreen;
            }

            if (PlayerControls != null)
            {
                PlayerControls.PointerEntered -= OnControlsPointerEntered;
                PlayerControls.PointerExited -= OnControlsPointerExited;
            }

            // Restore normal background
            this.Background = null;

            // Stop the hide timer
            _controlsHideTimer?.Stop();

            // Show controls when exiting fullscreen
            SetControlsVisibility(true);
        }
    }

    /// <summary>
    /// Handle pointer moved in fullscreen mode
    /// </summary>
    private void OnPointerMovedInFullscreen(object? sender, PointerEventArgs e)
    {
        if (!_isInFullscreenMode) return;

        _logger.LogDebug("Pointer moved in fullscreen - showing controls");

        // Show controls
        Dispatcher.UIThread.Post(() =>
        {
            SetControlsVisibility(true);
            _logger.LogDebug($"Controls visibility set to true, IsVisible: {PlayerControls?.IsVisible}");
        });

        // Restart the hide timer
        _controlsHideTimer?.Stop();
        _controlsHideTimer?.Start();
    }

    /// <summary>
    /// Handle pointer entered controls area
    /// </summary>
    private void OnControlsPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!_isInFullscreenMode) return;

        // Stop the hide timer when mouse is over controls
        _controlsHideTimer?.Stop();
    }

    /// <summary>
    /// Handle pointer exited controls area
    /// </summary>
    private void OnControlsPointerExited(object? sender, PointerEventArgs e)
    {
        if (!_isInFullscreenMode) return;

        // Restart the hide timer when mouse leaves controls
        _controlsHideTimer?.Stop();
        _controlsHideTimer?.Start();
    }

    /// <summary>
    /// Timer callback to hide controls
    /// </summary>
    private void ControlsHideTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isInFullscreenMode) return;

        Dispatcher.UIThread.Post(() =>
        {
            SetControlsVisibility(false);
        });
    }

    /// <summary>
    /// Update timer elapsed event
    /// </summary>
    private void UpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (_playerService == null || !IsVideoLoaded)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (!_isUserSeeking)
                {
                    _isTimerUpdatingSlider = true;
                    float position = _playerService.Position;
                    ProgressSlider.Value = position * 100;
                    _isTimerUpdatingSlider = false;
                }

                long currentTime = _playerService.Time;
                long duration = _playerService.Duration;

                TimeText.Text = FormatTime(currentTime);
                DurationText.Text = FormatTime(duration);

                UpdatePlayPauseButton();
            }
            catch
            {
                // Ignore UI update errors
                _isTimerUpdatingSlider = false;
            }
        });
    }

    /// <summary>
    /// Format milliseconds to time string
    /// </summary>
    private string FormatTime(long milliseconds)
    {
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);
        return timeSpan.ToString(@"hh\:mm\:ss");
    }

    /// <summary>
    /// Handle video ended event
    /// </summary>
    private void OnVideoEnded()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdatePlayPauseButton();
        });
    }

    /// <summary>
    /// Stop playback and cleanup
    /// </summary>
    public void Stop()
    {
        _updateTimer?.Stop();
        _playerService?.Stop();
        IsVideoLoaded = false;
        UpdatePlayPauseButton();
    }

    /// <summary>
    /// Cleanup resources
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_vlcHost != null)
        {
            _vlcHost.HandleCreated -= OnHandleCreated;
        }

        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        _controlsHideTimer?.Stop();
        _controlsHideTimer?.Dispose();
        _playerService?.Dispose();
    }
}

/// <summary>
/// Custom NativeControlHost for VLC video rendering
/// </summary>
public class VlcNativeControlHost : NativeControlHost
{
    public event Action<IntPtr>? HandleCreated;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var handle = base.CreateNativeControlCore(parent);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, create a child window for VLC
            var hwnd = CreateWindowForVlc(parent.Handle);
            HandleCreated?.Invoke(hwnd);
            return new PlatformHandle(hwnd, "HWND");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // On Linux, use the X11 handle
            HandleCreated?.Invoke(handle.Handle);
            return handle;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On macOS, use the NSView handle
            HandleCreated?.Invoke(handle.Handle);
            return handle;
        }

        HandleCreated?.Invoke(handle.Handle);
        return handle;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    private const uint WS_CHILD = 0x40000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_CLIPCHILDREN = 0x02000000;
    private const uint WS_CLIPSIBLINGS = 0x04000000;

    private IntPtr CreateWindowForVlc(IntPtr parent)
    {
        var hwnd = CreateWindowEx(
            0,
            "Static",
            "",
            WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
            0, 0, 100, 100,
            parent,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        return hwnd;
    }
}
