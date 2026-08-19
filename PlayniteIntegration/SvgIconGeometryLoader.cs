using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace ControllerSessionManager.PlayniteIntegration
{
    internal static class SvgIconGeometryLoader
    {
        private static readonly Dictionary<string, string> Cache =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string GetPathData(string fileName)
        {
            var safeName = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeName))
            {
                return string.Empty;
            }

            string cached;
            if (Cache.TryGetValue(safeName, out cached))
            {
                return cached;
            }

            try
            {
                var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var path = ResolveSvgPath(directory, safeName);
                if (string.IsNullOrEmpty(path))
                {
                    Cache[safeName] = string.Empty;
                    return string.Empty;
                }

                var document = XDocument.Load(path);
                cached = string.Join(" ", document.Descendants()
                    .Where(a => !string.Equals((string)a.Attribute("stroke"), "none", StringComparison.OrdinalIgnoreCase))
                    .Select(GetGeometryData)
                    .Where(a => !string.IsNullOrWhiteSpace(a)));
            }
            catch
            {
                cached = string.Empty;
            }

            Cache[safeName] = cached;
            return cached;
        }

        private static string ResolveSvgPath(string directory, string fileName)
        {
            var candidates = new[]
            {
                Path.Combine(directory, "Gamepads", fileName),
                Path.Combine(directory, "Icons", fileName)
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string GetGeometryData(XElement element)
        {
            if (string.Equals(element.Name.LocalName, "path", StringComparison.OrdinalIgnoreCase))
            {
                return (string)element.Attribute("d");
            }

            if (string.Equals(element.Name.LocalName, "line", StringComparison.OrdinalIgnoreCase))
            {
                var x1 = (string)element.Attribute("x1") ?? "0";
                var y1 = (string)element.Attribute("y1") ?? "0";
                var x2 = (string)element.Attribute("x2") ?? "0";
                var y2 = (string)element.Attribute("y2") ?? "0";
                return string.Format("M {0},{1} L {2},{3}", x1, y1, x2, y2);
            }

            return string.Empty;
        }
    }
}
