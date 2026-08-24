$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assemblyPath = Join-Path $root "bin\Release\ControllerSessionManager.OverlayHost.exe"
$outputPath = Join-Path $root "obj\OverlayPreview.png"

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
(Get-Field "controllerIcon").Data = Get-SvgGeometry "Gamepads\default.svg"
(Get-Field "pauseStatusIcon").Data = Get-SvgGeometry "Icons\player-pause.svg"
(Get-Field "connectionIcon").Data = Get-SvgGeometry "Icons\bluetooth.svg"
(Get-Field "batteryIcon").Data = Get-SvgGeometry "Icons\battery.svg"
$type.GetField("currentBatteryState", $flags).SetValue($window, "Medium")
$style = @(
    "100", "#96000000", "#EB121418", "#238FFF", "#FFFFFFFF", "#F5B542",
    "30", "22", "19", "15", "64", "18", "34", "True", "3", "13", "True", "True",
    "14", "Left", "True", "Default", "SemiBold", "True", "True", "True", "True", "True",
    "Center", "FadeScale", "Full", "620", "True",
    "Montserrat", "SemiBold", "Outfit", "SemiBold", "Inter", "Regular", "Rajdhani", "SemiBold",
    "#FFFFFFFF", "#FFFFFFFF", "#302391FF", "#602391FF", "1", "7", "14", "13",
    "#FFF5B542", "#FFF5B542", "#30F5B542", "#60F5B542", "1", "7", "14", "13",
    "True", "#FF4FC27E", "#FFF5B542", "#FFE05252", "#FFC92D45"
) -join ";"
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
