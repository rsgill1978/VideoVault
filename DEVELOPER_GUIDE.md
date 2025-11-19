# VideoVault Developer Guide

## Project Structure

```
VideoVault/
├── Models/
│   ├── AppSettings.cs          # JSON settings management
│   └── VideoFile.cs            # Video file data model
├── Services/
│   ├── DatabaseService.cs      # SQLite database operations
│   ├── FileScannerService.cs   # Directory scanning and file processing
│   └── DuplicateFinderService.cs # Duplicate detection logic
├── App.axaml                   # Application definition
├── App.axaml.cs                # Application code-behind
├── MainWindow.axaml            # Main window UI definition
├── MainWindow.axaml.cs         # Main window code-behind
├── MainWindowViewModel.cs      # Main window view model
├── Program.cs                  # Application entry point
├── VideoVault.csproj           # Project file
├── build.ps1                   # PowerShell build script
├── README.md                   # User documentation
└── .gitignore                  # Git ignore file
```

## Code Standards

### Comments
- Comments are placed above code lines, never on the same line
- Comments explain logic and implementation
- Written in Simplified Technical English
- Not every line requires a comment
- Focus on WHY code does something, not WHAT it does

### Naming Conventions
- Classes: PascalCase (e.g., `DatabaseService`)
- Methods: PascalCase (e.g., `GetAllVideosAsync`)
- Private fields: _camelCase (e.g., `_databaseService`)
- Properties: PascalCase (e.g., `VideoPath`)
- Local variables: camelCase (e.g., `videoFiles`)

### Async/Await
- All I/O operations are asynchronous
- Method names end with `Async` suffix
- Use `Task` or `Task<T>` return types
- Properly handle cancellation tokens

## Key Components

### DatabaseService

Manages all SQLite database operations including:
- Table creation and initialization
- CRUD operations for video files
- Duplicate detection queries
- Database connection management

Key methods:
- `AddVideoFileAsync()` - Add new video to database
- `GetAllVideosAsync()` - Retrieve all videos
- `FindDuplicatesByHashAsync()` - Find duplicates by hash
- `MarkAsDuplicateAsync()` - Mark file as duplicate

### FileScannerService

Handles directory scanning and file processing:
- Recursive directory traversal
- Video file filtering by extension
- SHA256 hash calculation
- Progress reporting

Key methods:
- `ScanDirectoryAsync()` - Scan directory recursively
- `ProcessFilesAsync()` - Process found video files
- `CalculateFileHashAsync()` - Calculate SHA256 hash

Events:
- `ProgressChanged` - Reports scan progress
- `FileProcessed` - Notifies when file is processed

### DuplicateFinderService

Manages duplicate detection and removal:
- Hash-based duplicate detection
- Similarity scoring
- Duplicate group management
- File deletion

Key methods:
- `FindDuplicatesAsync()` - Find all duplicates
- `MarkDuplicatesAsync()` - Mark files as duplicates
- `DeleteDuplicatesAsync()` - Delete duplicate files

### MainWindowViewModel

MVVM view model providing:
- Data binding for UI
- Command execution
- Property change notifications
- Background task management

Key properties:
- `Videos` - Observable collection of videos
- `DuplicateGroups` - Observable collection of duplicate groups
- `ScanProgress` / `ScanTotal` - Progress tracking
- `IsScanning` / `IsFindingDuplicates` - Operation flags

Key methods:
- `StartScanAsync()` - Begin directory scan
- `FindDuplicatesAsync()` - Begin duplicate detection
- `CancelScan()` - Cancel ongoing operation

## Threading Model

### UI Thread
- All UI updates occur on the UI thread
- Avalonia handles thread marshalling automatically
- ObservableCollection automatically marshals to UI thread

### Background Threads
- File I/O operations run on background threads
- Database operations run on background threads
- Hash calculations run on background threads

### Cancellation
- All long-running operations support cancellation
- `CancellationTokenSource` manages cancellation
- Operations check for cancellation regularly

## Database Schema

### VideoFiles Table
```sql
CREATE TABLE VideoFiles (
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
)
```

