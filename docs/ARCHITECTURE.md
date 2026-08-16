# Controller Session Manager — Arquitectura

## Límite de seguridad nativa en Fullscreen (0.5.7)

El proceso Fullscreen de Playnite nunca inicializa ni llama a SDL desde esta extensión. XInput y los eventos de mandos de Playnite se mantienen dentro del proceso; el enriquecimiento SDL de nombres, batería e input no XInput queda limitado a Desktop. Cualquier muestreo SDL futuro para Fullscreen deberá ejecutarse en un proceso auxiliar descartable, de modo que un fallo nativo del driver no pueda terminar Playnite.

Desde 0.5.8, Desktop persiste la última asociación entre el perfil físico y el slot XInput. El proceso Fullscreen reutiliza esa asociación para el nombre, alias e icono, pero no la considera una identificación criptográfica ni permanente: si Windows reasigna slots, Desktop corrige el vínculo en el siguiente arranque o refresco.

> Estado: propuesta previa a implementación  
> Fecha de revisión: 2026-08-14  
> Alcance inicial: Playnite 10.x, Windows 10 1903 o posterior, proceso x64

## 1. Objetivos y no objetivos

Controller Session Manager (CSM) mantiene un inventario normalizado de mandos, determina cuáles participan en una sesión de juego y aplica una política segura cuando uno de ellos desaparece. La prioridad es conservar el estado del juego y evitar falsos positivos.

El núcleo no presupone que «hay un mando conectado» equivale a «el mando del jugador sigue conectado». Tampoco presupone que VID/PID, un índice XInput o un nombre sean una identidad física estable.

Fuera del MVP quedan la interceptación exclusiva del input, la emulación de dispositivos, la lectura de protocolos propietarios no documentados y la suspensión de procesos. Estas capacidades sólo podrán añadirse tras prototipos aislados y revisión de seguridad.

## 2. Decisiones estructurales

### ADR-001: GenericPlugin del SDK de Playnite

Opciones:

| Opción | Ventajas | Inconvenientes | Decisión |
|---|---|---|---|
| Script PowerShell | Prototipo rápido | Sin UI personalizada; Playnite 11 eliminará su soporte | Rechazada |
| `GenericPlugin` C# | Eventos de juego, settings, controles WPF y ciclo de vida oficial | Requiere .NET Framework compatible con Playnite | Elegida |
| Proceso externo completo | Aislamiento | Despliegue y coordinación más complejos; pierde integración directa | Sólo posible auxiliar futuro |

Playnite documenta plugins en lenguajes compatibles con .NET Framework y actualmente señala `net462`; la versión exacta del SDK se fijará al crear el proyecto y se validará con Playnite 10 y la guía de migración de Playnite 11. No se copiarán ensamblados que Playnite ya cargue, para evitar conflictos de versiones.

### ADR-002: GameInput como proveedor primario, Playnite como señal complementaria

GameInput actual proporciona enumeración y callbacks de dispositivo, lecturas con timestamps, identidad local, `containerId`, VID/PID, ruta PnP, capacidades y feedback. Playnite proporciona eventos de controlador y `GetConnectedControllers`, pero su modelo público sólo confirma `InstanceId`, `Path`, `Name` y `Enabled`.

La fuente autoritativa del registro propio será GameInput. `PlayniteControllerBridge` enriquecerá/corroborará eventos y permitirá funcionamiento degradado si GameInput no está instalado. XInput será un proveedor auxiliar para slots 0–3 y batería discreta, no una segunda lista visible.

### ADR-003: separación entre observaciones y dispositivos lógicos

Cada API produce una `ControllerObservation`. El `ControllerIdentityResolver` agrupa observaciones en un solo `ControllerDevice`. Una observación nunca se expone directamente a la UI. Esto evita que un DualSense físico aparezca dos veces por estar visible mediante GameInput, Raw Input y un wrapper XInput.

### ADR-004: máquina de estados de sesión explícita

La protección usa estados y comandos idempotentes, no una cadena de handlers:

```text
Idle -> Observing -> SuspectDisconnect -> Protected -> Recovering -> Observing
                     | reconnects          | session stops
                     +----> Observing       +----> Idle (failsafe cleanup)
```

Cada transición lleva `SessionId` y `CancellationToken`. Los resultados tardíos de una sesión anterior se ignoran.

## 3. Capas y dependencias

