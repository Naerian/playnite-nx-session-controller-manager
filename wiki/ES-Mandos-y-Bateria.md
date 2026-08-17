# Mandos y batería

## Lista de dispositivos

La tabla muestra nombre detectado, nombre del usuario, icono asignado, conexión, batería, última entrada y acciones disponibles. Los alias e iconos se guardan por identidad de hardware siempre que es posible. Desktop también puede recordar qué mando corresponde a cada jugador XInput para reutilizar su nombre de forma segura en Fullscreen.

Usa la acción de vibración para relacionar una fila con el mando físico. Su disponibilidad depende del dispositivo, el driver y el protocolo activo.

## Tipo de conexión

Controller Session Manager combina metadatos y evidencias de transporte de Windows. USB requiere una ruta cableada, Bluetooth requiere evidencias específicas y wireless representa un receptor o dongle. Algunos drivers ocultan esta diferencia; el plugin prefiere un resultado desconocido o inalámbrico genérico antes que inventar el transporte.

## Niveles de batería

XInput suele ofrecer cuatro niveles: Vacía, Baja, Media y Llena. Se muestran con colores semánticos, pero no se convierten en porcentajes porque la API no aporta esa precisión.

Los mandos Bluetooth se asocian primero con su contenedor físico PnP de Windows. Cuando Windows expone una batería de solo lectura para ese contenedor, la versión 1.0.6 la convierte a los mismos niveles aproximados del resto de la interfaz. Esto permite usar dispositivos Bluetooth como 8BitDo sin descodificar informes propietarios. Se mantiene un fallback Sony HID estricto para informes documentados de DualSense y DualShock 4; los datos Bluetooth deben superar su CRC y el dispositivo debe coincidir con un VID/PID Sony verificado. Algunas rutas de controlador Bluetooth del DualSense no exponen ni ese canal seguro de batería ni vibración respaldada por el proveedor. El plugin deja esas capacidades como no disponibles en vez de enviar informes propietarios especulativos. Los patrones no verificados de receptores no se interpretan. **Desconocido** significa que ningún proveedor seguro devolvió un valor fiable; no significa que la batería esté llena.

El proveedor no representa una marca ni el transporte. Un 8BitDo puede aparecer mediante DInput/HID por Bluetooth y como endpoint compatible con XInput mediante el dongle. El inventario y los callbacks del SDK de Playnite son autoritativos para el estado de conexión. SDL enriquece metadatos en Desktop, XInput monitoriza los slots traducidos y Windows PnP aporta propiedades Bluetooth verificadas. Las observaciones se agrupan por ruta/slot XInput, ruta de dispositivo equivalente o un InstanceId SDL dentro de su propio proveedor; un número coincidente nunca basta para fusionar APIs diferentes.

## Diagnóstico HID

Usa **Avanzado > Exportar diagnóstico HID** cuando un mando no aparezca, se duplique o no muestre batería. El informe hace inventario de interfaces y capacidades relevantes sin enviar comandos del fabricante. Adjunta el archivo a una incidencia junto con modelo, conexión y software del driver.

## Informe de soporte

Usa **Avanzado > Informe de soporte** para el diagnóstico normal de incidencias. Incluye ajustes efectivos, proveedores elegidos, huellas anónimas de los mandos, estado de sesión y los eventos recientes de conexión, pausa e incidencias. Excluye rutas HID, números de serie, carpetas del usuario y contenido de los registros de Playnite. El diagnóstico HID de bajo nivel sí puede contener rutas o números de serie; revísalo antes de publicarlo.
