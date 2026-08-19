$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$output = Join-Path $root "obj\TesterTests"
New-Item -ItemType Directory -Path $output -Force | Out-Null

$compiler = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
}

$plugin = Join-Path $root "bin\Release\ControllerSessionManager.dll"
if (-not (Test-Path -LiteralPath $plugin)) {
    $plugin = Join-Path $root "bin\Debug\ControllerSessionManager.dll"
}
if (-not (Test-Path -LiteralPath $plugin)) {
    throw "ControllerSessionManager.dll was not found. Build the solution first."
}

$playniteSdk = "C:\Playnite\Playnite.SDK.dll"
$fw = Join-Path $root "packages\Microsoft.NETFramework.ReferenceAssemblies.net462.1.0.3\build\.NETFramework\v4.6.2"
if (-not (Test-Path -LiteralPath (Join-Path $fw "PresentationFramework.dll"))) {
    throw "WPF reference assemblies were not found at $fw. Run build.ps1 first."
}
$testExecutable = Join-Path $output "ControllerSessionManager.TesterTests.exe"
& $compiler /nologo /warn:4 /optimize+ /target:exe /out:$testExecutable `
    /reference:$plugin `
    /reference:$playniteSdk `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Xml.dll `
    /reference:System.Xml.Linq.dll `
    /reference:System.Xaml.dll `
    /reference:"$fw\WindowsBase.dll" `
    /reference:"$fw\PresentationCore.dll" `
    /reference:"$fw\PresentationFramework.dll" `
    (Join-Path $root "tests\Tester\Program.cs") `
    (Join-Path $root "tests\Tester\SimulatedGamepadInputProvider.cs")
if ($LASTEXITCODE -ne 0) {
    throw "Tester test compilation failed with exit code $LASTEXITCODE"
}

Copy-Item -LiteralPath $plugin -Destination (Join-Path $output "ControllerSessionManager.dll") -Force
Copy-Item -LiteralPath $playniteSdk -Destination (Join-Path $output "Playnite.SDK.dll") -Force

Push-Location $output
try {
    & $testExecutable
    if ($LASTEXITCODE -ne 0) {
        throw "Tester tests failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}
