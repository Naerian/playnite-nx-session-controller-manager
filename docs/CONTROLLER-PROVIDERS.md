# Proveedores de controladores

> Esta comparación describe capacidades comprobadas en documentación primaria. «Posible mediante HID» no significa «genérico para todos los modelos»: los reportes vendor-specific requieren perfiles probados.

## 1. Recomendación

Pila implementada en 1.0.6:

1. **Playnite SDK**: autoridad de inventario, conexión, desconexión y botones.
2. **XInput 1.4**: input de alta frecuencia, slots, vibración y batería cualitativa cuando el endpoint sea XInput.
3. **SDL limitado a Desktop**: nombre, ruta, VID/PID, input y capacidades que Playnite ya normaliza; nunca gobierna el ciclo de vida visible.
4. **Windows PnP/HID**: transporte, batería Bluetooth expuesta por Windows y perfiles de batería estrictamente conocidos.
5. **Raw Input/GameInput propios**: no se cargan en la versión actual. Sólo se reconsiderarán fuera de proceso y con una necesidad demostrada.

La base SDL GameControllerDB normaliza GUID, nombre, botones y ejes conocidos; no proporciona por sí misma batería, transporte ni un ciclo de vida independiente. La extensión aprovecha la compatibilidad SDL de Playnite mediante el SDK y evita crear un segundo propietario de eventos en Fullscreen.

## 2. Matriz de capacidades

| Capacidad | GameInput actual | XInput 1.4 | Raw Input | HID/SetupAPI | Playnite SDK |
|---|---|---|---|---|---|
| Conectar/desconectar | Callback | Polling 0–3 | `WM_INPUT_DEVICE_CHANGE`/enumeración | Notificaciones PnP | Eventos oficiales |
| Input | Polling o callback, timestamps | Polling, packet number | `WM_INPUT` por dispositivo | Lectura de reports | Botones de navegación |
| Más de 4 | Sí | No (4) | Sí | Sí | Sí, según backend de Playnite |
| VID/PID | Sí | No | Info/ruta HID | Sí | No directo; expone Path |
| Nombre/ruta PnP | Sí | No | Ruta | Sí | Sí |
| Identidad/root/container | `deviceId`, `deviceRootId`, `containerId` | Sólo slot | Ruta; correlación externa | Instance/container properties | InstanceId/path, estabilidad no documentada |
| Capabilities | Amplias | Tipo/subtipo y flags | Descriptor raw | Descriptor HID | No |
| Rumble/feedback | Sí, según dispositivo | 2 motores | No genérico | Reports específicos | No |
| Batería | **No en API actual** | Tipo + Empty/Low/Medium/Full | No genérico | Sólo si descriptor/driver/perfil lo expone | No documentada |
| Conexión USB/BT | Inferible por ruta/árbol, no asumir | Wired vs battery type de forma limitada | Inferible por ruta | Árbol PnP/propiedades | No documentada |

## 3. GameInput

### Capacidades confirmadas

- Windows 10 19H1+ y Windows 11; la versión actual para PC se distribuye mediante `Microsoft.GameInput` y requiere runtime instalado.
- API unificada para gamepads, mandos raw, volantes, joysticks, etc.
- `RegisterDeviceCallback` combina enumeración inicial y cambios posteriores sin carrera entre ambas.
- Lecturas actuales o callbacks, filtrables por dispositivo y tipo; timestamps monotónicos en microsegundos.
- `GameInputDeviceInfo`: VID, PID, revisión, familia, input soportado, motores de rumble, botones de sistema, `deviceId`, `deviceRootId`, `containerId`, nombre y ruta PnP.
- El mismo objeto de dispositivo permanece válido al desconectar (estado «zombie») y puede revivir al reconectar.
- Feedback, haptics, sensores y acceso raw dependen de versión/capacidades.

### Límite crítico de batería

`IGameInputDevice::GetBatteryState` existió en GameInput v0, pero Microsoft indica que nunca se implementó, devolvía `E_NOTIMPL`, y fue eliminado en v1. La API actual no ofrece consulta directa de batería. Por tanto, GameInput no será fuente de batería.

### Coste de despliegue

El runtime v1+ debe estar instalado en PC. No se instalará silenciosamente desde el plugin. El instalador/README deberá comprobarlo y ofrecer instrucciones. El adaptador se cargará dinámicamente y fallará de forma degradada si falta.

### Interop

