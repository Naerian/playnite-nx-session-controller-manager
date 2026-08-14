param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$packageVersion = "1.0.3"
$packageRoot = Join-Path $root "packages\Microsoft.NETFramework.ReferenceAssemblies.net462.$packageVersion"
$frameworkPath = Join-Path $packageRoot "build\.NETFramework\v4.6.2"

if (-not (Test-Path -LiteralPath $msbuild)) {
    throw "MSBuild was not found at $msbuild"
}

if (-not (Test-Path -LiteralPath (Join-Path $frameworkPath "mscorlib.dll"))) {
    $packagesDirectory = Join-Path $root "packages"
    $archive = Join-Path $packagesDirectory "net462-reference-assemblies.zip"
    $extract = Join-Path $packagesDirectory "net462-reference-assemblies"
    New-Item -ItemType Directory -Path $packagesDirectory -Force | Out-Null
    Invoke-WebRequest -UseBasicParsing `
        -Uri "https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net462/$packageVersion/microsoft.netframework.referenceassemblies.net462.$packageVersion.nupkg" `
        -OutFile $archive
    Expand-Archive -LiteralPath $archive -DestinationPath $extract -Force
    $sourceBuild = Join-Path $extract "build"
    New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
    Copy-Item -LiteralPath $sourceBuild -Destination $packageRoot -Recurse -Force
}

& $msbuild (Join-Path $root "ControllerSessionManager.sln") `
    /p:Configuration=$Configuration `
    /p:FrameworkPathOverride=$frameworkPath `
    /v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Build completed: $(Join-Path $root "bin\$Configuration")"