```text
PlaynitePlugin (composition root)
  ├─ PlayniteIntegration
  │    ├─ lifecycle/settings
  │    └─ custom UI element factories
  ├─ Application
  │    ├─ ControllerManager
  │    ├─ GameSessionManager
  │    ├─ ActiveControllerTracker
  │    ├─ BatteryManager
  │    ├─ GamePauseManager
  │    └─ NotificationCoordinator
  ├─ Domain
  │    ├─ ControllerDevice / observations
  │    ├─ session state machine
  │    └─ policies and value objects
  ├─ Infrastructure.Windows
  │    ├─ GameInput / XInput / RawInput-HID metadata
  │    ├─ input injection
  │    ├─ windows/process/monitor discovery
  │    └─ external overlay host
  └─ Presentation
       ├─ Playnite custom controls
       └─ Overlay protocol, view models and themes
```

Reglas:

- `Domain` no referencia Playnite, WPF ni P/Invoke.
- Los proveedores emiten snapshots inmutables o deltas; no modifican el registro.
- La UI observa snapshots publicados por `IControllerStateStore`.
- Toda API nativa queda detrás de una interfaz pequeña y se libera de forma determinista.
- Sólo el composition root conoce implementaciones concretas.

## 4. Contratos principales

```csharp
public interface IControllerProvider : IDisposable
{
    string ProviderId { get; }
    ControllerProviderCapabilities Capabilities { get; }
    Task StartAsync(IControllerObservationSink sink, CancellationToken token);
    Task StopAsync(CancellationToken token);
}

public interface IControllerObservationSink
{
    void PublishDevice(ControllerObservation observation);
    void PublishInput(ControllerInputObservation observation);
    void PublishRemoval(ProviderDeviceKey key, long timestampUs);
}

public interface IControllerIdentityResolver
{
    IdentityResolution Resolve(
        ControllerObservation observation,
        IReadOnlyCollection<ControllerDevice> candidates);
}
```

`IdentityResolution` contiene `Match`, `Confidence`, `Evidence[]` y `Conflicts[]`. Una coincidencia ambigua se mantiene separada; es preferible un duplicado diagnosticable a fusionar dos mandos físicos del mismo modelo.

## 5. Modelo `ControllerDevice`

```text
ControllerDevice
  Id: ControllerId                         // interno, opaco y persistible
  Presence: Present | Missing | Unknown
  DisplayName: SourcedValue<string>
  Family: Xbox | DualSense | DualShock4 | SwitchPro | JoyCon | Generic | Unknown
  IsVirtual: bool?                         // null = no determinado
  Connection: Usb | Bluetooth | WirelessAdapter | Virtual | Unknown
  VendorId/ProductId/Revision: ushort?
  ContainerId: Guid?
  StableHardwareKey: string?               // nunca se muestra ni registra completo por defecto
  PlayerIndex: int?                        // asignación dinámica, no identidad
  Capabilities: ControllerCapabilities
  Battery: BatteryInfo
  LastInputAt: MonotonicTimestamp?
  LastSeenAt: MonotonicTimestamp
  Observations: ProviderBinding[]
```

Principios del modelo:

- Todo dato incierto es nullable o `Unknown`.
- Cada campo enriquecido conserva `Source`, `Confidence` y `ObservedAt`.
- `PlayerIndex` puede cambiar después de reconectar.
- `ControllerId` no se deriva sólo de VID/PID ni del nombre.
- Los timestamps para intervalos son monotónicos; UTC sólo se usa en logs.

### `BatteryInfo`

```text
Availability: Available | Unavailable | Unknown
LevelKind: ExactPercent | Discrete | None
Percent: byte?                  // sólo cuando la fuente afirma porcentaje
DiscreteLevel: Empty | Low | Medium | Full | Unknown
PowerState: Charging | Discharging | Idle | Wired | Unknown
Source: XInput | HidFeature | BluetoothProperty | Vendor | Unknown
ObservedAt / StaleAfter
```

No se convierte `Low` en un porcentaje. La UI puede representar una barra cualitativa, pero debe indicar `LevelKind=Discrete`.

## 6. `ControllerManager`

Responsabilidades:

1. Arrancar/parar proveedores en orden.
2. Serializar observaciones en un único event loop (canal/cola) para evitar locks distribuidos.
3. Resolver identidad y mantener bindings por proveedor.
4. Publicar un snapshot sólo cuando cambie el estado semántico.
5. Conservar tombstones breves para correlacionar reconexiones.
6. Emitir diagnóstico estructurado sin datos sensibles innecesarios.

No decide qué mando es activo ni ejecuta protección; esas tareas pertenecen a sesión.

## 7. `ActiveControllerTracker`

El tracker empieza al recibir `OnGameStarted`, con una ventana de armado configurable (por defecto 8 s). Calcula actividad a partir de transiciones significativas, no de ruido analógico:

- botones: cambio press/release;
- triggers: cruce de umbral con histéresis;
- sticks: delta fuera de deadzone y por encima de magnitud mínima;
- timestamps repetidos o estados neutrales no cuentan.

Estados por dispositivo: `Candidate`, `Active`, `RecentlyActive`, `Inactive`, `Missing`. La actividad reciente usa una puntuación con decaimiento, pero la pertenencia a sesión es pegajosa: un mando que ya jugó no deja de ser protegido sólo por quedar quieto.

Política inicial:

- Un jugador: el primer dispositivo con input significativo después del inicio se convierte en primario.
- Multijugador automático: cada dispositivo distinto con actividad significativa entra en `ActiveControllers`, hasta el máximo configurado.
- Cambio de mando: requiere actividad significativa del nuevo mando y una ventana configurable; no elimina automáticamente al anterior en modo multijugador.
- Antes de observar input: no se dispara protección por mandos que desaparezcan, salvo que el usuario haya fijado un mando preferido.
- Controles virtuales: se incluyen si son los que realmente generan input; la exclusión es una política configurable, no una suposición.

## 8. Protección de desconexión

Al desaparecer un binding, el registro sólo marca el dispositivo `Missing` cuando desaparecen todas sus observaciones autoritativas. Si pertenecía a la sesión:

1. Crear `DisconnectIncident` con identidad esperada.
2. Iniciar grace period (por defecto propuesto: 1500 ms).
3. Cancelar si reaparece una identidad con coincidencia fuerte.
4. Al vencer, reevaluar sesión, juego y política.
5. Ejecutar pausa segura y overlay de forma independiente; el fallo de uno no impide el otro.
6. Al reconectar, ocultar overlay y reanudar sólo si CSM confirmó que él mismo pausó y la política lo permite.

Desde 0.4.0 la sustitución no es una preferencia separada. En automático, un input intencionado transfiere la propiedad al mando disponible; en multijugador local, sólo un dispositivo que no pertenezca ya a otro participante puede ocupar la plaza ausente.

Desde 0.5.1, `AdaptiveSessionScopeDetector` observa cambios únicos de `LastInputUtc`, compacta repeticiones inferiores a 180 ms y mantiene una ventana móvil de veinte segundos. Dos participantes con al menos dos muestras cada uno y tres transiciones promocionan la sesión automática a multijugador local hasta terminar el juego. Esto evita pedir clasificación al usuario sin confundir un cambio aislado de mando con cooperativo.

Desde 0.4.1, la evidencia de input conserva su clase (`DigitalButton`, `Trigger`, `Stick`, `DirectionalPad` o fallback de Playnite). SDL evalúa el desplazamiento respecto a una línea base capturada al abrir el dispositivo, no sólo el delta entre dos lecturas, para que un eje que vuelve a su valor inicial al apagarse no active al mando. Una sustitución exige además neutralidad continua durante 200 ms; mientras exista una incidencia, XInput/SDL se sondean cada 100 ms en lugar de 250 ms.

En 0.4.2, SDL usa la asignación canónica de botones y excluye Guide/PS/Home: pulsarlo para apagar el mando no implica que ese mando participase en el juego. Los sticks se reconocen desde 8.000 unidades respecto a la línea base y el reposo previo al relevo se reduce a 100 ms.

En 0.4.3, el proveedor se sondea cada 50 ms mientras una sesión está activa. Esto impide que un movimiento breve del stick empiece y vuelva a reposo entre dos muestras de inventario. Fuera de una partida se conservan 250 ms para reducir trabajo innecesario.

La implementación distingue dos alcances. En `MostRecent`, sólo el mando con input intencionado más reciente permanece protegido y una sustitución puede ocurrir desde que empieza el margen de desconexión. En `AllActive`, cada mando usado representa un participante local independiente: un mando que ya pertenece a la sesión no puede sustituir a otro participante desaparecido, pero uno nuevo/no asignado sí. Un mando retirado conserva un suelo de activación y no puede volver a entrar hasta superar su último timestamp conocido con input nuevo.

## 9. Pausa y seguridad

Interfaz:

```csharp
public interface IGamePauseStrategy
{
    string Id { get; }
    PauseRisk Risk { get; }
    Task<PauseReceipt> TryPauseAsync(GameTarget target, CancellationToken token);
    Task<ResumeResult> TryResumeAsync(PauseReceipt receipt, CancellationToken token);
}
```

Orden recomendado:

