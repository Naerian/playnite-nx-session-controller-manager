# Solución de problemas y preguntas frecuentes

## Falta un mando o aparece duplicado

Actualiza la lista, cierra temporalmente herramientas de remapeo y compara el dispositivo físico con el XInput virtual que puedan crear. Exporta el diagnóstico HID e indica modelo, conexión y software como Steam Input, DS4Windows o reWASD.

## ¿Por qué la batería aparece como Desconocida?

El driver o receptor activo no expone un valor estándar fiable. Es habitual con dongles propietarios. Los niveles XInput son aproximados y no pueden convertirse honestamente en porcentajes. La batería Bluetooth de Windows solo se usa en rutas HID Bluetooth reales, no en un wrapper XInput de dongle.

## ¿Por qué un mando por dongle salía como Bluetooth?

Un wrapper XInput es un cable o un receptor de 2,4 GHz salvo que sea un mando con licencia Xbox que también aparezca por Bluetooth. Un HID BLE hermano de la misma marca no debe relabelar la fila del dongle.

## ¿Por qué aparecía una fila genérica Game Controller?

Playnite y Windows suelen exponer una colección HID sin nombre mientras el mando termina de enumerarse. Los restos USB desconocidos se ocultan; un VID/PID conocido se muestra con el nombre del modelo.

## ¿Por qué Fullscreen muestra un jugador genérico?

Por estabilidad, el proceso Fullscreen de Playnite nunca inicializa SDL. Abre una vez Desktop con el mando conectado para asociar su identidad y perfil personalizado al slot XInput, guarda y reinicia Fullscreen.

## ¿Por qué no apareció el overlay?

Comprueba que monitorización, seguimiento de sesión y overlay estén activos. El mando debe recibir una entrada intencionada después de lanzar el juego y permanecer ausente más allá del margen de gracia. El juego debe haberse iniciado desde Playnite y no tener una política individual desactivada.

## ¿Por qué no se pausó el juego?

La pausa está desactivada por defecto. La tecla se omite si no puede verificarse el árbol del juego en primer plano. La pausa forzada tampoco suspende cuando encuentra evidencias online. El estado del overlay explica el resultado.

## ¿Puede el overlay impedir que otro mando controle el juego por detrás?

No de forma universal. El overlay no recibe clics ni instala hooks o drivers virtuales. Interceptar todas las APIs de entrada añadiría riesgos de compatibilidad, estabilidad y anticheat. El relevo se detecta rápidamente, pero alguna entrada puede llegar al juego.

## ¿Qué modos de pantalla funcionan?

Se recomiendan ventana y pantalla completa sin bordes. Un juego exclusivo heredado puede dibujarse por encima de ventanas externas. Ejecutar el juego como administrador y Playnite sin elevar también puede impedir la verificación del proceso.

## ¿Por qué avisa que Gamepad Tester sigue instalado?

Desinstala el complemento independiente Gamepad Tester. Controller Manager ya incluye ese tester, incluido el `SourceName` Fullscreen `GamepadTester`. Dos extensiones no pueden registrar la misma fuente de tema.

## ¿Por qué Tester no muestra ningún mando?

Abre **Ajustes > Tester** o un bloque Fullscreen del tester para que arranque `ControllerSessionManager.TesterHost.exe`. SDL se carga desde la carpeta de Playnite, no dentro del proceso del complemento. Si el host no está en la carpeta de la extensión, reinstala el `.pext`.

## ¿Qué debe incluir una incidencia útil?

Exporta **Avanzado > Informe de soporte** y adjúntalo junto con modo y tema de Playnite, modelo del mando, USB/Bluetooth/dongle, juego, herramientas de remapeo, pasos exactos y resultado esperado frente al real. El informe ya incluye versión, estado anónimo de la sesión y cronología reciente. Añade un diagnóstico HID revisado solo si el problema afecta a detección o batería.
