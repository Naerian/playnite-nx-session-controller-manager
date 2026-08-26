$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$output = Join-Path $root "obj\ImportedVisualProfileCatalogSmoke.exe"

& $compiler /nologo /target:exe /out:$output /reference:System.Web.Extensions.dll `
    (Join-Path $root "tests\ImportedVisualProfileCatalogSmoke.cs")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $output $root
exit $LASTEXITCODE
