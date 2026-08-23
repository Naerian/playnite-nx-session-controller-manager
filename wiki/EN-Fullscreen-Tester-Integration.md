# Fullscreen tester integration

Controller Manager does not take over Playnite's global controller navigation. A Fullscreen theme owns placement, focus, transitions, modal behavior, and the surrounding visual design.

The public tester theme contract is **1.1**. Block names, state properties, and resource keys in this contract are intended to remain backward compatible. Controller Manager owns the contract; uninstall the standalone Gamepad Tester extension.

For battery icons, status text and free composition, see [Theme Integration](EN-Theme-Integration). This page covers **tester blocks only**.

## Source names

| Role | SourceName | Element examples |
| --- | --- | --- |
| Canonical | `ControllerSessionManager` | `TesterLauncher`, `TesterStatusBadge`, `TesterButtonMap`, `TesterStickCheck`, `TesterTriggerCheck`, `TesterRumblePad`, `TesterLatencyMini` |
| Compatibility | `GamepadTester` | `GamepadTesterLauncher`, `StatusBadge`, `ButtonMap`, `StickCheck`, `TriggerCheck`, `RumblePad`, `LatencyMini` |

If a theme only checks the old addon id `GamepadTester_518dc982-…`, also accept:

`ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`.

## Commands

Theme commands still use the compatibility settings root (`SourceName = GamepadTester`, plugin property `TesterTheme`):

```xaml
Command="{PluginSettings Plugin=GamepadTester, Path=OpenTesterCommand}"
Command="{PluginSettings Plugin=GamepadTester, Path=OpenButtonTestCommand}"
Command="{PluginSettings Plugin=GamepadTester, Path=OpenSticksCommand}"
Command="{PluginSettings Plugin=GamepadTester, Path=OpenRumbleCommand}"
Command="{PluginSettings Plugin=GamepadTester, Path=OpenLatencyCommand}"
Command="{PluginSettings Plugin=GamepadTester, Path=RefreshThemeBlocksCommand}"
```

## Embeddable blocks

Name-based hosts (Playnite fills the ContentControl):

```xaml
<!-- Canonical -->
<ContentControl x:Name="ControllerSessionManager_TesterStatusBadge" />
<ContentControl x:Name="ControllerSessionManager_TesterButtonMap" />

<!-- Compatibility -->
<ContentControl x:Name="GamepadTester_StatusBadge" />
<ContentControl x:Name="GamepadTester_ButtonMap" />
<ContentControl x:Name="GamepadTester_StickCheck" />
<ContentControl x:Name="GamepadTester_TriggerCheck" />
<ContentControl x:Name="GamepadTester_RumblePad" />
<ContentControl x:Name="GamepadTester_LatencyMini" />
```

Available logical blocks: launcher, status badge, button map, stick check, trigger check, rumble pad, and latency mini. They share one lightweight polling runtime while visible and intentionally omit desktop-only exports and selectors.

## Dynamic views

Views created after the initial theme load are initialized on `Loaded`. The most reliable form uses the attached `Block` property:

```xaml
<UserControl
    xmlns:gt="clr-namespace:ControllerSessionManager.Tester.Views.ThemeIntegration;assembly=ControllerSessionManager">
    <ContentControl gt:GamepadTesterThemeHost.Block="ButtonMap" />
    <ContentControl gt:GamepadTesterThemeHost.Block="TriggerCheck" />
    <ContentControl gt:GamepadTesterThemeHost.Block="RumblePad" />
</UserControl>
```

The attached property is recommended for custom windows opened dynamically by helper plugins. Name-based initialization also accepts `GamepadTester_ButtonMap`, `GamepadTesterButtonMap`, `TesterButtonMap`, and the equivalent names for every block.

If a helper creates or replaces content after `Loaded`, request a new scan with `RefreshThemeBlocksCommand` (see Commands above).

