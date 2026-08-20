param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.10",
    [string]$ToolboxPath = "C:\Playnite\Toolbox.exe"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $root "build.ps1") -Configuration $Configuration

if (-not (Test-Path -LiteralPath $ToolboxPath)) {
    throw "Playnite Toolbox was not found at $ToolboxPath"
}

$source = Join-Path $root "bin\$Configuration"
$output = Join-Path $root "dist\v$Version"
New-Item -ItemType Directory -Path $output -Force | Out-Null
& $ToolboxPath pack $source $output
if ($LASTEXITCODE -ne 0) {
    throw "Playnite Toolbox pack failed with exit code $LASTEXITCODE"
}

$package = Get-ChildItem -LiteralPath $output -Filter '*.pext' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $package) {
    throw "Playnite Toolbox did not create a .pext package."
}

Write-Host "Package created: $($package.FullName)"
Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256
