# Troubleshooting & FAQ

## A controller is missing or duplicated

Refresh the controller list, close remapping tools temporarily and compare the physical device with any virtual XInput device they create. Export HID diagnostics and report the exact controller, connection mode and software such as Steam Input, DS4Windows or reWASD.

## Why is the battery Unknown?

The active driver or receiver did not expose a trustworthy standard value. This is common with proprietary dongles. XInput levels are coarse and cannot be converted honestly into percentages. Windows Bluetooth battery is only used on real Bluetooth HID paths, not on an XInput dongle wrapper.

## Why was a dongle pad shown as Bluetooth?

An XInput wrapper is a cable or 2.4 GHz receiver unless it is an Xbox-licensed pad that also appears on Bluetooth. A sibling BLE HID from the same brand must not relabel the dongle row.

## Why did a generic Game Controller row appear?

Playnite and Windows often expose an unnamed HID collection while the pad is still enumerating. Unknown USB leftovers stay hidden; a known VID/PID is shown with the model name instead.

## Why does Fullscreen show a generic player name?

For stability, the Fullscreen Playnite process never initializes SDL. Open Desktop with the controller connected once so the plugin can associate its friendly identity and custom profile with the XInput slot, save settings, then restart Fullscreen.

## Why did no overlay appear?

Check that monitoring, session tracking and the disconnect overlay are enabled. The controller must receive meaningful input after the game launches and remain absent beyond the grace period. Confirm that the game was started through Playnite and has not been given a disabled per-game policy.

## Why was the game not paused?

Pause is disabled by default. A pause key is skipped if the game process tree cannot be verified as foreground. Force-pause also avoids suspension when online evidence is found. The overlay status explains the result.

## Can the overlay stop another controller from controlling the game behind it?

Not universally. The overlay is intentionally click-through and does not install hooks or virtual drivers. Intercepting every possible input API would add compatibility, stability and anticheat risks. Controller takeover is detected quickly, but some input can still reach the game.

## Which fullscreen modes work?

Windowed and borderless are recommended. A legacy exclusive-fullscreen game can render above external topmost windows. Running the game elevated while Playnite is not elevated can also restrict process verification.

## What should a useful bug report include?

Export **Advanced > Support report** and include it with Playnite mode and theme, controller model, USB/Bluetooth/dongle mode, game, remapping tools, exact reproduction steps, and expected versus actual result. The report already includes the version, anonymized session state and recent incident timeline. Add a reviewed HID diagnostic only when detection or battery is involved.
