$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$viewSource = Get-Content -Raw (Join-Path $root "PlayniteIntegration\ControllerSessionManagerSettingsView.cs")
$pluginSource = Get-Content -Raw (Join-Path $root "PlayniteIntegration\ControllerSessionManagerPlugin.cs")
if ($viewSource -notmatch 'plugin\.ShowNotificationPresetPreview\s*\(\s*\)') {
    throw "Changing a notification style preset must launch its automatic preview."
}
if ($pluginSource -notmatch 'ShowDesktopNotificationPreview\s*\(\s*"connected"\s*,\s*false\s*\)' -or
    $pluginSource -notmatch 'ShowNotificationPreview\s*\(\s*"connected"\s*,\s*false\s*\)') {
    throw "The automatic notification preset preview must explicitly disable sound."
}

[Reflection.Assembly]::LoadFrom("C:\Playnite\Playnite.SDK.dll") | Out-Null
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName WindowsBase

$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root "bin\Release\ControllerSessionManager.dll"))
$catalogType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.CreatorThemeCatalog", $true)
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
$definition.Overlay = [Collections.Generic.Dictionary[string, object]]::new()
$definition.Overlay.Add("OverlayCardColor", "#FF334455")
$catalogFlags = [Reflection.BindingFlags]"Static,NonPublic"
$definitions = $catalogType.GetField("Definitions", $catalogFlags).GetValue($null)
$viewType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerSettingsView", $true)
$pluginType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerPlugin", $true)
$settingsType = $assembly.GetType(
    "ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerSettings", $true)
$constructor = $viewType.GetConstructor(@($pluginType))
if ($null -eq $constructor) {
    throw "Settings view test constructor was not found."
}

$view = $constructor.Invoke(@($null))
$selector = $view.FindName("NotificationSoundPackSelector")
if ($null -eq $selector) {
    throw "Settings view XAML did not create the notification sound pack selector."
}

$settings = [Activator]::CreateInstance($settingsType)
$settings.NotificationSoundPack = "5_Minimal_Soft"
$settings.NotificationStylePreset = "Custom"
$settings.DesktopNotificationStylePreset = "Custom"
$settings.SaveCurrentNotificationStyleAsCustom()
$settings.SaveCurrentDesktopNotificationStyleAsCustom()
$settings.NotificationWidth += 7
$settings.DesktopNotificationWidth += 9
$unsavedFullscreenWidth = $settings.NotificationWidth
$unsavedDesktopWidth = $settings.DesktopNotificationWidth
$view.DataContext = $settings
# DataContext assignment reloads the disk catalog. Reinsert the in-memory fixture because
# PowerShell 7 cannot instantiate the plugin's .NET Framework JavaScriptSerializer reliably.
$definitions.Add($definition.Id, $definition)
$viewFlags = [Reflection.BindingFlags]"Instance,NonPublic"
foreach ($methodName in @(
    "BuildNotificationStylePresetChips", "BuildNotificationPresetSelectors",
    "BuildOverlayStylePresetChips", "BuildOverlayPresetSelector")) {
    $viewType.GetMethod($methodName, $viewFlags).Invoke($view, $null) | Out-Null
}
$window = New-Object System.Windows.Window
$window.Content = $view
$window.Show()
$window.UpdateLayout()
if ($view.FindName("DesktopAlertsExpander").IsExpanded -or
    $view.FindName("DesktopDesignExpander").IsExpanded -or
    $view.FindName("DesktopAppearanceLayoutExpander").IsExpanded -or
    $view.FindName("FullscreenAlertsExpander").IsExpanded -or
    $view.FindName("FullscreenDesignExpander").IsExpanded -or
    $view.FindName("FullscreenAppearanceLayoutExpander").IsExpanded -or
    $view.FindName("OverlayDesignExpander").IsExpanded -or
    $view.FindName("OverlayAppearanceLayoutExpander").IsExpanded) {
    throw "Notification and overlay child sections must start collapsed."
}
if ($view.FindName("DesktopAdvancedDesignExpander").Visibility -ne [Windows.Visibility]::Collapsed -or
    $view.FindName("FullscreenAdvancedDesignExpander").Visibility -ne [Windows.Visibility]::Collapsed -or
    $view.FindName("OverlayAdvancedDesignExpander").Visibility -ne [Windows.Visibility]::Collapsed) {
    throw "Advanced Design must remain hidden from the normal settings interface."
}
if ($settings.NotificationStylePreset -ne "Custom" -or
    $settings.DesktopNotificationStylePreset -ne "Custom" -or
    $settings.NotificationWidth -ne $unsavedFullscreenWidth -or
    $settings.DesktopNotificationWidth -ne $unsavedDesktopWidth) {
    throw "Opening the settings view applied a notification preset or discarded unsaved custom values."
}
$desktopPresetSelector = $view.FindName("DesktopNotificationPresetSelector")
$fullscreenPresetSelector = $view.FindName("FullscreenNotificationPresetSelector")
$overlayPresetSelector = $view.FindName("OverlayPresetSelector")
if ($null -eq $desktopPresetSelector -or $null -eq $fullscreenPresetSelector -or
    $null -eq $overlayPresetSelector) {
    throw "The destination-specific appearance preset selectors were not created."
}
if ($desktopPresetSelector.Items.Count -ne 10 -or $fullscreenPresetSelector.Items.Count -ne 10 -or
    $overlayPresetSelector.Items.Count -ne 9) {
    throw "The grouped appearance selectors do not contain all plugin, creator, and custom presets."
}
if ($desktopPresetSelector.Items[0].Key -ne "Custom" -or
    $fullscreenPresetSelector.Items[0].Key -ne "Custom" -or
    $overlayPresetSelector.Items[0].Key -ne "Custom" -or
    @($desktopPresetSelector.Items | Where-Object { $_.IsHeader }).Count -ne 2 -or
    @($fullscreenPresetSelector.Items | Where-Object { $_.IsHeader }).Count -ne 2 -or
    @($overlayPresetSelector.Items | Where-Object { $_.IsHeader }).Count -ne 2 -or
    @($desktopPresetSelector.Items | Where-Object { $_.IsSelectable }).Count -ne 8) {
    throw "Appearance presets are not grouped into plugin, creator, and custom designs."
}
$notificationPluginPresets = $view.FindName("NotificationPluginPresetChips")
$notificationCreatorPresets = $view.FindName("NotificationCreatorPresetChips")
$notificationCustomPresets = $view.FindName("NotificationCustomPresetChips")
$overlayPluginPresets = $view.FindName("OverlayPluginPresetChips")
$overlayCreatorPresets = $view.FindName("OverlayCreatorPresetChips")
$overlayCustomPresets = $view.FindName("OverlayCustomPresetChips")
if ($notificationPluginPresets.Children.Count -ne 6 -or
    $notificationCreatorPresets.Children.Count -ne 1 -or
    $notificationCustomPresets.Children.Count -ne 1 -or
    $overlayPluginPresets.Children.Count -ne 5 -or
    $overlayCreatorPresets.Children.Count -ne 1 -or
    $overlayCustomPresets.Children.Count -ne 1) {
    throw "Plugin, creator, and custom appearance presets are not separated correctly."
}
$creatorCard = $notificationCreatorPresets.Children[0]
if ($creatorCard.Tag -ne "example.creator" -or
    $creatorCard.Content.Children[1].Text -ne "Test Author") {
    throw "Creator preset cards do not expose their design attribution."
}
if ($selector.Items.Count -lt 1) {
    throw "Settings view did not populate the notification sound packs."
}
if ($selector.SelectedValue -ne $settings.NotificationSoundPack) {
    throw "Settings view did not select the configured notification sound pack."
}
if ($settings.NotificationSoundPack -ne "5_Minimal_Soft") {
    throw "Opening the settings view replaced the saved notification sound pack."
}

