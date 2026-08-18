# Controller Session Manager

Controller Session Manager is a Playnite extension that identifies connected game controllers, shows their status and battery when Windows exposes it, and protects active game sessions when a controller disconnects.

It is designed for Desktop, Fullscreen and couch-gaming setups, including single-player games, controller handover and local multiplayer sessions with several controllers.

## Features

- Combine Playnite SDK controller events, XInput-compatible slots and safe Desktop-side SDL metadata without presenting each API observation as a separate physical controller.
- Keep a custom name and one of the bundled SVG icons for every known controller.
- Distinguish USB, Bluetooth and wireless-receiver connections when Windows exposes enough information.
- Show coarse battery states using semantic colors; unknown remains explicit instead of inventing a percentage.
- Test vibration from the controller table.
- Add an adaptive controller and battery indicator to the Playnite Desktop top panel.
- Show configurable connected and disconnected notifications while browsing Playnite Fullscreen.
- Track controllers that receive intentional input immediately before or during game startup instead of treating every connected device as a participant.
- Adapt automatically between normal single-player handover and detected local multiplayer activity.
- Show an external, controller-aware disconnect overlay when a participating controller disappears.
- Optionally send a configurable pause key after verifying that the game still owns the foreground window.
- Optionally force-pause verified offline games through a watchdog-backed external host.
- Avoid forced suspension when network evidence is present; only strong online-only metadata uses the non-blocking notification path, while weak TCP evidence retains the disconnect overlay.
- Store session-protection and pause policies independently for each game.
- Customize notification and overlay dimensions, typography, icons, borders, corner radii, backdrop and semantic colors.
- Preview connected, disconnected and warning notifications, plus the disconnect overlay, from settings.
- Export read-only HID diagnostics for unsupported controllers and battery investigations.
- Export a privacy-conscious support report containing effective settings, provider decisions, anonymized controller identities, session state and the latest incident timeline.
- Integrate status, controller information and player slots into compatible Playnite themes.
- Use Playnite localization dictionaries in 12 languages with English fallback.

## Requirements

- Windows 10 or Windows 11.
- Playnite 10.x.
- Playnite SDK 6.16 compatible runtime.

## Installation

### Playnite add-on browser

The add-on database manifest is prepared for submission. Until it is accepted, install the release package manually from GitHub.

### Manual installation

1. Download the latest `.pext` from the [GitHub releases page](https://github.com/Naerian/playnite-nx-session-controller-manager/releases/latest).
2. Open the file or drag it into Playnite.
3. Restart Playnite when requested.

Development packages are generated under `dist/` and are intentionally not committed to the repository.

## Quick Start

Open:

`Add-ons > Extension settings > Generic > Controller Session Manager`

1. Open **Controllers** and confirm that every controller appears once.
2. Give each controller an optional custom name and choose its icon.
3. Use the vibration button to verify which physical controller corresponds to each row.
4. Under **Advanced > Monitoring**, keep monitoring enabled.
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

The disconnect overlay is a separate external window intended for an active game session. It identifies the missing controller, explains how to continue and reports whether a pause action was requested, skipped or failed. Its backdrop and card presentation are fully configurable, including independent visibility controls for the controller-name icon and the pause/warning status icon. The overlay does not inject into games, install input hooks or load third-party theme code.

The host is prepared when a protected game session begins, and the overlay appears during the suspected-disconnect phase. Pause or process-suspension actions still wait for the configured confirmation grace period. Reconnecting the same controller resolves the incident. A different controller connected after the incident is accepted as an intentional replacement; a controller that was already connected must produce a button or stick input so an unrelated local-co-op device cannot silently take over.

## Desktop top-panel indicator

The optional Desktop indicator locates Playnite's internal `TopPanelItem` container by walking the visual tree and measuring its real width. It does not identify themes by name.

- At 58 px or more, it can show the assigned controller icon and available battery label.
- Below 58 px, it uses the icon-only compact presentation.
- An optional setting applies the semantic battery color to both icon and text.
- Clicking the indicator opens Controller Session Manager settings.

## Battery and controller limitations

Battery reporting depends on the protocol, receiver, firmware and driver. XInput and documented Sony HID reports remain the primary sources. Windows Bluetooth battery is used only for real Bluetooth HID paths, including BLE nodes that store the percentage on a sibling `BTHLE\DEV_{address}` device. XInput wrappers (`&ig_`) for dongles and cables are not labelled Bluetooth just because a BLE interface exists, and they do not keep a cached BLE reading after you switch transport. Xbox-licensed pads are the exception that can speak XInput over Bluetooth. USB receivers that expose no trustworthy Windows or protocol value remain **Unknown**. Controller Session Manager never converts coarse levels into invented percentages.

Controller Session Manager does not equate XInput with an Xbox-branded USB controller. A controller such as an 8BitDo can use DInput/HID over Bluetooth, expose an XInput-compatible endpoint through its dongle, and use another identity over USB. The provider column describes the observation API; the connection column describes the detected transport.

## Support and diagnostics

Use **Advanced > Support report** or the Playnite main menu to export a text report suitable for an issue. It contains the plugin and environment versions, effective protection settings, selected controller providers, anonymized device fingerprints, current session state and a bounded timeline of recent connection, pause and incident events. It deliberately excludes HID paths, serial numbers, user folders and Playnite log contents.

The separate HID diagnostic remains available for protocol investigations. Unlike the normal support report it is intentionally low-level and can contain device paths or serial data, so review it before sharing publicly.

Controller identity is strongest in Desktop, where safe metadata enrichment is available. Fullscreen deliberately avoids SDL initialization and native SDL calls because some driver hot-unplug paths can terminate Playnite without a managed exception. Previously learned XInput-slot associations preserve friendly names and custom icons across that safety boundary.

Playnite's controller inventory and connection callbacks are the normal lifecycle authority. XInput, SDL and Windows PnP enrich that record with input evidence, names, transport, battery and rumble capabilities. If Playnite misses a callback while a game owns the foreground, three consecutive missing samples from the already-associated provider can recover the disconnect. That provider may only reconnect a state it declared itself and can never reverse an explicit SDK disconnect. A completely empty or unavailable SDK registry retains a bounded fallback.

Online-session detection is necessarily best effort. Strong online-only metadata and established public TCP connections from the game process tree both prevent forced suspension. A TCP connection alone is weak evidence—it may be telemetry or a platform service—so it no longer hides the disconnect overlay. Only strong online-only metadata selects the notification-only path. UDP-only games, launchers, VPNs, telemetry and unusual process models can still produce false positives or false negatives, so pause behavior should be tested per game.

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
