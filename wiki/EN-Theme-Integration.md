# Theme integration

Theme authors can also ship reviewed notification, overlay, font and sound adaptations for Controller Manager. See the complete [Creator designs guide](EN-Creator-Designs).

Controller Manager exposes two official layers:

1. **Data API** (`PluginSettings` + `PluginConverter`) — full composition freedom.
2. **ContentControl elements** — drop-in, resizable shortcuts.

The automatic Desktop top-panel button is independent and does not require theme changes.

Addon Id: `ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`  
SourceName: `ControllerSessionManager`  
SettingsRoot: `Theme` (paths are **without** a `Theme.` prefix)

## 1. Data API (free composition)

```xml
<!-- Profile icon + battery dot (icon color follows the plugin setting) -->
<StackPanel Orientation="Horizontal"
            Visibility="{PluginStatus Plugin=ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc, Status=Installed}">
    <Path Width="28" Height="28" Stretch="Uniform" StrokeThickness="0.45" StrokeLineJoin="Round"
          Data="{PluginSettings Plugin=ControllerSessionManager, Path=PrimaryControllerIconGeometry, Converter={PluginConverter Plugin=ControllerSessionManager, Converter=IconGeometryConverter}}"
          Fill="{DynamicResource TextBrush}"
          Stroke="{DynamicResource TextBrush}"
          ToolTip="{PluginSettings Plugin=ControllerSessionManager, Path=PrimaryControllerTooltip}"/>
    <Ellipse Width="10" Height="10" Margin="6,0,0,0"
             Fill="{PluginSettings Plugin=ControllerSessionManager, Path=PrimaryControllerBatteryBrush}">
        <Ellipse.Style>
            <Style TargetType="Ellipse">
                <Setter Property="Visibility" Value="Collapsed"/>
                <Style.Triggers>
                    <DataTrigger Binding="{PluginSettings Plugin=ControllerSessionManager, Path=HasPrimaryControllerBattery}" Value="True">
                        <Setter Property="Visibility" Value="Visible"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Ellipse.Style>
    </Ellipse>
</StackPanel>
```

Common recipes:

| Goal | How |
|------|-----|
| Icon only, always battery-colored | `Fill`/`Stroke` → `PrimaryControllerBatteryBrush` |
| Icon only, theme foreground | `Fill` → `{DynamicResource TextBrush}` |
| Fixed pack icon (not the pad silhouette) | `Path=DefaultIconGeometry` (+ converter) |
| Same icon as Desktop top panel | `Path=TopPanelIconGeometry` |
| Level text only | `Text` → `PrimaryControllerBatteryLabel` |
| Theme-owned glyph + CSM color | Your FontIcon/Path + `PrimaryControllerBatteryBrush` on a dot |
| Honor Hidden/Default/Primary | Read `TopPanelControllerMode` / `IsTopPanelButtonVisible` / `ColorIconByBattery` |

### Properties (`Theme`)

| Property | Purpose |
|----------|---------|
| `ThemeApiVersion` | Contract version (currently `1`) |
| `ConnectedCount`, `HasConnectedControllers` | Count / presence |
| `PrimaryControllerName`, `StatusText`, `PrimaryControllerTooltip` | Text |
| `PrimaryControllerIconGeometry` | Chosen profile silhouette for the primary pad |
| `TopPanelIconGeometry` | Same logic as Desktop top panel (Default vs Primary) |
| `DefaultIconGeometry` | Fixed pack icon (e.g. tester) |
| `PrimaryControllerBatteryLabel` | Localized label (`Low`, `Full`, …) |
| `PrimaryControllerBatteryLevel` | Raw key: `Empty` / `Low` / `Medium` / `Full` |
| `PrimaryControllerBatteryBrush` | Level color whenever battery is known |
| `PrimaryControllerIconBrush` | Icon color **after** applying “color by battery”; may be `null`. Do not use `TargetNullValue={DynamicResource ...}` with `PluginSettings` (crashes the theme). For a setting-aware icon, use `ControllerIcon`. |
| `HasPrimaryControllerBattery` | Known battery level |
| `UsePrimaryControllerBatteryColor` | Known battery **and** user enabled coloring |
| `ColorIconByBattery` | Mirror of the settings checkbox |
| `TopPanelControllerMode` | `Hidden` / `Default` / `Primary` |
| `IsTopPanelButtonVisible` | Desktop top-panel button is visible |

### Converter

```xml
Converter={PluginConverter Plugin=ControllerSessionManager, Converter=IconGeometryConverter}
```

Turns the SVG path string into `Geometry` for `Path.Data`.

> **WPF note:** do not put `{PluginSettings ...}` inside `Setter.Value` on a `Style`/`DataTrigger`. Bind the property on the control, or use the `ControllerIcon` ContentControl, which applies the color setting in code.

## 2. ContentControl elements (shortcuts)

One `x:Name` per element per view (WPF names must be unique). Size with `Width`/`Height` on the placeholder; content scales.

```xml
<ContentControl x:Name="ControllerSessionManager_ControllerIcon"
                Width="28" Height="28"
                Foreground="{DynamicResource TextBrush}"
                Visibility="{PluginStatus Plugin=ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc, Status=Installed}"/>

<ContentControl x:Name="ControllerSessionManager_ControllerBatteryDot"
                Width="10" Height="10" Margin="6,0,0,0"/>

<ContentControl x:Name="ControllerSessionManager_ControllerBatteryText"
                FontSize="16" Margin="6,0,0,0"/>
```

| Element | Shows |
|---------|-------|
| `ControllerStatus` | Compact status text |
| `ControllerCount` | Connected count |
| `PrimaryController` | Primary name |
| `ControllerIcon` | Profile icon; color follows battery setting / placeholder `Foreground` |
| `TopPanelIcon` | Same as Desktop top panel |
| `ControllerBatteryText` | Level label (collapsed without battery); battery color |
| `ControllerBatteryDot` | Level-colored dot (collapsed without battery) |
| `TesterLauncher`, `TesterStatusBadge`, … | Tester blocks |

## 3. Tester (Fullscreen blocks)

Canonical ContentControls under `SourceName = ControllerSessionManager`:

`TesterLauncher`, `TesterStatusBadge`, `TesterButtonMap`, `TesterStickCheck`, `TesterTriggerCheck`, `TesterRumblePad`, `TesterLatencyMini`.

Compatibility aliases keep `SourceName = GamepadTester` and the original 1.1 names (`StatusBadge`, `ButtonMap`, …). Theme commands (`OpenTesterCommand`, `RefreshThemeBlocksCommand`, …) still use `Plugin=GamepadTester`.

If a theme only checks `GamepadTester_518dc982-…`, also add:

`ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`.

Full contract (focus, `CanNavigateBack`, resources, attached `Block` host): [Fullscreen tester integration](EN-Fullscreen-Tester-Integration) and [`docs/theme-integration/CONTRACT.md`](https://github.com/Naerian/playnite-nx-session-controller-manager/blob/main/docs/theme-integration/CONTRACT.md).

## 4. Technical detail

See [`docs/THEME-INTEGRATION.md`](https://github.com/Naerian/playnite-nx-session-controller-manager/blob/main/docs/THEME-INTEGRATION.md). Only items listed here are implemented and supported.
