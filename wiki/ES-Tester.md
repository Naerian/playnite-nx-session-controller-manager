# Tester

Controller Manager incluye ya el antiguo Gamepad Tester. Abre **Ajustes > Tester** para el mapa de botones, salud de sticks, latencia, perfil de diagnóstico, prueba guiada, registro de entrada, información del dispositivo y opciones.

El muestreo SDL GameController vive en `ControllerSessionManager.TesterHost.exe`, no dentro de Playnite. Cerrar ajustes, iniciar un juego protegido o descargar el plugin detiene ese host.

Desinstala el complemento independiente **Gamepad Tester**. Dos extensiones no pueden reclamar el mismo `SourceName` Fullscreen `GamepadTester`.

## Guías

| Tema | Página |
| --- | --- |
| Flujo de Escritorio | [Tester de escritorio](ES-Tester-de-escritorio) |
| Lista ordenada | [Prueba guiada](ES-Prueba-guiada) |
| Drift, cobertura, centro | [Sticks, calibración y salud](ES-Sticks-calibracion-y-salud) |
| Tiempos y exportaciones | [Latencia, registro e informes](ES-Latencia-registro-e-informes) |
| Layouts y familias | [Mandos y esquemas visuales](ES-Mandos-y-esquemas-visuales) |
| Opciones del tester | [Configuración del Tester](ES-Configuracion-del-Tester) |
| Bloques Fullscreen | [Integración Tester Fullscreen](ES-Integracion-Tester-Fullscreen) |
| UI de batería / identidad | [Integración con temas](ES-Integracion-con-Temas) |

## Desktop

- **Mandos > Probar mando** cambia a la pestaña **Probador** y deja seleccionado ese mando.
- La entrada opcional de la barra lateral abre la misma vista **sin** la pestaña Opciones. Esas opciones siguen en **Ajustes > Tester**.
- Si hay varios mandos conectados, elige uno en **Mando** en el panel izquierdo. **Prueba general**, **Info. dispositivo** y el resto de pestañas muestran solo ese mando.
- **Prueba guiada** deja la lista en vivo a la izquierda. Inicia o detén con el botón de la derecha; al parar o terminar aparece allí el informe, con un check verde o una X roja por control.

## Bloques Fullscreen

Los nombres canónicos usan `SourceName = ControllerSessionManager`:

- `TesterLauncher`
- `TesterStatusBadge`
- `TesterButtonMap`
- `TesterStickCheck`
- `TesterTriggerCheck`
- `TesterRumblePad`
- `TesterLatencyMini`

Los alias de compatibilidad mantienen `SourceName = GamepadTester` y los nombres originales. Detalle: [Integración Tester Fullscreen](ES-Integracion-Tester-Fullscreen) y `docs/theme-integration/CONTRACT.md`.