$desktopPresetSelector.SelectedItem = @($desktopPresetSelector.Items | Where-Object { $_.Key -eq "example.creator" })[0]
$window.UpdateLayout()
if ($settings.DesktopNotificationStylePreset -ne "example.creator" -or
    $settings.NotificationStylePreset -eq "example.creator") {
    throw "Selecting a desktop notification design also changed the fullscreen destination " +
        "(desktop=$($settings.DesktopNotificationStylePreset), fullscreen=$($settings.NotificationStylePreset))."
}
if ($view.FindName("DesktopNotificationStyleEditor").IsEnabled -or
    -not $view.FindName("FullscreenNotificationStyleEditor").IsEnabled -or
    $view.FindName("NotificationAudioEditor").IsEnabled -or
    $view.FindName("CopyFullscreenToDesktopButton").IsEnabled -or
    $view.FindName("CopyDesktopToFullscreenButton").IsEnabled -or
    $view.FindName("DesktopAppearanceLayoutExpander").IsExpanded -or
    $view.FindName("DesktopNotificationStyleEditor").Opacity -gt 0.5) {
    throw "Creator notification designs did not lock only their editor and the shared audio editor " +
        "(desktop=$($view.FindName('DesktopNotificationStyleEditor').IsEnabled), " +
        "fullscreen=$($view.FindName('FullscreenNotificationStyleEditor').IsEnabled), " +
        "audio=$($view.FindName('NotificationAudioEditor').IsEnabled), " +
        "opacity=$($view.FindName('DesktopNotificationStyleEditor').Opacity), " +
        "flags=$($settings.CanEditDesktopNotificationStyle)/$($settings.CanEditFullscreenNotificationStyle)/$($settings.CanEditNotificationAudio))."
}
$desktopPresetSelector.SelectedItem = @($desktopPresetSelector.Items | Where-Object { $_.Key -eq "Custom" })[0]
$window.UpdateLayout()
if (-not $view.FindName("DesktopNotificationStyleEditor").IsEnabled -or
    -not $view.FindName("NotificationAudioEditor").IsEnabled -or
    $view.FindName("DesktopNotificationStyleEditor").Opacity -lt 0.9) {
    throw "Returning to Custom did not unlock notification appearance and audio editing."
}
$overlayPresetSelector.SelectedItem = @($overlayPresetSelector.Items | Where-Object { $_.Key -eq "example.creator" })[0]
$window.UpdateLayout()
if ($settings.OverlayStylePreset -ne "example.creator" -or
    $view.FindName("OverlayStyleEditor").IsEnabled -or
    $view.FindName("OverlayAppearanceLayoutExpander").IsExpanded -or
    $view.FindName("OverlayStyleEditor").Opacity -gt 0.5) {
    throw "Creator overlay designs did not lock the overlay appearance editor " +
        "(preset=$($settings.OverlayStylePreset), editor=$($view.FindName('OverlayStyleEditor').IsEnabled), " +
        "expanded=$($view.FindName('OverlayAppearanceLayoutExpander').IsExpanded), " +
        "opacity=$($view.FindName('OverlayStyleEditor').Opacity))."
}
$overlayPresetSelector.SelectedItem = @($overlayPresetSelector.Items | Where-Object { $_.Key -eq "Custom" })[0]
$window.UpdateLayout()
if (-not $view.FindName("OverlayStyleEditor").IsEnabled) {
    throw "Returning the overlay to Custom did not unlock its appearance editor."
}

