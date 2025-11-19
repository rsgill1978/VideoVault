using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Linq;

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
}
