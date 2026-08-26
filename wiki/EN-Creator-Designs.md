# Creator designs

Controller Manager supports reviewed creator packs that can restyle Desktop and Fullscreen notifications, the disconnect overlay, fonts, images and notification sounds. Packs are data-only: they cannot execute code or arbitrary XAML.

The exhaustive property tables are maintained in the [canonical creator reference](https://github.com/Naerian/playnite-nx-session-controller-manager/blob/master/docs/CREATOR-THEMES.md).

## Capabilities

- Separate notification designs for Desktop and Fullscreen.
- A disconnect-overlay design.
- Layout, typography, colors, state surfaces, images, borders, gradients and glow.
- Pack-local fonts without requiring a Windows font installation.
- Custom connected, disconnected, low-battery and warning sounds.
- Associations with exact Playnite Desktop and Fullscreen theme IDs.

When a creator design is selected, its normal appearance controls are locked and dimmed. Authored notification sounds also lock the normal sound editor. Users can always return to **Custom** or a plugin preset.

## Pack structure

```text
CreatorThemes/
  MyCreatorPack/
    manifest.json
    notification.json       optional
    overlay.json            optional
    Images/                 optional
    Fonts/                  optional
    Audio/                  optional
```

At least one appearance JSON file must contain supported properties. Assets must stay inside the pack directory; escaping paths are rejected.

## Manifest

```json
{
  "Id": "author.theme.variant",
  "Name": "Theme Variant",
  "Author": "Author name",
  "Version": "1.0.0",
  "Description": "Short description.",
  "RecommendedTheme": "Theme display name",
  "DesktopThemeIds": ["desktop-theme-id"],
  "FullscreenThemeIds": ["fullscreen-theme-id"],
  "Fonts": [
    { "Id": "Heading", "Name": "Pack Heading", "Family": "Actual Font Family", "Folder": "Fonts" }
  ],
  "Sounds": {
    "Connected": "Audio/connected.wav",
    "Disconnected": "Audio/disconnected.wav",
    "LowBattery": "Audio/low-battery.wav",
    "Warning": "Audio/warning.wav"
  }
}
```

`Id`, `Name` and `Author` are required. Keep the ID unique and stable; changing it creates a new design for existing users.

Copy compatible IDs from the Playnite theme's `theme.yaml`:

- `DesktopThemeIds` controls Desktop filtering.
- `FullscreenThemeIds` controls Fullscreen filtering.
- `ThemeIds` is valid only when the same ID genuinely identifies both modes.
- `RecommendedTheme` is descriptive and a compatibility fallback when Playnite exposes a display name.

The user's **Show only creator designs for the current theme** option uses these IDs. The overlay accepts a match from either configured mode.

## Notification designs

Fullscreen properties use the `Notification` prefix. Desktop properties use `DesktopNotification`. One file can define either or both destinations.

```json
{
  "NotificationWidth": 560,
  "NotificationPosition": "TopRight",
  "NotificationBackgroundColor": "#F20D121A",
  "NotificationTextColor": "#FFFFFFFF",
  "NotificationSecondaryTextColor": "#FFD2D8E2",
  "NotificationTitleFontFamily": "$font:Heading",
  "NotificationTitleFontWeight": "SemiBold",
  "NotificationUseGradient": true,
  "NotificationGradientColor": "#F21A2633",
  "NotificationGradientAngle": 135,
  "NotificationShowBorder": true,
  "NotificationUseBorderGradient": true,
  "NotificationBorderGradientStartColor": "#99FFFFFF",
  "NotificationBorderGradientEndColor": "#FF55B8FF",
  "NotificationBorderGradientAngle": 45,
  "NotificationShowBorderGlow": true,
  "NotificationBorderGlowColor": "#9955B8FF",
  "NotificationBorderGlowBlur": 22,
  "NotificationBorderGlowOpacity": 70
}
```

### State-specific borders

Creators can style borders independently from icon/accent colors:

```json
{
  "NotificationUseStateBorderColors": true,
  "NotificationConnectedBorderColor": "#FF55D68B",
  "NotificationDisconnectedBorderColor": "#FF55B8FF",
  "NotificationWarningBorderColor": "#FFFFC857",
  "NotificationLowBatteryBorderColor": "#FFFF5D6C"
}
```

A solid border uses the state color directly. A gradient keeps `BorderGradientStartColor` as its common start and uses the current state color as its end. Desktop uses the same suffixes with the `DesktopNotification` prefix.

Notification packs can control:

- Width, scale, duration, position, screen margin and animation.
- Icon position, size, container, padding and spacing.
- Padding, element spacing, title/message order and alignment.
- Title, controller name, connection badge and message line count.
- Independent title/message families, weights and sizes.
- Solid, gradient or image surfaces with crop alignment, opacity and tint.
- Corner radius, full or independent side widths, gradient and border glow.
- Independent connected, disconnected, warning and low-battery backgrounds and borders.

**Advanced design** is hidden from the normal UI, but all advanced properties remain supported for creator JSON and imported profiles.

## Overlay designs

Overlay properties use the `Overlay` prefix:

```json
{
  "OverlayScalePercent": 110,
  "OverlayCardWidth": 720,
  "OverlayCardPosition": "Center",
  "OverlayLayoutMode": "Hero",
  "OverlayCardColor": "#EE0D121A",
  "OverlayUseGradient": true,
  "OverlayGradientColor": "#EE182737",
  "OverlayGradientAngle": 135,
  "OverlayAccentColor": "#FF55B8FF",
  "OverlayTextColor": "#FFFFFFFF",
  "OverlayUseBorderGradient": true,
  "OverlayBorderGradientStartColor": "#99FFFFFF",
  "OverlayBorderGradientEndColor": "#FF55B8FF",
  "OverlayShowBorderGlow": true,
  "OverlayBorderGlowColor": "#9955B8FF"
}
```

Overlay packs can control card/backdrop surfaces and images, layout and block order, placement, screen margins, animation, shadows, independent side borders, gradient/glow, controller container and icon, independent typography, connection badge, battery badge and its full/medium/low/empty colors.

## Assets

- Colors use `#AARRGGBB`; alpha comes first and `#00000000` is transparent.
- Images use pack-relative paths such as `Images/background.webp`.
- Image behavior supports stretch, horizontal/vertical crop alignment, opacity and tint.
- Reference a manifest font with `$font:<Id>`, for example `$font:Heading`.
- `Family` must contain the font's actual internal family name, not just its filename.
- Sounds belong in the manifest. Short normalized WAV files are the safest choice.
- Every asset needs redistribution permission and attribution where required.

## Authoring workflow

1. Fork the repository and create a folder under `CreatorThemes`.
2. Add the manifest and at least one appearance JSON file.
3. Start with a small property set, then preview all four notification states.
4. Test Desktop and Fullscreen independently.
5. Test the overlay at 100%, 125% and 150% Windows scaling.
6. Verify long controller names, missing battery data and transparency.
7. Confirm that all paths are relative and licenses permit redistribution.
8. Submit a pull request with screenshots, attribution and supported theme IDs.

## Review checklist

- Stable unique ID and author credit.
- Correct IDs copied from `theme.yaml`.
- No absolute paths, escaping paths, executable code or arbitrary XAML.
- Readable contrast in every event state.
- Connected, disconnected, warning and low-battery previews tested.
- Every supported Playnite mode and overlay tested.
- Fonts, images and sounds licensed for redistribution.
- Graceful behavior without optional images, battery data or short controller names.

The bundled Aniki ReMake and Helium packs are working examples. For every property, type and accepted range, consult the [exhaustive reference](https://github.com/Naerian/playnite-nx-session-controller-manager/blob/master/docs/CREATOR-THEMES.md).
