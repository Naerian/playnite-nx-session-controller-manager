param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$localization = Join-Path $root "Localization"
$englishPath = Join-Path $localization "en_US.xaml"
$expectedFiles = @(
    "de_DE.xaml", "en_US.xaml", "es_ES.xaml", "fr_FR.xaml",
    "it_IT.xaml", "ja_JP.xaml", "ko_KR.xaml", "pl_PL.xaml",
    "pt_BR.xaml", "ru_RU.xaml", "tr_TR.xaml", "zh_CN.xaml"
)

function Get-LocalizationKeys([string]$path) {
    $document = [xml](Get-Content -Raw -LiteralPath $path)
    return @($document.ResourceDictionary.ChildNodes |
        Where-Object { $_.NodeType -eq "Element" } |
        ForEach-Object { $_.GetAttribute("Key", "http://schemas.microsoft.com/winfx/2006/xaml") })
}

$actualFiles = @(Get-ChildItem -LiteralPath $localization -Filter '*.xaml' | Select-Object -ExpandProperty Name)
$missingFiles = @($expectedFiles | Where-Object { $_ -notin $actualFiles })
$unexpectedFiles = @($actualFiles | Where-Object { $_ -notin $expectedFiles })
if ($missingFiles.Count -or $unexpectedFiles.Count) {
    throw "Localization file mismatch. Missing: $($missingFiles -join ', '); unexpected: $($unexpectedFiles -join ', ')"
}

$englishKeys = Get-LocalizationKeys $englishPath
foreach ($fileName in $expectedFiles) {
    $keys = Get-LocalizationKeys (Join-Path $localization $fileName)
    $missingKeys = @($englishKeys | Where-Object { $_ -notin $keys })
    $extraKeys = @($keys | Where-Object { $_ -notin $englishKeys })
    if ($missingKeys.Count -or $extraKeys.Count -or $keys.Count -ne $englishKeys.Count) {
        throw "$fileName localization keys do not match en_US.xaml."
    }
}

& (Join-Path $root "tests\run-session-tests.ps1")

& (Join-Path $root "build.ps1") -Configuration $Configuration

$assemblyPath = Join-Path $root "bin\$Configuration\ControllerSessionManager.dll"
$assemblyName = [Reflection.AssemblyName]::GetAssemblyName($assemblyPath)
if ($assemblyName.Version.ToString() -ne "1.0.6.0") {
    throw "Unexpected assembly version: $($assemblyName.Version)"
}

$overlayPath = Join-Path $root "bin\$Configuration\ControllerSessionManager.OverlayHost.exe"
if (-not (Test-Path -LiteralPath $overlayPath)) {
    throw "Build output is missing ControllerSessionManager.OverlayHost.exe"
}
$overlayName = [Reflection.AssemblyName]::GetAssemblyName($overlayPath)
if ($overlayName.Version.ToString() -ne "1.0.6.0") {
    throw "Unexpected overlay host version: $($overlayName.Version)"
}

$outputLocalization = Join-Path $root "bin\$Configuration\Localization"
foreach ($fileName in $expectedFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $outputLocalization $fileName))) {
        throw "Build output is missing Localization\$fileName"
    }
}

Write-Host "Verification passed: $($expectedFiles.Count) locales, $($englishKeys.Count) keys each, plugin $($assemblyName.Version), overlay $($overlayName.Version)."

$distDir = Join-Path $root "dist"
if (-not (Test-Path -LiteralPath $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}
$toolbox = "C:\Playnite\Toolbox.exe"
if (Test-Path -LiteralPath $toolbox) {
    & $toolbox pack (Join-Path $root "bin\$Configuration") $distDir
} else {
    Write-Warning "Toolbox.exe not found at $toolbox - skipping .pext generation."
}