Indexes:
- `idx_filehash` on `FileHash` column for fast duplicate detection

## Settings Management

Settings are stored as JSON in the application data directory:

```json
{
  "LastVideoPath": "C:\\Videos",
  "WindowWidth": 1200,
  "WindowHeight": 800,
  "VideoExtensions": [".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v"],
  "DuplicateThreshold": 95
}
```

Settings persist between application sessions.

## Error Handling

### General Principles
- Use try-catch blocks for all I/O operations
- Log errors to console
- Display user-friendly messages in UI
- Continue operation when possible

### Specific Scenarios
- **Missing directory access**: Skip directory and continue
- **File in use**: Log error and skip file
- **Database error**: Display error message to user
- **Cancelled operation**: Clean up and reset UI

## Building and Testing

### Development Build
```powershell
dotnet build
```

### Run in Development
```powershell
dotnet run
```

### Release Build
```powershell
.\build.ps1
```

### Platform-Specific Build
```powershell
.\build.ps1 -Platform Windows
.\build.ps1 -Platform Linux
.\build.ps1 -Platform macOS
```

## Extending the Application

### Adding New Video Formats
1. Open `Models/AppSettings.cs`
2. Add extension to `VideoExtensions` list
3. Rebuild application

### Adding Database Fields
1. Update `VideoFile` model in `Models/VideoFile.cs`
2. Update SQL schema in `DatabaseService.InitializeDatabase()`
3. Update `MapReaderToVideoFile()` method
4. Update insert/update queries

### Adding UI Features
1. Add properties to `MainWindowViewModel`
2. Update `MainWindow.axaml` with new UI elements
3. Add event handlers in `MainWindow.axaml.cs`
4. Implement logic in view model

## Common Tasks

### Add Tooltip to UI Element
```xml
<Button Content="Click Me" 
        ToolTip.Tip="This button does something"/>
```

### Add New Property with Notification
```csharp
private string _myProperty = string.Empty;

public string MyProperty
{
    get => _myProperty;
    set
    {
        if (_myProperty != value)
        {
            _myProperty = value;
            OnPropertyChanged();
        }
    }
}
```

### Run Background Task
```csharp
public async Task MyBackgroundTaskAsync()
{
    await Task.Run(() =>
    {
        // Long-running operation here
    });
}
```

## Troubleshooting

### Application won't start
- Check .NET 8.0 SDK is installed
- Verify all NuGet packages restored
- Check for missing dependencies

### Database errors
- Delete `videovault.db` file to reset
- Check application data directory permissions
- Verify SQLite package is installed

### UI not updating
- Ensure property implements `INotifyPropertyChanged`
- Use `ObservableCollection` for lists
- Check data binding syntax in XAML

### Build fails
- Run `dotnet restore` to restore packages
- Check for syntax errors
- Verify .NET SDK version

## Performance Considerations

### File Scanning
- Scanning is I/O bound, limited by disk speed
- Use background threads to keep UI responsive
- Consider caching file lists for frequently scanned directories

### Hash Calculation
- SHA256 is CPU intensive for large files
- Calculate hashes on background threads
- Consider parallel processing for multiple files

### Database Operations
- Use parameterized queries to prevent SQL injection
- Create indexes on frequently queried columns
- Batch insert operations when possible

## Security Considerations

### File Access
- Validate all file paths before access
- Handle unauthorized access exceptions
- Don't trust user input for paths

### Database
- Use parameterized queries exclusively
- Validate all input data
- Handle SQL exceptions gracefully

### User Data
- Store sensitive settings securely
- Don't log file paths in production
- Respect user privacy

## Future Development

### Phase 2 Preparation
- Research video player libraries (LibVLCSharp)
- Plan fullscreen and minimized modes
- Design player controls UI

### Phase 3 Preparation
- Research FFmpeg integration
- Plan thumbnail cache structure
- Design thumbnail display grid

### Phase 4 Preparation
- Identify metadata sources
- Design scraping architecture
- Plan rate limiting and caching

## Contact

For questions or contributions, visit:
https://github.com/rsgill1978/VideoVault
