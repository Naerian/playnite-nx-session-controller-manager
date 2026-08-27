[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [string]$NotesPath = ".release-notes.md",
    [string]$Configuration = "Release",
    [string]$ToolboxPath = "C:\Playnite\Toolbox.exe",
    [string]$RequiredApiVersion = "6.16.0",
    [switch]$Publish,
    [switch]$Yes
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repository = "Naerian/playnite-nx-session-controller-manager"
$addonId = "ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc"
$tag = "v$Version"
$versionForFile = $Version -replace '\.', '_'
$releaseDate = Get-Date -Format "yyyy-MM-dd"
$packageName = "${addonId}_${versionForFile}.pext"
$packageUrl = "https://github.com/$repository/releases/download/$tag/$packageName"
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
$emDash = [char]0x2014

function Write-Utf8File([string]$Path, [string]$Content) {
    [System.IO.File]::WriteAllText($Path, $Content, $utf8WithoutBom)
}

function Invoke-Checked([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed ($LASTEXITCODE): $Command $($Arguments -join ' ')"
    }
}

function Test-GitHubReleaseExists([string]$Repository, [string]$Tag) {
    # Windows PowerShell 5.1 turns native stderr into a terminating error when
    # ErrorActionPreference is Stop. A missing release is an expected probe result,
    # so capture it under Continue and inspect both the exit code and message.
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& gh release view $Tag --repo $Repository 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -eq 0) {
        return $true
    }

    $message = (($output | ForEach-Object { $_.ToString() }) -join "`n").Trim()
    if ($message -match '(?i)release not found|HTTP\s+404|status code 404') {
        return $false
    }

    throw "Could not check whether GitHub release $Tag exists (exit $exitCode): $message"
}

function Get-ReleaseChanges([string]$Path) {
    $changes = @(
        Get-Content -LiteralPath $Path |
            ForEach-Object {
                if ($_ -match '^\s*-\s+(.+?)\s*$') {
                    $Matches[1]
                }
            }
    )
    if ($changes.Count -eq 0) {
        throw "No '- Change description' entries were found in $Path."
    }
    return $changes
}

function Write-ReleaseNotesTemplate([string]$Path, [string]$TargetVersion) {
    Write-Utf8File $Path @"
# Controller Manager $TargetVersion release notes
# Write one public, English change per line. This version marker prevents stale notes from being reused.
- Added ...
- Fixed ...
"@
}

Set-Location $root

if (-not [System.IO.Path]::IsPathRooted($NotesPath)) {
    $NotesPath = Join-Path $root $NotesPath
}
$defaultNotesPath = [System.IO.Path]::GetFullPath((Join-Path $root ".release-notes.md"))
$resolvedNotesPath = [System.IO.Path]::GetFullPath($NotesPath)
$usesDefaultNotesPath = $resolvedNotesPath.Equals(
    $defaultNotesPath, [System.StringComparison]::OrdinalIgnoreCase)

if (-not (Test-Path -LiteralPath $NotesPath)) {
    Write-ReleaseNotesTemplate $NotesPath $Version
    Write-Host "Created release-notes template: $NotesPath" -ForegroundColor Yellow
    Write-Host "Edit it and run this command again."
    exit 2
}

if ($usesDefaultNotesPath) {
    $notesHeader = Get-Content -LiteralPath $NotesPath -TotalCount 1
    $expectedHeader = "# Controller Manager $Version release notes"
    if ($notesHeader -ne $expectedHeader) {
        Write-ReleaseNotesTemplate $NotesPath $Version
        Write-Host "Replaced stale release notes with a template for ${Version}: $NotesPath" -ForegroundColor Yellow
        Write-Host "Edit it and run this command again."
        exit 2
    }
}

$changes = @(Get-ReleaseChanges $NotesPath)
$extensionPath = Join-Path $root "extension.yaml"
$assemblyInfoPath = Join-Path $root "Properties\AssemblyInfo.cs"
$changelogPath = Join-Path $root "CHANGELOG.md"
$installerPath = Join-Path $root "installer.yaml"

$currentVersionText = (
    Select-String -LiteralPath $extensionPath -Pattern '^\s*Version:\s*(.+)\s*$' |
        Select-Object -First 1
).Matches[0].Groups[1].Value.Trim()
if ([version]$Version -lt [version]$currentVersionText) {
    throw "Version $Version is older than the current manifest version $currentVersionText."
}

