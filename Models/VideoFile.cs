using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoVault.Models;

/// <summary>
/// Represents a video file in the catalog
/// </summary>
public class VideoFile : INotifyPropertyChanged
{
    private bool _isMarkedForDeletion;

    /// <summary>
    /// Unique identifier for the video
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Full path to the video file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the video file
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// SHA256 hash of the file for duplicate detection
    /// </summary>
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when file was added to catalog
    /// </summary>
    public DateTime DateAdded { get; set; }

    /// <summary>
    /// Duration of the video in seconds
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// Video resolution (e.g., "1920x1080")
    /// </summary>
    public string Resolution { get; set; } = string.Empty;

    /// <summary>
    /// File extension
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating if this file is a duplicate
    /// </summary>
    public bool IsDuplicate { get; set; }

    /// <summary>
    /// ID of the original file if this is a duplicate
    /// </summary>
    public int? OriginalFileId { get; set; }

    /// <summary>
    /// Flag indicating if this file is marked for deletion
    /// </summary>
    public bool IsMarkedForDeletion
    {
        get => _isMarkedForDeletion;
        set
        {
            if (_isMarkedForDeletion != value)
            {
                _isMarkedForDeletion = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raise property changed event
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
