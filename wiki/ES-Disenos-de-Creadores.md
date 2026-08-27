# Diseños de creadores

Controller Manager permite instalar packs visuales revisados para las notificaciones de mando y el overlay de desconexión. Pueden incluir distribuciones, colores, imágenes, fuentes y sonidos. Se muestran separados de los presets del plugin y los perfiles visuales importados.

## Usar diseños de creadores

- Pulsa **Apariencia → Actualizar diseños** para descargar versiones compatibles desde el catálogo oficial.
- Pulsa **Instalar diseño de creador** para instalar un archivo `.csmtheme` de confianza descargado manualmente, incluido el artefacto de prueba de una pull request.
- Un diseño de creador bloquea y atenúa los controles de apariencia que controla para evitar modificar accidentalmente el resultado del autor.
- Si una actualización no es compatible o no está disponible, Controller Manager conserva la última copia compatible instalada.
- `.csmtheme` no está registrado como tipo de archivo de Windows; se instala desde el panel de configuración, no mediante doble clic.

Antes de instalar un paquete local, Controller Manager muestra su nombre, autor y versión, comprueba la compatibilidad del esquema y del plugin, valida rutas, tamaños, tipos de archivo, recursos y propiedades de apariencia, y solo sustituye una copia existente cuando todo el proceso termina correctamente. Si se cancela o rechaza el paquete, el diseño instalado se conserva.

## Crear o contribuir un diseño

El formato, las plantillas, la referencia de propiedades, las reglas de compatibilidad, las validaciones, el flujo de pull requests y las herramientas de prueba se mantienen en:

- [Repositorio Controller Manager Creator Themes](https://github.com/Naerian/controller-manager-creator-themes)
- [Wiki de Controller Manager Creator Themes](https://github.com/Naerian/controller-manager-creator-themes/wiki)

Para contribuir, haz fork del repositorio de diseños, no del repositorio del plugin. Los diseños aceptados pasan a estar disponibles mediante **Actualizar diseños** sin necesitar una nueva versión de Controller Manager.