GameInput es una interfaz nativa C++ con semántica similar a COM pero no COM real. Para el plugin C# `net462`, las opciones son:

| Opción | Pros | Contras | Recomendación |
|---|---|---|---|
| P/Invoke/vtable manual | Un solo paquete | Riesgo alto de ABI y lifetime | No |
| Wrapper C++/CLI x64 | Tipado y RAII nativo | Build adicional; arquitectura fija | Prototipo preferido |
| Helper nativo fuera de proceso | Aisla crashes/ABI; permite runtime moderno | IPC y despliegue mayores | Alternativa si C++/CLI choca con Playnite |

La decisión final C++/CLI vs helper requiere un spike v0.0; no debe contaminar el dominio.

## 4. XInput

XInput soporta hasta cuatro user indexes (0–3), estado estándar, packet number, capacidades, vibración y batería. No expone VID/PID, nombre ni una identidad física estable. Un slot puede cambiar tras reconexión, por lo que `dwUserIndex` nunca será `ControllerId`.

`XInputGetBatteryInformation` devuelve:

- tipo: disconnected, wired, alkaline, NiMH o unknown;
- nivel: empty, low, medium o full;
- el nivel sólo es válido para un dispositivo inalámbrico con tipo conocido.

No devuelve porcentaje. El dominio conserva el nivel discreto.

Estrategia de polling:

- slots conectados: 100–250 ms durante sesión para input/presencia;
- slot vacío: 2–5 s, porque Microsoft desaconseja consultarlo cada frame;
- fuera de sesión: sólo inventario/batería a baja frecuencia.

La correlación XInput→GameInput se intentará por comportamiento temporal y metadatos disponibles, nunca se dará por segura sólo porque hay un único slot. Si no hay evidencia suficiente, la batería XInput se asocia al slot, no al dispositivo físico visible.

## 5. Raw Input

Raw Input permite registrar top-level collections HID, distinguir dispositivos por `hDevice`, recibir `WM_INPUT` incluso en background con `RIDEV_INPUTSINK`, enumerar con `GetRawInputDeviceList` y obtener información con `GetRawInputDeviceInfo`.

Ventajas:

- cobertura amplia de HID;
- input por dispositivo y eventos de llegada/salida;
- no exige abrir el dispositivo para recibir input.

Límites:

- los datos son de bajo nivel y dependen del descriptor;
- no ofrece batería o modelo semántico de forma universal;
- registrar una TLC es global por proceso: sólo la última ventana registrada para una clase recibe Raw Input. Integrarlo dentro de Playnite podría interferir con el propio host u otros plugins.

Por ese riesgo, Raw Input queda detrás de feature flag y sólo se añadirá si una matriz de hardware demuestra huecos reales de GameInput.

## 6. HID y SetupAPI/Configuration Manager

Las APIs HID (`HidD_*`, `HidP_*`) permiten descubrir atributos/cadenas/capacidades, interpretar reports y enviar reports. Los game controllers HID usan top-level collections compartidas. SetupAPI/Configuration Manager permiten recorrer nodos y propiedades PnP para correlacionar interfaces, contenedores y buses.

Uso permitido en CSM:

- obtener VID/PID, serial si existe, container/root, bus y nombre;
- reconocer perfiles versionados y probados;
- consultar feature reports de batería sólo cuando el protocolo esté documentado o validado por modelo/firmware/transporte;
- nunca abrir de forma exclusiva ni escribir reports en el monitor básico.

No existe un report HID universal de batería para todos los mandos. DualSense, DualShock y Nintendo requieren perfiles específicos y pueden cambiar entre USB/Bluetooth. Los perfiles se empaquetarán como `IHidDeviceProfile`, con tests de fixtures y allowlist de VID/PID.

## 7. Windows.Gaming.Input

WGI ofrece `Gamepad` y `RawGameController`, eventos Added/Removed, más de cuatro dispositivos, rumble y VID/PID en `RawGameController`. Microsoft lo recomienda frente a XInput para código UWP nuevo, pero GameInput es ahora la recomendación más amplia para código de juegos nuevo.

WGI puede servir como fallback si el spike de GameInput no es viable en el proceso Playnite. No se ejecutarán ambos proveedores principales por defecto: aumentaría observaciones duplicadas sin aportar batería genérica.

## 8. Playnite Controller API

Playnite 10 actual expone:

