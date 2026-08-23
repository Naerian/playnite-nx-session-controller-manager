# Sticks, calibration and health

The Tester reports stick axes from `-1.0` to `1.0` and calculates magnitude, angle, resting drift, circular coverage, and observed range.

## Resting drift and health

Health uses stable movement observed while the sticks are at rest. Normal gameplay movement and session peak values do not reduce the health estimate. Let go of both sticks for a few seconds before judging the result.

The health score remains unavailable while the tester collects enough stable rest samples. The confidence label changes from collecting to medium or high after the minimum sample requirement is met. This prevents an early startup reading or ordinary stick movement from being presented as a final health result.

Default drift bands are deliberately conservative:

- Below `0.08`: healthy deadzone.
- `0.08` to `0.14`: safe or small movement.
- `0.14` to `0.20`: minor drift.
- Above `0.20`: attention recommended.

These thresholds can be changed in **Settings > Tester**. A high reading can also come from touching the stick, an unstable surface, controller startup, or a mode/driver issue.

## Center capture

Place the controller on a stable surface, do not touch the sticks, and select **Capture center**. The plugin samples the real resting position for the configured duration and recommends a deadzone.

This is diagnostic only. It does not write calibration values to Windows, Steam Input, firmware, or a game.

## Circular coverage and range

Select **Test sticks**, then rotate each stick slowly around its full outer edge. The path shows recent movement, circular coverage tracks edge sectors reached above the measurement threshold, and range quality records the minimum and maximum axes.

The session remains active while either stick is below 100% circular coverage. It stops when you select **Stop stick test**, when both sticks reach 100%, or when the 1,800-sample safety limit is reached. The collected path and coverage remain visible until reset.

Measurement confidence and circular coverage are different values. High confidence means enough data exists to trust the current result; it does not mean every direction has been completed. Use the dedicated reset controls before repeating one part of the test.

Range confidence depends on both sample count and explored directions. Move around the complete circumference instead of repeatedly pushing only one direction; the quality percentage is treated as provisional until enough sectors have been explored.

**Export sticks** saves the session measurements through a standard Save As dialog.

Next: [Latency, logs and reports](EN-Latency-Logs-and-Reports)
