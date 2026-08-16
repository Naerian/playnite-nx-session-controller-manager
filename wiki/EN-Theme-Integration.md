# Theme Integration

Controller Session Manager exposes three custom elements and a small settings API for Playnite themes. The automatic Desktop top-panel button is separate and does not require theme changes.

## Custom elements

Register a placeholder with the exact case-sensitive name:

```xml
<ContentControl x:Name="ControllerSessionManager_ControllerStatus"
                Visibility="{PluginStatus Plugin=ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc, Status=Installed}" />
```

Available elements:

- `ControllerSessionManager_ControllerStatus`: compact live status text.
- `ControllerSessionManager_ControllerCount`: connected-controller count.
- `ControllerSessionManager_PrimaryController`: current primary controller name.

The source name is `ControllerSessionManager`. Themes should provide a graceful collapsed or empty fallback when the plugin is absent.

## Theme settings API

The stable `Theme` object currently exposes:

- `ThemeApiVersion`
- `ConnectedCount` and `HasConnectedControllers`
- `PrimaryControllerName` and `StatusText`
- `PrimaryControllerIconGeometry`
- `PrimaryControllerBatteryLabel`, `PrimaryControllerBatteryBrush` and `HasPrimaryControllerBattery`
- `PrimaryControllerTooltip`
- `UsePrimaryControllerBatteryColor`

Example:

```xml
<TextBlock Text="{PluginSettings Plugin=ControllerSessionManager, Path=Theme.PrimaryControllerName}" />
```

## Desktop quick-access indicator

The built-in Desktop indicator finds its internal `TopPanelItem` ancestor by runtime type name and listens to its real width. At 58 px or more it can show icon and battery; under 58 px it uses the icon only. No theme names or theme-specific exceptions are used. The `SizeChanged` subscription is released when the control unloads.

For implementation details and the evolving API roadmap, see [`docs/THEME-INTEGRATION.md`](https://github.com/Naerian/playnite-nx-session-controller-manager/blob/main/docs/THEME-INTEGRATION.md). Only the three elements and properties listed above should currently be treated as implemented.
