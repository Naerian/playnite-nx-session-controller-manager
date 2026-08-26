param([switch]$WithImage, [ValidateSet("Aniki", "Helium")][string]$Creator)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assemblyPath = Join-Path $root "bin\Release\ControllerSessionManager.OverlayHost.exe"
$outputName = if ($Creator) { "Toast$($Creator)Preview.png" } else { "ToastPreview.png" }
$outputPath = Join-Path $root ("obj\" + $outputName)

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
$type = $assembly.GetType("ControllerSessionManager.OverlayHost.ToastWindow", $true)
$window = [Activator]::CreateInstance($type, $true)

$svg = [xml](Get-Content -Raw -LiteralPath (Join-Path $root "Gamepads\default.svg"))
$geometry = (@($svg.svg.path | ForEach-Object { $_.d }) -join " ")
$connectionSvg = [xml](Get-Content -Raw -LiteralPath (Join-Path $root "Icons\bluetooth.svg"))
$connectionGeometry = (@($connectionSvg.svg.path | ForEach-Object { $_.d }) -join " ")
$backgroundImage = ""
if ($WithImage) {
    $backgroundImage = Join-Path $root "Images\NotifyBackgrounds\bg1.jpg"
}
$encodedBackgroundImage = if ($backgroundImage) {
    [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($backgroundImage))
} else { "" }
$style = @(
    "620", "125", "BottomRight", "#F20A0620", "#FFFFFFFF", "#FFD2C9FF",
    "#FF00FFC6", "#FF00C8FF", "#FFFFE45E", "28", "16", "58", "28",
    "True", "Full", "3", "26", "Right", "12", "#FFFF2E9D", "True", "30",
    "True", "Orbitron", "Bold", "Left", "IconAndBorder", "None", "True",
    $WithImage.ToString(), $encodedBackgroundImage, "UniformToFill", "Center", "Center", "65", "45",
    "21", "Orbitron", "Bold", "Rajdhani", "SemiBold", "3", "TopLeft",
    "True", "#F221063E", "25", "True",
    "True", "#3800FFC6", "#A000FFC6", "2", "18", "12"
) -join ";"
if ($Creator) {
    [Reflection.Assembly]::LoadFrom("C:\Playnite\Playnite.SDK.dll") | Out-Null
    $pluginAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $root "bin\Release\ControllerSessionManager.dll"))
    $settingsType = $pluginAssembly.GetType("ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerSettings", $true)
    $settings = [Activator]::CreateInstance($settingsType)
    $presetType = $pluginAssembly.GetType("ControllerSessionManager.PlayniteIntegration.NotificationStylePresets", $true)
    $presetType.GetMethod("Apply").Invoke($null, @($settings, $Creator)) | Out-Null
    $pluginType = $pluginAssembly.GetType("ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerPlugin", $true)
    $plugin = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($pluginType)
    $pluginType.GetField("settings", [Reflection.BindingFlags]"Instance,NonPublic").SetValue($plugin, $settings)
    $style = $pluginType.GetMethod("GetToastStylePayload", [Reflection.BindingFlags]"Instance,NonPublic").Invoke($plugin, $null)
}

$type.GetMethod("Enqueue").Invoke($window, @(
    "preview", 0, 15000, "connected", "MANDO CONECTADO",
    "DualSense Wireless Controller", $geometry, $style, $connectionGeometry
)) | Out-Null
$window.BeginAnimation([Windows.UIElement]::OpacityProperty, $null)
$window.Opacity = 1

$content = [Windows.FrameworkElement]$window.Content
$window.Content = $null
$preview = [Windows.Controls.Grid]::new()
$preview.Background = [Windows.Media.SolidColorBrush]::new([Windows.Media.Color]::FromRgb(54, 57, 63))
$preview.Children.Add($content) | Out-Null
$width = [Math]::Ceiling($window.Width + 48)
$height = [Math]::Ceiling($window.Height + 48)
$size = [Windows.Size]::new($width, $height)
$preview.Measure($size)
$preview.Arrange([Windows.Rect]::new($size))
$preview.UpdateLayout()

$bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($width, $height, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($preview)
$encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream = [IO.File]::Open($outputPath, [IO.FileMode]::Create)
try { $encoder.Save($stream) }
finally {
    $stream.Dispose()
    $window.Close()
}

Write-Host "Toast preview rendered: $outputPath"
