# Diseños de creadores para Controller Manager

Controller Manager puede distribuir diseños visuales completos para sus notificaciones de mando y el overlay de desconexión. Un diseño de creador es una carpeta autónoma, revisada y guardada en este repositorio que se incluye en el paquete `.pext`. Puede contener definiciones JSON, imágenes, fuentes y sonidos.

El sistema está pensado para autores de temas de Playnite y diseñadores que quieran integrar Controller Manager en su lenguaje visual.

## Diferencias entre diseños de creadores e importados

- **Diseños de creadores:** packs versionados bajo `CreatorThemes/`. Incluyen autor, versión, recursos y sonidos opcionales, se revisan mediante pull request y se distribuyen con el plugin. Sus controles se bloquean mientras están activos para conservar el diseño original.
- **Diseños importados:** archivos `.pcvisual` importados por un usuario. Contienen una captura con nombre de las dos notificaciones, el overlay y el sonido. Se guardan en los datos del plugin y pueden eliminarse desde el selector. Importar otro con el mismo nombre lo sustituye.
- **Presets del plugin:** diseños mantenidos directamente en el código de Controller Manager.
- **Personalizado:** configuración editable del usuario.

No envíes un `.pcvisual` como diseño de creador. Utiliza el formato de carpeta descrito aquí.

## Flujo de contribución

1. Haz un fork del repositorio de Controller Manager.
2. Crea una carpeta única bajo `CreatorThemes/`; usa letras ASCII, números, `_` o `-`.
3. Añade `manifest.json` y al menos uno entre `notification.json` y `overlay.json`.
4. Incluye únicamente recursos redistribuibles y sus licencias o créditos.
5. Compila y prueba el pack en Playnite Desktop y Fullscreen.
6. Confirma la carpeta y abre un pull request con capturas de todas las superficies compatibles.

Los packs no se copian a `ExtensionsData` y no existe una recarga desde una carpeta del usuario. Los packs nuevos o modificados llegan mediante una versión de Controller Manager.

## Estructura de carpetas

```text
CreatorThemes/
└── MiTema/
    ├── manifest.json             obligatorio
    ├── notification.json         opcional; notificaciones
    ├── overlay.json              opcional; overlay de desconexión
    ├── Images/                   opcional
    │   └── background.png
    ├── Fonts/                    opcional
    │   ├── MiFuente-Regular.ttf
    │   └── LICENSE.txt
    ├── Audio/                    opcional
    │   ├── connected.wav
    │   ├── disconnected.wav
    │   ├── warning.wav
    │   └── low-battery.wav
    ├── LICENSE.txt               recomendado
    └── CREDITS.md                recomendado
```

Todos los recursos deben permanecer dentro del pack. Se rechazan rutas que intenten salir mediante `..`. Conserva también las mayúsculas y minúsculas de los nombres de archivo.

## `manifest.json`

```json
{
  "Id": "mi-tema",
  "Name": "Mi tema",
  "Author": "Nombre del creador",
  "Version": "1.0.0",
  "Description": "Descripción breve mostrada en Controller Manager.",
  "RecommendedTheme": "Nombre opcional del tema de Playnite",
  "DesktopThemeIds": ["id-del-tema-desktop"],
  "FullscreenThemeIds": ["id-del-tema-fullscreen"],
  "Fonts": [
    {
      "Id": "Heading",
      "Name": "Mi fuente — Títulos",
      "Family": "Familia real almacenada dentro del archivo",
      "Folder": "Fonts"
    }
  ],
  "Sounds": {
    "Connected": "Audio/connected.wav",
    "Disconnected": "Audio/disconnected.wav",
    "Warning": "Audio/warning.wav",
    "LowBattery": "Audio/low-battery.wav"
  }
}
```

