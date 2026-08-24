$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

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

$settings.NotificationSoundPack = "ArcadePulse"
if ($selector.SelectedValue -ne $settings.NotificationSoundPack) {
    throw "Settings view did not follow an externally changed notification sound pack."
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

Write-Host "Settings view construction and sound pack binding tests passed."
