# VideoVault Installer Notes

## Windows Installation

### MSI Installer (Recommended)
The Windows MSI installer (`VideoVault-Setup-x64.msi`) provides the best installation experience:

**Installation Details:**
- **Install Location**: `%LOCALAPPDATA%\VideoVault` (User profile - typically `C:\Users\<username>\AppData\Local\VideoVault`)
- **Install Scope**: Per-user installation (no administrator privileges required)
- **Start Menu**: Creates "VideoVault" folder with application and uninstall shortcuts
- **Uninstall**: Available via Windows Settings > Apps or Start Menu > VideoVault > Uninstall

**Why User Profile Installation?**
- No admin rights needed for installation
- Each user has their own copy with their own settings
- Easier updates without affecting other users
- Better security isolation between users
- Follows Microsoft's recommended practices for per-user applications

**Data Storage:**
- Application files: `%LOCALAPPDATA%\VideoVault\`
- Settings: `%APPDATA%\VideoVault\settings.json`
- Database: `%APPDATA%\VideoVault\videovault.db`
- Thumbnails: `%APPDATA%\VideoVault\thumbnails\`
- Logs: `%APPDATA%\VideoVault\logs\`

### ZIP Package (Alternative)
If you prefer manual installation or the MSI is not available:
- Extract `VideoVault-Windows-x64.zip` to any location
- Run `VideoVault.exe` directly
- Portable - can run from USB drive or any folder

## macOS Installation

### DMG Package (when available)
- Double-click the DMG file
- Drag VideoVault.app to Applications folder
- Launch from Applications

### ZIP Package
- Extract `VideoVault-macOS-x64.zip` or `VideoVault-macOS-arm64.zip`
- Move VideoVault.app to /Applications
- First launch: Right-click > Open (to bypass Gatekeeper if not code-signed)

**Universal Binary:**
The universal package contains both Intel and Apple Silicon binaries for optimal performance on all Macs.

## Linux Installation

### DEB Package (Debian/Ubuntu/Mint)
```bash
sudo dpkg -i installers/videovault-deb/videovault*.deb
# Or build the DEB first:
sudo dpkg-deb --build installers/videovault-deb videovault-1.0.0-amd64.deb
sudo dpkg -i videovault-1.0.0-amd64.deb
```

Install location: `/usr/local/bin/videovault/`

### TAR.GZ Package
```bash
tar -xzf VideoVault-Linux-x64.tar.gz -C ~/videovault
cd ~/videovault
./VideoVault
```

## Building Installers

### Prerequisites
**Windows MSI:**
```powershell
dotnet tool install --global wix
```

**All Platforms:**
- .NET 8.0 SDK
- PowerShell (Windows) or PowerShell Core (Linux/macOS)

### Build Commands
```powershell
# Build for current platform
.\build.ps1 -Platform Windows
.\build.ps1 -Platform macOS
.\build.ps1 -Platform Linux

# Build for all platforms
.\build.ps1 -Platform All

# Clean build
.\build.ps1 -Platform All -Clean

# Skip installer generation (binaries only)
.\build.ps1 -Platform All -SkipInstallers
```

### Output Locations
- Binaries: `bin/Release/{runtime}/`
- Installers: `installers/`
- MSI: `installers/VideoVault-Setup-x64.msi`
- Packages: `installers/VideoVault-{Platform}-{Arch}.{zip|tar.gz}`

## Security Notes

- Windows: MSI is not code-signed. Users may see SmartScreen warnings on first run.
- macOS: App is not notarized. Users will need to right-click > Open on first launch.
- Linux: DEB package is not signed.

For production releases, consider:
- Code signing certificates (Windows Authenticode)
- Apple Developer ID signing and notarization (macOS)
- PGP signing for Linux packages
