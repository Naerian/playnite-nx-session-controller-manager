using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ControllerSessionManager.Controllers
{
    /// <summary>
    /// Read-only fallback over SDL_GameControllerDB. It does not mutate Playnite's process-wide
    /// SDL state; the isolated TesterHost loads the same file through SDL itself.
    /// </summary>
    public static class ControllerMappingDatabase
    {
        private const int MaximumDatabaseBytes = 2 * 1024 * 1024;
        private const int MinimumWindowsMappings = 100;
        private static readonly object sync = new object();
        private static string configuredPath;
        private static Dictionary<string, string> windowsNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string ConfiguredPath
        {
            get { lock (sync) { return configuredPath; } }
        }

        public static bool Configure(string path)
        {
            Dictionary<string, string> parsed;
            if (!TryLoad(path, out parsed))
            {
                return false;
            }

            lock (sync)
            {
                configuredPath = Path.GetFullPath(path);
                windowsNames = parsed;
            }
            return true;
        }

        public static string ResolveName(string sdlGuid, string fallback)
        {
            var normalized = NormalizeGuid(sdlGuid);
            if (normalized.Length != 32)
            {
                return fallback;
            }

            lock (sync)
            {
                string mapped;
                return windowsNames.TryGetValue(normalized, out mapped) &&
                    !string.IsNullOrWhiteSpace(mapped)
                    ? mapped : fallback;
            }
        }

        public static bool IsValidFile(string path)
        {
            Dictionary<string, string> ignored;
            return TryLoad(path, out ignored);
        }

        private static bool TryLoad(string path, out Dictionary<string, string> result)
        {
            result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists || file.Length <= 0 || file.Length > MaximumDatabaseBytes)
                {
                    return false;
                }

                foreach (var rawLine in File.ReadLines(file.FullName))
                {
                    var line = rawLine == null ? string.Empty : rawLine.Trim();
                    if (line.Length == 0 || line[0] == '#' || line.Length > 16384)
                    {
                        continue;
                    }

                    var fields = line.Split(',');
                    if (fields.Length < 4 ||
                        !fields.Skip(2).Any(a => string.Equals(a.Trim(), "platform:Windows",
                            StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var guid = NormalizeGuid(fields[0]);
                    var name = fields[1].Trim();
                    if (guid.Length == 32 && name.Length > 0 && name.Length <= 256)
                    {
                        result[guid] = name;
                    }
                }

                return result.Count >= MinimumWindowsMappings;
            }
            catch
            {
                result.Clear();
                return false;
            }
        }

        private static string NormalizeGuid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chars = value.Trim().Where(Uri.IsHexDigit).Select(char.ToLowerInvariant).ToArray();
            return chars.Length == 32 ? new string(chars) : string.Empty;
        }
    }
}
