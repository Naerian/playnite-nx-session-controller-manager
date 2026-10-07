# Creator designs

The separate creator-design catalog (`.csmtheme` packs, **Update designs** and **Install creator design**) has been removed.

Looks now come from two places:

- **Playnite theme authors** ship a `ControllerManager/` folder inside the theme. See [Embedded appearance packs](EN-Theme-Appearance-Packs).
- **Everyone else** exports and imports a visual profile (`.pcvisual`) from **Appearance → Looks**.

Packs already installed from the old catalog are no longer loaded. Export a `.pcvisual` profile before updating if you still have that look selected and want to keep it. After the update, switch to a plugin look, Custom, an imported profile, or the Playnite theme styling toggles.
