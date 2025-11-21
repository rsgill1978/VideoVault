# VideoVault - Adult Video Catalog Application

**Version:** 1.0.0 - Phase 2  
**Platform:** Cross-platform (Windows, Linux, macOS)  
**Framework:** .NET 8.0 + Avalonia UI 11.0  
**Repository:** https://github.com/rsgill1978/VideoVault

---

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [System Requirements](#system-requirements)
4. [Installation](#installation)
5. [Quick Start Guide](#quick-start-guide)
6. [User Guide](#user-guide)
7. [Troubleshooting](#troubleshooting)
8. [Project Roadmap](#project-roadmap)
9. [Technical Information](#technical-information)
10. [License](#license)

---

## Overview

VideoVault is a cross-platform desktop application designed to catalog, organize, and manage adult video collections. Built with .NET 8 and Avalonia UI, it provides a modern, responsive interface with powerful features for video management.

### Key Capabilities
- **Video Cataloging**: Recursive scanning of directories to build comprehensive video libraries
- **Embedded Video Player**: LibVLC-based player with full playback controls
- **Duplicate Detection**: Advanced algorithms to find and remove duplicate files
- **SQLite Database**: Fast, reliable local storage for your catalog
- **Cross-Platform**: Runs on Windows, Linux, and macOS

---

## Features

### Phase 1: Core Functionality ✅
- ✅ Recursive directory scanning for video files
- ✅ SQLite database for catalog storage
- ✅ File metadata extraction (size, hash, date, duration, resolution)
- ✅ Duplicate file detection based on file hash and size
- ✅ Configurable settings (log levels, retention, video file extensions)
- ✅ Comprehensive logging system
- ✅ Progress indicators for all operations
- ✅ Video library display with file details

### Phase 2: Video Player & Deletion ✅
- ✅ Embedded LibVLC video player
- ✅ Play/pause/stop controls
- ✅ Video seeking with progress slider
- ✅ Volume control with mute option
- ✅ Fullscreen mode (embedded in main window)
- ✅ Playback time display
- ✅ Duplicate file deletion with safety confirmations
- ✅ Automatic UI refresh after deletions
- ✅ Collapsible video player pane

### Coming Soon
- 🔄 **Phase 3**: Automatic thumbnail generation, installer generation
- 🔄 **Phase 4**: Web scraping for metadata, performer identification, act cataloging
- 🔄 **Phase 5**: Metadata editing capabilities
- 🔄 **Phase 6**: Advanced search and filtering options

---

## System Requirements

### Required
- **.NET 8.0 SDK** or later
- **4GB RAM** minimum (8GB recommended)
- **500MB** free disk space for application
- **Additional storage** for video catalog database

### Platform-Specific
#### Windows
- Windows 10 or later
- No additional requirements

#### Linux
- Ubuntu 20.04+ / Debian 11+ / Fedora 36+ or equivalent
- VLC media player: `sudo apt-get install vlc` (Debian/Ubuntu)

#### macOS
- macOS 10.15 (Catalina) or later
- May require security approval on first run

---

## Installation

### Build from Source

1. **Clone the Repository**
   ```bash
   git clone https://github.com/rsgill1978/VideoVault.git
   cd VideoVault
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the Application**
   ```powershell
   # Windows PowerShell
   .\build.ps1
   
   # Or build for specific platform
   .\build.ps1 -Platform Windows
   .\build.ps1 -Platform Linux
   .\build.ps1 -Platform macOS
   ```

4. **Run the Application**
   - **Windows**: `bin\Release\win-x64\VideoVault.exe`
   - **Linux**: `bin/Release/linux-x64/VideoVault`
   - **macOS**: `bin/Release/osx-x64/VideoVault`

---

## Quick Start Guide

### First Launch

1. **Launch VideoVault** - Run the executable for your platform

2. **Select Video Directory**
   - Click **Browse** → Navigate to folder with videos → Click Select

3. **Scan for Videos**
   - Click **Scan** → Wait for completion → Videos appear in library

4. **Play a Video**
   - Select from library → Video loads automatically
   - Use controls: play/pause, seek, volume
   - Press **F** or use button for fullscreen
   - Press **ESC** to exit fullscreen

5. **Find Duplicates**
   - Menu: **Tools** → **Find Duplicates**
   - Check boxes for files to delete
   - Click **Delete Selected** → Confirm

---

## User Guide

### Main Window Components

- **Menu Bar**: File (Settings, Exit), Tools (Find Duplicates, Logs), Help (About)
- **Path Selection**: Browse, Scan, Cancel, Settings buttons
- **Progress Bar**: Shows operation status
- **Video Player** (Collapsible): Embedded player with controls
- **Video Library**: List of cataloged videos
- **Duplicates Panel**: Shows duplicate file groups

### Video Player Controls

| Control | Function |
|---------|----------|
| Play/Pause Button | Start or pause playback (shows ▶ when paused, ⏸ when playing) |
| Progress Slider | Seek to specific time by dragging or clicking on the bar |
| Time Display | Shows current position / total duration (HH:MM:SS format) |
| Volume Button | Click to mute/unmute audio (icon changes: 🔇 muted, 🔉 low, 🔊 high) |
| Volume Slider | Adjust volume from 0 (muted) to 100 (maximum) |
| Fullscreen Button | Toggle fullscreen mode (⛶ icon) |
| Double-click Video | Toggle fullscreen mode |
| ESC key | Exit fullscreen mode |

**Fullscreen Mode Features:**
- Controls overlay the video at the bottom of the screen
- Auto-hide after 3 seconds of inactivity
- Move mouse to show controls again
- Controls remain visible while mouse is over them
- Semi-transparent dark background for better visibility

### Settings

Access via **File** → **Settings**:
- **Video Extensions**: File types to scan (comma-separated)
- **Log Level**: Debug, Info, Warning, Error
- **Log Retention**: Days to keep logs

### Duplicate Management

1. Scan videos first
2. **Tools** → **Find Duplicates**
3. Review groups (same hash/size = duplicates)
4. Check boxes for files to delete (keep at least one!)
5. Click **Delete Marked** → Confirm
6. Files removed from disk and database

---

## Troubleshooting

### App Won't Start
- Verify .NET 8.0: `dotnet --version`
- Check logs: `%AppData%\VideoVault\Logs\` (Windows) or `~/.config/VideoVault/Logs/` (Linux/macOS)
- On Windows, ensure `app.manifest` is present

### Videos Won't Play
- **Linux**: Install VLC: `sudo apt-get install vlc`
- Check logs for LibVLC errors
- Verify file format is VLC-compatible

### Scan Issues
- Verify directory path and permissions
- Ensure directory contains supported video files
- Check configured extensions in settings

### Performance
- Large collections take time to load
- Consider scanning smaller batches
- Check disk speed
- Review log retention settings

---

## Project Roadmap

### ✅ Phase 1: Core Functionality
Video cataloging, scanning, duplicate detection, settings, logging

### ✅ Phase 2: Video Player & Deletion
Embedded player, controls, fullscreen, duplicate deletion

### 🔄 Phase 3: Thumbnails & Installers
Auto thumbnail generation, DMG/EXE/DEB installers

### 🔄 Phase 4: Metadata & AI
Web scraping, performer identification, act cataloging

### 🔄 Phase 5: Metadata Editing
Edit video metadata, performer info, tags

### 🔄 Phase 6: Advanced Search
Search by performer, acts, tags, advanced filters

---

## Technical Information

### Architecture
MVVM pattern with Models, Views, ViewModels, and Services

### Technology Stack
- Avalonia UI 11.0 (cross-platform UI)
- .NET 8.0 runtime
- SQLite database
- LibVLCSharp for video playback
- Custom logging service

### Database Schema
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
);
```

### Configuration
Location: `%AppData%\VideoVault\appsettings.json` (Windows) or `~/.config/VideoVault/appsettings.json` (Linux/macOS)

---

## License

**Personal use only.** Users must comply with all local laws regarding adult content.

- Respect copyright and intellectual property
- Ensure content is legally obtained
- Follow local regulations
- Use responsibly and ethically

---

## Getting Help

### Log Files
- Windows: `%AppData%\VideoVault\Logs\`
- Linux/macOS: `~/.config/VideoVault/Logs/`

Access via **Tools** → **Open Log Folder**

### Reporting Issues
Include: OS version, .NET version, logs, steps to reproduce

### Support
- GitHub Issues: https://github.com/rsgill1978/VideoVault/issues
- Discussions: https://github.com/rsgill1978/VideoVault/discussions

---

**Thank you for using VideoVault!**
