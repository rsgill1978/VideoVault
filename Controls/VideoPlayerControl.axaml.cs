using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Platform;  // For PixelFormat, AlphaFormat
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
    private WriteableBitmap? _videoBitmap;
    private IntPtr _videoBuffer = IntPtr.Zero;
    private int _videoWidth;
    private int _videoHeight;
    private VideoRenderControl VideoHost;

    // Custom control for video rendering
    private class VideoRenderControl : Control
    {
        private WriteableBitmap? _bitmap;
        private int _width;
        private int _height;

        public void SetBitmap(WriteableBitmap? bitmap, int width, int height)
        {
            _bitmap = bitmap;
            _width = width;
            _height = height;
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (_bitmap != null)
            {
                context.DrawImage(_bitmap, new Rect(0, 0, _width, _height));
            }
        }
    }

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

        // Create and add custom video render control
        VideoHost = new VideoRenderControl();
        VideoContainer.Children.Add(VideoHost);
    }

    /// <summary>
    /// Handle when VideoHost is initialized
    /// </summary>
    private void OnVideoHostInitialized(object? sender, EventArgs e)
    {
        // Not used
    }

    /// <summary>
    /// Handle when VideoHost is loaded
    /// </summary>
    private void OnVideoHostLoaded(object? sender, RoutedEventArgs e)
    {
        // Not used
    }

    /// <summary>
    /// Handle when VideoHost is attached to visual tree
    /// </summary>
    private void OnVideoHostAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Not used
    }

    // LibVLC video callback: Setup video format
    private uint SetupVideo(ref IntPtr opaque, ref IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        chroma = Marshal.StringToHGlobalAnsi("RV32");
        _videoWidth = (int)width;
        _videoHeight = (int)height;
        pitches = (uint)(_videoWidth * 4);
        lines = (uint)_videoHeight;
        _videoBuffer = Marshal.AllocHGlobal((int)(pitches * lines));
        return 1;
    }

    // LibVLC video callback: Cleanup
    private void CleanupVideo(ref IntPtr opaque)
    {
        if (_videoBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_videoBuffer);
            _videoBuffer = IntPtr.Zero;
        }
        _videoBitmap = null;
        VideoHost.SetBitmap(null, 0, 0);
    }

    // LibVLC video callback: Lock frame
    private IntPtr LockVideo(IntPtr opaque, ref IntPtr planes)
    {
        planes = _videoBuffer;
        return IntPtr.Zero;
    }

    // LibVLC video callback: Unlock frame
    private void UnlockVideo(IntPtr opaque, IntPtr picture, ref IntPtr planes)
    {
        // No action needed
    }

    // LibVLC video callback: Display frame
    private void DisplayVideo(IntPtr opaque, IntPtr picture)
    {
        if (_videoBitmap == null || _videoBitmap.PixelSize.Width != _videoWidth || _videoBitmap.PixelSize.Height != _videoHeight)
        {
            _videoBitmap = new WriteableBitmap(new PixelSize(_videoWidth, _videoHeight), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        using (var buffer = _videoBitmap.Lock())
        {
            unsafe
            {
                byte* dst = (byte*)buffer.Address.ToPointer();
                byte* src = (byte*)_videoBuffer.ToPointer();
                int stride = _videoWidth * 4;
                for (int y = 0; y < _videoHeight; y++)
                {
                    Buffer.MemoryCopy(src + y * stride, dst + y * buffer.RowBytes, stride, stride);
                }
            }
        }

        VideoHost.SetBitmap(_videoBitmap, _videoWidth, _videoHeight);
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
        try
        {
            _logger.LogInfo("Initializing video player service");
            
            _playerService = new VideoPlayerService();
            _playerService.Initialize();

            // Set up LibVLC video callbacks for OpenGL rendering
            if (_playerService.MediaPlayer != null)
            {
                _playerService.MediaPlayer.SetVideoFormatCallbacks(SetupVideo, CleanupVideo);
                _playerService.MediaPlayer.SetVideoCallbacks(LockVideo, UnlockVideo, DisplayVideo);
            }

            // Subscribe to player events
            if (_playerService != null)
            {
                _playerService.EndReached += OnVideoEnded;
            }

            _logger.LogInfo("Video player initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize video player", ex);
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
            
            if (_playerService == null)
            {
                InitializePlayer();
            }

            if (_playerService == null)
            {
                _logger.LogError("Player service null");
                return;
            }

            // Load the video
            _playerService.LoadVideo(filePath);
            
            IsVideoLoaded = true;
            
            if (NoVideoText != null)
            {
                NoVideoText.IsVisible = false;
            }
            
            _updateTimer?.Start();
            UpdatePlayPauseButton();
            
            _logger.LogInfo("Video loaded");
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

        _logger.LogInfo($"IsPlaying before: {_playerService.IsPlaying}");
        
        _playerService.TogglePlayPause();
        UpdatePlayPauseButton();
        
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
    /// Handle progress slider value changed
    /// </summary>
    private void ProgressSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUserSeeking && _playerService != null)
        {
            float position = (float)(e.NewValue / 100.0);
            _playerService.SetPosition(position);
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
            _playerService.SetVolume(_volumeBeforeMute);
            VolumeSlider.Value = _volumeBeforeMute;
            _isMuted = false;
        }
        else
        {
            _volumeBeforeMute = _playerService.GetVolume();
            _playerService.SetVolume(0);
            VolumeSlider.Value = 0;
            _isMuted = true;
        }

        UpdateVolumeButton();
    }

    /// <summary>
    /// Update volume button icon
    /// </summary>
    private void UpdateVolumeButton()
    {
        if (VolumeButton.Content is TextBlock textBlock && _playerService != null)
        {
            int volume = _playerService.GetVolume();
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
        
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        _playerService?.Dispose();
    }
}