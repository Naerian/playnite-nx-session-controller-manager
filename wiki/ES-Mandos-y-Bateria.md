# Mandos y batería

## Lista de dispositivos

La tabla muestra nombre detectado, nombre del usuario, icono asignado, conexión, batería, última entrada y acciones disponibles. Los alias e iconos se guardan por identidad de hardware siempre que es posible. Desktop también puede recordar qué mando corresponde a cada jugador XInput para reutilizar su nombre de forma segura en Fullscreen.

Usa la acción de vibración para relacionar una fila con el mando físico. Su disponibilidad depende del dispositivo, el driver y el protocolo activo.

## Tipo de conexión

Controller Session Manager combina metadatos y evidencias de transporte de Windows. USB requiere una ruta cableada, Bluetooth requiere evidencias específicas y wireless representa un receptor o dongle. Algunos drivers ocultan esta diferencia; el plugin prefiere un resultado desconocido o inalámbrico genérico antes que inventar el transporte.

## Niveles de batería

XInput suele ofrecer cuatro niveles: Vacía, Baja, Media y Llena. Se muestran con colores semánticos, pero no se convierten en porcentajes porque la API no aporta esa precisión.

Muchos receptores USB propietarios no exponen una colección estándar de batería. Puede existir un protocolo del fabricante, pero debe estudiarse y probarse para cada modelo. **Desconocido** significa que ningún proveedor seguro devolvió un valor fiable; no significa que la batería esté llena.

## Diagnóstico HID

Usa **Avanzado > Exportar diagnóstico HID** cuando un mando no aparezca, se duplique o no muestre batería. El informe inventaría interfaces y capacidades relevantes sin enviar comandos del fabricante. Adjunta el archivo a una incidencia junto con modelo, conexión y software del driver.
