using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Xml.Linq;

namespace ControllerSessionManager.Tester.Models
{
    public sealed class TesterSvgGeometryData
    {
        public Geometry Primary { get; set; }
        public Geometry Secondary { get; set; }
    }

    public static class TesterSvgGeometryLoader
    {
        private static readonly Dictionary<string, TesterSvgGeometryData> Cache =
            new Dictionary<string, TesterSvgGeometryData>(StringComparer.OrdinalIgnoreCase);

        public static TesterSvgGeometryData Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
            {
                return new TesterSvgGeometryData();
            }

            lock (Cache)
            {
                TesterSvgGeometryData cached;
                if (Cache.TryGetValue(fileName, out cached))
                {
                    return cached;
                }

                var loaded = LoadCore(fileName);
                Cache[fileName] = loaded;
                return loaded;
            }
        }

        private static TesterSvgGeometryData LoadCore(string fileName)
        {
            try
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var baseDirectory = Path.GetDirectoryName(assemblyPath) ?? AppDomain.CurrentDomain.BaseDirectory;
                var path = Path.Combine(baseDirectory, "Gamepads", "Tester", fileName);
                if (!File.Exists(path))
                {
                    return new TesterSvgGeometryData();
                }

                var document = XDocument.Load(path);
                var paths = document.Descendants().Where(element => element.Name.LocalName == "path").ToList();
                return new TesterSvgGeometryData
                {
                    Primary = ParseGeometry(paths.Where(element => !HasClass(element, "ccsvg__secondary"))),
                    Secondary = ParseGeometry(paths.Where(element => HasClass(element, "ccsvg__secondary")))
                };
            }
            catch
            {
                return new TesterSvgGeometryData();
            }
        }

        private static Geometry ParseGeometry(IEnumerable<XElement> elements)
        {
            var geometryText = string.Join(" ", elements.Select(element =>
            {
                var data = (string)element.Attribute("d") ?? string.Empty;
                var fillRule = (string)element.Attribute("fill-rule");
                return string.Equals(fillRule, "evenodd", StringComparison.OrdinalIgnoreCase)
                    ? "F0 " + data
                    : data;
            }).Where(data => !string.IsNullOrWhiteSpace(data)));

            if (string.IsNullOrWhiteSpace(geometryText))
            {
                return null;
            }

            var geometry = Geometry.Parse(geometryText);
            if (geometry.CanFreeze)
            {
                geometry.Freeze();
            }

            return geometry;
        }

        private static bool HasClass(XElement element, string className)
        {
            var value = (string)element.Attribute("class") ?? string.Empty;
            return value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(item => string.Equals(item, className, StringComparison.Ordinal));
        }
    }
}
