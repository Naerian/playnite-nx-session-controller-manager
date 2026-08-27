using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>
    /// Discovers reviewed, self-contained creator appearance packs bundled beside the plugin
    /// or downloaded into the plugin's user-data directory.
    /// </summary>
    public static class CreatorThemeCatalog
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, CreatorThemeDefinition> Definitions =
            new Dictionary<string, CreatorThemeDefinition>(StringComparer.OrdinalIgnoreCase);
        private static string bundledRoot;
        private static string downloadedRoot;

        public static void Configure(string pluginDirectory)
        {
            Configure(pluginDirectory, null);
        }

        public static void Configure(string pluginDirectory, string userDataDirectory)
        {
            lock (Sync)
            {
                bundledRoot = Path.Combine(pluginDirectory ?? string.Empty, "CreatorThemes");
                downloadedRoot = string.IsNullOrWhiteSpace(userDataDirectory) ? null
                    : Path.Combine(userDataDirectory, "CreatorThemes");
                ReloadCore();
            }
        }

        public static string DownloadedRoot
        {
            get { lock (Sync) return downloadedRoot; }
        }

        public static void Reload()
        {
            lock (Sync) ReloadCore();
        }

        public static string[] GetPresetIds(string surface)
        {
            lock (Sync)
            {
                return Definitions.Values
                    .Where(a => a.Supports(surface))
                    .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(a => a.Id).ToArray();
            }
        }

        public static bool Contains(string id, string surface)
        {
            CreatorThemeDefinition definition;
            lock (Sync)
                return !string.IsNullOrWhiteSpace(id) && Definitions.TryGetValue(id, out definition) &&
                    definition.Supports(surface);
        }

        public static string GetName(string id)
        {
            CreatorThemeDefinition definition;
            lock (Sync)
                return Definitions.TryGetValue(id ?? string.Empty, out definition) ? definition.Name : id;
        }

        public static string GetAuthor(string id)
        {
            CreatorThemeDefinition definition;
            lock (Sync)
                return Definitions.TryGetValue(id ?? string.Empty, out definition) ? definition.Author : string.Empty;
        }

        public static string GetDescription(string id)
        {
            CreatorThemeDefinition definition;
            lock (Sync)
            {
                if (!Definitions.TryGetValue(id ?? string.Empty, out definition)) return string.Empty;
                var theme = string.IsNullOrWhiteSpace(definition.RecommendedTheme) ? string.Empty
                    : " · " + definition.RecommendedTheme;
                return definition.Description + "\n" + definition.Author + " · v" + definition.Version + theme;
            }
        }

        public static bool MatchesTheme(string id, string themeId, bool fullscreen)
        {
            if (string.IsNullOrWhiteSpace(themeId)) return false;
            var activeTheme = themeId.Trim();

            CreatorThemeDefinition definition;
            lock (Sync)
            {
                if (!Definitions.TryGetValue(id ?? string.Empty, out definition)) return false;
                var compatibleThemeIds = definition.GetThemeIds(fullscreen)
                    .Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
                if (compatibleThemeIds.Any(a => ThemeIdentifiersMatch(a, activeTheme)))
                    return true;

                // Some Playnite versions/themes expose the display name instead of the manifest ID.
                var normalizedRecommendedTheme = NormalizeThemeIdentifier(definition.RecommendedTheme);
                // Packs with no declared target are intentionally universal and remain visible
                // when users enable filtering by their current Playnite theme.
                if (compatibleThemeIds.Count == 0 && normalizedRecommendedTheme.Length == 0)
                    return true;
                return normalizedRecommendedTheme.Length > 0 &&
                    NormalizeThemeIdentifier(activeTheme).Contains(normalizedRecommendedTheme);
            }
        }

        private static bool ThemeIdentifiersMatch(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual)) return false;
            if (string.Equals(expected.Trim(), actual.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            var normalizedExpected = NormalizeThemeIdentifier(expected);
            var normalizedActual = NormalizeThemeIdentifier(actual);
            return normalizedExpected.Length > 0 && normalizedActual.Length > 0 &&
                (normalizedExpected.Contains(normalizedActual) || normalizedActual.Contains(normalizedExpected));
        }

        private static string NormalizeThemeIdentifier(string value)
        {
            return new string((value ?? string.Empty).Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant).ToArray());
        }

        public static bool TryApply(ControllerSessionManagerSettings settings, string id, string surface)
        {
            CreatorThemeDefinition definition;
            lock (Sync)
            {
                if (!Definitions.TryGetValue(id ?? string.Empty, out definition)) return false;
            }
            var values = string.Equals(surface, "overlay", StringComparison.OrdinalIgnoreCase)
                ? definition.Overlay : definition.Notification;
            if (values == null || values.Count == 0) return false;
            ApplyValues(settings, values, definition, surface);
            return true;
        }

        public static string GetSoundPath(string id, NotificationSoundKind kind)
        {
            CreatorThemeDefinition definition;
            lock (Sync)
            {
                if (!Definitions.TryGetValue(id ?? string.Empty, out definition)) return string.Empty;
            }
            string relative;
            if (!definition.Sounds.TryGetValue(kind.ToString(), out relative) ||
                string.IsNullOrWhiteSpace(relative)) return string.Empty;
            try
            {
                var root = Path.GetFullPath(definition.Directory).TrimEnd(Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;
                var resolved = Path.GetFullPath(Path.Combine(definition.Directory, relative));
                return resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(resolved)
                    ? resolved : string.Empty;
            }
            catch { return string.Empty; }
        }

        private static void ReloadCore()
        {
            Definitions.Clear();
            LoadRoot(bundledRoot);
            // Reviewed remote packs override an older bundled copy with the same stable ID.
            LoadRoot(downloadedRoot);
        }

        private static void LoadRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;
            foreach (var directory in Directory.GetDirectories(root))
            {
                try
                {
                    var manifestPath = Path.Combine(directory, "manifest.json");
                    if (!File.Exists(manifestPath)) continue;
                    var serializer = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 };
                    var manifest = serializer.Deserialize<CreatorThemeManifest>(
                        File.ReadAllText(manifestPath, Encoding.UTF8));
                    if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id) ||
                        string.IsNullOrWhiteSpace(manifest.Name) || string.IsNullOrWhiteSpace(manifest.Author)) continue;
                    var definition = new CreatorThemeDefinition
                    {
                        Id = manifest.Id.Trim(), Name = manifest.Name.Trim(), Author = manifest.Author.Trim(),
                        Version = manifest.Version ?? "1.0.0", Description = manifest.Description ?? string.Empty,
                        RecommendedTheme = manifest.RecommendedTheme ?? string.Empty,
                        ThemeIds = CleanIds(manifest.ThemeIds),
                        DesktopThemeIds = CleanIds(manifest.DesktopThemeIds),
                        FullscreenThemeIds = CleanIds(manifest.FullscreenThemeIds),
                        Directory = directory,
                        Notification = ReadValues(serializer, Path.Combine(directory, "notification.json")),
                        Overlay = ReadValues(serializer, Path.Combine(directory, "overlay.json"))
                    };
                    foreach (var sound in manifest.Sounds ?? new Dictionary<string, string>())
                    {
                        if (!string.IsNullOrWhiteSpace(sound.Key) && !string.IsNullOrWhiteSpace(sound.Value))
                            definition.Sounds[sound.Key.Trim()] = sound.Value.Trim();
                    }
                    foreach (var font in manifest.Fonts ?? new List<CreatorThemeFontManifest>())
                    {
                        if (font == null || string.IsNullOrWhiteSpace(font.Id) ||
                            string.IsNullOrWhiteSpace(font.Family)) continue;
                        var fontFolder = Path.GetFullPath(Path.Combine(directory,
                            string.IsNullOrWhiteSpace(font.Folder) ? "Fonts" : font.Folder));
                        var packRoot = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) +
                            Path.DirectorySeparatorChar;
                        if (!fontFolder.StartsWith(packRoot, StringComparison.OrdinalIgnoreCase)) continue;
                        definition.Fonts[font.Id.Trim()] = NotificationFontCatalog.RegisterExternalFont(
                            fontFolder, font.Family.Trim(), string.IsNullOrWhiteSpace(font.Name) ? font.Family : font.Name);
                    }
                    if (definition.Notification.Count > 0 || definition.Overlay.Count > 0)
                        Definitions[definition.Id] = definition;
                }
                catch { /* A malformed community pack must never break the settings window. */ }
            }
        }

        private static Dictionary<string, object> ReadValues(JavaScriptSerializer serializer, string path)
        {
            return File.Exists(path)
                ? serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(path, Encoding.UTF8)) ??
                    new Dictionary<string, object>()
                : new Dictionary<string, object>();
        }

        private static List<string> CleanIds(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void ApplyValues(ControllerSessionManagerSettings settings,
            IDictionary<string, object> values, CreatorThemeDefinition definition, string surface)
        {
            if (settings == null) return;
            foreach (var pair in values)
            {
                if (!ControllerSessionManagerSettingsView.IsCreatorThemePropertyAllowed(
                    pair.Key, surface)) continue;
                var property = typeof(ControllerSessionManagerSettings).GetProperty(pair.Key,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null || !property.CanWrite || pair.Value == null) continue;
                try
                {
                    var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    var text = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
                    if (type == typeof(string) && pair.Key.EndsWith("FontFamily", StringComparison.OrdinalIgnoreCase) &&
                        text != null && text.StartsWith("$font:", StringComparison.OrdinalIgnoreCase))
                    {
                        string fontToken;
                        if (definition.Fonts.TryGetValue(text.Substring(6), out fontToken)) text = fontToken;
                    }
                    if (type == typeof(string) && pair.Key.EndsWith("Path", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(text) && !Path.IsPathRooted(text))
                    {
                        var root = Path.GetFullPath(definition.Directory).TrimEnd(Path.DirectorySeparatorChar) +
                            Path.DirectorySeparatorChar;
                        var resolved = Path.GetFullPath(Path.Combine(definition.Directory, text));
                        if (!resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;
                        text = resolved;
                    }
                    var converted = type == typeof(string) ? text
                        : type.IsEnum ? Enum.Parse(type, pair.Value.ToString(), true)
                        : Convert.ChangeType(pair.Value, type, CultureInfo.InvariantCulture);
                    property.SetValue(settings, converted, null);
                }
                catch { }
            }
        }

        public sealed class CreatorThemeManifest
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Author { get; set; }
            public string Version { get; set; }
            public int SchemaVersion { get; set; }
            public string MinimumPluginVersion { get; set; }
            public string MaximumPluginVersion { get; set; }
            public string Description { get; set; }
            public string RecommendedTheme { get; set; }
            public List<string> ThemeIds { get; set; }
            public List<string> DesktopThemeIds { get; set; }
            public List<string> FullscreenThemeIds { get; set; }
            public List<CreatorThemeFontManifest> Fonts { get; set; }
            public Dictionary<string, string> Sounds { get; set; }
        }

        public sealed class CreatorThemeFontManifest
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Family { get; set; }
            public string Folder { get; set; }
        }
    }

    internal sealed class CreatorThemeDefinition
    {
        public string Id, Name, Author, Version, Description, RecommendedTheme, Directory;
        public List<string> ThemeIds = new List<string>();
        public List<string> DesktopThemeIds = new List<string>();
        public List<string> FullscreenThemeIds = new List<string>();
        public Dictionary<string, object> Notification, Overlay;
        public readonly Dictionary<string, string> Fonts =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, string> Sounds =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public bool Supports(string surface)
        {
            return string.Equals(surface, "overlay", StringComparison.OrdinalIgnoreCase)
                ? Overlay != null && Overlay.Count > 0 : Notification != null && Notification.Count > 0;
        }
        public IEnumerable<string> GetThemeIds(bool fullscreen)
        {
            return ThemeIds.Concat(fullscreen ? FullscreenThemeIds : DesktopThemeIds);
        }
    }
}
