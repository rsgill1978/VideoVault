# Changelog

All notable changes to VideoVault will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0-Phase2-Hotfix1] - 2025-11-21

### Fixed
- **Fullscreen Video Player Controls Not Visible**
  - Fixed critical bug where playback controls were not visible in fullscreen mode
  - Controls were positioned below the video area instead of overlaying the video
  - Changed PlayerControls positioning from Grid.RowSpan=2 to Grid.RowSpan=1
  - Controls now correctly overlay the video at the bottom with VerticalAlignment.Bottom
  - Added proper margin (10px bottom) for visual spacing from screen edge
  - Controls maintain ZIndex=100 to render above video surface

### Changed
- **Enhanced Documentation**
  - Added comprehensive inline comments to VideoPlayerControl.axaml.cs
  - All fullscreen mode logic now thoroughly documented with implementation details
  - Added detailed XML comments to XAML file explaining layout structure
  - Added tooltips to all video player UI elements in XAML
  - Documented fullscreen mode behavior and control overlay mechanism
  - All comments positioned above code lines per coding standards

---

## [1.0.0-Phase2] - 2025-11-20

### Added
- **Video Player**
  - LibVLC-based embedded video player component
  - Play/pause/stop controls with visual feedback
  - Seek functionality via progress slider
  - Volume control with mute/unmute button
  - Volume adjustment slider (0-100)
  - Fullscreen mode (embedded in main window, not overlay)
  - Playback time display (current/total duration)
  - Double-click video area to toggle fullscreen
  - ESC key to exit fullscreen mode
  - Video player collapse/expand functionality with proper space management

- **Duplicate File Deletion**
  - Delete marked duplicate files from groups
  - Safety checks (must keep at least one file per group)
  - Confirmation dialogs before deletion
  - Automatic removal from both filesystem and database
  - UI refresh after successful deletion

- **Windows Compatibility**
  - Added app.manifest for Windows NativeControlHost support
  - Declared Windows 7-11 compatibility
  - DPI awareness configuration

- **Enhanced Error Handling**
  - Graceful degradation if video player fails to initialize
  - Comprehensive logging throughout video player lifecycle
  - User-friendly error messages
  - Application continues running even if video player unavailable

### Fixed
- **Phase 2 Bugs:**
  - Video player pane now properly collapses with video library expanding to fill space
  - Video plays embedded in NativeControlHost control (not in overlay covering entire window)
  - ESC key correctly exits fullscreen mode
  - Fullscreen uses main window instead of creating separate overlay window
  - Video playback never blocks access to main application window
  - Collapse button properly hides video player and allows other panes to expand

- **Critical Startup Issues:**
  - Fixed NativeControlHost crash on Windows due to missing app manifest
  - Added proper error handling in MainWindow constructor
  - Fixed null reference issues with ViewModel
  - Prevented app crash when database initialization fails
  - Added comprehensive logging for startup troubleshooting

- **Video Rendering Issues:**
  - Fixed LibVLC rendering to entire main window instead of video control
  - Properly attached video output to NativeControlHost handle
  - Video now renders only in the designated video player area

### Changed
- **Documentation Consolidation:**
  - Merged multiple .txt and .md files into comprehensive README.md
  - Created CHANGELOG.md for version history (this file)
  - Created CONTRIBUTING.md for developer documentation
  - Removed phase-specific documentation in favor of comprehensive guides
  - Cleaned up project root (minimal files)

- **Folder Reorganization:**
  - Organized all windows into `Views/` folder
  - Organized all reusable controls into `Controls/` folder
  - Organized all view models into `ViewModels/` folder
  - Updated all namespaces accordingly:
    - `VideoVault.Views` for windows
    - `VideoVault.Controls` for UI controls
    - `VideoVault.ViewModels` for view models
  - Files moved:
    - MainWindow.axaml/cs → Views/
    - SettingsWindow.axaml/cs → Views/
    - VideoPlayerControl.axaml/cs → Controls/
    - MainWindowViewModel.cs → ViewModels/
    - SettingsWindowViewModel.cs → ViewModels/
  - Result: Clean, scalable folder structure ready for future growth

- **Video Player Architecture:**
  - Fullscreen now uses WindowState instead of separate window
  - Removed external overlay window implementation
  - Improved video player initialization with timeout protection
  - Made video player initialization asynchronous to prevent UI blocking

