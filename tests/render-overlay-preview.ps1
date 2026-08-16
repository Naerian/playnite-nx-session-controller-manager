$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assemblyPath = Join-Path $root "bin\Release\ControllerSessionManager.OverlayHost.exe"
$outputPath = Join-Path $root "obj\OverlayPreview.png"

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
$type = $assembly.GetType("ControllerSessionManager.OverlayHost.OverlayWindow", $true)
$flags = [Reflection.BindingFlags]::Instance -bor [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic
$window = [Activator]::CreateInstance($type, $flags, $null, @([Diagnostics.Process]::GetCurrentProcess().Id), $null)

function Get-Field([string]$name) {
    return $type.GetField($name, $flags).GetValue($window)
}

function Get-SvgGeometry([string]$name) {
    $document = [xml](Get-Content -Raw -LiteralPath (Join-Path $root "Icons\$name"))
    $paths = @($document.svg.path | Where-Object { $_.stroke -ne "none" } | ForEach-Object { $_.d })
    return [Windows.Media.Geometry]::Parse(($paths -join " "))
}

(Get-Field "titleText").Text = "Mando desconectado"
(Get-Field "messageText").Text = "8BitDo Ultimate 2 Wireless"
(Get-Field "instructionText").Text = "Vuelve a conectarlo para continuar."
(Get-Field "pauseStatusText").Text = "Pausa solicitada autom$([char]0x00E1)ticamente"
(Get-Field "controllerIcon").Data = Get-SvgGeometry "device-gamepad.svg"
(Get-Field "pauseStatusIcon").Data = Get-SvgGeometry "player-pause.svg"
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
