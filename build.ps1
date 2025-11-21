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

    # Create ZIP package for Windows (always create as fallback)
    Write-Host "Creating ZIP package for Windows..." -ForegroundColor Yellow
    $ZipPath = Join-Path $InstallerDir "VideoVault-Windows-x64.zip"
    Compress-Archive -Path "$BuildOutput\*" -DestinationPath $ZipPath -Force
    Write-Host "Windows package created: $ZipPath" -ForegroundColor Green
    Write-Host ""

    # Check if WiX Toolset is installed for MSI creation
    Write-Host "Checking for WiX Toolset..." -ForegroundColor Yellow
    $WixAvailable = $false

    try {
        $wixVersion = & wix --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "WiX Toolset found: $wixVersion" -ForegroundColor Green
            $WixAvailable = $true
        }
    }
    catch {
        # WiX not found
    }

    # If WiX is not available, try to install it
    if (-not $WixAvailable) {
        Write-Host "WiX Toolset not found. Installing..." -ForegroundColor Yellow
        try {
            & dotnet tool install --global wix 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }

            if ($LASTEXITCODE -eq 0) {
                Write-Host "WiX Toolset installed successfully!" -ForegroundColor Green

                # Verify installation
                try {
                    $wixVersion = & wix --version 2>&1
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "WiX Toolset version: $wixVersion" -ForegroundColor Green
                        $WixAvailable = $true
                    }
                }
                catch {
                    Write-Host "WARNING: WiX installed but not available in PATH. You may need to restart your terminal." -ForegroundColor Yellow
                }
            }
            else {
                Write-Host "WARNING: Failed to install WiX Toolset automatically." -ForegroundColor Yellow
                Write-Host "Please install manually: dotnet tool install --global wix" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "WARNING: Failed to install WiX Toolset: $($_.Exception.Message)" -ForegroundColor Yellow
            Write-Host "Please install manually: dotnet tool install --global wix" -ForegroundColor Yellow
        }
        Write-Host ""
    }

    if ($WixAvailable) {
        try {
            Write-Host "Creating MSI installer..." -ForegroundColor Yellow

            $WxsFile = Join-Path $ProjectDir "VideoVault.wxs"
            $WixObjDir = Join-Path $InstallerDir "wixobj"
            $HarvestedWxs = Join-Path $WixObjDir "HarvestedFiles.wxs"
            $MsiPath = Join-Path $InstallerDir "VideoVault-Setup-x64.msi"

            # Ensure wixobj directory exists
            if (-not (Test-Path $WixObjDir)) {
                New-Item -ItemType Directory -Path $WixObjDir -Force | Out-Null
            }

            # Generate file list from build output directory
            Write-Host "  Generating component manifest from build files..." -ForegroundColor Cyan

            # WiX v6 approach: manually generate components file
            $files = Get-ChildItem -Path $BuildOutput -Recurse -File

            $wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="ApplicationFiles" Directory="INSTALLFOLDER">
"@

            $componentId = 1
            foreach ($file in $files) {
                $relativePath = $file.FullName.Substring($BuildOutput.Length + 1)
                $fileId = "File$componentId"
                $compId = "Component$componentId"
                $compGuid = [guid]::NewGuid().ToString().ToUpper()

                $wxsContent += @"

      <Component Id="$compId" Guid="$compGuid">
        <File Id="$fileId" Source="$($file.FullName)" KeyPath="yes" />
      </Component>
"@
                $componentId++
            }

            $wxsContent += @"

    </ComponentGroup>
  </Fragment>
</Wix>
"@

            $wxsContent | Out-File -FilePath $HarvestedWxs -Encoding UTF8
            Write-Host "  Generated $($files.Count) file components" -ForegroundColor Green

            # Build the MSI
            Write-Host "  Compiling MSI package..." -ForegroundColor Cyan

            $buildArgs = @(
                "build"
                "-arch"
                "x64"
                "-src"
                $WxsFile
                "-src"
                $HarvestedWxs
                "-out"
                $MsiPath
            )

            & wix $buildArgs 2>&1 | ForEach-Object {
                $line = $_.ToString()
                if ($line -match "error") {
                    Write-Host "  $line" -ForegroundColor Red
                }
                elseif ($line -match "warning") {
                    Write-Host "  $line" -ForegroundColor Yellow
                }
                else {
                    Write-Host "  $line" -ForegroundColor Gray
                }
            }

            if ($LASTEXITCODE -eq 0 -and (Test-Path $MsiPath)) {
                $msiSize = (Get-Item $MsiPath).Length / 1MB
                Write-Host "MSI installer created successfully!" -ForegroundColor Green
                Write-Host "  File: $MsiPath" -ForegroundColor Cyan
                Write-Host "  Size: $($msiSize.ToString('F2')) MB" -ForegroundColor Cyan
                Write-Host "  Installation location: %LOCALAPPDATA%\VideoVault" -ForegroundColor Cyan
                Write-Host "  Install mode: Per-User (no admin privileges required)" -ForegroundColor Cyan
            }
            else {
                Write-Host "WARNING: MSI creation failed. See output above for details." -ForegroundColor Red
                Write-Host "ZIP package is available as fallback: $ZipPath" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "ERROR: Failed to create MSI: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "ZIP package is available as fallback: $ZipPath" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "WiX Toolset could not be installed automatically." -ForegroundColor Red
        Write-Host "MSI creation skipped. ZIP package is available: $ZipPath" -ForegroundColor Yellow
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

    # Check if build output exists
    if (-not (Test-Path $BuildOutput)) {
        Write-Host "WARNING: Build output not found: $BuildOutput" -ForegroundColor Red
        Write-Host "Skipping app bundle creation for $RuntimeIdentifier" -ForegroundColor Yellow
        return
    }

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
    $PostInstScript | Out-File -FilePath (Join-Path $DebControlDir "postinst") -Encoding ASCII -NoNewline

    # Make postinst executable (set permission bits)
    if ($IsLinux -or $IsMacOS) {
        chmod +x (Join-Path $DebControlDir "postinst")
    }

    Write-Host "DEB package structure created: $DebDir" -ForegroundColor Green

    # Try to build DEB package
    $DebPath = Join-Path $InstallerDir "videovault_1.0.0_amd64.deb"

    if ($IsLinux) {
        # On Linux, we can build the DEB directly
        Write-Host "Building DEB package..." -ForegroundColor Yellow

        try {
            dpkg-deb --build $DebDir $DebPath 2>&1 | Out-Null

            if ($LASTEXITCODE -eq 0 -and (Test-Path $DebPath)) {
                $debSize = (Get-Item $DebPath).Length / 1MB
                Write-Host "DEB package created successfully!" -ForegroundColor Green
                Write-Host "  File: $DebPath" -ForegroundColor Cyan
                Write-Host "  Size: $($debSize.ToString('F2')) MB" -ForegroundColor Cyan
            } else {
                Write-Host "WARNING: DEB package creation failed" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "WARNING: Failed to create DEB package: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Note: DEB package can only be built on Linux. Use the DEB structure in: $DebDir" -ForegroundColor Yellow
        Write-Host "On Linux, run: dpkg-deb --build $DebDir $DebPath" -ForegroundColor Cyan
    }

    # Create tar.gz as cross-platform alternative
    Write-Host "Creating TAR.GZ package for Linux..." -ForegroundColor Yellow
    $TarPath = Join-Path $InstallerDir "VideoVault-Linux-x64.tar.gz"

    if ($IsLinux -or $IsMacOS) {
        tar -czf $TarPath -C $BuildOutput .

        if (Test-Path $TarPath) {
            $tarSize = (Get-Item $TarPath).Length / 1MB
            Write-Host "TAR.GZ package created: $TarPath" -ForegroundColor Green
            Write-Host "  Size: $($tarSize.ToString('F2')) MB" -ForegroundColor Cyan
        }
    } else {
        # On Windows, tar might not be available, create ZIP instead
        $ZipPath = Join-Path $InstallerDir "VideoVault-Linux-x64.zip"
        Compress-Archive -Path "$BuildOutput\*" -DestinationPath $ZipPath -Force

        if (Test-Path $ZipPath) {
            $zipSize = (Get-Item $ZipPath).Length / 1MB
            Write-Host "ZIP package created (Windows fallback): $ZipPath" -ForegroundColor Green
            Write-Host "  Size: $($zipSize.ToString('F2')) MB" -ForegroundColor Cyan
            Write-Host "  Note: On Linux/macOS, TAR.GZ will be created instead" -ForegroundColor Yellow
        }
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
