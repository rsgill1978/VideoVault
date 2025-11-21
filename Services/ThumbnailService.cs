using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using VideoVault.Models;

namespace VideoVault.Services;

/// <summary>
/// Service for generating video thumbnails
/// </summary>
public class ThumbnailService
{
    private readonly string _thumbnailDirectory;
    private readonly LoggingService _logger;
    private readonly LibVLC _libVLC;

    public ThumbnailService()
    {
        _logger = LoggingService.Instance;

        // Create thumbnails directory in app data
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VideoVault",
            "thumbnails"
        );

        _thumbnailDirectory = appDataPath;

        if (!Directory.Exists(_thumbnailDirectory))
        {
            Directory.CreateDirectory(_thumbnailDirectory);
            _logger.LogInfo($"Created thumbnail directory: {_thumbnailDirectory}");
        }

        // Initialize LibVLC
        Core.Initialize();
        _libVLC = new LibVLC();
        _logger.LogInfo("ThumbnailService initialized");
    }

    /// <summary>
    /// Generate a thumbnail for a video file
    /// </summary>
    /// <param name="videoFile">Video file to generate thumbnail for</param>
    /// <param name="timePositionSeconds">Time position in seconds to capture thumbnail (default: 5 seconds)</param>
    /// <returns>Path to the generated thumbnail or empty string if failed</returns>
    public async Task<string> GenerateThumbnailAsync(VideoFile videoFile, double timePositionSeconds = 5.0)
    {
        try
        {
            if (!File.Exists(videoFile.FilePath))
            {
                _logger.LogWarning($"Video file not found: {videoFile.FilePath}");
                return string.Empty;
            }

            // Generate thumbnail filename based on video file hash
            var thumbnailFileName = $"{videoFile.FileHash}.jpg";
            var thumbnailPath = Path.Combine(_thumbnailDirectory, thumbnailFileName);

            // Check if thumbnail already exists
            if (File.Exists(thumbnailPath))
            {
                _logger.LogDebug($"Thumbnail already exists: {thumbnailFileName}");
                return thumbnailPath;
            }

            _logger.LogDebug($"Generating thumbnail for: {videoFile.FileName}");

            // Generate thumbnail using LibVLC
            await Task.Run(() =>
            {
                using var media = new Media(_libVLC, videoFile.FilePath);
                using var mediaplayer = new MediaPlayer(media);

                // Parse media to get duration
                media.Parse(MediaParseOptions.ParseNetwork);

                // Ensure time position is valid
                var duration = videoFile.Duration > 0 ? videoFile.Duration : 10.0;
                var captureTime = Math.Min(timePositionSeconds, duration * 0.5);

                // Take snapshot
                mediaplayer.Play();

                // Wait a bit for media to start
                System.Threading.Thread.Sleep(100);

                // Seek to the desired position
                mediaplayer.Time = (long)(captureTime * 1000);

                // Wait for seeking to complete
                System.Threading.Thread.Sleep(500);

                // Take snapshot at specified dimensions
                mediaplayer.TakeSnapshot(0, thumbnailPath, 320, 240);

                // Wait for snapshot to be saved
                System.Threading.Thread.Sleep(500);

                mediaplayer.Stop();
            });

            // Verify thumbnail was created
            if (File.Exists(thumbnailPath))
            {
                _logger.LogInfo($"Thumbnail generated successfully: {thumbnailFileName}");
                return thumbnailPath;
            }
            else
            {
                _logger.LogWarning($"Failed to generate thumbnail for: {videoFile.FileName}");
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating thumbnail for {videoFile.FileName}", ex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Delete thumbnail for a video file
    /// </summary>
    public void DeleteThumbnail(VideoFile videoFile)
    {
        try
        {
            if (string.IsNullOrEmpty(videoFile.ThumbnailPath))
                return;

            if (File.Exists(videoFile.ThumbnailPath))
            {
                File.Delete(videoFile.ThumbnailPath);
                _logger.LogDebug($"Deleted thumbnail: {videoFile.ThumbnailPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting thumbnail for {videoFile.FileName}", ex);
        }
    }

    /// <summary>
    /// Clean up orphaned thumbnails (thumbnails without corresponding videos)
    /// </summary>
    public void CleanupOrphanedThumbnails(System.Collections.Generic.List<VideoFile> existingVideos)
    {
        try
        {
            _logger.LogInfo("Cleaning up orphaned thumbnails...");

            var existingHashes = new System.Collections.Generic.HashSet<string>(
                existingVideos.Select(v => v.FileHash)
            );

            var thumbnailFiles = Directory.GetFiles(_thumbnailDirectory, "*.jpg");
            int deletedCount = 0;

            foreach (var thumbnailFile in thumbnailFiles)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(thumbnailFile);

                if (!existingHashes.Contains(fileNameWithoutExtension))
                {
                    File.Delete(thumbnailFile);
                    deletedCount++;
                    _logger.LogDebug($"Deleted orphaned thumbnail: {Path.GetFileName(thumbnailFile)}");
                }
            }

            _logger.LogInfo($"Cleanup complete. Deleted {deletedCount} orphaned thumbnails.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during thumbnail cleanup", ex);
        }
    }

    /// <summary>
    /// Get the thumbnail directory path
    /// </summary>
    public string ThumbnailDirectory => _thumbnailDirectory;
}
