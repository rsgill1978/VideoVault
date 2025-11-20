# VideoVault Developer Guide

## Project Structure

```
VideoVault/
├── Models/
│   ├── AppSettings.cs          # JSON settings management
│   └── VideoFile.cs            # Video file data model with deletion marking
├── Services/
│   ├── DatabaseService.cs      # SQLite database operations
│   ├── FileScannerService.cs   # Directory scanning and file processing
│   ├── DuplicateFinderService.cs # Duplicate detection logic
│   ├── VideoPlayerService.cs   # Video playback service (Phase 2)
│   └── LoggingService.cs       # Application logging
├── App.axaml                   # Application definition
├── App.axaml.cs                # Application code-behind
├── MainWindow.axaml            # Main window UI definition
├── MainWindow.axaml.cs         # Main window code-behind
├── MainWindowViewModel.cs      # Main window view model
├── VideoPlayerControl.axaml    # Video player UI control (Phase 2)
├── VideoPlayerControl.axaml.cs # Video player control logic (Phase 2)
├── SettingsWindow.axaml        # Settings dialog UI
├── SettingsWindow.axaml.cs     # Settings dialog logic
├── SettingsWindowViewModel.cs  # Settings view model
├── Program.cs                  # Application entry point
├── VideoVault.csproj           # Project file
├── build.ps1                   # PowerShell build script
├── README.md                   # User documentation
├── QUICK_START.md              # Quick start guide
├── PHASE2_CHANGES.md           # Phase 2 changes summary
└── .gitignore                  # Git ignore file
```

## Phase 2 Architecture

### Video Player Component

```
┌─────────────────────────────────────┐
│       MainWindow                    │
│  ┌──────────────────────────────┐   │
│  │   VideoPlayerControl         │   │
│  │  ┌────────────────────────┐  │   │
│  │  │  VideoPlayerService    │  │   │
│  │  │  ┌──────────────────┐  │  │   │
│  │  │  │   LibVLC Core    │  │  │   │
│  │  │  └──────────────────┘  │  │   │
│  │  └────────────────────────┘  │   │
│  └──────────────────────────────┘   │
└─────────────────────────────────────┘
```

### Duplicate Deletion Flow

```
User Action → MainWindow → MainWindowViewModel
                              ↓
                    DeleteMarkedDuplicatesAsync
                              ↓
                    ┌─────────┴─────────┐
                    ↓                   ↓
            DatabaseService      File System
                 (Delete)          (Delete)
                    ↓                   ↓
                    └─────────┬─────────┘
                              ↓
                        Reload Videos
                              ↓
                         Update UI
```

## Code Standards

### Comments
- Comments are placed above code lines, never on the same line
- Comments explain logic and implementation
- Written in Simplified Technical English
- Not every line requires a comment
- Focus on WHY code does something, not WHAT it does

### Naming Conventions
- Classes: PascalCase (e.g., `VideoPlayerService`)
- Methods: PascalCase (e.g., `LoadVideo`)
- Private fields: _camelCase (e.g., `_mediaPlayer`)
- Properties: PascalCase (e.g., `IsPlaying`)
- Local variables: camelCase (e.g., `filePath`)
- Event handlers: PascalCase with suffix (e.g., `OnTimeChanged`)

### Async/Await
- All I/O operations are asynchronous
- Method names end with `Async` suffix
- Use `Task` or `Task<T>` return types
- Properly handle cancellation tokens

## Key Components

### VideoPlayerService

Manages video playback using LibVLC:
- Initializes LibVLC core
- Creates and manages MediaPlayer
- Handles playback control
- Manages volume and position
- Fires events for UI updates

Key methods:
- `Initialize()` - Initialize LibVLC core
- `Play(string filePath)` - Load and play video
- `Pause()` / `Resume()` - Control playback
- `Stop()` - Stop playback
- `SetPosition(float)` - Seek to position
- `SetVolume(int)` - Adjust volume

Events:
- `PositionChanged` - Playback position updated
- `TimeChanged` - Playback time updated
- `EndReached` - Video ended

### VideoPlayerControl

UI control for video player:
- Embeds video display
- Provides playback controls
- Manages fullscreen mode
- Updates UI in real-time

Key features:
- Auto-updates every 100ms
- Thread-safe UI updates
- Keyboard shortcuts support
- Fullscreen toggling

### Duplicate Deletion

Enhanced `VideoFile` model:
```csharp
public class VideoFile : INotifyPropertyChanged
{
    public bool IsMarkedForDeletion { get; set; }
    // ... other properties
}
```

New ViewModel method:
```csharp
public async Task DeleteMarkedDuplicatesAsync(
    DuplicateGroup group, 
    List<VideoFile> filesToDelete)
{
    // Delete from filesystem
    // Delete from database
    // Refresh UI
}
```

## Threading Model

### UI Thread
- All UI updates occur on UI thread
- Avalonia handles marshalling automatically
- ObservableCollection auto-marshals