| Campo | Obligatorio | Significado |
| --- | --- | --- |
| `Id` | Sí | Identificador estable y único. No debe cambiar tras publicarse. |
| `Name` | Sí | Nombre visible del diseño. |
| `Author` | Sí | Creador o equipo mostrado en el selector. |
| `Version` | No | Versión del pack; por defecto `1.0.0`. Se recomienda versionado semántico. |
| `Description` | No | Explicación breve de la intención visual. |
| `RecommendedTheme` | No | Tema de Playnite al que acompaña. También sirve como fallback de compatibilidad por nombre. |
| `ThemeIds` | No | IDs aceptados en Desktop y Fullscreen cuando ambos usan realmente el mismo ID. |
| `DesktopThemeIds` | No | IDs exactos de los temas Desktop compatibles. |
| `FullscreenThemeIds` | No | IDs exactos de los temas Fullscreen compatibles. |
| `Fonts` | No | Fuentes registradas por el pack. |
| `Sounds` | No | Mapa de audio por evento. |

`Id`, `Name` y `Author` no pueden estar vacíos. El pack se ignora si ninguno de los dos JSON de apariencia contiene propiedades. No se admiten IDs duplicados.

Controller Manager lee los IDs configurados por Playnite y, en instalaciones portables, los verifica en `config.json` y `fullscreenConfig.json`. Al activar **En diseños de creador, mostrar solo los del tema actual**, el selector de escritorio usa `DesktopThemeIds` y el de pantalla completa usa `FullscreenThemeIds`. El overlay acepta coincidencias de cualquiera de los dos modos. `RecommendedTheme` solo actúa como fallback cuando Playnite expone un nombre en lugar de un ID; nunca se inspeccionan nombres de carpetas ni la estructura visual.

Obtén el ID del `theme.yaml`, no del nombre de la carpeta:

```yaml
Id: my_desktop_theme_00000000-0000-0000-0000-000000000000
Name: Mi tema de escritorio
```

## Aplicación de los JSON de apariencia

`notification.json` y `overlay.json` son objetos JSON planos. Cada clave debe coincidir con el nombre de una propiedad pública de Controller Manager. La comparación no distingue mayúsculas, pero debe usarse la nomenclatura documentada.

```json
{
  "OverlayCardColor": "#F020232A",
  "OverlayScalePercent": 105,
  "OverlayShowBorder": true
}
```

Los valores se aplican sobre una base estable del plugin. Las propiedades omitidas conservan esa base: no heredan el diseño Personalizado del usuario. Las propiedades desconocidas, de solo lectura o con un tipo incorrecto se ignoran. Un pack mal formado no debe impedir que se abra la configuración, pero puede quedar incompleto.

Los colores usan notación hexadecimal WPF. Se recomienda `#AARRGGBB`, que incluye alfa; `#RRGGBB` se acepta como opaco. Los números deben ser números JSON, los booleanos `true` o `false`, y las opciones deben escribirse exactamente como aparecen aquí.

## Destinos de notificación

Escritorio y pantalla completa se seleccionan por separado:

- `Notification...` controla **pantalla completa**.
- `DesktopNotification...` controla **escritorio**.
- `ShowControllerNameInNotifications` controla pantalla completa.
- `ShowControllerNameInDesktopNotifications` controla escritorio.

Para igualarlos, duplica cada propiedad cambiando el prefijo. Para adaptarlos, utiliza anchos, escala, padding o tipografía diferentes.

Al activar un diseño de creador se bloquean y atenúan sus controles editables y el botón de copia hacia el otro destino. Si cualquiera de las dos notificaciones usa un diseño de creador, el editor normal de sonido también queda bloqueado.

## Referencia de propiedades de notificación

En las tablas siguientes, sustituye `{P}` por `Notification` o `DesktopNotification`. **Diseño avanzado** permanece oculto en la interfaz normal, pero todas sus propiedades son compatibles con packs de creadores y perfiles importados.

### Ventana, posición y movimiento

| Propiedad | Tipo o valores | Finalidad |
| --- | --- | --- |
| `{P}Width` | entero `300–900` | Anchura base. |
| `{P}ScalePercent` | entero `80–160` | Escala completa. |
| `{P}DurationMilliseconds` | entero `2000–15000` | Tiempo visible. |
| `{P}Position` | `TopRight`, `TopLeft`, `BottomRight`, `BottomLeft` | Esquina de pantalla. |
| `{P}ScreenMargin` | entero `8–64` | Distancia a los bordes de pantalla. |
| `{P}Animation` | `None`, `Fade`, `FadeScale`, `Slide` | Animación de entrada y salida. |
| `{P}ShowShadow` | booleano | Sombra adaptada al redondeo. |

