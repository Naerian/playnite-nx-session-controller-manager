using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class CreatorThemePackageInstaller
    {
        public const string FileExtension = ".csmtheme";
        private const int MaximumPackageBytes = 32 * 1024 * 1024;
        private const int MaximumEntryBytes = 12 * 1024 * 1024;
        private const int MaximumEntries = 256;
        private static readonly Regex SafeId = new Regex("^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$",
            RegexOptions.CultureInvariant);
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(
            new[] { ".json", ".png", ".jpg", ".jpeg", ".ttf", ".otf", ".wav", ".mp3", ".wma", ".txt", ".md" },
            StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> AudioExtensions = new HashSet<string>(
            new[] { ".wav", ".mp3", ".wma" }, StringComparer.OrdinalIgnoreCase);

        private readonly string themesRoot;
        private readonly Version pluginVersion;

        public CreatorThemePackageInstaller(string downloadedThemesRoot)
            : this(downloadedThemesRoot,
                Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0))
        {
        }

        internal CreatorThemePackageInstaller(string downloadedThemesRoot, Version currentPluginVersion)
        {
            themesRoot = downloadedThemesRoot;
            pluginVersion = currentPluginVersion ?? new Version(0, 0);
        }

        public CreatorThemeCatalog.CreatorThemeManifest Inspect(string packagePath)
        {
            ValidatePackageFile(packagePath);
            using (var stream = File.OpenRead(packagePath))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ValidateArchiveEntries(archive);
                var manifestEntry = archive.Entries.FirstOrDefault(a =>
                    string.Equals(NormalizeEntryName(a.FullName), "manifest.json",
                        StringComparison.OrdinalIgnoreCase));
                if (manifestEntry == null || manifestEntry.Length <= 0 ||
                    manifestEntry.Length > 2 * 1024 * 1024)
                    throw new InvalidDataException("Creator-theme package has no valid root manifest.json.");
                using (var reader = new StreamReader(manifestEntry.Open(), Encoding.UTF8, true))
                {
                    var serializer = new JavaScriptSerializer { MaxJsonLength = 2 * 1024 * 1024 };
                    var manifest = serializer.Deserialize<CreatorThemeCatalog.CreatorThemeManifest>(
                        reader.ReadToEnd().TrimStart('\uFEFF'));
                    ValidateManifest(manifest);
                    return manifest;
                }
            }
        }

        public Task<CreatorThemeCatalog.CreatorThemeManifest> InstallAsync(string packagePath,
            CancellationToken cancellationToken)
        {
            return Task.Run(() => Install(packagePath, cancellationToken), cancellationToken);
        }

        private CreatorThemeCatalog.CreatorThemeManifest Install(string packagePath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(themesRoot))
                throw new InvalidOperationException("The creator-theme directory is unavailable.");
            var inspected = Inspect(packagePath);
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(themesRoot);
            var staging = Path.Combine(themesRoot, ".import-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(staging);
                ExtractValidated(packagePath, staging, cancellationToken);
                var installedManifest = ValidateExtracted(staging, inspected.Id);
                ReplaceDirectory(staging, Path.Combine(themesRoot, installedManifest.Id));
                CreatorThemeCatalog.Reload();
                return installedManifest;
            }
            finally
            {
                TryDeleteDirectory(staging);
            }
        }

        private void ValidateManifest(CreatorThemeCatalog.CreatorThemeManifest manifest)
        {
            if (manifest == null || !SafeId.IsMatch(manifest.Id ?? string.Empty) ||
                string.IsNullOrWhiteSpace(manifest.Name) || string.IsNullOrWhiteSpace(manifest.Author) ||
                !TryVersion(manifest.Version, out _))
                throw new InvalidDataException("Creator-theme manifest is invalid.");
            if (manifest.SchemaVersion != CreatorThemeUpdater.SupportedThemeSchema)
                throw new CreatorThemeCompatibilityException(pluginVersion, manifest,
                    CreatorThemeCompatibilityReason.Schema);
            Version minimum;
            Version maximum;
            if (!string.IsNullOrWhiteSpace(manifest.MinimumPluginVersion) &&
                !TryVersion(manifest.MinimumPluginVersion, out minimum))
                throw new InvalidDataException("Creator-theme minimum plugin version is invalid.");
            if (!string.IsNullOrWhiteSpace(manifest.MaximumPluginVersion) &&
                !TryVersion(manifest.MaximumPluginVersion, out maximum))
                throw new InvalidDataException("Creator-theme maximum plugin version is invalid.");
            minimum = ParseVersion(manifest.MinimumPluginVersion) ?? new Version(0, 0);
            maximum = ParseVersion(manifest.MaximumPluginVersion);
            if (pluginVersion < minimum || (maximum != null && pluginVersion > maximum))
                throw new CreatorThemeCompatibilityException(pluginVersion, manifest,
                    CreatorThemeCompatibilityReason.PluginVersion);
        }

        private CreatorThemeCatalog.CreatorThemeManifest ValidateExtracted(string root, string expectedId)
        {
            var manifestPath = Path.Combine(root, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new InvalidDataException("Creator-theme package has no root manifest.json.");
            var serializer = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 };
            var manifest = serializer.Deserialize<CreatorThemeCatalog.CreatorThemeManifest>(
                File.ReadAllText(manifestPath, Encoding.UTF8).TrimStart('\uFEFF'));
            ValidateManifest(manifest);
            if (!string.Equals(manifest.Id, expectedId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Creator-theme manifest changed during installation.");

            var notificationPath = Path.Combine(root, "notification.json");
            var overlayPath = Path.Combine(root, "overlay.json");
            if (!File.Exists(notificationPath) && !File.Exists(overlayPath))
                throw new InvalidDataException("Creator-theme package has no appearance definition.");
            if (File.Exists(notificationPath)) ValidateAppearance(serializer, notificationPath, root, "notification");
            if (File.Exists(overlayPath)) ValidateAppearance(serializer, overlayPath, root, "overlay");
            ValidateManifestAssets(manifest, root);
            return manifest;
        }

        private static void ValidateAppearance(JavaScriptSerializer serializer, string path,
            string root, string surface)
        {
            var values = serializer.Deserialize<Dictionary<string, object>>(
                File.ReadAllText(path, Encoding.UTF8).TrimStart('\uFEFF'));
            if (values == null || values.Count == 0)
                throw new InvalidDataException(Path.GetFileName(path) + " is empty or invalid.");
            foreach (var pair in values)
            {
                if (!ControllerSessionManagerSettingsView.IsCreatorThemePropertyAllowed(pair.Key, surface))
                    throw new InvalidDataException("Creator-theme package contains an unsupported appearance property: " + pair.Key);
                var property = typeof(ControllerSessionManagerSettings).GetProperty(pair.Key,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null || !property.CanWrite || pair.Value == null)
                    throw new InvalidDataException("Creator-theme appearance value is invalid: " + pair.Key);
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                try
                {
                    if (targetType == typeof(string))
                    {
                        var text = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
                        if (pair.Key.EndsWith("Path", StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(text))
                            ValidateRelativeAsset(root, text, null);
                    }
                    else if (targetType.IsEnum)
                    {
                        Enum.Parse(targetType, pair.Value.ToString(), true);
                    }
                    else
                    {
                        Convert.ChangeType(pair.Value, targetType, CultureInfo.InvariantCulture);
                    }
                }
                catch (InvalidDataException) { throw; }
                catch
                {
                    throw new InvalidDataException("Creator-theme appearance value has the wrong type: " + pair.Key);
                }
            }
        }

        private static void ValidateManifestAssets(CreatorThemeCatalog.CreatorThemeManifest manifest,
            string root)
        {
            foreach (var sound in manifest.Sounds ?? new Dictionary<string, string>())
            {
                NotificationSoundKind ignored;
                if (!Enum.TryParse(sound.Key, true, out ignored))
                    throw new InvalidDataException("Creator-theme package contains an unknown sound state: " + sound.Key);
                ValidateRelativeAsset(root, sound.Value, AudioExtensions);
            }
            foreach (var font in manifest.Fonts ?? new List<CreatorThemeCatalog.CreatorThemeFontManifest>())
            {
                if (font == null || string.IsNullOrWhiteSpace(font.Id) ||
                    string.IsNullOrWhiteSpace(font.Family))
                    throw new InvalidDataException("Creator-theme package contains an invalid font declaration.");
                var folder = string.IsNullOrWhiteSpace(font.Folder) ? "Fonts" : font.Folder;
                var resolved = ResolveRelative(root, folder);
                if (!Directory.Exists(resolved))
                    throw new InvalidDataException("Creator-theme font folder does not exist: " + folder);
            }
        }

        private static void ValidateRelativeAsset(string root, string relative,
            ISet<string> allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
                throw new InvalidDataException("Creator-theme asset path is invalid.");
            var resolved = ResolveRelative(root, relative);
            if (!File.Exists(resolved))
                throw new InvalidDataException("Creator-theme asset does not exist: " + relative);
            if (allowedExtensions != null && !allowedExtensions.Contains(Path.GetExtension(resolved)))
                throw new InvalidDataException("Creator-theme asset type is unsupported: " + relative);
        }

        private static string ResolveRelative(string root, string relative)
        {
            var safeRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var resolved = Path.GetFullPath(Path.Combine(root,
                (relative ?? string.Empty).Replace('/', Path.DirectorySeparatorChar)));
            if (!resolved.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Creator-theme package contains an unsafe asset path.");
            return resolved;
        }

        private static void ValidatePackageFile(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath) ||
                !string.Equals(Path.GetExtension(packagePath), FileExtension,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The selected file is not a .csmtheme package.");
            var info = new FileInfo(packagePath);
            if (!info.Exists || info.Length <= 0 || info.Length > MaximumPackageBytes)
                throw new InvalidDataException("Creator-theme package is empty or larger than allowed.");
        }

        private static void ValidateArchiveEntries(ZipArchive archive)
        {
            if (archive.Entries.Count == 0 || archive.Entries.Count > MaximumEntries)
                throw new InvalidDataException("Creator-theme package contains an invalid number of files.");
            long total = 0;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                if (entry.Length > MaximumEntryBytes || (total += entry.Length) > MaximumPackageBytes)
                    throw new InvalidDataException("Creator-theme package is larger than allowed.");
                var name = NormalizeEntryName(entry.FullName);
                if (!names.Add(name))
                    throw new InvalidDataException("Creator-theme package contains duplicate files.");
                var extension = Path.GetExtension(entry.Name);
                if (!AllowedExtensions.Contains(extension) &&
                    !string.Equals(entry.Name, "LICENSE", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Creator-theme package contains a forbidden file type.");
                if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                    throw new InvalidDataException("Creator-theme package contains a symbolic link.");
                if (name.StartsWith("../", StringComparison.Ordinal) || name.Contains("/../") ||
                    Path.IsPathRooted(entry.FullName))
                    throw new InvalidDataException("Creator-theme package contains an unsafe path.");
            }
        }

        private static void ExtractValidated(string packagePath, string target,
            CancellationToken cancellationToken)
        {
            using (var stream = File.OpenRead(packagePath))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ValidateArchiveEntries(archive);
                var root = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    var destination = Path.GetFullPath(Path.Combine(target,
                        entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                    if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Creator-theme package contains an unsafe path.");
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    using (var input = entry.Open())
                    using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                        CopyLimited(input, output, MaximumEntryBytes, cancellationToken);
                }
            }
        }

        private static void CopyLimited(Stream input, Stream output, int limit,
            CancellationToken token)
        {
            var buffer = new byte[32768];
            var total = 0;
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();
                total += read;
                if (total > limit) throw new InvalidDataException("Creator-theme file is larger than allowed.");
                output.Write(buffer, 0, read);
            }
        }

        private static void ReplaceDirectory(string staging, string destination)
        {
            var backup = destination + ".previous";
            TryDeleteDirectory(backup);
            if (Directory.Exists(destination)) Directory.Move(destination, backup);
            try
            {
                Directory.Move(staging, destination);
                TryDeleteDirectory(backup);
            }
            catch
            {
                if (Directory.Exists(destination)) TryDeleteDirectory(destination);
                if (Directory.Exists(backup)) Directory.Move(backup, destination);
                throw;
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try { if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) Directory.Delete(path, true); }
            catch { }
        }

        private static string NormalizeEntryName(string value)
        {
            return (value ?? string.Empty).Replace('\\', '/').TrimStart('/');
        }

        private static Version ParseVersion(string value)
        {
            Version result;
            return TryVersion(value, out result) ? result : null;
        }

        private static bool TryVersion(string value, out Version result)
        {
            return Version.TryParse((value ?? string.Empty).Trim(), out result);
        }
    }

    public enum CreatorThemeCompatibilityReason
    {
        PluginVersion,
        Schema
    }

    public sealed class CreatorThemeCompatibilityException : Exception
    {
        public Version PluginVersion { get; private set; }
        public CreatorThemeCatalog.CreatorThemeManifest Manifest { get; private set; }
        public CreatorThemeCompatibilityReason Reason { get; private set; }

        public CreatorThemeCompatibilityException(Version pluginVersion,
            CreatorThemeCatalog.CreatorThemeManifest manifest, CreatorThemeCompatibilityReason reason)
            : base("The creator theme is not compatible with this Controller Manager version.")
        {
            PluginVersion = pluginVersion;
            Manifest = manifest;
            Reason = reason;
        }
    }
}
