using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VideoVault.Services;

namespace VideoVault;

public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handle browse button click to select directory
    /// </summary>
    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
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
        // Start scanning in background
        await ViewModel.StartScanAsync();
    }

    /// <summary>
    /// Handle cancel button click to stop scanning
    /// </summary>
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        // Cancel ongoing scan
        ViewModel.CancelScan();
    }

    /// <summary>
    /// Handle find duplicates button click
    /// </summary>
    private async void FindDuplicatesButton_Click(object? sender, RoutedEventArgs e)
    {
        // Start finding duplicates in background
        await ViewModel.FindDuplicatesAsync();
    }

    /// <summary>
    /// Handle settings button click
    /// </summary>
    private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        // Open settings window
        var settingsWindow = new SettingsWindow(ViewModel.GetSettings());
        var result = await settingsWindow.ShowDialog<bool>(this);

        // Reload settings if saved
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
        // Close the application
        Close();
    }

    /// <summary>
    /// Handle open log folder menu item click
    /// </summary>
    private void OpenLogFolder_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Get log file path
            var logger = LoggingService.Instance;
            string logFilePath = logger.GetLogFilePath();
            string? logDirectory = Path.GetDirectoryName(logFilePath);
            
            if (!string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory))
            {
                // Open folder in file explorer
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
            Console.WriteLine($"Failed to open log folder: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle about menu item click
    /// </summary>
    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        // Create about dialog
        var aboutDialog = new Window
        {
            Title = "About VideoVault",
            Width = 450,
            Height = 300,
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
                        Text = "Version 1.0.0 - Phase 1", 
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
                        Text = "• Recursive video file scanning\n• SQLite database catalog\n• Duplicate file detection\n• Comprehensive logging\n• Cross-platform (Windows, Linux, macOS)",
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
}