### Distribución y contenido

| Propiedad | Tipo o valores | Finalidad |
| --- | --- | --- |
| `{P}Padding` | entero `0–40` | Espacio interior; `0` llega hasta el borde. |
| `{P}ElementSpacing` | entero `0–40` | Separación entre textos y elementos. |
| `{P}IconSpacing` | entero `0–40` | Separación exclusiva entre icono y contenido. |
| `{P}IconPosition` | `Left`, `Right`, `Top`, `Bottom` | Posición del icono. |
| `{P}IconSize` | entero `20–96` | Tamaño del icono. |
| `{P}TextAlignment` | `Left`, `Center`, `Right` | Alineación de título y mensaje. |
| `{P}TextOrder` | `TitleFirst`, `MessageFirst` | Orden de los bloques de texto. |
| `{P}MessageMaxLines` | entero `1–6` | Máximo de líneas del mensaje. |
| `{P}ShowTitle` | booleano | Muestra el título del evento. |
| `{P}UppercaseTitle` | booleano | Convierte el título a mayúsculas. |
| `ShowControllerNameInNotifications` / `ShowControllerNameInDesktopNotifications` | booleano | Muestra el nombre detectado. |

### Tipografía

| Propiedad | Tipo o valores | Finalidad |
| --- | --- | --- |
| `{P}FontFamily` | token o cadena | Familia común de respaldo. |
| `{P}FontWeight` | `Regular`, `SemiBold`, `Bold` | Peso común de respaldo. |
| `{P}TitleFontFamily` | token o cadena | Familia del título. |
| `{P}TitleFontWeight` | `Regular`, `SemiBold`, `Bold` | Peso del título. |
| `{P}TitleFontSize` | entero `10–48` | Tamaño del título. |
| `{P}MessageFontFamily` | token o cadena | Familia del mensaje. |
| `{P}MessageFontWeight` | `Regular`, `SemiBold`, `Bold` | Peso del mensaje. |
| `{P}MessageFontSize` | entero `10–36` | Tamaño del mensaje. |

Usa `$font:<Id>` para una fuente del manifest, por ejemplo `"$font:Heading"`. Nunca escribas una ruta de fuentes local.

### Fondo y colores por estado

| Propiedad | Tipo | Finalidad |
| --- | --- | --- |
| `{P}BackgroundColor` | color | Superficie base. |
| `{P}UseGradient` | booleano | Activa el degradado del fondo. |
| `{P}GradientColor` | color | Segundo color del degradado. |
| `{P}GradientAngle` | entero `0–359` | Dirección del degradado. |
| `{P}TextColor` | color | Texto principal. |
| `{P}SecondaryTextColor` | color | Mensaje o texto secundario. |
| `{P}ConnectedColor` | color | Acento de conexión. |
| `{P}DisconnectedColor` | color | Acento de desconexión. |
| `{P}WarningColor` | color | Acento de aviso. |
| `{P}LowBatteryColor` | color | Acento de batería baja. |
| `{P}UseStateBackgroundColors` | booleano | Activa un fondo por evento. |
| `{P}ConnectedBackgroundColor` | color | Fondo de conexión. |
| `{P}DisconnectedBackgroundColor` | color | Fondo de desconexión. |
| `{P}WarningBackgroundColor` | color | Fondo de aviso. |
| `{P}LowBatteryBackgroundColor` | color | Fondo de batería baja. |
| `{P}AccentMode` | `IconAndBorder`, `IconOnly`, `TintedBackground`, `SolidBackground` | Aplicación del acento. |

### Imagen de fondo

| Propiedad | Tipo o valores | Finalidad |
| --- | --- | --- |
| `{P}UseBackgroundImage` | booleano | Activa la imagen. |
| `{P}BackgroundImagePath` | ruta relativa | Imagen dentro del pack. |
| `{P}BackgroundImageStretch` | `UniformToFill`, `Uniform`, `Fill` | Cubrir, contener o estirar. |
| `{P}BackgroundImageHorizontalAlignment` | `Left`, `Center`, `Right` | Punto focal horizontal. |
| `{P}BackgroundImageVerticalAlignment` | `Top`, `Center`, `Bottom` | Punto focal vertical. |
| `{P}BackgroundImageOpacity` | entero `0–100` | Opacidad de imagen. |
| `{P}BackgroundImageTintOpacity` | entero `0–100` | Intensidad del tinte. |

