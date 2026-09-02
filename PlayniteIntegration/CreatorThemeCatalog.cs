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

        public const string OriginFileName = ".csm-origin";
        public const string OriginCatalog = "catalog";
        public const string OriginSideload = "sideload";

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

        public static bool IsUserInstalled(string id)
        {
            CreatorThemeDefinition definition;
            lock (Sync)
            {
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(downloadedRoot) ||
                    !Definitions.TryGetValue(id, out definition) ||
                    string.IsNullOrWhiteSpace(definition.Directory))
                    return false;
            }
            try
            {
                var root = Path.GetFullPath(downloadedRoot).TrimEnd(Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;
                var directory = Path.GetFullPath(definition.Directory);
                if (!directory.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    return false;
                return string.Equals(ReadOrigin(directory), OriginSideload, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static void MarkCatalogOrigin(string directory)
        {
            WriteOrigin(directory, OriginCatalog);
        }

        public static void MarkSideloadOrigin(string directory)
        {
            WriteOrigin(directory, OriginSideload);
        }

        private static string ReadOrigin(string directory)
        {
            try
            {
                var path = Path.Combine(directory, OriginFileName);
                return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void WriteOrigin(string directory, string origin)
        {
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(origin)) return;
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, OriginFileName), origin.Trim() + Environment.NewLine,
                new UTF8Encoding(false));
        }

        public static bool TryRemoveUserInstalled(string id)
        {
            if (!IsUserInstalled(id)) return false;
            string directory;
            lock (Sync)
            {
                CreatorThemeDefinition definition;
                if (!Definitions.TryGetValue(id, out definition)) return false;
                directory = definition.Directory;
            }
            try
            {
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch
            {
                return false;
            }
            Reload();
            return !IsUserInstalled(id);
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

                var normalizedRecommendedTheme = NormalizeThemeIdentifier(definition.RecommendedTheme);
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
            return CreatorThemePackLoader.TryApplySurface(settings, definition, surface);
        }

        public static string GetSoundPath(string id, NotificationSoundKind kind)
        {
            CreatorThemeDefinition definition;
            lock (Sync)
            {
                if (!Definitions.TryGetValue(id ?? string.Empty, out definition)) return string.Empty;
            }
            return ResolveSoundPath(definition, kind);
        }

        public static string[] GetCompleteSoundPackIds()
        {
            lock (Sync)
            {
                return Definitions.Values.Where(IsEligibleSoundPack)
                    .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(a => NotificationSoundCatalog.CreatorPackPrefix + a.Id).ToArray();
            }
        }

        public static string GetSoundPackId(string creatorPresetId)
        {
            CreatorThemeDefinition definition;
            lock (Sync)
            {
                return Definitions.TryGetValue(creatorPresetId ?? string.Empty, out definition) &&
                    IsEligibleSoundPack(definition)
                        ? NotificationSoundCatalog.CreatorPackPrefix + definition.Id : string.Empty;
            }
        }

        public static string GetSoundPackDisplayName(string soundPackId)
        {
            CreatorThemeDefinition definition;
            return TryGetSoundPackDefinition(soundPackId, out definition)
                ? definition.Name + " — " + definition.Author : soundPackId;
        }

        public static bool IsCompleteSoundPack(string soundPackId)
        {
            CreatorThemeDefinition definition;
            return TryGetSoundPackDefinition(soundPackId, out definition) &&
                IsEligibleSoundPack(definition);
        }

        public static string GetSoundPathForPack(string soundPackId, NotificationSoundKind kind)
        {
            CreatorThemeDefinition definition;
            return TryGetSoundPackDefinition(soundPackId, out definition)
                ? ResolveSoundPath(definition, kind) : string.Empty;
        }

        private static bool TryGetSoundPackDefinition(string soundPackId,
            out CreatorThemeDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(soundPackId) ||
                !soundPackId.StartsWith(NotificationSoundCatalog.CreatorPackPrefix,
                    StringComparison.OrdinalIgnoreCase)) return false;
            var id = soundPackId.Substring(NotificationSoundCatalog.CreatorPackPrefix.Length);
            lock (Sync) return Definitions.TryGetValue(id, out definition);
        }

        private static bool IsEligibleSoundPack(CreatorThemeDefinition definition)
        {
            return definition != null &&
                Enum.GetValues(typeof(NotificationSoundKind))
                .Cast<NotificationSoundKind>().All(kind =>
                    !string.IsNullOrWhiteSpace(ResolveSoundPath(definition, kind)));
        }

        private static string ResolveSoundPath(CreatorThemeDefinition definition,
            NotificationSoundKind kind)
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
                CreatorThemeDefinition definition;
                if (!CreatorThemePackLoader.TryLoad(directory, out definition)) continue;
                Definitions[definition.Id] = definition;
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
