# Changelog

## 1.0.6 — Unreleased test build
- Replaced the Windows color dialog with a themed picker that includes a live preview and an opacity percentage, so transparency is adjustable without editing hex.
- Recovered Windows Bluetooth battery for BLE HID pads such as the 8BitDo Ultimate 2 Wireless, whose percentage lives on a `BTHLE\DEV_{address}` node that does not share a PnP container or VID/PID with the gamepad HID path.
- Recovered Windows Bluetooth battery for XInput-wrapped pads such as 8BitDo, whose `&ig_` HID path is not the device node that Settings uses for the percentage.
- Let the disconnect-overlay preview fill the remaining settings height so the mockup has more room around the card.
- Made the fullscreen and desktop notification rows look expandable, with a configure hint and a chevron control while they are collapsed.
- Restyled fullscreen/desktop notification expanders to match the side navigation, restored Overview cards, and added title underlines plus more page spacing.
- Grouped overlay icon/border checkboxes with their sliders and tightened those rows so the live preview can stay on screen.
- Ignored HID mice/keyboards and leftover generic HID rows so Mandos no longer lists pointers as "Game Controller".
- Tightened the settings appearance layout: controller icons sit unboxed and centered, notification/overlay colors sit in a right-hand column, and preview buttons follow the active Playnite theme.
- Renamed the controller-notification expanders to fullscreen/desktop notification and dropped the Ko-fi helper text so that card is only the support button.
- Stopped every in-process SDL call from this plugin. Playnite's Input setting "Enable game controller API support" already owns the process-wide SDL loop; sharing it was aborting Desktop on controller connect/disconnect.
- Restored connection type, VID/PID and battery from Windows HID/PnP (and XInput battery) so Desktop no longer needs in-process SDL for that metadata.
- Stopped in-process SDL during game sessions and for two seconds after a controller connect/disconnect, and polled XInput before any SDL call so USB/dongle unplug cannot hit Playnite's native event loop.
- Abandoned SDL joystick references without closing them when the XInput topology changes, and never opened native SDL handles for XInput-backed pads.
- Deferred native SDL joystick opens and HID battery reads until a newly connected controller has been seen on a later poll, so turning a pad on in Desktop cannot terminate Playnite the way hot-unplug already could in Fullscreen.
- Contained Playnite controller callbacks, provider polling and settings overview refreshes so a hardware notification cannot take down the UI thread.
- Replaced middle-dot message fragments with complete sentences and rewrote protection/pause labels in plain, action-oriented language.
- Added independent overlay appearance options for the controller-name icon and the pause/warning status icon, including live preview support.
- Displayed the disconnect overlay as soon as a tracked controller becomes suspect while keeping pause actions behind the configured confirmation grace period.
- Prewarmed the external overlay host at game-session startup to remove first-incident process startup latency.
- Accepted a controller newly connected after a disconnect as an intentional replacement even when its connection/Home input is not exposed; controllers already present still require real gameplay input.
- Clarified the takeover instruction to explicitly request a button press or stick movement on an already-connected replacement.
- Split strong online-only metadata from weak TCP evidence: both prevent unsafe forced suspension, but a lone game-owned TCP connection now retains the disconnect overlay instead of hiding it behind a notification.
- Reworded the network safety status to state that forced suspension was skipped without claiming whether the game paused itself.
- Armed one conservative startup controller when a game captures input before Playnite can observe it, preventing protected sessions from starting with zero participants.
- Kept additional connected controllers unassigned until real input is observed, and let the first real input replace the inferred owner immediately for safe single-player and local co-op behavior.
- Recovered controller disconnects while a launched game owns the foreground by requiring three consecutive missing provider samples; only fallback-owned disconnects may be reversed by provider reconnection.
- Correlated Playnite and SDL HID records by unique VID/PID evidence when native paths and instance IDs differ, restoring battery and rumble routing only when the active provider actually exposes those capabilities.
- Inherited intentional input from the ten seconds immediately before game startup so a controller used to launch a title from Desktop is protected even when the game subsequently captures input exclusively.

