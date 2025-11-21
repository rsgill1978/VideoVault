using System;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
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

    public VideoPlayerControl()
    {
        InitializeComponent();
        
        // Set up update timer for UI updates
        _updateTimer = new Timer(100);
        _updateTimer.Elapsed += UpdateTimer_Elapsed;
        
        // Add double-click handler for fullscreen
        if (VideoContainer != null)
        {
            VideoContainer.DoubleTapped += VideoContainer_DoubleTapped;
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
        try
        {
            _playerService = new VideoPlayerService();
            _playerService.Initialize();

            // Attach LibVLC video output to the native control host
            if (_playerService.MediaPlayer != null && VideoHost != null)
            {
                // Wait for the control to be initialized
                VideoHost.AttachedToVisualTree += (s, e) =>
                {
                    try
                    {
                        // Get the platform handle using IPlatformHandle interface
                        var platformHandle = (VideoHost as IPlatformHandle)?.Handle ?? IntPtr.Zero;
                        
                        if (platformHandle != IntPtr.Zero && _playerService.MediaPlayer != null)
                        {
                            // Set the window handle for video rendering
                            _playerService.MediaPlayer.Hwnd = platformHandle;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error attaching video output: {ex.Message}");
                    }
                };
            }

            // Subscribe to player events
            if (_playerService != null)
            {
                _playerService.EndReached += OnVideoEnded;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize video player: {ex.Message}");
        }
    }

    /// <summary>
    /// Load and play a video file
    /// </summary>
    public void LoadVideo(string filePath)
    {
        try
        {
            if (_playerService == null)
            {
                InitializePlayer();
            }

            if (_playerService == null)
            {
                return;
            }

            _playerService.LoadVideo(filePath);
            
            IsVideoLoaded = true;
            
            // Hide "No video loaded" text
            if (NoVideoText != null)
            {
                NoVideoText.IsVisible = false;
            }
            
            _updateTimer?.Start();
            UpdatePlayPauseButton();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load video: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle play/pause button click
    /// </summary>
    private void PlayPauseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_playerService == null)
        {
            return;
        }

        _playerService.TogglePlayPause();
        UpdatePlayPauseButton();
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
        // Only seek if user is interacting with slider
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
            
            // Update mute button
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
            _playerService.SetVolume(_volumeBeforeMute);
            VolumeSlider.Value = _volumeBeforeMute;
            _isMuted = false;
        }
        else
        {
            // Mute
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

        // Update UI on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                // Update progress slider
                if (!_isUserSeeking)
                {
                    float position = _playerService.Position;
                    ProgressSlider.Value = position * 100;
                }

                // Update time display
                long currentTime = _playerService.Time;
                long duration = _playerService.Duration;

                TimeText.Text = FormatTime(currentTime);
                DurationText.Text = FormatTime(duration);

                // Update play/pause button
                UpdatePlayPauseButton();
            }
            catch
            {
                // Ignore errors during UI updates
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
