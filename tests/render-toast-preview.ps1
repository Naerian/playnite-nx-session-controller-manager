$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assemblyPath = Join-Path $root "bin\Release\ControllerSessionManager.OverlayHost.exe"
$outputPath = Join-Path $root "obj\ToastPreview.png"

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
$type = $assembly.GetType("ControllerSessionManager.OverlayHost.ToastWindow", $true)
$window = [Activator]::CreateInstance($type, $true)

$svg = [xml](Get-Content -Raw -LiteralPath (Join-Path $root "Gamepads\default.svg"))
$geometry = (@($svg.svg.path | ForEach-Object { $_.d }) -join " ")
$style = @(
    "520", "100", "TopRight", "#F4121418", "#FFFFFFFF", "#FFC6CBD4",
    "#FF4FC27E", "#FF50AAFF", "#FFF5B542", "19", "15", "38", "18",
    "True", "Full", "2", "24", "Left", "8", "#FFE05252", "False", "28",
    "True", "Inter", "SemiBold", "Left", "IconAndBorder", "None", "True"
) -join ";"

$type.GetMethod("Enqueue").Invoke($window, @(
    "preview", 0, 15000, "connected", "Mando conectado",
    "DualSense Wireless Controller", $geometry, $style, $null
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
