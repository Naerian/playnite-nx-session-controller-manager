# Sistema de overlay

> Estado de implementación 0.5.9: el host externo gestiona el overlay, notificaciones Fullscreen en cola y una concesión opcional de pausa forzada para juegos offline. Usa pipe local autenticado, ACL del usuario, ventanas click-through/no-activate, selección de monitor, heartbeat y recuperación automática. Escala, posición, tipografía, iconos, padding, borde, radio de esquina, backdrop y paleta se reciben como datos validados; nunca se carga XAML ni código de terceros. Las notificaciones convierten DIP a píxeles según el DPI real, filtran durante 300 ms los cambios transitorios de XInput y los fallos de arranque o IPC quedan contenidos sin atravesar el callback de Playnite.

## 1. Decisión: proceso WPF independiente

Opciones:

| Opción | Pros | Contras | Decisión |
|---|---|---|---|
| Ventana dentro del proceso Playnite | Simple; comparte modelos | Un fallo afecta Playnite; lifecycle/foco más acoplado | No para gameplay |
| Proceso WPF independiente | Aislamiento, top-level window propia, reinicio y DPI controlables | IPC y binario extra | Elegida |
| Overlay DirectX/hook in-process del juego | Puede cubrir más modos exclusivos | Anti-cheat, estabilidad y complejidad inaceptables | Rechazada |

El overlay no inyecta DLLs, no hookea DirectX y no depende de que Playnite esté visible.

## 2. Componentes

```text
Plugin process                          OverlayHost.exe
OverlayManager                         IpcServer
  ├─ OverlayClient <--- named pipe ---> OverlayStateStore
  ├─ GameWindowLocator                 ThemeLoader
  └─ Incident coordinator              OverlayWindow per monitor
                                            └─ stable ViewModel API
```

El plugin inicia el host bajo el mismo usuario/integrity. IPC local mediante named pipe con ACL del usuario actual, mensajes con longitud máxima, versión y nonce de sesión. Ningún XAML cruza IPC; sólo DTOs validados.

## 3. Protocolo IPC CSM3

Envelope:

```json
{
  "protocolVersion": 3,
  "messageId": "guid",
  "sessionId": "guid",
  "type": "ShowDisconnect | Toast | HideAll | Heartbeat | Shutdown",
  "payload": { "pauseStatus": "text", "forcePause": false, "verifiedProcessId": 0 }
}
```

Comandos idempotentes:

- `Initialize(themeId, locale, accessibility, apiVersion)`;
- `ShowDisconnect(incidentId, controllers[], targetMonitor)`;
- `UpdateControllers(incidentId, controllers[])`;
- `ShowNotification(notificationId, kind, expiresAt)`;
- `Hide(incidentId)`;
- `HideAll(sessionId)`;
- `Shutdown`.

Eventos: `Ready`, `Shown`, `Hidden`, `ThemeRejected`, `Fault`, `Heartbeat`. Un comando de una sesión antigua se descarta. Al perder IPC, el host oculta todo tras un timeout corto configurable; no queda una pantalla huérfana.

## 4. Ventana

Configuración base:

- `WindowStyle=None`;
- `ShowInTaskbar=False`;
- `Topmost=True`, reforzado de forma prudente con `SetWindowPos(HWND_TOPMOST)` al mostrar;
- posición/tamaño iguales a bounds del monitor objetivo;
- `ShowActivated=False` y `WS_EX_NOACTIVATE` por defecto para no robar foco;
- transparencia sólo si el theme la requiere;
- modos DPI per-monitor aware;
- una ventana por monitor sólo cuando sea necesaria.

La documentación WPF confirma topmost, ventana sin borde, transparencia y exclusión de taskbar. La selección del monitor usa primero la ventana validada del juego (`MonitorFromWindow`), después el monitor con mayor intersección y finalmente el primario/override.

## 5. Compatibilidad fullscreen

| Modo | Expectativa | Política |
|---|---|---|
| Windowed | Alta | Topmost normal |
| Borderless fullscreen | Alta | DWM compone el overlay; probar flip/MPO/HDR |
| Fullscreen optimizations / independent flip | Media-alta | La aparición de contenido superior puede forzar composición o MPO |
| Exclusive fullscreen legado | No garantizada | Overlay puede quedar detrás; documentar limitación y sugerir borderless |
| Juegos elevados | Variable | CSM no se eleva automáticamente; input/orden z pueden estar restringidos |
| Anti-cheat | Evitar técnicas invasivas | Sólo ventana normal; sin hooks/injection |

No se prometerá «funciona sobre cualquier fullscreen». Microsoft documenta que flip/independent flip puede saltarse composición y volver a composición cuando aparece contenido por encima; la conducta depende del presentation path y hardware.

## 6. Foco e input

Por defecto el overlay es informativo y click-through/no-activate: el juego conserva foco. Bloquear input es una función separada, opt-in y fuera del primer MVP, porque:

- un overlay WPF normal no bloquea de forma fiable APIs de input raw/XInput/GameInput del juego;
- capturar teclado/mouse no captura necesariamente el mando;
- hooks/global filters aumentan riesgo y conflictos.

