# Integración Tester Fullscreen

Controller Manager no sustituye la navegación global de Playnite. El tema Fullscreen controla la colocación, el foco, las transiciones, el comportamiento modal y el diseño exterior.

El contrato público del tester para temas es la versión **1.1**. Los nombres de bloques, propiedades de estado y recursos incluidos en este contrato están pensados para conservar compatibilidad hacia atrás. Controller Manager es el dueño del contrato; desinstala el complemento independiente Gamepad Tester.

Para iconos de batería, texto de estado y composición libre, véase [Integración con temas](ES-Integracion-con-Temas). Esta página cubre **solo los bloques del tester**.

## Source names

| Rol | SourceName | Ejemplos de elementos |
| --- | --- | --- |
| Canónico | `ControllerSessionManager` | `TesterLauncher`, `TesterStatusBadge`, `TesterButtonMap`, `TesterStickCheck`, `TesterTriggerCheck`, `TesterRumblePad`, `TesterLatencyMini` |
| Compatibilidad | `GamepadTester` | `GamepadTesterLauncher`, `StatusBadge`, `ButtonMap`, `StickCheck`, `TriggerCheck`, `RumblePad`, `LatencyMini` |

Si un tema solo comprueba el addon id antiguo `GamepadTester_518dc982-…`, añade también:

`ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`.

## Comandos

Los comandos de tema siguen usando la raíz de compatibilidad (`SourceName = GamepadTester`, propiedad del plugin `TesterTheme`):

```xaml
Command="{PluginSettings Plugin=GamepadTester, Path=OpenTesterCommand}"
Command="{PluginSettings Plugin=GamepadTester, Path=OpenButtonTestCommand}"
Command="{PluginSettings Plugin=GamepadTester, Path=OpenSticksCommand}"
Command="{PluginSettings Plugin=GamepadTester, Path=OpenRumbleCommand}"
Command="{PluginSettings Plugin=GamepadTester, Path=OpenLatencyCommand}"
Command="{PluginSettings Plugin=GamepadTester, Path=RefreshThemeBlocksCommand}"
```

## Bloques integrables

Hosts por nombre (Playnite rellena el ContentControl):

```xaml
<!-- Canónico -->
<ContentControl x:Name="ControllerSessionManager_TesterStatusBadge" />
<ContentControl x:Name="ControllerSessionManager_TesterButtonMap" />

<!-- Compatibilidad -->
<ContentControl x:Name="GamepadTester_StatusBadge" />
<ContentControl x:Name="GamepadTester_ButtonMap" />
<ContentControl x:Name="GamepadTester_StickCheck" />
<ContentControl x:Name="GamepadTester_TriggerCheck" />
<ContentControl x:Name="GamepadTester_RumblePad" />
<ContentControl x:Name="GamepadTester_LatencyMini" />
```

Bloques lógicos disponibles: launcher, badge de estado, mapa de botones, sticks, gatillos, rumble y latencia. Comparten un runtime de polling ligero mientras están visibles y omiten intencionadamente exportaciones y selectores exclusivos de Escritorio.

## Vistas dinámicas

Las vistas creadas después de cargar el tema se inicializan en el evento `Loaded`. La forma más fiable utiliza la propiedad adjunta `Block`:

```xaml
<UserControl
    xmlns:gt="clr-namespace:ControllerSessionManager.Tester.Views.ThemeIntegration;assembly=ControllerSessionManager">
    <ContentControl gt:GamepadTesterThemeHost.Block="ButtonMap" />
    <ContentControl gt:GamepadTesterThemeHost.Block="TriggerCheck" />
    <ContentControl gt:GamepadTesterThemeHost.Block="RumblePad" />
</UserControl>
```

La propiedad adjunta es la opción recomendada para ventanas personalizadas abiertas dinámicamente por plugins auxiliares. La inicialización por nombre también admite `GamepadTester_ButtonMap`, `GamepadTesterButtonMap`, `TesterButtonMap` y los nombres equivalentes para cada bloque.

Si un plugin auxiliar crea o sustituye contenido después de `Loaded`, solicita un nuevo escaneo con `RefreshThemeBlocksCommand` (véase Comandos).

Cada host marcado expone las propiedades adjuntas de solo lectura `InitializationState`, `InitializationMessage`, `ResolvedBlock` y `ContractVersion`. `InitializationState` puede ser `Pending`, `WaitingForPlugin`, `Ready`, `UnknownBlock`, `Occupied` o `Error`.

