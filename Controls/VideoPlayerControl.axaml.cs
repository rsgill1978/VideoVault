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
    private bool _isUpdatingVolume = false;
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
        if (_playerService == null) return;

        // Determine icon based on playback state
        string icon = _playerService.IsPlaying ? "⏸" : "▶";

        // Update normal controls button
        if (PlayPauseButton.Content is TextBlock textBlock)
        {
            textBlock.Text = icon;
        }

        // Update fullscreen controls button
        if (FullscreenPlayPauseIcon != null)
        {
            FullscreenPlayPauseIcon.Text = icon;
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
        // Prevent feedback loops when syncing sliders
        if (_isUpdatingVolume) return;

        if (_playerService != null)
        {
            int volume = (int)e.NewValue;
            _playerService.SetVolume(volume);

            // Sync the other slider
            _isUpdatingVolume = true;
            try
            {
                if (sender == VolumeSlider && FullscreenVolumeSlider != null)
                {
                    FullscreenVolumeSlider.Value = volume;
                }
                else if (sender == FullscreenVolumeSlider && VolumeSlider != null)
                {
                    VolumeSlider.Value = volume;
                }

                UpdateVolumeButton();
            }
            finally
            {
                _isUpdatingVolume = false;
            }
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

        _isUpdatingVolume = true;
        try
        {
            if (_isMuted)
            {
                // Unmute
                _isMuted = false;
                _playerService.SetVolume(_volumeBeforeMute);
                VolumeSlider.Value = _volumeBeforeMute;
                if (FullscreenVolumeSlider != null)
                {
                    FullscreenVolumeSlider.Value = _volumeBeforeMute;
                }
            }
            else
            {
                // Mute
                _volumeBeforeMute = _playerService.GetVolume();
                _isMuted = true;
                _playerService.SetVolume(0);
                VolumeSlider.Value = 0;
                if (FullscreenVolumeSlider != null)
                {
                    FullscreenVolumeSlider.Value = 0;
                }
            }

            UpdateVolumeButton();
        }
        finally
        {
            _isUpdatingVolume = false;
        }
    }

    /// <summary>
    /// Update volume button icon
    /// </summary>
    private void UpdateVolumeButton()
    {
        if (VolumeSlider == null) return;

        // Determine icon based on volume level
        int volume = (int)VolumeSlider.Value;
        string icon = volume == 0 ? "🔇" : (volume < 50 ? "🔉" : "🔊");

        // Update normal controls button
        if (VolumeButton.Content is TextBlock textBlock)
        {
            textBlock.Text = icon;
        }

        // Update fullscreen controls button
        if (FullscreenVolumeIcon != null)
        {
            FullscreenVolumeIcon.Text = icon;
        }

        // Sync fullscreen volume slider
        if (FullscreenVolumeSlider != null)
        {
            FullscreenVolumeSlider.Value = volume;
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
    /// <param name="visible">True to show controls, false to hide</param>
    public void SetControlsVisibility(bool visible)
    {
        // In fullscreen mode, control fullscreen overlay canvas
        if (_isInFullscreenMode)
        {
            if (FullscreenControlsCanvas != null)
            {
                FullscreenControlsCanvas.IsVisible = visible;
            }
        }
        else
        {
            // In normal mode, control regular controls
            if (PlayerControls != null)
            {
                PlayerControls.IsVisible = visible;
            }
        }
    }

    /// <summary>
    /// Show controls and start auto-hide timer
    /// </summary>
    public void ShowControlsWithAutoHide()
    {
        if (!_isInFullscreenMode) return;

        SetControlsVisibility(true);

        _controlsHideTimer?.Stop();
        _controlsHideTimer?.Start();
    }

    /// <summary>
    /// Enable or disable fullscreen mode
    /// </summary>
    public void EnableFullscreenMode(bool enable)
    {
        _isInFullscreenMode = enable;

        if (enable)
        {
            // Resize VLC window to leave space for controls at bottom
            if (_vlcHost != null && this.Bounds.Height > 0)
            {
                double controlsHeight = 120;
                double videoHeight = this.Bounds.Height - controlsHeight;
                _vlcHost.Height = videoHeight;
                _vlcHost.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            }

            // Hide normal mode controls
            if (PlayerControls != null)
            {
                PlayerControls.IsVisible = false;
            }

            // Show fullscreen overlay
            if (FullscreenControlsCanvas != null)
            {
                FullscreenControlsCanvas.IsVisible = true;
                FullscreenControlsCanvas.InvalidateMeasure();
                FullscreenControlsCanvas.InvalidateArrange();
            }

            // Copy current control states to fullscreen controls
            SyncControlsToFullscreen();

            // Enable pointer events for showing controls on mouse movement
            this.Background = Avalonia.Media.Brushes.Transparent;
            this.PointerMoved += OnPointerMovedInFullscreen;

            if (VideoContainer != null)
            {
                VideoContainer.PointerMoved += OnPointerMovedInFullscreen;
            }

            // Pause auto-hide when mouse is over controls
            if (FullscreenControlsCanvas != null)
            {
                FullscreenControlsCanvas.PointerEntered += OnControlsPointerEntered;
                FullscreenControlsCanvas.PointerExited += OnControlsPointerExited;
            }

            // Start auto-hide timer
            _controlsHideTimer?.Stop();
            _controlsHideTimer?.Start();
        }
        else
        {
            // Restore VLC window to full size
            if (_vlcHost != null)
            {
                _vlcHost.Height = double.NaN;
                _vlcHost.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            }

            // Show normal controls
            if (PlayerControls != null)
            {
                PlayerControls.IsVisible = true;
            }

            // Hide fullscreen overlay
            if (FullscreenControlsCanvas != null)
            {
                FullscreenControlsCanvas.IsVisible = false;
            }

            // Remove pointer event handlers
            this.PointerMoved -= OnPointerMovedInFullscreen;

            if (VideoContainer != null)
            {
                VideoContainer.PointerMoved -= OnPointerMovedInFullscreen;
            }

            if (FullscreenControlsCanvas != null)
            {
                FullscreenControlsCanvas.PointerEntered -= OnControlsPointerEntered;
                FullscreenControlsCanvas.PointerExited -= OnControlsPointerExited;
            }

            this.Background = null;
            _controlsHideTimer?.Stop();
        }
    }

    /// <summary>
    /// Copy control states from normal mode to fullscreen mode
    /// </summary>
    private void SyncControlsToFullscreen()
    {
        if (FullscreenPlayPauseIcon != null && PlayPauseButton.Content is TextBlock normalIcon)
        {
            FullscreenPlayPauseIcon.Text = normalIcon.Text;
        }

        if (FullscreenProgressSlider != null && ProgressSlider != null)
        {
            FullscreenProgressSlider.Value = ProgressSlider.Value;
        }

        if (FullscreenTimeText != null && TimeText != null)
        {
            FullscreenTimeText.Text = TimeText.Text;
        }

        if (FullscreenDurationText != null && DurationText != null)
        {
            FullscreenDurationText.Text = DurationText.Text;
        }

        if (FullscreenVolumeSlider != null && VolumeSlider != null)
        {
            FullscreenVolumeSlider.Value = VolumeSlider.Value;
        }

        if (FullscreenVolumeIcon != null && VolumeButton.Content is TextBlock volumeIcon)
        {
            FullscreenVolumeIcon.Text = volumeIcon.Text;
        }
    }

    /// <summary>
    /// Show controls when mouse moves in fullscreen
    /// </summary>
    private void OnPointerMovedInFullscreen(object? sender, PointerEventArgs e)
    {
        if (!_isInFullscreenMode) return;

        Dispatcher.UIThread.Post(() =>
        {
            SetControlsVisibility(true);
        });

        _controlsHideTimer?.Stop();
        _controlsHideTimer?.Start();
    }

    /// <summary>
    /// Stop auto-hide when mouse enters controls
    /// </summary>
    private void OnControlsPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!_isInFullscreenMode) return;
        _controlsHideTimer?.Stop();
    }

    /// <summary>
    /// Restart auto-hide when mouse leaves controls
    /// </summary>
    private void OnControlsPointerExited(object? sender, PointerEventArgs e)
    {
        if (!_isInFullscreenMode) return;
        _controlsHideTimer?.Stop();
        _controlsHideTimer?.Start();
    }

    /// <summary>
    /// Hide controls after timer expires
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
                // Update progress slider if user is not seeking
                if (!_isUserSeeking)
                {
                    _isTimerUpdatingSlider = true;
                    float position = _playerService.Position;

                    // Update normal controls
                    if (ProgressSlider != null)
                    {
                        ProgressSlider.Value = position * 100;
                    }

                    // Update fullscreen controls
                    if (FullscreenProgressSlider != null)
                    {
                        FullscreenProgressSlider.Value = position * 100;
                    }

                    _isTimerUpdatingSlider = false;
                }

                // Get current time and duration
                long currentTime = _playerService.Time;
                long duration = _playerService.Duration;
                string timeStr = FormatTime(currentTime);
                string durationStr = FormatTime(duration);

                // Update normal controls time display
                if (TimeText != null)
                {
                    TimeText.Text = timeStr;
                }

                if (DurationText != null)
                {
                    DurationText.Text = durationStr;
                }

                // Update fullscreen controls time display
                if (FullscreenTimeText != null)
                {
                    FullscreenTimeText.Text = timeStr;
                }

                if (FullscreenDurationText != null)
                {
                    FullscreenDurationText.Text = durationStr;
                }

                // Update play/pause button icons
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
