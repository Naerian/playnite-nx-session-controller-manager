$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

[Reflection.Assembly]::LoadFrom("C:\Playnite\Playnite.SDK.dll") | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root "bin\Release\ControllerSessionManager.dll"))
$catalogType = $assembly.GetType("ControllerSessionManager.PlayniteIntegration.CreatorThemeCatalog", $true)
$configureCatalogArgs = [object[]]@([string](Join-Path $root "obj\EmptyCreatorPackPlugin"))
$catalogType.GetMethod("Configure").Invoke($null, $configureCatalogArgs) | Out-Null
$definitionType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.CreatorThemeDefinition", $true)
$definition = [Activator]::CreateInstance($definitionType)
$definition.Id = "example.creator"
$definition.Name = "Example Creator Design"
$definition.Author = "Test Author"
$definition.Version = "1.0.0"
$definition.Description = "Test-only creator design."
$definition.Directory = Join-Path $root "obj\TestCreatorPack"
$definition.Notification = [Collections.Generic.Dictionary[string, object]]::new()
$definition.Notification.Add("NotificationBackgroundColor", "#FF112233")
$definition.Notification.Add("NotificationUseBorderGradient", $true)
$definition.Notification.Add("NotificationShowBorderGlow", $true)
$definition.Notification.Add("DesktopNotificationBackgroundColor", "#FF223344")
$definition.Notification.Add("DesktopNotificationShowIconContainer", $true)
$definition.Overlay = [Collections.Generic.Dictionary[string, object]]::new()
$definition.Overlay.Add("OverlayCardColor", "#FF334455")
$definition.Overlay.Add("OverlayUseIndependentBorders", $true)
$definition.Overlay.Add("OverlayBlockOrder", "Title,Controller,Metadata,Instruction,Status")
$catalogFlags = [Reflection.BindingFlags]"Static,NonPublic"
$definitions = $catalogType.GetField("Definitions", $catalogFlags).GetValue($null)
$definitions.Add($definition.Id, $definition)
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
if ($settings.SettingsSchemaVersion -ne 21) {
    throw "Settings were not migrated to schema 21."
}
$notificationStateType = $assembly.GetType("ControllerSessionManager.PlayniteIntegration.NotificationStyleState", $true)
$legacyCustom = [Activator]::CreateInstance($type)
$legacyCustom.SettingsSchemaVersion = 20
$legacyCustom.NotificationStylePreset = "Custom"
$legacyCustom.DesktopNotificationStylePreset = "Custom"
$legacyFullscreenStyle = $notificationStateType.GetMethod("CaptureFullscreen").Invoke($null, @($legacyCustom))
$legacyDesktopStyle = $notificationStateType.GetMethod("CaptureDesktop").Invoke($null, @($legacyCustom))
$legacyFullscreenStyle.Remove("NotificationUseStateBorderColors") | Out-Null
$legacyFullscreenStyle.Remove("NotificationConnectedBorderColor") | Out-Null
$legacyFullscreenStyle.Remove("NotificationDisconnectedBorderColor") | Out-Null
$legacyFullscreenStyle.Remove("NotificationWarningBorderColor") | Out-Null
$legacyFullscreenStyle.Remove("NotificationLowBatteryBorderColor") | Out-Null
$legacyDesktopStyle.Remove("DesktopNotificationUseStateBorderColors") | Out-Null
$legacyDesktopStyle.Remove("DesktopNotificationConnectedBorderColor") | Out-Null
$legacyDesktopStyle.Remove("DesktopNotificationDisconnectedBorderColor") | Out-Null
$legacyDesktopStyle.Remove("DesktopNotificationWarningBorderColor") | Out-Null
$legacyDesktopStyle.Remove("DesktopNotificationLowBatteryBorderColor") | Out-Null
$legacyCustom.SavedCustomNotificationStyle = $legacyFullscreenStyle
$legacyCustom.SavedCustomDesktopNotificationStyle = $legacyDesktopStyle
$method.Invoke($legacyCustom, $null) | Out-Null
if ($legacyCustom.HasUnsavedCustomNotificationStyle -or $legacyCustom.HasUnsavedCustomDesktopNotificationStyle) {
    throw "Schema 21 introduced a false unsaved warning for an existing Custom style."
}
if ($settings.DesktopNotificationStylePreset -ne $settings.NotificationStylePreset) {
    throw "The existing notification preset was not migrated to both destinations."
}