- Made Playnite SDK inventory and controller callbacks authoritative for connected/disconnected state.
- Relegated XInput, SDL and Windows PnP observations to identity, input, battery, transport and rumble enrichment; supplemental polling can no longer reverse an SDK disconnect or create duplicate rows after SDK initialization.
- Kept XInput as a startup fallback only when the Playnite controller inventory is unavailable.
- Required two missing SDK inventory passes before recovering a missed disconnect, while explicit SDK disconnect callbacks remain immediate.
- Prevented equal numeric SDL instance IDs and XInput slots from being treated as the same controller without path or provider evidence.
- Added lifecycle and capability-provider decisions to diagnostics and the exportable support report.
- Added a read-only Windows Bluetooth PnP battery provider that follows the physical device container and uses the battery value already exposed by Windows.
- Kept the established coarse battery presentation while recording `Windows.BluetoothPnP` as the diagnostic source.
- Removed PID-only 8BitDo transport assumptions and prioritized concrete USB/Bluetooth path evidence because the same model can expose different transports and protocol identities.
- Pumped SDL device events before Desktop inventory refreshes so switching between dongle, cable and Bluetooth is reflected without restarting Playnite.
- Preserved SDL device paths and used them to collapse matching Playnite SDK/DInput observations into one physical controller row.
- Re-resolved the live controller before vibration tests so a stale transport instance is never used after a mode switch.
- Renamed the summary provider label to describe the combined input-provider state instead of implying that every controller is XInput.

## 1.0.0 — 2026-08-16

- Added a privacy-conscious support report with effective settings, anonymized controller identities, provider decisions, current session state and a bounded incident timeline.
- Added a verified Sony HID battery provider for DualSense and DualShock 4 USB/Bluetooth reports, including Bluetooth CRC validation and strict VID/PID matching.
- Kept unsupported receiver protocols explicitly unknown instead of deriving battery values from unverified byte heuristics.
- Added package icon and project links to the Playnite extension manifest.
- Prepared the installer manifest and add-on database submission metadata.
- Expanded the About page, README, English/Spanish Wiki and troubleshooting guidance for the stable 1.0 release.

## 0.5.9 — 2026-08-16

- Added immediate previews for connected, disconnected and warning notifications so every semantic color can be checked independently.
- Added left, right, top, bottom and hidden icon placements for Fullscreen notifications.
- Advanced pending connection notifications on every XInput poll instead of waiting for the five-second reconciliation pass.
- Reduced the stable-state debounce to 300 ms while retaining protection against transient connection flaps.
- Reorganized the public README and added matching English and Spanish Wiki guides for installation, controllers, session protection, presentation, theme integration and troubleshooting.

## 0.5.8 — 2026-08-16

- Persisted the last friendly Desktop identity associated with each XInput player slot.
- Reused that identity, custom name and assigned icon in Fullscreen without making any SDL call.
- Kept an explicit player-number fallback when a slot has never been identified safely in Desktop.

## 0.5.7 — 2026-08-16

- Established a hard process-safety boundary that prevents every SDL initialization, enumeration and input call inside Playnite Fullscreen.
- Retained XInput polling and Playnite-native controller callbacks in Fullscreen for connection state, button evidence and session protection.
- Kept full SDL metadata and non-XInput sampling in Desktop while an out-of-process Fullscreen input provider is designed.

## 0.5.6 — 2026-08-16

- Kept the standalone overlay border-thickness slider aligned with the rest of its group.
- Moved the notification preview action outside the color group so it clearly applies to the complete notification configuration.
- Avoided opening redundant SDL handles for XInput-backed devices and disabled SDL input-handle sampling while browsing Fullscreen.
- Dropped stale SDL handle references without invoking native close operations from the hot-unplug path, preventing a driver-level termination of Playnite.
- Renamed the overlay screen dim to `Backdrop` and documented its alpha-based opacity, including the fully transparent value, in all 12 locales.