CSM no presenta el overlay como una barrera universal: una ventana externa puede capturar ratón/teclado al tomar foco, pero no puede impedir de forma fiable el sondeo XInput, Raw Input, Steam Input o GameInput configurado para segundo plano. La protección se basa en la pausa segura y en resolver rápidamente la asignación del mando, sin drivers, hooks ni dispositivos virtuales.

En 0.4.1, el relevo no se completa mientras el mando alternativo conserve una entrada activa. El proveedor publica neutralidad y su inicio; la sesión exige 200 ms continuos en reposo y acelera temporalmente el sondeo de 250 a 100 ms. SDL compara cada eje contra la línea base tomada al conectar para descartar retornos a reposo durante el apagado. La geometría del mando aparece en una fila compacta junto a su nombre.

En 0.4.2, el tiempo de reposo baja a 100 ms. El botón canónico Guide/PS/Home queda excluido de la participación y el umbral de stick se ajusta a 8.000 unidades, de modo que el relevo responda a un movimiento normal sin aceptar drift respecto a la línea base.

En 0.4.3, el sondeo funciona a 50 ms durante la partida para capturar incluso movimientos breves antes de que el mando vuelva a reposo. Fuera de una sesión permanece en 250 ms.

La navegación del overlay sólo se habilitará si se diseña un modo interactivo explícito. Nunca se registrarán hotkeys o Raw Input globales desde themes.

La pausa por tecla de 0.3.0 sigue verificando que la ventana foreground pertenece al PID iniciado por Playnite o a su árbol de descendientes, vuelve a comprobar HWND/PID y envía exactamente un par down/up mediante `SendInput`.

En 0.3.1 el protocolo compacto `CSM2` añade el tipo y la geometría del estado. El host renderiza una etiqueta con `player-pause.svg` para pausa solicitada/desactivada y `alert-triangle.svg` para comprobaciones omitidas o fallos. El título, los nombres y la instrucción tienen jerarquía independiente y variantes singulares/plurales. El plugin puede enviar una tecla simple configurada, manteniendo una sola tentativa por incidencia.

En 0.5.0 el modo avanzado de pausa forzada es independiente de la pausa por tecla y está desactivado por defecto. El plugin valida primero el proceso foreground. Después busca evidencia de sesión online: metadatos fuertes de juego exclusivamente online o conexiones TCP públicas establecidas pertenecientes al árbol del juego. Con evidencia online se muestra un toast y no se suspende nada. Sin ella, el host abre el proceso validado con el permiso mínimo de suspensión y mantiene una concesión idempotente mediante `NtSuspendProcess`/`NtResumeProcess`.

La concesión se libera al resolver la incidencia, terminar el juego, recibir `Shutdown`, morir Playnite o perder el heartbeat durante ocho segundos. El cliente nunca mata el host mientras cree que puede existir una concesión activa; si el cierre ordenado falla, deja actuar al watchdog. Esta medida prima la recuperación del juego sobre la limpieza inmediata del proceso auxiliar.

La detección online es deliberadamente de mejor esfuerzo. Una partida UDP, un servicio separado o tráfico encapsulado puede no detectarse; telemetría TCP puede causar un fallback conservador. Por eso el modo sigue siendo opt-in y debe probarse por juego.

La paleta predeterminada no se obtiene del tema de Playnite: usa fondo oscuro translúcido, contenido blanco, azul para el borde y los estados informativos, y ámbar para advertencias. El host es un proceso WPF aislado y no carga diccionarios XAML de terceros en tiempo de ejecución. Esta decisión evita dependencias visuales y mantiene contraste estable sobre el juego, con independencia de que la sesión se iniciara desde Desktop o Fullscreen.

## 7. Modelo visual estable

`OverlayViewModel` API v1 (sólo lectura):

```text
ApiVersion: 1
Kind: Disconnect | Reconnect | BatteryLow | BatteryCritical | Connected
Title / Message / Instruction
IsBlockingIncident
PrimaryController: OverlayControllerViewModel?
Controllers: ReadOnlyObservableCollection<OverlayControllerViewModel>
PlayerLabel
CanDismiss / CanTakeOver
Progress: grace/reconnect state, never battery unless labelled
```

`OverlayControllerViewModel`:

```text
Id (ephemeral presentation id)
Name, Family, IsConnected, IsActive, PlayerIndex
ConnectionType
BatteryAvailability, BatteryLevelKind, BatteryPercent?, BatteryDiscreteLevel
BatteryState, IconKey, ConnectionIconKey
LastInputAge (coalesced, not updated every millisecond)
```

Commands v1 son limitados: `DismissCommand` y `AcceptTakeoverCommand`, y pueden ser `null`. No se expone `Pause`, procesos, filesystem, services ni objetos del plugin.

## 8. Formato de theme

```text
OverlayThemes/
  Default/
    theme.yaml
    Overlay.xaml
    Resources.xaml
    Assets/
      controller-generic.png
      battery-low.png
```

