# Tester

Controller Manager now includes the former Gamepad Tester. Open **Settings > Tester** for the live button map, stick health, latency, diagnostic profile, guided test, input log, device info and options.

SDL GameController sampling runs in `ControllerSessionManager.TesterHost.exe`, not inside Playnite. Closing settings, starting a protected game or unloading the plugin stops that host.

## Desktop

- **Controllers > Test controller** switches to the **Tester** tab and selects that pad.
- An optional sidebar entry opens the same Tester view without the Options tab. Change tester options from **Settings > Tester**.
- If more than one controller is connected, choose it under **Controller** in the left panel. **General test**, **Device info** and the other tester tabs always describe that selected pad.
- **Guided test** keeps the live checklist on the left. Start or stop from the right-hand button; stopping or finishing writes a results list there, with a green check or a red cross per control.
- Uninstall the old **Gamepad Tester** extension. Two plugins cannot register the same Fullscreen `GamepadTester` source.

## Fullscreen theme blocks

Canonical names use `SourceName = ControllerSessionManager`:

- `TesterLauncher`
- `TesterStatusBadge`
- `TesterButtonMap`
- `TesterStickCheck`
- `TesterTriggerCheck`
- `TesterRumblePad`
- `TesterLatencyMini`

Compatibility aliases keep `SourceName = GamepadTester` and the original block names (`GamepadTesterLauncher`, `StatusBadge`, `ButtonMap`, `StickCheck`, `TriggerCheck`, `RumblePad`, `LatencyMini`). See [Theme Integration](EN-Theme-Integration) and `docs/theme-integration/CONTRACT.md`.
