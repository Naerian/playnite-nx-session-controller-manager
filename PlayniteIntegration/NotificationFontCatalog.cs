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
        public const string Exo2 = "Exo 2";
        public const string Inter = "Inter";
        public const string Montserrat = "Montserrat";
        public const string Orbitron = "Orbitron";
        public const string Outfit = "Outfit";
        public const string Poppins = "Poppins";
        public const string Rajdhani = "Rajdhani";
        public const string Trebuchet = "Trebuchet MS";

        private static readonly string[] BuiltInFonts =
        {
            SystemDefault, Inter, Montserrat, Outfit, Poppins, Rajdhani, ChakraPetch, Exo2, Orbitron, Trebuchet
        };
        private static readonly IList<string> ExternalFonts = new List<string>();
        public static string[] NamedFonts
        {
            get
            {
                lock (ExternalFonts)
                {
                    var values = new List<string>(BuiltInFonts);
                    foreach (var value in ExternalFonts) if (!values.Contains(value)) values.Add(value);
                    return values.ToArray();
                }
            }
        }

        private static readonly IDictionary<string, string> FolderNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ChakraPetch, "Chakra_Petch" }, { Exo2, "Exo2" }, { Inter, "Inter" }, { Montserrat, "Montserrat" },
                { Orbitron, "Orbitron" }, { Outfit, "Outfit" }, { Poppins, "Poppins" }, { Rajdhani, "Rajdhani" }
            };

        private static readonly IDictionary<string, string> FamilyNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ChakraPetch, "Chakra Petch" }, { Exo2, "Exo 2" }, { Inter, "Inter 18pt 18pt" }, { Montserrat, "Montserrat" },
                { Orbitron, "Orbitron" }, { Outfit, "Outfit" }, { Poppins, "Poppins" }, { Rajdhani, "Rajdhani" }
            };

        private static readonly IDictionary<string, FontFamily> Cache =
            new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);

        public static string Normalize(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("ExternalFont|",
                StringComparison.Ordinal)) return value;
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
            if (id == Trebuchet)
            {
                return new FontFamily("Trebuchet MS");
            }
            if (id.StartsWith("ExternalFont|", StringComparison.Ordinal))
            {
                try
                {
                    var parts = id.Split('|');
                    var folder = Decode(parts[1]);
                    var externalFamilyName = Decode(parts[2]);
                    return new FontFamily(new Uri(folder.TrimEnd(Path.DirectorySeparatorChar) +
                        Path.DirectorySeparatorChar, UriKind.Absolute), "./#" + externalFamilyName);
                }
                catch { return SystemFonts.MessageFontFamily; }
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

        public static string RegisterExternalFont(string folder, string familyName, string displayName)
        {
            if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(familyName) ||
                !Directory.Exists(folder)) return SystemDefault;
            var token = "ExternalFont|" + Encode(Path.GetFullPath(folder)) + "|" + Encode(familyName) +
                "|" + Encode(string.IsNullOrWhiteSpace(displayName) ? familyName : displayName);
            lock (ExternalFonts) if (!ExternalFonts.Contains(token)) ExternalFonts.Add(token);
            return token;
        }

        public static string DisplayName(string value)
        {
            if (value != null && value.StartsWith("ExternalFont|", StringComparison.Ordinal))
            {
                try { return Decode(value.Split('|')[3]); } catch { }
            }
            return value;
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string Decode(string value)
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
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
