# Roadmap de Controller Session Manager

## 1. Método

Las versiones son incrementales, pero cada una tiene un **gate**: evidencia que debe existir antes de avanzar. No se congela una API pública ni se añade una estrategia peligrosa porque «parezca funcionar» en una sola máquina.

## 2. v0.0 — Spikes y banco de pruebas

Objetivo: retirar los riesgos que pueden invalidar la arquitectura.

Entregables:

- solución mínima `GenericPlugin` cargando en Playnite estable;
- decisión documentada C++/CLI vs helper nativo para GameInput;
- prueba de enumeración/callback/input GameInput en Xbox + DualSense;
- prueba de runtime ausente y degradación limpia;
- correlación de `pnpPath`, root y `containerId` con observaciones Playnite;
- ventana overlay externa sobre windowed/borderless y muestra exclusive;
- prueba de Custom UI Element en theme Desktop y Fullscreen;
- herramienta diagnóstica local que exporte observaciones redactadas.

Gate:

- 100 ciclos connect/disconnect sin handles/callbacks perdidos;
- plugin puede cerrar aunque el provider/overlay falle;
- se conoce y documenta el requisito real de runtime/arquitectura.

## 3. v0.1 — Inventario fiable

Incluye:

- `ControllerDevice`, observations y source confidence;
- `IControllerProvider` y GameInput provider;
- `ControllerManager`, registry y snapshots;
- Playnite bridge como fallback/enriquecimiento;
- identidad por container/root/path;
- connected/missing/unknown;
- settings mínimos y logging estructurado;
- página diagnóstico de sólo lectura;
- tests unitarios y contract fixtures.

No incluye batería, pausa ni overlay.

Gate:

- Xbox, DualSense, DS4 y un genérico enumeran sin duplicados en los casos de prueba definidos;
- los casos ambiguos se muestran como tales, no se fusionan incorrectamente;
- CPU idle y memoria se miden y publican.

## 4. v0.2 — Sesiones y mando activo

Incluye:

- integración `OnGameStarting/Started/Stopped/StartupCancelled`;
- `GameSessionManager` con cancellation y session id;
- `ActiveControllerTracker` con deadzones/histéresis;
- uno o varios mandos activos;
- sticky membership y cambio/takeover configurable;
- `SuspectDisconnect` y grace period cancelable;
- overrides por `Game.Id` para protección y grace period;
- simulador/replay de trazas para tests deterministas.

Todavía sólo registra/notifica dentro de Playnite; no inyecta pausa.

Gate:

- microcortes bajo grace period no crean incidente confirmado;
- desconectar un mando inactivo no dispara protección;
- desconectar uno de dos activos identifica exactamente cuál falta;
- eventos tardíos de una sesión anterior no afectan la siguiente.

## 5. v0.3 — Pausa segura y overlay Default

Incluye:

- `GameTargetResolver` y confidence;
- `GamePauseManager` con `None`, `SendEscape` y `CustomKey`;
- verificación de foreground/target antes de `SendInput`;
- `PauseReceipt`; auto-resume desactivado por defecto;
- OverlayHost WPF, IPC v1, watchdog y Default theme integrado;
- monitor del juego, multimonitor y DPI;
- reconnect/hide/takeover flow;
- overrides por juego.

Excluye `SuspendProcess` y bloqueo global de input.

Gate:

- ninguna tecla se envía si foreground/target es ambiguo;
- overlay se autooculta al perder plugin o terminar sesión;
- matrices windowed/borderless pasan; exclusive queda explícitamente documentado;
- un crash del theme/host no afecta Playnite ni deja juego «pausado por CSM» sin receipt.

## 6. v0.4 — Batería y UI para themes Playnite

Incluye:

- `BatteryManager` y modelo exact/discrete/unavailable;
- XInput battery provider con niveles cualitativos;
- primer perfil HID sólo si está validado en hardware y transporte;
- low/critical warnings con histéresis/cooldown;
- catálogo Custom UI Elements v1;
- iconos por familia/estado con fallback;
- `ControllerList` y PlayerSlot 1–4;
- samples Desktop/Fullscreen y documentación con screenshots.

Gate:

- ningún nivel discreto aparece como porcentaje;
- batería stale se distingue de batería actual;
- cero/uno/cuatro mandos y todos los estados visuales pasan snapshot tests;
- API v1 se valida con al menos dos themes de muestra.

## 7. v0.5 — Overlay Theme API

Incluye:

- manifiesto `theme.yaml` y `ControllerSessionManagerApiVersion: 1`;
- resource/template theme model allowlisted;
- loader, validación, fallback y diagnóstico;
- paquetes de themes externos en user data path;
- Default + Minimal sample;
- guía completa para fork y compatibilidad N/N−1.

Gate:

- suite de themes maliciosos/rotos no accede a red/filesystem ni tumba Playnite;
- cualquier fallo vuelve a Default;
- validación visual completa y documentación publicada.

## 8. v0.6+ — Cobertura y refinamiento

Candidatos, sólo por evidencia:

- perfiles HID DualSense/DS4/Nintendo por transporte;
- Raw Input fallback opt-in;
- perfiles/import/export de mappings virtual↔physical;
- feedback/rumble de prueba en diagnóstico con consentimiento;
- overlay interactivo accesible;
- más de cuatro player slots;
- protocolos/localizaciones adicionales.

`SuspendProcess` no entra automáticamente en este bloque: requiere ADR propia, API soportada o helper con watchdog, allowlist explícita y pruebas de procesos/anti-cheat. La recomendación actual sigue siendo no implementarlo.

## 9. Riesgos priorizados

| Riesgo | Prob. | Impacto | Mitigación / gate |
|---|---:|---:|---|
| Interop GameInput dentro de Playnite | Alta | Alta | Spike v0.0; wrapper aislado y fallback |
| Runtime GameInput no instalado | Media | Alta | Detección, instrucciones, Playnite bridge degradado |
| Duplicados físico/virtual | Alta | Alta | observations + confidence + no merge ambiguo + UI de mapping |
| Identidad cambia al reconectar/puerto | Alta | Alta | container/root/serial/tombstone; matching explícito |
| Active tracker confunde drift con input | Media | Alta | deadzone, histéresis, fixtures y telemetría local |
| Juego exclusivo tapa overlay | Media | Media | matriz real; promesa limitada; recomendar borderless |
| `SendInput` llega a otra ventana | Media | Alta | verificar foreground inmediatamente; abortar ante duda |
| Juego elevado/anti-cheat | Media | Alta | sin elevación/hooks; `None` por override y default prudente |
| XAML externo ejecuta capacidades no deseadas | Media | Alta | templates/resources allowlisted y proceso aislado |
| Playnite UI thread violation | Media | Alta | cola + UIDispatcher sólo en borde visual |
| API de themes se congela demasiado pronto | Alta | Media | samples/spikes antes de declarar v1 estable |
| Información de batería engañosa | Alta | Media | provenance + exact/discrete/unknown; sin conversión falsa |

## 10. Métricas de aceptación

Presupuestos iniciales a validar, no promesas todavía:

- CPU monitor básico idle: mediana <0.2 % en equipo de referencia;
- memoria incremental plugin+host dormido: medir y fijar después del spike;
- confirmación de desconexión: callback/poll + grace period, sin trabajo UI bloqueante;
- cero crecimiento sostenido de handles/threads en soak;
- cero eventos de sesión aplicados tras `Stop`;
- fallback funcional ante ausencia de cada provider opcional.

Los resultados y equipo/OS se publicarán para que las cifras sean reproducibles.

## 11. Backlog de documentación/wiki

La carpeta `docs/` es la fuente versionada y la GitHub Wiki será una publicación/curación de ella. Páginas:

- Home
- Installation
- Getting Started
- Settings
- Supported Controllers
- Controller Detection
- Battery Information
- Game Pause Strategies
- Overlay System
- Overlay Themes
- Theme Integration
- Developer Documentation
- Custom UI Elements
- Creating an Overlay Theme
- Troubleshooting
- FAQ
- Known Limitations

Cada release debe actualizar compatibilidad, limitaciones y capturas en el mismo PR que cambia comportamiento. La wiki no será la única copia de información contractual.

## 12. Orden inmediato de trabajo

1. Crear ADR-001 de target framework/Playnite SDK real.
2. Implementar spike GameInput nativo y registrar el ABI/despliegue elegido.
3. Construir herramienta de enumeración con dump redactado.
4. Recoger trazas de hardware y wrappers.
5. Implementar dominio/identity resolver con fixtures antes del plugin completo.
6. Probar custom elements y overlay mínimo.
7. Sólo entonces crear el esqueleto productivo v0.1.

## 13. Criterio de “soportado”

Un dispositivo sólo se declara soportado si existe:

- hardware/driver/transporte probado;
- versión de Windows y provider registrada;
- resultado esperado por capability;
- fixture o test reproducible;
- limitaciones conocidas.

«Detectado» no implica batería, conexión exacta, rumble ni identidad persistente.

