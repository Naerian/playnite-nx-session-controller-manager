using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ControllerSessionManager.Controllers
{
    public sealed class ControllerMappingDatabaseUpdater
    {
        private const string SourceUrl =
            "https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt";
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
        private readonly string bundledPath;
        private readonly string cacheDirectory;
        private readonly string cachedPath;
        private readonly string metadataPath;

        public ControllerMappingDatabaseUpdater(string bundledDatabasePath, string userDataDirectory)
        {
            bundledPath = bundledDatabasePath;
            cacheDirectory = Path.Combine(userDataDirectory, "ControllerDatabase");
            cachedPath = Path.Combine(cacheDirectory, "gamecontrollerdb.txt");
            metadataPath = Path.Combine(cacheDirectory, "gamecontrollerdb.meta");
        }

        public string ActivePath
        {
            get
            {
                return ControllerMappingDatabase.IsValidFile(cachedPath)
                    ? cachedPath : bundledPath;
            }
        }

        public bool ConfigureActiveDatabase()
        {
            return ControllerMappingDatabase.Configure(ActivePath);
        }

        public Task<ControllerMappingDatabaseUpdateResult> CheckForUpdateAsync(bool force)
        {
            return Task.Run(() => CheckForUpdate(force));
        }

        private ControllerMappingDatabaseUpdateResult CheckForUpdate(bool force)
        {
            try
            {
                Directory.CreateDirectory(cacheDirectory);
                var metadata = ReadMetadata();
                if (!force && metadata.CheckedUtc.HasValue &&
                    DateTime.UtcNow - metadata.CheckedUtc.Value < CheckInterval)
                {
                    return ControllerMappingDatabaseUpdateResult.NotDue(ActivePath);
                }

                var request = (HttpWebRequest)WebRequest.Create(SourceUrl);
                request.Method = "GET";
                request.UserAgent = "ControllerManager-Playnite";
                request.Timeout = 10000;
                request.ReadWriteTimeout = 10000;
                request.AllowAutoRedirect = true;
                if (!string.IsNullOrWhiteSpace(metadata.ETag))
                {
                    request.Headers[HttpRequestHeader.IfNoneMatch] = metadata.ETag;
                }

                try
                {
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        var temporary = cachedPath + ".download";
                        using (var input = response.GetResponseStream())
                        using (var output = new FileStream(temporary, FileMode.Create,
                            FileAccess.Write, FileShare.None))
                        {
                            CopyLimited(input, output, 2 * 1024 * 1024);
                        }

                        if (!ControllerMappingDatabase.IsValidFile(temporary))
                        {
                            File.Delete(temporary);
                            WriteMetadata(new DatabaseMetadata
                            {
                                CheckedUtc = DateTime.UtcNow,
                                ETag = metadata.ETag,
                                Sha256 = metadata.Sha256
                            });
                            return ControllerMappingDatabaseUpdateResult.Failed(
                                ActivePath, "The downloaded controller database was invalid.");
                        }

                        var nextHash = ComputeSha256(temporary);
                        var changed = !string.Equals(nextHash, metadata.Sha256,
                            StringComparison.OrdinalIgnoreCase) || !File.Exists(cachedPath);
                        if (changed)
                        {
                            ReplaceAtomically(temporary, cachedPath);
                        }
                        else
                        {
                            File.Delete(temporary);
                        }

                        WriteMetadata(new DatabaseMetadata
                        {
                            CheckedUtc = DateTime.UtcNow,
                            ETag = response.Headers[HttpResponseHeader.ETag],
                            Sha256 = nextHash
                        });
                        ControllerMappingDatabase.Configure(ActivePath);
                        return ControllerMappingDatabaseUpdateResult.Success(ActivePath, changed);
                    }
                }
                catch (WebException error)
                {
                    var response = error.Response as HttpWebResponse;
                    if (response != null && response.StatusCode == HttpStatusCode.NotModified)
                    {
                        metadata.CheckedUtc = DateTime.UtcNow;
                        WriteMetadata(metadata);
                        return ControllerMappingDatabaseUpdateResult.Success(ActivePath, false);
                    }
                    return ControllerMappingDatabaseUpdateResult.Failed(ActivePath, error.Message);
                }
            }
            catch (Exception error)
            {
                return ControllerMappingDatabaseUpdateResult.Failed(ActivePath, error.Message);
            }
        }

        private static void CopyLimited(Stream input, Stream output, int maximumBytes)
        {
            if (input == null)
            {
                throw new InvalidDataException("The controller database response was empty.");
            }
            var buffer = new byte[32768];
            var total = 0;
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > maximumBytes)
                {
                    throw new InvalidDataException("The controller database was larger than expected.");
                }
                output.Write(buffer, 0, read);
            }
        }

        private static void ReplaceAtomically(string source, string destination)
        {
            if (!File.Exists(destination))
            {
                File.Move(source, destination);
                return;
            }

            var backup = destination + ".previous";
            File.Replace(source, destination, backup, true);
        }

        private static string ComputeSha256(string path)
        {
            using (var input = File.OpenRead(path))
            using (var hash = SHA256.Create())
            {
                return BitConverter.ToString(hash.ComputeHash(input)).Replace("-", string.Empty);
            }
        }

        private DatabaseMetadata ReadMetadata()
        {
            var result = new DatabaseMetadata();
            try
            {
                foreach (var line in File.ReadAllLines(metadataPath))
                {
                    var separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }
                    var key = line.Substring(0, separator);
                    var value = line.Substring(separator + 1);
                    long ticks;
                    if (key == "CheckedUtc" && long.TryParse(value, out ticks))
                    {
                        result.CheckedUtc = new DateTime(ticks, DateTimeKind.Utc);
                    }
                    else if (key == "ETag")
                    {
                        result.ETag = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                    }
                    else if (key == "Sha256")
                    {
                        result.Sha256 = value;
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        private void WriteMetadata(DatabaseMetadata metadata)
        {
            var etag = Convert.ToBase64String(Encoding.UTF8.GetBytes(metadata.ETag ?? string.Empty));
            File.WriteAllLines(metadataPath, new[]
            {
                "CheckedUtc=" + (metadata.CheckedUtc ?? DateTime.UtcNow).Ticks,
                "ETag=" + etag,
                "Sha256=" + (metadata.Sha256 ?? string.Empty)
            });
        }

        private sealed class DatabaseMetadata
        {
            public DateTime? CheckedUtc;
            public string ETag;
            public string Sha256;
        }
    }

    public sealed class ControllerMappingDatabaseUpdateResult
    {
        public bool Checked { get; private set; }
        public bool Updated { get; private set; }
        public bool Succeeded { get; private set; }
        public string ActivePath { get; private set; }
        public string Error { get; private set; }

        internal static ControllerMappingDatabaseUpdateResult NotDue(string path)
        {
            return new ControllerMappingDatabaseUpdateResult
                { ActivePath = path, Succeeded = true };
        }

        internal static ControllerMappingDatabaseUpdateResult Success(string path, bool updated)
        {
            return new ControllerMappingDatabaseUpdateResult
                { ActivePath = path, Checked = true, Updated = updated, Succeeded = true };
        }

        internal static ControllerMappingDatabaseUpdateResult Failed(string path, string error)
        {
            return new ControllerMappingDatabaseUpdateResult
                { ActivePath = path, Checked = true, Error = error, Succeeded = false };
        }
    }
}
