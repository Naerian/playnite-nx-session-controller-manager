# Embedded appearance packs for Playnite themes

**Audience:** developers who maintain a **Playnite theme** (Desktop and/or Fullscreen) and want Controller Manager notifications, overlay and sounds to match that theme automatically.

This is **not** the same as publishing a [community creator design](EN-Creator-Designs) (`.csmtheme`). Theme developers ship files **inside the theme folder**; community creators publish installable packs to the catalog.

## Quick comparison

| | Playnite theme developer | Community creator (`.csmtheme`) |
|---|---|---|
| **Goal** | Bundle styling with your theme | Share a look other users can install |
| **Where it lives** | `{ThemeFolder}/ControllerManager/` | Catalog or manual `.csmtheme` install |
| **How users enable it** | **Appearance → Looks** → Playnite theme styling toggles (per surface) | Select the design in the Looks dropdown |
| **Documentation** | This page + [Theme integration](EN-Theme-Integration) | [Creator Themes Wiki](https://github.com/Naerian/controller-manager-creator-themes/wiki) |
| **Repository to fork** | Your Playnite theme repo | [controller-manager-creator-themes](https://github.com/Naerian/controller-manager-creator-themes) |

## Folder layout

Ship this inside the active Playnite theme:

```text
Themes/Desktop/{ThemeId}/ControllerManager/
Themes/Fullscreen/{ThemeId}/ControllerManager/
  manifest.json
  notification.json      optional layout/colors for notifications
  overlay.json           optional layout/colors for disconnect overlay
  theme-bridge.json      optional live palette bridge
  Audio/                 optional notification sounds
  Fonts/                 optional embedded fonts
  assets/                images referenced by JSON
```

`manifest.json`, `notification.json` and `overlay.json` use the **same JSON schema** as creator designs. You do **not** need a separate `.csmtheme` file for theme users.

## How it behaves at runtime

1. The user picks a **look** in **Appearance → Looks** (plugin preset, custom, imported profile or creator design). That look applies when the Playnite theme styling toggle is off, or when the active theme has no embedded `ControllerManager/` pack for that surface.
2. When a **Playnite theme styling** toggle is on and the active theme ships `notification.json` or `overlay.json` in `ControllerManager/`, that embedded design fully controls rendering for that surface (layout, colors, fonts, images and sounds).
3. Optional **`theme-bridge.json`** maps your theme's WPF resource keys to Controller Manager color/typeface roles so in-theme color packs stay in sync at display time when the toggle is on.

## `theme-bridge.json`

Path: `{ThemeFolder}/ControllerManager/theme-bridge.json`

```json
{
  "Notification": {
    "Background": "NotificationBackgroundBrush",
    "Gradient": "MenuBackgroundBottomColor",
    "TextStyle": "TextBlockBoldBaseStyle",
    "MessageStyle": "TextBlockBaseStyle",
    "Border": "PopupBorderBrush",
    "Accent": "GlyphBrush",
    "SecondaryText": "TextBrushDarker",
    "Warning": "WarningBrush"
  },
  "Overlay": {
    "Background": "ControlBackgroundBrush",
    "Text": "TextBrush",
    "Accent": "GlyphBrush",
    "Border": "NormalBorderBrush",
    "Warning": "WarningBrush"
  }
}
```

Keys on the left are Controller Manager roles. Values are **your theme's** resource keys. The plugin resolves them with `Application.Current.TryFindResource` when the matching toggle is on.

Full bridge contract: [Playnite Theme Bridge](https://github.com/Naerian/controller-manager-creator-themes/wiki/Playnite-Theme-Bridge) (maintained in the creator-themes wiki because it shares vocabulary with pack authoring).

## Testing tips

- **Fullscreen notifications:** preview from **Playnite Fullscreen**. Desktop mode cannot load the active fullscreen theme, so desktop settings previews may not match fullscreen.
- **Desktop notifications:** test with the Desktop theme active and the desktop styling toggle on.
- **Overlay:** runs in a separate process; colors arrive as resolved hex values (bridge still applies in the Playnite process before IPC).

## Related docs

- [Theme integration](EN-Theme-Integration) — ContentControl elements and `PluginSettings` API inside theme XAML
- [Notifications & overlay](EN-Notifications-and-Overlay) — end-user behavior
- [Creator designs](EN-Creator-Designs) — community `.csmtheme` catalog (different workflow)
