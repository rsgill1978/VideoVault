# Contributing to VideoVault

Thank you for your interest in contributing to VideoVault! This document provides technical information for developers.

---

## Table of Contents

1. [Development Setup](#development-setup)
2. [Project Structure](#project-structure)
3. [Architecture](#architecture)
4. [Coding Standards](#coding-standards)
5. [Building](#building)
6. [Testing](#testing)
7. [Pull Request Process](#pull-request-process)

---

## Development Setup

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- Git for version control
- PowerShell 5.1+ (for build scripts)

### Platform-Specific Requirements
- **Linux**: VLC media player (`sudo apt-get install vlc`)
- **macOS**: May need to approve security for dev builds

### Getting Started
```bash
# Clone the repository
git clone https://github.com/rsgill1978/VideoVault.git
cd VideoVault

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

---

## Project Structure

```
VideoVault/
├── README.md                      # User documentation
├── CHANGELOG.md                   # Version history
├── CONTRIBUTING.md                # This file
├── app.manifest                   # Windows application manifest
├── VideoVault.csproj              # Project configuration
├── build.ps1                      # Build script
│
├── Program.cs                     # Application entry point
├── App.axaml                      # Application XAML definition
├── App.axaml.cs                   # Application code-behind
│
├── Views/                         # Window views
│   ├── MainWindow.axaml           # Main window XAML
│   ├── MainWindow.axaml.cs        # Main window code-behind
│   ├── SettingsWindow.axaml       # Settings window XAML
│   └── SettingsWindow.axaml.cs    # Settings window code-behind
│
├── Controls/                      # Reusable UI controls
│   ├── VideoPlayerControl.axaml   # Video player XAML component
│   └── VideoPlayerControl.axaml.cs # Video player code-behind
│
├── ViewModels/                    # View models (MVVM pattern)
│   ├── MainWindowViewModel.cs     # Main window view model
│   └── SettingsWindowViewModel.cs # Settings view model
│
├── Models/                        # Data models
│   ├── VideoFile.cs               # Video file model
│   ├── AppSettings.cs             # Application settings
│   └── DuplicateGroup.cs          # Duplicate file grouping
│
└── Services/                      # Business logic services
    ├── VideoPlayerService.cs      # Video playback management
    ├── DatabaseService.cs         # Database operations
    ├── FileScannerService.cs      # File system scanning
    ├── DuplicateFinderService.cs  # Duplicate detection
    └── LoggingService.cs          # Logging functionality
```

---

## Architecture

### MVVM Pattern

VideoVault follows the Model-View-ViewModel pattern with organized folder structure:

#### Models (`Models/` folder)
Data structures representing domain objects:
- `VideoFile`: Represents a cataloged video file
- `AppSettings`: Application configuration
- `DuplicateGroup`: Group of duplicate files

#### Views (`Views/` folder)
Window AXAML files defining the visual layout:
- `MainWindow.axaml`: Main application window
- `SettingsWindow.axaml`: Settings dialog

**Namespace:** `VideoVault.Views`

#### Controls (`Controls/` folder)
Reusable UI components:
- `VideoPlayerControl.axaml`: Video player component

**Namespace:** `VideoVault.Controls`

#### ViewModels (`ViewModels/` folder)
Business logic and data binding:
- `MainWindowViewModel`: Manages main window state and operations
- `SettingsWindowViewModel`: Manages settings state
- Implement `INotifyPropertyChanged` for data binding

**Namespace:** `VideoVault.ViewModels`

#### Services (`Services/` folder)
Reusable business logic:
- `VideoPlayerService`: Encapsulates LibVLC operations
- `DatabaseService`: Provides database access
- `FileScannerService`: Handles file system operations
- `DuplicateFinderService`: Implements duplicate detection algorithms
- `LoggingService`: Centralized logging (Singleton pattern)

**Namespace:** `VideoVault.Services`

### Data Flow

```
User Action → View → ViewModel → Service → Database/FileSystem
                ↑                    ↓
                └────── Events ──────┘
```

### Threading Model

- **UI Thread**: Avalonia UI operations, data binding
- **Background Threads**: File scanning, hash calculation, database operations
- **Async/Await**: Used throughout for responsive UI
- **CancellationToken**: Supports operation cancellation

---

## Coding Standards

### General Guidelines

1. **NEVER REMOVE FUNCTIONALITY TO FIX A PROBLEM**
   - This is a critical project rule
   - Always find solutions that preserve existing features
   - If a feature must be disabled, make it temporary and document why

2. **Comments**
   - Use Simplified Technical English
   - Comments should explain logic and implementation
   - Place comments above code blocks (never inline on same line)
   - Not every line needs a comment - focus on complex logic
   - XML documentation for public APIs

3. **Code Style**
   - Follow C# coding conventions
   - Use meaningful names (no single letters except loop counters)
   - Keep methods focused and small (< 50 lines ideally)
   - Proper indentation (4 spaces)

### Example Comment Style

```csharp
// CORRECT: Comment above code, explains logic
// Calculate SHA256 hash of the file to detect duplicates
// Hash is computed in chunks to handle large files efficiently
using var sha256 = SHA256.Create();
var hash = sha256.ComputeHash(stream);

// INCORRECT: Inline comment
var hash = sha256.ComputeHash(stream); // compute hash
```

### Naming Conventions

- **Classes**: PascalCase (`VideoFile`, `DatabaseService`)
- **Methods**: PascalCase (`ScanDirectoryAsync`, `FindDuplicates`)
- **Private Fields**: camelCase with underscore (`_logger`, `_database`)
- **Properties**: PascalCase (`IsScanning`, `VideoPath`)
- **Local Variables**: camelCase (`fileName`, `videoCount`)
- **Constants**: PascalCase (`MaxRetries`, `DefaultExtensions`)

### Async/Await Guidelines

```csharp
// Use async suffix for async methods
public async Task<List<VideoFile>> GetAllVideosAsync()
{
    // Use await for async operations
    var videos = await _database.QueryAsync<VideoFile>();
    return videos;
}

// Cancel long-running operations
public async Task ScanAsync(CancellationToken cancellationToken)
{
    foreach (var file in files)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ProcessFileAsync(file);
    }
}
```

### Error Handling

```csharp
try
{
    // Log at start of operation
    _logger.LogInfo("Starting video scan");
    
    await ScanVideosAsync();
    
    // Log successful completion
    _logger.LogInfo("Video scan completed successfully");
}
catch (OperationCanceledException)
{
    // Handle cancellation separately
    _logger.LogWarning("Video scan cancelled by user");
}
catch (Exception ex)
{
    // Log error with exception
    _logger.LogError("Failed to scan videos", ex);
    
    // Re-throw or handle gracefully
    throw;
}
```

---

## Building

### Using Build Script (Recommended)

```powershell
# Build for all platforms
.\build.ps1

# Build for specific platform
.\build.ps1 -Platform Windows
.\build.ps1 -Platform Linux
.\build.ps1 -Platform macOS
```

### Using .NET CLI

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Platform-specific release
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained
```

### Output Locations

- **Windows**: `bin/Release/win-x64/VideoVault.exe`
- **Linux**: `bin/Release/linux-x64/VideoVault`
- **macOS**: `bin/Release/osx-x64/VideoVault`

---

## Testing

### Manual Testing Checklist

Before submitting changes, verify:

#### Basic Functionality
- [ ] Application starts without errors
- [ ] Main window displays correctly
- [ ] Can browse to a directory
- [ ] Scan operation works and shows progress
- [ ] Videos appear in library after scan
- [ ] Can open settings window
- [ ] Settings can be changed and saved

#### Video Player (Phase 2+)
- [ ] Video loads when selected
- [ ] Play/pause button works
- [ ] Seek slider functions correctly
- [ ] Volume controls work (mute, slider)
- [ ] Fullscreen mode activates
- [ ] ESC key exits fullscreen
- [ ] Video stays embedded in main window
- [ ] No overlay windows created

#### Duplicate Detection
- [ ] Find Duplicates menu item works
- [ ] Duplicates are correctly identified
- [ ] Can select files for deletion
- [ ] Deletion confirmation appears
- [ ] Files are deleted from disk and database
- [ ] Cannot delete all files in a group

#### Error Handling
- [ ] Invalid directory path handled gracefully
- [ ] Missing LibVLC libraries don't crash app
- [ ] Database errors are logged and handled
- [ ] Application logs errors appropriately

### Platform-Specific Testing

Test on all supported platforms when possible:
- Windows 10/11
- Ubuntu 20.04+ or equivalent Linux
- macOS 10.15+

---

## Pull Request Process

### Before Submitting

1. **Test Thoroughly**
   - Run full manual test checklist
   - Test on your platform
   - Check logs for errors

2. **Code Quality**
   - Follow coding standards
   - Add comments for complex logic
   - Ensure proper error handling
   - No debugging code left in

3. **Documentation**
   - Update README.md if adding features
   - Add entry to CHANGELOG.md
   - Update this file if architecture changes

### PR Guidelines

1. **Title**: Clear, concise description
   - Good: "Fix video player fullscreen overlay bug"
   - Bad: "Fixed bug"

2. **Description**: Include:
   - What the PR does
   - Why the change is needed
   - How it was tested
   - Any breaking changes
   - Related issues (if applicable)

3. **Commits**: 
   - Keep commits logical and atomic
   - Write meaningful commit messages
   - Squash work-in-progress commits

### Review Process

1. Maintainer reviews code
2. Feedback addressed
3. Approved PRs merged to main branch
4. Version number updated if needed

---

## Development Tips

### Debugging

#### Enable Debug Logging
In settings, set log level to "Debug" for verbose output.

#### Log File Location
- Windows: `%AppData%\VideoVault\Logs\`
- Linux/macOS: `~/.config/VideoVault/Logs/`

#### Attach Debugger
```bash
# VS Code: F5 or Run → Start Debugging
# Visual Studio: F5 or Debug → Start Debugging
# Rider: Shift+F9 or Run → Debug
```

### Common Issues

#### LibVLC Not Found (Linux)
```bash
sudo apt-get install vlc
# Or
sudo dnf install vlc
```

#### Database Locked
Stop all running instances of VideoVault before debugging.

#### XAML Hot Reload
Avalonia supports XAML hot reload in Visual Studio 2022.

---

## Questions?

- Open an issue on GitHub
- Check existing issues for answers
- Review logs for error details

---

**Thank you for contributing to VideoVault!**
