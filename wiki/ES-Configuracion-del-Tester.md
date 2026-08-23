# Configuración del Tester

Abre **Complementos > Configuración de extensiones > Genérico > Controller Manager** y la pestaña **Tester**. La entrada opcional de la barra lateral de Escritorio abre el tester sin este panel de opciones; usa Ajustes para estos valores.

## Integración con Playnite

- **Mostrar en la barra lateral:** añade un acceso en Escritorio. Reinicia Playnite después de cambiarlo.
- **Mostrar en el panel superior:** expone el acceso compacto donde el tema sea compatible.
- **Usar ventana adaptada a Fullscreen:** abre los comandos simplificados maximizados y orientados al mando.

## Comportamiento de las pruebas

- Reiniciar los diagnósticos al cambiar de mando.
- Mantener visible el selector con un único mando.
- Activar o desactivar las pruebas de vibración.
- Activar el registro de inputs de forma predeterminada.

## Umbrales y calibración

- Umbral de zona muerta saludable.
- Umbral de drift menor.
- Umbral de atención.
- Umbral de borde del stick usado en la prueba guiada.
- Umbral de pulsación completa del gatillo.
- Duración de la captura del centro en milisegundos.

Los valores están normalizados. Los sticks usan magnitud de `0` a `1` y los gatillos presión de `0` a `1`. La extensión limita valores inseguros o contradictorios al guardar.

Siguiente: [Integración Tester Fullscreen](ES-Integracion-Tester-Fullscreen)
