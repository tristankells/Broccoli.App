#Requires -Version 7.0
<#
.SYNOPSIS
    Builds and installs Broccoli.Avalonia.Desktop to the current PC,
    creating a Desktop and Start Menu shortcut.

.DESCRIPTION
    Publishes the desktop app (self-contained Release build) and copies the
    output to the current user's local Programs folder. Shortcuts are created
    on the Desktop and in the Start Menu, so no admin rights are required.

.PARAMETER InstallDir
    Destination folder. Defaults to "$env:LOCALAPPDATA\Programs\Broccoli".

.PARAMETER SkipBuild
    Skip the dotnet publish step and install from an existing publish folder.

.EXAMPLE
    .\deploy.ps1
#>
[CmdletBinding()]
param(
    [string]$InstallDir = "$env:LOCALAPPDATA\Programs\Broccoli",
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $RepoRoot 'Broccoli.Avalonia\Broccoli.Avalonia.Desktop\Broccoli.Avalonia.Desktop.csproj'
$PublishDir = Join-Path $RepoRoot 'artifacts\publish\desktop'
$ExecutableName = 'Broccoli.Avalonia.Desktop.exe'

function Get-RuntimeIdentifier {
    switch ($env:PROCESSOR_ARCHITECTURE) {
        'ARM64' { return 'win-arm64' }
        'x86'   { return 'win-x86' }
        default { return 'win-x64' }
    }
}

if (-not $SkipBuild) {
    if (-not (Test-Path -LiteralPath $Project)) {
        throw "Project not found: $Project"
    }

    $rid = Get-RuntimeIdentifier
    Write-Host "Publishing $rid (self-contained Release)..." -ForegroundColor Cyan

    if (Test-Path -LiteralPath $PublishDir) {
        Remove-Item -LiteralPath $PublishDir -Recurse -Force
    }

    dotnet publish $Project `
        -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=false `
        -o $PublishDir

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}

$Executable = Join-Path $PublishDir $ExecutableName
if (-not (Test-Path -LiteralPath $Executable)) {
    throw "Published executable not found: $Executable"
}

Write-Host "Installing to $InstallDir ..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

# Copy published output (overwrite existing files, prune stale ones).
Copy-Item -Path (Join-Path $PublishDir '*') -Destination $InstallDir -Recurse -Force

$TargetExe = Join-Path $InstallDir $ExecutableName

$Shell = New-Object -ComObject WScript.Shell

$DesktopShortcut = $Shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'Broccoli.lnk'))
$DesktopShortcut.TargetPath = $TargetExe
$DesktopShortcut.WorkingDirectory = $InstallDir
$DesktopShortcut.IconLocation = "$TargetExe,0"
$DesktopShortcut.Description = 'Broccoli — meal planning, pantry & grocery lists'
$DesktopShortcut.Save()

$StartMenuDir = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\Broccoli'
New-Item -ItemType Directory -Path $StartMenuDir -Force | Out-Null

$StartMenuShortcut = $Shell.CreateShortcut((Join-Path $StartMenuDir 'Broccoli.lnk'))
$StartMenuShortcut.TargetPath = $TargetExe
$StartMenuShortcut.WorkingDirectory = $InstallDir
$StartMenuShortcut.IconLocation = "$TargetExe,0"
$StartMenuShortcut.Description = 'Broccoli — meal planning, pantry & grocery lists'
$StartMenuShortcut.Save()

Write-Host "Done. Installed to $InstallDir" -ForegroundColor Green
Write-Host "Shortcuts created on the Desktop and in the Start Menu."