Manifiesto:

```yaml
Id: Default
Name: Default
Author: Controller Session Manager
Version: 1.0.0
ControllerSessionManagerApiVersion: 1
EntryPoint: Overlay.xaml
Resources: Resources.xaml
MinHostVersion: 0.3.1
```

Las claves son case-sensitive; `Id` sólo admite ASCII seguro. Se rechazan rutas absolutas, `..`, URI de red y archivos fuera del directorio del theme.

## 9. Carga segura de XAML

XAML WPF no es una frontera de seguridad fuerte. Un theme instalado es código/contenido local confiable en cierto grado. Para reducir superficie:

- no usar `XamlReader` sobre assemblies/tipos arbitrarios sin validación;
- allowlist de namespaces y tipos visuales WPF simples;
- rechazar `x:Class`, `ObjectDataProvider`, code-behind, event handlers, `ResourceDictionary Source` remoto, pack URI externo y tipos custom no suministrados;
- resolver assets dentro de la carpeta canónica del theme;
- tamaño máximo de XAML/assets y profundidad limitada;
- cargar en el proceso overlay, nunca en Playnite;
- fallback atómico al theme Default si validación/carga falla.

Se realizará un spike para decidir entre (A) parser XAML allowlisted, flexible pero complejo, y (B) themes sólo como ResourceDictionary + templates predefinidos, más seguro. Recomendación inicial: **B para API v1**. Permite colores, tipografía, iconos, templates y animaciones declarativas sin un árbol completamente arbitrario.

## 10. Versionado

- `ControllerSessionManagerApiVersion`: contrato de ViewModel, commands, resource keys y manifiesto.
- Major incompatible requiere nueva versión; el host puede mantener adaptadores para N y N−1.
- Campos nuevos opcionales no rompen la versión.
- Cada theme declara una sola major.
- El loader valida antes de reemplazar el theme activo.

## 11. Recursos y sustitución

Resource keys v1, con fallback interno:

```text
CSM.Overlay.BackgroundBrush
CSM.Overlay.CardStyle
CSM.Overlay.TitleStyle
CSM.Overlay.MessageStyle
CSM.Icon.Controller.{Family}
CSM.Icon.Connection.{Type}
CSM.Icon.Battery.{State}
CSM.Animation.Enter / Exit
```

Los assets pueden ser PNG/WebP compatible con el runtime o geometrías XAML allowlisted. Se fijarán límites de resolución/memoria para evitar imágenes descomprimidas enormes.

## 12. Notificaciones y anti-spam

`NotificationCoordinator` deduplica por `(ControllerId, Kind, threshold generation)`:

- low: una vez al cruzar umbral; se rearma al subir por encima de umbral + histéresis;
- critical: igual y con prioridad superior;
- charging/connected: opcional, cooldown configurable;
- disconnect de sesión: modal persistente hasta resolución, no timeout normal.

Las notificaciones transitorias se encolan. Desde 0.5.9 permiten previsualizar de inmediato los estados conectado, desconectado y aviso, y colocar el icono a la izquierda, derecha, arriba, abajo u ocultarlo. También admiten texto multilínea, ancho, escala, duración, una de las cuatro esquinas, fondo, textos y acentos semánticos. Mantienen el icono asignado y nombre personalizado en una ventana topmost click-through/no-activate. Las notificaciones normales de conexión se limitan a la interfaz Fullscreen sin juego activo; el fallback online puede avisar durante la partida.

El overlay de desconexión permite configurar por separado escala, oscurecimiento de pantalla, fondo de tarjeta, texto, acento principal y aviso. El host sólo acepta colores hexadecimales y límites numéricos conocidos enviados por el plugin. Esta personalización es de datos, no un cargador de themes ni una vía para ejecutar contenido externo.

## 13. Failsafes

- `HideAll` en `OnGameStopped`, disable, theme switch y shutdown.
- Watchdog del host: oculta si pierde pipe/heartbeat.
- Cierre ordenado `SHUTDOWN`; el cliente sólo mata su host si no existe una concesión de suspensión activa.
- La concesión de suspensión se libera antes de ocultar, cerrar o expirar por watchdog.
- Fallback Default ante cualquier excepción del theme.
- Crash loop: máximo un reinicio por sesión.
- El host sólo reanuda el proceso que él mismo suspendió y una única vez por concesión.

## 14. Validación visual

Pruebas obligatorias por release:

- 1920×1080, 2560×1440, 4K; DPI 100/125/150/200 %;
- monitor primario/secundario, coordenadas negativas y distintas escalas;
- SDR/HDR;
- windowed/borderless y muestra de exclusive legacy;
- textos ES/EN largos y nombres de mando extensos;
- screen reader/contraste/reduced motion;
- cambio/desconexión de monitor durante overlay.

## 15. Fuentes primarias

- [WPF windows: topmost, transparencia y taskbar](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/windows/)
- [DXGI flip model](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-flip-model)
- [DirectFlip e independent flip](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model)
- [SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos)
- [MonitorFromWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfromwindow)
