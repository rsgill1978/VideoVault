using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VideoVault.Models;

namespace VideoVault.Services;

/// <summary>
/// Service for detecting and managing duplicate video files
/// </summary>
public class DuplicateFinderService
{
    private readonly DatabaseService _databaseService;

    public DuplicateFinderService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    /// <summary>
    /// Event for reporting duplicate detection progress
    /// </summary>
    public event Action<int, int, string>? ProgressChanged;

    /// <summary>
    /// Event for reporting found duplicates
    /// </summary>
    public event Action<DuplicateGroup>? DuplicateFound;

    /// <summary>
    /// Find all duplicate files in the database
    /// </summary>
    public async Task<List<DuplicateGroup>> FindDuplicatesAsync(CancellationToken cancellationToken)
    {
        var duplicateGroups = new List<DuplicateGroup>();

        // Get all videos from database
        var allVideos = await _databaseService.GetAllVideosAsync();

        // Group videos by hash
        var hashGroups = allVideos.GroupBy(v => v.FileHash)
                                  .Where(g => g.Count() > 1)
                                  .ToList();

        int total = hashGroups.Count;
        int processed = 0;

        // Process each group of potential duplicates
        foreach (var group in hashGroups)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            processed++;
            ProgressChanged?.Invoke(processed, total, $"Analyzing group {processed}");

            // Create duplicate group
            var duplicateGroup = new DuplicateGroup
            {
                Files = group.ToList(),
                Hash = group.Key
            };

            // Calculate similarity scores
            CalculateSimilarityScores(duplicateGroup);

            duplicateGroups.Add(duplicateGroup);

            // Notify duplicate found
            DuplicateFound?.Invoke(duplicateGroup);
        }

        return duplicateGroups;
    }

    /// <summary>
    /// Calculate similarity scores between files in a duplicate group
    /// </summary>
    private void CalculateSimilarityScores(DuplicateGroup group)
    {
        // Files with identical hashes have 100% similarity
        foreach (var file in group.Files)
        {
            group.SimilarityScores[file.Id] = 100;
        }

        // Sort by file size (largest first) to identify best quality file
        group.Files = group.Files.OrderByDescending(f => f.FileSize).ToList();
    }

    /// <summary>
    /// Mark files as duplicates in the database
    /// </summary>
    public async Task MarkDuplicatesAsync(DuplicateGroup group, int keepFileId)
    {
        // Mark all files except the one to keep as duplicates
        foreach (var file in group.Files)
        {
            if (file.Id != keepFileId)
            {
                await _databaseService.MarkAsDuplicateAsync(file.Id, keepFileId);
            }
        }
    }

    /// <summary>
    /// Delete duplicate files from filesystem and database
    /// </summary>
    public async Task DeleteDuplicatesAsync(DuplicateGroup group, int keepFileId)
    {
        // Delete all files except the one to keep
        foreach (var file in group.Files)
        {
            if (file.Id != keepFileId)
            {
                try
                {
                    // Delete file from filesystem
                    if (File.Exists(file.FilePath))
                    {
                        File.Delete(file.FilePath);
                    }

                    // Delete from database
                    await _databaseService.DeleteVideoAsync(file.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {file.FilePath}: {ex.Message}");
                }
            }
        }
    }
}

/// <summary>
/// Represents a group of duplicate video files
/// </summary>
public class DuplicateGroup
{
    /// <summary>
    /// List of duplicate files
    /// </summary>
    public List<VideoFile> Files { get; set; } = new();

    /// <summary>
    /// Common hash for all files in the group
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Similarity scores for each file (file ID to score mapping)
    /// </summary>
    public Dictionary<int, int> SimilarityScores { get; set; } = new();
}
