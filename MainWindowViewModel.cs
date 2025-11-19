using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VideoVault.Models;
using VideoVault.Services;

namespace VideoVault;

/// <summary>
/// ViewModel for the main window
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;
    private readonly FileScannerService _fileScannerService;
    private readonly DuplicateFinderService _duplicateFinderService;
    private readonly AppSettings _settings;
    private CancellationTokenSource? _cancellationTokenSource;

    private string _videoPath = string.Empty;
    private bool _isScanning = false;
    private bool _isFindingDuplicates = false;
    private int _scanProgress = 0;
    private int _scanTotal = 0;
    private string _scanStatus = "Ready";
    private VideoFile? _selectedVideo;

    public MainWindowViewModel()
    {
        // Initialize services
        _settings = AppSettings.Load();
        _databaseService = new DatabaseService();
        _fileScannerService = new FileScannerService(_settings);
        _duplicateFinderService = new DuplicateFinderService(_databaseService);

        // Subscribe to scanner events
        _fileScannerService.ProgressChanged += OnScanProgressChanged;
        _fileScannerService.FileProcessed += OnFileProcessed;

        // Subscribe to duplicate finder events
        _duplicateFinderService.ProgressChanged += OnDuplicateProgressChanged;
        _duplicateFinderService.DuplicateFound += OnDuplicateFound;

        // Set initial video path from settings
        VideoPath = _settings.LastVideoPath;

        // Load existing videos from database
        LoadVideosAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Collection of video files in the catalog
    /// </summary>
    public ObservableCollection<VideoFile> Videos { get; } = new();

    /// <summary>
    /// Collection of duplicate groups
    /// </summary>
    public ObservableCollection<DuplicateGroup> DuplicateGroups { get; } = new();

    /// <summary>
    /// Path to video directory
    /// </summary>
    public string VideoPath
    {
        get => _videoPath;
        set
        {
            if (_videoPath != value)
            {
                _videoPath = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Flag indicating if scan is in progress
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (_isScanning != value)
            {
                _isScanning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanScan));
                OnPropertyChanged(nameof(CanCancelScan));
            }
        }
    }

    /// <summary>
    /// Flag indicating if duplicate finding is in progress
    /// </summary>
    public bool IsFindingDuplicates
    {
        get => _isFindingDuplicates;
        set
        {
            if (_isFindingDuplicates != value)
            {
                _isFindingDuplicates = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanFindDuplicates));
            }
        }
    }

    /// <summary>
    /// Current scan progress
    /// </summary>
    public int ScanProgress
    {
        get => _scanProgress;
        set
        {
            if (_scanProgress != value)
            {
                _scanProgress = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Total items to scan
    /// </summary>
    public int ScanTotal
    {
        get => _scanTotal;
        set
        {
            if (_scanTotal != value)
            {
                _scanTotal = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Current scan status message
    /// </summary>
    public string ScanStatus
    {
        get => _scanStatus;
        set
        {
            if (_scanStatus != value)
            {
                _scanStatus = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Currently selected video
    /// </summary>
    public VideoFile? SelectedVideo
    {
        get => _selectedVideo;
        set
        {
            if (_selectedVideo != value)
            {
                _selectedVideo = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Check if scan can be started
    /// </summary>
    public bool CanScan => !IsScanning && !string.IsNullOrEmpty(VideoPath) && Directory.Exists(VideoPath);

    /// <summary>
    /// Check if scan can be cancelled
    /// </summary>
    public bool CanCancelScan => IsScanning;

    /// <summary>
    /// Check if duplicate finding can be started
    /// </summary>
    public bool CanFindDuplicates => !IsFindingDuplicates && Videos.Count > 0;

    /// <summary>
    /// Start scanning for video files
    /// </summary>
    public async Task StartScanAsync()
    {
        if (!CanScan) return;

        IsScanning = true;
        ScanStatus = "Scanning for video files...";
        ScanProgress = 0;
        ScanTotal = 0;

        // Create cancellation token
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // Save video path to settings
            _settings.LastVideoPath = VideoPath;
            _settings.Save();

            // Scan directory for video files
            var filePaths = await _fileScannerService.ScanDirectoryAsync(VideoPath, _cancellationTokenSource.Token);

            ScanStatus = $"Found {filePaths.Count} video files. Processing...";
            ScanTotal = filePaths.Count;

            // Process video files
            var videoFiles = await _fileScannerService.ProcessFilesAsync(filePaths, _cancellationTokenSource.Token);

            // Add to database
            ScanStatus = "Adding to database...";
            int added = 0;

            foreach (var video in videoFiles)
            {
                // Check if file already exists
                if (!await _databaseService.FileExistsAsync(video.FilePath))
                {
                    await _databaseService.AddVideoFileAsync(video);
                    added++;
                }
            }

            ScanStatus = $"Scan complete. Added {added} new video files.";
        }
        catch (OperationCanceledException)
        {
            ScanStatus = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            ScanStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Cancel ongoing scan
    /// </summary>
    public void CancelScan()
    {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Start finding duplicate files
    /// </summary>
    public async Task FindDuplicatesAsync()
    {
        if (!CanFindDuplicates) return;

        IsFindingDuplicates = true;
        ScanStatus = "Finding duplicates...";
        ScanProgress = 0;
        ScanTotal = 0;
        DuplicateGroups.Clear();

        // Create cancellation token
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // Find duplicates
            var duplicates = await _duplicateFinderService.FindDuplicatesAsync(_cancellationTokenSource.Token);

            ScanStatus = $"Found {duplicates.Count} duplicate groups.";
        }
        catch (OperationCanceledException)
        {
            ScanStatus = "Duplicate search cancelled.";
        }
        catch (Exception ex)
        {
            ScanStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsFindingDuplicates = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Delete selected duplicate group
    /// </summary>
    public async Task DeleteDuplicateGroupAsync(DuplicateGroup group, int keepFileId)
    {
        try
        {
            await _duplicateFinderService.DeleteDuplicatesAsync(group, keepFileId);

            // Reload videos
            await LoadVideosAsync();

            // Remove from duplicate groups
            DuplicateGroups.Remove(group);

            ScanStatus = "Duplicates deleted successfully.";
        }
        catch (Exception ex)
        {
            ScanStatus = $"Error deleting duplicates: {ex.Message}";
        }
    }

    /// <summary>
    /// Load videos from database
    /// </summary>
    private async Task LoadVideosAsync()
    {
        try
        {
            var videos = await _databaseService.GetAllVideosAsync();

            Videos.Clear();
            foreach (var video in videos)
            {
                Videos.Add(video);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading videos: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle scan progress changed event
    /// </summary>
    private void OnScanProgressChanged(int current, int total, string fileName)
    {
        ScanProgress = current;
        ScanTotal = total;
        ScanStatus = $"Processing: {fileName} ({current}/{total})";
    }

    /// <summary>
    /// Handle file processed event
    /// </summary>
    private void OnFileProcessed(VideoFile video)
    {
        // File processed notification can be used for real-time updates
    }

    /// <summary>
    /// Handle duplicate detection progress changed event
    /// </summary>
    private void OnDuplicateProgressChanged(int current, int total, string message)
    {
        ScanProgress = current;
        ScanTotal = total;
        ScanStatus = message;
    }

    /// <summary>
    /// Handle duplicate found event
    /// </summary>
    private void OnDuplicateFound(DuplicateGroup group)
    {
        DuplicateGroups.Add(group);
    }

    /// <summary>
    /// Raise property changed event
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
