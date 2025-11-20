# VideoVault - Adult Video Catalog Application

## Overview

VideoVault is a cross-platform desktop application for cataloging and organizing adult video files. Built with C# and Avalonia UI, it runs on Windows, Linux, and macOS.

## Current Version: Phase 2

This is Phase 2 of the VideoVault project, which includes all Phase 1 features plus:

### Phase 1 Features (Complete)
- **Project Scaffolding**: Complete cross-platform C# application structure
- **Logging System**: Comprehensive file-based logging with configurable levels
- **Settings UI**: Full settings configuration interface
- **File Browser**: Browse and select video directories
- **Recursive Scanning**: Scan directories recursively for video files
- **Database Catalog**: Automatic SQLite database creation and management
- **Progress Tracking**: Visual progress bars for all background operations
- **Duplicate Detection**: Find duplicate files using SHA256 hash comparison
- **Multi-threaded**: Non-blocking UI with background task processing
- **Settings Persistence**: JSON-based settings storage with UI editor
- **Menu System**: Comprehensive menu bar with File, Tools, and Help menus

### Phase 2 Features (NEW!)
- **Embedded Video Player**: Full-featured video player using LibVLC
- **Playback Controls**: Play, pause, stop, seek, and volume control
- **Fullscreen Mode**: Double-click video or use fullscreen button
- **Time Display**: Current time and duration display
- **Progress Seeking**: Click and drag to seek through video
- **Delete Duplicates**: Select and delete duplicate files from the application
- **Confirmation Dialogs**: Safe deletion with user confirmation

## System Requirements

- .NET 8.0 SDK or later
- Windows 10/11, Linux (Mint or other distributions), or macOS 10.15+
- PowerShell 5.1 or later (for build script)
- LibVLC 3.0.20 (automatically installed via NuGet packages)

## Building the Application

### Prerequisites

1. Install .NET 8.0 SDK from https://dotnet.microsoft.com/download
2. Ensure PowerShell is available on your system
3. Run `dotnet restore` to install all dependencies including LibVLC

### Build Commands

To build for all platforms:
```powershell
.\build.ps1
```

To build for a specific platform:
```powershell
.\build.ps1 -Platform Windows
.\build.ps1 -Platform Linux
.\build.ps1 -Platform macOS
```

To clean and rebuild:
```powershell
.\build.ps1 -Clean
```

### Build Output

Compiled binaries will be located in:
- Windows: `bin/Release/win-x64/VideoVault.exe`
- Linux: `bin/Release/linux-x64/VideoVault`
- macOS Intel: `bin/Release/osx-x64/VideoVault`
- macOS Apple Silicon: `bin/Release/osx-arm64/VideoVault`

## Running the Application

### Windows
```cmd
bin\Release\win-x64\VideoVault.exe
```

### Linux
```bash
chmod +x bin/Release/linux-x64/VideoVault
./bin/Release/linux-x64/VideoVault
```

### macOS
```bash
chmod +x bin/Release/osx-x64/VideoVault
./bin/Release/osx-x64/VideoVault
```

## Using the Application

### Adding Videos

1. Enter or browse to a directory containing video files
2. Click the "Scan" button to start scanning
3. The application will recursively scan all subdirectories
4. Progress will be displayed in the progress bar
5. Videos will appear in the library as they are processed

### Playing Videos

1. Select a video from the library list on the left
2. The video will automatically load in the player
3. Use the playback controls:
   - **Play/Pause**: Click the play button or press spacebar
   - **Seek**: Drag the progress slider
   - **Volume**: Adjust the volume slider or click the volume button to mute
   - **Fullscreen**: Click the fullscreen button or double-click the video
   - **Exit Fullscreen**: Press Escape key

### Finding and Deleting Duplicates

1. Ensure you have videos in your library
2. Click the "Find Duplicates" button
3. Duplicate groups will appear in the right panel
4. Each group shows files with identical content (based on SHA256 hash)
5. Check the boxes next to files you want to delete
6. Click "Delete Selected" to remove the files
7. Confirm the deletion when prompted

**Important**: You must keep at least one file from each duplicate group.

### Configuring Settings

1. Click the "Settings" button or go to File > Settings
2. Adjust settings as needed:
   - **Window Size**: Set default window dimensions
   - **Video Extensions**: Customize supported video formats
   - **Duplicate Threshold**: Adjust duplicate detection sensitivity
   - **Logging Level**: Control log verbosity (Debug, Info, Warning, Error, Critical)
   - **Log Retention**: Set how many days to keep old logs
