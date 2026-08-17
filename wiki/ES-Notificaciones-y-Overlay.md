# Notificaciones y overlay

## Notificaciones Fullscreen

Las notificaciones de conexión están pensadas para navegar por la interfaz Fullscreen de Playnite sin un juego activo. El fallback de seguridad online también puede usar el estilo de aviso durante una partida. Las ventanas se muestran por encima, no reciben clics ni activación y desaparecen automáticamente.

En **Apariencia > Notificación en pantalla** puedes configurar ancho, escala, duración, esquina, tipografía, icono, padding, borde, redondeo y colores. El icono puede colocarse a izquierda, derecha, arriba, abajo u ocultarse. Hay vistas previas independientes para conectado, desconectado y aviso.

Los cambios XInput estables usan un antirrebote de 300 ms para descartar oscilaciones rápidas del driver sin esperar a la reconciliación lenta.

## Overlay de desconexión

El overlay aparece cuando un mando participante supera el margen de gracia desconectado. Muestra el dispositivo ausente, la instrucción para continuar y el resultado de la pausa. Reconectar o completar un relevo válido lo cierra; en multijugador local la incidencia permanece hasta recuperar la plaza correspondiente.

La tarjeta y el backdrop de pantalla completa tienen colores, tamaños, tipografías, iconos, padding, borde y redondeo independientes. El icono situado junto al nombre del mando y el icono del estado de pausa o aviso pueden mostrarse u ocultarse por separado. Los valores `#AARRGGBB` admiten alfa; `#00000000` hace transparente el backdrop. La vista previa compacta se actualiza al editar.

## Compatibilidad y entrada

El host es un proceso WPF separado conectado mediante un named pipe autenticado por instancia. No se activa ni se inyecta en el juego. Los modos ventana y sin bordes ofrecen la mejor compatibilidad; la pantalla completa exclusiva antigua puede dibujarse por encima.

El overlay no puede bloquear universalmente XInput, Raw Input, Steam Input o GameInput sin hooks o drivers virtuales invasivos. La ruta acelerada de relevo reduce el retraso, pero no pretende interceptar la entrada recibida por el juego.
