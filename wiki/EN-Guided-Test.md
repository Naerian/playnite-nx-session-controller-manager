# Guided test

The guided test is a clean, ordered pass through the standard normalized controls. It is useful after a repair, when buying a used controller, or when checking a suspicious button.

## Run the test

1. Open **Settings > Tester** (or **Controllers > Test controller**) and select **Open guided test**.
2. Start the pass.
3. Press only the highlighted control.
4. Continue in the displayed order until the progress reaches 100%.

The current target animates until the expected input is observed. Inputs pressed out of order do not advance the test. Restarting creates a clean pass.

Stopping or finishing writes a results list with a green check or a red cross per control.

## What it covers

The pass checks the standard SDL GameController controls: face buttons, shoulders, triggers, D-pad, system buttons, stick clicks, and analog stick edge checks.

Manufacturer-specific paddles, profile buttons, LED controls, touchpad gestures, motion sensors, adaptive-trigger modes, and other non-standard features may not be exposed by SDL and are not required to complete the test.

## If a step does not advance

- Confirm that the live map reacts to the control.
- Fully press analog triggers.
- Move the requested stick far enough to pass the configured edge threshold.
- Check **Device info** for SDL mapping status.
- Try another hardware input mode when the controller supports XInput and DInput.

Next: [Sticks, calibration and health](EN-Sticks-Calibration-and-Health)
