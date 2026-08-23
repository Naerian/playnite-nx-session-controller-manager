# Tester settings

Open **Add-ons > Extension settings > Generic > Controller Manager**, then the **Tester** tab. The optional Desktop sidebar entry opens the tester without this Options panel; use Settings for these values.

## Playnite integration

- **Show sidebar item:** adds a Desktop sidebar shortcut. Restart Playnite after changing it.
- **Show top panel item:** exposes the compact top-panel shortcut where supported.
- **Use fullscreen-friendly window:** opens simplified Fullscreen commands in a maximized, controller-oriented window.

## Testing behavior

- Reset diagnostics when switching controller.
- Keep the device selector visible with one controller.
- Enable or disable rumble tests.
- Enable input logging by default.

## Thresholds and calibration

- Healthy deadzone threshold.
- Minor drift threshold.
- Attention drift threshold.
- Stick edge threshold used by guided checks.
- Trigger full-press threshold.
- Center capture duration in milliseconds.

Values are normalized. Stick thresholds use magnitude from `0` to `1`; trigger pressure uses `0` to `1`. The plugin constrains unsafe or contradictory values when settings are saved.

Next: [Fullscreen tester integration](EN-Fullscreen-Tester-Integration)
