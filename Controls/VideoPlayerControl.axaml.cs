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
        _logger.LogInfo($"Native control handle created: {_videoHandle}");

        // If player service already exists, set the handle now
        if (_playerService?.MediaPlayer != null)
        {
            _playerService.MediaPlayer.Hwnd = _videoHandle;
            _logger.LogInfo("Handle set on existing MediaPlayer");
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
            _logger.LogInfo("Initializing video player service");

            _playerService = new VideoPlayerService();
            _playerService.Initialize();

            // Set the window handle if we have it
            if (_playerService.MediaPlayer != null && _handleReady && _videoHandle != IntPtr.Zero)
            {
                _playerService.MediaPlayer.Hwnd = _videoHandle;
                _logger.LogInfo($"MediaPlayer.Hwnd set to: {_videoHandle}");
            }
            else
            {
                _logger.LogWarning($"Handle not ready - MediaPlayer: {_playerService.MediaPlayer != null}, HandleReady: {_handleReady}, Handle: {_videoHandle}");
            }

            // Subscribe to player events
            if (_playerService != null)
            {
                _playerService.EndReached += OnVideoEnded;
            }

            _logger.LogInfo("Video player initialized");

            // Load pending video if there is one
            if (_pendingVideoPath != null)
            {
                var pendingPath = _pendingVideoPath;
                _pendingVideoPath = null;
                _logger.LogInfo($"Loading pending video: {pendingPath}");
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
    /// Load a video file (does NOT auto-play)
    /// </summary>
    public void LoadVideo(string filePath)
    {
        try
        {
            _logger.LogInfo($"LoadVideo: {filePath}");

            // If initialization is in progress, queue this video
            if (_isInitializing)
            {
                _logger.LogInfo("Initialization in progress, queueing video");
                _pendingVideoPath = filePath;
                return;
            }

            // Initialize player if not already initialized
            if (_playerService == null)
            {
                _logger.LogInfo("Player not initialized, starting initialization and queueing video");
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
                _logger.LogInfo($"Handle set before loading: {_videoHandle}");
            }
            else
            {
                _logger.LogWarning($"Handle not ready - HandleReady: {_handleReady}, Handle: {_videoHandle}");
            }

            // Load the video
            _playerService.LoadVideo(filePath);

            // Store the video name (will be displayed when playback starts)
            _currentVideoName = System.IO.Path.GetFileName(filePath);

            IsVideoLoaded = true;

            if (NoVideoText != null)
            {
                NoVideoText.IsVisible = false;
            }

            _updateTimer?.Start();
            UpdatePlayPauseButton();

            _logger.LogInfo("Video loaded successfully");
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
        _logger.LogInfo("Play button clicked");
        
        if (_playerService == null)
        {
            _logger.LogError("Player service null");
            return;
        }

        if (!IsVideoLoaded)
        {
            _logger.LogError("No video loaded");
            return;
        }

        // Verify handle is set before playing
        if (_playerService.MediaPlayer != null && _handleReady && _videoHandle != IntPtr.Zero)
        {
            _playerService.MediaPlayer.Hwnd = _videoHandle;
            _logger.LogInfo($"Handle set before playing: {_videoHandle}");
        }
        else
        {
            _logger.LogError($"Cannot play - handle not available: HandleReady={_handleReady}, Handle={_videoHandle}");
        }

        _logger.LogInfo($"IsPlaying before: {_playerService.IsPlaying}");

        bool wasPlaying = _playerService.IsPlaying;
        _playerService.TogglePlayPause();
        UpdatePlayPauseButton();

        // If we just started playing, fire the VideoStarted event
        if (!wasPlaying && _playerService.IsPlaying && _currentVideoName != null)
        {
            VideoStarted?.Invoke(_currentVideoName);
            _logger.LogInfo($"Video started playing: {_currentVideoName}");
        }

        _logger.LogInfo($"IsPlaying after: {_playerService.IsPlaying}");
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
                _logger.LogInfo($"Seeking to position: {position:P0}");
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
                _logger.LogInfo($"Seeking to position (capture lost): {position:P0}");
            }
        }
    }

    /// <summary>
    /// Handle progress slider value changed
    /// </summary>
    private void ProgressSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        // Don't update video position while timer is updating the slider
        // Only seek when user manually changes it (handled in PointerReleased)
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
                    float position = _playerService.Position;
                    ProgressSlider.Value = position * 100;
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