### Technical Details
- Added `app.manifest` with Windows OS compatibility declarations
- Updated `VideoVault.csproj` to reference manifest
- Refactored fullscreen implementation to use main window state
- Added ESC key handler to main window for fullscreen exit
- Implemented proper async/await patterns for video player initialization

---

## [1.0.0-Phase1] - 2025-11-15

### Added
- **Core Application**
  - Cross-platform desktop application using Avalonia UI 11.0
  - .NET 8.0 runtime support
  - MVVM architecture with proper separation of concerns
  - Windows, Linux, and macOS support

- **Video Cataloging**
  - Recursive directory scanning for video files
  - Configurable video file extensions
  - File metadata extraction (size, hash, duration, resolution, date)
  - SHA256 hash calculation for duplicate detection
  - Progress reporting during scan operations

- **Database Management**
  - SQLite database for video catalog storage
  - Automatic database initialization on first run
  - Video file CRUD operations
  - Indexed file hash for fast duplicate queries
  - Database location: AppData/VideoVault/videovault.db

- **Duplicate Detection**
  - Advanced duplicate detection based on file hash and size
  - Similarity scoring algorithm
  - Grouping of duplicate files
  - Real-time progress reporting
  - Display of duplicate groups with file details

- **Settings Management**
  - JSON-based configuration file
  - Configurable video file extensions
  - Adjustable log levels (Debug, Info, Warning, Error)
  - Log retention policy (days)
  - Settings persistence between sessions
  - Settings UI window for easy configuration

- **Logging System**
  - Comprehensive application logging
  - File-based logs with timestamps
  - Configurable log levels
  - Automatic log file rotation
  - Log retention based on age
  - Logs location: AppData/VideoVault/Logs/

- **User Interface**
  - Clean, modern interface design
  - Video library list with sortable columns
  - Progress bars for long-running operations
  - Status messages for user feedback
  - Resizable main window
  - Menu bar with File, Tools, and Help menus
  - Tooltips on all UI elements

- **Build System**
  - PowerShell build script for all platforms
  - Platform-specific builds (Windows, Linux, macOS)
  - Automatic dependency restoration
  - Release configuration builds

### Technical Details
- Avalonia UI 11.0.10
- .NET 8.0 target framework
- Microsoft.Data.Sqlite 8.0.0
- LibVLCSharp 3.8.5
- Platform-specific LibVLC packages
- MVVM pattern with INotifyPropertyChanged
- Async/await throughout for responsive UI
- CancellationToken support for cancellable operations

---

## Upcoming Releases

### [Phase 3] - Planned
- Automatic thumbnail generation for all videos
- Thumbnail display in video library
- Thumbnail caching system
- Custom thumbnail selection
- Platform-specific installers:
  - Windows: EXE or MSI installer
  - macOS: DMG installer
  - Linux Mint: DEB package

### [Phase 4] - Planned
- Web scraping for missing video metadata
- Performer identification and recognition
- Performer database with details (name, aliases, DOB, characteristics)
- Act cataloging and tagging
- Advanced metadata retrieval from online sources

### [Phase 5] - Planned
- Metadata editing interface
- Update video file information
- Edit performer details
- Manage tags and categories
- Bulk metadata operations
- Import/export metadata

### [Phase 6] - Planned
- Search by performer name
- Filter by acts and tags
- Advanced query builder
- Combined search criteria
- Saved searches
- Sort options
- Custom filters

---

## Version History Summary

| Version | Release Date | Key Features |
|---------|-------------|--------------|
| 1.0.0-Phase2 | 2025-11-20 | Video player, duplicate deletion, bug fixes |
| 1.0.0-Phase1 | 2025-11-15 | Core app, cataloging, duplicate detection, settings |

---

## Notes

### Breaking Changes
None yet - this is the initial release series.

### Deprecations
None yet.

### Known Issues
- LibVLC initialization may fail if VLC not installed (Linux)
- Large video collections (10,000+ files) may have slower load times
- Metadata extraction depends on file format support

### Performance Improvements
- Phase 2: Async video player initialization prevents UI blocking
- Phase 2: Timeout protection for video player init (5 seconds)
- Phase 1: Indexed database queries for fast duplicate detection

---

## Migration Guide

### Phase 1 to Phase 2
No migration needed. Database schema unchanged. Existing catalogs will work without modification.

### Future Phases
Migration notes will be added as needed for schema changes or breaking updates.

---

**For detailed information about any release, see the README.md file or visit the GitHub repository.**
