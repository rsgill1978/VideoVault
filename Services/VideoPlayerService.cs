using System;
using System.IO;
using LibVLCSharp.Shared;

namespace VideoVault.Services;

/// <summary>
/// Service for video playback using LibVLC
/// </summary>
public class VideoPlayerService : IDisposable
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private Media? _currentMedia;
    private readonly LoggingService _logger;
    private bool _isInitialized = false;

    public VideoPlayerService()
    {
        _logger = LoggingService.Instance;
    }

    /// <summary>
    /// Event fired when playback position changes
    /// </summary>
    public event Action<float>? PositionChanged;

    /// <summary>
    /// Event fired when playback time changes
    /// </summary>
    public event Action<long>? TimeChanged;

    /// <summary>
    /// Event fired when media ends
    /// </summary>
    public event Action? EndReached;

    /// <summary>
    /// Get the media player instance
    /// </summary>
    public MediaPlayer? MediaPlayer => _mediaPlayer;

    /// <summary>
    /// Initialize LibVLC core
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            _logger.LogInfo("Initializing LibVLC core...");

            // Initialize LibVLC
            try
            {
                Core.Initialize();
                _logger.LogInfo("LibVLC Core.Initialize() completed");
            }
            catch (Exception ex)
            {
                _logger.LogError("LibVLC Core.Initialize() failed", ex);
                throw new Exception("LibVLC libraries not found. Please ensure VLC is installed.", ex);
            }

            // Create LibVLC instance with parameters for embedded playback
            _libVLC = new LibVLC("--no-video-title-show");
            _logger.LogInfo("LibVLC instance created");

            // Create media player
            _mediaPlayer = new MediaPlayer(_libVLC);
            _logger.LogInfo("MediaPlayer created");

            // Subscribe to events
            _mediaPlayer.PositionChanged += OnPositionChanged;
            _mediaPlayer.TimeChanged += OnTimeChanged;
            _mediaPlayer.EndReached += OnEndReached;

            _isInitialized = true;
            _logger.LogInfo("LibVLC initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize LibVLC", ex);
            throw;
        }
    }

    /// <summary>
    /// Load a video file without playing
    /// </summary>
    public void LoadVideo(string filePath)
    {
        if (!_isInitialized || _mediaPlayer == null || _libVLC == null)
        {
            throw new InvalidOperationException("VideoPlayerService is not initialized");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Video file not found: {filePath}");
        }

        try
        {
            _logger.LogInfo($"Loading video: {filePath}");

            // Dispose previous media if exists
            _currentMedia?.Dispose();

            // Create media from file and keep reference
            _currentMedia = new Media(_libVLC, filePath, FromType.FromPath);

            // Set media to player
            _mediaPlayer.Media = _currentMedia;

            _logger.LogInfo("Video loaded (paused, ready to play)");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load video: {filePath}", ex);
            throw;
        }
    }

    /// <summary>
    /// Load and play a video file
    /// </summary>
    public void Play(string filePath)
    {
        LoadVideo(filePath);
        
        // Start playback
        _mediaPlayer?.Play();
        _logger.LogInfo("Video playback started");
    }

    /// <summary>
    /// Play currently loaded video
    /// </summary>
    public void Play()
    {
        if (_mediaPlayer != null && _mediaPlayer.Media != null)
        {
            _mediaPlayer.Play();
            _logger.LogInfo("Video playback started");
        }
        else
        {
            _logger.LogWarning("Cannot play - no media loaded");
        }
    }

    /// <summary>
    /// Pause playback
    /// </summary>
    public void Pause()
    {
        if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
            _logger.LogDebug("Video playback paused");
        }
    }

    /// <summary>
    /// Resume playback
    /// </summary>
    public void Resume()
    {
        if (_mediaPlayer != null && !_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Play();
            _logger.LogDebug("Video playback resumed");
        }
    }

    /// <summary>
    /// Stop playback
    /// </summary>
    public void Stop()
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Stop();
            _logger.LogDebug("Video playback stopped");
        }
    }

    /// <summary>
    /// Toggle play/pause
    /// </summary>
    public void TogglePlayPause()
    {
        if (_mediaPlayer != null)
        {
            if (_mediaPlayer.IsPlaying)
            {
                Pause();
            }
            else
            {
                // If not playing, start playing (works for both stopped and paused states)
                Play();
            }
        }
    }

    /// <summary>
    /// Set playback position (0.0 to 1.0)
    /// </summary>
    public void SetPosition(float position)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Position = Math.Clamp(position, 0.0f, 1.0f);
        }
    }

    /// <summary>
    /// Set volume (0 to 100)
    /// </summary>
    public void SetVolume(int volume)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = Math.Clamp(volume, 0, 100);
        }
    }

    /// <summary>
    /// Get current volume
    /// </summary>
    public int GetVolume()
    {
        return _mediaPlayer?.Volume ?? 0;
    }

    /// <summary>
    /// Check if video is currently playing
    /// </summary>
    public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

    /// <summary>
    /// Get current playback position (0.0 to 1.0)
    /// </summary>
    public float Position => _mediaPlayer?.Position ?? 0.0f;

    /// <summary>
    /// Get current playback time in milliseconds
    /// </summary>
    public long Time => _mediaPlayer?.Time ?? 0;

    /// <summary>
    /// Get total duration in milliseconds
    /// </summary>
    public long Duration => _mediaPlayer?.Length ?? 0;

    /// <summary>
    /// Handle position changed event
    /// </summary>
    private void OnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
    {
        PositionChanged?.Invoke(e.Position);
    }

    /// <summary>
    /// Handle time changed event
    /// </summary>
    private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        TimeChanged?.Invoke(e.Time);
    }

    /// <summary>
    /// Handle end reached event
    /// </summary>
    private void OnEndReached(object? sender, EventArgs e)
    {
        _logger.LogDebug("Video playback ended");
        EndReached?.Invoke();
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.PositionChanged -= OnPositionChanged;
            _mediaPlayer.TimeChanged -= OnTimeChanged;
            _mediaPlayer.EndReached -= OnEndReached;
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }

        _currentMedia?.Dispose();
        _currentMedia = null;

        _libVLC?.Dispose();
        _libVLC = null;

        _isInitialized = false;
        _logger.LogInfo("VideoPlayerService disposed");
    }
}
