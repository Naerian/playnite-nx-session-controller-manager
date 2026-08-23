# Tester de escritorio

El modo Escritorio contiene el flujo completo del Tester dentro de Controller Manager. El tema activo de Playnite aporta los colores y controles exteriores.

Ábrelo desde **Ajustes > Tester**, **Mandos > Probar mando** o la entrada opcional de la barra lateral de Escritorio.

## Panel Test

El panel principal combina el dibujo del mando con la información más útil de la sesión:

- Iluminación en directo de botones estándar y direcciones de la cruceta.
- Porcentaje de los gatillos y posición actual de ambos sticks.
- Identidad del dispositivo y esquema visual detectado.
- Inputs actuales, resumen de salud y lecturas compactas de los sticks.
- Modos de vibración cuando la prueba está activada.
- Acceso a la prueba guiada ordenada.
- Un perfil de diagnóstico en radar que resume estabilidad del centro, ambos sticks, gatillos, cobertura de controles y tiempos, con instrucciones para completar cada eje.

El esquema visual se selecciona automáticamente a partir del mando detectado. Puedes cambiarlo debajo del dibujo si la identificación no es correcta o prefieres otra distribución.

Las secciones usan las superficies, bordes, textos y colores de acento estándar del tema de Playnite para mantener el mismo aspecto en el SidebarItem y en la pestaña de ajustes.

## Otras secciones

- **Sticks y calibración:** recorridos, cobertura circular, captura del centro, rango exterior y exportación.
- **Latencia:** tiempos observados, estadísticas de polling, gráfica de sesión de tamaño fijo y color temático, reinicio y exportación.
- **Registro de inputs:** historial opcional de eventos, reinicio y exportación.
- **Info. dispositivo:** nombre, identidad original, VID/PID, layout, backend, mapeo SDL, capacidades y controles extra.

## Asistente de compatibilidad

La parte superior de Info. dispositivo evalúa el mapeo SDL y las capacidades que el hardware expone a Playnite. Muestra la ruta de entrada inferida, la cobertura del mapeo estándar, bindings normalizados ausentes, cantidades anormalmente bajas de ejes o botones y recomendaciones para modos de 8BitDo. Si no hay datos suficientes para distinguir XInput de DInput, lo indica como desconocido en vez de tratarlo como un fallo.

Usa **Exportar informe de compatibilidad** cuando necesites soporte. El informe incluye el resultado del asistente, bindings ausentes, GUID de SDL, mapeo original y capacidades, sin incluir la biblioteca de Playnite ni datos personales.

## No se detecta ningún mando

La interfaz de diagnóstico se oculta si no hay un mando mapeado. Conecta o reconecta el dispositivo, o cambia su modo físico. En mandos 8BitDo, XInput suele ser el mejor primer intento; DInput también puede funcionar si SDL dispone de un mapeo compatible.

Abre **Ajustes > Tester** para que arranque `ControllerSessionManager.TesterHost.exe`. SDL se carga desde la carpeta de Playnite.

Siguiente: [Prueba guiada](ES-Prueba-guiada)
