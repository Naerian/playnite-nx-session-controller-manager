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
    /// <summary>Loads creator-theme JSON packs from a folder (catalog, sideload, or Playnite theme).</summary>
    internal static class CreatorThemePackLoader
    {
        public static bool TryLoad(string packDirectory, out CreatorThemeDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(packDirectory) || !Directory.Exists(packDirectory))
                return false;
            try
            {
                var manifestPath = Path.Combine(packDirectory, "manifest.json");
                if (!File.Exists(manifestPath)) return false;
                var serializer = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 };
                var manifest = serializer.Deserialize<CreatorThemeCatalog.CreatorThemeManifest>(
                    File.ReadAllText(manifestPath, Encoding.UTF8).TrimStart('\uFEFF'));
                if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id) ||
                    string.IsNullOrWhiteSpace(manifest.Name) || string.IsNullOrWhiteSpace(manifest.Author))
                    return false;
                definition = new CreatorThemeDefinition
                {
                    Id = manifest.Id.Trim(),
                    Name = manifest.Name.Trim(),
                    Author = manifest.Author.Trim(),
                    Version = manifest.Version ?? "1.0.0",
                    Description = manifest.Description ?? string.Empty,
                    RecommendedTheme = manifest.RecommendedTheme ?? string.Empty,
                    ThemeIds = CleanIds(manifest.ThemeIds),
                    DesktopThemeIds = CleanIds(manifest.DesktopThemeIds),
                    FullscreenThemeIds = CleanIds(manifest.FullscreenThemeIds),
                    Directory = packDirectory,
                    Notification = ReadValues(serializer, Path.Combine(packDirectory, "notification.json")),
                    Overlay = ReadValues(serializer, Path.Combine(packDirectory, "overlay.json"))
                };
                foreach (var sound in manifest.Sounds ?? new Dictionary<string, string>())
                {
                    if (!string.IsNullOrWhiteSpace(sound.Key) && !string.IsNullOrWhiteSpace(sound.Value))
                        definition.Sounds[sound.Key.Trim()] = sound.Value.Trim();
                }
                foreach (var font in manifest.Fonts ?? new List<CreatorThemeCatalog.CreatorThemeFontManifest>())
                {
                    if (font == null || string.IsNullOrWhiteSpace(font.Id) ||
                        string.IsNullOrWhiteSpace(font.Family)) continue;
                    var fontFolder = Path.GetFullPath(Path.Combine(packDirectory,
                        string.IsNullOrWhiteSpace(font.Folder) ? "Fonts" : font.Folder));
                    var packRoot = Path.GetFullPath(packDirectory).TrimEnd(Path.DirectorySeparatorChar) +
                        Path.DirectorySeparatorChar;
                    if (!fontFolder.StartsWith(packRoot, StringComparison.OrdinalIgnoreCase)) continue;
                    definition.Fonts[font.Id.Trim()] = NotificationFontCatalog.RegisterExternalFont(
                        fontFolder, font.Family.Trim(),
                        string.IsNullOrWhiteSpace(font.Name) ? font.Family : font.Name);
                }
                return definition.Notification.Count > 0 || definition.Overlay.Count > 0;
            }
            catch
            {
                definition = null;
                return false;
            }
        }

        public static void ApplyValues(ControllerSessionManagerSettings settings,
            IDictionary<string, object> values, CreatorThemeDefinition definition, string surface)
        {
            if (settings == null || values == null || values.Count == 0) return;
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
                    if (!string.Equals(surface, "notification", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var desktopKey = DesktopNotificationPropertyName(pair.Key);
                    if (string.IsNullOrEmpty(desktopKey) || HasValueKey(values, desktopKey))
                        continue;
                    var desktopProperty = typeof(ControllerSessionManagerSettings).GetProperty(desktopKey,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (desktopProperty == null || !desktopProperty.CanWrite) continue;
                    desktopProperty.SetValue(settings, converted, null);
                }
                catch { }
            }
        }

        public static bool TryApplySurface(ControllerSessionManagerSettings settings,
            CreatorThemeDefinition definition, string surface)
        {
            if (settings == null || definition == null) return false;
            var values = string.Equals(surface, "overlay", StringComparison.OrdinalIgnoreCase)
                ? definition.Overlay : definition.Notification;
            if (values == null || values.Count == 0) return false;
            ApplyValues(settings, values, definition, surface);
            return true;
        }

        private static Dictionary<string, object> ReadValues(JavaScriptSerializer serializer, string path)
        {
            return File.Exists(path)
                ? serializer.Deserialize<Dictionary<string, object>>(
                    File.ReadAllText(path, Encoding.UTF8).TrimStart('\uFEFF')) ??
                    new Dictionary<string, object>()
                : new Dictionary<string, object>();
        }

        private static List<string> CleanIds(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool HasValueKey(IDictionary<string, object> values, string key)
        {
            return values.Keys.Any(a => string.Equals(a, key, StringComparison.OrdinalIgnoreCase));
        }

        private static string DesktopNotificationPropertyName(string propertyName)
        {
            if (string.Equals(propertyName, "ShowControllerNameInNotifications",
                StringComparison.OrdinalIgnoreCase))
                return "ShowControllerNameInDesktopNotifications";
            if (!string.IsNullOrEmpty(propertyName) &&
                propertyName.StartsWith("Notification", StringComparison.OrdinalIgnoreCase) &&
                !propertyName.StartsWith("Desktop", StringComparison.OrdinalIgnoreCase))
                return "Desktop" + propertyName;
            return string.Empty;
        }
    }
}
