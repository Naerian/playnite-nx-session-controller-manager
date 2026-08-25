# Guía de publicación de Controller Manager

Esta guía describe el proceso completo para subir una nueva versión, generar el
paquete `.pext`, publicar los cambios en GitHub y verificar la release.

Todos los textos públicos de la release, incluido el changelog de
`installer.yaml`, deben escribirse en inglés.

## Método recomendado: script automatizado

La forma más rápida y segura es usar `release.ps1`. El script actualiza las
versiones, genera `CHANGELOG.md` e `installer.yaml`, ejecuta todas las pruebas,
crea el `.pext` y verifica su contenido y hash.

Primero crea `.release-notes.md` con una línea por cambio, siempre en inglés:

```markdown
- Added the main new feature.
- Fixed the relevant bug.
- Improved controller compatibility.
```

El fichero está ignorado por Git y se reutiliza para el changelog, el instalador
y las notas de GitHub.

Preparar y validar sin publicar nada:

```powershell
.\release.ps1 -Version 1.0.24
```

La primera ejecución crea una plantilla de `.release-notes.md` si todavía no
existe. Tras editarla, vuelve a ejecutar el comando.

Cuando hayas revisado el diff, publicar la release completa:

```powershell
.\release.ps1 -Version 1.0.24 -Publish
```

Antes del commit y la publicación exige escribir `RELEASE 1.0.24`. Para una
ejecución no interactiva deliberada puede usarse `-Yes`:

```powershell
.\release.ps1 -Version 1.0.24 -Publish -Yes
```

Las secciones siguientes documentan el procedimiento manual equivalente y
sirven también para diagnosticar cualquier fallo del script.

## 1. Abrir el repositorio

```powershell
cd C:\Proyectos\playnite-nx-session-controller-manager
```

## 2. Comprobar los requisitos

```powershell
gh auth status
Test-Path C:\Playnite\Toolbox.exe
gh release list --limit 5
git status --short
```

`Test-Path` debe devolver `True`. Antes de continuar, revisa que el estado de Git
solo contenga los cambios que quieres publicar.

Si GitHub CLI todavía no está autenticado:

```powershell
gh auth login
```

## 3. Elegir y configurar la versión

Ejemplo para la versión siguiente:

```powershell
$Version = "1.0.24"
$Tag = "v$Version"
$VersionForFile = $Version -replace '\.', '_'
```

Actualiza estos ficheros:

1. `extension.yaml`

   ```yaml
   Version: 1.0.24
   ```

2. `Properties\AssemblyInfo.cs`

   ```csharp
   [assembly: AssemblyVersion("1.0.24.0")]
   [assembly: AssemblyFileVersion("1.0.24.0")]
   ```

3. Añade una sección nueva al principio de `CHANGELOG.md`:

   ```markdown
   ## 1.0.24 — AAAA-MM-DD
   - Describe the main new feature.
   - Describe important fixes.
   ```

4. Añade el nuevo paquete al principio de `installer.yaml`:

   ```yaml
   - Version: 1.0.24
     RequiredApiVersion: 6.16.0
     ReleaseDate: AAAA-MM-DD
     PackageUrl: https://github.com/Naerian/playnite-nx-session-controller-manager/releases/download/v1.0.24/ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc_1_0_24.pext
     Changelog:
       - Describe the main new feature.
       - Describe important fixes.
   ```

No elimines las versiones anteriores de `installer.yaml`.

## 4. Ejecutar las pruebas

```powershell
.\tests\run-session-tests.ps1
.\tests\run-tester-tests.ps1
.\tests\run-settings-migration-tests.ps1
.\tests\run-settings-view-tests.ps1
```

Todas las pruebas deben finalizar correctamente antes de empaquetar.

## 5. Generar el paquete `.pext`

```powershell
.\package.ps1
```

El script obtiene la versión de `extension.yaml`, compila la solución y genera
el paquete dentro de `dist\<versión>\`.

Guardar la ruta y el hash del paquete:

```powershell
$Package = Get-ChildItem "dist\$Version\*.pext" | Select-Object -First 1
$LocalHash = (Get-FileHash -LiteralPath $Package.FullName -Algorithm SHA256).Hash

$Package.FullName
$LocalHash
```

Comprobar que el paquete contiene los componentes esenciales:

```powershell
tar -tf $Package.FullName |
    Select-String "extension.yaml|ControllerSessionManager.dll|gamecontrollerdb.txt|SDL_GameControllerDB.LICENSE"