Usa PNG, JPG o JPEG. Las rutas se resuelven desde la carpeta del pack y no pueden salir de ella.

### Contenedor del icono e insignia

| Propiedad | Tipo o valores | Finalidad |
| --- | --- | --- |
| `{P}ShowIconContainer` | booleano | Dibuja una forma tras el icono. |
| `{P}IconContainerColor` | color | Fondo del contenedor. |
| `{P}IconContainerBorderColor` | color | Color del borde. |
| `{P}IconContainerBorderThickness` | entero `0–8` | Grosor del borde. |
| `{P}IconContainerCornerRadius` | entero `0–40` | Redondeo. |
| `{P}IconContainerPadding` | entero `0–24` | Espacio alrededor del icono. |
| `{P}ShowConnectionBadge` | booleano | Muestra el tipo de conexión. |
| `{P}BadgePosition` | `Content`, `Icon`, `Bottom` | Posición de la insignia. |

### Borde, forma y resplandor

| Propiedad | Tipo o valores | Finalidad |
| --- | --- | --- |
| `{P}ShowBorder` | booleano | Activa el borde principal. |
| `{P}BorderPosition` | `Left`, `Top`, `Right`, `Bottom`, `Full` | Posición del borde. |
| `{P}BorderThickness` | entero `0–10` | Grosor principal. |
| `{P}CornerRadius` | entero `0–40` | Redondeo de la superficie. |
| `{P}UseIndependentBorders` | booleano | Activa grosores independientes. |
| `{P}BorderLeftThickness` | entero `0–12` | Borde izquierdo. |
| `{P}BorderTopThickness` | entero `0–12` | Borde superior. |
| `{P}BorderRightThickness` | entero `0–12` | Borde derecho. |
| `{P}BorderBottomThickness` | entero `0–12` | Borde inferior. |
| `{P}UseBorderGradient` | booleano | Activa el degradado del borde. |
| `{P}UseStateBorderColors` | booleano | Activa colores de borde independientes por estado. |
| `{P}ConnectedBorderColor` | color | Borde de conexión. |
| `{P}DisconnectedBorderColor` | color | Borde de desconexión. |
| `{P}WarningBorderColor` | color | Borde de aviso. |
| `{P}LowBatteryBorderColor` | color | Borde de batería baja. |
| `{P}BorderGradientStartColor` | color | Inicio del degradado. |
| `{P}BorderGradientEndColor` | color | Final común del degradado. |
| `{P}BorderGradientAngle` | entero `0–359` | Dirección del degradado. |
| `{P}ShowBorderGlow` | booleano | Activa el resplandor exterior. |
| `{P}BorderGlowColor` | color | Color del resplandor. |
| `{P}BorderGlowBlur` | entero `0–40` | Radio o suavidad. |
| `{P}BorderGlowOpacity` | entero `0–100` | Opacidad. |

Por defecto el borde sigue el acento del evento. Al activar `{P}UseStateBorderColors`, el creador controla el borde sin alterar el icono. Con borde sólido se usa directamente el color del estado; con degradado, `{P}BorderGradientStartColor` es el inicio común y el color del estado es el final. Si se desactiva, todos los estados comparten el inicio y final explícitos.

## Referencia de propiedades del overlay

El overlay es una capa a pantalla completa con una tarjeta configurable. Todas las claves comienzan por `Overlay`. Sus propiedades avanzadas siguen siendo compatibles aunque el bloque **Diseño avanzado** esté oculto.

### Posición, composición y visibilidad