Every marked host exposes the read-only attached properties `InitializationState`, `InitializationMessage`, `ResolvedBlock`, and `ContractVersion`. `InitializationState` can be `Pending`, `WaitingForPlugin`, `Ready`, `UnknownBlock`, `Occupied`, or `Error`.

```xaml
<TextBlock Text="{Binding ElementName=ButtonMapHost, Path=(gt:GamepadTesterThemeHost.InitializationState)}" />
<TextBlock Text="{Binding ElementName=ButtonMapHost, Path=(gt:GamepadTesterThemeHost.InitializationMessage)}" />
```

`Occupied` means the host already had content and the plugin deliberately left it untouched.

## Focus and modal behavior

Place interactive blocks in a focus scope, move focus into the first button when opening the page, and contain directional navigation. If the page behaves like a modal, disable or hide the underlying game list so Playnite does not navigate behind it.

`ButtonMap`, `StickCheck`, `RumblePad`, and `LatencyMini` contain interactive controls. Button, stick, and latency capture start only after their own action is activated. While a capture is active, themes must suppress their Back/B and close actions; bind those actions to `CanNavigateBack`, which is false while capture owns controller input. The plugin also exposes `IsButtonCaptureRunning`, `IsStickCaptureRunning`, `IsLatencyTestRunning`, and the aggregate `IsFullscreenInputCaptureActive` on each block's data context. When capture stops, the extension releases the navigation guard and attempts to restore focus to an optional element named `GamepadTester_BackButton`.

The plugin automatically disables a visible control named `GamepadTester_BackButton` during capture. Keep the `CanNavigateBack` binding as well because helper plugins can implement B/close by removing content without invoking WPF window closing.

`StatusBadge` and `TriggerCheck` are display-only. The controller drawing is supplied by the plugin; the theme controls its container, sizing, placement, visibility, and surrounding UI.

Each embedded block also exposes `IsControllerConnected`, `IsInputCaptureActive`, `CanNavigateBack`, `ActiveTestKind`, and `ThemeContractVersion`. `ActiveTestKind` uses stable values: `None`, `Buttons`, `Sticks`, `Latency`, and `Rumble`. The shared data context exposes the corresponding commands and detailed live values, so themes should bind to state rather than inspect child visuals. A helper that removes custom content instead of closing a WPF window must check `CanNavigateBack` itself.

```xaml
<Button x:Name="GamepadTester_BackButton"
        IsEnabled="{Binding ElementName=GamepadTester_ButtonMap, Path=Content.CanNavigateBack}"
        Command="{Binding CloseCommand}" />
```

Holding `LB + RB` for one second finishes button, stick, or latency capture before Back navigation becomes available again.

## Theme resources

Embedded blocks use dedicated dynamic resources, so themes can customize them locally without changing Playnite's global brushes:

```xaml
<UserControl.Resources>
    <SolidColorBrush x:Key="GamepadTesterControlBackgroundBrush" Color="#181C24" />
    <SolidColorBrush x:Key="GamepadTesterButtonBackgroundBrush" Color="#242A35" />
    <SolidColorBrush x:Key="GamepadTesterControlBorderBrush" Color="#566174" />
    <SolidColorBrush x:Key="GamepadTesterStickGuideBrush" Color="#75839A" />
    <SolidColorBrush x:Key="GamepadTesterTextBrush" Color="#F4F6FA" />
</UserControl.Resources>
```

`GamepadTesterStickGuideBrush` controls the outer circles, range rings, and horizontal/vertical guides in `StickCheck` without changing panel or button borders. When a dedicated resource is not defined, the plugin falls back to Playnite's matching generic brush; the stick guide falls back to `ControlBorderBrush`. Resources declared on the custom window or view take precedence over the application-level fallback.

A copy-ready reference view and the concise contract are in the repository under `docs/theme-integration`.

Next: [Troubleshooting & FAQ](EN-Troubleshooting-and-FAQ)
