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
$view.DataContext = $settings
$window = New-Object System.Windows.Window
$window.Content = $view
$window.Show()
$window.UpdateLayout()
if ($selector.Items.Count -lt 1) {
    throw "Settings view did not populate the notification sound packs."
}
if ($selector.SelectedValue -ne $settings.NotificationSoundPack) {
    throw "Settings view did not select the configured notification sound pack."
}
if ($settings.NotificationSoundPack -ne "5_Minimal_Soft") {
    throw "Opening the settings view replaced the saved notification sound pack."
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
if ($desktopImageRow.IsEnabled -or $fullscreenImageRow.IsEnabled) {
    throw "Background image selectors must be disabled while background images are disabled " +
        "(desktop=$($desktopImageRow.IsEnabled), fullscreen=$($fullscreenImageRow.IsEnabled); " +
        "settings=$($settings.DesktopNotificationUseBackgroundImage)/$($settings.NotificationUseBackgroundImage); " +
        "bindings=$($desktopImageRow.GetBindingExpression([System.Windows.UIElement]::IsEnabledProperty).Status)/" +
        "$($fullscreenImageRow.GetBindingExpression([System.Windows.UIElement]::IsEnabledProperty).Status))."
}
$settings.DesktopNotificationUseBackgroundImage = $true
$settings.NotificationUseBackgroundImage = $true
if (-not $desktopImageRow.IsEnabled -or -not $fullscreenImageRow.IsEnabled) {
    throw "Background image selectors did not follow their enable settings."
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
