# Installation & Quick Start

## Requirements

- Windows 10 or Windows 11.
- Playnite 10.x.
- One or more Windows-compatible controllers.

## Install

1. Download the latest `.pext` from [Releases](https://github.com/Naerian/playnite-nx-session-controller-manager/releases/latest).
2. Open it or drag it into Playnite.
3. Restart Playnite when requested.

## First setup

Open `Add-ons > Extension settings > Generic > Controller Manager`.

1. In **Controllers**, confirm that each physical controller appears once.
2. Assign optional names and icons, then save.
3. Use **Probar** to open the Tester tab for that pad, or vibration to identify it physically.
4. Keep **Advanced > Monitoring** enabled.
5. Configure the Desktop shortcut, Fullscreen notification and disconnect overlay under **Appearance**.
6. Leave automatic/adaptive protection enabled initially.

If the older **Gamepad Tester** extension is still installed, uninstall it. Both plugins cannot register the same Fullscreen `GamepadTester` theme source.

## First protection test

1. Start an offline single-player game from Playnite.
2. Press buttons or move a stick on the intended controller.
3. Disconnect that controller during gameplay.
4. Confirm that the overlay appears after the configured grace period.
5. Reconnect it, use another available controller when takeover is allowed, or press a key / click to continue with keyboard and mouse in single-player.

Start with **Overlay only**. After confirming the overlay works for that title, try **OfflineOnly** or **Always** per game if you want process suspension. Test each mode before relying on it permanently.

Next: [Controllers & Battery](EN-Controllers-and-Battery), [Tester](EN-Tester) or [Session Protection](EN-Session-Protection).