$localTagOutput = @(& git tag --list $tag)
if ($LASTEXITCODE -ne 0) {
    throw "Could not inspect local Git tags."
}
$localTag = ($localTagOutput -join "`n").Trim()
if ($localTag -eq $tag) {
    throw "Tag $tag already exists locally; choose a new version."
}

if ($Publish) {
    Invoke-Checked "gh" @("auth", "status")
    if (Test-GitHubReleaseExists $repository $tag) {
        throw "Release $tag already exists."
    }
}

# Version metadata. These replacements are safe to run again after a failed publish.
$extension = [System.IO.File]::ReadAllText($extensionPath)
$extension = [regex]::Replace($extension, '(?m)^Version:\s*[^\r\n]+', "Version: $Version")
Write-Utf8File $extensionPath $extension

$assemblyInfo = [System.IO.File]::ReadAllText($assemblyInfoPath)
$assemblyInfo = [regex]::Replace($assemblyInfo,
    'AssemblyVersion\("[^"]+"\)', "AssemblyVersion(`"$Version.0`")")
$assemblyInfo = [regex]::Replace($assemblyInfo,
    'AssemblyFileVersion\("[^"]+"\)', "AssemblyFileVersion(`"$Version.0`")")
Write-Utf8File $assemblyInfoPath $assemblyInfo

$changelog = [System.IO.File]::ReadAllText($changelogPath)
if ($changelog -notmatch "(?m)^## $([regex]::Escape($Version))(?=\s|$)") {
    $changeLines = ($changes | ForEach-Object { "- $_" }) -join "`n"
    $entry = "## $Version $emDash $releaseDate`n$changeLines`n`n"
    $changelog = [regex]::Replace($changelog, '(?m)^(# Changelog\s*\r?\n)',
        { param($match) $match.Groups[1].Value + "`n" + $entry })
    Write-Utf8File $changelogPath $changelog
}

$installer = [System.IO.File]::ReadAllText($installerPath)
if ($installer -notmatch "(?m)^\s+- Version:\s*$([regex]::Escape($Version))\s*$") {
    $yamlChanges = ($changes | ForEach-Object {
        "      - '" + ($_ -replace "'", "''") + "'"
    }) -join "`n"
    $packageBlock = @"
  - Version: $Version
    RequiredApiVersion: $RequiredApiVersion
    ReleaseDate: $releaseDate
    PackageUrl: $packageUrl
    Changelog:
$yamlChanges
"@
    $installer = [regex]::Replace($installer, '(?m)^(Packages:\s*\r?\n)',
        { param($match) $match.Groups[1].Value + $packageBlock + "`n" })
    Write-Utf8File $installerPath $installer
}

Write-Host "Running release checks for Controller Manager $Version..." -ForegroundColor Cyan
foreach ($test in @(
    "tests\run-session-tests.ps1",
    "tests\run-tester-tests.ps1",
    "tests\run-settings-migration-tests.ps1",
    "tests\run-settings-view-tests.ps1",
    "tests\run-creator-theme-tests.ps1",
    "tests\run-creator-theme-package-installer-tests.ps1",
    "tests\run-imported-visual-profile-tests.ps1",
    "tests\run-overlay-composition-tests.ps1",
    "tests\run-release-script-tests.ps1"
)) {
    Invoke-Checked "powershell.exe" @("-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", (Join-Path $root $test))
}

Invoke-Checked "powershell.exe" @("-NoProfile", "-ExecutionPolicy", "Bypass",
    "-File", (Join-Path $root "package.ps1"), "-Configuration", $Configuration,
    "-Version", $Version, "-ToolboxPath", $ToolboxPath)

$packagePath = Join-Path $root "dist\$Version\$packageName"
if (-not (Test-Path -LiteralPath $packagePath)) {
    throw "Expected package was not created: $packagePath"
}

$contents = @(tar -tf $packagePath)
if ($LASTEXITCODE -ne 0) {
    throw "Could not inspect package contents."
}
foreach ($required in @(
    "ControllerSessionManager.dll",
    "extension.yaml",
    "gamecontrollerdb.txt",
    "SDL_GameControllerDB.LICENSE"
)) {
    if (-not ($contents | Where-Object { $_ -eq $required })) {
        throw "Package is missing required file: $required"
    }
}

$localHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
Invoke-Checked "git" @("-c", "core.safecrlf=false", "--no-pager", "diff", "--check")

Write-Host ""
Write-Host "Prepared successfully" -ForegroundColor Green
Write-Host "Version : $Version"
Write-Host "Package : $packagePath"
Write-Host "SHA-256: $localHash"
Write-Host "Changes :"
$changes | ForEach-Object { Write-Host "  - $_" }
Write-Host ""
Invoke-Checked "git" @("--no-pager", "status", "--short")

if (-not $Publish) {
    Write-Host "Nothing was published." -ForegroundColor Yellow
    Write-Host "Review the changes, then run:"
    Write-Host ".\release.ps1 -Version $Version -Publish"
    exit 0
}

$branchOutput = @(& git branch --show-current)
if ($LASTEXITCODE -ne 0) {
    throw "Could not inspect the current Git branch."
}
$branch = ($branchOutput -join "`n").Trim()
if ($branch -ne "main") {
    throw "Publishing is only allowed from the main branch (current: $branch)."
}

if (-not $Yes) {
    $confirmation = Read-Host "Type RELEASE $Version to commit, push and publish"
    if ($confirmation -ne "RELEASE $Version") {
        throw "Publication cancelled."
    }
}

Invoke-Checked "git" @("add", "-A")
& git diff --cached --quiet
if ($LASTEXITCODE -eq 1) {
    Invoke-Checked "git" @("commit", "-m", "Release Controller Manager $Version")
}
elseif ($LASTEXITCODE -ne 0) {
    throw "Could not inspect staged changes."
}

Invoke-Checked "git" @("push", "origin", "main")

$releaseNotesPath = Join-Path $env:TEMP "controller-manager-$Version-release-notes.md"
$releaseBullets = ($changes | ForEach-Object { "- $_" }) -join "`n"
$releaseNotes = @"
## What's new

$releaseBullets

## Verification

- Session/controller tests passed.
- Tester checks passed.
- Settings construction and appearance migration checks passed.

SHA-256: ``$localHash``
"@
Write-Utf8File $releaseNotesPath $releaseNotes

try {
    Invoke-Checked "gh" @("release", "create", $tag, $packagePath,
        "--repo", $repository, "--target", "main",
        "--title", "Controller Manager $Version", "--notes-file", $releaseNotesPath)
}
finally {
    if (Test-Path -LiteralPath $releaseNotesPath) {
        Remove-Item -LiteralPath $releaseNotesPath -Force
    }
}

$publishedJson = & gh release view $tag --repo $repository `
    --json url,isDraft,isPrerelease,tagName,targetCommitish,assets
if ($LASTEXITCODE -ne 0) {
    throw "Could not verify the published release."
}
$published = $publishedJson | ConvertFrom-Json
$asset = $published.assets | Where-Object { $_.name -eq $packageName } | Select-Object -First 1
if (-not $asset -or $published.isDraft -or $published.isPrerelease) {
    throw "The release is missing its asset or is not a final public release."
}
$remoteHash = ($asset.digest -replace '^sha256:', '').ToUpperInvariant()
if ($remoteHash -ne $localHash) {
    throw "Public asset hash mismatch. Local=$localHash Remote=$remoteHash"
}

$installerUrl =
    "https://raw.githubusercontent.com/$repository/main/installer.yaml?release=$Version"
$publicInstaller = (Invoke-WebRequest -UseBasicParsing -Uri $installerUrl).Content
if ($publicInstaller -notmatch "(?m)^\s+- Version:\s*$([regex]::Escape($Version))\s*$" -or
    $publicInstaller.IndexOf($packageUrl, [StringComparison]::Ordinal) -lt 0) {
    throw "The public installer.yaml does not advertise $Version correctly."
}

Invoke-Checked "git" @("fetch", "--tags")
if ($usesDefaultNotesPath -and (Test-Path -LiteralPath $NotesPath)) {
    Remove-Item -LiteralPath $NotesPath -Force
    Write-Host "Removed published release notes: $NotesPath"
}
Write-Host ""
Write-Host "Release published and verified: $($published.url)" -ForegroundColor Green
Write-Host "SHA-256: $localHash"