## 0.5.5 — 2026-08-16

- Reorganized notification and disconnect-overlay appearance settings into compact, clearly labelled groups.
- Shortened the Fullscreen notification side-navigation label in all 12 locales.
- Allowed a border thickness of zero for both notifications and overlay cards.
- Stopped treating Playnite's transient `XINPUT#n` bridge as an independent physical controller.
- Added an 800 ms stable-state filter that cancels short inverse connection flaps before showing Fullscreen notifications.
- Kept physical XInput slots as the authoritative notification identity so reconnect metadata changes cannot create duplicate or generic-device notices.

## 0.5.4 — 2026-08-16

- Replaced notification and overlay appearance number fields with bounded sliders that show their supported ranges.
- Added configurable notification corner radius and retained independent overlay corner-radius control.
- Reduced the minimum notification width to 300 px and aligned the host duration limits with settings validation.
- Increased vertical breathing room in the compact overlay preview.
- Renamed the desktop top-panel appearance section to the shorter “Quick access”.
- Released SDL game-controller handles before their joystick handles during hot-unplug to prevent native Fullscreen termination, and excluded Guide/Home from Playnite-bridge participation evidence.

## 0.5.3 — 2026-08-16

- Fix Fullscreen toast clipping on displays using Windows DPI scaling by converting WPF device-independent units to physical window bounds.
- Coalesce Playnite `XINPUT#n` bridge entries with their physical XInput slot even when reconnecting changes the transient Playnite instance id.
- Prevent one physical controller from falsely promoting a session to local multiplayer and suppressing its disconnect incident.
- Add visual color-palette buttons for every notification and overlay color while preserving hexadecimal alpha values.
- Add independent overlay title, controller, instruction and status text sizes.
- Add independent controller/status icon sizes, card padding, optional border, border thickness and corner radius.
- Extend the compact live preview to reflect the new typography, icons, padding, border and corner controls.
- Expand deterministic coverage to 19 scenarios and localize 10 new appearance strings across all 12 locales.

## 0.5.2 — 2026-08-16

- Shorten the Session side-navigation label to `Monitoring` in all locales.
- Add independent notification title, message and icon sizes plus inner padding.
- Add an optional semantic-color border with selectable side and thickness.
- Let users hide physical device names and retain a concise generic notification title.
- Add a compact live preview for disconnect-overlay colors and scale.
- Contain snapshot, host-start and IPC failures so controller notifications cannot terminate Playnite Fullscreen.
- Add diagnostic logging around isolated-host startup and skipped controller updates.
- Validate all new visual ranges and localize the new controls across all 12 locales.

## 0.5.1 — 2026-08-16

- Replace the fixed compact toast with a wider, scalable, multiline notification layout.
- Let users configure notification width, scale, duration, corner position, background, text and semantic accent colors.
- Add an immediate notification preview from settings.
- Let users configure disconnect-overlay scale, dimming, card, text, primary accent and warning colors independently.
- Reorganize settings into dedicated Session and Appearance areas with focused side navigation.
- Add adaptive session scope detection that starts as single-player and promotes sustained alternating multi-controller input to local multiplayer.
- Keep a one-off controller switch in single-player mode so selecting the wrong controller does not create a co-op participant.
- Show automatic local-multiplayer promotion in the session summary and retain the manual per-game choice only as an override.
- Validate appearance ranges, position and hexadecimal colors before saving.
- Extend deterministic coverage from 16 to 18 scenarios and localize 30 new presentation and adaptive-session strings in all 12 locales.

## 0.5.0 — 2026-08-16