$settings.NotificationSoundPack = "4_Retro_Arcade"
if ($selector.SelectedValue -ne $settings.NotificationSoundPack) {
    throw "Settings view did not follow an externally changed notification sound pack."
}
$selector.SelectedIndex = 6
$window.UpdateLayout()
if ($settings.NotificationSoundPack -ne "7_Handheld_Haptic") {
    $refreshField = $viewType.GetField(
        "refreshingNotificationSoundPackSelection", [Reflection.BindingFlags]"Instance,NonPublic")
    throw "Selecting a notification sound pack did not update the settings model " +
        "(selector=$($selector.SelectedValue), settings=$($settings.NotificationSoundPack), " +
        "refreshing=$($refreshField.GetValue($view)))."
}

$desktopImageRow = $view.FindName("DesktopBackgroundImagePickerRow")
$fullscreenImageRow = $view.FindName("FullscreenBackgroundImagePickerRow")
$overlayImageRow = $view.FindName("OverlayBackgroundImagePickerRow")
if ($desktopImageRow.IsEnabled -or $fullscreenImageRow.IsEnabled -or $overlayImageRow.IsEnabled) {
    throw "Background image selectors must be disabled while background images are disabled " +
        "(desktop=$($desktopImageRow.IsEnabled), fullscreen=$($fullscreenImageRow.IsEnabled); " +
        "settings=$($settings.DesktopNotificationUseBackgroundImage)/$($settings.NotificationUseBackgroundImage); " +
        "bindings=$($desktopImageRow.GetBindingExpression([System.Windows.UIElement]::IsEnabledProperty).Status)/" +
        "$($fullscreenImageRow.GetBindingExpression([System.Windows.UIElement]::IsEnabledProperty).Status))."
}
$settings.DesktopNotificationUseBackgroundImage = $true
$settings.NotificationUseBackgroundImage = $true
$settings.OverlayUseBackgroundImage = $true
if (-not $desktopImageRow.IsEnabled -or -not $fullscreenImageRow.IsEnabled -or
    -not $overlayImageRow.IsEnabled) {
    throw "Background image selectors did not follow their enable settings " +
        "(desktop=$($desktopImageRow.IsEnabled), fullscreen=$($fullscreenImageRow.IsEnabled), overlay=$($overlayImageRow.IsEnabled); " +
        "presets=$($settings.DesktopNotificationStylePreset)/$($settings.NotificationStylePreset)/$($settings.OverlayStylePreset); " +
        "values=$($settings.DesktopNotificationUseBackgroundImage)/$($settings.NotificationUseBackgroundImage)/$($settings.OverlayUseBackgroundImage); " +
        "editable=$($settings.CanEditDesktopNotificationStyle)/$($settings.CanEditFullscreenNotificationStyle)/$($settings.CanEditOverlayStyle); " +
        "suppress=$($viewType.GetField('suppressingStylePresetMark',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($view)))."
}
$window.Close()

$reopenedView = $constructor.Invoke(@($null))
$reopenedView.DataContext = $settings
$reopenedWindow = New-Object System.Windows.Window
$reopenedWindow.Content = $reopenedView
$reopenedWindow.Show()
$reopenedWindow.UpdateLayout()
$reopenedSelector = $reopenedView.FindName("NotificationSoundPackSelector")
if ($settings.NotificationSoundPack -ne "7_Handheld_Haptic" -or
    $reopenedSelector.SelectedValue -ne "7_Handheld_Haptic") {
    throw "Reopening the settings view did not retain the selected notification sound pack."
}
$reopenedWindow.Close()

Write-Host "Settings view construction and sound pack binding tests passed."
