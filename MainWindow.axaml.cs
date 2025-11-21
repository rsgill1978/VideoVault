using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoVault.Models;
using VideoVault.Services;
using VideoVault.ViewModels;
using VideoVault.Controls;

namespace VideoVault.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;
    private bool _isVideoPlayerCollapsed = false;
    private readonly LoggingService _logger;

    public MainWindow()
    {
        try
        {
            _logger = LoggingService.Instance;
            
            InitializeComponent();
            
            // Add ESC key handler for exiting fullscreen
            this.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Escape && this.WindowState == WindowState.FullScreen)
                {
                    ExitFullscreen();
                    e.Handled = true;
                }
            };
            
            // Initialize video player after the window is loaded
            this.Loaded += async (s, e) =>
            {
                if (ViewModel == null)
                {
                    _logger.LogError("MainWindowViewModel is not available");
                    return;
                }
                
                // Initialize video player asynchronously
                await Task.Run(() =>
                {
                    try
                    {
                        var initTask = Task.Run(() =>
                        {
                            VideoPlayer.InitializePlayer();
                        });
                        
                        if (!initTask.Wait(TimeSpan.FromSeconds(5)))
                        {
                            _logger.LogWarning("Video player initialization timed out");
                            return;
                        }
                        
                        VideoPlayer.FullscreenToggled += OnFullscreenToggled;
                        _logger.LogInfo("Video player initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to initialize video player", ex);
                        
                        // Disable video player UI
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            if (VideoPlayerContent != null)
                            {
                                VideoPlayerContent.IsVisible = false;
                            }
                            if (VideoPlayerCollapseButton != null)
                            {
                                VideoPlayerCollapseButton.IsEnabled = false;
                            }
                        });
                    }
                });
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in MainWindow constructor: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Handle video player collapse/expand button click
    /// </summary>
    private void VideoPlayerCollapse_Click(object? sender, RoutedEventArgs e)
    {
        _isVideoPlayerCollapsed = !_isVideoPlayerCollapsed;

        if (_isVideoPlayerCollapsed)
        {
            // Collapse video player
            if (VideoPlayerContent != null)
            {
                VideoPlayerContent.IsVisible = false;
            }
            if (VideoPlayerCollapseButton != null)
            {
                VideoPlayerCollapseButton.Content = "Expand";
            }
            if (VideoPlayerCollapseIcon != null)
            {
                VideoPlayerCollapseIcon.Text = "▶";
            }
            
            // Stop video playback when collapsed
            VideoPlayer?.Stop();
        }
        else
        {
            // Expand video player
            if (VideoPlayerContent != null)
            {
                VideoPlayerContent.IsVisible = true;
            }
            if (VideoPlayerCollapseButton != null)
            {
                VideoPlayerCollapseButton.Content = "Collapse";
            }
            if (VideoPlayerCollapseIcon != null)
            {
                VideoPlayerCollapseIcon.Text = "▼";
            }
        }
    }

    /// <summary>
    /// Handle browse button click to select directory
    /// </summary>
    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        
        // Open folder picker dialog
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Video Directory",
            AllowMultiple = false
        });

        // Update video path if folder was selected
        if (folders.Count > 0)
        {
            ViewModel.VideoPath = folders[0].Path.LocalPath;
        }
    }

    /// <summary>
    /// Handle scan button click to start scanning
    /// </summary>
    private async void ScanButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        
        await ViewModel.StartScanAsync();
    }

    /// <summary>
    /// Handle cancel button click to stop scanning
    /// </summary>
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        
        ViewModel.CancelScan();
    }

    /// <summary>
    /// Handle find duplicates button click
    /// </summary>
    private async void FindDuplicatesButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        
        await ViewModel.FindDuplicatesAsync();
    }

    /// <summary>
    /// Handle video list selection changed
    /// </summary>
    private void VideoList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || ViewModel.SelectedVideo == null) return;
        
        try
        {
            var filePath = ViewModel.SelectedVideo.FilePath;
            
            // Check if file exists
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Selected video file not found: {filePath}");
                return;
            }
            
            // Load selected video
            VideoPlayer.LoadVideo(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load selected video", ex);
        }
    }

    /// <summary>
    /// Handle delete duplicates button click
    /// </summary>
    private async void DeleteDuplicates_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        
        if (sender is Button button && button.Tag is DuplicateGroup group)
        {
            // Get files marked for deletion
            var filesToDelete = group.Files.Where(f => f.IsMarkedForDeletion).ToList();

            if (filesToDelete.Count == 0)
            {
                await ShowMessageBox("No files selected", "Please select files to delete by checking the boxes.");
                return;
            }

            if (filesToDelete.Count == group.Files.Count)
            {
                await ShowMessageBox("Cannot delete all files", "You must keep at least one file from each duplicate group.");
                return;
            }

            // Confirm deletion
            bool confirmed = await ShowConfirmDialog(
                "Confirm Deletion",
                $"Are you sure you want to delete {filesToDelete.Count} file(s)? This action cannot be undone."
            );

            if (!confirmed)
            {
                return;
            }

            // Delete files
            await ViewModel.DeleteMarkedDuplicatesAsync(group, filesToDelete);
        }
    }

    /// <summary>
    /// Handle fullscreen toggle
    /// </summary>
    private void OnFullscreenToggled(bool isFullscreen)
    {
        if (isFullscreen)
        {
            EnterFullscreen();
        }
        else
        {
            ExitFullscreen();
        }
    }

    /// <summary>
    /// Enter fullscreen mode
    /// </summary>
    private void EnterFullscreen()
    {
        _logger.LogDebug("Entering fullscreen mode");
        this.WindowState = WindowState.FullScreen;
    }

    /// <summary>
    /// Exit fullscreen mode
    /// </summary>
    private void ExitFullscreen()
    {
        _logger.LogDebug("Exiting fullscreen mode");
        this.WindowState = WindowState.Normal;
    }

    /// <summary>
    /// Handle settings button click
    /// </summary>
    private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        
        var settingsWindow = new SettingsWindow(ViewModel.GetSettings());
        var result = await settingsWindow.ShowDialog<bool>(this);

        if (result)
        {
            ViewModel.ReloadSettings();
        }
    }

    /// <summary>
    /// Handle exit menu item click
    /// </summary>
    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        VideoPlayer.Stop();
        Close();
    }

    /// <summary>
    /// Handle open log folder menu item click
    /// </summary>
    private void OpenLogFolder_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string logFilePath = _logger.GetLogFilePath();
            string? logDirectory = Path.GetDirectoryName(logFilePath);
            
            if (!string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory))
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start("explorer.exe", logDirectory);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", logDirectory);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", logDirectory);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open log folder", ex);
        }
    }

    /// <summary>
    /// Handle about menu item click
    /// </summary>
    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        var aboutDialog = new Window
        {
            Title = "About VideoVault",
            Width = 450,
            Height = 350,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(30),
                Spacing = 15,
                Children =
                {
                    new TextBlock 
                    { 
                        Text = "VideoVault", 
                        FontSize = 24, 
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock 
                    { 
                        Text = "Version 1.0.0 - Phase 2", 
                        FontSize = 14,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock 
                    { 
                        Text = "Adult Video Catalog Application", 
                        FontSize = 12,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 10, 0, 0)
                    },
                    new TextBlock 
                    { 
                        Text = "A cross-platform desktop application for cataloging and organizing video files.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextAlignment = Avalonia.Media.TextAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 10, 0, 0)
                    },
                    new TextBlock 
                    { 
                        Text = "Features:",
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        Margin = new Avalonia.Thickness(0, 10, 0, 0)
                    },
                    new TextBlock 
                    { 
                        Text = "• Recursive video file scanning\n• SQLite database catalog\n• Duplicate file detection & deletion\n• Embedded video player\n• Fullscreen playback\n• Cross-platform (Windows, Linux, macOS)",
                        Margin = new Avalonia.Thickness(20, 0, 0, 0)
                    },
                    new TextBlock 
                    { 
                        Text = "© 2025 VideoVault Project",
                        FontSize = 10,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 20, 0, 0)
                    },
                    new Button 
                    { 
                        Content = "OK", 
                        Width = 100,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 10, 0, 0)
                    }
                }
            }
        };

        // Setup OK button click handler
        if (aboutDialog.Content is StackPanel panel)
        {
            var button = panel.Children.OfType<Button>().FirstOrDefault();
            if (button != null)
            {
                button.Click += (s, args) => aboutDialog.Close();
            }
        }

        await aboutDialog.ShowDialog(this);
    }

    /// <summary>
    /// Show a message box dialog
    /// </summary>
    private async Task ShowMessageBox(string title, string message)
    {
        var messageBox = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 15,
                Children =
                {
                    new TextBlock 
                    { 
                        Text = message, 
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap 
                    },
                    new Button 
                    { 
                        Content = "OK", 
                        Width = 100,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    }
                }
            }
        };

        // Setup OK button
        if (messageBox.Content is StackPanel panel)
        {
            var button = panel.Children.OfType<Button>().FirstOrDefault();
            if (button != null)
            {
                button.Click += (s, args) => messageBox.Close();
            }
        }

        await messageBox.ShowDialog(this);
    }

    /// <summary>
    /// Show a confirmation dialog
    /// </summary>
    private async Task<bool> ShowConfirmDialog(string title, string message)
    {
        bool result = false;

        var confirmDialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 15,
                Children =
                {
                    new TextBlock 
                    { 
                        Text = message, 
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap 
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 10,
                        Children =
                        {
                            new Button { Content = "Yes", Width = 100 },
                            new Button { Content = "No", Width = 100 }
                        }
                    }
                }
            }
        };

        // Setup button handlers
        if (confirmDialog.Content is StackPanel panel)
        {
            var buttonPanel = panel.Children.OfType<StackPanel>().FirstOrDefault();
            if (buttonPanel != null)
            {
                var buttons = buttonPanel.Children.OfType<Button>().ToList();
                if (buttons.Count == 2)
                {
                    buttons[0].Click += (s, args) => { result = true; confirmDialog.Close(); };
                    buttons[1].Click += (s, args) => { result = false; confirmDialog.Close(); };
                }
            }
        }

        await confirmDialog.ShowDialog(this);
        return result;
    }
}