- Add queued, non-activating controller connection and disconnection notifications to the Playnite Fullscreen interface.
- Add an opt-in offline force-pause mode that suspends only a verified foreground game process.
- Keep forced suspension in the isolated overlay host under an idempotent safety lease.
- Automatically resume on reconnection, controller takeover, session end, graceful shutdown, parent loss or heartbeat timeout.
- Detect strong online-only metadata and established public TCP connections owned by the game process tree.
- Fall back to a lightweight warning notification instead of suspending a game when online activity is detected.
- Keep all online classification explicitly best effort and document UDP, launcher, VPN and telemetry limitations.
- Upgrade the authenticated local overlay protocol to CSM3 and shut the host down gracefully while a suspension lease may exist.
- Add the new settings and runtime messages to all 12 locales with English fallback.
- Expand deterministic coverage to 16 scenarios.

## 0.4.3 — 2026-08-16

- Poll XInput and SDL every 50 ms while a game session is active so brief stick movements cannot fall entirely between inventory samples.
- Retain low-frequency 250 ms polling outside game sessions.
- Add deterministic coverage for the active and idle polling policy, bringing the suite to 14 scenarios.

## 0.4.2 — 2026-08-16

- Ignore SDL's canonical Guide/PS/Home button as gameplay participation, including power-off presses.
- Reduce the stick activation threshold from 12,000 to 8,000 units while retaining baseline-relative drift filtering.
- Reduce the neutral settling gate from 200 to 100 ms for near-immediate takeover after input.
- Replace the awkward release instruction with natural takeover copy in all 12 locales.
- Report the running assembly version in logs instead of a stale hard-coded value.

## 0.4.1 — 2026-08-16

- Compare SDL axes with their connection baseline so shutdown resets do not masquerade as player input.
- Record the accepted input evidence in session diagnostics.
- Keep a takeover overlay visible while the alternative controller still has an active control.
- Require 200 ms of neutral controls before completing a takeover and poll at 100 ms during incidents.
- Explain the release step in all 12 localized takeover instructions.
- Place the assigned controller icon compactly beside the controller name instead of above the message.
- Expand deterministic coverage from 12 to 13 scenarios.

## 0.4.0 — 2026-08-16

- Make Automatic / single player the default session ownership mode.
- Require intentional input before a connected controller can become a session participant.
- Ignore button releases, raw XInput packet changes and minor SDL/XInput analog noise.
- Transfer single-player ownership silently to the most recently used controller.
- Resolve a missing controller automatically from fresh input, including during the grace period.
- In local multiplayer, allow only a new or unassigned controller to replace a missing participant.
- Remove the manual takeover choice and simplify per-game protection to Automatic, Local multiplayer or Disabled.
- Migrate ambiguous legacy protect-all defaults to Automatic while retaining explicit modern overrides.
- Expand deterministic coverage from 8 to 12 session and intentional-input scenarios.
- Explain why local/online game classification cannot be inferred reliably from library metadata.

## 0.3.2 — 2026-08-16

- Split the game context menu into native session-protection and automatic-pause submenus.
- Mark the effective per-game choice with a visible check mark on every supported Playnite theme.
- Let session and pause settings inherit their global values independently without deleting the other override.
- Clarify single-player controller switching, post-disconnect takeover and strict local-multiplayer protection in every locale.
- Preserve existing per-game overrides through a backward-compatible settings migration.
- Document the overlay's fixed high-contrast visual language and why an isolated host does not inherit Playnite theme resources.

## 0.3.1 — 2026-08-16

- Redesign the disconnect card with a smaller layout, clearer hierarchy and a dedicated semantic status badge.
- Use the new pause icon for normal/disabled pause states and the alert icon for skipped or failed pause attempts.
- Replace implementation-oriented status sentences with concise user-facing copy.
- Show controller names on their own line and add localized plural titles and instructions for local multiplayer.
- Add a configurable global pause key and per-game profiles for overlay only, Escape or the configured key.
- Validate safe single keys and explicitly reject modifier combinations.
- Add deterministic tests for unrelated foreground windows, co-op one-shot pause gating and supported key profiles.

## 0.3.0 — 2026-08-16

