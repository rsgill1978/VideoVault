using System.Security.Cryptography;
using VideoVault.Models;

namespace VideoVault.Services;

/// <summary>
/// Service for scanning directories and processing video files
/// </summary>
public class FileScannerService
{
    private readonly AppSettings _settings;

    public FileScannerService(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Event for reporting scan progress
    /// </summary>
    public event Action<int, int, string>? ProgressChanged;

    /// <summary>
    /// Event for reporting when a file is processed
    /// </summary>
    public event Action<VideoFile>? FileProcessed;

    /// <summary>
    /// Scan directory recursively for video files
    /// </summary>
    public async Task<List<string>> ScanDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        var videoFiles = new List<string>();

        try
        {
            // Get all files recursively
            await Task.Run(() =>
            {
                ScanDirectoryRecursive(path, videoFiles, cancellationToken);
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Scan was cancelled
            throw;
        }

        return videoFiles;
    }

    /// <summary>
    /// Recursively scan directory and subdirectories
    /// </summary>
    private void ScanDirectoryRecursive(string path, List<string> videoFiles, CancellationToken cancellationToken)
    {
        // Check for cancellation
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Get all files in current directory
            var files = Directory.GetFiles(path);

            // Filter video files by extension
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string extension = Path.GetExtension(file).ToLowerInvariant();
                if (_settings.VideoExtensions.Contains(extension))
                {
                    videoFiles.Add(file);
                }
            }

            // Recursively scan subdirectories
            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                ScanDirectoryRecursive(directory, videoFiles, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories without access permission
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scanning directory {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Process video files and create VideoFile objects
    /// </summary>
    public async Task<List<VideoFile>> ProcessFilesAsync(List<string> filePaths, CancellationToken cancellationToken)
    {
        var videoFiles = new List<VideoFile>();
        int total = filePaths.Count;
        int processed = 0;

        foreach (var filePath in filePaths)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Report progress
                processed++;
                ProgressChanged?.Invoke(processed, total, Path.GetFileName(filePath));

                // Create VideoFile object
                var videoFile = await CreateVideoFileAsync(filePath);
                videoFiles.Add(videoFile);

                // Notify that file was processed
                FileProcessed?.Invoke(videoFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }

        return videoFiles;
    }

    /// <summary>
    /// Create VideoFile object from file path
    /// </summary>
    private async Task<VideoFile> CreateVideoFileAsync(string filePath)
    {
        // Get file information
        var fileInfo = new FileInfo(filePath);

        // Calculate file hash for duplicate detection
        string hash = await CalculateFileHashAsync(filePath);

        // Create VideoFile object
        var videoFile = new VideoFile
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            FileSize = fileInfo.Length,
            FileHash = hash,
            DateAdded = DateTime.Now,
            Extension = fileInfo.Extension,
            Resolution = string.Empty,
            Duration = 0
        };

        return videoFile;
    }

    /// <summary>
    /// Calculate SHA256 hash of file for duplicate detection
    /// </summary>
    private async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);

        // Calculate hash asynchronously
        byte[] hashBytes = await sha256.ComputeHashAsync(stream);

        // Convert to hexadecimal string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
