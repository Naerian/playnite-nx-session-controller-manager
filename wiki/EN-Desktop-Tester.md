# Desktop tester

Desktop mode contains the complete Tester workflow inside Controller Manager. The active Playnite theme provides the surrounding colors and controls.

Open it from **Settings > Tester**, **Controllers > Test controller**, or the optional Desktop sidebar entry.

## Test dashboard

The main dashboard combines the live controller drawing with the most useful session information:

- Live pressed-state highlights for standard buttons and D-pad directions.
- Analog trigger percentages and current stick position.
- Connected-device identity and detected visual scheme.
- Current inputs, health summary, and compact stick readings.
- Rumble modes when vibration testing is enabled.
- A button that opens the ordered guided test.
- A diagnostic profile radar that summarizes center stability, both sticks, triggers, control coverage, and timing, with instructions for completing each axis.

The visual scheme is selected automatically from the detected controller. You can override it below the controller drawing when identification is incomplete or you prefer another layout.

Dashboard sections use Playnite's standard theme surfaces, borders, text, and accent resources so the embedded sidebar view and the settings tab remain visually consistent with the active Desktop theme.

## Other sections

- **Sticks & Calibration:** paths, circular coverage, center capture, outer range, and exports.
- **Latency:** observed event timing, polling statistics, a fixed-size theme-colored session graph, reset, and export.
- **Input log:** opt-in button-event history with reset and export.
- **Device info:** display name, raw identity, VID/PID, layout, backend, SDL mapping, capabilities, and extra controls.

## Compatibility assistant

The top of Device info evaluates the SDL mapping and the hardware capabilities exposed to Playnite. It reports the inferred input path, standard mapping coverage, missing normalized bindings, suspiciously low axis or button counts, and 8BitDo mode guidance. Unknown XInput/DInput state is reported as unknown rather than treated as a fault.

Use **Export compatibility report** when asking for support. The text report includes the assistant result, missing bindings, SDL GUID, raw mapping, and capability counts without including Playnite library or personal data.

## No controller detected

The diagnostic UI is hidden when no mapped controller is available. Connect a device, reconnect it, or change its hardware input mode. For 8BitDo controllers, XInput is usually the simplest first test; DInput also works when SDL has a compatible mapping.

Open **Settings > Tester** so `ControllerSessionManager.TesterHost.exe` can start. SDL is loaded from Playnite's install folder.

Next: [Guided test](EN-Guided-Test)
