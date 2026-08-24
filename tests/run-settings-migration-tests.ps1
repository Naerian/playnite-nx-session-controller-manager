$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

[Reflection.Assembly]::LoadFrom("C:\Playnite\Playnite.SDK.dll") | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root "bin\Release\ControllerSessionManager.dll"))
$type = $assembly.GetType("ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerSettings", $true)
$settings = [Activator]::CreateInstance($type)

$preserved = @{
    NotificationWidth = 777
    NotificationScalePercent = 135
    NotificationBackgroundColor = "#D0112233"
    NotificationPadding = 7
    NotificationCornerRadius = 29
    DesktopNotificationWidth = 633
    DesktopNotificationBackgroundColor = "#C0445566"
    DesktopNotificationPadding = 3
    DesktopNotificationCornerRadius = 17
    OverlayScalePercent = 137
    OverlayCardColor = "#B0778899"
    OverlayPadding = 53
    OverlayCornerRadius = 31
    OverlayCardWidth = 940
    OverlayCardPosition = "TopLeft"
}

$settings.SettingsSchemaVersion = 8
foreach ($entry in $preserved.GetEnumerator()) {
    $settings.($entry.Key) = $entry.Value
}

$method = $type.GetMethod("MigrateSettings", [Reflection.BindingFlags]"Instance,NonPublic")
$method.Invoke($settings, $null) | Out-Null

foreach ($entry in $preserved.GetEnumerator()) {
    $actual = $settings.($entry.Key)
    if ($actual -ne $entry.Value) {
        throw "Migration changed $($entry.Key): expected '$($entry.Value)', got '$actual'."
    }
}

if ($settings.NotificationStylePreset -ne "Custom" -or $settings.OverlayStylePreset -ne "Custom") {
    throw "An existing installation must retain its custom notification and overlay appearance."
}

# The removed Medium selector remains import-compatible and maps to the visually equivalent face.
$settings.NotificationFontWeight = "Medium"
if ($settings.NotificationFontWeight -ne "SemiBold") {
    throw "Legacy Medium font weight was not migrated to SemiBold."
}

$presetType = $assembly.GetType("ControllerSessionManager.PlayniteIntegration.OverlayStylePresets", $true)
$applyPreset = $presetType.GetMethod("Apply", [Reflection.BindingFlags]"Static,Public")
$applyPreset.Invoke($null, @($settings, "Compact")) | Out-Null
if ($settings.OverlayCardPosition -ne "Center" -or $settings.OverlayBorderPosition -ne "Top") {
    throw "Compact preset must be centered with a top border."
}
$applyPreset.Invoke($null, @($settings, "Bold")) | Out-Null
if ($settings.OverlayScalePercent -ne 100) { throw "Bold preset must use 100% scale." }
$applyPreset.Invoke($null, @($settings, "Arcade")) | Out-Null
if ($settings.OverlayScalePercent -ne 110) { throw "Arcade preset must use 110% scale." }

Write-Host "Settings migration and overlay preset tests passed."
