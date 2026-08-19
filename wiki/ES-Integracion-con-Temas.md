# Integración con temas

Controller Manager expone tres elementos personalizados y una pequeña API de ajustes para temas de Playnite. El botón automático de la barra superior de Desktop es independiente y no requiere modificar el tema.

## Elementos personalizados

Registra un placeholder con el nombre exacto y respetando mayúsculas:

```xml
<ContentControl x:Name="ControllerSessionManager_ControllerStatus"
                Visibility="{PluginStatus Plugin=ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc, Status=Installed}" />
```

Elementos disponibles:

- `ControllerSessionManager_ControllerStatus`: estado compacto en tiempo real.
- `ControllerSessionManager_ControllerCount`: número de mandos conectados.
- `ControllerSessionManager_PrimaryController`: nombre del mando principal.
- `ControllerSessionManager_TesterLauncher`, `TesterStatusBadge`, `TesterButtonMap`, `TesterStickCheck`, `TesterTriggerCheck`, `TesterRumblePad`, `TesterLatencyMini`: bloques del tester. SDL se muestrea en un host externo.

Los alias de compatibilidad conservan `SourceName = GamepadTester` y los nombres 1.1 originales. Desinstala el complemento Gamepad Tester independiente antes de usarlos.

Los temas que muestran u ocultan el tester con `{PluginStatus Plugin=GamepadTester_518dc982-32b5-4493-b32d-1f71de2fe4ad, Status=Installed}` lo darán por ausente tras esa desinstalación. Hay que añadir un segundo trigger para `ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`. Los nombres de bloque, `Tag`, brushes `GamepadTester_*` y `GamepadTester_BackButton` siguen siendo compatibles; la ventana del tester de Aniki ya usa esos alias.

El nombre de origen de estado es `ControllerSessionManager`. El tema debe colapsar o dejar vacío el espacio cuando el plugin no esté instalado.

## API de ajustes para temas

El objeto estable `Theme` expone actualmente:

- `ThemeApiVersion`
- `ConnectedCount` y `HasConnectedControllers`
- `PrimaryControllerName` y `StatusText`
- `PrimaryControllerIconGeometry`
- `PrimaryControllerBatteryLabel`, `PrimaryControllerBatteryBrush` y `HasPrimaryControllerBattery`
- `PrimaryControllerTooltip`
- `UsePrimaryControllerBatteryColor`

Ejemplo:

```xml
<TextBlock Text="{PluginSettings Plugin=ControllerSessionManager, Path=Theme.PrimaryControllerName}" />
```

## Acceso rápido de Desktop

El indicador incluido localiza su ancestro interno `TopPanelItem` por el nombre del tipo en ejecución y escucha su ancho real. Con 58 px o más puede mostrar icono y batería; por debajo usa solo el icono. No contiene excepciones por nombre de tema y libera la suscripción `SizeChanged` al descargarse.

Para detalles técnicos y la evolución prevista consulta [`docs/THEME-INTEGRATION.md`](https://github.com/Naerian/playnite-nx-session-controller-manager/blob/main/docs/THEME-INTEGRATION.md). Solo deben considerarse implementados los tres elementos y propiedades enumerados aquí.
