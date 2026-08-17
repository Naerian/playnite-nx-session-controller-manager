$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$output = Join-Path $root "obj\SessionTests"
New-Item -ItemType Directory -Path $output -Force | Out-Null

$compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

$testExecutable = Join-Path $output "SessionManagerTests.exe"
& $compiler /nologo /warn:4 /warnaserror+ /optimize+ /target:exe /out:$testExecutable `
    /reference:System.Core.dll `
    (Join-Path $root "Controllers\ControllerDeviceSnapshot.cs") `
    (Join-Path $root "Controllers\ControllerSnapshotMerger.cs") `
    (Join-Path $root "Controllers\ControllerMetadata.cs") `
    (Join-Path $root "Controllers\IControllerBatteryProvider.cs") `
    (Join-Path $root "Controllers\HidDiagnosticsService.cs") `
    (Join-Path $root "Controllers\PlayStationHidBatteryProvider.cs") `
    (Join-Path $root "Controllers\WindowsBluetoothBatteryProvider.cs") `
    (Join-Path $root "Controllers\ControllerDeviceIdentity.cs") `
    (Join-Path $root "Controllers\ControllerBridgeIdentity.cs") `
    (Join-Path $root "Controllers\IntentionalInputDetector.cs") `
    (Join-Path $root "Sessions\GameSessionManager.cs") `
    (Join-Path $root "Sessions\GamePauseService.cs") `
    (Join-Path $root "Sessions\OnlineSessionDetector.cs") `
    (Join-Path $root "Sessions\AdaptiveSessionScopeDetector.cs") `
    (Join-Path $root "Sessions\InputPollingPolicy.cs") `
    (Join-Path $root "tests\SessionManagerTests.cs")
if ($LASTEXITCODE -ne 0) {
    throw "Session manager test compilation failed with exit code $LASTEXITCODE"
}

& $testExecutable
if ($LASTEXITCODE -ne 0) {
    throw "Session manager tests failed with exit code $LASTEXITCODE"
}
