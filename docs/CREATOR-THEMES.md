# Creator themes for Controller Manager

> The maintained contribution workflow, templates and complete author reference now live in the dedicated [Controller Manager Creator Themes Wiki](https://github.com/Naerian/controller-manager-creator-themes/wiki). Creator packs are submitted to that repository and installed with **Update designs**; they no longer require a Controller Manager release.

Downloaded `.csmtheme` packages can also be installed from **Appearance → Install creator design**. The plugin asks for confirmation with the design name, author and version, rejects incompatible schema/plugin ranges, validates files and properties, and preserves the previous installed copy if installation fails or is cancelled. The package is not registered as a Windows file type, so double-click installation is intentionally unnecessary.

The catalog repository protects `main` behind pull requests and its required `validate` check. Validation covers the complete documented notification and overlay property contracts (names, JSON types, ranges, colors and enumerations), manifests, safe asset paths, declared fonts and sounds, previews and license/credit evidence. Generated packages live in the separate `catalog` branch. Visual quality, accessibility in real themes, asset provenance and the truth of license declarations remain a maintainer review.

Controller Manager can ship complete visual designs for its controller notifications and disconnect overlay. A creator theme is a reviewed, self-contained folder committed to this repository and included in the `.pext` package. It may contain JSON appearance definitions, images, fonts and notification sounds.

This system is intended for Playnite theme authors and visual designers who want Controller Manager to look native to their theme.

## Creator themes and imported designs are different

- **Creator designs** are source-controlled packs under `CreatorThemes/`. They have an author, version, optional assets and sounds, are reviewed in a pull request and are distributed with the plugin. Their appearance controls are locked while selected so the authored design remains intact.
- **Imported designs** are `.pcvisual` files imported by an individual user. They contain a named snapshot of both notification destinations, the overlay and sound configuration. They are stored in the plugin data directory, appear under **Imported designs**, and can be deleted from the preset selector. Importing another profile with the same embedded name overwrites the existing entry instead of creating a duplicate.
- **Plugin presets** are maintained directly in Controller Manager's code.
- **Custom** is the user's editable configuration.

Do not submit a `.pcvisual` file as a creator theme. Use the folder format below.

## Contribution workflow

1. Fork the Controller Manager repository.
2. Create a unique folder under `CreatorThemes/`; use ASCII letters, digits, `_` or `-` for the folder name.
3. Add `manifest.json` and at least one of `notification.json` or `overlay.json`.
4. Add only redistributable assets and their licenses/credits.
5. Build and test the pack locally in both Playnite Desktop and Fullscreen modes.
6. Commit the folder and open a pull request with screenshots of every supported surface.

Creator packs are not copied to `ExtensionsData`, and there is no user-folder reload workflow. A new or changed creator pack becomes available through a Controller Manager release.

## Directory layout

```text
CreatorThemes/
└── MyTheme/
    ├── manifest.json             required
    ├── notification.json         optional; notification appearance
    ├── overlay.json              optional; disconnect overlay appearance
    ├── Images/                   optional
    │   └── background.png
    ├── Fonts/                    optional
    │   ├── MyFont-Regular.ttf
    │   └── LICENSE.txt
    ├── Audio/                    optional
    │   ├── connected.wav
    │   ├── disconnected.wav
    │   ├── warning.wav
    │   └── low-battery.wav
    ├── LICENSE.txt               recommended
    └── CREDITS.md                recommended
```

Keep every asset inside the pack folder. Paths which escape it with `..` are rejected. Use consistent path casing even though Windows normally resolves paths case-insensitively.

## `manifest.json`

```json
{
  "Id": "my-theme",
  "Name": "My Theme",
  "Author": "Creator name",
  "Version": "1.0.0",
  "Description": "A short description shown in Controller Manager.",
  "RecommendedTheme": "Optional Playnite theme name",
  "DesktopThemeIds": ["desktop-theme-id"],
  "FullscreenThemeIds": ["fullscreen-theme-id"],
  "Fonts": [
    {
      "Id": "Heading",
      "Name": "My Font — Heading",
      "Family": "Actual font family stored in the font file",
      "Folder": "Fonts"
    }
  ],
  "Sounds": {
    "Connected": "Audio/connected.wav",
    "Disconnected": "Audio/disconnected.wav",
    "Warning": "Audio/warning.wav",
    "LowBattery": "Audio/low-battery.wav"
  }
}
```

| Field | Required | Meaning |
| --- | --- | --- |
| `Id` | Yes | Stable, globally unique preset identifier. Never change it after release or existing selections will fall back. |
| `Name` | Yes | User-facing design name. |
| `Author` | Yes | Creator or team credited in the selector. |
| `Version` | No | Pack version; defaults to `1.0.0`. Semantic versioning is recommended. |
| `Description` | No | Short explanation of the visual intent. |
| `RecommendedTheme` | No | Playnite theme this pack accompanies. Informational only; selection is never automatic. |
| `ThemeIds` | No | Theme IDs accepted in both Desktop and Fullscreen. Use only when the same ID genuinely identifies both variants. |
| `DesktopThemeIds` | No | Exact IDs from the `Id` field of compatible Desktop themes. |
| `FullscreenThemeIds` | No | Exact IDs from the `Id` field of compatible Fullscreen themes. |
| `Fonts` | No | Fonts registered from this pack. See [Fonts](#fonts). |
| `Sounds` | No | Per-event audio map. See [Sounds](#sounds). |

`Id`, `Name` and `Author` must be non-empty. A pack is ignored if neither appearance JSON file contains a property. Duplicate IDs are not allowed in contributions.

Controller Manager reads the configured IDs from Playnite's settings and, for portable installations, verifies them against `config.json` and `fullscreenConfig.json`. When the user enables **In creator designs, show only those for the current theme**, the Desktop notification selector uses `DesktopThemeIds` and the Fullscreen notification selector uses `FullscreenThemeIds`. Because the disconnect overlay can be launched while either Playnite mode is configured, its selector accepts a match from either list. `RecommendedTheme` is used only as a compatibility fallback when a Playnite version exposes a theme name instead of its ID; folder names and visual structure are never inspected. A currently selected creator design remains visible so an existing configuration never becomes blank.

A pack with no `ThemeIds`, `DesktopThemeIds`, `FullscreenThemeIds` or `RecommendedTheme` is universal. Universal designs remain visible when filtering by the current Playnite theme; use this for standalone visual systems such as NarianUX that are not adaptations of one specific theme.

Obtain the ID from the theme's `theme.yaml`, not from its directory name:

```yaml
Id: my_desktop_theme_00000000-0000-0000-0000-000000000000
Name: My Desktop Theme
```

## How appearance JSON is applied

`notification.json` and `overlay.json` are flat JSON objects. Each key is the exact name of a public Controller Manager setting. Matching is case-insensitive, but use the documented casing.

```json
{
  "OverlayCardColor": "#F020232A",
  "OverlayScalePercent": 105,
  "OverlayShowBorder": true
}
```

Values are applied over a stable plugin baseline. Omitted properties retain that baseline; they do not inherit the user's previous Custom design. Unknown, read-only or incorrectly typed properties are ignored. One malformed pack never prevents the settings panel from opening, but ignored values can make the design incomplete.

Colors use WPF hexadecimal notation. `#AARRGGBB` is recommended and includes alpha; `#RRGGBB` is accepted for opaque colors. For example, `#CC101820` is a roughly 80%-opaque dark blue-grey. Numbers are JSON numbers, booleans are `true`/`false`, and choice values are strings exactly as documented. The ranges below match the settings UI and are the supported design range.

## Notification destinations

Desktop and Fullscreen notifications are independently selectable. A pack may define either or both appearances in the same `notification.json`:

- `Notification...` properties control **Fullscreen**.
- `DesktopNotification...` properties control **Desktop**.
- `ShowControllerNameInNotifications` controls Fullscreen.
- `ShowControllerNameInDesktopNotifications` controls Desktop.

To keep both destinations identical, duplicate each property with the other prefix. To adapt the design, use different widths, scale, padding or typography for each destination.

When a creator notification design is active for a destination, all editable appearance controls and its cross-destination copy button are disabled. Starting with Controller Manager 1.0.28, audio remains user-controlled: complete creator sound sets appear as selectable packs, while built-in packs, custom per-state files, volume and sound toggles remain editable.

### Notification property reference

In the following tables, replace `{P}` with `Notification` for Fullscreen or `DesktopNotification` for Desktop.
The normal settings interface intentionally hides **Advanced design**. Its properties remain fully supported for reviewed creator packs and imported profiles, giving authors access without exposing a dense editor to every user.

#### Window, placement and motion

| Property | Type / values | Purpose |
| --- | --- | --- |
| `{P}Width` | integer `300–900` | Base toast width. |
| `{P}ScalePercent` | integer `80–160` | Complete toast scale. |
| `{P}DurationMilliseconds` | integer `2000–15000` | Display time. |
| `{P}Position` | `TopRight`, `TopLeft`, `BottomRight`, `BottomLeft` | Screen corner. |
| `{P}ScreenMargin` | integer `8–64` | Distance from screen edges. |
| `{P}Animation` | `None`, `Fade`, `FadeScale`, `Slide` | Entrance/exit motion. |
| `{P}ShowShadow` | boolean | Enables the clipped, corner-aware drop shadow. |

#### Layout and content

| Property | Type / values | Purpose |
| --- | --- | --- |
| `{P}Padding` | integer `0–40` | Inner space around all content; `0` is edge-to-edge. |
| `{P}ElementSpacing` | integer `0–40` | Space between text/content elements. |
| `{P}IconSpacing` | integer `0–40` | Dedicated separation between icon and content. |
| `{P}IconPosition` | `Left`, `Right`, `Top`, `Bottom` | Icon placement. |
| `{P}IconSize` | integer `20–96` | Controller icon size. |
| `{P}TextAlignment` | `Left`, `Center`, `Right` | Title/message alignment. |
| `{P}TextOrder` | `TitleFirst`, `MessageFirst` | Text block order. |
| `{P}MessageMaxLines` | integer `1–6` | Maximum visible message lines. |
| `{P}ShowTitle` | boolean | Shows the event title. |
| `{P}UppercaseTitle` | boolean | Uppercases the title. |
| `ShowControllerNameInNotifications` / `ShowControllerNameInDesktopNotifications` | boolean | Shows the detected device name. |

#### Typography

| Property | Type / values | Purpose |
| --- | --- | --- |
| `{P}FontFamily` | font token/string | Common family fallback. |
| `{P}FontWeight` | `Regular`, `SemiBold`, `Bold` | Common weight fallback. |
| `{P}TitleFontFamily` | font token/string | Title-specific family. |
| `{P}TitleFontWeight` | `Regular`, `SemiBold`, `Bold` | Title weight. |
| `{P}TitleFontSize` | integer `10–48` | Title size. |
| `{P}MessageFontFamily` | font token/string | Message-specific family. |
| `{P}MessageFontWeight` | `Regular`, `SemiBold`, `Bold` | Message weight. |
| `{P}MessageFontSize` | integer `10–36` | Message size. |

Use `$font:<Id>` for a manifest font, for example `"$font:Heading"`. Never put a machine-specific font path in JSON.

#### Background and state colors

| Property | Type | Purpose |
| --- | --- | --- |
| `{P}BackgroundColor` | color | Base surface. |
| `{P}UseGradient` | boolean | Enables the surface gradient. |
| `{P}GradientColor` | color | Second gradient color. |
| `{P}GradientAngle` | integer `0–359` | Gradient direction. |
| `{P}TextColor` | color | Primary/title text. |
| `{P}SecondaryTextColor` | color | Message/secondary text. |
| `{P}ConnectedColor` | color | Connected accent. |
| `{P}DisconnectedColor` | color | Disconnected accent. |
| `{P}WarningColor` | color | Warning accent. |
| `{P}LowBatteryColor` | color | Low-battery accent. |
| `{P}UseStateBackgroundColors` | boolean | Enables a surface per event. |
| `{P}ConnectedBackgroundColor` | color | Connected surface. |
| `{P}DisconnectedBackgroundColor` | color | Disconnected surface. |
| `{P}WarningBackgroundColor` | color | Warning surface. |
| `{P}LowBatteryBackgroundColor` | color | Low-battery surface. |
| `{P}AccentMode` | `IconAndBorder`, `IconOnly`, `TintedBackground`, `SolidBackground` | How the event accent is applied. |

#### Background image

| Property | Type / values | Purpose |
| --- | --- | --- |
| `{P}UseBackgroundImage` | boolean | Enables the image. |
| `{P}BackgroundImagePath` | relative path | Image inside the pack, e.g. `Images/toast.png`. |
| `{P}BackgroundImageStretch` | `UniformToFill`, `Uniform`, `Fill` | Cover, contain or stretch. |
| `{P}BackgroundImageHorizontalAlignment` | `Left`, `Center`, `Right` | Horizontal focal position. |
| `{P}BackgroundImageVerticalAlignment` | `Top`, `Center`, `Bottom` | Vertical focal position. |
| `{P}BackgroundImageOpacity` | integer `0–100` | Image opacity. |
| `{P}BackgroundImageTintOpacity` | integer `0–100` | Surface tint strength. |

Use PNG, JPG or JPEG. Relative values for every property ending in `Path` are resolved from the pack folder and may not escape it.

#### Icon container and connection badge

| Property | Type / values | Purpose |
| --- | --- | --- |
| `{P}ShowIconContainer` | boolean | Draws a shape behind the icon. |
| `{P}IconContainerColor` | color | Container fill. |
| `{P}IconContainerBorderColor` | color | Container border. |
| `{P}IconContainerBorderThickness` | integer `0–8` | Container border width. |
| `{P}IconContainerCornerRadius` | integer `0–40` | Container roundness. |
| `{P}IconContainerPadding` | integer `0–24` | Space around the icon. |
| `{P}ShowConnectionBadge` | boolean | Shows connection type. |
| `{P}BadgePosition` | `Content`, `Icon`, `Bottom` | Badge placement. |

#### Border, shape and glow

| Property | Type / values | Purpose |
| --- | --- | --- |
| `{P}ShowBorder` | boolean | Enables the main border. |
| `{P}BorderPosition` | `Left`, `Top`, `Right`, `Bottom`, `Full` | Accent/border placement. |
| `{P}BorderThickness` | integer `0–10` | Main border width. |
| `{P}CornerRadius` | integer `0–40` | Surface roundness. |
| `{P}UseIndependentBorders` | boolean | Enables four independent side widths. |
| `{P}BorderLeftThickness` | integer `0–12` | Left width. |
| `{P}BorderTopThickness` | integer `0–12` | Top width. |
| `{P}BorderRightThickness` | integer `0–12` | Right width. |
| `{P}BorderBottomThickness` | integer `0–12` | Bottom width. |
| `{P}UseBorderGradient` | boolean | Enables a gradient stroke. |
| `{P}UseStateBorderColors` | boolean | Enables border colors that are independent from the icon/accent colors for every event state. |
| `{P}ConnectedBorderColor` | color | Border color for Connected. |
| `{P}DisconnectedBorderColor` | color | Border color for Disconnected. |
| `{P}WarningBorderColor` | color | Border color for Warning. |
| `{P}LowBatteryBorderColor` | color | Border color for Low battery. |
| `{P}BorderGradientStartColor` | color | Gradient start. |
| `{P}BorderGradientEndColor` | color | Gradient end. |
| `{P}BorderGradientAngle` | integer `0–359` | Stroke gradient direction. |
| `{P}ShowBorderGlow` | boolean | Enables an outer colored glow. |
| `{P}BorderGlowColor` | color | Glow color. |
| `{P}BorderGlowBlur` | integer `0–40` | Glow softness/radius. |
| `{P}BorderGlowOpacity` | integer `0–100` | Glow opacity. |

By default, the border follows the event accent (`{P}ConnectedColor`, `{P}DisconnectedColor`, `{P}WarningColor` or `{P}LowBatteryColor`). Creator packs can enable `{P}UseStateBorderColors` to control the border independently with the four dedicated properties above. This works for solid borders and gradients: a solid border uses the state border color directly, while a gradient keeps `{P}BorderGradientStartColor` as its common start and uses the current state border color as its end. Leave it disabled to share the explicit start/end gradient across every state. For a luminous edge, combine a partially transparent gradient border with a glow of the same hue. These are Controller Manager effects; creator packs cannot execute or reuse arbitrary XAML effects from a Playnite theme.

## Overlay property reference

Overlay advanced properties follow the same author-only policy: they are supported in creator JSON and imported profiles while the normal settings panel keeps **Advanced design** hidden.

The overlay is a full-screen dim layer containing a configurable card. All keys begin with `Overlay`.

### Placement, composition and visibility

| Property | Type / values | Purpose |
| --- | --- | --- |
| `OverlayScalePercent` | integer `80–140` | Complete card scale. |
| `OverlayCardWidth` | integer `320–1000` | Card width. |
| `OverlayCardPosition` | `Center`, `Top`, `Bottom`, `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` | Card placement. |
| `OverlayScreenMargin` | integer `0–160` | Distance from screen edges. |
| `OverlayLayoutMode` | `Standard`, `Split`, `Hero`, `Alert` | Major composition. |
| `OverlayContentAlignment` | `Left`, `Center`, `Right` | Content alignment. |
| `OverlayAnimation` | `None`, `Fade`, `FadeScale`, `Slide` | Entrance/exit. |
| `OverlayBlockOrder` | comma-separated block list | Standard layouts use `Title`, `Controller`, `Metadata`, `Instruction`, `Status`; Alert uses `Incident`, `Title`, `ControllerName`, `Metadata`, `Instruction`, `Status`. Optional `Timer` may appear once in either composition. |
| `OverlayMetadataOrientation` | `Horizontal`, `Vertical` | Badge arrangement. |
| `OverlayPadding` | integer `12–80` | Card inner padding. |
| `OverlayElementSpacing` | integer `0–48` | Space between blocks. |
| `OverlayShowTitle` | boolean | Shows the disconnect title. |
| `OverlayUppercaseTitle` | boolean | Uppercases the title. |
| `OverlayShowInstruction` | boolean | Shows reconnection instructions. |
| `OverlayShowPauseStatus` | boolean | Shows pause/resume state. |
| `OverlayShowDisconnectTimer` | boolean | Shows a live localized disconnect duration. Position it with the optional `Timer` block. |
| `OverlayShowControllerName` | boolean | Shows the detected controller name. |
| `OverlayShowControllerIcon` | boolean | Shows the controller icon. |
| `OverlayShowStatusIcon` | boolean | Shows the state icon. |
| `OverlayShowConnectionBadge` | boolean | Shows connection metadata. |
| `OverlayShowBatteryBadge` | boolean | Shows battery metadata. |
| `OverlayControllerIconPosition` | `Left`, `Center`, `Right`, `Top` | Controller icon placement. |
| `OverlayControllerIconSize` | integer `16–96` | Controller icon size. |
| `OverlayStatusIconSize` | integer `12–64` | State icon size. |

Example composition:

```json
{
  "OverlayBlockOrder": "Incident,Title,ControllerName,Timer,Metadata,Instruction,Status",
  "OverlayMetadataOrientation": "Vertical",
  "OverlayLayoutMode": "Alert",
  "OverlayShowDisconnectTimer": true
}
```

### Surfaces, images and colors

| Property | Type / values | Purpose |
| --- | --- | --- |
| `OverlayDimColor` | color | Full-screen dim layer; alpha controls game visibility. |
| `OverlayCardColor` | color | Card surface. |
| `OverlayUseGradient` | boolean | Enables card gradient. |
| `OverlayGradientColor` | color | Second card color. |
| `OverlayGradientAngle` | integer `0–359` | Gradient direction. |
| `OverlayAccentColor` | color | Main highlight/icon accent. |
| `OverlayTextColor` | color | Main text. |
| `OverlayWarningColor` | color | Warning/status emphasis. |
| `OverlayUseBackgroundImage` | boolean | Enables the card image. |
| `OverlayBackgroundImagePath` | relative path | Image inside the pack. |
| `OverlayBackgroundImageStretch` | `UniformToFill`, `Uniform`, `Fill` | Cover, contain or stretch. |
| `OverlayBackgroundImageHorizontalAlignment` | `Left`, `Center`, `Right` | Horizontal focal point. |
| `OverlayBackgroundImageVerticalAlignment` | `Top`, `Center`, `Bottom` | Vertical focal point. |
| `OverlayBackgroundImageOpacity` | integer `0–100` | Image opacity. |
| `OverlayBackgroundImageTintOpacity` | integer `0–100` | Surface tint strength. |

### Typography

| Element | Family property | Weight property | Size property / range |
| --- | --- | --- | --- |
| Common fallback | `OverlayFontFamily` | `OverlayFontWeight` | — |
| Title | `OverlayTitleFontFamily` | `OverlayTitleFontWeight` | `OverlayTitleFontSize`, `16–52` |
| Controller | `OverlayControllerFontFamily` | `OverlayControllerFontWeight` | `OverlayControllerFontSize`, `12–36` |
| Instruction | `OverlayInstructionFontFamily` | `OverlayInstructionFontWeight` | `OverlayInstructionFontSize`, `10–30` |
| Status | `OverlayStatusFontFamily` | `OverlayStatusFontWeight` | `OverlayStatusFontSize`, `10–28` |

Weights are `Regular`, `SemiBold` or `Bold`. Every family can independently use `$font:<Id>`.

### Controller container

| Property | Type / range | Purpose |
| --- | --- | --- |
| `OverlayShowControllerContainer` | boolean | Enables the icon/name container. |
| `OverlayControllerContainerColor` | color | Fill. |
| `OverlayControllerContainerBorderColor` | color | Border. |
| `OverlayControllerContainerBorderThickness` | integer `0–8` | Border width. |
| `OverlayControllerContainerCornerRadius` | integer `0–40` | Roundness. |
| `OverlayControllerContainerPadding` | integer `0–32` | Inner padding. |

### Connection and battery badges

Each badge has independent text, icon, background and border colors and dimensions:

| Connection badge | Battery badge | Type / range |
| --- | --- | --- |
| `OverlayConnectionBadgeTextColor` | `OverlayBatteryBadgeTextColor` | color |
| `OverlayConnectionBadgeIconColor` | `OverlayBatteryBadgeIconColor` | color |
| `OverlayConnectionBadgeBackgroundColor` | `OverlayBatteryBadgeBackgroundColor` | color |
| `OverlayConnectionBadgeBorderColor` | `OverlayBatteryBadgeBorderColor` | color |
| `OverlayConnectionBadgeBorderThickness` | `OverlayBatteryBadgeBorderThickness` | integer `0–8` |
| `OverlayConnectionBadgeCornerRadius` | `OverlayBatteryBadgeCornerRadius` | integer `0–32` |
| `OverlayConnectionBadgeIconSize` | `OverlayBatteryBadgeIconSize` | integer `8–40` |
| `OverlayConnectionBadgeTextSize` | `OverlayBatteryBadgeTextSize` | integer `8–28` |

Battery state colors are optional:

```json
{
  "OverlayBatteryBadgeUseStateColors": true,
  "OverlayBatteryBadgeFullColor": "#FF65D68A",
  "OverlayBatteryBadgeMediumColor": "#FFFFC857",
  "OverlayBatteryBadgeLowColor": "#FFFF8A4C",
  "OverlayBatteryBadgeEmptyColor": "#FFFF4D5A"
}
```

### Border, shape, shadow and glow

- `OverlayShowBorder` (boolean)
- `OverlayBorderPosition`: `Left`, `Top`, `Right`, `Bottom`, `Full`
- `OverlayBorderThickness`: integer `0–12`
- `OverlayCornerRadius`: integer `0–40`
- `OverlayShowShadow` (boolean)
- `OverlayUseIndependentBorders` (boolean)
- `OverlayBorderLeftThickness`, `OverlayBorderTopThickness`, `OverlayBorderRightThickness`, `OverlayBorderBottomThickness`: integer `0–12`
- `OverlayUseBorderGradient` (boolean)
- `OverlayBorderGradientStartColor`, `OverlayBorderGradientEndColor` (colors)
- `OverlayBorderGradientAngle`: integer `0–359`
- `OverlayShowBorderGlow` (boolean)
- `OverlayBorderGlowColor` (color)
- `OverlayBorderGlowBlur`: integer `0–40`
- `OverlayBorderGlowOpacity`: integer `0–100`

Independent borders support motifs such as a single top line or asymmetric frame. Gradient, shadow and glow can be combined. Leave sufficient screen/card margin for large glows.

## Fonts

Font registration is folder based. `Family` must be the actual family name embedded in the file, not the filename. Several aliases may reference the same folder.

```json
"Fonts": [
  { "Id": "Display", "Name": "My Theme Display", "Family": "Exo 2", "Folder": "Fonts" },
  { "Id": "Body", "Name": "My Theme Body", "Family": "Inter", "Folder": "Fonts" }
]
```

Reference an alias from either JSON file:

```json
{
  "NotificationTitleFontFamily": "$font:Display",
  "DesktopNotificationMessageFontFamily": "$font:Body",
  "OverlayTitleFontFamily": "$font:Display"
}
```

The portable font descriptor is also sent to the external overlay/toast process. Include every required `.ttf`/`.otf` file and a license permitting redistribution. Test all selected weights; choosing `Bold` does not create a genuine bold face if the font files do not contain one.

## Sounds

`Sounds` accepts the exact event names `Connected`, `Disconnected`, `Warning`, and `LowBattery`. Supported files are `.wav`, `.mp3`, and `.wma`; `.wav` is recommended for predictable low-latency playback.

Each entry is optional for the design, but starting with Controller Manager 1.0.28 a creator sound pack is shown in the selector only when all four events have valid existing files. Selecting a notification design with a complete set selects that pack by default; incomplete sets are not offered as audio packs.

Users can then choose another pack or assign a custom file to any event. Custom per-state audio takes priority over the selected pack. Keep clips short, normalized, free of leading silence and at a consistent perceived loudness. Global volume and the per-event and per-destination switches still apply.

## Complete starter examples

`notification.json`:

```json
{
  "NotificationWidth": 560,
  "NotificationScalePercent": 105,
  "NotificationBackgroundColor": "#F20C1118",
  "NotificationUseGradient": true,
  "NotificationGradientColor": "#F2182530",
  "NotificationGradientAngle": 135,
  "NotificationTitleFontFamily": "$font:Display",
  "NotificationTitleFontWeight": "SemiBold",
  "NotificationMessageFontFamily": "$font:Body",
  "NotificationIconPosition": "Left",
  "NotificationIconSpacing": 16,
  "NotificationPadding": 20,
  "NotificationShowBorder": true,
  "NotificationBorderPosition": "Full",
  "NotificationBorderThickness": 2,
  "NotificationCornerRadius": 14,
  "NotificationUseBorderGradient": true,
  "NotificationUseStateBorderColors": true,
  "NotificationConnectedBorderColor": "#FF55D68B",
  "NotificationDisconnectedBorderColor": "#FF55B8FF",
  "NotificationWarningBorderColor": "#FFFFC857",
  "NotificationLowBatteryBorderColor": "#FFFF5D6C",
  "NotificationBorderGradientStartColor": "#99FFFFFF",
  "NotificationBorderGradientEndColor": "#FF55B8FF",
  "NotificationBorderGradientAngle": 45,
  "NotificationShowBorderGlow": true,
  "NotificationBorderGlowColor": "#9955B8FF",
  "NotificationBorderGlowBlur": 22,
  "NotificationBorderGlowOpacity": 55,

  "DesktopNotificationWidth": 440,
  "DesktopNotificationScalePercent": 100,
  "DesktopNotificationBackgroundColor": "#F20C1118",
  "DesktopNotificationTitleFontFamily": "$font:Display",
  "DesktopNotificationMessageFontFamily": "$font:Body",
  "DesktopNotificationIconSpacing": 14,
  "DesktopNotificationPadding": 16,
  "DesktopNotificationShowBorder": true,
  "DesktopNotificationBorderPosition": "Top",
  "DesktopNotificationBorderThickness": 2,
  "DesktopNotificationCornerRadius": 8
}
```

`overlay.json`:

```json
{
  "OverlayScalePercent": 105,
  "OverlayDimColor": "#A8000000",
  "OverlayCardColor": "#F20C1118",
  "OverlayCardWidth": 720,
  "OverlayCardPosition": "Center",
  "OverlayLayoutMode": "Hero",
  "OverlayContentAlignment": "Center",
  "OverlayBlockOrder": "Controller,Title,Metadata,Instruction,Status",
  "OverlayMetadataOrientation": "Horizontal",
  "OverlayTitleFontFamily": "$font:Display",
  "OverlayControllerFontFamily": "$font:Body",
  "OverlayInstructionFontFamily": "$font:Body",
  "OverlayStatusFontFamily": "$font:Body",
  "OverlayAccentColor": "#FF55B8FF",
  "OverlayTextColor": "#FFF5F7FA",
  "OverlayShowBorder": true,
  "OverlayBorderThickness": 2,
  "OverlayCornerRadius": 16,
  "OverlayUseBorderGradient": true,
  "OverlayBorderGradientStartColor": "#99FFFFFF",
  "OverlayBorderGradientEndColor": "#FF55B8FF",
  "OverlayBorderGradientAngle": 45,
  "OverlayShowBorderGlow": true,
  "OverlayBorderGlowColor": "#9955B8FF",
  "OverlayBorderGlowBlur": 28,
  "OverlayBorderGlowOpacity": 55
}
```

## Local development and testing

Build the plugin using the repository's normal build command, then place or symlink the built extension into Playnite. Creator discovery occurs at plugin startup, so restart Playnite after changing a pack.

Useful checks:

```powershell
.\tests\run-creator-theme-tests.ps1
.\tests\render-toast-preview.ps1 -Creator my-theme
.\tests\render-overlay-preview.ps1 -Creator my-theme
```

Replace `my-theme` with the pack ID from your manifest. Also run the repository's full test suite before opening a pull request.

Test at minimum:

1. Desktop connected, disconnected, warning and low-battery previews.
2. Fullscreen connected, disconnected, warning and low-battery previews.
3. Overlay at 100%, 125% and 150% Windows display scaling.
4. Long controller names and translated text.
5. Battery and connection badges both present and absent.
6. Every bundled font face and weight.
7. Every sound at low and high plugin volume.
8. Background images with wide and tall aspect ratios.
9. Switching between Custom, plugin, imported and creator designs.
10. Playnite restart with the design still selected.

## Pull request checklist

- [ ] `Id` is unique and stable.
- [ ] JSON files use strict JSON: no comments or trailing commas.
- [ ] Only documented appearance properties are used.
- [ ] All paths are relative and remain inside the pack.
- [ ] Assets have attribution and redistributable licenses.
- [ ] Fonts include the faces needed for every selected weight.
- [ ] Sounds are short, normalized and legally redistributable.
- [ ] Screenshots cover every supported surface.
- [ ] The design remains readable over bright and dark game content.
- [ ] Long/localized text does not clip.
- [ ] Glow and shadow are not clipped at intended margins.
- [ ] Creator-theme smoke tests pass.

## Troubleshooting

### The design does not appear

Check that the pack is under the repository/plugin `CreatorThemes` folder, the manifest has non-empty `Id`, `Name` and `Author`, and at least one appearance file contains a JSON object. Rebuild and restart Playnite.

### A property has no effect

Verify its exact name and JSON type. A string such as `"105"` is not the same as the number `105`. Confirm the destination: `Notification...` is Fullscreen; `DesktopNotification...` is Desktop.

### A font falls back

Check the embedded family name, manifest alias, folder and font files. Use `$font:Id`, not a local filesystem path. Ensure the requested weight exists.

### An image or sound is missing

Use a relative path, confirm the extension is supported and ensure the resolved path does not leave the pack. Avoid filenames which differ only by case.

### The glow does not exactly match a Playnite theme effect

Creator themes configure Controller Manager's WPF rendering primitives. They cannot import arbitrary controls, shaders, storyboards or resource dictionaries from the active Playnite theme. Recreate the visual language with gradient borders, glow color/blur/opacity, surface gradients, images and shadows.

### The settings controls are disabled

That is intentional. Select **Custom**, a plugin preset or an imported design to return to an editable appearance. Creator designs are locked so their preview and audio remain faithful to the submitted pack.

## Compatibility and security

Creator packs are data, not executable code. Do not add DLLs, scripts or external downloads. New Controller Manager versions may add properties, but existing documented keys should remain compatible. If a property must be replaced, the plugin should migrate or retain it long enough for bundled packs to update.

The maintainers may adjust a submitted design for legibility, security, performance or compatibility. Adaptations of third-party Playnite themes must credit their original authors and must not imply endorsement.
