using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>
    /// Resolves the font families shipped with the extension without requiring a Windows install.
    /// This file is linked into OverlayHost so both processes resolve fonts from their own binary folder.
    /// </summary>
    public static class NotificationFontCatalog
    {
        public const string SystemDefault = "Default";
        public const string ChakraPetch = "ChakraPetch";
        public const string Inter = "Inter";
        public const string Montserrat = "Montserrat";
        public const string Orbitron = "Orbitron";
        public const string Outfit = "Outfit";
        public const string Poppins = "Poppins";
        public const string Rajdhani = "Rajdhani";

        public static readonly string[] NamedFonts =
        {
            SystemDefault, Inter, Montserrat, Outfit, Poppins, Rajdhani, ChakraPetch, Orbitron
        };

        private static readonly IDictionary<string, string> FolderNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ChakraPetch, "Chakra_Petch" }, { Inter, "Inter" }, { Montserrat, "Montserrat" },
                { Orbitron, "Orbitron" }, { Outfit, "Outfit" }, { Poppins, "Poppins" }, { Rajdhani, "Rajdhani" }
            };

        private static readonly IDictionary<string, string> FamilyNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ChakraPetch, "Chakra Petch" }, { Inter, "Inter 18pt 18pt" }, { Montserrat, "Montserrat" },
                { Orbitron, "Orbitron" }, { Outfit, "Outfit" }, { Poppins, "Poppins" }, { Rajdhani, "Rajdhani" }
            };

        private static readonly IDictionary<string, FontFamily> Cache =
            new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);

        public static string Normalize(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                foreach (var font in NamedFonts)
                {
                    if (string.Equals(font, value.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return font;
                    }
                }
            }

            return SystemDefault;
        }

        public static FontFamily Resolve(string value)
        {
            return Resolve(value, "Regular");
        }

        public static FontFamily Resolve(string value, string weight)
        {
            var id = Normalize(value);
            if (id == SystemDefault)
            {
                return SystemFonts.MessageFontFamily;
            }

            var normalizedWeight = NormalizeWeight(weight);
            var cacheKey = id + "|" + normalizedWeight;
            FontFamily cached;
            if (Cache.TryGetValue(cacheKey, out cached))
            {
                return cached;
            }

            string folderName;
            string familyName;
            if (!FolderNames.TryGetValue(id, out folderName) || !FamilyNames.TryGetValue(id, out familyName))
            {
                return SystemFonts.MessageFontFamily;
            }

            try
            {
                var binaryPath = typeof(NotificationFontCatalog).Assembly.Location;
                var basePath = Path.GetDirectoryName(binaryPath) ?? AppDomain.CurrentDomain.BaseDirectory;
                var fontFolder = Path.Combine(basePath, "Fonts", folderName);
                if (!Directory.Exists(fontFolder))
                {
                    return SystemFonts.MessageFontFamily;
                }

                var folderUri = new Uri(fontFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                    UriKind.Absolute);
                if (normalizedWeight == "SemiBold" || normalizedWeight == "Bold") familyName += " SemiBold";
                var family = new FontFamily(folderUri, "./#" + familyName);
                Cache[cacheKey] = family;
                return family;
            }
            catch
            {
            }

            return SystemFonts.MessageFontFamily;
        }

        public static string NormalizeWeight(string value)
        {
            // Medium was exposed in 1.0.21, but the bundled face was visually indistinguishable
            // from SemiBold in WPF. Preserve old profiles by treating it as the surviving weight.
            if (string.Equals(value, "Medium", StringComparison.OrdinalIgnoreCase))
            {
                return "SemiBold";
            }

            if (string.Equals(value, "Regular", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Bold", StringComparison.OrdinalIgnoreCase))
            {
                return char.ToUpperInvariant(value.Trim()[0]) + value.Trim().Substring(1).ToLowerInvariant();
            }

            return "SemiBold";
        }

        public static FontWeight ResolveWeight(string value)
        {
            switch (NormalizeWeight(value))
            {
                case "Regular": return FontWeights.Normal;
                case "Bold": return FontWeights.Bold;
                default: return FontWeights.SemiBold;
            }
        }

        public static FontWeight ResolveEffectiveWeight(string fontFamily, string value)
        {
            if (Normalize(fontFamily) == SystemDefault)
            {
                return ResolveWeight(value);
            }

            return NormalizeWeight(value) == "Bold" ? FontWeights.Bold : FontWeights.Normal;
        }

        public static string NormalizeAlignment(string value)
        {
            if (string.Equals(value, "Center", StringComparison.OrdinalIgnoreCase)) return "Center";
            if (string.Equals(value, "Right", StringComparison.OrdinalIgnoreCase)) return "Right";
            return "Left";
        }

        public static TextAlignment ResolveAlignment(string value)
        {
            switch (NormalizeAlignment(value))
            {
                case "Center": return TextAlignment.Center;
                case "Right": return TextAlignment.Right;
                default: return TextAlignment.Left;
            }
        }

        public static string NormalizeAccentMode(string value)
        {
            if (string.Equals(value, "IconOnly", StringComparison.OrdinalIgnoreCase)) return "IconOnly";
            if (string.Equals(value, "TintedBackground", StringComparison.OrdinalIgnoreCase)) return "TintedBackground";
            if (string.Equals(value, "SolidBackground", StringComparison.OrdinalIgnoreCase)) return "SolidBackground";
            return "IconAndBorder";
        }

        public static string NormalizeAnimation(string value)
        {
            if (string.Equals(value, "None", StringComparison.OrdinalIgnoreCase)) return "None";
            if (string.Equals(value, "Slide", StringComparison.OrdinalIgnoreCase)) return "Slide";
            if (string.Equals(value, "Scale", StringComparison.OrdinalIgnoreCase)) return "Scale";
            return "Fade";
        }
    }
}
