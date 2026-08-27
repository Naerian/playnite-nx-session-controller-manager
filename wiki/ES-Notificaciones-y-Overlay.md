# Notificaciones y overlay

Los selectores separan presets del plugin, diseños importados y diseños revisados de creadores. Estos últimos pueden incluir distribución avanzada, fuentes, imágenes, bordes por estado y sonidos; mientras están activos, sus controles quedan bloqueados y atenuados visualmente. Los autores pueden consultar la guía de [Diseños de creadores](ES-Disenos-de-Creadores).

Usa **Actualizar diseños** para obtener versiones compatibles desde el catálogo oficial. También puedes instalar un archivo `.csmtheme` de confianza mediante **Instalar diseño de creador**, situado junto a las acciones de perfiles visuales. El plugin valida la compatibilidad y el contenido antes de sustituir atómicamente el diseño; no registra la instalación mediante doble clic en Windows.

A partir de Controller Manager 1.0.28, si el diseño de notificaciones seleccionado incluye los cuatro sonidos de evento válidos, su pack se selecciona por defecto y aparece en **Pack de sonido**. El audio nunca se bloquea: puedes elegir otro pack, seguir usando las pruebas e interruptores o asignar archivos personalizados. El sonido personalizado de un evento tiene prioridad sobre el pack seleccionado. Los conjuntos incompletos de un creador no aparecen como packs elegibles.

## Notificaciones Fullscreen

Las notificaciones de conexión están pensadas para navegar por la interfaz Fullscreen de Playnite sin un juego activo. El fallback de seguridad online también puede usar el estilo de aviso durante una partida. Las ventanas se muestran por encima, no reciben clics ni activación y desaparecen automáticamente.

En **Apariencia > Notificación a pantalla completa** y **Apariencia > Notificación de escritorio** puedes configurar ancho, escala, duración, esquina, tipografía, icono, padding, borde, redondeo, sombra, colores, acento semántico y animación. La extensión incluye Inter, Montserrat, Outfit, Poppins, Rajdhani, Chakra Petch y Orbitron sin instalarlas en Windows. El selector de color incluye la opacidad en porcentaje. El icono puede colocarse a izquierda, derecha, arriba, abajo u ocultarse. Los botones de cada subsección prueban conectado, desconectado, aviso y batería baja mediante la notificación real. Los presets sustituyen al antiguo botón de restablecimiento: **Suave** es el punto de partida neutro y los demás ofrecen composiciones distintas.

Los cambios XInput estables usan un antirrebote de 300 ms para descartar oscilaciones rápidas del driver sin esperar a la reconciliación lenta.

## Overlay de desconexión

El overlay aparece cuando un mando participante supera el margen de gracia desconectado. Muestra el dispositivo ausente, la instrucción para continuar y el resultado de la pausa. La opción **Mostrar tiempo de desconexión** añade una duración localizada que se actualiza una vez por segundo. Reconectar o completar un relevo válido lo cierra; en multijugador local la incidencia permanece hasta recuperar la plaza correspondiente.

La tarjeta y el backdrop de pantalla completa tienen colores, tamaños, iconos, padding, borde y redondeo independientes. También puedes elegir ancho y posición de la tarjeta, animación de entrada, sombra y qué lado del borde usa el acento. Título, nombre del mando, instrucción y estado/insignias disponen de familia y peso tipográfico propios, y el título, la instrucción y el estado de pausa se pueden ocultar por separado.

Las insignias opcionales de conexión y batería tienen controles independientes de color de texto, icono, fondo y borde, además de grosor, redondeo y tamaños de texto e icono. La batería puede colorearse según sus estados completo, medio, bajo y vacío. Los valores `#AARRGGBB` admiten alfa; `#00000000` hace transparente el backdrop. La vista previa compacta se actualiza al editar y los presets aplican combinaciones de composición claramente diferenciadas.

Los diseños revisados de creadores también pueden usar una composición `Alert`, degradados de escena a pantalla completa, una imagen, brillos ambientales y una cuadrícula detrás de la tarjeta. Son efectos declarativos validados, no CSS, XAML ni código ejecutable arbitrario.

## Compatibilidad y entrada

El host es un proceso WPF separado conectado mediante un named pipe autenticado por instancia. No se activa ni se inyecta en el juego. Los modos ventana y sin bordes ofrecen la mejor compatibilidad; la pantalla completa exclusiva antigua puede dibujarse por encima.

El overlay no puede bloquear universalmente XInput, Raw Input, Steam Input o GameInput sin hooks o drivers virtuales invasivos. La ruta acelerada de relevo reduce el retraso, pero no pretende interceptar la entrada recibida por el juego.
