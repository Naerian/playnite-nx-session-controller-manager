using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>Downloads reviewed data-only creator packs from the official catalog.</summary>
    public sealed class CreatorThemeUpdater
    {
        public const int SupportedCatalogSchema = 1;
        public const int SupportedThemeSchema = 1;
        public const string DefaultCatalogUrl =
            "https://raw.githubusercontent.com/Naerian/controller-manager-creator-themes/catalog/dist/catalog.json";

        private const int MaximumCatalogBytes = 2 * 1024 * 1024;
        private const int MaximumPackageBytes = 32 * 1024 * 1024;
        private const int MaximumEntryBytes = 12 * 1024 * 1024;
        private const int MaximumEntries = 256;
        private static readonly Regex SafeId = new Regex("^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$",
            RegexOptions.CultureInvariant);
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(
            new[] { ".json", ".png", ".jpg", ".jpeg", ".ttf", ".otf", ".wav", ".mp3", ".wma", ".txt", ".md" },
            StringComparer.OrdinalIgnoreCase);

        private readonly string themesRoot;
        private readonly string catalogUrl;
        private readonly Version pluginVersion;
        private readonly string metadataPath;
        private readonly string cachedCatalogPath;

        public CreatorThemeUpdater(string downloadedThemesRoot)
            : this(downloadedThemesRoot,
                Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0),
                DefaultCatalogUrl)
        {
        }

        internal CreatorThemeUpdater(string downloadedThemesRoot, Version currentPluginVersion,
            string sourceCatalogUrl)
        {
            themesRoot = downloadedThemesRoot;
            pluginVersion = currentPluginVersion ?? new Version(0, 0);
            catalogUrl = sourceCatalogUrl;
            var parent = Path.GetDirectoryName(themesRoot ?? string.Empty) ?? string.Empty;
            metadataPath = Path.Combine(parent, "CreatorThemes.catalog.meta");
            cachedCatalogPath = Path.Combine(parent, "CreatorThemes.catalog.json");
        }

        public Task<CreatorThemeUpdateResult> CheckForUpdatesAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => CheckForUpdates(cancellationToken), cancellationToken);
        }

        private CreatorThemeUpdateResult CheckForUpdates(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(themesRoot))
                return CreatorThemeUpdateResult.Failed("The creator-theme directory is unavailable.");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Directory.CreateDirectory(themesRoot);
                var metadata = ReadMetadata();
                string etag;
                bool notModified;
                var json = DownloadText(catalogUrl, metadata.ETag, MaximumCatalogBytes,
                    cancellationToken, out etag, out notModified);
                if (notModified)
                {
                    if (!File.Exists(cachedCatalogPath))
                        return CreatorThemeUpdateResult.Success(0, 0, 0, true);
                    json = File.ReadAllText(cachedCatalogPath, Encoding.UTF8);
                }

                var serializer = new JavaScriptSerializer { MaxJsonLength = MaximumCatalogBytes };
                var catalog = serializer.Deserialize<CreatorThemeRemoteCatalog>(
                    (json ?? string.Empty).TrimStart('\uFEFF'));
                if (catalog == null || catalog.SchemaVersion != SupportedCatalogSchema || catalog.Themes == null)
                    return CreatorThemeUpdateResult.Failed("The creator-theme catalog is invalid or unsupported.");

                var installed = 0;
                var updated = 0;
                var incompatible = 0;
                foreach (var theme in catalog.Themes.Where(a => a != null))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!SafeId.IsMatch(theme.Id ?? string.Empty))
                        continue;
                    var compatible = SelectCompatibleVersion(theme.Versions);
                    if (compatible == null)
                    {
                        incompatible++;
                        continue;
                    }
                    var current = ReadInstalledVersion(theme.Id);
                    Version nextVersion;
                    if (!TryVersion(compatible.Version, out nextVersion) ||
                        (current != null && current >= nextVersion))
                        continue;

                    Install(theme.Id, compatible, cancellationToken);
                    if (current == null) installed++; else updated++;
                }

                if (!notModified)
                {
                    File.WriteAllText(cachedCatalogPath, json, new UTF8Encoding(false));
                    WriteMetadata(etag);
                }
                CreatorThemeCatalog.Reload();
                return CreatorThemeUpdateResult.Success(installed, updated, incompatible,
                    installed == 0 && updated == 0 && incompatible == 0);
            }
            catch (OperationCanceledException)
            {
                return CreatorThemeUpdateResult.CancelledResult();
            }
            catch (Exception error)
            {
                return CreatorThemeUpdateResult.Failed(error.Message);
            }
        }

        private CreatorThemeRemoteVersion SelectCompatibleVersion(
            IEnumerable<CreatorThemeRemoteVersion> versions)
        {
            return (versions ?? Enumerable.Empty<CreatorThemeRemoteVersion>())
                .Where(IsCompatible)
                .Select(a => new { Item = a, Parsed = ParseVersion(a.Version) })
                .Where(a => a.Parsed != null)
                .OrderByDescending(a => a.Parsed)
                .Select(a => a.Item).FirstOrDefault();
        }

        private bool IsCompatible(CreatorThemeRemoteVersion item)
        {
            if (item == null || item.SchemaVersion != SupportedThemeSchema ||
                string.IsNullOrWhiteSpace(item.Url) || string.IsNullOrWhiteSpace(item.Sha256)) return false;
            var minimum = ParseVersion(item.MinimumPluginVersion) ?? new Version(0, 0);
            var maximum = ParseVersion(item.MaximumPluginVersion);
            return pluginVersion >= minimum && (maximum == null || pluginVersion <= maximum);
        }

        private Version ReadInstalledVersion(string id)
        {
            try
            {
                var path = Path.Combine(themesRoot, id, "manifest.json");
                if (!File.Exists(path)) return null;
                var manifest = new JavaScriptSerializer().Deserialize<CreatorThemeCatalog.CreatorThemeManifest>(
                    File.ReadAllText(path, Encoding.UTF8));
                return manifest != null && string.Equals(manifest.Id, id, StringComparison.OrdinalIgnoreCase)
                    ? ParseVersion(manifest.Version) : null;
            }
            catch { return null; }
        }

        private void Install(string id, CreatorThemeRemoteVersion release,
            CancellationToken cancellationToken)
        {
            var packageUri = new Uri(release.Url, UriKind.Absolute);
            if (packageUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidDataException("Creator-theme packages must use HTTPS.");
            var temporaryPackage = Path.Combine(themesRoot, ".download-" + Guid.NewGuid().ToString("N") + ".csmtheme");
            var staging = Path.Combine(themesRoot, ".staging-" + Guid.NewGuid().ToString("N"));
            try
            {
                DownloadFile(packageUri, temporaryPackage, MaximumPackageBytes, cancellationToken);
                var hash = ComputeSha256(temporaryPackage);
                if (!string.Equals(hash, NormalizeHash(release.Sha256), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Creator-theme package hash mismatch.");
                if (release.Size > 0 && new FileInfo(temporaryPackage).Length != release.Size)
                    throw new InvalidDataException("Creator-theme package size mismatch.");

                Directory.CreateDirectory(staging);
                ExtractValidated(temporaryPackage, staging, cancellationToken);
                ValidateExtracted(staging, id, release);
                ReplaceDirectory(staging, Path.Combine(themesRoot, id));
            }
            finally
            {
                TryDeleteFile(temporaryPackage);
                TryDeleteDirectory(staging);
            }
        }

        private static void ExtractValidated(string packagePath, string target,
            CancellationToken cancellationToken)
        {
            using (var stream = File.OpenRead(packagePath))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                if (archive.Entries.Count > MaximumEntries)
                    throw new InvalidDataException("Creator-theme package contains too many files.");
                long total = 0;
                var root = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    if (entry.Length > MaximumEntryBytes || (total += entry.Length) > MaximumPackageBytes)
                        throw new InvalidDataException("Creator-theme package is larger than allowed.");
                    var extension = Path.GetExtension(entry.Name);
                    if (!AllowedExtensions.Contains(extension) &&
                        !string.Equals(entry.Name, "LICENSE", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Creator-theme package contains a forbidden file type.");
                    if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                        throw new InvalidDataException("Creator-theme package contains a symbolic link.");
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

        private static void ValidateExtracted(string root, string id,
            CreatorThemeRemoteVersion release)
        {
            var manifestPath = Path.Combine(root, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new InvalidDataException("Creator-theme package has no root manifest.json.");
            var serializer = new JavaScriptSerializer { MaxJsonLength = MaximumCatalogBytes };
            var manifest = serializer.Deserialize<CreatorThemeCatalog.CreatorThemeManifest>(
                File.ReadAllText(manifestPath, Encoding.UTF8));
            if (manifest == null || !string.Equals(manifest.Id, id, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(manifest.Version, release.Version, StringComparison.OrdinalIgnoreCase) ||
                manifest.SchemaVersion != release.SchemaVersion ||
                !string.Equals(manifest.MinimumPluginVersion, release.MinimumPluginVersion,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(manifest.MaximumPluginVersion ?? string.Empty,
                    release.MaximumPluginVersion ?? string.Empty, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(manifest.Name) || string.IsNullOrWhiteSpace(manifest.Author))
                throw new InvalidDataException("Creator-theme manifest does not match its catalog entry.");
            if (!File.Exists(Path.Combine(root, "notification.json")) &&
                !File.Exists(Path.Combine(root, "overlay.json")))
                throw new InvalidDataException("Creator-theme package has no appearance definition.");
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

        private static string DownloadText(string url, string etag, int limit,
            CancellationToken token, out string responseEtag, out bool notModified)
        {
            responseEtag = etag;
            notModified = false;
            var request = CreateRequest(new Uri(url), etag);
            using (token.Register(request.Abort))
            {
                try
                {
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var input = response.GetResponseStream())
                    using (var output = new MemoryStream())
                    {
                        CopyLimited(input, output, limit, token);
                        responseEtag = response.Headers[HttpResponseHeader.ETag];
                        return Encoding.UTF8.GetString(output.ToArray());
                    }
                }
                catch (WebException error)
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException(token);
                    var response = error.Response as HttpWebResponse;
                    if (response != null && response.StatusCode == HttpStatusCode.NotModified)
                    {
                        notModified = true;
                        return null;
                    }
                    throw;
                }
            }
        }

        private static void DownloadFile(Uri uri, string destination, int limit,
            CancellationToken token)
        {
            var request = CreateRequest(uri, null);
            using (token.Register(request.Abort))
            {
                try
                {
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var input = response.GetResponseStream())
                    using (var output = new FileStream(destination, FileMode.Create,
                        FileAccess.Write, FileShare.None))
                        CopyLimited(input, output, limit, token);
                }
                catch (WebException)
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException(token);
                    throw;
                }
            }
        }

        private static HttpWebRequest CreateRequest(Uri uri, string etag)
        {
            // .NET Framework 4.6.2 can otherwise negotiate an obsolete protocol when Playnite
            // runs on a machine whose system defaults have not been modernized.
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; // TLS 1.2
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.UserAgent = "ControllerManager-Playnite-CreatorThemes";
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            request.AllowAutoRedirect = true;
            if (!string.IsNullOrWhiteSpace(etag)) request.Headers[HttpRequestHeader.IfNoneMatch] = etag;
            return request;
        }

        private static void CopyLimited(Stream input, Stream output, int limit,
            CancellationToken token)
        {
            if (input == null) throw new InvalidDataException("The server returned an empty response.");
            var buffer = new byte[32768];
            var total = 0;
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();
                total += read;
                if (total > limit) throw new InvalidDataException("The download is larger than allowed.");
                output.Write(buffer, 0, read);
            }
        }

        private static string ComputeSha256(string path)
        {
            using (var input = File.OpenRead(path))
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(input)).Replace("-", string.Empty);
        }

        private static string NormalizeHash(string value)
        {
            return (value ?? string.Empty).Replace("sha256:", string.Empty).Replace("-", string.Empty).Trim();
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

        private CatalogMetadata ReadMetadata()
        {
            try
            {
                var value = File.ReadAllText(metadataPath, Encoding.UTF8).Trim();
                return new CatalogMetadata { ETag = Encoding.UTF8.GetString(Convert.FromBase64String(value)) };
            }
            catch { return new CatalogMetadata(); }
        }

        private void WriteMetadata(string etag)
        {
            var parent = Path.GetDirectoryName(metadataPath);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            File.WriteAllText(metadataPath,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(etag ?? string.Empty)), new UTF8Encoding(false));
        }

        private static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void TryDeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
        }

        private sealed class CatalogMetadata { public string ETag; }
    }

    public sealed class CreatorThemeRemoteCatalog
    {
        public int SchemaVersion { get; set; }
        public string GeneratedUtc { get; set; }
        public List<CreatorThemeRemoteEntry> Themes { get; set; }
    }

    public sealed class CreatorThemeRemoteEntry
    {
        public string Id { get; set; }
        public List<CreatorThemeRemoteVersion> Versions { get; set; }
    }

    public sealed class CreatorThemeRemoteVersion
    {
        public string Version { get; set; }
        public int SchemaVersion { get; set; }
        public string MinimumPluginVersion { get; set; }
        public string MaximumPluginVersion { get; set; }
        public string Url { get; set; }
        public string Sha256 { get; set; }
        public long Size { get; set; }
    }

    public sealed class CreatorThemeUpdateResult
    {
        public bool Succeeded { get; private set; }
        public bool Cancelled { get; private set; }
        public bool CatalogCurrent { get; private set; }
        public int Installed { get; private set; }
        public int Updated { get; private set; }
        public int Incompatible { get; private set; }
        public string Error { get; private set; }

        internal static CreatorThemeUpdateResult Success(int installed, int updated,
            int incompatible, bool catalogCurrent)
        {
            return new CreatorThemeUpdateResult { Succeeded = true, Installed = installed,
                Updated = updated, Incompatible = incompatible, CatalogCurrent = catalogCurrent };
        }

        internal static CreatorThemeUpdateResult CancelledResult()
        {
            return new CreatorThemeUpdateResult { Cancelled = true };
        }

        internal static CreatorThemeUpdateResult Failed(string error)
        {
            return new CreatorThemeUpdateResult { Error = error ?? string.Empty };
        }
    }
}
