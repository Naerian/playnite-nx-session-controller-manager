# Protección de sesiones

## Cómo entra un mando en la sesión

Un mando conectado no se considera automáticamente un jugador. Tras iniciar el juego, Controller Session Manager busca entradas intencionadas: botones, gatillos por encima del umbral o movimientos amplios de stick. Una ventana de diez segundos conserva el mando usado para lanzar el juego desde Desktop. Si el juego captura todo el input antes de que Playnite pueda observarlo, se arma exactamente un mando conectado como propietario inicial conservador. La primera entrada real puede reemplazar esa inferencia de inmediato; el resto de mandos no se convierten en jugadores por estar conectados. Se ignoran liberaciones, drift pequeño, simples cambios de paquete y botones Guide/Home.

## Modo automático/adaptativo

El modo automático comienza como un jugador. La entrada intencionada de otro mando conectado puede transferir la propiedad, cubriendo el caso habitual de iniciar con el mando equivocado. Durante una incidencia confirmada, reconectar el mando perdido o usar de forma intencionada un sustituto válido resuelve el overlay.

El overlay se muestra ya durante la fase de sospecha para responder con rapidez, aunque la pausa continúa esperando al margen configurado. Un mando conectado después de la incidencia se considera un sustituto intencionado porque algunos drivers ocultan el botón Home usado para encenderlo. Si el mando ya estaba conectado, debe registrarse una pulsación o movimiento de stick.

En Apariencia se puede ocultar de forma independiente el icono situado junto al nombre del mando y el icono del estado de pausa o aviso. Ambos cambios se reflejan en la previsualización del overlay.

Una alternancia sostenida entre varios mandos promociona la sesión a multijugador local. Desde ese momento se protege cada participante por separado. El mando ya activo de otro jugador no sustituye silenciosamente al jugador ausente, aunque un dispositivo nuevo o sin asignar sí puede ocupar esa plaza.

## Política por juego

Abre el menú contextual del juego y selecciona **Controller Session Manager > Protección de sesión**:

- **Usar configuración global**: hereda la configuración general.
- **Automático / adaptativo**: relevo normal y detección automática de multijugador local.
- **Multijugador local**: protege explícitamente todos los participantes activos.
- **Desactivada**: no genera incidencias para ese juego.

La opción efectiva aparece marcada.

## Política de pausa

El submenú independiente **Pausa automática** permite heredar, usar solo overlay, forzar pausa offline con aviso si hay actividad online, enviar Escape o la tecla configurada. La tecla solo se envía una vez y después de verificar el árbol de procesos en primer plano.

La pausa forzada actúa únicamente sobre un proceso offline verificado y su concesión de seguridad lo reanuda al resolver la incidencia, terminar el juego, cerrar Playnite o perder la comunicación. Los metadatos inequívocos de juego exclusivamente online usan un aviso no bloqueante. Una conexión TCP pública también evita la suspensión, pero conserva el overlay porque podría ser solo telemetría o un servicio de la plataforma. La detección online es aproximada; prueba siempre cada juego.
