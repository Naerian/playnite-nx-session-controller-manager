# Latency, logs and reports

## Latency test

Select **Start latency** and press controller buttons repeatedly. The tester records observed intervals between input changes and updates the current, average, minimum, maximum, sample count, duration, and polling graph until you stop the session.

The graph has a stable layout and uses the active Playnite theme accent. Sampling remains frozen after you stop the test, so the final figures and history can be reviewed or exported without continuing to change.

Use it to compare the same controller in wired, Bluetooth, receiver, XInput, or DInput modes under similar conditions.

> This is an application-level observation through SDL and Playnite. It is not a laboratory measurement of end-to-end USB, display, or game latency.

Reset clears the latency session. Export is available after stopping and when samples exist.

Latency confidence is based on the number of observed input changes. Keep pressing controls during the test until the indicator reaches a useful confidence level; very short sessions are intentionally shown as provisional.

## Input log

Input logging is disabled by default to keep normal testing light and uncluttered. Enable it in the **Input log** section when you need a detailed event trail. Each row identifies the control, state, and event timing.

- **Reset log** clears the current session.
- **Export input log** opens a Save As dialog.
- Closing the tester clears the in-memory log.

## Reports

The Test dashboard can export a broader diagnostic report. Stick and latency sections also provide focused exports.

**Device Info > Export compatibility report** creates a technical text report containing the controller name, VID/PID, detected layout, SDL runtime version, SDL GUID, mapping string, exposed axes/buttons/hats, normalized state, and diagnostic confidence. It does not include Playnite library, account, game, or user-profile data. Device identifiers and mapping strings can still identify a hardware model, so review the file before posting it publicly.

For broader Controller Manager issues (session protection, battery, overlay), prefer **Advanced > Support report**.

Next: [Controllers and visual schemes](EN-Controllers-and-Visual-Schemes)