- Add optional one-shot automatic pause by sending Escape after a confirmed controller disconnection.
- Require the current foreground window to belong to the Playnite-started game process or a descendant before sending input.
- Reject unrelated foreground applications, missing game process identifiers and foreground changes during verification.
- Never change focus, suspend the game process or resume the game automatically.
- Add global and per-game pause policies while keeping pause disabled by default.
- Show whether pause was sent, disabled or safely skipped directly in the disconnect overlay.
- Add deterministic process-tree and native input-layout safety tests.

## 0.2.2 — 2026-08-16

- Add explicit protection scopes for the most recently used controller and every controller used in local multiplayer.
- Keep local multiplayer participants independently protected so one active player cannot dismiss another player's disconnect incident.
- Require fresh input before a controller removed by takeover can re-enter the protected session.
- Prevent stale input timestamps from causing takeover ping-pong after reconnects.
- Add a per-game local multiplayer policy and deterministic session tests for co-op, takeover, stale input and grace-period recovery.
- Keep automatic game pause disabled while the controller state machine is stabilized.

## 0.2.1 — 2026-08-16

- Add a Desktop setting to enable or disable semantic battery coloring for the top-panel indicator.
- Apply the setting jointly to both the controller icon and battery text, matching Audio Switcher behavior.
- Preserve the theme foreground for both elements when battery coloring is disabled.

## 0.2.0 — 2026-08-16

- Add an isolated external WPF overlay for confirmed active-controller disconnections.
- Use a current-user named pipe, per-instance token, bounded messages and parent/heartbeat watchdogs.
- Show the assigned controller icon, localized controller name and the configured same-controller or takeover instruction.
- Close the overlay automatically on reconnection, controller takeover, game stop, monitoring disable or Playnite shutdown.
- Target the monitor containing the game's main window, with a primary-monitor fallback.
- Color both the Desktop top-panel icon and battery text with the semantic battery color when data is available.
- Add a global setting for enabling the informational disconnect overlay.

## 0.1.17 — 2026-08-16

- Commit controller icon selections to their profiles immediately instead of relying on the DataGrid edit lifecycle.
- Make the icon selector binding explicitly two-way with immediate source updates so Save persists the selected icon reliably.

## 0.1.16 — 2026-08-16

- Show only the controller icon in compact Desktop top-panel buttons, using the semantic battery color when available.
- Keep the normal theme foreground for compact controllers whose battery is unavailable.
- Add a separated, localized Settings entry at the end of the Playnite main menu section.
- Confirm through the Dead Island 2 test that active-controller disconnect confirmation and same-controller recovery work as designed.

## 0.1.15 — 2026-08-15

- Adapt the Desktop top-panel indicator to its real runtime container width without theme-specific detection.
- Use a compact battery-only presentation below 58 px, with icon fallback when battery data is unavailable.
- Add a truthful single-line controller and battery tooltip and keep semantic battery colors.
- Shorten per-game context actions and remove the visible `@` prefix from the plugin menu section.
- Document the adaptive indicator in README, Wiki source and theme integration guides.

## 0.1.14 — 2026-08-15

- Resolve confirmed incidents when the same controller reconnects.
- Add an optional takeover policy so a different controller can replace a disconnected session controller after new input.
- Ignore disconnections from controllers that never provided input during the running game.
- Add global takeover settings and per-game policies through Playnite's game context menu.
- Keep gamepad icon choices limited to the consistent Gamepad 1–4 and Nintendo set.

## 0.1.13 — 2026-08-15

- Removed the visually inconsistent Gamepad 5, Gamepad 6 and Gamepad 7 assets and selector entries.
- Migrated profiles using removed or invalid icons back to the Gamepad 4 default.

## 0.1.12 — 2026-08-15

- Made the overview primary-controller card reflect the active game-session controller while a game is running.
- Included disconnected controller names and state in confirmed session incidents.
- Rendered the original filled Gamepad 5–7 silhouettes without double contour lines while keeping line icons normalized.

## 0.1.11 — 2026-08-15

