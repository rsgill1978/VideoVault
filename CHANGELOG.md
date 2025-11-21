# Changelog

All notable changes to VideoVault will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0-Phase3] - 2025-11-21

### Added
- **Thumbnail Generation**
  - Automatic thumbnail generation for all imported videos
  - LibVLC-based thumbnail capture at 5-second mark
  - 320x240 JPG thumbnails stored in `%APPDATA%\VideoVault\thumbnails`
  - Thumbnails cached by video file hash to avoid regeneration
  - `ThumbnailService` for managing thumbnail creation and cleanup

- **Video Library Thumbnails**
  - 80x60 thumbnail previews displayed in video library
  - Video icon (📹) shown when thumbnail not available
  - Grid layout with thumbnail on left, file info on right
  - Maintains existing file size and extension display

- **Platform-Specific Installers**
  - **Windows MSI**: Per-user installer (no admin required)
    - Installs to `%LOCALAPPDATA%\VideoVault`
    - Start Menu shortcuts (app + uninstall)
    - Automatic WiX Toolset installation
    - ZIP package fallback
  - **macOS Universal Binary**: Combined Intel + Apple Silicon
    - .app bundle with Info.plist
    - ZIP package distribution
    - DMG creation support (on macOS)
  - **Linux DEB**: Debian/Ubuntu/Mint package
    - Package structure for `dpkg-deb`
    - TAR.GZ alternative
  - **Build Script Enhancement**: Automatic installer generation

### Changed
- Database schema: Added `ThumbnailPath TEXT` column
- Database migration: Automatic column addition for existing databases
- Updated `VideoFile` model with `ThumbnailPath` property
- Enhanced build script with `-SkipInstallers` flag
- Documentation consolidated: Merged INSTALLER-NOTES.md into README.md
- Updated README.md for Phase 3 completion

### Fixed
- Universal macOS build: Added existence check before copying files
- Build script: Properly handles missing build outputs

### Technical Details
- New `ThumbnailService.cs` using LibVLCSharp for snapshot generation
- Database: `UpdateThumbnailPathAsync` method for storing thumbnail locations
- WiX Toolset v4 integration for MSI creation
- Cross-platform package creation (ZIP/TAR.GZ/DEB/DMG)
- `.claude/` directory added to .gitignore

---

## [1.0.0-Phase2-Hotfix2] - 2025-11-21

### Fixed
- **Fullscreen Controls Visibility Across Different Viewports**
  - Fixed controls not appearing on different screen sizes and resolutions
  - Changed from Grid layout to DockPanel for fullscreen overlay
  - DockPanel with `DockPanel.Dock="Bottom"` provides consistent positioning
  - Controls now properly anchor to bottom regardless of viewport size or DPI
  - Works reliably across all machines and screen configurations

- **Volume Slider Not Working in Fullscreen**
  - Added `_isUpdatingVolume` flag to prevent feedback loops
  - Both volume sliders now sync properly without blocking each other
  - Volume changes in fullscreen mode now work correctly

### Changed
- **Simplified Documentation**
  - Updated all XAML comments to simplified technical English
  - Updated all code comments to clear, concise descriptions
  - Removed verbose inline comments
  - All comments positioned above code lines
  - Removed debug logging traces from fullscreen implementation

### Technical Details
- Replaced Canvas/Grid layout with DockPanel for fullscreen controls
- Added volume slider sync logic with feedback loop prevention
- Cleaned up fullscreen initialization and teardown code
- Removed unused debug logging and fields

---

## [1.0.0-Phase2-Hotfix1] - 2025-11-21

### Fixed
- **Fullscreen Video Player Controls Not Visible**
  - Fixed critical bug where playback controls were not visible in fullscreen mode
  - Implemented separate fullscreen overlay control set
  - Created dedicated FullscreenControls with ZIndex 100
  - Added state sync between normal and fullscreen controls

### Changed
- **XAML Structure Redesign**
  - Dual control architecture with normal and fullscreen sets
  - Controls automatically sync state when switching modes

### Technical Details
- New method: `SyncControlsToFullscreen()` - Syncs UI state between control sets
- Updated: `EnableFullscreenMode()` - Toggles between control sets
- Updated: `UpdatePlayPauseButton()` and `UpdateVolumeButton()` - Sync across both sets

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
