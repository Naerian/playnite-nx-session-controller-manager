# Creator designs (`.csmtheme`)

Controller Manager can install reviewed visual packs for controller notifications and the disconnect overlay. Creator designs may include layouts, colors, images, fonts and notification sounds. They are kept separate from plugin presets and imported visual profiles.

> **Not a Playnite theme developer?** If you maintain a Playnite **theme** and want to bundle styling inside it, see [Embedded appearance packs for Playnite themes](EN-Theme-Appearance-Packs) instead. Community creators publish `.csmtheme` files to the catalog; theme developers ship a `ControllerManager/` folder inside the theme.

## Using creator designs

- Select **Appearance → Update designs** to download compatible releases from the official catalog.
- Select **Install creator design** to install a trusted `.csmtheme` file downloaded manually, including a pull-request test artifact.
- A creator design locks and dims the appearance controls it owns so the authored result is not changed accidentally.
- If a downloaded update is incompatible or unavailable, Controller Manager keeps the last compatible installed copy.
- `.csmtheme` is not registered as a Windows file type; install it from the settings panel instead of double-clicking it.

Before a local package is installed, Controller Manager shows its name, author and version, checks its schema and plugin compatibility, validates archive paths, sizes, file types, assets and appearance properties, and replaces an existing copy only after the complete package succeeds. Cancelling or rejecting a package preserves the installed design.

## Creating or contributing a design

You are authoring a **community look** (`.csmtheme`) for other Controller Manager users—not embedding files inside a Playnite theme.

The authoring format, templates, property reference, compatibility rules, validation, pull-request workflow and testing tools are maintained in the dedicated:

- [Controller Manager Creator Themes repository](https://github.com/Naerian/controller-manager-creator-themes)
- [Controller Manager Creator Themes Wiki](https://github.com/Naerian/controller-manager-creator-themes/wiki)

Fork the creator-theme repository—not the plugin repository—to contribute a design. Accepted designs become available through **Update designs** and do not require a new Controller Manager release.
