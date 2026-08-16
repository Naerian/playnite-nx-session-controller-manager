# Troubleshooting & FAQ

## A controller is missing or duplicated

Refresh the controller list, close remapping tools temporarily and compare the physical device with any virtual XInput device they create. Export HID diagnostics and report the exact controller, connection mode and software such as Steam Input, DS4Windows or reWASD.

## Why is the battery Unknown?

The active driver or receiver did not expose a trustworthy standard value. This is common with proprietary dongles. XInput levels are coarse and cannot be converted honestly into percentages.

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

Include Playnite mode and theme, plugin version, controller model, USB/Bluetooth/dongle mode, game, remapping tools, exact reproduction steps, expected and actual result, and a HID diagnostic when detection or battery is involved.
