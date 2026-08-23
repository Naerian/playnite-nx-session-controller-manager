# Controllers and visual schemes

This page covers how the **Tester** identifies layouts and drawings. For battery, transport and session identity, see [Controllers & Battery](EN-Controllers-and-Battery).

## Input architecture

The Tester uses SDL GameController normalization and prefers Playnite's bundled SDL runtime. Sampling runs in `ControllerSessionManager.TesterHost.exe`. SDL provides one consistent set of controls across mapped XInput and DInput devices.

Normalized labels follow the Xbox convention: `LS`, `RS`, `LB`, `RB`, `LT`, `RT`, `A`, `B`, `X`, and `Y`. PlayStation and Nintendo drawings use their familiar visual symbols where appropriate, while logs retain stable normalized control names.

## Supported families

Automatic identification and layouts cover common Xbox One, Xbox Series/Elite, DualShock, DualSense, Nintendo Switch Pro, 8BitDo, Steam Controller, and generic SDL-compatible devices. Detection depends on the name and VID/PID exposed by the active driver and controller mode.

Available visual schemes include:

- Universal
- Xbox Series X / S
- Xbox One
- PlayStation / DualShock
- DualSense
- Nintendo Switch Pro
- 8BitDo Ultimate
- 8BitDo Pro
- Steam Controller

The visual scheme selector changes only the drawing and labels. It does not change the selected device, input mapping, or driver.

## 8BitDo modes and extra buttons

8BitDo controllers can communicate through XInput or DInput depending on their hardware mode. Both can work when SDL recognizes the device, but the exposed name, VID/PID, button mapping, rumble, and extra controls may differ. XInput is usually the most predictable first option.

Back paddles, profile buttons, LED controls, touchpads, and other vendor controls are shown only when SDL exposes them. The plugin cannot reliably infer proprietary labels that are absent from the API.

Next: [Tester settings](EN-Tester-Settings)
