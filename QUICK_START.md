# VideoVault - Quick Start Guide

## Phase 2: Video Player & Duplicate Deletion

### Installation

1. **Install .NET 8.0 SDK**
   - Download from: https://dotnet.microsoft.com/download
   - Follow platform-specific installation instructions

2. **Clone or Download the Project**
   ```bash
   cd VideoVault
   ```

3. **Restore Dependencies**
   ```bash
   dotnet restore
   ```
   This automatically downloads LibVLC and all required packages.

### Building

**Build for your platform:**
```powershell
.\build.ps1 -Platform Windows   # For Windows
.\build.ps1 -Platform Linux     # For Linux
.\build.ps1 -Platform macOS     # For macOS
```

**Build for all platforms:**
```powershell
.\build.ps1
```

### Running

**Windows:**
```cmd
bin\Release\win-x64\VideoVault.exe
```

**Linux:**
```bash
chmod +x bin/Release/linux-x64/VideoVault
./bin/Release/linux-x64/VideoVault
```

**macOS:**
```bash
chmod +x bin/Release/osx-x64/VideoVault
./bin/Release/osx-x64/VideoVault
```

### First Time Setup

1. **Launch the application**
2. **Browse** to a folder containing video files
3. **Click "Scan"** to index your videos
4. Wait for scanning to complete

### Using Phase 2 Features

#### Playing Videos
1. Select a video from the library list (left panel)
2. Video automatically loads and starts playing
3. Use playback controls:
   - **Space bar or Play button**: Play/Pause
   - **Progress slider**: Seek to position
   - **Volume slider**: Adjust volume
   - **Volume button**: Mute/Unmute
   - **Fullscreen button or double-click**: Enter fullscreen
   - **Escape key**: Exit fullscreen

#### Deleting Duplicates
1. **Click "Find Duplicates"** button
2. Review duplicate groups in right panel
3. **Check boxes** next to files you want to delete
   - Largest file is usually best quality (at top)
4. **Click "Delete Selected"** button
5. **Confirm** deletion when prompted
6. ✅ Files removed from disk and database

### Important Notes

- **Keep at least one file** from each duplicate group
- **Deletion is permanent** - files are removed from disk
- **All operations are logged** - check logs if issues occur
- **Video player requires LibVLC** - installed automatically via NuGet

### Troubleshooting

**Videos won't play:**
- Linux: `sudo apt-get install vlc` or `sudo dnf install vlc`
- macOS: Approve app in System Preferences > Security & Privacy
- Windows: Run as administrator if needed

**Can't delete files:**
- Check file permissions
- Ensure files aren't open in another program
- Check logs in `%AppData%/VideoVault/Logs/`

**Build errors:**
- Run `dotnet clean` then `dotnet restore`
- Ensure .NET 8.0 SDK is installed
- Check internet connection (for package downloads)

### Configuration

**Settings location:**
- Windows: `%AppData%\VideoVault\settings.json`
- Linux/macOS: `~/.config/VideoVault/settings.json`

**Database location:**
- Windows: `%AppData%\VideoVault\videovault.db`
- Linux/macOS: `~/.config/VideoVault/videovault.db`

**Logs location:**
- Windows: `%AppData%\VideoVault\Logs\`
- Linux/macOS: `~/.config/VideoVault/Logs/`

### Keyboard Shortcuts

- **Space**: Play/Pause
- **Escape**: Exit fullscreen
- **Double-click video**: Toggle fullscreen

### Next Steps

Once comfortable with Phase 2, look forward to:
- **Phase 3**: Thumbnail generation
- **Phase 4**: Web scraping & metadata
- **Phase 5**: Metadata editing
- **Phase 6**: Advanced search

### Getting Help

1. Check the logs for error messages
2. Review README.md for detailed documentation
3. See PHASE2_CHANGES.md for technical details
4. Visit: https://github.com/rsgill1978/VideoVault

---

**Version**: 1.0.0-Phase2  
**Last Updated**: November 2025