```

## 6. Revisar los cambios

```powershell
git diff --check
git diff --stat
git status --short
```

Opcionalmente, revisa el diff completo:

```powershell
git diff
```

No continúes si aparecen archivos privados, diagnósticos, configuraciones locales,
logs, secretos o cambios que no pertenecen a la release.

## 7. Crear el commit y subirlo

```powershell
git add -A
git commit -m "Release Controller Manager $Version"
git push origin main
```

Comprueba que el commit local y el remoto coinciden:

```powershell
git log -1 --oneline --decorate
git status --short
```

## 8. Preparar las notas de GitHub

Las notas deben estar en inglés y mencionar los cambios principales y las pruebas
realizadas.

```powershell
$NotesFile = Join-Path $env:TEMP "controller-manager-$Version-release-notes.md"

@"
## What's new

- Describe the main new feature.
- Describe important fixes.
- Mention relevant compatibility improvements.

## Verification

- Session/controller tests passed.
- Tester checks passed.
- Settings and migration checks passed.

SHA-256: ``$LocalHash``
"@ | Set-Content -LiteralPath $NotesFile -Encoding UTF8
```

Puedes revisar las notas antes de publicar:

```powershell
Get-Content -LiteralPath $NotesFile
```

## 9. Crear la release

```powershell
gh release create $Tag $Package.FullName `
    --repo Naerian/playnite-nx-session-controller-manager `
    --target main `
    --title "Controller Manager $Version" `
    --notes-file $NotesFile
```

El comando crea el tag remoto, publica la release y sube el `.pext`.

Cuando termine:

```powershell
Remove-Item -LiteralPath $NotesFile
```

## 10. Verificar la release pública

Consultar el estado y los assets publicados:

```powershell
$Published = gh release view $Tag `
    --repo Naerian/playnite-nx-session-controller-manager `
    --json url,isDraft,isPrerelease,tagName,targetCommitish,assets |
    ConvertFrom-Json

$Published.url
$Published.isDraft
$Published.isPrerelease
$Published.assets
```

La release debe tener:

- `isDraft` igual a `False`.
- `isPrerelease` igual a `False`, salvo que se trate de una beta deliberada.
- Un único `.pext` con el nombre y la versión esperados.

Comparar el hash local con el asset publicado:

```powershell
$RemoteHash = (
    $Published.assets |
    Where-Object { $_.name -eq $Package.Name } |
    Select-Object -ExpandProperty digest
) -replace '^sha256:', ''

if ($LocalHash -ne $RemoteHash.ToUpperInvariant()) {
    throw "The public asset hash does not match the local package."
}

"Public asset hash verified: $LocalHash"
```

## 11. Verificar el instalador público

Usa un parámetro en la URL para evitar una respuesta antigua de la caché:

```powershell
$InstallerUrl = "https://raw.githubusercontent.com/Naerian/playnite-nx-session-controller-manager/main/installer.yaml?release=$Version"
$PublicInstaller = (Invoke-WebRequest -UseBasicParsing -Uri $InstallerUrl).Content

$PublicInstaller |
    Select-String -Pattern "Version: $Version|PackageUrl:"
```

La primera entrada debe ser la nueva versión y su `PackageUrl` debe coincidir
exactamente con el asset publicado.

## 12. Comprobación final

```powershell
git fetch --tags
git status --short
git log -1 --oneline --decorate
git tag --points-at HEAD
```

La publicación está cerrada correctamente cuando:

- El tag aparece sobre el commit esperado.
- `main` coincide con `origin/main`.
- `git status --short` no devuelve cambios.
- El `.pext` público tiene el mismo SHA-256 que el paquete local.
- El `installer.yaml` público anuncia la versión nueva.

## Resumen rápido

Después de actualizar los cuatro ficheros de versión y changelog, el flujo mínimo
es:

```powershell
$Version = "1.0.24"
$Tag = "v$Version"

.\tests\run-session-tests.ps1
.\tests\run-tester-tests.ps1
.\tests\run-settings-migration-tests.ps1
.\tests\run-settings-view-tests.ps1
.\package.ps1

$Package = Get-ChildItem "dist\$Version\*.pext" | Select-Object -First 1
$LocalHash = (Get-FileHash -LiteralPath $Package.FullName -Algorithm SHA256).Hash

git diff --check
git add -A
git commit -m "Release Controller Manager $Version"
git push origin main

# Crear las notas en $NotesFile antes de continuar.
gh release create $Tag $Package.FullName `
    --repo Naerian/playnite-nx-session-controller-manager `
    --target main `
    --title "Controller Manager $Version" `
    --notes-file $NotesFile
```
