using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VideoVault.Models;

namespace VideoVault.ViewModels;

/// <summary>
/// ViewModel for the settings window
/// </summary>
public class SettingsWindowViewModel : INotifyPropertyChanged
{
    private AppSettings _settings;
    private double _windowWidth;
    private double _windowHeight;
    private string _videoExtensions = string.Empty;
    private int _duplicateThreshold;
    private string _logLevel = "Info";
    private int _logRetentionDays;

    public SettingsWindowViewModel(AppSettings settings)
    {
        // Load current settings
        _settings = settings;
        _windowWidth = settings.WindowWidth;
        _windowHeight = settings.WindowHeight;
        _videoExtensions = string.Join(",", settings.VideoExtensions);
        _duplicateThreshold = settings.DuplicateThreshold;
        _logLevel = settings.LogLevel;
        _logRetentionDays = settings.LogRetentionDays;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Window width setting
    /// </summary>
    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            if (_windowWidth != value)
            {
                _windowWidth = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Window height setting
    /// </summary>
    public double WindowHeight
    {
        get => _windowHeight;
        set
        {
            if (_windowHeight != value)
            {
                _windowHeight = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Video extensions as comma-separated string
    /// </summary>
    public string VideoExtensions
    {
        get => _videoExtensions;
        set
        {
            if (_videoExtensions != value)
            {
                _videoExtensions = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Duplicate detection threshold
    /// </summary>
    public int DuplicateThreshold
    {
        get => _duplicateThreshold;
        set
        {
            if (_duplicateThreshold != value)
            {
                _duplicateThreshold = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Logging level
    /// </summary>
    public string LogLevel
    {
        get => _logLevel;
        set
        {
            if (_logLevel != value)
            {
                _logLevel = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Log retention days
    /// </summary>
    public int LogRetentionDays
    {
        get => _logRetentionDays;
        set
        {
            if (_logRetentionDays != value)
            {
                _logRetentionDays = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Apply settings to AppSettings object
    /// </summary>
    public void ApplySettings()
    {
        _settings.WindowWidth = WindowWidth;
        _settings.WindowHeight = WindowHeight;
        
        // Parse video extensions
        _settings.VideoExtensions.Clear();
        var extensions = VideoExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var ext in extensions)
        {
            string trimmed = ext.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                // Ensure extension starts with dot
                if (!trimmed.StartsWith("."))
                {
                    trimmed = "." + trimmed;
                }
                _settings.VideoExtensions.Add(trimmed.ToLowerInvariant());
            }
        }

        _settings.DuplicateThreshold = DuplicateThreshold;
        _settings.LogLevel = LogLevel;
        _settings.LogRetentionDays = LogRetentionDays;
    }

    /// <summary>
    /// Restore default settings
    /// </summary>
    public void RestoreDefaults()
    {
        WindowWidth = 1200;
        WindowHeight = 800;
        VideoExtensions = ".mp4,.avi,.mkv,.mov,.wmv,.flv,.webm,.m4v";
        DuplicateThreshold = 95;
        LogLevel = "Info";
        LogRetentionDays = 30;
    }

    /// <summary>
    /// Raise property changed event
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