| Propiedad | Tipo o valores | Finalidad |
| --- | --- | --- |
| `OverlayScalePercent` | entero `80–140` | Escala completa. |
| `OverlayCardWidth` | entero `320–1000` | Anchura de tarjeta. |
| `OverlayCardPosition` | `Center`, `Top`, `Bottom`, `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` | Posición. |
| `OverlayScreenMargin` | entero `0–160` | Margen de pantalla. |
| `OverlayLayoutMode` | `Standard`, `Split`, `Hero` | Composición principal. |
| `OverlayContentAlignment` | `Left`, `Center`, `Right` | Alineación del contenido. |
| `OverlayAnimation` | `None`, `Fade`, `FadeScale`, `Slide` | Entrada y salida. |
| `OverlayBlockOrder` | lista separada por comas | Orden de `Title`, `Controller`, `Metadata`, `Instruction`, `Status`, una vez cada uno. |
| `OverlayMetadataOrientation` | `Horizontal`, `Vertical` | Distribución de insignias. |
| `OverlayPadding` | entero `12–80` | Padding interior. |
| `OverlayElementSpacing` | entero `0–48` | Separación entre bloques. |
| `OverlayShowTitle` | booleano | Muestra el título. |
| `OverlayUppercaseTitle` | booleano | Título en mayúsculas. |
| `OverlayShowInstruction` | booleano | Muestra instrucciones. |
| `OverlayShowPauseStatus` | booleano | Muestra el estado de pausa. |
| `OverlayShowControllerName` | booleano | Muestra el nombre del mando. |
| `OverlayShowControllerIcon` | booleano | Muestra su icono. |
| `OverlayShowStatusIcon` | booleano | Muestra el icono de estado. |
| `OverlayShowConnectionBadge` | booleano | Muestra conexión. |
| `OverlayShowBatteryBadge` | booleano | Muestra batería. |
| `OverlayControllerIconPosition` | `Left`, `Center`, `Right`, `Top` | Posición del icono. |
| `OverlayControllerIconSize` | entero `16–96` | Tamaño del icono del mando. |
| `OverlayStatusIconSize` | entero `12–64` | Tamaño del icono de estado. |

```json
{
  "OverlayBlockOrder": "Controller,Title,Metadata,Instruction,Status",
  "OverlayMetadataOrientation": "Vertical",
  "OverlayLayoutMode": "Hero"
}
```

### Superficies, imágenes y colores

| Propiedad | Tipo o valores | Finalidad |
| --- | --- | --- |
| `OverlayDimColor` | color | Capa sobre el juego; el alfa controla su visibilidad. |
| `OverlayCardColor` | color | Superficie de tarjeta. |
| `OverlayUseGradient` | booleano | Activa el degradado. |
| `OverlayGradientColor` | color | Segundo color. |
| `OverlayGradientAngle` | entero `0–359` | Dirección. |
| `OverlayAccentColor` | color | Acento principal. |
| `OverlayTextColor` | color | Texto principal. |
| `OverlayWarningColor` | color | Énfasis de aviso o estado. |
| `OverlayUseBackgroundImage` | booleano | Activa la imagen. |
| `OverlayBackgroundImagePath` | ruta relativa | Imagen dentro del pack. |
| `OverlayBackgroundImageStretch` | `UniformToFill`, `Uniform`, `Fill` | Cubrir, contener o estirar. |
| `OverlayBackgroundImageHorizontalAlignment` | `Left`, `Center`, `Right` | Punto focal horizontal. |
| `OverlayBackgroundImageVerticalAlignment` | `Top`, `Center`, `Bottom` | Punto focal vertical. |
| `OverlayBackgroundImageOpacity` | entero `0–100` | Opacidad. |
| `OverlayBackgroundImageTintOpacity` | entero `0–100` | Intensidad del tinte. |

### Tipografía

| Elemento | Familia | Peso | Tamaño y rango |
| --- | --- | --- | --- |
| Respaldo común | `OverlayFontFamily` | `OverlayFontWeight` | — |
| Título | `OverlayTitleFontFamily` | `OverlayTitleFontWeight` | `OverlayTitleFontSize`, `16–52` |
| Mando | `OverlayControllerFontFamily` | `OverlayControllerFontWeight` | `OverlayControllerFontSize`, `12–36` |
| Instrucción | `OverlayInstructionFontFamily` | `OverlayInstructionFontWeight` | `OverlayInstructionFontSize`, `10–30` |
| Estado | `OverlayStatusFontFamily` | `OverlayStatusFontWeight` | `OverlayStatusFontSize`, `10–28` |

