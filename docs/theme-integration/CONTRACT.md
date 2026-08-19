# Gamepad Tester Fullscreen contract 1.1

This contract is the stable surface intended for Playnite Fullscreen theme developers.

Controller Manager now owns this contract. Canonical block names live under `SourceName = ControllerSessionManager` as `TesterLauncher`, `TesterStatusBadge`, `TesterButtonMap`, `TesterStickCheck`, `TesterTriggerCheck`, `TesterRumblePad` and `TesterLatencyMini`. The `GamepadTester` source name and original block names remain compatibility aliases. Uninstall the standalone Gamepad Tester extension; two plugins cannot register the same `SourceName`.

SDL GameController sampling runs in `ControllerSessionManager.TesterHost.exe`, not in the Playnite process.

## Blocks

`GamepadTesterLauncher`, `StatusBadge`, `ButtonMap`, `StickCheck`, `TriggerCheck`, `RumblePad`, and `LatencyMini`.

Use the attached host property for static or dynamically created views:

```xaml
<ContentControl gt:GamepadTesterThemeHost.Block="ButtonMap" />
```

The plugin initializes marked hosts on `Loaded`. A theme helper can force another scan with the compatibility settings root (this command lived on `GamepadTester` in 1.1 and still does):

```xaml
Command="{PluginSettings Plugin=GamepadTester, Path=RefreshThemeBlocksCommand}"
```

## Host diagnostics

Every marked `ContentControl` exposes these read-only attached properties:

- `InitializationState`: `Pending`, `WaitingForPlugin`, `Ready`, `UnknownBlock`, `Occupied`, or `Error`.
- `InitializationMessage`: a developer-facing explanation.
- `ResolvedBlock`: the normalized block name.
- `ContractVersion`: currently `1.0`.

Bind them with `(gt:GamepadTesterThemeHost.InitializationState)` and the equivalent property paths. `Occupied` means the host already had content and the plugin intentionally did not replace it.

## Runtime state

Each embedded block exposes:

- `IsControllerConnected`
- `IsInputCaptureActive`
- `CanNavigateBack`: `false` while button, stick, or latency capture owns controller input.
- `ActiveTestKind`: `None`, `Buttons`, `Sticks`, `Latency`, or `Rumble`
- `ThemeContractVersion`

The shared data context also exposes `HasController`, `IsAnyTestRunning`, the individual `Is...Running` properties, commands, and live diagnostic values. Theme code should bind to these states instead of inspecting child visuals.

## Resources

- `GamepadTesterControlBackgroundBrush`
- `GamepadTesterButtonBackgroundBrush`
- `GamepadTesterControlBorderBrush`
- `GamepadTesterStickGuideBrush`
- `GamepadTesterTextBrush`

Declare overrides at window or view scope. The plugin falls back to the corresponding Playnite theme resources.

## Navigation responsibility

The theme owns focus, transitions, close behavior, and background navigation. Keep test blocks in a contained focus scope and bind every Back button, B gesture, and close command to `CanNavigateBack`. The plugin blocks WPF window closing while capture is active, but a helper that swaps or removes custom content must honor this property itself.

```xaml
<Button x:Name="GamepadTester_BackButton"
        IsEnabled="{Binding ElementName=ButtonMapHost, Path=Content.CanNavigateBack}"
        Command="{Binding CloseCommand}" />
```

Name the return control `GamepadTester_BackButton` so the plugin can disable it during capture and restore focus there afterwards. Holding `LB + RB` for one second finishes button, stick, or latency capture before Back navigation becomes available again.
