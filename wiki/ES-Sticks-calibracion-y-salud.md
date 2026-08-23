# Sticks, calibración y salud

El Tester muestra los ejes entre `-1,0` y `1,0`, y calcula magnitud, ángulo, drift en reposo, cobertura circular y rango observado.

## Drift en reposo y salud

La salud utiliza movimiento estable observado cuando los sticks están en reposo. El movimiento normal durante el uso y los picos de la sesión no reducen la estimación. Suelta ambos sticks durante unos segundos antes de valorar el resultado.

La puntuación de salud permanece sin evaluar mientras el tester reúne suficientes muestras estables en reposo. La etiqueta de confianza pasa de recopilando a media o alta cuando se alcanza el mínimo necesario. Así, una lectura inicial o el movimiento normal del stick no se presentan como un resultado definitivo.

Los umbrales predeterminados son conservadores:

- Menos de `0,08`: zona muerta saludable.
- De `0,08` a `0,14`: movimiento seguro o pequeño.
- De `0,14` a `0,20`: drift menor.
- Más de `0,20`: conviene revisar el mando.

Estos valores se pueden cambiar en **Ajustes > Tester**. Una lectura alta también puede deberse a tocar el stick, una superficie inestable, el arranque del mando o un problema de modo o controlador.

## Captura del centro

Coloca el mando sobre una superficie estable, no toques los sticks y selecciona **Capturar centro**. La extensión muestrea la posición real de reposo durante el tiempo configurado y recomienda una zona muerta.

El proceso es únicamente diagnóstico. No escribe ajustes en Windows, Steam Input, el firmware ni los juegos.

## Cobertura circular y rango

Selecciona **Probar sticks** y gira cada stick lentamente por todo su borde exterior. El recorrido muestra el movimiento reciente, la cobertura circular registra los sectores del borde alcanzados por encima del umbral de medición y la calidad de rango conserva los mínimos y máximos de cada eje.

La sesión permanece activa mientras alguno de los sticks esté por debajo del 100 % de cobertura circular. Se detiene al seleccionar **Detener prueba de sticks**, cuando ambos llegan al 100 % o al alcanzar el límite de seguridad de 1.800 muestras. El recorrido y la cobertura permanecen visibles hasta que se reinician.

La confianza de la medición y la cobertura circular son valores distintos. Una confianza alta indica que hay datos suficientes para considerar fiable el resultado actual; no significa que se hayan completado todas las direcciones. Usa los reinicios independientes antes de repetir una parte concreta.

La confianza del rango depende tanto del número de muestras como de las direcciones exploradas. Recorre toda la circunferencia en lugar de repetir una sola dirección; el porcentaje se considera provisional hasta cubrir suficientes sectores.

**Exportar sticks** guarda las mediciones mediante el diálogo estándar Guardar como.

Siguiente: [Latencia, registro e informes](ES-Latencia-registro-e-informes)
