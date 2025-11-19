using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using VideoVault.Models;

namespace VideoVault.Services;

/// <summary>
/// Database service for managing video catalog using SQLite
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        // Get application data directory
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VideoVault"
        );

        // Ensure directory exists
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        // Set database file path
        string dbPath = Path.Combine(appDataPath, "videovault.db");
        _connectionString = $"Data Source={dbPath}";

        // Initialize database tables
        InitializeDatabase();
    }

    /// <summary>
    /// Initialize database tables if they do not exist
    /// </summary>
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Create VideoFiles table
        string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS VideoFiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FilePath TEXT NOT NULL UNIQUE,
                FileName TEXT NOT NULL,
                FileSize INTEGER NOT NULL,
                FileHash TEXT NOT NULL,
                DateAdded TEXT NOT NULL,
                Duration REAL DEFAULT 0,
                Resolution TEXT DEFAULT '',
                Extension TEXT NOT NULL,
                IsDuplicate INTEGER DEFAULT 0,
                OriginalFileId INTEGER NULL
            )";

        using var command = new SqliteCommand(createTableQuery, connection);
        command.ExecuteNonQuery();

        // Create index on FileHash for faster duplicate detection
        string createIndexQuery = "CREATE INDEX IF NOT EXISTS idx_filehash ON VideoFiles(FileHash)";
        using var indexCommand = new SqliteCommand(createIndexQuery, connection);
        indexCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Add a video file to the database
    /// </summary>
    public async Task<int> AddVideoFileAsync(VideoFile video)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Insert video file record
        string insertQuery = @"
            INSERT INTO VideoFiles (FilePath, FileName, FileSize, FileHash, DateAdded, Duration, Resolution, Extension, IsDuplicate, OriginalFileId)
            VALUES (@FilePath, @FileName, @FileSize, @FileHash, @DateAdded, @Duration, @Resolution, @Extension, @IsDuplicate, @OriginalFileId);
            SELECT last_insert_rowid();";

        using var command = new SqliteCommand(insertQuery, connection);
        command.Parameters.AddWithValue("@FilePath", video.FilePath);
        command.Parameters.AddWithValue("@FileName", video.FileName);
        command.Parameters.AddWithValue("@FileSize", video.FileSize);
        command.Parameters.AddWithValue("@FileHash", video.FileHash);
        command.Parameters.AddWithValue("@DateAdded", video.DateAdded.ToString("o"));
        command.Parameters.AddWithValue("@Duration", video.Duration);
        command.Parameters.AddWithValue("@Resolution", video.Resolution);
        command.Parameters.AddWithValue("@Extension", video.Extension);
        command.Parameters.AddWithValue("@IsDuplicate", video.IsDuplicate ? 1 : 0);
        command.Parameters.AddWithValue("@OriginalFileId", video.OriginalFileId.HasValue ? video.OriginalFileId.Value : DBNull.Value);

        // Execute query and get the new ID
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Get all video files from the database
    /// </summary>
    public async Task<List<VideoFile>> GetAllVideosAsync()
    {
        var videos = new List<VideoFile>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Select all video files
        string selectQuery = "SELECT * FROM VideoFiles ORDER BY DateAdded DESC";

        using var command = new SqliteCommand(selectQuery, connection);
        using var reader = await command.ExecuteReaderAsync();

        // Read all records
        while (await reader.ReadAsync())
        {
            videos.Add(MapReaderToVideoFile(reader));
        }

        return videos;
    }

    /// <summary>
    /// Check if a file already exists in the database by path
    /// </summary>
    public async Task<bool> FileExistsAsync(string filePath)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Check if file path exists
        string query = "SELECT COUNT(*) FROM VideoFiles WHERE FilePath = @FilePath";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@FilePath", filePath);

        // Execute query and check count
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// Find potential duplicate files based on hash
    /// </summary>
    public async Task<List<VideoFile>> FindDuplicatesByHashAsync(string hash)
    {
        var duplicates = new List<VideoFile>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Find all files with the same hash
        string query = "SELECT * FROM VideoFiles WHERE FileHash = @Hash";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@Hash", hash);

        using var reader = await command.ExecuteReaderAsync();

        // Read all matching records
        while (await reader.ReadAsync())
        {
            duplicates.Add(MapReaderToVideoFile(reader));
        }

        return duplicates;
    }

    /// <summary>
    /// Mark a file as duplicate
    /// </summary>
    public async Task MarkAsDuplicateAsync(int fileId, int originalFileId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Update duplicate flag and original file reference
        string updateQuery = @"
            UPDATE VideoFiles 
            SET IsDuplicate = 1, OriginalFileId = @OriginalFileId 
            WHERE Id = @Id";

        using var command = new SqliteCommand(updateQuery, connection);
        command.Parameters.AddWithValue("@Id", fileId);
        command.Parameters.AddWithValue("@OriginalFileId", originalFileId);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Delete a video file from the database
    /// </summary>
    public async Task DeleteVideoAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Delete video file record
        string deleteQuery = "DELETE FROM VideoFiles WHERE Id = @Id";

        using var command = new SqliteCommand(deleteQuery, connection);
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Map database reader to VideoFile object
    /// </summary>
    private VideoFile MapReaderToVideoFile(SqliteDataReader reader)
    {
        return new VideoFile
        {
            Id = reader.GetInt32(0),
            FilePath = reader.GetString(1),
            FileName = reader.GetString(2),
            FileSize = reader.GetInt64(3),
            FileHash = reader.GetString(4),
            DateAdded = DateTime.Parse(reader.GetString(5)),
            Duration = reader.GetDouble(6),
            Resolution = reader.GetString(7),
            Extension = reader.GetString(8),
            IsDuplicate = reader.GetInt32(9) == 1,
            OriginalFileId = reader.IsDBNull(10) ? null : reader.GetInt32(10)
        };
    }
}
