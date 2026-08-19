# Codex Handoff: Controller Manager

This file is a continuity note for future Codex sessions after a context reset.
It captures the current project state, release workflow, local paths and the
important decisions that should be preserved.

## Project

- Extension name: Controller Manager
- Repository: https://github.com/Naerian/playnite-nx-session-controller-manager
- Add-on id: `ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`
- Author shown in Playnite: `Narian`
- Extension type: `GenericPlugin`
- Main local checkout: `C:\Proyectos\playnite-nx-session-controller-manager`
- Playnite install path used during development: `C:\Playnite`
- Installed extension folder:
  `C:\Playnite\Extensions\ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`

## Current Release State

- Latest released version: `1.0.7`
- Latest release tag: `v1.0.7`
- Current package name:
  `ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc_1_0_7.pext`
- Package output folder:
  `C:\Proyectos\playnite-nx-session-controller-manager\dist\v1.0.7`
- Public package SHA-256 verified for v1.0.7:
  `67B0537EE6032D196B08872A8D4350D35718437EE3C73295A82330F7969E053C`

When continuing work, first verify the current repository state instead of
assuming this file is still current.

## What The Extension Does

Controller Manager is a Playnite extension that detects connected game
controllers, tracks which controllers actually participate in a game session,
and reacts when a participating controller disconnects.

Main capabilities currently implemented:

- Controller inventory and identity resolution across Playnite SDK, XInput,
  SDL and Windows PnP evidence.
- Optional custom controller names and bundled SVG icons.
- USB, Bluetooth and wireless-receiver connection differentiation when enough
  evidence exists.
- Coarse semantic battery states with English fallback and no invented
  percentages.
- Vibration testing from the controller table.
- Adaptive Desktop quick-access indicator that respects compact theme buttons.
- Configurable Fullscreen notifications for connection, disconnection and
  warning states.
- External disconnect overlay for protected sessions.
- Safe automatic handover, local multiplayer detection and per-game policy
  overrides.
- Optional pause actions, conservative online-game handling and safe fallback
  notification paths.
- Privacy-conscious support reports and HID diagnostics.
- Localization dictionaries in 12 languages with English fallback.

## Important Product Decisions

- Use the Playnite SDK as the authority for connection state whenever possible.
- Treat XInput, SDL and Windows PnP as enrichment sources rather than competing
  sources of truth.
- Never merge devices only because a numeric slot or VID/PID looks similar.
- Keep battery states coarse unless a provider truly exposes an exact value.
- Do not invent battery percentages from discrete or unknown states.
- Keep Fullscreen SDL usage conservative because native hot-unplug behavior can
  terminate Playnite if the wrong path is used.
- Keep the overlay external and isolated from Playnite's UI thread.
- Prefer explicit per-game overrides only when automatic detection is not enough.
- Preserve the distinction between single-player handover and local multiplayer
  protection.
- Treat online-game detection as best effort and do not force suspension when
  evidence suggests it is unsafe.

## Local Files To Treat Carefully

- `media/icon.png` is intentionally tracked and used by `extension.yaml`,
  `installer.yaml` and the add-on database metadata.
- `extension.yaml` and `installer.yaml` must stay aligned with the current
  version and add-on id.
- `playnite-addon.yaml` mirrors the metadata that should be submitted to the
  official Playnite add-on database.
- `dist/` contains generated package outputs and should not be edited manually.
- Never commit Playnite settings exports, logs, secrets or machine-specific
  data.

## Build

This is a classic Playnite C# plugin project. Use the existing scripts and
tooling rather than inventing a new build path.

Preferred verification flow:

```powershell
.\verify.ps1
```

If you need a direct build, use the repository's existing MSBuild-based flow
instead of `dotnet build`.

## Package

Use the existing package script and Playnite Toolbox flow when preparing a
release package.

Typical release artifacts:

- `extension.yaml`
- `installer.yaml`
- `.pext` generated under `dist\`

Verify the installer manifest before release.

## Install Locally

Before installing into `C:\Playnite`, always check both Playnite processes:

```powershell
Get-Process Playnite.DesktopApp,Playnite.FullscreenApp -ErrorAction SilentlyContinue
```

If either process is running, do not overwrite the installed DLL.

When Playnite is closed, copy the release output and required folders into:

```text
C:\Playnite\Extensions\ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc
```

Keep the install scoped to this extension folder. Do not delete broad paths.

## Version And Release Checklist

When the user asks to publish a new version, perform the complete workflow
unless they explicitly say otherwise:

1. Check `git status --short`.
2. Decide the next version number.
3. Update all version sources:
   - `extension.yaml`
   - `installer.yaml`
   - assembly metadata if it is used for display
   - README/version text if necessary
4. Run `verify.ps1`.
5. Build/package the `.pext`.
6. Verify `installer.yaml`.
7. Commit only intended files.
8. Push `main`.
9. Create and push the tag.
10. Create the GitHub release and upload the `.pext`.
11. Compare the public release asset hash with the local package.
12. Verify the raw or API-visible `installer.yaml` references the new package.

Write all public release text in English. This includes the GitHub release body,
release notes and manifest changelog entries.

## Git Commands Usually Used

```powershell
git status --short
git add <intended-files-only>
git commit -m "Add Codex handoff documentation"
git push origin main
git tag vX.Y.Z
git push origin vX.Y.Z
```

For documentation-only commits, keep the message specific and descriptive.

## Useful Validation Checks

Check the localized dictionaries remain aligned:

```powershell
$files = Get-ChildItem C:\Proyectos\playnite-nx-session-controller-manager\Localization\*.xaml
$keySets = @{}
foreach ($file in $files) {
    $keys = Select-String -Path $file.FullName -Pattern 'x:Key="([^"]+)"' |
        ForEach-Object { $_.Matches[0].Groups[1].Value } |
        Sort-Object -Unique
    $keySets[$file.Name] = $keys
}
$baseline = $keySets['en_US.xaml']
foreach ($name in $keySets.Keys) {
    $missing = $baseline | Where-Object { $keySets[$name] -notcontains $_ }
    $extra = $keySets[$name] | Where-Object { $baseline -notcontains $_ }
    if ($missing -or $extra) {
        "$name missing=$($missing.Count) extra=$($extra.Count)"
    }
}
```

Check package hashes:

```powershell
Get-FileHash C:\Proyectos\playnite-nx-session-controller-manager\dist\v1.0.7\*.pext -Algorithm SHA256
```

## Current Follow-Up Ideas

These are not mandatory, but they are useful next places to improve the
extension:

- Continue refining the support report and incident timeline so issues are easy
  to reproduce.
- Keep expanding only the battery providers that are proven safe and
  documented.
- Keep the overlay and notifications fully customizable while preserving theme
  readability.
- Continue hardening online/offline session handling and controller handover.
- Keep the Playnite add-on database metadata synchronized with the release
  process.
- Expand wiki pages and screenshots whenever the UI changes.

## After Reinstalling The PC

Recommended recovery flow:

1. Install Playnite and Codex CLI.
2. Clone the repo:

   ```powershell
   git clone https://github.com/Naerian/playnite-nx-session-controller-manager.git C:\Proyectos\playnite-nx-session-controller-manager
   ```

3. Restore or reinstall Playnite at `C:\Playnite` if that path is still desired.
4. Re-open this file before continuing work.

Suggested prompt:

```text
Continue Controller Manager. Read docs/CODEX_HANDOFF.md first.
```
