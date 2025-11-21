using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using VideoVault.Models;
using VideoVault.Services;
using VideoVault.ViewModels;

namespace VideoVault.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsWindowViewModel _viewModel;
    private readonly LoggingService _logger;

    /// <summary>
    /// Initialize settings window
    /// </summary>
    public SettingsWindow() : this(new AppSettings())
    {
    }

    /// <summary>
    /// Initialize settings window with existing settings
    /// </summary>
    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        
        _settings = settings;
        _logger = LoggingService.Instance;
        _viewModel = new SettingsWindowViewModel(settings);
        
        DataContext = _viewModel;
        
        _logger.LogInfo("Settings window opened");
    }

    /// <summary>
    /// Handle save button click
    /// </summary>
    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInfo("Saving settings...");
            
            // Apply settings from view model to settings object
            _viewModel.ApplySettings();
            
            // Save settings to file
            _settings.Save();
            
            _logger.LogInfo("Settings saved successfully");
            
            // Update logging level immediately
            if (Enum.TryParse<LogLevel>(_settings.LogLevel, out var logLevel))
            {
                LoggingService.Instance.SetMinimumLevel(logLevel);
            }
            
            // Close window with success result
            Close(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save settings", ex);
            
            // Show error message to user
            var messageBox = new Window
            {
                Title = "Error",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock { Text = "Failed to save settings:", FontWeight = Avalonia.Media.FontWeight.Bold },
                        new TextBlock { Text = ex.Message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new Button 
                        { 
                            Content = "OK", 
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Width = 100
                        }
                    }
                }
            };
            
            messageBox.ShowDialog(this);
        }
    }

    /// <summary>
    /// Handle cancel button click
    /// </summary>
    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        _logger.LogInfo("Settings window cancelled");
        
        // Close window without saving
        Close(false);
    }

    /// <summary>
    /// Handle restore defaults button click
    /// </summary>
    private void RestoreDefaults_Click(object? sender, RoutedEventArgs e)
    {
        _logger.LogInfo("Restoring default settings");
        
        // Restore default values in view model
        _viewModel.RestoreDefaults();
    }

    /// <summary>
    /// Handle open log folder button click
    /// </summary>
    private void OpenLogFolder_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Get log file path
            string logFilePath = _logger.GetLogFilePath();
            string? logDirectory = Path.GetDirectoryName(logFilePath);
            
            if (!string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory))
            {
                _logger.LogInfo($"Opening log folder: {logDirectory}");
                
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
            else
            {
                _logger.LogWarning("Log directory does not exist");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open log folder", ex);
        }
    }
}
