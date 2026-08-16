# Notifications & Overlay

## Fullscreen notifications

Connection notifications are intended for browsing the Playnite Fullscreen interface without an active game. Online safety fallbacks can also use the warning style during gameplay. Toast windows are topmost, click-through, non-activating and close automatically.

Under **Appearance > On-screen notification**, configure width, scale, duration, screen corner, typography, icon size, padding, border, corner radius and colors. The icon can appear left, right, above, below or be hidden. Separate preview buttons exercise the connected, disconnected and warning colors immediately.

Stable XInput changes use a short 300 ms debounce to reject rapid driver flaps without waiting for the slower reconciliation pass.

## Disconnect overlay

The overlay appears after a participating controller remains absent beyond the configured grace period. It shows the missing device, continuation instruction and pause result. Reconnecting or completing an eligible handover closes it; local multiplayer incidents remain until the corresponding player slot is recovered.

The overlay card and full-screen backdrop have independent colors, sizing, typography, icon sizes, padding, border and corner radius. `#AARRGGBB` values support alpha; `#00000000` makes the backdrop transparent. The compact preview updates while settings change.

## Compatibility and input

The host is a separate WPF process connected through a per-instance authenticated named pipe. It does not activate itself or inject into the game. Borderless and windowed games offer the best compatibility; legacy exclusive fullscreen can render above external windows.

The overlay cannot universally block XInput, Raw Input, Steam Input or GameInput without invasive hooks or virtual drivers. The accelerated takeover path minimizes the delay but does not claim to intercept input sent to the game.
