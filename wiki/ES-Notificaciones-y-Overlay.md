# Notificaciones y overlay

## Notificaciones Fullscreen

Las notificaciones de conexión están pensadas para navegar por la interfaz Fullscreen de Playnite sin un juego activo. El fallback de seguridad online también puede usar el estilo de aviso durante una partida. Las ventanas se muestran por encima, no reciben clics ni activación y desaparecen automáticamente.

En **Apariencia > Notificación a pantalla completa** y **Apariencia > Notificación de escritorio** puedes configurar ancho, escala, duración, esquina, tipografía, icono, padding, borde, redondeo, sombra, colores, acento semántico y animación. La extensión incluye Inter, Montserrat, Outfit, Poppins, Rajdhani, Chakra Petch y Orbitron sin instalarlas en Windows. El selector de color incluye la opacidad en porcentaje. El icono puede colocarse a izquierda, derecha, arriba, abajo u ocultarse. Los botones de cada subsección prueban conectado, desconectado, aviso y batería baja mediante la notificación real. Los presets sustituyen al antiguo botón de restablecimiento: **Suave** es el punto de partida neutro y los demás ofrecen composiciones distintas.

Los cambios XInput estables usan un antirrebote de 300 ms para descartar oscilaciones rápidas del driver sin esperar a la reconciliación lenta.

## Overlay de desconexión

El overlay aparece cuando un mando participante supera el margen de gracia desconectado. Muestra el dispositivo ausente, la instrucción para continuar y el resultado de la pausa. Reconectar o completar un relevo válido lo cierra; en multijugador local la incidencia permanece hasta recuperar la plaza correspondiente.

La tarjeta y el backdrop de pantalla completa tienen colores, tamaños, iconos, padding, borde y redondeo independientes. También puedes elegir ancho y posición de la tarjeta, animación de entrada, sombra y qué lado del borde usa el acento. Título, nombre del mando, instrucción y estado/insignias disponen de familia y peso tipográfico propios, y el título, la instrucción y el estado de pausa se pueden ocultar por separado.

Las insignias opcionales de conexión y batería tienen controles independientes de color de texto, icono, fondo y borde, además de grosor, redondeo y tamaños de texto e icono. La batería puede colorearse según sus estados completo, medio, bajo y vacío. Los valores `#AARRGGBB` admiten alfa; `#00000000` hace transparente el backdrop. La vista previa compacta se actualiza al editar y los presets aplican combinaciones de composición claramente diferenciadas.

## Compatibilidad y entrada

El host es un proceso WPF separado conectado mediante un named pipe autenticado por instancia. No se activa ni se inyecta en el juego. Los modos ventana y sin bordes ofrecen la mejor compatibilidad; la pantalla completa exclusiva antigua puede dibujarse por encima.

El overlay no puede bloquear universalmente XInput, Raw Input, Steam Input o GameInput sin hooks o drivers virtuales invasivos. La ruta acelerada de relevo reduce el retraso, pero no pretende interceptar la entrada recibida por el juego.