if ($settings.NotificationTextOrder -ne "TitleFirst" -or
    $settings.OverlayBlockOrder -ne "Title,Controller,Metadata,Instruction,Status" -or
    $settings.OverlayMetadataOrientation -ne "Horizontal") {
    throw "Advanced appearance migration did not preserve the legacy composition."
}
if ($settings.NotificationTitleFontFamily -ne $settings.NotificationFontFamily -or
    $settings.NotificationTitleFontWeight -ne $settings.NotificationFontWeight -or
    $settings.NotificationMessageFontFamily -ne $settings.NotificationFontFamily -or
    $settings.NotificationMessageFontWeight -ne $settings.NotificationFontWeight -or
    $settings.NotificationMessageMaxLines -ne 2 -or
    $settings.NotificationBadgePosition -ne "TopRight" -or
    $settings.OverlayContentAlignment -ne "Center" -or
    $settings.OverlayScreenMargin -ne 42) {
    throw "Advanced appearance migration did not preserve the legacy rendering."
}
if ($settings.NotificationUseGradient -or $settings.DesktopNotificationUseGradient -or
    $settings.OverlayUseGradient -or $settings.NotificationUppercaseTitle -or
    $settings.DesktopNotificationUppercaseTitle -or $settings.OverlayUppercaseTitle -or
    $settings.NotificationUseBorderGradient -or $settings.DesktopNotificationUseBorderGradient -or
    $settings.OverlayUseBorderGradient -or $settings.NotificationShowBorderGlow -or
    $settings.DesktopNotificationShowBorderGlow -or $settings.OverlayShowBorderGlow) {
    throw "Gradient/title migration changed a legacy visual style."
}
if ($settings.NotificationShowIconContainer -or
    $settings.DesktopNotificationShowIconContainer -or
    $settings.OverlayShowControllerContainer -or
    $settings.OverlayUseBackgroundImage -or
    $settings.OverlayLayoutMode -ne "Standard") {
    throw "Advanced container/composition migration changed a legacy visual style."
}
if ($settings.NotificationIconSpacing -ne 8 -or
    $settings.DesktopNotificationIconSpacing -ne 8) {
    throw "Migration did not preserve the legacy icon-to-content gap."
}
if (-not $settings.EnableDesktopNotificationSounds -or
    -not $settings.EnableFullscreenNotificationSounds -or
    $settings.NotificationPreviewWithSound) {
    throw "Notification sound scope migration has unsafe defaults."
}
$legacyMuted = [Activator]::CreateInstance($type)
$legacyMuted.SettingsSchemaVersion = 13
$legacyMuted.EnableNotificationSounds = $false
$method.Invoke($legacyMuted, $null) | Out-Null
if ($legacyMuted.EnableDesktopNotificationSounds -or
    $legacyMuted.EnableFullscreenNotificationSounds -or
    -not $legacyMuted.EnableNotificationSounds) {
    throw "The removed master sound switch was not migrated to both destinations."
}
if (-not $settings.HasSavedCustomNotificationStyle) {
    throw "The existing custom notification style was not preserved for later restoration."
}

