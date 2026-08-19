# Tester

Controller Manager incluye ya el antiguo Gamepad Tester. Abre **Ajustes > Tester** para el mapa de botones, salud de sticks, latencia, perfil de diagnóstico, prueba guiada, registro de entrada, información del dispositivo y opciones.

El muestreo SDL GameController vive en `ControllerSessionManager.TesterHost.exe`, no dentro de Playnite. Cerrar ajustes, iniciar un juego protegido o descargar el plugin detiene ese host.

## Desktop

- **Mandos > Probar mando** cambia a la pestaña **Probador** y deja seleccionado ese mando.
- La entrada opcional de la barra lateral abre la misma vista **sin** la pestaña Opciones. Esas opciones siguen en **Ajustes > Tester**.
- Si hay varios mandos conectados, elige uno en **Mando** en el panel izquierdo. **Prueba general**, **Info. dispositivo** y el resto de pestañas muestran solo ese mando.
- **Prueba guiada** deja la lista en vivo a la izquierda. Inicia o detén con el botón de la derecha; al parar o terminar aparece allí el informe, con un check verde o una X roja por control.
- Desinstala el complemento antiguo **Gamepad Tester**. Dos extensiones no pueden reclamar el mismo `SourceName` Fullscreen `GamepadTester`.

## Bloques Fullscreen

Los nombres canónicos usan `SourceName = ControllerSessionManager`:

- `TesterLauncher`
- `TesterStatusBadge`
- `TesterButtonMap`
- `TesterStickCheck`
- `TesterTriggerCheck`
- `TesterRumblePad`
- `TesterLatencyMini`

Los alias de compatibilidad mantienen `SourceName = GamepadTester` y los nombres originales. Véase [Integración con temas](ES-Integracion-con-Temas) y `docs/theme-integration/CONTRACT.md`.