- `OnControllerConnected` y `OnControllerDisconnected`;
- `OnControllerButtonStateChanged` y variante Desktop;
- `IPlayniteAPI.GetConnectedControllers()`;
- `GamepadController`: `InstanceId`, `Path`, `Name`, `Enabled`.

Restricciones documentadas:

- los eventos sólo están disponibles para plugins, no scripts;
- en Desktop el soporte de controller API debe habilitarse en settings de input;
- el modelo no expone batería, VID/PID o capabilities.

Se usa como bridge/fallback y para respetar la navegación de Playnite, no como única fuente de sesión.

## 9. Steam Input, DS4Windows y dispositivos virtuales

Steam Input y DS4Windows pueden ocultar el dispositivo físico y exponer uno virtual (frecuentemente XInput/ViGEm); también pueden dejar visibles ambos. Desde fuera del juego no existe una forma universal de saber qué endpoint consume el juego.

Política:

- modelar `Physical`, `Virtual` y `Relationship=PossibleTransform` cuando haya evidencia;
- proteger el endpoint que genera input durante la sesión, aunque sea virtual;
- no colapsar físico+virtual automáticamente salvo evidencia fuerte y simultaneidad compatible;
- permitir al usuario marcar mappings persistentes o ignorar endpoints;
- registrar decisiones y confidence.

El mejor detector práctico de «activo» es input reciente en el endpoint observado, no una lista estática de marcas compatibles.

## 10. Compatibilidad esperada

| Familia | Ruta inicial | Resultado esperado |
|---|---|---|
| Xbox 360/One/Series | GameInput + XInput | Presencia/input; rumble; batería discreta cuando XInput la expone |
| DualSense/DS4 nativo | GameInput; perfil HID posterior | Presencia/input/modelo; batería sólo tras perfil validado |
| Switch Pro/Joy-Con | GameInput; perfil posterior | Presencia/input; identidad compuesta requiere pruebas |
| Genérico HID | GameInput Controller/Raw | Presencia/input/capabilities variables |
| DS4Windows/ViGEm | GameInput + XInput | Endpoint virtual activo; relación física posible, no garantizada |
| Steam Input | Endpoint visible al sistema | No se promete correlación con el físico oculto |

## 11. Estrategia de deduplicación

Evidencia, de mayor a menor fuerza:

1. mismo `containerId` no vacío y relación root compatible;
2. mismo `deviceRootId`/nodo raíz de GameInput;
3. misma ruta de interfaz normalizada o mapeo documentado entre paths;
4. serial estable + VID/PID + transporte;
5. VID/PID + ubicación/puerto + coincidencia temporal;
6. correlación de input (misma secuencia/tiempo) para mapping XInput/virtual;
7. nombre solamente: nunca suficiente.

Reglas:

- No fusionar dos ejemplares idénticos sólo por VID/PID.
- Un dispositivo compuesto puede tener varias interfaces bajo un `deviceRootId`.
- `deviceId` de GameInput es application-local y puede depender del puerto; sirve como binding persistente condicionado, no como serial global.
- La reconexión abre una ventana de matching con tombstones; si hay empate, crea un nuevo dispositivo y marca ambigüedad.
- El usuario puede fijar/romper un mapping; esa decisión tiene precedencia y versión de esquema.

## 12. Fuentes primarias

- [GameInput FAQ y requisitos](https://learn.microsoft.com/en-us/gaming/gdk/docs/features/common/input/overviews/input-faq)
- [GameInputDeviceInfo](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/structs/gameinputdeviceinfo)
- [GameInput callbacks](https://learn.microsoft.com/en-us/gaming/gdk/docs/features/common/input/advanced/input-callbacks)
- [GameInput battery removida](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/deprecated/interfaces/igameinputdevice/methods/igameinputdevice_getbatterystate)
- [XInput getting started](https://learn.microsoft.com/en-us/windows/win32/xinput/getting-started-with-xinput)
- [XINPUT_BATTERY_INFORMATION](https://learn.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_battery_information)
- [Raw Input overview](https://learn.microsoft.com/en-us/windows/win32/inputdev/about-raw-input)
- [HID API](https://learn.microsoft.com/en-us/windows-hardware/drivers/hid/hid-api)
- [WGI RawGameController](https://learn.microsoft.com/en-us/windows/uwp/gaming/raw-game-controller)
- [Playnite GamepadController](https://api.playnite.link/docs/api/Playnite.SDK.Events.GamepadController.html)
