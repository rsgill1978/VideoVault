using System;
using System.IO;

namespace VideoVault.Services;

/// <summary>
/// Logging levels for controlling log output detail
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Detailed debugging information
    /// </summary>
    Debug = 0,

    /// <summary>
    /// General informational messages
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning messages for potential issues
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error messages for failures
    /// </summary>
    Error = 3,

    /// <summary>
    /// Critical errors that may cause application failure
    /// </summary>
    Critical = 4
}

/// <summary>
/// Logging service for application-wide logging
/// </summary>
public class LoggingService
{
    private static LoggingService? _instance;
    private readonly string _logFilePath;
    private readonly object _lockObject = new object();
    private LogLevel _minimumLevel;

    private LoggingService()
    {
        // Get application data directory
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VideoVault",
            "Logs"
        );

        // Ensure directory exists
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        // Create log file with timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _logFilePath = Path.Combine(appDataPath, $"VideoVault_{timestamp}.log");

        // Set default logging level
        _minimumLevel = LogLevel.Info;

        // Write initial log entry
        LogInfo("VideoVault logging initialized");
        LogInfo($"Log file: {_logFilePath}");
    }

    /// <summary>
    /// Get singleton instance of logging service
    /// </summary>
    public static LoggingService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new LoggingService();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Set minimum logging level
    /// </summary>
    public void SetMinimumLevel(LogLevel level)
    {
        _minimumLevel = level;
        LogInfo($"Logging level set to: {level}");
    }

    /// <summary>
    /// Log debug message
    /// </summary>
    public void LogDebug(string message)
    {
        Log(LogLevel.Debug, message);
    }

    /// <summary>
    /// Log informational message
    /// </summary>
    public void LogInfo(string message)
    {
        Log(LogLevel.Info, message);
    }

    /// <summary>
    /// Log warning message
    /// </summary>
    public void LogWarning(string message)
    {
        Log(LogLevel.Warning, message);
    }

    /// <summary>
    /// Log error message
    /// </summary>
    public void LogError(string message)
    {
        Log(LogLevel.Error, message);
    }

    /// <summary>
    /// Log error message with exception details
    /// </summary>
    public void LogError(string message, Exception exception)
    {
        Log(LogLevel.Error, $"{message} - Exception: {exception.GetType().Name}: {exception.Message}");
        Log(LogLevel.Error, $"Stack trace: {exception.StackTrace}");
    }

    /// <summary>
    /// Log critical error message
    /// </summary>
    public void LogCritical(string message)
    {
        Log(LogLevel.Critical, message);
    }

    /// <summary>
    /// Log critical error message with exception details
    /// </summary>
    public void LogCritical(string message, Exception exception)
    {
        Log(LogLevel.Critical, $"{message} - Exception: {exception.GetType().Name}: {exception.Message}");
        Log(LogLevel.Critical, $"Stack trace: {exception.StackTrace}");
    }

    /// <summary>
    /// Write log entry to file
    /// </summary>
    private void Log(LogLevel level, string message)
    {
        // Check if message should be logged based on minimum level
        if (level < _minimumLevel)
        {
            return;
        }

        // Create log entry with timestamp and level
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string logLevel = level.ToString().ToUpper().PadRight(8);
        string logEntry = $"[{timestamp}] [{logLevel}] {message}";

        // Write to console for debugging
        Console.WriteLine(logEntry);

        // Write to file with thread safety
        lock (_lockObject)
        {
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // If logging fails, write to console only
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get path to current log file
    /// </summary>
    public string GetLogFilePath()
    {
        return _logFilePath;
    }

    /// <summary>
    /// Clean up old log files older than specified days
    /// </summary>
    public void CleanOldLogs(int daysToKeep = 30)
    {
        try
        {
            string logDirectory = Path.GetDirectoryName(_logFilePath) ?? string.Empty;
            if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
            {
                return;
            }

            // Get all log files
            var logFiles = Directory.GetFiles(logDirectory, "VideoVault_*.log");

            // Delete files older than specified days
            DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            int deletedCount = 0;

            foreach (var logFile in logFiles)
            {
                FileInfo fileInfo = new FileInfo(logFile);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(logFile);
                    deletedCount++;
                }
            }

            if (deletedCount > 0)
            {
                LogInfo($"Cleaned up {deletedCount} old log file(s)");
            }
        }
        catch (Exception ex)
        {
            LogError("Failed to clean old log files", ex);
        }
    }
}
