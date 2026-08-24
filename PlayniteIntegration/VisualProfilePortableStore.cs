using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web.Script.Serialization;

namespace ControllerSessionManager.PlayniteIntegration
{
    internal static class VisualProfilePortableStore
    {
        private const string ManifestEntry = "visual-profile.json";

        public static void Export(VisualProfileSnapshot snapshot, string filePath)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A file path is required.", "filePath");
            }

            var json = Serialize(snapshot);
            using (var stream = File.Create(filePath))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry(ManifestEntry, CompressionLevel.Optimal);
                using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                {
                    writer.Write(json);
                }
            }
        }

        public static VisualProfileSnapshot Import(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("Visual profile file was not found.", filePath);
            }

            string json;
            if (string.Equals(Path.GetExtension(filePath), ".json", StringComparison.OrdinalIgnoreCase))
            {
                json = File.ReadAllText(filePath, Encoding.UTF8);
            }
            else
            {
                using (var stream = File.OpenRead(filePath))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    var entry = archive.GetEntry(ManifestEntry);
                    if (entry == null)
                    {
                        throw new InvalidDataException("The selected file is not a Controller Manager visual profile.");
                    }

                    using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                    {
                        json = reader.ReadToEnd();
                    }
                }
            }

            var snapshot = Deserialize(json);
            if (snapshot == null)
            {
                throw new InvalidDataException("The visual profile manifest is empty or invalid.");
            }

            if (snapshot.Version <= 0 || snapshot.Version > VisualProfileSnapshot.CurrentVersion)
            {
                throw new InvalidDataException(
                    "This visual profile was created with a newer version of Controller Manager.");
            }

            return snapshot;
        }

        private static string Serialize(VisualProfileSnapshot snapshot)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            return serializer.Serialize(snapshot);
        }

        private static VisualProfileSnapshot Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            return serializer.Deserialize<VisualProfileSnapshot>(json);
        }
    }
}