```xaml
<TextBlock Text="{Binding ElementName=ButtonMapHost, Path=(gt:GamepadTesterThemeHost.InitializationState)}" />
<TextBlock Text="{Binding ElementName=ButtonMapHost, Path=(gt:GamepadTesterThemeHost.InitializationMessage)}" />
```

`Occupied` significa que el host ya tenía contenido y el plugin decidió no reemplazarlo.

## Foco y comportamiento modal

Coloca los bloques interactivos dentro de un ámbito de foco, mueve el foco al primer botón al abrir la página y contiene la navegación direccional. Si la página funciona como modal, desactiva u oculta la lista de juegos inferior para evitar que Playnite navegue por detrás.

`ButtonMap`, `StickCheck`, `RumblePad` y `LatencyMini` contienen controles interactivos. La captura de botones, sticks y latencia solo comienza al activar su propia acción. Mientras haya una captura activa, el tema debe desactivar las acciones Volver/B y cerrar; enlaza esas acciones a `CanNavigateBack`, que será falso mientras la captura controle la entrada. La extensión también expone `IsButtonCaptureRunning`, `IsStickCaptureRunning`, `IsLatencyTestRunning` y el estado agregado `IsFullscreenInputCaptureActive` en el contexto de datos de cada bloque. Al detener la captura, la extensión libera el bloqueo de navegación e intenta devolver el foco a un elemento opcional llamado `GamepadTester_BackButton`.

La extensión desactiva automáticamente un control visible llamado `GamepadTester_BackButton` durante la captura. Mantén también el enlace a `CanNavigateBack`, porque algunos helpers implementan B/cerrar retirando el contenido sin ejecutar el cierre WPF de la ventana.

`StatusBadge` y `TriggerCheck` son bloques informativos. El dibujo del mando lo aporta la extensión; el tema controla su contenedor, tamaño, posición, visibilidad y toda la interfaz que lo rodea.

Cada bloque también expone `IsControllerConnected`, `IsInputCaptureActive`, `CanNavigateBack`, `ActiveTestKind` y `ThemeContractVersion`. `ActiveTestKind` utiliza valores estables: `None`, `Buttons`, `Sticks`, `Latency` y `Rumble`. El contexto de datos compartido expone los comandos y valores detallados, por lo que los temas deben enlazarse a esos estados en vez de inspeccionar los elementos visuales internos. Un helper que retire contenido personalizado en vez de cerrar una ventana WPF debe comprobar `CanNavigateBack` por su cuenta.

```xaml
<Button x:Name="GamepadTester_BackButton"
        IsEnabled="{Binding ElementName=GamepadTester_ButtonMap, Path=Content.CanNavigateBack}"
        Command="{Binding CloseCommand}" />
```

Mantener `LB + RB` un segundo finaliza la captura de botones, sticks o latencia antes de que Volver vuelva a estar disponible.

## Recursos del tema

Los bloques integrados utilizan recursos dinámicos propios, de modo que el tema puede personalizarlos localmente sin cambiar los brushes globales de Playnite:

```xaml
<UserControl.Resources>
    <SolidColorBrush x:Key="GamepadTesterControlBackgroundBrush" Color="#181C24" />
    <SolidColorBrush x:Key="GamepadTesterButtonBackgroundBrush" Color="#242A35" />
    <SolidColorBrush x:Key="GamepadTesterControlBorderBrush" Color="#566174" />
    <SolidColorBrush x:Key="GamepadTesterStickGuideBrush" Color="#75839A" />
    <SolidColorBrush x:Key="GamepadTesterTextBrush" Color="#F4F6FA" />
</UserControl.Resources>
```

`GamepadTesterStickGuideBrush` controla los círculos exteriores, los anillos de rango y las guías horizontales y verticales de `StickCheck` sin modificar los bordes de paneles o botones. Si un recurso específico no está definido, la extensión utiliza el pincel genérico correspondiente de Playnite; las guías de sticks usan `ControlBorderBrush` como alternativa. Los recursos declarados en la ventana o vista personalizada tienen prioridad sobre el fallback de la aplicación.

Una vista de referencia y el contrato condensado están en el repositorio bajo `docs/theme-integration`.

Siguiente: [Solución de problemas y FAQ](ES-Solucion-de-Problemas-y-Preguntas-Frecuentes)