- Normalized all SVG geometries to a common 24-unit canvas for consistent line weight.
- Distinguished DualSense USB and Bluetooth transports using SDL device paths and current Windows USB presence.
- Added game-session lifecycle tracking and sticky active-controller membership based on real post-launch input.
- Added configurable disconnect grace periods with suspected, cancelled and confirmed incident states.
- Added localized session status and active-controller cards to the overview.

## 0.1.10 — 2026-08-15

- Removed row hover and selection visuals from the controller table.
- Added a per-controller vibration test action using XInput or SDL where supported.
- Added Gamepad 5, Gamepad 6 and Gamepad 7 icon choices with filled-SVG rendering.

## 0.1.9 — 2026-08-15

- Centered the Desktop top-panel controller indicator.
- Kept controller-table text readable with the active theme when rows are selected.
- Removed the empty icon choice, renamed the gamepad icons consistently and made Gamepad 4 the default.
- Migrated existing controller profiles without an icon to Gamepad 4.

## 0.1.8 — 2026-08-15

- Added controller Devices/General side navigation and a real Playnite Desktop top-panel controller indicator.
- Reworked the controller table with themed spacing, visible cell separators and connection SVG icons.
- Added battery availability guidance and expanded the About page with author, project and Ko-fi links.
- Added complete localization for the new interface in all 12 supported languages.

## 0.1.7 — 2026-08-15

- Added SVG line-element support so the Gamepad 4 controls render completely.
- Preserved round SVG line caps so small button marks remain visible on Gamepad and Gamepad 4.

## 0.1.6 — 2026-08-15

- Restored localized coarse battery levels: Empty, Low, Medium and Full.
- Added semantic battery colors for faster status recognition in the controller table.
- Kept unknown and unavailable battery readings neutral instead of presenting inferred percentages.

## 0.1.5 — 2026-08-14

- Added input activity tracking for SDL-only controllers such as DualSense.
- Displayed coarse battery readings as honest percentage ranges.
- Added a read-only HID diagnostic exporter with interface, capability and current-report capture.
- Identified the 8BitDo Ultimate 2 vendor interface at MI_02 / Usage Page FF7A for future protocol work.

## 0.1.4 — 2026-08-14

- Fixed mixed XInput/SDL inventories so non-XInput controllers are no longer omitted.
- Matched XInput slots to SDL metadata using SDL player indices instead of enumeration order.
- Added a themed controller table with subtle borders, alternating rows and hover selection states.

## 0.1.3 — 2026-08-14

- Merged detected name, user name and icon assignment into the full-width controller table.
- Defaulted the user-facing name to the detected hardware name.
- Added SDL joystick power as a secondary battery source for dongle-connected controllers.

## 0.1.2 — 2026-08-14

- Added SDL metadata enrichment based on the Gamepad Tester identification rules.
- Corrected supported wireless dongles that XInput incorrectly reports as wired.
- Added persistent custom names and assignable SVG icons per controller hardware identity.
- Matched the overview summary cards to the Audio Switcher and Metadata AI styles.

## 0.1.1 — 2026-08-14

- Added independent XInput monitoring for up to four controllers.
- Fixed controller detection when Playnite's controller bridge is unavailable in Desktop mode.
- Added wired/wireless and battery-state diagnostics where XInput exposes them.
- Redesigned settings with overview, controller, general, advanced and about tabs.
- Expanded all twelve localizations for the new interface.

## 0.1.0 — 2026-08-14

- Added the installable Playnite GenericPlugin foundation.
- Added controller inventory through Playnite's official controller API.
- Added connect, disconnect, last-input and low-frequency reconciliation tracking.
- Added localized settings and diagnostics.
- Added `ControllerStatus`, `ControllerCount` and `PrimaryController` theme elements.
- Added English fallback and translations for the same language set used by Audio Switcher and Metadata AI, including Turkish from Audio Switcher.
- Added reproducible build and `.pext` packaging scripts.