# A saved Custom style survives named presets, and copy operations include the new icon gap.
$settings.NotificationWidth = 701
$settings.NotificationIconSpacing = 23
$settings.SaveCurrentNotificationStyleAsCustom()
$settings.NotificationWidth = 401
$settings.NotificationIconSpacing = 4
if (-not $settings.HasUnsavedCustomNotificationStyle) {
    throw "Unsaved custom notification changes were not detected."
}
$settings.RestoreSavedCustomNotificationStyle() | Out-Null
if ($settings.NotificationWidth -ne 701 -or $settings.NotificationIconSpacing -ne 23) {
    throw "The saved Custom notification style was not restored."
}
$settings.DesktopNotificationWidth = 612
$settings.SaveCurrentDesktopNotificationStyleAsCustom()
if ($settings.HasUnsavedCustomDesktopNotificationStyle) {
    throw "An untouched saved desktop Custom style was incorrectly marked as dirty."
}
$settings.DesktopNotificationWidth = 613
if (-not $settings.HasUnsavedCustomDesktopNotificationStyle) {
    throw "Desktop Custom changes were not detected independently."
}
$settings.RestoreSavedCustomDesktopNotificationStyle() | Out-Null
$settings.DesktopNotificationIconSpacing = 31
$styleStateType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.NotificationStyleState", $true)
$copyDesktop = $styleStateType.GetMethod("CopyDesktopToFullscreen", [Reflection.BindingFlags]"Static,Public")
$copyDesktop.Invoke($null, @($settings)) | Out-Null
if ($settings.NotificationIconSpacing -ne 31) {
    throw "Copying the desktop notification style omitted icon-to-content spacing."
}
$settings.DesktopNotificationTitleFontFamily = "Orbitron"
$settings.DesktopNotificationMessageFontWeight = "Regular"
$settings.DesktopNotificationMessageMaxLines = 5
$settings.DesktopNotificationBadgePosition = "TopLeft"
$settings.DesktopNotificationShowIconContainer = $true
$settings.DesktopNotificationIconContainerPadding = 17
$copyDesktop.Invoke($null, @($settings)) | Out-Null
if ($settings.NotificationTitleFontFamily -ne "Orbitron" -or
    $settings.NotificationMessageFontWeight -ne "Regular" -or
    $settings.NotificationMessageMaxLines -ne 5 -or
    $settings.NotificationBadgePosition -ne "TopLeft" -or
    -not $settings.NotificationShowIconContainer -or
    $settings.NotificationIconContainerPadding -ne 17) {
    throw "Copying the desktop notification style omitted advanced typography, badge placement, or icon container."
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
$applyPreset.Invoke($null, @($settings, "example.creator")) | Out-Null
if ($settings.OverlayCardColor -ne "#FF334455" -or
    -not $settings.OverlayUseIndependentBorders -or
    $settings.OverlayBlockOrder -ne "Title,Controller,Metadata,Instruction,Status") {
    throw "The creator overlay fixture does not retain its authored appearance."
}

$notificationPresetType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.NotificationStylePresets", $true)
$applyNotificationPreset = $notificationPresetType.GetMethod("Apply", [Reflection.BindingFlags]"Static,Public")
$applyNotificationPreset.Invoke($null, @($settings, "Cinematic")) | Out-Null
if (-not $settings.NotificationUseBackgroundImage -or
    -not $settings.DesktopNotificationUseBackgroundImage -or
    -not (Test-Path -LiteralPath $settings.NotificationBackgroundImagePath)) {
    throw "Cinematic notification preset did not activate its bundled background image."
}
$applyNotificationPreset.Invoke($null, @($settings, "example.creator")) | Out-Null
if ($settings.NotificationBackgroundColor -ne "#FF112233" -or
    $settings.DesktopNotificationBackgroundColor -ne "#FF223344" -or
    -not $settings.NotificationUseBorderGradient -or
    -not $settings.NotificationShowBorderGlow -or
    -not $settings.DesktopNotificationShowIconContainer) {
    throw "The creator notification fixture does not retain its authored appearance."
}
$applyNotificationPreset.Invoke($null, @($settings, "Soft")) | Out-Null
if ($settings.NotificationUseBackgroundImage -or $settings.DesktopNotificationUseBackgroundImage) {
    throw "A non-image notification preset retained the Cinematic background image."
}

# Removed development presets are never presented as plugin presets and become Custom
# so their already-materialized appearance remains untouched in existing profiles.
$normalizeNotification = $notificationPresetType.GetMethod("Normalize", [Reflection.BindingFlags]"Static,Public")
$normalizeOverlay = $presetType.GetMethod("Normalize", [Reflection.BindingFlags]"Static,Public")
if ($normalizeNotification.Invoke($null, @("Studio")) -ne "Custom" -or
    $normalizeNotification.Invoke($null, @("NeonPulse")) -ne "Custom" -or
    $normalizeNotification.Invoke($null, @("removed.creator")) -ne "Custom" -or
    $normalizeOverlay.Invoke($null, @("Studio")) -ne "Custom" -or
    $normalizeOverlay.Invoke($null, @("NeonPulse")) -ne "Custom" -or
    $normalizeOverlay.Invoke($null, @("removed.creator")) -ne "Custom") {
    throw "Removed or unavailable presets were not migrated safely to Custom."
}

# Visual profiles carry notification background images instead of exporting machine-local paths.
$imagePath = Join-Path $root "media\icon.png"
$settings.NotificationUseBackgroundImage = $true
$settings.NotificationBackgroundImagePath = $imagePath
$settings.NotificationBackgroundImageStretch = "UniformToFill"
$settings.NotificationBackgroundImageOpacity = 60
$settings.NotificationUseGradient = $true
$settings.NotificationGradientColor = "#FF220044"
$settings.NotificationGradientAngle = 47
$settings.NotificationUppercaseTitle = $true
$settings.OverlayUseGradient = $true
$settings.OverlayGradientColor = "#FF001F33"
$settings.OverlayGradientAngle = 73
$settings.NotificationShowIconContainer = $true
$settings.NotificationIconContainerPadding = 19
$settings.OverlayUseBackgroundImage = $true
$settings.OverlayBackgroundImagePath = $imagePath
$settings.OverlayBackgroundImageOpacity = 52
$settings.OverlayBackgroundImageTintOpacity = 71
$settings.OverlayShowControllerContainer = $true
$settings.OverlayControllerContainerPadding = 18
$settings.OverlayLayoutMode = "Hero"
$settings.NotificationTextOrder = "MessageFirst"
$settings.NotificationUseIndependentBorders = $true
$settings.NotificationBorderLeftThickness = 7
$settings.NotificationUseStateBackgroundColors = $true
$settings.NotificationConnectedBackgroundColor = "#FF102030"
$settings.OverlayBlockOrder = "Status,Title,Controller,Metadata,Instruction"
$settings.OverlayMetadataOrientation = "Vertical"
$settings.OverlayUseIndependentBorders = $true
$settings.OverlayBorderRightThickness = 8
$settings.NotificationUseBorderGradient = $true
$settings.NotificationBorderGradientStartColor = "#22112233"
$settings.NotificationBorderGradientEndColor = "#AA445566"
$settings.NotificationBorderGradientAngle = 123
$settings.NotificationShowBorderGlow = $true
$settings.NotificationBorderGlowColor = "#88778899"
$settings.NotificationBorderGlowBlur = 21
$settings.NotificationBorderGlowOpacity = 37
$settings.OverlayUseBorderGradient = $true
$settings.OverlayBorderGradientStartColor = "#33445566"
$settings.OverlayBorderGradientEndColor = "#AA778899"
$settings.OverlayShowBorderGlow = $true
$settings.OverlayBorderGlowBlur = 28
$settings.DesktopNotificationStylePreset = "Compact"
$snapshotType = $assembly.GetType("ControllerSessionManager.PlayniteIntegration.VisualProfileSnapshot", $true)
$fromSettings = $snapshotType.GetMethod("FromSettings", [Reflection.BindingFlags]"Static,Public")
$snapshot = $fromSettings.Invoke($null, @($settings, "Portable image test"))
if ([string]::IsNullOrWhiteSpace($snapshot.NotificationBackgroundImageData)) {
    throw "Visual profile did not embed the notification background image."
}
if ([string]::IsNullOrWhiteSpace($snapshot.OverlayBackgroundImageData)) {
    throw "Visual profile did not embed the overlay background image."
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
if ($restoredSettings.NotificationTitleFontFamily -ne $settings.NotificationTitleFontFamily -or
    $restoredSettings.NotificationMessageFontWeight -ne $settings.NotificationMessageFontWeight -or
    $restoredSettings.NotificationMessageMaxLines -ne $settings.NotificationMessageMaxLines -or
    $restoredSettings.NotificationBadgePosition -ne $settings.NotificationBadgePosition -or
    $restoredSettings.OverlayContentAlignment -ne $settings.OverlayContentAlignment -or
    $restoredSettings.OverlayScreenMargin -ne $settings.OverlayScreenMargin -or
    $restoredSettings.NotificationUseGradient -ne $settings.NotificationUseGradient -or
    $restoredSettings.NotificationGradientColor -ne $settings.NotificationGradientColor -or
    $restoredSettings.NotificationGradientAngle -ne $settings.NotificationGradientAngle -or
    $restoredSettings.NotificationUppercaseTitle -ne $settings.NotificationUppercaseTitle -or
    $restoredSettings.OverlayUseGradient -ne $settings.OverlayUseGradient -or
    $restoredSettings.OverlayGradientAngle -ne $settings.OverlayGradientAngle -or
    $restoredSettings.NotificationShowIconContainer -ne $settings.NotificationShowIconContainer -or
    $restoredSettings.NotificationIconContainerPadding -ne $settings.NotificationIconContainerPadding -or
    $restoredSettings.OverlayShowControllerContainer -ne $settings.OverlayShowControllerContainer -or
    $restoredSettings.OverlayControllerContainerPadding -ne $settings.OverlayControllerContainerPadding -or
    $restoredSettings.OverlayLayoutMode -ne $settings.OverlayLayoutMode -or
    $restoredSettings.NotificationTextOrder -ne $settings.NotificationTextOrder -or
    $restoredSettings.NotificationUseIndependentBorders -ne $settings.NotificationUseIndependentBorders -or
    $restoredSettings.NotificationBorderLeftThickness -ne $settings.NotificationBorderLeftThickness -or
    $restoredSettings.NotificationConnectedBackgroundColor -ne $settings.NotificationConnectedBackgroundColor -or
    $restoredSettings.OverlayBlockOrder -ne $settings.OverlayBlockOrder -or
    $restoredSettings.OverlayMetadataOrientation -ne $settings.OverlayMetadataOrientation -or
    $restoredSettings.OverlayBorderRightThickness -ne $settings.OverlayBorderRightThickness -or
    $restoredSettings.NotificationUseBorderGradient -ne $settings.NotificationUseBorderGradient -or
    $restoredSettings.NotificationBorderGradientStartColor -ne $settings.NotificationBorderGradientStartColor -or
    $restoredSettings.NotificationBorderGradientAngle -ne $settings.NotificationBorderGradientAngle -or
    $restoredSettings.NotificationShowBorderGlow -ne $settings.NotificationShowBorderGlow -or
    $restoredSettings.NotificationBorderGlowBlur -ne $settings.NotificationBorderGlowBlur -or
    $restoredSettings.OverlayUseBorderGradient -ne $settings.OverlayUseBorderGradient -or
    $restoredSettings.OverlayBorderGradientEndColor -ne $settings.OverlayBorderGradientEndColor -or
    $restoredSettings.OverlayShowBorderGlow -ne $settings.OverlayShowBorderGlow -or
    $restoredSettings.OverlayBorderGlowBlur -ne $settings.OverlayBorderGlowBlur -or
    $restoredSettings.DesktopNotificationStylePreset -ne $settings.DesktopNotificationStylePreset -or
    $restoredSettings.OverlayBackgroundImageOpacity -ne $settings.OverlayBackgroundImageOpacity -or
    $restoredSettings.OverlayBackgroundImageTintOpacity -ne $settings.OverlayBackgroundImageTintOpacity) {
    throw "Visual profile did not restore advanced notification and overlay appearance."
}
if (-not $restoredSettings.NotificationUseBackgroundImage -or
    -not (Test-Path -LiteralPath $restoredSettings.NotificationBackgroundImagePath)) {
    throw "Visual profile did not restore the embedded notification background image."
}
if (-not $restoredSettings.OverlayUseBackgroundImage -or
    -not (Test-Path -LiteralPath $restoredSettings.OverlayBackgroundImagePath)) {
    throw "Visual profile did not restore the embedded overlay background image."
}
if ((Get-FileHash -LiteralPath $imagePath).Hash -ne
    (Get-FileHash -LiteralPath $restoredSettings.OverlayBackgroundImagePath).Hash) {
    throw "Restored overlay background image differs from the exported image."
}
if ((Get-FileHash -LiteralPath $imagePath).Hash -ne
    (Get-FileHash -LiteralPath $restoredSettings.NotificationBackgroundImagePath).Hash) {
    throw "Restored notification background image differs from the exported image."
}

# Visual profiles also carry custom audio and the independent sound-scope settings.
$customSoundPath = Join-Path $root "Audio\1_Modern_Crystal\connected.wav"
$settings.CustomConnectedSoundPath = $customSoundPath
$settings.EnableDesktopNotificationSounds = $false
$settings.EnableFullscreenNotificationSounds = $true
$soundSnapshot = $fromSettings.Invoke($null, @($settings, "Portable sound test"))
if ([string]::IsNullOrWhiteSpace($soundSnapshot.CustomConnectedSoundData)) {
    throw "Visual profile did not embed the custom connected sound."
}
$soundRestoredSettings = [Activator]::CreateInstance($type)
$soundRestoreDirectory = Join-Path $root "obj\ProfileSoundRestore"
New-Item -ItemType Directory -Path $soundRestoreDirectory -Force | Out-Null
$applyToPortable = $snapshotType.GetMethods() | Where-Object {
    $_.Name -eq "ApplyTo" -and $_.GetParameters().Count -eq 3
} | Select-Object -First 1
$soundArguments = [object[]]::new(3)
$soundArguments[0] = $soundRestoredSettings
$soundArguments[1] = [string]$restoreDirectory
$soundArguments[2] = [string]$soundRestoreDirectory
$applyToPortable.Invoke($soundSnapshot, $soundArguments) | Out-Null
if ($soundRestoredSettings.EnableDesktopNotificationSounds -or
    -not $soundRestoredSettings.EnableFullscreenNotificationSounds -or
    -not (Test-Path -LiteralPath $soundRestoredSettings.CustomConnectedSoundPath)) {
    throw "Visual profile did not restore custom audio and notification sound scopes."
}
if ((Get-FileHash -LiteralPath $customSoundPath).Hash -ne
    (Get-FileHash -LiteralPath $soundRestoredSettings.CustomConnectedSoundPath).Hash) {
    throw "Restored custom notification sound differs from the exported sound."
}

$audioServiceType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.NotificationAudioService", $true)
$soundKindType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.NotificationSoundKind", $true)
$soundScopeType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.NotificationSoundScope", $true)
$connectedKind = [Enum]::Parse($soundKindType, "Connected")
$desktopScope = [Enum]::Parse($soundScopeType, "Desktop")
$fullscreenScope = [Enum]::Parse($soundScopeType, "Fullscreen")
$loggerType = [Playnite.SDK.ILogger]
$audioConstructor = $audioServiceType.GetConstructor(@($loggerType, [string]))
$audioConstructorArguments = [object[]]::new(2)
$audioConstructorArguments[0] = $null
$audioConstructorArguments[1] = [string]$root
$audioService = $audioConstructor.Invoke($audioConstructorArguments)
$resolveCustom = $audioServiceType.GetMethods() | Where-Object {
    $_.Name -eq "ResolvePath" -and $_.GetParameters().Count -eq 2 -and
    $_.GetParameters()[1].ParameterType -eq $type
} | Select-Object -First 1
if ($resolveCustom.Invoke($audioService, @($connectedKind, $settings)) -ne $customSoundPath) {
    throw "Notification playback did not prefer the selected custom sound."
}
$isScopeEnabled = $audioServiceType.GetMethod(
    "IsScopeEnabled", [Reflection.BindingFlags]"Static,Public")
if ($isScopeEnabled.Invoke($null, @($desktopScope, $settings)) -or
    -not $isScopeEnabled.Invoke($null, @($fullscreenScope, $settings))) {
    throw "Desktop and fullscreen notification sound switches are not independent."
}
$audioService.Dispose()

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

# Creator designs discovered from disk remain locked while selected.
$creatorLockSettings = [Activator]::CreateInstance($type)
$creatorLockSettings.NotificationStylePreset = "example.creator"
$creatorLockSettings.DesktopNotificationStylePreset = "example.creator"
$creatorLockSettings.OverlayStylePreset = "example.creator"
if (-not $creatorLockSettings.IsFullscreenNotificationCreatorThemeActive -or
    -not $creatorLockSettings.IsDesktopNotificationCreatorThemeActive -or
    -not $creatorLockSettings.IsOverlayCreatorThemeActive -or
    $creatorLockSettings.CanEditFullscreenNotificationStyle -or
    $creatorLockSettings.CanEditDesktopNotificationStyle -or
    $creatorLockSettings.CanEditOverlayStyle -or
    $creatorLockSettings.CanCopyNotificationStyles) {
    throw "Creator designs were not classified as locked after catalog discovery."
}

Write-Host "Settings migration and overlay preset tests passed."
