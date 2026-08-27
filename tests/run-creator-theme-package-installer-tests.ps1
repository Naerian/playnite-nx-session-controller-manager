if ($PSVersionTable.PSEdition -eq 'Core') {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $MyInvocation.MyCommand.Path
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$assembly = Join-Path $root 'bin\Release\ControllerSessionManager.dll'
if (-not (Test-Path -LiteralPath $assembly)) {
    throw 'Build the Release configuration before running this test.'
}

[void][Reflection.Assembly]::LoadFrom('C:\Playnite\Playnite.SDK.dll')
[void][Reflection.Assembly]::LoadFrom($assembly)
$temp = Join-Path ([IO.Path]::GetTempPath()) ('csmtheme-installer-' + [guid]::NewGuid().ToString('N'))
$source = Join-Path $temp 'source'
$installed = Join-Path $temp 'installed'
New-Item -ItemType Directory -Path $source,$installed | Out-Null

try {
    @{
        SchemaVersion = 1
        Id = 'test.safe-theme'
        Name = 'Safe test theme'
        Author = 'Controller Manager tests'
        Version = '1.0.0'
        MinimumPluginVersion = '1.0.0'
        MaximumPluginVersion = '9.0.0'
        Description = 'Installer smoke test'
        ThemeIds = @()
        DesktopThemeIds = @()
        FullscreenThemeIds = @()
        Fonts = @()
        Sounds = @{}
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $source 'manifest.json') -Encoding UTF8
    @{ OverlayShowDisconnectTimer = $true; OverlayBlockOrder = 'Incident,Title,ControllerName,Timer,Metadata,Instruction,Status' } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $source 'overlay.json') -Encoding UTF8
    $package = Join-Path $temp 'valid.csmtheme'
    Compress-Archive -Path (Join-Path $source '*') -DestinationPath ($package + '.zip')
    Move-Item -LiteralPath ($package + '.zip') -Destination $package

    $installer = New-Object ControllerSessionManager.PlayniteIntegration.CreatorThemePackageInstaller($installed)
    $manifest = $installer.Inspect($package)
    if ($manifest.Id -ne 'test.safe-theme') { throw 'Package inspection returned the wrong manifest.' }
    $installer.InstallAsync($package, [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
    if (-not (Test-Path -LiteralPath (Join-Path $installed 'test.safe-theme\overlay.json'))) {
        throw 'Validated package was not installed.'
    }

    @{ OverlayShowDisconnectTimer = $true; EnableDebugLogging = $true } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $source 'overlay.json') -Encoding UTF8
    Remove-Item -LiteralPath $package
    Compress-Archive -Path (Join-Path $source '*') -DestinationPath ($package + '.zip')
    Move-Item -LiteralPath ($package + '.zip') -Destination $package
    try {
        $installer.InstallAsync($package, [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
        throw 'A functional plugin setting was accepted as a creator appearance property.'
    } catch {
        if ($_.Exception.ToString() -notmatch 'unsupported appearance property') { throw }
    }

    $manifestData = Get-Content -LiteralPath (Join-Path $source 'manifest.json') -Raw | ConvertFrom-Json
    $manifestData.MinimumPluginVersion = '99.0.0'
    $manifestData | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $source 'manifest.json') -Encoding UTF8
    @{ OverlayShowDisconnectTimer = $true } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $source 'overlay.json') -Encoding UTF8
    Remove-Item -LiteralPath $package
    Compress-Archive -Path (Join-Path $source '*') -DestinationPath ($package + '.zip')
    Move-Item -LiteralPath ($package + '.zip') -Destination $package
    try {
        $installer.Inspect($package) | Out-Null
        throw 'An incompatible plugin-version range was accepted.'
    } catch {
        if ($_.Exception.ToString() -notmatch 'not compatible') { throw }
    }

    Write-Host 'Creator theme package inspection, compatibility, installation and appearance allowlist tests passed.'
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
