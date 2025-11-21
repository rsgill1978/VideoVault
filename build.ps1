# VideoVault Build Script
# This script builds the VideoVault application for Windows, Linux, and macOS

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Windows", "Linux", "macOS", "All")]
    [string]$Platform = "All",
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean
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

# Clean build output if requested
if ($Clean) {
    Write-Host "Cleaning build output..." -ForegroundColor Yellow
    if (Test-Path $OutputDir) {
        Remove-Item -Path $OutputDir -Recurse -Force
        Write-Host "Build output cleaned." -ForegroundColor Green
    }
    Write-Host ""
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

# Build for requested platforms
switch ($Platform) {
    "Windows" {
        Build-ForRuntime -RuntimeIdentifier "win-x64" -PlatformName "Windows"
    }
    "Linux" {
        Build-ForRuntime -RuntimeIdentifier "linux-x64" -PlatformName "Linux"
    }
    "macOS" {
        Build-ForRuntime -RuntimeIdentifier "osx-x64" -PlatformName "macOS (Intel)"
        Build-ForRuntime -RuntimeIdentifier "osx-arm64" -PlatformName "macOS (Apple Silicon)"
    }
    "All" {
        Build-ForRuntime -RuntimeIdentifier "win-x64" -PlatformName "Windows"
        Build-ForRuntime -RuntimeIdentifier "linux-x64" -PlatformName "Linux"
        Build-ForRuntime -RuntimeIdentifier "osx-x64" -PlatformName "macOS (Intel)"
        Build-ForRuntime -RuntimeIdentifier "osx-arm64" -PlatformName "macOS (Apple Silicon)"
    }
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Build process completed!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run the application:" -ForegroundColor Yellow
Write-Host "  Windows: bin\Release\win-x64\VideoVault.exe" -ForegroundColor White
Write-Host "  Linux:   bin/Release/linux-x64/VideoVault" -ForegroundColor White
Write-Host "  macOS:   bin/Release/osx-x64/VideoVault" -ForegroundColor White
Write-Host ""
