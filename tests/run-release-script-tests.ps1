$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$source = Join-Path $root "release.ps1"
$fixture = Join-Path ([IO.Path]::GetTempPath()) ("csm-release-script-" + [guid]::NewGuid().ToString("N"))

try {
    New-Item -Path $fixture -ItemType Directory -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination (Join-Path $fixture "release.ps1")
    $script = Join-Path $fixture "release.ps1"
    $notes = Join-Path $fixture ".release-notes.md"

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $script -Version 9.9.8 *> $null
    if ($LASTEXITCODE -ne 2 -or -not (Test-Path -LiteralPath $notes) -or
        (Get-Content -LiteralPath $notes -TotalCount 1) -ne "# Controller Manager 9.9.8 release notes") {
        throw "A missing notes file did not create a version-bound template."
    }

    [IO.File]::WriteAllText($notes,
        "# Controller Manager 9.9.7 release notes`n- Stale previous release note.`n",
        [Text.UTF8Encoding]::new($false))
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $script -Version 9.9.8 *> $null
    $content = Get-Content -LiteralPath $notes -Raw
    if ($LASTEXITCODE -ne 2 -or
        (Get-Content -LiteralPath $notes -TotalCount 1) -ne "# Controller Manager 9.9.8 release notes" -or
        $content -match "Stale previous release note") {
        throw "Stale notes were not replaced for the requested release version."
    }

    $releaseSource = Get-Content -LiteralPath $source -Raw
    if ($releaseSource -notmatch 'Remove-Item -LiteralPath \$NotesPath -Force') {
        throw "The default notes file is not removed after successful publication verification."
    }

    Write-Host "Release-note version isolation tests passed."
}
finally {
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
