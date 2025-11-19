using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace VideoVault.Models;

/// <summary>
/// Application settings stored as JSON
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Last used video directory path
    /// </summary>
    public string LastVideoPath { get; set; } = string.Empty;

    /// <summary>
    /// Window width
    /// </summary>
    public double WindowWidth { get; set; } = 1200;

    /// <summary>
    /// Window height
    /// </summary>
    public double WindowHeight { get; set; } = 800;

    /// <summary>
    /// Supported video file extensions
    /// </summary>
    public List<string> VideoExtensions { get; set; } = new()
    {
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v"
    };

    /// <summary>
    /// Duplicate detection threshold (0-100)
    /// </summary>
    public int DuplicateThreshold { get; set; } = 95;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VideoVault",
        "settings.json"
    );

    /// <summary>
    /// Load settings from JSON file
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            // Check if settings file exists
            if (File.Exists(SettingsPath))
            {
                // Read JSON from file
                string json = File.ReadAllText(SettingsPath);
                
                // Deserialize JSON to AppSettings object
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }

        // Return default settings if file does not exist or error occurred
        return new AppSettings();
    }

    /// <summary>
    /// Save settings to JSON file
    /// </summary>
    public void Save()
    {
        try
        {
            // Ensure directory exists
            string? directory = Path.GetDirectoryName(SettingsPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Serialize object to JSON with indentation
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Write JSON to file
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}
