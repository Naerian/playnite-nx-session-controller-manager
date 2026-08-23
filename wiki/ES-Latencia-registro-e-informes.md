# Latencia, registro e informes

## Prueba de latencia

Selecciona **Iniciar latencia** y pulsa botones repetidamente. El tester registra los intervalos observados entre cambios de input y actualiza el valor actual, media, mínimo, máximo, muestras, duración y gráfica de polling hasta que detengas la sesión.

La gráfica mantiene un tamaño estable y utiliza el color de acento del tema activo de Playnite. Al detener la prueba, el muestreo queda congelado para poder revisar o exportar los valores y el historial final sin que sigan cambiando.

Sirve para comparar el mismo mando por cable, Bluetooth, receptor, XInput o DInput en condiciones similares.

> Es una observación a nivel de aplicación a través de SDL y Playnite. No es una medición de laboratorio de la latencia completa de USB, pantalla o juego.

Reiniciar borra la sesión. La exportación está disponible cuando la prueba está detenida y existen muestras.

La confianza de latencia depende del número de cambios de input observados. Continúa pulsando controles durante la prueba hasta alcanzar un nivel de confianza útil; las sesiones muy cortas se muestran de forma intencionada como provisionales.

## Registro de inputs

El registro está desactivado de forma predeterminada para mantener la prueba normal ligera y limpia. Actívalo en **Registro de inputs** cuando necesites un historial detallado. Cada fila identifica el control, su estado y el momento del evento.

- **Reiniciar registro** borra la sesión actual.
- **Exportar registro de inputs** abre un diálogo Guardar como.
- Cerrar el tester elimina el registro almacenado en memoria.

## Informes

El panel Test permite exportar un informe general. Las secciones de sticks y latencia también ofrecen exportaciones específicas.

**Info. dispositivo > Exportar informe de compatibilidad** crea un archivo técnico con el nombre del mando, VID/PID, layout detectado, versión de SDL, GUID de SDL, cadena de mapping, ejes/botones/hats expuestos, estado normalizado y confianza diagnóstica. No incluye la biblioteca de Playnite, cuentas, juegos ni datos del perfil de usuario. Los identificadores y el mapping sí pueden revelar el modelo de hardware, por lo que conviene revisar el archivo antes de publicarlo.

Para problemas más amplios de Controller Manager (protección de sesión, batería, overlay), usa **Avanzado > Informe de soporte**.

Siguiente: [Mandos y esquemas visuales](ES-Mandos-y-esquemas-visuales)
