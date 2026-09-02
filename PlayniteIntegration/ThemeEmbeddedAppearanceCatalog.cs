using System;
using System.IO;
using System.Linq;
using Playnite.SDK;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>
    /// Loads creator-theme JSON from the active Playnite theme's ControllerManager folder.
    /// </summary>
    internal static class ThemeEmbeddedAppearanceCatalog
    {
        private static readonly object Sync = new object();
        private static string cachedThemeDirectory;
        private static CreatorThemeDefinition cachedDefinition;

        public static bool TryGetDefinition(IPlayniteAPI api, ThemeAppearanceSurface surface,
            out CreatorThemeDefinition definition)
        {
            definition = null;
            var packDirectory = GetPackDirectory(api, surface);
            if (string.IsNullOrWhiteSpace(packDirectory)) return false;
            lock (Sync)
            {
                if (!string.Equals(cachedThemeDirectory, packDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    cachedThemeDirectory = packDirectory;
                    CreatorThemeDefinition loaded;
                    cachedDefinition = CreatorThemePackLoader.TryLoad(packDirectory, out loaded)
                        ? loaded : null;
                }
                definition = cachedDefinition;
            }
            return definition != null;
        }

        public static bool HasEmbeddedLayout(IPlayniteAPI api, ThemeAppearanceSurface surface)
        {
            CreatorThemeDefinition definition;
            if (!TryGetDefinition(api, surface, out definition)) return false;
            return definition.Supports(SurfaceKey(surface));
        }

        public static bool HasBridgeFile(IPlayniteAPI api, ThemeAppearanceSurface surface)
        {
            var themeDirectory = GetThemeRoot(api, surface);
            if (string.IsNullOrWhiteSpace(themeDirectory)) return false;
            return File.Exists(Path.Combine(themeDirectory, "ControllerManager", "theme-bridge.json"));
        }

        public static void InvalidateCache()
        {
            lock (Sync)
            {
                cachedThemeDirectory = null;
                cachedDefinition = null;
            }
        }

        public static bool TryApply(ControllerSessionManagerSettings settings, IPlayniteAPI api,
            ThemeAppearanceSurface surface)
        {
            CreatorThemeDefinition definition;
            if (settings == null || !TryGetDefinition(api, surface, out definition)) return false;
            return CreatorThemePackLoader.TryApplySurface(settings, definition, SurfaceKey(surface));
        }

        /// <summary>
        /// Builds appearance from the active theme's ControllerManager pack only, without
        /// merging preset or user customization from saved settings.
        /// </summary>
        public static bool TryCreateThemedAppearance(IPlayniteAPI api, ThemeAppearanceSurface surface,
            out ControllerSessionManagerSettings appearance)
        {
            appearance = null;
            if (!HasEmbeddedLayout(api, surface)) return false;
            var themed = new ControllerSessionManagerSettings();
            if (!TryApply(themed, api, surface)) return false;
            appearance = themed;
            return true;
        }

        public static string GetSoundPath(IPlayniteAPI api, NotificationSoundKind kind)
        {
            CreatorThemeDefinition definition;
            if (!TryGetDefinition(api, ThemeAppearanceSurface.FullscreenNotification, out definition) &&
                !TryGetDefinition(api, ThemeAppearanceSurface.DesktopNotification, out definition))
                return string.Empty;
            return ResolveSoundPath(definition, kind);
        }

        public static bool HasCompleteSoundPack(IPlayniteAPI api)
        {
            CreatorThemeDefinition definition;
            if (!TryGetDefinition(api, ThemeAppearanceSurface.FullscreenNotification, out definition) &&
                !TryGetDefinition(api, ThemeAppearanceSurface.DesktopNotification, out definition))
                return false;
            return Enum.GetValues(typeof(NotificationSoundKind))
                .Cast<NotificationSoundKind>()
                .All(kind => !string.IsNullOrWhiteSpace(ResolveSoundPath(definition, kind)));
        }

        public static string GetSoundPackDisplayName(IPlayniteAPI api)
        {
            CreatorThemeDefinition definition;
            if (!TryGetDefinition(api, ThemeAppearanceSurface.FullscreenNotification, out definition) &&
                !TryGetDefinition(api, ThemeAppearanceSurface.DesktopNotification, out definition))
                return NotificationSoundCatalog.ThemeEmbeddedPack;
            return definition.Name + " — " + definition.Author;
        }

        private static string ResolveSoundPath(CreatorThemeDefinition definition, NotificationSoundKind kind)
        {
            if (definition == null) return string.Empty;
            string relative;
            if (!definition.Sounds.TryGetValue(kind.ToString(), out relative) ||
                string.IsNullOrWhiteSpace(relative)) return string.Empty;
            try
            {
                var root = Path.GetFullPath(definition.Directory).TrimEnd(Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;
                var resolved = Path.GetFullPath(Path.Combine(definition.Directory, relative));
                var extension = Path.GetExtension(resolved);
                var supported = string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".wma", StringComparison.OrdinalIgnoreCase);
                return supported && resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(resolved)
                    ? resolved : string.Empty;
            }
            catch { return string.Empty; }
        }

        private static string GetPackDirectory(IPlayniteAPI api, ThemeAppearanceSurface surface)
        {
            var themeRoot = GetThemeRoot(api, surface);
            if (string.IsNullOrWhiteSpace(themeRoot)) return string.Empty;
            var packDirectory = Path.Combine(themeRoot, "ControllerManager");
            return Directory.Exists(packDirectory) ? packDirectory : string.Empty;
        }

        private static string GetThemeRoot(IPlayniteAPI api, ThemeAppearanceSurface surface)
        {
            var fullscreen = surface != ThemeAppearanceSurface.DesktopNotification;
            return ThemeAppearanceBridge.FindThemeDirectory(api, fullscreen);
        }

        private static string SurfaceKey(ThemeAppearanceSurface surface)
        {
            return surface == ThemeAppearanceSurface.Overlay ? "overlay" : "notification";
        }
    }
}
