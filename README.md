# Controller Session Manager

Controller Session Manager is a Playnite extension that identifies connected game controllers, shows their status and battery when Windows exposes it, and protects active game sessions when a controller disconnects.

It is designed for Desktop, Fullscreen and couch-gaming setups, including single-player games, controller handover and local multiplayer sessions with several controllers.

## Features

- Detect XInput controllers and enrich their identity with safe Desktop-side device metadata.
- Keep a custom name and one of the bundled SVG icons for every known controller.
- Distinguish USB, Bluetooth and wireless-receiver connections when Windows exposes enough information.
- Show coarse battery states using semantic colors; unknown remains explicit instead of inventing a percentage.
- Test vibration from the controller table.
- Add an adaptive controller and battery indicator to the Playnite Desktop top panel.
- Show configurable connected and disconnected notifications while browsing Playnite Fullscreen.
- Track controllers that receive intentional input after a game starts instead of treating every connected device as a participant.
- Adapt automatically between normal single-player handover and detected local multiplayer activity.
- Show an external, controller-aware disconnect overlay when a participating controller disappears.
- Optionally send a configurable pause key after verifying that the game still owns the foreground window.
- Optionally force-pause verified offline games through a watchdog-backed external host.
- Fall back to a non-blocking warning when the game appears to have an online session.
- Store session-protection and pause policies independently for each game.
- Customize notification and overlay dimensions, typography, icons, borders, corner radii, backdrop and semantic colors.
- Preview connected, disconnected and warning notifications, plus the disconnect overlay, from settings.
- Export read-only HID diagnostics for unsupported controllers and battery investigations.
- Integrate status, controller information and player slots into compatible Playnite themes.
- Use Playnite localization dictionaries in 12 languages with English fallback.

## Requirements

- Windows 10 or Windows 11.
- Playnite 10.x.
- Playnite SDK 6.16 compatible runtime.

## Installation

### Playnite add-on browser

Controller Session Manager is still under active development. Once it is published in the Playnite add-on database, this section will contain its direct installation link.

### Manual installation

1. Download the latest `.pext` from the [GitHub releases page](https://github.com/Naerian/playnite-nx-session-controller-manager/releases/latest).
2. Open the file or drag it into Playnite.
3. Restart Playnite when requested.

Development packages are generated under `dist/` and are intentionally not committed to the repository.

## Quick Start

Open:

`Add-ons > Extension settings > Generic > Controller Session Manager`

1. Open **Controllers > Devices** and confirm that every controller appears once.
2. Give each controller an optional custom name and choose its icon.
3. Use the vibration button to verify which physical controller corresponds to each row.
4. Under **Session > Monitoring**, keep monitoring enabled.
5. Under **Appearance**, configure the Desktop quick-access indicator, Fullscreen notifications and disconnect overlay.
6. Start a game, use the intended controller, disconnect it and verify the selected protection behavior.

The default automatic mode starts with the controller that receives meaningful input. A normal switch to another connected controller transfers ownership. Sustained alternating input from several controllers promotes the session to local multiplayer and protects each participant independently.

## Session protection and pause modes

The game context menu contains two independent submenus:

- **Session protection**: inherit global settings, automatic/adaptive, force local multiplayer, or disable protection.
- **Automatic pause**: inherit global settings, overlay only, force-pause offline with online fallback, send Escape, or send the configured key.

The effective choice is marked with a check. Per-game overrides are useful for titles with unusual controller behavior, but normal single-player handover and local multiplayer detection are automatic.

Pause-key delivery is conservative: Controller Session Manager verifies the foreground process tree and sends the key only once per incident. Force-pause is opt-in, disabled by default and owned by an external safety lease that resumes the process if the incident resolves, the game ends or communication is lost.

## Notifications and disconnect overlay

Fullscreen connection notifications are lightweight, topmost, click-through and non-activating. They support independent connected, disconnected and warning colors; configurable position, size, duration, padding and border; and icon placement on the left, right, top, bottom or hidden.

The disconnect overlay is a separate external window intended for an active game session. It identifies the missing controller, explains how to continue and reports whether a pause action was requested, skipped or failed. Its backdrop and card presentation are fully configurable. The overlay does not inject into games, install input hooks or load third-party theme code.

## Desktop top-panel indicator

The optional Desktop indicator locates Playnite's internal `TopPanelItem` container by walking the visual tree and measuring its real width. It does not identify themes by name.

- At 58 px or more, it can show the assigned controller icon and available battery label.
- Below 58 px, it uses the icon-only compact presentation.
- An optional setting applies the semantic battery color to both icon and text.
- Clicking the indicator opens Controller Session Manager settings.

## Battery and controller limitations

Battery reporting depends on the protocol, receiver, firmware and driver. XInput commonly exposes only `Empty`, `Low`, `Medium` and `Full`; many USB receivers expose no standard battery channel at all. Controller Session Manager therefore keeps unavailable values as **Unknown** and never converts coarse levels into invented percentages.

Controller identity is strongest in Desktop, where safe metadata enrichment is available. Fullscreen deliberately avoids SDL initialization and native SDL calls because some driver hot-unplug paths can terminate Playnite without a managed exception. Previously learned XInput-slot associations preserve friendly names and custom icons across that safety boundary.

Online-session detection is necessarily best effort. Strong online-only metadata and established public TCP connections from the game process tree are treated as online evidence. UDP-only games, launchers, VPNs, telemetry and unusual process models can produce false positives or false negatives, so pause behavior should be tested per game.

## Documentation

- [Documentation in English](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki/EN-Overview)
- [Documentación en español](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki/ES-Descripcion-General)
- [Installation and quick start](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki/EN-Installation-and-Quick-Start)
- [Session protection](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki/EN-Session-Protection)
- [Notifications and overlay](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki/EN-Notifications-and-Overlay)
- [Theme integration](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki/EN-Theme-Integration)
- [Troubleshooting and FAQ](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki/EN-Troubleshooting-and-FAQ)

Developer-oriented architecture, provider research, overlay protocol and roadmap documents remain under [`docs/`](docs/).

## Localization

The plugin ships Playnite resource dictionaries for German, English, Spanish, French, Italian, Japanese, Korean, Polish, Brazilian Portuguese, Russian, Turkish and Simplified Chinese. Unsupported locales fall back to English. Translation contributions are welcome.

## Support

Please use [GitHub Issues](https://github.com/Naerian/playnite-nx-session-controller-manager/issues) for reproducible bugs and include the Playnite mode, controller model, connection type, game and exact steps. The **Export HID diagnostics** action provides a read-only hardware report when device or battery detection needs investigation.

If you find the project useful and want to support its development, consider buying me a coffee:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/naerian)
