# Controller Manager

Controller Manager identifies and tracks controllers actively used during a game session. It provides five core features: adaptive controller switching for single-player and multiplayer games, built-in gamepad testing, connection notifications, disconnection alerts, and automatic game pausing upon disconnection. It is highly customizable, adapting to both desktop and fullscreen modes while supporting battery status indicators, custom device names, and icons.

It is designed for Desktop, Fullscreen and couch-gaming setups, including single-player games, controller handover and local multiplayer sessions with several controllers.

## Documentation

The complete user guide is available in the project Wiki:

- [English documentation](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki/EN-Overview)
- [Documentación en español](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki/ES-Descripcion-General)
- [Wiki language selector](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki)

The Wiki covers installation, controllers and battery, the Tester, session protection, notifications and the disconnect overlay, theme integration, and troubleshooting.

Developer-oriented architecture, overlay protocol and roadmap documents remain under [`docs/`](docs/).

## Who this extension is for

Controller Manager is mainly intended for people who play from Playnite with a gamepad, especially on a TV or in Fullscreen, and want the library to treat controllers more like a console does.

It is useful when you need to see which pads are connected, test a controller before starting a game, keep a custom name and icon for each pad, or avoid losing progress because a controller disconnected mid-session. It also covers local co-op: each participating pad can be protected independently.

It is not a remapper, a Steam Input replacement, or an in-game overlay that injects into titles. Controller testing and disconnect protection stay outside the game process.

Controller Manager includes the former Gamepad Tester. Uninstall the standalone Gamepad Tester extension so Fullscreen theme blocks are not registered twice.

## Features

- Identify connected controllers once, with a custom name and one of the bundled SVG icons for every known pad.
- Distinguish USB, Bluetooth and wireless-receiver connections when Windows exposes enough information.
- Show coarse battery states using semantic colors; unknown remains explicit instead of inventing a percentage.
- Test buttons, sticks, triggers, rumble, drift, latency and device information from the **Tester** tab, with SDL sampling in a separate host process.
- Open the Tester for a specific pad from **Controllers > Probar**, or use vibration to identify it in your hands.
- Add an adaptive controller and battery indicator to the Playnite Desktop top panel.
- Show configurable connected and disconnected notifications while browsing Playnite Fullscreen.
- Track controllers that receive intentional input immediately before or during game startup instead of treating every connected device as a participant.
- Adapt automatically between normal single-player handover and detected local multiplayer activity.
- Show an external, controller-aware disconnect overlay when a participating controller disappears.
- Optionally send a configurable pause key, or force-pause verified offline games, after a disconnect.
- Store session-protection and pause policies independently for each game.
- Customize notification and overlay colors, size, typography, icons and layout, with live previews in settings.
- Export a privacy-conscious support report and read-only HID diagnostics for unsupported controllers.
- Integrate status, controller information, player slots and Tester blocks into compatible Playnite themes.
- Use Playnite localization dictionaries in 12 languages with English fallback.

## Tester

The Tester lives in **Settings > Tester** and in an optional Desktop sidebar entry. With several pads connected, pick one in the left panel; device info and the rest of the session follow that pad.

Use it to check the button map, stick health and circular range, input latency, rumble motors, an input log and hardware identity (name, VID/PID, layout, mapping). Guided tests and compatibility notes are available from the same view.

SDL GameController sampling runs in `ControllerSessionManager.TesterHost.exe`, not inside Playnite. Closing settings, starting a protected game or unloading the plugin stops that host.

## Session protection and pause

A controller becomes part of the session only after meaningful input. Switching pads in a single-player game normally transfers ownership. Sustained alternating input from several controllers promotes the session to local multiplayer and protects each participant independently.

The game context menu contains two independent submenus:

- **Session protection**: inherit global settings, automatic/adaptive, force local multiplayer, or disable protection.
- **Automatic pause**: inherit global settings, overlay only, force-pause offline with online fallback, send Escape, or send the configured key.

Pause-key delivery is conservative: Controller Manager verifies the foreground process tree and sends the key only once per incident. Force-pause is opt-in, disabled by default, and owned by an external safety lease that resumes the process if the incident resolves, the game ends or communication is lost.

Online-session detection is best effort. Test pause behavior with each game before relying on it.

## Notifications and disconnect overlay

Fullscreen connection notifications are lightweight, topmost, click-through and non-activating. Colors, position, size, duration and icon placement are configurable independently for connected, disconnected and warning states.

The disconnect overlay is a separate external window for an active game session. It identifies the missing controller, explains how to continue and reports whether a pause action was requested. The overlay does not inject into games, install input hooks or load third-party theme code.

Reconnecting the same controller resolves the incident. A different controller connected after the incident is accepted as an intentional replacement; a controller that was already connected must produce a button or stick input so an unrelated local-co-op device cannot silently take over.

## Installation

Download the `.pext` package from [GitHub releases](https://github.com/Naerian/playnite-nx-session-controller-manager/releases/latest) and install it in Playnite. Open the file or drag it into Playnite, then restart when requested.

Requirements: Windows 10 or Windows 11, Playnite 10.x, Playnite SDK 6.16 compatible runtime, and one or more Windows-compatible controllers.

For manual installation during development:

1. Build the project in Release mode.
2. Copy the build output into a folder under Playnite's Extensions directory.
3. Restart Playnite.

Development packages generated under `dist/` are intentionally not committed to the repository.

## Playnite add-on browser

After approval in the Playnite add-on database, Controller Manager can be installed from Playnite's integrated add-on browser.

Direct install URI:

`playnite://playnite/installaddon/ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`

Web add-on page:

`https://playnite.link/addons.html#ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`

## Configuration overview

Open Playnite and go to **Add-ons > Extension settings > Generic > Controller Manager**.

The main sections are:

- **Overview**: current controllers, session status and shortcuts.
- **Controllers**: detected pads, custom names, icons, vibration identify and **Probar**.
- **Tester**: live button map, sticks, latency, input log, device info and tester options.
- **In-game**: session protection, disconnect overlay and pause policies.
- **Appearance**: Desktop indicator, Fullscreen notifications, overlay layout and colors.
- **Advanced**: monitoring, HID diagnostics and the support report.
- **About**: what the extension does and links.

Keep monitoring enabled. Start with automatic/adaptive protection and **Overlay only** pause until you have confirmed which key safely opens each game's pause menu.

## Localization

The plugin uses Playnite localization resource dictionaries under `Localization/`.

It ships German, English, Spanish, French, Italian, Japanese, Korean, Polish, Brazilian Portuguese, Russian, Turkish and Simplified Chinese. Unsupported locales fall back to English.

To add or update a translation, copy an existing locale file, rename it to the target locale, and translate the string values while keeping the same resource keys.

Community translation contributions are welcome.

## Support

Please use [GitHub Issues](https://github.com/Naerian/playnite-nx-session-controller-manager/issues) for reproducible bugs and include the Playnite mode, controller model, connection type, game and exact steps. **Export support report** from Advanced or the Playnite main menu is usually enough; use **Export HID diagnostics** only when device or battery detection needs a hardware-level report, and review that file before sharing it publicly.

If you find the project useful and want to support its development, consider buying me a coffee:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/naerian)