Los pesos son `Regular`, `SemiBold` o `Bold`. Cada familia puede usar `$font:<Id>` de forma independiente.

### Contenedor del mando

| Propiedad | Tipo o rango | Finalidad |
| --- | --- | --- |
| `OverlayShowControllerContainer` | booleano | Activa el contenedor. |
| `OverlayControllerContainerColor` | color | Fondo. |
| `OverlayControllerContainerBorderColor` | color | Borde. |
| `OverlayControllerContainerBorderThickness` | entero `0–8` | Grosor. |
| `OverlayControllerContainerCornerRadius` | entero `0–40` | Redondeo. |
| `OverlayControllerContainerPadding` | entero `0–32` | Padding. |

### Insignias de conexión y batería

| Conexión | Batería | Tipo o rango |
| --- | --- | --- |
| `OverlayConnectionBadgeTextColor` | `OverlayBatteryBadgeTextColor` | color |
| `OverlayConnectionBadgeIconColor` | `OverlayBatteryBadgeIconColor` | color |
| `OverlayConnectionBadgeBackgroundColor` | `OverlayBatteryBadgeBackgroundColor` | color |
| `OverlayConnectionBadgeBorderColor` | `OverlayBatteryBadgeBorderColor` | color |
| `OverlayConnectionBadgeBorderThickness` | `OverlayBatteryBadgeBorderThickness` | entero `0–8` |
| `OverlayConnectionBadgeCornerRadius` | `OverlayBatteryBadgeCornerRadius` | entero `0–32` |
| `OverlayConnectionBadgeIconSize` | `OverlayBatteryBadgeIconSize` | entero `8–40` |
| `OverlayConnectionBadgeTextSize` | `OverlayBatteryBadgeTextSize` | entero `8–28` |

Colores opcionales de batería:

```json
{
  "OverlayBatteryBadgeUseStateColors": true,
  "OverlayBatteryBadgeFullColor": "#FF65D68A",
  "OverlayBatteryBadgeMediumColor": "#FFFFC857",
  "OverlayBatteryBadgeLowColor": "#FFFF8A4C",
  "OverlayBatteryBadgeEmptyColor": "#FFFF4D5A"
}
```

### Borde, forma, sombra y resplandor

- `OverlayShowBorder`: booleano.
- `OverlayBorderPosition`: `Left`, `Top`, `Right`, `Bottom`, `Full`.
- `OverlayBorderThickness`: entero `0–12`.
- `OverlayCornerRadius`: entero `0–40`.
- `OverlayShowShadow`: booleano.
- `OverlayUseIndependentBorders`: booleano.
- `OverlayBorderLeftThickness`, `OverlayBorderTopThickness`, `OverlayBorderRightThickness`, `OverlayBorderBottomThickness`: entero `0–12`.
- `OverlayUseBorderGradient`: booleano.
- `OverlayBorderGradientStartColor`, `OverlayBorderGradientEndColor`: colores.
- `OverlayBorderGradientAngle`: entero `0–359`.
- `OverlayShowBorderGlow`: booleano.
- `OverlayBorderGlowColor`: color.
- `OverlayBorderGlowBlur`: entero `0–40`.
- `OverlayBorderGlowOpacity`: entero `0–100`.

Los bordes independientes permiten líneas superiores o marcos asimétricos. Degradado, sombra y resplandor pueden combinarse. Deja margen suficiente para no recortar un glow grande.

## Fuentes

El registro se realiza por carpetas. `Family` debe coincidir con la familia interna del archivo, no con el nombre del fichero. Varios alias pueden apuntar a la misma carpeta.

```json
"Fonts": [
  { "Id": "Display", "Name": "Mi tema Display", "Family": "Exo 2", "Folder": "Fonts" },
  { "Id": "Body", "Name": "Mi tema Body", "Family": "Inter", "Folder": "Fonts" }
]
```

```json
{
  "NotificationTitleFontFamily": "$font:Display",
  "DesktopNotificationMessageFontFamily": "$font:Body",
  "OverlayTitleFontFamily": "$font:Display"
}
```