3. Click "Save" to apply changes
4. Click "Restore Defaults" to reset all settings

### Supported Video Formats

- MP4 (.mp4)
- AVI (.avi)
- MKV (.mkv)
- MOV (.mov)
- WMV (.wmv)
- FLV (.flv)
- WebM (.webm)
- M4V (.m4v)

## Application Structure

### Models
- `VideoFile`: Represents a video file in the catalog
- `AppSettings`: Application configuration and preferences

### Services
- `DatabaseService`: Manages SQLite database operations
- `FileScannerService`: Scans directories and processes video files
- `DuplicateFinderService`: Detects and manages duplicate files
- `VideoPlayerService`: Manages video playback using LibVLC
- `LoggingService`: Application-wide logging system

### UI
- `MainWindow`: Primary application window with all features
- `MainWindowViewModel`: MVVM view model for data binding
- `VideoPlayerControl`: Embedded video player control
- `SettingsWindow`: Settings configuration dialog

## Data Storage

### Database
- Location: `%AppData%/VideoVault/videovault.db` (Windows)
- Location: `~/.config/VideoVault/videovault.db` (Linux/macOS)
- Type: SQLite
- Contains: Video file metadata, file hashes, duplicate relationships

### Settings
- Location: `%AppData%/VideoVault/settings.json` (Windows)
- Location: `~/.config/VideoVault/settings.json` (Linux/macOS)
- Format: JSON
- Contains: User preferences, window size, last used paths

### Log Files
- Location: `%AppData%/VideoVault/Logs/` (Windows)
- Location: `~/.config/VideoVault/Logs/` (Linux/macOS)
- Format: Text files with timestamps
- Naming: `VideoVault_YYYY-MM-DD_HH-mm-ss.log`
- Retention: Configurable (default 30 days)

## Technical Details

### Architecture
- **Framework**: .NET 8.0
- **UI Framework**: Avalonia UI 11.0
- **Database**: SQLite with Microsoft.Data.Sqlite
- **Video Player**: LibVLCSharp 3.8.5
- **Pattern**: MVVM (Model-View-ViewModel)

### Multi-threading
- File scanning runs on background threads
- UI remains responsive during all operations
- Cancellation support for long-running operations
- Video playback runs on separate thread

### Video Playback
- Uses LibVLC media player engine
- Hardware acceleration support
- Wide format compatibility
- Subtitle support (automatic)
- Audio track selection

## Future Phases

### Phase 3: Thumbnail Generation
- Auto-generate video thumbnails
- Display thumbnails in library
- Thumbnail cache management

### Phase 4: Web Scraping & Metadata
- Scrape metadata from online sources
- Automatic information enrichment
- Performer identification
- Act cataloging

### Phase 5: Metadata Editing
- Edit video metadata
- Add custom tags and notes
- Performer management

### Phase 6: Advanced Search
- Search by performer
- Search by acts
- Advanced filtering options
- Saved search queries

## Troubleshooting

### Video Player Issues

**Videos won't play:**
- Ensure LibVLC is properly installed (it should be automatic via NuGet)
- Check that the video file format is supported
- Try running the application as administrator (Windows)

**No audio:**
- Check volume slider in player
- Verify system audio is not muted
- Check audio device settings

**Performance issues:**
- Close other applications
- Reduce video quality/resolution
- Check system resources

### General Issues

**Application won't start:**
- Check .NET 8.0 SDK is installed
- Verify all NuGet packages restored
- Check for missing dependencies

**Database errors:**
- Delete `videovault.db` file to reset
- Check application data directory permissions
- Verify SQLite package is installed

**UI not updating:**
- Ensure property implements `INotifyPropertyChanged`
- Check data binding syntax in XAML
- Restart application

## License

This project is intended for personal use. Please ensure compliance with local laws regarding adult content.

## Support

For issues or questions, please refer to the project repository at:
https://github.com/rsgill1978/VideoVault

## Version History

- **v1.0.0-Phase2** (Current): Video player, duplicate deletion
- **v1.0.0-Phase1**: Initial release with scanning and duplicate detection

## Acknowledgments

- LibVLC for video playback capabilities
- Avalonia UI team for the cross-platform framework
- SQLite for the database engine
