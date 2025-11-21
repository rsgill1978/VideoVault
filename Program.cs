using System;
using Avalonia;
using Avalonia.Skia;  // Add this using directive

namespace VideoVault;

class Program
{
    // Application entry point
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("VideoVault starting...");
            
            var exitCode = BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            
            Console.WriteLine($"Application exited with code: {exitCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    // Configure Avalonia application
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseSkia()  // Enable Skia
            .WithSkiaOptions(new SkiaOptions { Backend = SkiaOptions.BackendType.OpenGl })  // Force OpenGL backend
            .LogToTrace();
    }
}