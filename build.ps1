# VideoVault Build Script
# This script builds the VideoVault application for Windows, Linux, and macOS
# and generates platform-specific installers

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Windows", "Linux", "macOS", "All")]
    [string]$Platform = "All",

    [Parameter(Mandatory=$false)]
    [switch]$Clean,

    [Parameter(Mandatory=$false)]
    [switch]$SkipInstallers
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Display build information
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "VideoVault Build Script" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Get project directory
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputDir = Join-Path $ProjectDir "bin/Release"
$InstallerDir = Join-Path $ProjectDir "installers"

# Clean build output if requested
if ($Clean) {
    Write-Host "Cleaning build output..." -ForegroundColor Yellow
    if (Test-Path $OutputDir) {
        Remove-Item -Path $OutputDir -Recurse -Force
        Write-Host "Build output cleaned." -ForegroundColor Green
    }
    if (Test-Path $InstallerDir) {
        Remove-Item -Path $InstallerDir -Recurse -Force
        Write-Host "Installer output cleaned." -ForegroundColor Green
    }
    Write-Host ""
}

# Ensure installer directory exists
if (-not (Test-Path $InstallerDir)) {
    New-Item -ItemType Directory -Path $InstallerDir -Force | Out-Null
}

# Function to build for specific runtime
function Build-ForRuntime {
    param(
        [string]$RuntimeIdentifier,
        [string]$PlatformName
    )
    
    Write-Host "Building for $PlatformName ($RuntimeIdentifier)..." -ForegroundColor Yellow
    
    try {
        # Build command with runtime identifier
        $BuildOutput = Join-Path $OutputDir $RuntimeIdentifier

        # Use the csproj file directly instead of solution to avoid NETSDK1194 warning
        $ProjectFile = Join-Path $ProjectDir "VideoVault.csproj"

        dotnet publish $ProjectFile `
            --configuration Release `
            --runtime $RuntimeIdentifier `
            --self-contained true `
            --output $BuildOutput `
            /p:PublishTrimmed=false
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Build successful for $PlatformName!" -ForegroundColor Green
            Write-Host "Output location: $BuildOutput" -ForegroundColor Cyan
        } else {
            Write-Host "Build failed for $PlatformName!" -ForegroundColor Red
            exit 1
        }
    }
    catch {
        $errorMessage = $_.Exception.Message
        Write-Host "Error building for ${PlatformName}: $errorMessage" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
}

# Function to create Windows installer (MSI)
function Create-WindowsInstaller {
    param(
        [string]$RuntimeIdentifier
    )

    if ($SkipInstallers) {
        Write-Host "Skipping Windows installer creation (SkipInstallers flag set)" -ForegroundColor Yellow
        return
    }

    Write-Host "Creating Windows installer..." -ForegroundColor Yellow

    $BuildOutput = Join-Path $OutputDir $RuntimeIdentifier

    # Create ZIP package for Windows
    Write-Host "Creating ZIP package for Windows..." -ForegroundColor Yellow
    $ZipPath = Join-Path $InstallerDir "VideoVault-Windows-x64.zip"
    Compress-Archive -Path "$BuildOutput\*" -DestinationPath $ZipPath -Force
    Write-Host "Windows package created: $ZipPath" -ForegroundColor Green

    # Check if WiX Toolset is installed for MSI creation
    $WixToolsetPath = "wix"
    try {
        $wixCheck = & $WixToolsetPath --version 2>&1
        Write-Host "WiX Toolset found: $wixCheck - MSI creation available" -ForegroundColor Green
        # TODO: Implement WiX-based MSI creation here
    }
    catch {
        Write-Host "Note: WiX Toolset not found. Install via 'dotnet tool install --global wix' to create MSI installers." -ForegroundColor Yellow
    }

    Write-Host ""
}

# Function to create macOS DMG
function Create-MacDmg {
    param(
        [string]$RuntimeIdentifier
    )

    if ($SkipInstallers) {
        Write-Host "Skipping macOS DMG creation (SkipInstallers flag set)" -ForegroundColor Yellow
        return
    }

    Write-Host "Creating macOS application bundle..." -ForegroundColor Yellow

    $BuildOutput = Join-Path $OutputDir $RuntimeIdentifier
    $AppBundlePath = Join-Path $InstallerDir "VideoVault.app"

    # Create app bundle structure
    $ContentsDir = Join-Path $AppBundlePath "Contents"
    $MacOSDir = Join-Path $ContentsDir "MacOS"
    $ResourcesDir = Join-Path $ContentsDir "Resources"

    New-Item -ItemType Directory -Path $MacOSDir -Force | Out-Null
    New-Item -ItemType Directory -Path $ResourcesDir -Force | Out-Null

    # Copy application files
    Copy-Item -Path "$BuildOutput\*" -Destination $MacOSDir -Recurse -Force

    # Create Info.plist
    $InfoPlist = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>VideoVault</string>
    <key>CFBundleIdentifier</key>
    <string>com.videovault.app</string>
    <key>CFBundleName</key>
    <string>VideoVault</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
</dict>
</plist>
"@
    $InfoPlist | Out-File -FilePath (Join-Path $ContentsDir "Info.plist") -Encoding UTF8

    Write-Host "macOS app bundle created: $AppBundlePath" -ForegroundColor Green

    # On macOS, we could create a DMG using hdiutil
    # On Windows/Linux, we'll create a ZIP for now
    if ($IsMacOS) {
        Write-Host "Creating DMG file..." -ForegroundColor Yellow
        $DmgPath = Join-Path $InstallerDir "VideoVault-macOS-$RuntimeIdentifier.dmg"
        # hdiutil create -volname "VideoVault" -srcfolder $AppBundlePath -ov -format UDZO $DmgPath
        Write-Host "Note: DMG creation requires macOS. Package available as .app bundle." -ForegroundColor Yellow
    } else {
        Write-Host "Creating ZIP package for macOS..." -ForegroundColor Yellow
        $ZipPath = Join-Path $InstallerDir "VideoVault-macOS-$RuntimeIdentifier.zip"
        Compress-Archive -Path $AppBundlePath -DestinationPath $ZipPath -Force
        Write-Host "macOS package created: $ZipPath" -ForegroundColor Green
    }

    Write-Host ""
}

# Function to create macOS universal binary
function Create-MacUniversal {
    if ($SkipInstallers) {
        Write-Host "Skipping macOS universal binary creation (SkipInstallers flag set)" -ForegroundColor Yellow
        return
    }

    Write-Host "Creating macOS universal binary..." -ForegroundColor Yellow

    $IntelOutput = Join-Path $OutputDir "osx-x64"
    $ArmOutput = Join-Path $OutputDir "osx-arm64"
    $UniversalOutput = Join-Path $OutputDir "osx-universal"

    # Check if both builds exist
    if (-not (Test-Path $IntelOutput) -or -not (Test-Path $ArmOutput)) {
        Write-Host "WARNING: Both Intel and ARM builds are required for universal binary" -ForegroundColor Red
        return
    }

    # Create universal output directory
    New-Item -ItemType Directory -Path $UniversalOutput -Force | Out-Null

    # Copy Intel files as base
    Copy-Item -Path "$IntelOutput\*" -Destination $UniversalOutput -Recurse -Force

    # On macOS, we would use 'lipo' to create universal binaries
    # For now, document the process
    Write-Host "Note: Creating universal binaries requires 'lipo' tool on macOS" -ForegroundColor Yellow
    Write-Host "Command: lipo -create path/to/x64/binary path/to/arm64/binary -output path/to/universal/binary" -ForegroundColor Cyan

    # Create app bundle for universal
    Create-MacDmg -RuntimeIdentifier "universal"

    Write-Host ""
}

# Function to create Linux DEB package
function Create-LinuxDeb {
    param(
        [string]$RuntimeIdentifier
    )

    if ($SkipInstallers) {
        Write-Host "Skipping Linux DEB creation (SkipInstallers flag set)" -ForegroundColor Yellow
        return
    }

    Write-Host "Creating Linux DEB package..." -ForegroundColor Yellow

    $BuildOutput = Join-Path $OutputDir $RuntimeIdentifier
    $DebDir = Join-Path $InstallerDir "videovault-deb"
    $DebBinDir = Join-Path $DebDir "usr/local/bin/videovault"
    $DebControlDir = Join-Path $DebDir "DEBIAN"

    # Create directory structure
    New-Item -ItemType Directory -Path $DebBinDir -Force | Out-Null
    New-Item -ItemType Directory -Path $DebControlDir -Force | Out-Null

    # Copy application files
    Copy-Item -Path "$BuildOutput\*" -Destination $DebBinDir -Recurse -Force

    # Create control file
    $ControlFile = @"
Package: videovault
Version: 1.0.0
Section: video
Priority: optional
Architecture: amd64
Maintainer: VideoVault Team <support@videovault.com>
Description: Adult Video Catalog Application
 VideoVault is a comprehensive video cataloging application
 for managing and organizing video files.
"@
    $ControlFile | Out-File -FilePath (Join-Path $DebControlDir "control") -Encoding ASCII

    # Create postinst script to make executable
    $PostInstScript = @"
#!/bin/bash
chmod +x /usr/local/bin/videovault/VideoVault
"@
    $PostInstScript | Out-File -FilePath (Join-Path $DebControlDir "postinst") -Encoding ASCII

    Write-Host "DEB package structure created: $DebDir" -ForegroundColor Green
    Write-Host "Note: To build DEB package, run on Linux: dpkg-deb --build $DebDir" -ForegroundColor Yellow

    # Create tar.gz as cross-platform alternative
    Write-Host "Creating TAR.GZ package for Linux..." -ForegroundColor Yellow
    $TarPath = Join-Path $InstallerDir "VideoVault-Linux-x64.tar.gz"
    if ($IsLinux -or $IsMacOS) {
        tar -czf $TarPath -C $BuildOutput .
        Write-Host "Linux package created: $TarPath" -ForegroundColor Green
    } else {
        # On Windows, create ZIP instead
        $ZipPath = Join-Path $InstallerDir "VideoVault-Linux-x64.zip"
        Compress-Archive -Path "$BuildOutput\*" -DestinationPath $ZipPath -Force
        Write-Host "Linux package created: $ZipPath" -ForegroundColor Green
    }

    Write-Host ""
}

# Build for requested platforms
switch ($Platform) {
    "Windows" {
        Build-ForRuntime -RuntimeIdentifier "win-x64" -PlatformName "Windows"
        Create-WindowsInstaller -RuntimeIdentifier "win-x64"
    }
    "Linux" {
        Build-ForRuntime -RuntimeIdentifier "linux-x64" -PlatformName "Linux"
        Create-LinuxDeb -RuntimeIdentifier "linux-x64"
    }
    "macOS" {
        Build-ForRuntime -RuntimeIdentifier "osx-x64" -PlatformName "macOS (Intel)"
        Build-ForRuntime -RuntimeIdentifier "osx-arm64" -PlatformName "macOS (Apple Silicon)"
        Create-MacUniversal
    }
    "All" {
        Build-ForRuntime -RuntimeIdentifier "win-x64" -PlatformName "Windows"
        Create-WindowsInstaller -RuntimeIdentifier "win-x64"

        Build-ForRuntime -RuntimeIdentifier "linux-x64" -PlatformName "Linux"
        Create-LinuxDeb -RuntimeIdentifier "linux-x64"

        Build-ForRuntime -RuntimeIdentifier "osx-x64" -PlatformName "macOS (Intel)"
        Build-ForRuntime -RuntimeIdentifier "osx-arm64" -PlatformName "macOS (Apple Silicon)"
        Create-MacUniversal
    }
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Build process completed!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

if (-not $SkipInstallers) {
    Write-Host "Installers and packages available in: $InstallerDir" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "To run the application:" -ForegroundColor Yellow
Write-Host "  Windows: bin\Release\win-x64\VideoVault.exe" -ForegroundColor White
Write-Host "  Linux:   bin/Release/linux-x64/VideoVault" -ForegroundColor White
Write-Host "  macOS:   bin/Release/osx-x64/VideoVault" -ForegroundColor White
Write-Host ""
