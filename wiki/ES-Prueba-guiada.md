# Prueba guiada

La prueba guiada realiza una pasada limpia y ordenada por los controles normalizados estándar. Es útil tras una reparación, al comprar un mando usado o para comprobar un botón sospechoso.

## Realizar la prueba

1. Abre **Ajustes > Tester** (o **Mandos > Probar mando**) y selecciona **Abrir prueba guiada**.
2. Inicia la pasada.
3. Pulsa únicamente el control resaltado.
4. Continúa en el orden mostrado hasta alcanzar el 100 %.

El objetivo actual se anima hasta que se detecta el input esperado. Pulsar controles fuera de orden no hace avanzar la prueba. Reiniciarla crea una pasada limpia.

Al detener o terminar aparece un informe con un check verde o una X roja por control.

## Qué comprueba

La pasada cubre los controles estándar de SDL GameController: botones frontales, superiores, gatillos, cruceta, botones de sistema, clics de los sticks y comprobaciones del borde analógico.

Las palancas traseras, botones de perfil, controles LED, gestos del panel táctil, sensores de movimiento, modos de gatillo adaptativo y otras funciones propietarias pueden no estar expuestas por SDL y no son necesarias para completar la prueba.

## Si un paso no avanza

- Comprueba que el esquema en directo reacciona al control.
- Pulsa completamente los gatillos analógicos.
- Mueve el stick solicitado hasta superar el umbral de borde configurado.
- Revisa el estado del mapeo SDL en **Info. dispositivo**.
- Prueba otro modo físico si el mando admite XInput y DInput.

Siguiente: [Sticks, calibración y salud](ES-Sticks-calibracion-y-salud)
