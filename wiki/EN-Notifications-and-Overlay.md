# Notifications & Overlay

## Fullscreen notifications

Connection notifications are intended for browsing the Playnite Fullscreen interface without an active game. Online safety fallbacks can also use the warning style during gameplay. Toast windows are topmost, click-through, non-activating and close automatically.

Under **Appearance > Fullscreen notification** and **Appearance > Desktop notification**, configure width, scale, duration, screen corner, typography, icon size, padding, border, corner radius, shadow, colors, semantic accent and animation. Inter, Montserrat, Outfit, Poppins, Rajdhani, Chakra Petch and Orbitron ship with the extension and do not need to be installed in Windows. The color picker includes opacity as a percentage. The icon can appear left, right, above, below or be hidden. Each subsection's buttons exercise connected, disconnected, warning and low-battery states through the real notification renderer. Presets replace the old reset action: **Soft** is the neutral baseline and the others provide distinct compositions.

Preset selectors separate plugin presets, imported designs and reviewed creator designs. Creator designs may include advanced layout, fonts, images, state-specific borders and sounds; their authored controls are locked and visually dimmed while active. Theme authors can use the complete [Creator designs guide](EN-Creator-Designs).

Stable XInput changes use a short 300 ms debounce to reject rapid driver flaps without waiting for the slower reconciliation pass.

## Disconnect overlay

The overlay appears after a participating controller remains absent beyond the configured grace period. It shows the missing device, continuation instruction and pause result. Reconnecting or completing an eligible handover closes it; local multiplayer incidents remain until the corresponding player slot is recovered.

The overlay card and full-screen backdrop have independent colors, sizing, icon sizes, padding, border and corner radius. Card width and position, entry animation, shadow and the accented border edge are configurable too. Title, controller name, instruction, and status/badges each have their own font family and weight; title, instruction and pause status can also be hidden independently.

Optional connection and battery badges have independent text, icon, background and border colors, as well as border thickness, corner radius, icon size and text size. Battery text and icon can follow configurable full, medium, low and empty state colors. `#AARRGGBB` values support alpha; `#00000000` makes the backdrop transparent. The compact preview updates while settings change, and presets apply visibly distinct compositions.

## Compatibility and input

The host is a separate WPF process connected through a per-instance authenticated named pipe. It does not activate itself or inject into the game. Borderless and windowed games offer the best compatibility; legacy exclusive fullscreen can render above external windows.

The overlay cannot universally block XInput, Raw Input, Steam Input or GameInput without invasive hooks or virtual drivers. The accelerated takeover path minimizes the delay but does not claim to intercept input sent to the game.
