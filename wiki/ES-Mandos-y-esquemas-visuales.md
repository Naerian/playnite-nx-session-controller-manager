# Mandos y esquemas visuales

Esta página describe cómo el **Tester** identifica layouts y dibujos. Para batería, transporte e identidad de sesión, véase [Mandos y batería](ES-Mandos-y-Bateria).

## Arquitectura de entrada

El Tester utiliza la normalización de SDL GameController y prefiere el runtime SDL incluido con Playnite. El muestreo corre en `ControllerSessionManager.TesterHost.exe`. SDL ofrece un conjunto coherente de controles para dispositivos XInput y DInput que están correctamente mapeados.

Los nombres normalizados siguen la convención Xbox: `LS`, `RS`, `LB`, `RB`, `LT`, `RT`, `A`, `B`, `X` e `Y`. Los dibujos de PlayStation y Nintendo utilizan sus símbolos habituales cuando corresponde, mientras que el registro conserva nombres estables.

## Familias compatibles

La identificación automática y los layouts cubren mandos comunes Xbox One, Xbox Series/Elite, DualShock, DualSense, Nintendo Switch Pro, 8BitDo, Steam Controller y dispositivos genéricos compatibles con SDL. La detección depende del nombre y VID/PID expuestos por el controlador y el modo activo.

Esquemas disponibles:

- Universal
- Xbox Series X / S
- Xbox One
- PlayStation / DualShock
- DualSense
- Nintendo Switch Pro
- 8BitDo Ultimate
- 8BitDo Pro
- Steam Controller

El selector de esquema solo cambia el dibujo y sus etiquetas. No modifica el dispositivo seleccionado, el mapeo ni el controlador.

## Modos 8BitDo y botones adicionales

Los mandos 8BitDo pueden comunicarse mediante XInput o DInput según su modo físico. Ambos pueden funcionar si SDL reconoce el dispositivo, pero pueden cambiar el nombre, VID/PID, mapeo, vibración y controles adicionales expuestos. XInput suele ser la primera opción más predecible.

Las palancas traseras, botones de perfil, controles LED, paneles táctiles y otros controles propietarios solo se muestran si SDL los expone. La extensión no puede deducir de forma fiable nombres que no estén presentes en la API.

Siguiente: [Configuración del Tester](ES-Configuracion-del-Tester)
