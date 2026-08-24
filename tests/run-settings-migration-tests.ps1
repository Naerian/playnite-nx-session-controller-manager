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

$notificationPresetType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.NotificationStylePresets", $true)
$applyNotificationPreset = $notificationPresetType.GetMethod("Apply", [Reflection.BindingFlags]"Static,Public")
$applyNotificationPreset.Invoke($null, @($settings, "Cinematic")) | Out-Null
if (-not $settings.NotificationUseBackgroundImage -or
    -not $settings.DesktopNotificationUseBackgroundImage -or
    -not (Test-Path -LiteralPath $settings.NotificationBackgroundImagePath)) {
    throw "Cinematic notification preset did not activate its bundled background image."
}
$applyNotificationPreset.Invoke($null, @($settings, "Soft")) | Out-Null
if ($settings.NotificationUseBackgroundImage -or $settings.DesktopNotificationUseBackgroundImage) {
    throw "A non-image notification preset retained the Cinematic background image."
}

# Visual profiles carry notification background images instead of exporting machine-local paths.
$imagePath = Join-Path $root "media\icon.png"
$settings.NotificationUseBackgroundImage = $true
$settings.NotificationBackgroundImagePath = $imagePath
$settings.NotificationBackgroundImageStretch = "UniformToFill"
$settings.NotificationBackgroundImageOpacity = 60
$snapshotType = $assembly.GetType("ControllerSessionManager.PlayniteIntegration.VisualProfileSnapshot", $true)
$fromSettings = $snapshotType.GetMethod("FromSettings", [Reflection.BindingFlags]"Static,Public")
$snapshot = $fromSettings.Invoke($null, @($settings, "Portable image test"))
if ([string]::IsNullOrWhiteSpace($snapshot.NotificationBackgroundImageData)) {
    throw "Visual profile did not embed the notification background image."
}
$restoredSettings = [Activator]::CreateInstance($type)
$restoreDirectory = Join-Path $root "obj\ProfileImageRestore"
New-Item -ItemType Directory -Path $restoreDirectory -Force | Out-Null
$applyTo = $snapshotType.GetMethods() | Where-Object {
    $_.Name -eq "ApplyTo" -and $_.GetParameters().Count -eq 2
} | Select-Object -First 1
$applyArguments = [object[]]::new(2)
$applyArguments[0] = $restoredSettings
$applyArguments[1] = [string]$restoreDirectory
$applyTo.Invoke($snapshot, $applyArguments) | Out-Null
if (-not $restoredSettings.NotificationUseBackgroundImage -or
    -not (Test-Path -LiteralPath $restoredSettings.NotificationBackgroundImagePath)) {
    throw "Visual profile did not restore the embedded notification background image."
}
if ((Get-FileHash -LiteralPath $imagePath).Hash -ne
    (Get-FileHash -LiteralPath $restoredSettings.NotificationBackgroundImagePath).Hash) {
    throw "Restored notification background image differs from the exported image."
}

# Imported notification backgrounds are bounded and re-encoded before being stored.
Add-Type -AssemblyName PresentationCore
$largeImagePath = Join-Path $root "obj\notification-background-source.jpg"
$optimizedImagePath = Join-Path $root "obj\notification-background-optimized.jpg"
Add-Type -AssemblyName System.Drawing
$largeBitmap = New-Object System.Drawing.Bitmap 2400,1200
try {
    $largeBitmap.Save($largeImagePath, [System.Drawing.Imaging.ImageFormat]::Jpeg)
}
finally {
    $largeBitmap.Dispose()
}
$sourceFrame = [System.Windows.Media.Imaging.BitmapFrame]::Create(
    [Uri]::new($largeImagePath, [UriKind]::Absolute))
$pluginType = $assembly.GetType("ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerPlugin", $true)
$optimizeMethod = $pluginType.GetMethod(
    "SaveOptimizedNotificationBackground", [Reflection.BindingFlags]"Static,NonPublic")
$optimizeArguments = [object[]]::new(3)
$optimizeArguments[0] = $sourceFrame
$optimizeArguments[1] = [string]$optimizedImagePath
$optimizeArguments[2] = [string]".jpg"
$optimizeMethod.Invoke($null, $optimizeArguments) | Out-Null
$optimizedFrame = [System.Windows.Media.Imaging.BitmapFrame]::Create(
    [Uri]::new($optimizedImagePath, [UriKind]::Absolute))
if ($optimizedFrame.PixelWidth -gt 1920 -or $optimizedFrame.PixelHeight -gt 1080) {
    throw "Optimized notification background exceeds 1920x1080."
}

Write-Host "Settings migration and overlay preset tests passed."