### Background Threads
- File I/O on background threads
- Database operations async
- Video playback on LibVLC threads
- Hash calculations on background threads

### Video Player Threads
- LibVLC manages playback threads
- Events marshaled to UI thread
- Timer for UI updates (100ms interval)

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
- `idx_filehash` on `FileHash` for duplicate detection

## LibVLC Integration

### Initialization
```csharp
Core.Initialize();
_libVLC = new LibVLC(enableDebugLogs: false);
_mediaPlayer = new MediaPlayer(_libVLC);
```

### Playing Video
```csharp
using var media = new Media(_libVLC, filePath, FromType.FromPath);
_mediaPlayer.Media = media;
_mediaPlayer.Play();
```

### Event Handling
```csharp
_mediaPlayer.PositionChanged += OnPositionChanged;
_mediaPlayer.TimeChanged += OnTimeChanged;
_mediaPlayer.EndReached += OnEndReached;
```

## Error Handling

### Video Player Errors
- Catch `FileNotFoundException` for missing files
- Handle `InvalidOperationException` for uninitialized player
- Log all errors to LoggingService

### Deletion Errors
- Try-catch around file deletion
- Continue even if some files fail
- Report success/failure counts
- Update UI regardless of errors

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

## Extending Phase 2

### Adding Video Player Features

1. **Add UI control** in `VideoPlayerControl.axaml`
2. **Add event handler** in `VideoPlayerControl.axaml.cs`
3. **Add service method** in `VideoPlayerService.cs` if needed
4. **Update documentation**

Example - Adding playback speed control:
```csharp
// In VideoPlayerService.cs
public void SetPlaybackRate(float rate)
{
    if (_mediaPlayer != null)
    {
        _mediaPlayer.SetRate(rate);
    }
}

// In VideoPlayerControl.axaml.cs
private void SpeedButton_Click(object? sender, RoutedEventArgs e)
{
    _playerService?.SetPlaybackRate(1.5f);
}
```

### Adding Deletion Features

1. **Update UI** in `MainWindow.axaml` duplicate group template
2. **Add handler** in `MainWindow.axaml.cs`
3. **Update ViewModel** in `MainWindowViewModel.cs`
4. **Test thoroughly** - deletion is permanent!

## Performance Considerations

### Video Playback
- LibVLC handles decoding efficiently
- Hardware acceleration enabled by default
- UI updates limited to 100ms interval

### File Deletion
- Async operations prevent UI blocking
- Individual file try-catch for resilience
- Database operations batched when possible

### Memory Management
- Dispose MediaPlayer properly
- Release media resources
- Clean up event handlers

## Security Considerations

### File Deletion
- Always confirm before deletion
- Validate file selections
- Enforce "keep at least one" rule
- Log all deletion operations

### Video Player
- Validate file paths
- Handle malformed media gracefully
- Don't trust file extensions
- Sanitize user input

## Debugging Tips

### Video Player Issues
1. Check LibVLC initialization
2. Verify media file exists
3. Check file format support
4. Review LibVLC logs

### Deletion Issues
1. Check file permissions
2. Verify file isn't open
3. Check database state
4. Review application logs

### UI Issues
1. Check data binding
2. Verify thread marshalling
3. Check property notifications
4. Review XAML for errors

## Phase 3 Preparation

### Thumbnail Generation
- Research FFmpeg integration
- Plan thumbnail storage
- Design cache structure
- Consider async generation

### Implementation Approach
1. Add FFmpeg NuGet package
2. Create ThumbnailService
3. Generate thumbnails during scan
4. Store in database or cache
5. Display in UI grid

## Testing Checklist

### Video Player
- [ ] Plays various formats
- [ ] Play/pause works
- [ ] Seeking works smoothly
- [ ] Volume control responsive
- [ ] Fullscreen toggles correctly
- [ ] Time display accurate
- [ ] Handles errors gracefully

### Duplicate Deletion
- [ ] Finds duplicates correctly
- [ ] UI displays checkboxes
- [ ] Validates selections
- [ ] Shows confirmation dialog
- [ ] Deletes from filesystem
- [ ] Updates database
- [ ] Refreshes UI
- [ ] Logs operations

## Common Tasks

### Add New Video Format Support
1. Open `Models/AppSettings.cs`
2. Add extension to `VideoExtensions` list
3. Rebuild application

### Customize Player Controls
1. Edit `VideoPlayerControl.axaml`
2. Add event handlers in `.axaml.cs`
3. Update service methods if needed

### Add Deletion Safeguards
1. Update validation in `MainWindow.axaml.cs`
2. Add checks in `MainWindowViewModel.cs`
3. Update confirmation dialogs

## Resources

- LibVLCSharp docs: https://code.videolan.org/videolan/LibVLCSharp
- Avalonia docs: https://docs.avaloniaui.net/
- .NET docs: https://docs.microsoft.com/dotnet/

## Contact

For questions or contributions:
https://github.com/rsgill1978/VideoVault
