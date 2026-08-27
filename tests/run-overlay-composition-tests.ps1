if ($PSVersionTable.PSEdition -eq 'Core') {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $MyInvocation.MyCommand.Path
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$assembly = [Reflection.Assembly]::LoadFrom(
    (Join-Path $root 'bin\Release\ControllerSessionManager.OverlayHost.exe'))
$type = $assembly.GetType('ControllerSessionManager.OverlayHost.OverlayWindow', $true)
$flags = [Reflection.BindingFlags]'Instance,Public,NonPublic'
$constructor = $type.GetConstructors($flags) |
    Where-Object { $_.GetParameters().Count -eq 2 } | Select-Object -First 1
$window = $constructor.Invoke(@([Diagnostics.Process]::GetCurrentProcess().Id, $null))

function Get-Field([string]$name) { $type.GetField($name, $flags).GetValue($window) }
function New-Style([string]$mode, [string]$order) {
    $parts = [string[]]::new(137)
    for ($index = 0; $index -lt $parts.Length; $index++) { $parts[$index] = '' }
    $parts[16] = 'True'; $parts[17] = 'True'; $parts[20] = 'True'
    $parts[25] = 'True'; $parts[26] = 'True'; $parts[27] = 'True'
    $parts[68] = $mode; $parts[82] = $order
    $parts[126] = 'True'; $parts[133] = 'True'; $parts[136] = 'True'
    return $parts -join ';'
}

(Get-Field 'titleText').Text = 'Controller disconnected'
(Get-Field 'messageText').Text = 'Test controller'
(Get-Field 'instructionText').Text = 'Reconnect it to continue.'
(Get-Field 'pauseStatusText').Text = 'Game paused'
(Get-Field 'incidentStateText').Text = 'DISCONNECTED'
(Get-Field 'disconnectTimerText').Text = 'Disconnected for 42s'

$formatterType = $assembly.GetType('ControllerSessionManager.Overlay.DisconnectDurationFormatter', $true)
$format = $formatterType.GetMethod('Format', [Type[]]@([TimeSpan]))
function Assert-Duration([TimeSpan]$elapsed, [string]$expected) {
    $actual = [string]$format.Invoke($null, @($elapsed))
    if ($actual -ne $expected) {
        throw "Duration format mismatch for $elapsed. Expected '$expected', got '$actual'."
    }
}
Assert-Duration ([TimeSpan]::FromSeconds(42)) '42s'
Assert-Duration ([TimeSpan]::FromSeconds(90)) '1m 30s'
Assert-Duration ([TimeSpan]::FromHours(1).Add([TimeSpan]::FromMinutes(2))) '1h 2m'
Assert-Duration ([TimeSpan]::FromDays(2).Add([TimeSpan]::FromHours(3))) '2d 3h'

$apply = $type.GetMethod('ApplyPresentationStyle', $flags)
$alert = New-Style 'Alert' 'Incident,Title,ControllerName,Timer,Metadata,Instruction,Status'
$standard = New-Style 'Standard' 'Title,Controller,Timer,Metadata,Instruction,Status'
$apply.Invoke($window, @($alert)) | Out-Null
$apply.Invoke($window, @($standard)) | Out-Null
$apply.Invoke($window, @($alert)) | Out-Null

Write-Host 'Overlay Alert/Standard composition switching with status metadata passed.'
