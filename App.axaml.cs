using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using VideoVault.Services;

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
        logger.LogInfo("OnFrameworkInitializationCompleted called");
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            logger.LogInfo("Desktop lifetime detected");
            
            // Set shutdown mode to prevent app from closing prematurely
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            logger.LogInfo("ShutdownMode set to OnMainWindowClose");
            
            try
            {
                logger.LogInfo("Creating MainWindowViewModel...");
                var viewModel = new MainWindowViewModel();
                logger.LogInfo("MainWindowViewModel created successfully");
                
                logger.LogInfo("Creating MainWindow...");
                var mainWindow = new MainWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                logger.LogInfo("MainWindow created successfully");
                
                logger.LogInfo("Assigning DataContext...");
                mainWindow.DataContext = viewModel;
                logger.LogInfo("DataContext assigned successfully");
                
                logger.LogInfo("Assigning to desktop.MainWindow...");
                desktop.MainWindow = mainWindow;
                logger.LogInfo("MainWindow assigned to desktop lifetime successfully");
                
                // Force show and activate the window
                logger.LogInfo("Calling Show() on MainWindow...");
                mainWindow.Show();
                logger.LogInfo("Show() called successfully");
                
                logger.LogInfo("Calling Activate() on MainWindow...");
                mainWindow.Activate();
                logger.LogInfo("Activate() called successfully");
                
                logger.LogInfo($"Window state after Show/Activate - IsVisible: {mainWindow.IsVisible}, WindowState: {mainWindow.WindowState}");
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to create MainWindow", ex);
                Console.WriteLine($"ERROR: Failed to initialize: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                try
                {
                    logger.LogInfo("Attempting to create fallback window...");
                    var fallbackWindow = new MainWindow
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    desktop.MainWindow = fallbackWindow;
                    fallbackWindow.Show();
                    fallbackWindow.Activate();
                    logger.LogInfo("Fallback MainWindow created and shown");
                }
                catch (Exception ex2)
                {
                    logger.LogError("Failed to create fallback window", ex2);
                    Console.WriteLine($"CRITICAL: Failed to create fallback window: {ex2.Message}");
                }
            }
        }
        else
        {
            logger.LogWarning("Not running as desktop application");
        }

        logger.LogInfo("Calling base.OnFrameworkInitializationCompleted");
        base.OnFrameworkInitializationCompleted();
        logger.LogInfo("OnFrameworkInitializationCompleted finished");
    }
}
