# Packs de apariencia embebidos en temas de Playnite

**Público:** desarrolladores que mantienen un **tema de Playnite** (Escritorio y/o Fullscreen) y quieren que las notificaciones, el overlay y los sonidos de Controller Manager encajen con ese tema de forma automática.

Incluye los archivos **dentro de la carpeta del tema**. Quien no diseña temas comparte un look exportando un perfil visual (`.pcvisual`) desde **Apariencia → Estilos**.

## Estructura de carpetas

Incluye esto dentro del tema de Playnite activo:

```text
Themes/Desktop/{ThemeId}/ControllerManager/
Themes/Fullscreen/{ThemeId}/ControllerManager/
  manifest.json
  notification.json      layout/colores opcionales de notificaciones
  overlay.json           layout/colores opcionales del overlay
  theme-bridge.json      puente opcional de paletas en vivo
  Audio/                 sonidos opcionales
  Fonts/                 fuentes opcionales
  assets/                imágenes referenciadas por el JSON
```

`manifest.json`, `notification.json` y `overlay.json` describen el look. Los usuarios del tema **no** necesitan un paquete aparte.

## Comportamiento en tiempo de ejecución

1. El usuario elige un **look** en **Apariencia → Estilos** (preset del plugin, personalizado o perfil importado). Ese look se aplica cuando el interruptor de diseño del tema está desactivado o cuando el tema activo no incluye pack `ControllerManager/` para esa superficie.
2. Si un **interruptor de diseño del tema de Playnite** está activo y el tema incluye `notification.json` u `overlay.json` en `ControllerManager/`, ese diseño embebido controla por completo esa superficie (layout, colores, fuentes, imágenes y sonidos).
3. Opcionalmente, **`theme-bridge.json`** mapea las claves de recursos WPF de tu tema a roles de color/tipografía de Controller Manager para que los packs de color del tema sigan en sincronía cuando el interruptor está activo.

## `theme-bridge.json`

Ruta: `{CarpetaTema}/ControllerManager/theme-bridge.json`

```json
{
  "Notification": {
    "Background": "NotificationBackgroundBrush",
    "Gradient": "MenuBackgroundBottomColor",
    "TextStyle": "TextBlockBoldBaseStyle",
    "MessageStyle": "TextBlockBaseStyle",
    "Border": "PopupBorderBrush",
    "Accent": "GlyphBrush",
    "SecondaryText": "TextBrushDarker",
    "Warning": "WarningBrush"
  },
  "Overlay": {
    "Background": "ControlBackgroundBrush",
    "Text": "TextBrush",
    "Accent": "GlyphBrush",
    "Border": "NormalBorderBrush",
    "Warning": "WarningBrush"
  }
}
```

Las claves de la izquierda son roles de Controller Manager. Los valores son **las claves de recursos de tu tema**. El plugin las resuelve con `Application.Current.TryFindResource` cuando el interruptor correspondiente está activo.

## Consejos de prueba

- **Notificaciones fullscreen:** previsualiza desde **Playnite Fullscreen**. El modo escritorio no puede cargar el tema fullscreen activo, así que las vistas previas desde ajustes de escritorio pueden no coincidir.
- **Notificaciones de escritorio:** prueba con el tema Desktop activo y el interruptor de escritorio encendido.
- **Overlay:** se ejecuta en un proceso aparte; los colores llegan como hex resuelto (el puente se aplica en el proceso de Playnite antes del IPC).

## Documentación relacionada

- [Integración con temas](ES-Integracion-con-Temas) — elementos ContentControl y API `PluginSettings` en XAML del tema
- [Notificaciones y overlay](ES-Notificaciones-y-Overlay) — comportamiento para el usuario final
