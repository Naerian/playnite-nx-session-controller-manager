# Mandos y batería

## Lista de dispositivos

La página **Mandos** muestra el nombre detectado, el alias opcional, el icono, el tipo de conexión, la batería y el proveedor como badges del tema. Los alias e iconos se guardan por identidad de hardware siempre que es posible. Desktop también puede recordar qué mando corresponde a cada jugador XInput para reutilizar su nombre de forma segura en Fullscreen.

Usa la acción de vibración para relacionar una fila con el mando físico. Su disponibilidad depende del dispositivo, el driver y el protocolo activo.

Los restos HID USB sin nombre, como una fila genérica **Game Controller**, se ocultan hasta que Windows o Playnite identifican el mando. Un VID/PID conocido sustituye ese marcador por el nombre del modelo.

## Tipo de conexión

Controller Session Manager combina metadatos y evidencias de transporte de Windows. USB requiere una ruta cableada, Bluetooth requiere evidencias específicas y wireless representa un receptor o dongle. Un wrapper XInput (`&ig_`) se trata como cable o dongle de 2,4 GHz salvo que sea un mando con licencia Xbox que también aparezca por Bluetooth. Algunos drivers ocultan esta diferencia; el plugin prefiere un resultado desconocido o inalámbrico genérico antes que inventar el transporte.

## Niveles de batería

XInput suele ofrecer cuatro niveles: Vacía, Baja, Media y Llena. Se muestran con colores semánticos en el badge de batería, pero no se convierten en porcentajes porque la API no aporta esa precisión.

La batería Bluetooth de Windows solo se lee en rutas HID Bluetooth reales. Los gamepads BLE pueden guardar el porcentaje en un nodo `BTHLE\DEV_{dirección}` distinto que comparte la dirección Bluetooth con la ruta HID; el plugin relaciona esos nodos hermanos sin descodificar informes propietarios. Esto permite usar dispositivos Bluetooth como 8BitDo. Un wrapper XInput de dongle o cable no hereda esa lectura BLE, así que un valor Media no puede quedarse pegado al cambiar al receptor. Los mandos con licencia Xbox sí pueden usar XInput por Bluetooth y entonces conservan la API de batería XInput.

Se mantiene un fallback Sony HID estricto para informes documentados de DualSense y DualShock 4; los datos Bluetooth deben superar su CRC y el dispositivo debe coincidir con un VID/PID Sony verificado. Algunas rutas de controlador Bluetooth del DualSense no exponen ni ese canal seguro de batería ni vibración respaldada por el proveedor. El plugin deja esas capacidades como no disponibles en vez de enviar informes propietarios especulativos. Los patrones no verificados de receptores no se interpretan. **Desconocido** significa que ningún proveedor seguro devolvió un valor fiable; no significa que la batería esté llena.

El badge de proveedor no representa una marca ni el transporte. Un 8BitDo puede aparecer mediante DInput/HID por Bluetooth y como endpoint compatible con XInput mediante el dongle. El inventario y los callbacks del SDK de Playnite son autoritativos para el estado de conexión. XInput monitoriza los slots traducidos y Windows PnP aporta propiedades Bluetooth verificadas. Las observaciones se agrupan por ruta/slot XInput o ruta de dispositivo equivalente; un número coincidente nunca basta para fusionar APIs diferentes.

## Diagnóstico HID

Usa **Avanzado > Exportar diagnóstico HID** cuando un mando no aparezca, se duplique o no muestre batería. El informe hace inventario de interfaces y capacidades relevantes sin enviar comandos del fabricante. Adjunta el archivo a una incidencia junto con modelo, conexión y software del driver.

## Informe de soporte

Usa **Avanzado > Informe de soporte** para el diagnóstico normal de incidencias. Incluye ajustes efectivos, proveedores elegidos, huellas anónimas de los mandos, estado de sesión y los eventos recientes de conexión, pausa e incidencias. Excluye rutas HID, números de serie, carpetas del usuario y contenido de los registros de Playnite. El diagnóstico HID de bajo nivel sí puede contener rutas o números de serie; revísalo antes de publicarlo.
