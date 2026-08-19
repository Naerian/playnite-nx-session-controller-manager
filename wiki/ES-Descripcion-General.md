# Descripción general

Controller Manager es una extensión de Playnite para visualizar mandos, probarlos dentro del plugin y proteger sesiones de juego en Windows.

## Qué permite hacer

- Mostrar mandos conectados y conocidos con nombre real, alias e icono asignable.
- Indicar USB, Bluetooth o receptor inalámbrico cuando Windows aporta evidencias suficientes.
- Mostrar niveles de batería XInput sin inventar porcentajes.
- Probar la vibración, abrir la pestaña Tester completa y exportar diagnósticos HID de solo lectura.
- Añadir un acceso adaptativo con mando y batería a la barra superior de Desktop.
- Mostrar notificaciones configurables al navegar por Fullscreen.
- Registrar qué mandos participan realmente después de iniciar un juego.
- Mostrar un overlay externo si se desconecta un mando participante.
- Enviar una tecla de pausa de forma segura o pausar forzosamente juegos offline de manera opcional.
- Detectar actividad multijugador local sostenida y proteger cada participante por separado.

## Prioridades de diseño

La extensión prioriza la estabilidad frente a la interceptación invasiva. No se inyecta en juegos, no instala hooks de entrada y no carga código arbitrario de temas en el overlay externo. En Fullscreen se evitan deliberadamente las llamadas SDL porque ciertas rutas nativas de desconexión podían cerrar Playnite con algunos drivers.

El modo automático es el recomendado. Un mando solo entra en la sesión después de recibir una entrada intencionada. Cambiar de mando en un juego individual transfiere normalmente el control, mientras que una alternancia sostenida entre varios dispositivos promociona la sesión a multijugador local.

## Limitaciones importantes

La batería y el transporte dependen del firmware y los drivers. La detección de sesiones online es aproximada y Windows no ofrece una API universal que determine si cualquier juego está offline, online o en cooperativo local. Conviene probar la pausa en cada juego.

Continúa con [Instalación e inicio rápido](ES-Instalacion-e-Inicio-Rapido).