El descriptor portable se envía también al proceso externo del overlay y las notificaciones. Incluye todos los `.ttf` o `.otf` necesarios y una licencia de redistribución. Prueba cada peso: seleccionar `Bold` no crea una variante real si el archivo no la contiene.

## Sonidos

`Sounds` acepta exactamente `Connected`, `Disconnected`, `Warning` y `LowBattery`. Se admiten `.wav`, `.mp3` y `.wma`; `.wav` ofrece la reproducción más predecible y de menor latencia.

Cada entrada es opcional. Si falta un audio, se usa el resolvedor normal de Controller Manager. Los clips deben ser cortos, normalizados, sin silencio inicial y con volumen percibido consistente. El volumen global y los interruptores por evento siguen aplicándose.

Si escritorio y pantalla completa usan diseños distintos, cada destino resuelve el audio de su propio pack. El editor permanece bloqueado mientras cualquiera use un diseño de creador.

## Ejemplo completo de `notification.json`

```json
{
  "NotificationWidth": 560,
  "NotificationScalePercent": 105,
  "NotificationBackgroundColor": "#F20C1118",
  "NotificationUseGradient": true,
  "NotificationGradientColor": "#F2182530",
  "NotificationGradientAngle": 135,
  "NotificationTitleFontFamily": "$font:Display",
  "NotificationTitleFontWeight": "SemiBold",
  "NotificationMessageFontFamily": "$font:Body",
  "NotificationIconPosition": "Left",
  "NotificationIconSpacing": 16,
  "NotificationPadding": 20,
  "NotificationShowBorder": true,
  "NotificationBorderPosition": "Full",
  "NotificationBorderThickness": 2,
  "NotificationCornerRadius": 14,
  "NotificationUseBorderGradient": true,
  "NotificationUseStateBorderColors": true,
  "NotificationConnectedBorderColor": "#FF55D68B",
  "NotificationDisconnectedBorderColor": "#FF55B8FF",
  "NotificationWarningBorderColor": "#FFFFC857",
  "NotificationLowBatteryBorderColor": "#FFFF5D6C",
  "NotificationBorderGradientStartColor": "#99FFFFFF",
  "NotificationBorderGradientEndColor": "#FF55B8FF",
  "NotificationBorderGradientAngle": 45,
  "NotificationShowBorderGlow": true,
  "NotificationBorderGlowColor": "#9955B8FF",
  "NotificationBorderGlowBlur": 22,
  "NotificationBorderGlowOpacity": 55,

  "DesktopNotificationWidth": 440,
  "DesktopNotificationScalePercent": 100,
  "DesktopNotificationBackgroundColor": "#F20C1118",
  "DesktopNotificationTitleFontFamily": "$font:Display",
  "DesktopNotificationMessageFontFamily": "$font:Body",
  "DesktopNotificationIconSpacing": 14,
  "DesktopNotificationPadding": 16,
  "DesktopNotificationShowBorder": true,
  "DesktopNotificationBorderPosition": "Top",
  "DesktopNotificationBorderThickness": 2,
  "DesktopNotificationCornerRadius": 8
}
```

## Ejemplo completo de `overlay.json`

```json
{
  "OverlayScalePercent": 105,
  "OverlayDimColor": "#A8000000",
  "OverlayCardColor": "#F20C1118",
  "OverlayCardWidth": 720,
  "OverlayCardPosition": "Center",
  "OverlayLayoutMode": "Hero",
  "OverlayContentAlignment": "Center",
  "OverlayBlockOrder": "Controller,Title,Metadata,Instruction,Status",
  "OverlayMetadataOrientation": "Horizontal",
  "OverlayTitleFontFamily": "$font:Display",
  "OverlayControllerFontFamily": "$font:Body",
  "OverlayInstructionFontFamily": "$font:Body",
  "OverlayStatusFontFamily": "$font:Body",
  "OverlayAccentColor": "#FF55B8FF",
  "OverlayTextColor": "#FFF5F7FA",
  "OverlayShowBorder": true,
  "OverlayBorderThickness": 2,
  "OverlayCornerRadius": 16,
  "OverlayUseBorderGradient": true,
  "OverlayBorderGradientStartColor": "#99FFFFFF",
  "OverlayBorderGradientEndColor": "#FF55B8FF",
  "OverlayBorderGradientAngle": 45,
  "OverlayShowBorderGlow": true,
  "OverlayBorderGlowColor": "#9955B8FF",
  "OverlayBorderGlowBlur": 28,
  "OverlayBorderGlowOpacity": 55
}
```

