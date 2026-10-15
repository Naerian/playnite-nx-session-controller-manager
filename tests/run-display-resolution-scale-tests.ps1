$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$output = Join-Path $root "obj\DisplayResolutionScaleSmoke.exe"
New-Item -ItemType Directory -Force -Path (Split-Path $output) | Out-Null
& $compiler /nologo /target:exe /out:$output (Join-Path $root "tests\DisplayResolutionScaleSmoke.cs")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $output $root
exit $LASTEXITCODE
