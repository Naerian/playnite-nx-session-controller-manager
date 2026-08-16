# Integración con temas

Controller Session Manager expone tres elementos personalizados y una pequeña API de ajustes para temas de Playnite. El botón automático de la barra superior de Desktop es independiente y no requiere modificar el tema.

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

El nombre de origen es `ControllerSessionManager`. El tema debe colapsar o dejar vacío el espacio cuando el plugin no esté instalado.

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
