# VideoVault - Adult Video Catalog Application

## Overview

VideoVault is a cross-platform desktop application for cataloging and organizing adult video files. Built with C# and Avalonia UI, it runs on Windows, Linux, and macOS.

## Phase 1 Features

This is Phase 1 of the VideoVault project, which includes:

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

## System Requirements

- .NET 8.0 SDK or later
- Windows 10/11, Linux (Mint or other distributions), or macOS 10.15+
- PowerShell 5.1 or later (for build script)

## Building the Application

### Prerequisites

1. Install .NET 8.0 SDK from https://dotnet.microsoft.com/download
2. Ensure PowerShell is available on your system

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

### Finding Duplicates

1. Ensure you have videos in your library
2. Click the "Find Duplicates" button
3. Duplicate groups will appear in the right panel
4. Each group shows files with identical content (based on SHA256 hash)

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

### UI
- `MainWindow`: Primary application window with all features
- `MainWindowViewModel`: MVVM view model for data binding

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

## Logging System

The application includes a comprehensive logging system with configurable levels:

### Log Levels
- **Debug**: Detailed debugging information (property changes, detailed operations)
- **Info**: General informational messages (default level)
- **Warning**: Warning messages for potential issues
- **Error**: Error messages for failures
- **Critical**: Critical errors that may cause application failure

### Configuring Logging

Edit the `settings.json` file to configure logging:

```json
{
  "LogLevel": "Info",
  "LogRetentionDays": 30
}
```

Available log levels: `Debug`, `Info`, `Warning`, `Error`, `Critical`

### Viewing Logs

Log files are automatically created each time the application starts. To view logs:

**Windows:**
```
%AppData%\VideoVault\Logs\
```

**Linux/macOS:**
```
~/.config/VideoVault/Logs/
```

Logs include:
- Application startup and shutdown
- File scanning operations
- Database operations
- Error messages with stack traces
- Performance information

## Technical Details

### Architecture
- **Framework**: .NET 8.0
- **UI Framework**: Avalonia UI 11.0
- **Database**: SQLite with Microsoft.Data.Sqlite
- **Pattern**: MVVM (Model-View-ViewModel)

### Multi-threading
- File scanning runs on background threads
- UI remains responsive during all operations
- Cancellation support for long-running operations

### Duplicate Detection
- Uses SHA256 hashing for file comparison
- 100% accuracy for identical files
- Sorted by file size to identify best quality version

## Future Phases

### Phase 2: Video Player
- Embedded video player
- Full screen mode
- Minimized playback mode

### Phase 3: Thumbnail Generation
- Auto-generate video thumbnails
- Display thumbnails in library

### Phase 4: Web Scraping
- Scrape metadata from online sources
- Automatic information enrichment

### Phase 5: Metadata Editing
- Edit video metadata
- Add custom tags and notes

### Phase 6: Advanced Search
- Search by performer
- Search by acts
- Advanced filtering options

## License

This project is intended for personal use. Please ensure compliance with local laws regarding adult content.

## Support

For issues or questions, please refer to the project repository at:
https://github.com/rsgill1978/VideoVault

## Version

Current Version: 1.0.0-Phase1
Release Date: 2025
