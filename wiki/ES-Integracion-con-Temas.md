# Integración con temas

Controller Manager ofrece dos capas oficiales para el **XAML del tema de Playnite**:

1. **API de datos** (`PluginSettings` + `PluginConverter`) — libertad total para componer UI.
2. **Elementos ContentControl** — atajos listos para soltar y redimensionar.

El botón automático de la barra superior de Desktop es independiente y no requiere modificar el tema.

Las **notificaciones, el overlay y los sonidos** siguen otro camino de integración: incluye una [carpeta `ControllerManager/` dentro del tema](ES-Integracion-de-Apariencia-en-Temas) (con `theme-bridge.json` opcional). Eso **no** es lo mismo que publicar un [diseño de creador para la comunidad](ES-Disenos-de-Creadores) (`.csmtheme`).

Addon Id: `ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`  
SourceName: `ControllerSessionManager`  
SettingsRoot: `Theme` (las rutas van **sin** prefijo `Theme.`)

## 1. API de datos (composición libre)

```xml
<!-- Ejemplo: icono del perfil + punto de batería (el color del icono respeta el ajuste del plugin) -->
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

Otras composiciones habituales:

| Idea | Cómo |
|------|------|
| Solo icono, siempre con color de batería | `Fill`/`Stroke` → `PrimaryControllerBatteryBrush` |
| Solo icono, sin color (sigue el tema) | `Fill` → `{DynamicResource TextBrush}` |
| Icono fijo del pack (no el del mando) | `Path=DefaultIconGeometry` (+ converter) |
| Icono como el top panel Desktop | `Path=TopPanelIconGeometry` |
| Solo texto de nivel | `Text` → `PrimaryControllerBatteryLabel` |
| Icono propio del tema + color CSM | Tu FontIcon / Path + `PrimaryControllerBatteryBrush` en un dot |
| Respetar modo Hidden/Default/Primary | Leer `TopPanelControllerMode` / `IsTopPanelButtonVisible` / `ColorIconByBattery` |

### Propiedades (`Theme`)

| Propiedad | Uso |
|-----------|-----|
| `ThemeApiVersion` | Versión del contrato (actualmente `1`) |
| `ConnectedCount`, `HasConnectedControllers` | Conteo / presencia |
| `PrimaryControllerName`, `StatusText`, `PrimaryControllerTooltip` | Texto |
| `PrimaryControllerIconGeometry` | Silueta del perfil elegido del primario |
| `TopPanelIconGeometry` | Misma lógica que el top panel Desktop (Default vs Primary) |
| `DefaultIconGeometry` | Icono fijo del pack (p. ej. tester) |
| `PrimaryControllerBatteryLabel` | Etiqueta localizada (`Low`, `Full`, …) |
| `PrimaryControllerBatteryLevel` | Clave cruda: `Empty` / `Low` / `Medium` / `Full` |
| `PrimaryControllerBatteryBrush` | Color del nivel (siempre que haya batería) |
| `PrimaryControllerIconBrush` | Color del icono **tras** aplicar «colorear por batería»; puede ser `null`. No uses `TargetNullValue={DynamicResource ...}` con `PluginSettings` (rompe el tema). Para icono con color según ajuste, usa `ControllerIcon`. |
| `HasPrimaryControllerBattery` | Hay nivel conocido |
| `UsePrimaryControllerBatteryColor` | Batería conocida **y** el usuario activó colorear |
| `ColorIconByBattery` | Espejo del checkbox de ajustes |
| `TopPanelControllerMode` | `Hidden` / `Default` / `Primary` |
| `IsTopPanelButtonVisible` | El botón Desktop del top panel está visible |

### Converter

```xml
Converter={PluginConverter Plugin=ControllerSessionManager, Converter=IconGeometryConverter}
```

Convierte el string de geometría SVG en `Geometry` para `Path.Data`.

> **Nota WPF:** no uses `{PluginSettings ...}` dentro de `Setter.Value` en un `Style`/`DataTrigger`. Enlaza la propiedad directamente en el control, o usa `ControllerIcon ContentControl (aplica el ajuste de color).

## 2. Elementos ContentControl (atajos)

Un `x:Name` por elemento y por vista (WPF no permite duplicados). Redimensiona con `Width`/`Height` en el placeholder; el contenido escala.

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

| Elemento | Qué muestra |
|----------|-------------|
| `ControllerStatus` | Texto de estado compacto |
| `ControllerCount` | Número de mandos |
| `PrimaryController` | Nombre del primario |
| `ControllerIcon` | Icono de perfil; color según ajuste de batería / `Foreground` del placeholder |
| `TopPanelIcon` | Como el top panel Desktop |
| `ControllerBatteryText` | Etiqueta de nivel (oculto sin batería); color de batería |
| `ControllerBatteryDot` | Punto con color de nivel (oculto sin batería) |
| `TesterLauncher`, `TesterStatusBadge`, … | Bloques del tester |

## 3. Tester (bloques Fullscreen)

ContentControls canónicos bajo `SourceName = ControllerSessionManager`:

`TesterLauncher`, `TesterStatusBadge`, `TesterButtonMap`, `TesterStickCheck`, `TesterTriggerCheck`, `TesterRumblePad`, `TesterLatencyMini`.

Los alias de compatibilidad mantienen `SourceName = GamepadTester` y los nombres 1.1 originales (`StatusBadge`, `ButtonMap`, …). Los comandos de tema (`OpenTesterCommand`, `RefreshThemeBlocksCommand`, …) siguen usando `Plugin=GamepadTester`.

Si el tema solo comprueba `GamepadTester_518dc982-…`, añade también:

`ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`.

Contrato completo (foco, `CanNavigateBack`, recursos, host adjunto `Block`): [Integración Tester Fullscreen](ES-Integracion-Tester-Fullscreen) y [`docs/theme-integration/CONTRACT.md`](https://github.com/Naerian/playnite-nx-session-controller-manager/blob/main/docs/theme-integration/CONTRACT.md).

## 4. Detalle técnico

Más contexto en [`docs/THEME-INTEGRATION.md`](https://github.com/Naerian/playnite-nx-session-controller-manager/blob/main/docs/THEME-INTEGRATION.md). Lo implementado y soportado es lo enumerado en este documento.
