# Integración con themes de Playnite

## 1. Qué permite realmente Playnite

Playnite ofrece un mecanismo oficial de Custom UI Integration:

1. el plugin registra nombres con `AddCustomElementSupport`;
2. el theme declara un `ContentControl` cuyo `x:Name` es `SourceName_ElementName`;
3. Playnite llama `GetGameViewControl` y el plugin inyecta un `Control`;
4. el control puede derivar de `PluginUserControl` y acceder a `GameContext`.

También existen:

- `PluginStatus` para saber si el add-on está instalado;
- `PluginSettings` para enlazar configuración que el plugin registró con `AddSettingsSupport`;
- `PluginConverter` para usar converters registrados por el plugin.

No está documentado un mecanismo para que un theme haga binding arbitrario a un ViewModel runtime global del plugin. Por ello, CSM no promete XAML como `{Binding Controller.BatteryPercent}` fuera de un control suministrado por CSM. La flexibilidad avanzada se proporciona mediante controles pequeños y templates/resource keys estables.

## 2. Identidad y versiones

```text
SourceName: ControllerSessionManager
ThemeApiVersion: 1
AddonId: se fijará al crear extension.yaml y no cambiará
```

La API v1 incluye nombres de elementos, significado, DependencyProperties públicas, estados visuales, resource keys y converters documentados. La versión del plugin no equivale a Theme API version.

Compatibilidad:

- añadir un elemento o propiedad opcional es compatible;
- eliminar/renombrar o cambiar semántica requiere Theme API major nueva;
- elementos v1 permanecerán al menos durante dos releases major del plugin tras anunciar deprecación;
- los estados desconocidos siempre tienen fallback visual.

## 3. Integración mínima

```xml
<ContentControl
    x:Name="ControllerSessionManager_BatteryIndicator"
    Visibility="{PluginStatus Plugin=ADDON_GUID, Status=Installed}" />
```

El `ContentControl` es un placeholder: Playnite lo reemplaza/puebla con el control que devuelve CSM. Tanto el plugin como el theme deben soportar el elemento.

## 4. Catálogo v1

### `ControllerStatus`

Resumen compacto del primario: icono, nombre corto, conexión y batería si existe.

```xml
<ContentControl x:Name="ControllerSessionManager_ControllerStatus" />
```

Estados: `NoController`, `Connected`, `Missing`, `Unknown`. Si no existe primario, muestra fallback configurable; nunca inventa batería.

### `BatteryIndicator`

Componente completo icono + texto + barra/segmentos.

```xml
<ContentControl x:Name="ControllerSessionManager_BatteryIndicator" />
```

Estados: `Unavailable`, `Exact`, `Discrete`, `Wired`, `Charging`, `Low`, `Critical`. En `Discrete`, muestra `Low/Medium/Full` o segmentos, no un porcentaje.

### `ControllerIcon`

Icono de familia y estado del primario.

```xml
<ContentControl x:Name="ControllerSessionManager_ControllerIcon" />
```

Familias v1: `Xbox`, `XboxSeries`, `DualSense`, `DualShock4`, `SwitchPro`, `JoyCon`, `Generic`, `Unknown`. La clasificación depende de evidencia; `Unknown` es normal.

### `ControllerInfo`

Panel detallado con nombre, batería, conexión, player y estado.

```xml
<ContentControl x:Name="ControllerSessionManager_ControllerInfo" />
```

Los campos no disponibles se ocultan o muestran `—` según recursos del theme.

### Elementos modulares

```xml
<StackPanel Orientation="Horizontal">
    <ContentControl x:Name="ControllerSessionManager_ControllerIcon" />
    <ContentControl x:Name="ControllerSessionManager_BatteryText" />
    <ContentControl x:Name="ControllerSessionManager_BatteryBar" />
    <ContentControl x:Name="ControllerSessionManager_ConnectionIcon" />
</StackPanel>
```

- `BatteryText`: `82%`, `Low`, `Full`, `Wired` o `—`.
- `BatteryBar`: continua sólo para exacto; segmentada para discreto.
- `ConnectionIcon`: `Usb`, `Bluetooth`, `WirelessAdapter`, `Virtual`, `Unknown`.
- `ControllerCount`: connected/active según configuración del control.

### `PrimaryController` y `ActiveController`

`PrimaryController` representa la preferencia/primario actual. `ActiveController` representa el de input significativo más reciente en la sesión. Pueden diferir y no deben tratarse como sinónimos.

### `ControllerList`

```xml
<ContentControl x:Name="ControllerSessionManager_ControllerList" />
```

Lista reutilizable para todos los mandos. El control interno usa una colección de snapshots y virtualización cuando la plantilla lo permita. Modos planeados v1: `Connected`, `SessionActive`, `AllKnown`.

### `PlayerSlot1` … `PlayerSlot4`

```xml
<UniformGrid Columns="2">
    <ContentControl x:Name="ControllerSessionManager_PlayerSlot1" />
    <ContentControl x:Name="ControllerSessionManager_PlayerSlot2" />
    <ContentControl x:Name="ControllerSessionManager_PlayerSlot3" />
    <ContentControl x:Name="ControllerSessionManager_PlayerSlot4" />
</UniformGrid>
```

El player slot es lógico y puede provenir de XInput o asignación de sesión. Si no está disponible, queda vacío/unknown; no se deduce del orden arbitrario de enumeración.

## 5. Estilizado avanzado soportado

Los controles buscarán recursos por clave y conservarán defaults internos. Un theme puede sobrescribirlos en su diccionario:

```xml
<SolidColorBrush x:Key="CSM.ForegroundBrush" Color="#FFFFFFFF" />
<SolidColorBrush x:Key="CSM.LowBatteryBrush" Color="#FFFFB020" />
<SolidColorBrush x:Key="CSM.CriticalBatteryBrush" Color="#FFFF4050" />

<Style x:Key="CSM.BatteryTextStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="18" />
</Style>
```

Catálogo de keys v1 previsto:

```text
CSM.ForegroundBrush
CSM.SecondaryForegroundBrush
CSM.ConnectedBrush / MissingBrush / UnknownBrush
CSM.LowBatteryBrush / CriticalBatteryBrush / ChargingBrush
CSM.ControllerIconStyle
CSM.BatteryTextStyle / BatteryBarStyle
CSM.ControllerItemStyle
CSM.ControllerListItemTemplate
CSM.Icon.Controller.{Family}
CSM.Icon.Connection.{Type}
CSM.Icon.Battery.{State}
```

El spike de theme integration debe confirmar el orden exacto de resolución de recursos cuando un control inyectado busca claves definidas por el theme. Si el host no permite override fiable, la API v1 usará DependencyProperties/configuración de los controles y documentará esa limitación antes de estabilizar nombres.

## 6. Propiedades visuales

Cada control raíz implementará DependencyProperties sólo donde Playnite preserve atributos del placeholder o donde el control pueda configurarse por recursos. Esto debe comprobarse: `GetGameViewControl` devuelve un control nuevo y no hay garantía documentada de copiar propiedades arbitrarias desde el `ContentControl` placeholder.

Por tanto, la API comprometida inicialmente es:

- selección mediante distintos nombres de elemento;
- estilos/templates mediante resource keys verificadas;
- settings globales mediante `PluginSettings` sólo para configuración real;
- no atributos inventados en el placeholder hasta que un spike confirme el comportamiento.

## 7. Settings visibles al theme

Ejemplo oficial del mecanismo:

```xml
<TextBlock Text="{PluginSettings Plugin=ControllerSessionManager, Path=Overlay.SelectedTheme}" />
```

Sólo se expondrá un subconjunto estable y no sensible, por ejemplo:

```text
ThemeApiVersion
Display.ShowBatteryPercentWhenExact
Display.UnknownBatteryText
Display.ControllerListMode
Display.UseColoredWarnings
```

No se expondrán rutas, seriales, logs, PIDs ni objetos de servicio. El estado vivo fluye dentro de los custom controls.

## 8. Converters

Converters potenciales:

- `BatteryLevelToBrushConverter`;
- `ConnectionTypeToIconConverter`;
- `ControllerFamilyToIconConverter`;
- `NullOrUnknownToVisibilityConverter`.

Sólo son útiles cuando el binding fuente ya está disponible en el contexto del control/theme. No convierten `PluginConverter` en un canal para obtener el estado de CSM.

## 9. Desktop y Fullscreen

La misma lista lógica se registrará para ambos modos, pero cada elemento se probará en ambos. El theme debe colocar explícitamente los placeholders en sus vistas. CSM no modifica archivos del theme instalado, porque Playnite reemplaza el directorio completo del theme durante actualizaciones.

Recomendaciones para Fullscreen:

- targets de 44–48 px o más si el control es interactivo;
- evitar texto que cambie ancho cada segundo;
- usar `BatteryText` sólo si hay espacio, `BatteryIndicator` para composición completa;
- reservar `ControllerList` para paneles, no top bar estrecha;
- mantener fallback cuando el plugin no esté instalado mediante `PluginStatus`.

## 10. Assets

El paquete del plugin incluye iconos neutrales. Los themes pueden sustituir resource keys, no archivos dentro del directorio del plugin. Así una actualización no borra modificaciones ni un theme escribe fuera de su ámbito.

Cada icono debe tener fallback `Generic/Unknown`. VID/PID ayuda a clasificar, pero el theme no recibe esos valores por defecto para evitar acoplar diseño con identificación hardware.

## 11. Checklist para autores

1. Declarar compatibilidad con Theme API v1 en la documentación del theme.
2. Añadir `PluginStatus` o diseñar un espacio que colapse si falta CSM.
3. Usar nombres exactos y case-sensitive.
4. Probar batería exacta, discreta, wired y unavailable.
5. Probar cero, uno y cuatro mandos.
6. Probar nombre largo y familia unknown.
7. No asumir que player index siempre existe.
8. Probar Desktop/Fullscreen y DPI alto.

## 12. Trabajo necesario antes de congelar API v1

- Prototipo real con un theme Desktop y uno Fullscreen.
- Confirmar resolución/override de resource dictionaries.
- Confirmar recreación y disposal de controles al cambiar theme/view.
- Verificar frecuencia segura de `INotifyPropertyChanged`.
- Capturar screenshots de todos los estados.
- Publicar sample theme mínimo y sample modular.
- Añadir contract tests del catálogo de nombres.

## 13. Fuentes oficiales

- [Playnite: Custom UI Integration para plugins](https://api.playnite.link/docs/tutorials/extensions/customUiIntegration.html)
- [Playnite: integración desde themes](https://api.playnite.link/docs/tutorials/themes/extensionIntegration.html)
- [Playnite: introducción a themes WPF](https://api.playnite.link/docs/tutorials/themes/introduction.html)
- [Playnite: instalación y reemplazo de directorios de themes](https://api.playnite.link/docs/manual/features/themesSupport/installingThemes.html)