1. `None` — siempre disponible.
2. `SendKey` (`Escape` por defecto) — sólo después de verificar que la ventana foreground pertenece al proceso/árbol esperado; un par key-down/key-up; sin forzar foco si cambió de forma inesperada.
3. `CustomKey` — override por juego.
4. `SendControllerMenu` — experimental; inyectar un mando fiable sin crear un dispositivo virtual no está garantizado por las APIs elegidas.
5. `SuspendProcess` — fuera de versiones iniciales. Las primitivas NT habituales no son una API Win32 pública soportada y suspender árboles, anti-cheat, launchers u online es peligroso.

`PauseReceipt` registra exactamente qué se hizo. Nunca se envía una tecla de reanudación si no existe receipt válido. En shutdown se ocultan overlays y se intentan sólo compensaciones seguras; no se presume que Escape sea un toggle reversible.

## 10. Concurrencia, rendimiento y limpieza

- Callbacks nativos: copiar datos mínimos y encolar; nunca tocar WPF/Playnite desde su hilo.
- Un consumidor serial actualiza estado.
- GameInput: callbacks para presencia; lectura adaptativa para actividad.
- Durante sesión: objetivo inicial 60–125 ms de muestreo para actividad/desconexión de proveedores que requieran polling; medir antes de fijar el valor.
- Fuera de sesión: sin polling de input; batería cada 60–120 s o por evento si la fuente lo permite.
- XInput: slots conectados a 100–250 ms en sesión; slots vacíos cada 2–5 s, siguiendo la recomendación de Microsoft de no consultarlos cada frame.
- Publicación UI coalescida (máximo sugerido 4 Hz para batería/estado no crítico).
- `Dispose` cancela, desregistra callbacks, espera al consumidor con timeout, cierra handles y ventanas.

## 11. Configuración y overrides

`GlobalSettings` se guarda mediante `LoadPluginSettings`/`SavePluginSettings`. Los overrides se almacenan como diccionario por `Game.Id` (GUID de base de datos de Playnite), no por nombre ni `GameId` externo:

```text
PerGameSettings
  SchemaVersion
  Overrides: Dictionary<Guid, GameOverride>
```

Datos voluminosos (diagnósticos, caché de identidad, themes instalados por el usuario) van en `GetPluginUserDataPath`, porque el directorio de la extensión se reemplaza al actualizar.

La precedencia es: override por juego > perfil seleccionado > global > defaults seguros.

## 12. Diagnóstico

Eventos estructurados mínimos:

```text
provider.device_observed
identity.resolved / identity.ambiguous
session.started / session.stopped
session.controller_activated
disconnect.suspected / cancelled / confirmed
pause.attempted / succeeded / failed
overlay.shown / hidden / failed
battery.updated / unavailable
```

Cada evento incluye `CorrelationId`, `SessionId`, `ControllerId` interno, proveedor y evidencia. Las rutas PnP/seriales se redactan en logs normales y sólo aparecen, con advertencia, en un paquete diagnóstico explícito.

## 13. Estrategia de pruebas

- Unitarias: identidad, estados de sesión, grace period con reloj virtual, tracker y normalización.
- Contract tests: trazas grabadas por proveedor sin hardware.
- Integración: conectar/desconectar Xbox, DualSense USB/BT, DS4Windows/ViGEm y Steam Input.
- Soak: 8 h en idle y 4 h de sesión, contadores de handles, callbacks, CPU y allocations.
- Fault injection: callback tardío, overlay que falla, Playnite cerrándose, PID inválido y reconexión con nuevo binding.
- Matriz visual: Desktop/Fullscreen, DPI 100–200 %, HDR, múltiples monitores y juegos windowed/borderless/exclusive.

## 14. Fuentes primarias

- [Playnite: plugins](https://api.playnite.link/docs/tutorials/extensions/plugins.html)
- [Playnite: clase Plugin y ciclo de vida](https://api.playnite.link/docs/api/Playnite.SDK.Plugins.Plugin.html)
- [Playnite: eventos](https://api.playnite.link/docs/tutorials/extensions/events.html)
- [GameInput: introducción](https://learn.microsoft.com/en-us/gaming/gdk/docs/features/common/input/overviews/input-overview)
- [GameInput: dispositivos](https://learn.microsoft.com/en-us/gaming/gdk/docs/features/common/input/overviews/input-devices)
- [GameInput: callbacks](https://learn.microsoft.com/en-us/gaming/gdk/docs/features/common/input/advanced/input-callbacks)
- [XInput: uso y rendimiento](https://learn.microsoft.com/en-us/windows/win32/xinput/getting-started-with-xinput)
