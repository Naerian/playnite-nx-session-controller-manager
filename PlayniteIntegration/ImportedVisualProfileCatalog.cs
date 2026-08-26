using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>
    /// Persistent catalog of portable visual profiles imported by the user.
    /// Profiles keep their embedded assets and can be selected from any appearance preset list.
    /// </summary>
    public static class ImportedVisualProfileCatalog
    {
        private const string IdPrefix = "Imported:";
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, ImportedVisualProfile> Profiles =
            new Dictionary<string, ImportedVisualProfile>(StringComparer.OrdinalIgnoreCase);
        private static string rootDirectory;

        public static void Configure(string pluginUserDataDirectory)
        {
            lock (Sync)
            {
                rootDirectory = Path.Combine(pluginUserDataDirectory ?? string.Empty, "VisualProfiles");
                ReloadCore();
            }
        }

        public static void Reload()
        {
            lock (Sync) ReloadCore();
        }

        public static bool Contains(string id)
        {
            lock (Sync) return !string.IsNullOrWhiteSpace(id) && Profiles.ContainsKey(id);
        }

        public static string[] GetIds()
        {
            lock (Sync)
                return Profiles.Values.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(a => a.Id).ToArray();
        }

        public static string GetName(string id)
        {
            ImportedVisualProfile profile;
            lock (Sync) return Profiles.TryGetValue(id ?? string.Empty, out profile) ? profile.Name : id;
        }

        public static string Import(string sourcePath)
        {
            var snapshot = VisualProfilePortableStore.Import(sourcePath);
            lock (Sync)
            {
                if (string.IsNullOrWhiteSpace(rootDirectory))
                    throw new InvalidOperationException("The imported visual profile catalog is not configured.");
                Directory.CreateDirectory(rootDirectory);
                var name = string.IsNullOrWhiteSpace(snapshot.Name)
                    ? Path.GetFileNameWithoutExtension(sourcePath)
                    : snapshot.Name.Trim();
                if (string.IsNullOrWhiteSpace(name)) name = "Visual profile";
                var existing = Profiles.Values.FirstOrDefault(a =>
                    string.Equals(a.Name, name, StringComparison.CurrentCultureIgnoreCase));
                var token = existing == null
                    ? Guid.NewGuid().ToString("N")
                    : existing.Id.Substring(IdPrefix.Length);
                var path = existing == null
                    ? Path.Combine(rootDirectory, token + VisualProfileSnapshot.FileExtension)
                    : existing.Path;
                VisualProfilePortableStore.Export(snapshot, path);
                var id = existing == null ? IdPrefix + token : existing.Id;
                Profiles[id] = new ImportedVisualProfile
                {
                    Id = id,
                    Name = name,
                    Path = path,
                    Snapshot = snapshot
                };
                return id;
            }
        }

        public static bool TryGetSnapshot(string id, out VisualProfileSnapshot snapshot)
        {
            ImportedVisualProfile profile;
            lock (Sync)
            {
                if (!Profiles.TryGetValue(id ?? string.Empty, out profile))
                {
                    snapshot = null;
                    return false;
                }
                snapshot = profile.Snapshot;
                return snapshot != null;
            }
        }

        public static bool Delete(string id)
        {
            ImportedVisualProfile profile;
            lock (Sync)
            {
                if (!Profiles.TryGetValue(id ?? string.Empty, out profile)) return false;
                try
                {
                    if (File.Exists(profile.Path)) File.Delete(profile.Path);
                    Profiles.Remove(id);
                    return true;
                }
                catch { return false; }
            }
        }

        private static void ReloadCore()
        {
            Profiles.Clear();
            if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory)) return;
            foreach (var path in Directory.GetFiles(rootDirectory, "*" + VisualProfileSnapshot.FileExtension))
            {
                try
                {
                    var snapshot = VisualProfilePortableStore.Import(path);
                    var token = Path.GetFileNameWithoutExtension(path);
                    var id = IdPrefix + token;
                    Profiles[id] = new ImportedVisualProfile
                    {
                        Id = id,
                        Name = string.IsNullOrWhiteSpace(snapshot.Name) ? token : snapshot.Name.Trim(),
                        Path = path,
                        Snapshot = snapshot
                    };
                }
                catch { /* One damaged profile must not hide the rest of the settings view. */ }
            }
        }

        private sealed class ImportedVisualProfile
        {
            public string Id;
            public string Name;
            public string Path;
            public VisualProfileSnapshot Snapshot;
        }
    }
}
