param([switch]$WithImage, [switch]$Split, [string]$Creator)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assemblyPath = Join-Path $root "bin\Release\ControllerSessionManager.OverlayHost.exe"
$outputName = if ($Creator) { "Overlay$($Creator)Preview.png" } elseif ($WithImage -or $Split) { "OverlayImagePreview.png" } else { "OverlayPreview.png" }
$outputPath = Join-Path $root ("obj\" + $outputName)

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
$type = $assembly.GetType("ControllerSessionManager.OverlayHost.OverlayWindow", $true)
$flags = [Reflection.BindingFlags]::Instance -bor [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic
$constructor = $type.GetConstructors($flags) |
    Where-Object { $_.GetParameters().Count -eq 2 } |
    Select-Object -First 1
$window = $constructor.Invoke(@([Diagnostics.Process]::GetCurrentProcess().Id, $null))

function Get-Field([string]$name) {
    return $type.GetField($name, $flags).GetValue($window)
}

function Get-SvgGeometry([string]$name) {
    $document = [xml](Get-Content -Raw -LiteralPath (Join-Path $root $name))
    $paths = @($document.svg.path | Where-Object { $_.stroke -ne "none" } | ForEach-Object { $_.d })
    return [Windows.Media.Geometry]::Parse(($paths -join " "))
}

(Get-Field "titleText").Text = "Mando desconectado"
(Get-Field "messageText").Text = "8BitDo Ultimate 2 Wireless"
(Get-Field "instructionText").Text = "Vuelve a conectarlo para continuar."
(Get-Field "pauseStatusText").Text = "Pausa solicitada autom$([char]0x00E1)ticamente"
(Get-Field "connectionText").Text = "Bluetooth"
(Get-Field "batteryText").Text = "Bater$([char]0x00ED)a baja"
(Get-Field "incidentStateText").Text = "DESCONECTADO"
(Get-Field "controllerIcon").Data = Get-SvgGeometry "Gamepads\default.svg"
(Get-Field "pauseStatusIcon").Data = Get-SvgGeometry "Icons\player-pause.svg"
(Get-Field "connectionIcon").Data = Get-SvgGeometry "Icons\bluetooth.svg"
(Get-Field "batteryIcon").Data = Get-SvgGeometry "Icons\battery.svg"
$type.GetField("currentBatteryState", $flags).SetValue($window, "Medium")
$layoutMode = if ($Split) { "Split" } else { "Hero" }
$backgroundImage = if ($WithImage) { Join-Path $root "Images\NotifyBackgrounds\bg1.jpg" } else { "" }
$encodedBackgroundImage = if ($backgroundImage) {
    [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($backgroundImage))
} else { "" }
$style = @(
    "120", "#D0000010", "#F20A0620", "#FF00FFC6", "#FFFFFFFF", "#FFFFE45E",
    "42", "26", "21", "16", "54", "20", "52", "True", "5", "24", "True", "True",
    "18", "Right", "True", "Orbitron", "Bold", "True", "True", "True", "True", "True",
    "BottomRight", "FadeScale", "Full", "820", "True",
    "Orbitron", "Bold", "Rajdhani", "SemiBold", "Outfit", "SemiBold", "Orbitron", "Bold",
    "#FFFFFFFF", "#FFFFFFFF", "#3000FFC6", "#7000FFC6", "1", "12", "16", "14",
    "#FFFFE45E", "#FFFFE45E", "#30FFE45E", "#70FFE45E", "1", "12", "16", "14",
    "True", "#FF00FFC6", "#FFFFE45E", "#FFFF2E9D", "#FFFF1744", "Right", "38",
    "True", "#F22A064A", "30", "True",
    $layoutMode, $WithImage.ToString(), $encodedBackgroundImage,
    "UniformToFill", "Center", "Center", "70", "45",
    "True", "#3800FFC6", "#A000FFC6", "2", "18", "16"
) -join ";"
if ($Creator) {
    [Reflection.Assembly]::LoadFrom("C:\Playnite\Playnite.SDK.dll") | Out-Null
    $pluginAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $root "bin\Release\ControllerSessionManager.dll"))
    $catalogType = $pluginAssembly.GetType("ControllerSessionManager.PlayniteIntegration.CreatorThemeCatalog", $true)
    $catalogType.GetMethod("Configure", [type[]]@([string])).Invoke($null, @([string]$root)) | Out-Null
    $settingsType = $pluginAssembly.GetType("ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerSettings", $true)
    $settings = [Activator]::CreateInstance($settingsType)
    $presetType = $pluginAssembly.GetType("ControllerSessionManager.PlayniteIntegration.OverlayStylePresets", $true)
    $presetType.GetMethod("Apply").Invoke($null, @($settings, $Creator)) | Out-Null
    $pluginType = $pluginAssembly.GetType("ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerPlugin", $true)
    $plugin = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($pluginType)
    $pluginType.GetField("settings", [Reflection.BindingFlags]"Instance,NonPublic").SetValue($plugin, $settings)
    $style = $pluginType.GetMethod("GetOverlayStylePayload", [Reflection.BindingFlags]"Instance,NonPublic").Invoke($plugin, $null)
}
$type.GetMethod("ApplyPresentationStyle", $flags).Invoke($window, @($style)) | Out-Null
$type.GetMethod("ApplyPauseStatusStyle", $flags).Invoke($window, @("pause")) | Out-Null

$content = [Windows.FrameworkElement]$window.Content
$content.Background = [Windows.Media.SolidColorBrush]::new([Windows.Media.Color]::FromRgb(8, 10, 14))
$size = [Windows.Size]::new(1200, 760)
$content.Measure($size)
$content.Arrange([Windows.Rect]::new($size))
$content.UpdateLayout()

$bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(1200, 760, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($content)
$encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream = [IO.File]::Open($outputPath, [IO.FileMode]::Create)
try {
    $encoder.Save($stream)
}
finally {
    $stream.Dispose()
    $window.Close()
}

Write-Host "Overlay preview rendered: $outputPath"
