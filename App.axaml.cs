using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using VideoVault.Services;
using VideoVault.Views;
using VideoVault.ViewModels;

namespace VideoVault;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var logger = LoggingService.Instance;
        logger.LogInfo("Application framework initialization started");
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Set shutdown mode to prevent app from closing prematurely
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            
            try
            {
                // Create view model and main window
                var viewModel = new MainWindowViewModel();
                var mainWindow = new MainWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    DataContext = viewModel
                };
                
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                mainWindow.Activate();
                
                logger.LogInfo("Main window created and displayed successfully");
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to create main window", ex);
                Console.WriteLine($"CRITICAL ERROR: Failed to initialize: {ex.Message}");
                
                // Try to create a fallback window
                try
                {
                    var fallbackWindow = new MainWindow
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    desktop.MainWindow = fallbackWindow;
                    fallbackWindow.Show();
                    fallbackWindow.Activate();
                    logger.LogInfo("Fallback window created");
                }
                catch (Exception ex2)
                {
                    logger.LogCritical("Failed to create fallback window", ex2);
                }
            }
        }

        base.OnFrameworkInitializationCompleted();
        logger.LogInfo("Application framework initialization completed");
    }
}