## Desarrollo y pruebas locales

Compila el plugin con el comando habitual del repositorio y coloca o enlaza la extensión compilada dentro de Playnite. El descubrimiento se realiza al iniciar el plugin; reinicia Playnite después de cambiar un pack.

```powershell
.\tests\run-creator-theme-tests.ps1
.\tests\render-toast-preview.ps1 -Creator my-theme
.\tests\render-overlay-preview.ps1 -Creator my-theme
```

Sustituye `my-theme` por el ID de tu manifest y ejecuta también todas las pruebas antes del pull request.

Prueba como mínimo:

1. Los cuatro estados en escritorio.
2. Los cuatro estados en pantalla completa.
3. El overlay con escalado de Windows al 100 %, 125 % y 150 %.
4. Nombres largos y textos traducidos.
5. Insignias presentes y ausentes.
6. Todas las fuentes y pesos incluidos.
7. Cada sonido con volumen bajo y alto.
8. Imágenes anchas y verticales.
9. Cambios entre Personalizado, presets del plugin, importados y creadores.
10. Reinicio de Playnite conservando el diseño.

## Checklist del pull request

- [ ] `Id` único y estable.
- [ ] JSON estricto, sin comentarios ni comas finales.
- [ ] Solo propiedades documentadas.
- [ ] Todas las rutas son relativas y permanecen dentro del pack.
- [ ] Recursos con créditos y licencias redistribuibles.
- [ ] Fuentes con las variantes necesarias para cada peso.
- [ ] Sonidos cortos, normalizados y redistribuibles.
- [ ] Capturas de todas las superficies compatibles.
- [ ] Lectura correcta sobre contenido claro y oscuro.
- [ ] Textos largos y traducidos sin recortes.
- [ ] Glow y sombra sin recortes con los márgenes previstos.
- [ ] Pruebas de packs de creadores superadas.

## Solución de problemas

### El diseño no aparece

Comprueba la carpeta `CreatorThemes`, los campos obligatorios del manifest y que al menos un JSON contenga un objeto. Recompila y reinicia Playnite.

### Una propiedad no tiene efecto

Verifica el nombre y el tipo JSON. `"105"` no equivale al número `105`. Confirma también el destino: `Notification...` es pantalla completa y `DesktopNotification...` es escritorio.

### Una fuente usa el fallback

Comprueba la familia interna, el alias, la carpeta y los archivos. Usa `$font:Id`, nunca una ruta local, y verifica que exista el peso solicitado.

### Falta una imagen o sonido

Usa una ruta relativa, una extensión compatible y confirma que la ruta no salga del pack. Evita nombres que solo se diferencien en mayúsculas.

### El brillo no coincide exactamente con el tema de Playnite

Los packs configuran primitivas WPF de Controller Manager. No pueden importar controles, shaders, storyboards ni diccionarios de recursos arbitrarios. Reproduce el lenguaje visual mediante degradados, color, blur, opacidad, imágenes y sombras.

### Los controles están desactivados

Es intencionado. Selecciona **Personalizado**, un preset del plugin o un diseño importado para editar. Los packs se bloquean y atenúan para conservar su composición y audio.

## Compatibilidad y seguridad

Los packs contienen datos, no código ejecutable. No añadas DLL, scripts ni descargas externas. Las versiones futuras pueden incorporar propiedades, pero las claves documentadas existentes deben conservar compatibilidad. Si alguna se reemplaza, el plugin debe migrarla o mantenerla el tiempo necesario.

Los mantenedores pueden ajustar un diseño por legibilidad, seguridad, rendimiento o compatibilidad. Las adaptaciones de temas de terceros deben acreditar a sus autores y no pueden insinuar su respaldo.
