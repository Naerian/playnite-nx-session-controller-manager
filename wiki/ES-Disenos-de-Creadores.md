# Diseños de creadores

Controller Manager admite packs revisados para personalizar notificaciones de escritorio y pantalla completa, el overlay, imágenes, fuentes y sonidos. Son packs de datos: no pueden ejecutar código ni XAML arbitrario.

La tabla exhaustiva de propiedades se mantiene en la [referencia canónica para creadores](https://github.com/Naerian/playnite-nx-session-controller-manager/blob/master/docs/CREATOR-THEMES.md).

## Estructura

```text
CreatorThemes/
  MiPack/
    manifest.json
    notification.json       opcional
    overlay.json            opcional
    Images/                 opcional
    Fonts/                  opcional
    Audio/                  opcional
```

Debe existir al menos un JSON de apariencia con propiedades compatibles. Todos los recursos deben permanecer dentro de la carpeta del pack.

## Manifest

```json
{
  "Id": "autor.tema.variante",
  "Name": "Nombre visible",
  "Author": "Autor",
  "Version": "1.0.0",
  "Description": "Descripción breve",
  "RecommendedTheme": "Nombre del tema",
  "DesktopThemeIds": ["id-del-tema-desktop"],
  "FullscreenThemeIds": ["id-del-tema-fullscreen"],
  "Fonts": [
    { "Id": "Heading", "Name": "Título", "Family": "Familia real", "Folder": "Fonts" }
  ],
  "Sounds": {
    "Connected": "Audio/connected.wav",
    "Disconnected": "Audio/disconnected.wav",
    "LowBattery": "Audio/low-battery.wav",
    "Warning": "Audio/warning.wav"
  }
}
```

`Id`, `Name` y `Author` son obligatorios. El ID debe ser único y estable. Copia los identificadores compatibles desde el campo `Id` del `theme.yaml` de Playnite. El filtro **Mostrar solo los del tema actual** utiliza `DesktopThemeIds` y `FullscreenThemeIds`; el overlay acepta una coincidencia de cualquiera de los dos modos.

## Notificaciones

Pantalla completa usa el prefijo `Notification`; escritorio usa `DesktopNotification`. Un mismo archivo puede definir ambos destinos.

```json
{
  "NotificationWidth": 560,
  "NotificationBackgroundColor": "#F20D121A",
  "NotificationTextColor": "#FFFFFFFF",
  "NotificationTitleFontFamily": "$font:Heading",
  "NotificationUseGradient": true,
  "NotificationGradientColor": "#F21A2633",
  "NotificationShowBorder": true,
  "NotificationUseBorderGradient": true,
  "NotificationBorderGradientStartColor": "#99FFFFFF",
  "NotificationBorderGradientEndColor": "#FF55B8FF",
  "NotificationShowBorderGlow": true,
  "NotificationBorderGlowColor": "#9955B8FF"
}
```

Los creadores pueden independizar el borde del color del icono y definirlo por estado:

```json
{
  "NotificationUseStateBorderColors": true,
  "NotificationConnectedBorderColor": "#FF55D68B",
  "NotificationDisconnectedBorderColor": "#FF55B8FF",
  "NotificationWarningBorderColor": "#FFFFC857",
  "NotificationLowBatteryBorderColor": "#FFFF5D6C"
}
```

Un borde sólido usa directamente el color del estado. En un degradado, `BorderGradientStartColor` es el inicio común y el color del estado es el final. Para escritorio se usan los mismos sufijos con `DesktopNotification`.

Los packs pueden controlar tamaño, escala, duración, posición, animación, icono, contenedor, padding, separación, orden y alineación, contenido, fuentes independientes, imagen y recorte, fondos por estado, redondeo, grosor por cada lado, degradado y resplandor.

Aunque **Diseño avanzado** esté oculto para el usuario normal, sus propiedades siguen disponibles para JSON de creadores y perfiles importados.

## Overlay

Las propiedades empiezan por `Overlay`:

```json
{
  "OverlayScalePercent": 110,
  "OverlayCardWidth": 720,
  "OverlayCardPosition": "Center",
  "OverlayLayoutMode": "Hero",
  "OverlayCardColor": "#EE0D121A",
  "OverlayUseGradient": true,
  "OverlayGradientColor": "#EE182737",
  "OverlayAccentColor": "#FF55B8FF",
  "OverlayUseBorderGradient": true,
  "OverlayBorderGradientStartColor": "#99FFFFFF",
  "OverlayBorderGradientEndColor": "#FF55B8FF",
  "OverlayShowBorderGlow": true
}
```

El pack puede controlar tarjeta y fondo de pantalla, imágenes, distribución y orden de bloques, posición, márgenes, animación, sombras, bordes por lado, degradado, brillo, contenedor e icono del mando, tipografías y las insignias de conexión y batería con sus estados.

## Recursos y sonidos

- Los colores usan `#AARRGGBB`; el alfa va primero.
- Las imágenes usan rutas relativas como `Images/background.webp`.
- Las fuentes del manifest se referencian mediante `$font:Id`.
- `Family` debe ser el nombre interno real de la familia tipográfica.
- Los sonidos se declaran en el manifest; WAV corto y normalizado es la opción más segura.
- No se permiten rutas absolutas, recursos externos a la carpeta, código ni XAML arbitrario.
- Todos los recursos deben permitir su redistribución y conservar los créditos necesarios.

## Flujo recomendado

1. Haz un fork del repositorio.
2. Crea el pack bajo `CreatorThemes`.
3. Añade el manifest y al menos un JSON de apariencia.
4. Previsualiza conectado, desconectado, aviso y batería baja.
5. Prueba escritorio y pantalla completa por separado.
6. Prueba el overlay con escalado de Windows al 100%, 125% y 150%.
7. Comprueba nombres largos, ausencia de batería y transparencias.
8. Verifica rutas, licencias y atribuciones.
9. Envía un pull request con capturas e IDs compatibles.

Los packs incluidos de Aniki ReMake y Helium sirven como ejemplos reales. Consulta la [referencia exhaustiva](https://github.com/Naerian/playnite-nx-session-controller-manager/blob/master/docs/CREATOR-THEMES.md) para conocer todos los nombres, tipos y rangos.
