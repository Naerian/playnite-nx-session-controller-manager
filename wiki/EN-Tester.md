# Tester

Controller Manager includes the former Gamepad Tester. Open **Settings > Tester** for the live button map, stick health, latency, diagnostic profile, guided test, input log, device info and options.

SDL GameController sampling runs in `ControllerSessionManager.TesterHost.exe`, not inside Playnite. Closing settings, starting a protected game or unloading the plugin stops that host.

Uninstall the standalone **Gamepad Tester** extension. Two plugins cannot register the same Fullscreen `GamepadTester` source.

## Guides

| Topic | Page |
| --- | --- |
| Desktop workflow | [Desktop tester](EN-Desktop-Tester) |
| Ordered checklist | [Guided test](EN-Guided-Test) |
| Drift, coverage, center | [Sticks, calibration & health](EN-Sticks-Calibration-and-Health) |
| Timing and exports | [Latency, logs & reports](EN-Latency-Logs-and-Reports) |
| Layouts and families | [Controllers & visual schemes](EN-Controllers-and-Visual-Schemes) |
| Tester options | [Tester settings](EN-Tester-Settings) |
| Fullscreen theme blocks | [Fullscreen tester integration](EN-Fullscreen-Tester-Integration) |
| Battery / identity UI | [Theme Integration](EN-Theme-Integration) |

## Desktop

- **Controllers > Test controller** switches to the **Tester** tab and selects that pad.
- An optional sidebar entry opens the same Tester view without the Options tab. Change tester options from **Settings > Tester**.
- If more than one controller is connected, choose it under **Controller** in the left panel. **General test**, **Device info** and the other tester tabs always describe that selected pad.
- **Guided test** keeps the live checklist on the left. Start or stop from the right-hand button; stopping or finishing writes a results list there, with a green check or a red cross per control.

## Fullscreen theme blocks

Canonical names use `SourceName = ControllerSessionManager`:

- `TesterLauncher`
- `TesterStatusBadge`
- `TesterButtonMap`
- `TesterStickCheck`
- `TesterTriggerCheck`
- `TesterRumblePad`
- `TesterLatencyMini`

Compatibility aliases keep `SourceName = GamepadTester` and the original block names (`GamepadTesterLauncher`, `StatusBadge`, `ButtonMap`, `StickCheck`, `TriggerCheck`, `RumblePad`, `LatencyMini`). Details: [Fullscreen tester integration](EN-Fullscreen-Tester-Integration) and `docs/theme-integration/CONTRACT.md`.
