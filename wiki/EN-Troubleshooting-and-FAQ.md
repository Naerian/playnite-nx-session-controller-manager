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

## Why does Playnite warn that Gamepad Tester is still installed?

Uninstall the standalone Gamepad Tester extension. Controller Manager now owns that tester, including the Fullscreen `GamepadTester` source name. Two plugins cannot register the same theme source.

## Why does Tester show no controller?

Open **Settings > Tester** or a Fullscreen tester block so `ControllerSessionManager.TesterHost.exe` can start. SDL is loaded from Playnite's install folder, not inside the plugin process. If the host is missing from the extension folder, reinstall the `.pext`.

## Buttons work in games but not in the Tester

Games or remappers may use a raw HID or vendor API while the Tester uses SDL GameController. Check the mapping status in **Device info**, try another hardware mode, and include the raw name and VID/PID when reporting the problem.

## The health score changes while moving a stick

Current movement and session peaks are displayed, but health is based on stable resting drift. Release both sticks and allow the reading to settle. Reset diagnostics before a controlled test.

## Rumble does not work in the Tester

Confirm rumble is enabled under **Settings > Tester**. Support depends on the controller mode, driver, SDL mapping, and connection type. A device can provide input correctly without exposing rumble through the same API.

## Latency values differ from another tester

Controller Manager measures event timing observed inside Playnite through SDL. Browser WebHID tools, driver utilities, and hardware analyzers measure different layers. Compare modes within the same tool and environment.

## Can it calibrate my controller?

It measures the center and recommends a deadzone, but does not modify system or firmware calibration. Apply changes in the controller utility, Steam Input, driver, emulator, or game that owns the actual deadzone.

More Tester detail: [Tester](EN-Tester).

## What should a useful bug report include?

Export **Advanced > Support report** and include it with Playnite mode and theme, controller model, USB/Bluetooth/dongle mode, game, remapping tools, exact reproduction steps, and expected versus actual result. The report already includes the version, anonymized session state and recent incident timeline. Add a reviewed HID diagnostic only when detection or battery is involved.
