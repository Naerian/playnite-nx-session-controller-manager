using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using Playnite.SDK;

namespace ControllerSessionManager.PlayniteIntegration
{
    internal enum ThemeAppearanceSurface
    {
        DesktopNotification,
        FullscreenNotification,
        Overlay
    }

    /// <summary>
    /// Resolves live Playnite theme colors at display time. Theme authors map their own
    /// resource keys in ControllerManager/theme-bridge.json so color packs keep working
    /// without Controller Manager knowing Aniki, Helium, or any other naming scheme.
    /// </summary>
    internal static class ThemeAppearanceBridge
    {
        public const string RelativePath = "ControllerManager/theme-bridge.json";

        public static ThemeAppearanceColors Resolve(IPlayniteAPI api, ThemeAppearanceSurface surface)
        {
            var colors = new ThemeAppearanceColors();
            var fullscreenTheme = surface != ThemeAppearanceSurface.DesktopNotification &&
                (surface == ThemeAppearanceSurface.FullscreenNotification || IsFullscreen(api));
            var mapping = ReadMapping(FindThemeDirectory(api, fullscreenTheme));
            IDictionary<string, string> keys = ToStringMap(mapping == null
                ? null
                : surface == ThemeAppearanceSurface.Overlay ? mapping.Overlay : mapping.Notification);
            Bind(keys, "Background", null, colors.SetBackground);
            Bind(keys, "Gradient", null, colors.SetGradient);
            Bind(keys, "Text", null, colors.SetText);
            Bind(keys, "SecondaryText", null, colors.SetSecondaryText);
            Bind(keys, "Accent", null, colors.SetAccent);
            Bind(keys, "Border", null, colors.SetBorder);
            Bind(keys, "Warning", null, colors.SetWarning);
            BindStyle(keys, "TextStyle", colors.ApplyTextStyle);
            BindStyle(keys, "TitleStyle", colors.ApplyTitleStyle);
            BindStyle(keys, "MessageStyle", colors.ApplyMessageStyle);
            return colors;
        }

        private static Dictionary<string, string> ToStringMap(Dictionary<string, object> source)
        {
            if (source == null) return null;
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in source)
            {
                map[pair.Key] = pair.Value == null
                    ? null
                    : Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
            }
            return map;
        }

        private static bool IsFullscreen(IPlayniteAPI api)
        {
            return api != null && api.ApplicationInfo != null &&
                api.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
        }

        private static void Bind(IDictionary<string, string> keys, string name, string fallback,
            Action<string, bool> apply)
        {
            string resourceKey = Lookup(keys, name);
            if (string.IsNullOrWhiteSpace(resourceKey)) resourceKey = fallback;
            string hex;
            bool gradient;
            if (!string.IsNullOrWhiteSpace(resourceKey) &&
                TryResolve(resourceKey, out hex, out gradient))
            {
                apply(hex, gradient);
            }
        }

        private static string Lookup(IDictionary<string, string> keys, string name)
        {
            if (keys == null || string.IsNullOrWhiteSpace(name)) return null;
            string value;
            if (keys.TryGetValue(name, out value)) return value;
            foreach (var pair in keys)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }
            return null;
        }

        private static void BindStyle(IDictionary<string, string> keys, string name,
            Action<ThemeTypeface> apply)
        {
            var resourceKey = Lookup(keys, name);
            ThemeTypeface typeface;
            if (!string.IsNullOrWhiteSpace(resourceKey) && TryResolveStyle(resourceKey, out typeface))
                apply(typeface);
        }

        internal static bool TryResolveStyle(string resourceKey, out ThemeTypeface typeface)
        {
            typeface = new ThemeTypeface();
            if (string.IsNullOrWhiteSpace(resourceKey) || Application.Current == null) return false;
            object resource = null;
            try
            {
                resource = Application.Current.TryFindResource(resourceKey.Trim());
            }
            catch
            {
                return false;
            }
            var style = resource as Style;
            if (style == null) return false;
            ApplyStyleSetters(style, typeface);
            return typeface.HasAny;
        }

        private static void ApplyStyleSetters(Style style, ThemeTypeface typeface)
        {
            if (style == null) return;
            if (style.BasedOn != null) ApplyStyleSetters(style.BasedOn, typeface);
            foreach (var baseSetter in style.Setters)
            {
                var setter = baseSetter as Setter;
                if (setter == null || setter.Property == null) continue;
                var value = ResolveSetterValue(setter.Value);
                var property = setter.Property.Name;
                if (string.Equals(property, "FontFamily", StringComparison.OrdinalIgnoreCase))
                {
                    var family = FormatFontFamily(value);
                    if (!string.IsNullOrWhiteSpace(family)) typeface.FontFamily = family;
                }
                else if (string.Equals(property, "FontWeight", StringComparison.OrdinalIgnoreCase))
                {
                    var weight = FormatFontWeight(value);
                    if (!string.IsNullOrWhiteSpace(weight)) typeface.FontWeight = weight;
                }
                else if (string.Equals(property, "Foreground", StringComparison.OrdinalIgnoreCase))
                {
                    string hex;
                    bool gradient;
                    if (TryConvert(value, out hex, out gradient)) typeface.Foreground = hex;
                }
            }
        }

        private static object ResolveSetterValue(object value)
        {
            var dynamicResource = value as DynamicResourceExtension;
            if (dynamicResource != null && dynamicResource.ResourceKey != null && Application.Current != null)
            {
                try
                {
                    var resolved = Application.Current.TryFindResource(dynamicResource.ResourceKey);
                    if (resolved != null) return resolved;
                }
                catch { }
            }
            return value;
        }

        private static string FormatFontFamily(object value)
        {
            var family = value as FontFamily;
            if (family != null)
            {
                if (!string.IsNullOrWhiteSpace(family.Source) &&
                    family.Source.IndexOf("://", StringComparison.OrdinalIgnoreCase) < 0)
                    return family.Source.Trim();
                foreach (var name in family.FamilyNames.Values)
                {
                    if (!string.IsNullOrWhiteSpace(name)) return name.Trim();
                }
            }
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static string FormatFontWeight(object value)
        {
            if (value is FontWeight)
            {
                var weight = (FontWeight)value;
                if (weight >= FontWeights.Bold) return "Bold";
                if (weight >= FontWeights.SemiBold) return "SemiBold";
                return "Regular";
            }
            return NotificationFontCatalog.NormalizeWeight(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public static bool TryResolve(string resourceKey, out string hex, out bool isGradient)
        {
            hex = null;
            isGradient = false;
            if (string.IsNullOrWhiteSpace(resourceKey) || Application.Current == null) return false;
            object resource = null;
            try
            {
                resource = Application.Current.TryFindResource(resourceKey.Trim());
            }
            catch
            {
                return false;
            }
            return TryConvert(resource, out hex, out isGradient);
        }

        internal static bool TryConvert(object resource, out string hex, out bool isGradient)
        {
            hex = null;
            isGradient = false;
            if (resource is Color)
            {
                var color = (Color)resource;
                if (color.A == 0) return false;
                hex = Format(color);
                return true;
            }
            var solid = resource as SolidColorBrush;
            if (solid != null)
            {
                if (solid.Color.A == 0) return false;
                hex = Format(solid.Color);
                return true;
            }
            var gradient = resource as GradientBrush;
            if (gradient != null && gradient.GradientStops != null && gradient.GradientStops.Count > 0)
            {
                for (var i = gradient.GradientStops.Count - 1; i >= 0; i--)
                {
                    var stop = gradient.GradientStops[i].Color;
                    if (stop.A == 0) continue;
                    hex = Format(stop);
                    isGradient = true;
                    return true;
                }
                return false;
            }
            return false;
        }

        public static string FindThemeDirectory(IPlayniteAPI api, bool fullscreen)
        {
            if (api == null || api.ApplicationSettings == null) return string.Empty;
            var themeId = fullscreen
                ? api.ApplicationSettings.FullscreenTheme
                : api.ApplicationSettings.DesktopTheme;
            if (string.IsNullOrWhiteSpace(themeId)) return string.Empty;
            var mode = fullscreen ? "Fullscreen" : "Desktop";
            var roots = new List<string>();
            if (api.Paths != null)
            {
                if (!string.IsNullOrWhiteSpace(api.Paths.ApplicationPath))
                    roots.Add(Path.Combine(api.Paths.ApplicationPath, "Themes", mode));
                if (!string.IsNullOrWhiteSpace(api.Paths.ConfigurationPath))
                    roots.Add(Path.Combine(api.Paths.ConfigurationPath, "Themes", mode));
            }
            foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(root)) continue;
                var exact = Path.Combine(root, themeId.Trim());
                if (Directory.Exists(exact)) return exact;
                try
                {
                    var wanted = themeId.Trim();
                    foreach (var directory in Directory.GetDirectories(root))
                    {
                        var name = Path.GetFileName(directory);
                        if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase) ||
                            name.EndsWith("_" + wanted, StringComparison.OrdinalIgnoreCase))
                            return directory;
                    }
                }
                catch { }
            }
            return string.Empty;
        }

        private static ThemeBridgeFile ReadMapping(string themeDirectory)
        {
            if (string.IsNullOrWhiteSpace(themeDirectory)) return null;
            var path = Path.Combine(themeDirectory, "ControllerManager", "theme-bridge.json");
            if (!File.Exists(path)) return null;
            try
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = 256 * 1024 };
                var raw = serializer.Deserialize<Dictionary<string, object>>(
                    File.ReadAllText(path, System.Text.Encoding.UTF8).TrimStart('\uFEFF'));
                if (raw == null) return null;
                return new ThemeBridgeFile
                {
                    Notification = AsObjectMap(LookupRaw(raw, "Notification")),
                    Overlay = AsObjectMap(LookupRaw(raw, "Overlay"))
                };
            }
            catch
            {
                return null;
            }
        }

        private static object LookupRaw(Dictionary<string, object> raw, string name)
        {
            object value;
            if (raw.TryGetValue(name, out value)) return value;
            foreach (var pair in raw)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }
            return null;
        }

        private static Dictionary<string, object> AsObjectMap(object value)
        {
            var map = value as Dictionary<string, object>;
            if (map != null) return map;
            return null;
        }

        private static string Format(Color color)
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}",
                color.A, color.R, color.G, color.B);
        }

        public sealed class ThemeBridgeFile
        {
            public Dictionary<string, object> Notification { get; set; }
            public Dictionary<string, object> Overlay { get; set; }
        }

        public sealed class ThemeAppearanceColors
        {
            public string Background { get; private set; }
            public string Gradient { get; private set; }
            public string Text { get; private set; }
            public string SecondaryText { get; private set; }
            public string Accent { get; private set; }
            public string Border { get; private set; }
            public string Warning { get; private set; }
            public string FontFamily { get; private set; }
            public string FontWeight { get; private set; }
            public string TitleFontFamily { get; private set; }
            public string TitleFontWeight { get; private set; }
            public string MessageFontFamily { get; private set; }
            public string MessageFontWeight { get; private set; }
            public bool HasGradient { get; private set; }

            public bool HasAny
            {
                get
                {
                    return !string.IsNullOrWhiteSpace(Background) ||
                        !string.IsNullOrWhiteSpace(Gradient) ||
                        !string.IsNullOrWhiteSpace(Text) ||
                        !string.IsNullOrWhiteSpace(SecondaryText) ||
                        !string.IsNullOrWhiteSpace(Accent) ||
                        !string.IsNullOrWhiteSpace(Border) ||
                        !string.IsNullOrWhiteSpace(Warning) ||
                        !string.IsNullOrWhiteSpace(FontFamily) ||
                        !string.IsNullOrWhiteSpace(TitleFontFamily) ||
                        !string.IsNullOrWhiteSpace(MessageFontFamily);
                }
            }

            public void SetBackground(string hex, bool gradient)
            {
                Background = hex;
            }

            public void SetGradient(string hex, bool gradient)
            {
                Gradient = hex;
                if (gradient) HasGradient = true;
            }

            public void SetText(string hex, bool gradient) { Text = hex; }
            public void SetSecondaryText(string hex, bool gradient) { SecondaryText = hex; }
            public void SetAccent(string hex, bool gradient) { Accent = hex; }
            public void SetBorder(string hex, bool gradient) { Border = hex; }
            public void SetWarning(string hex, bool gradient) { Warning = hex; }

            public void ApplyTextStyle(ThemeTypeface typeface)
            {
                if (typeface == null) return;
                if (!string.IsNullOrWhiteSpace(typeface.FontFamily)) FontFamily = typeface.FontFamily;
                if (!string.IsNullOrWhiteSpace(typeface.FontWeight)) FontWeight = typeface.FontWeight;
                if (string.IsNullOrWhiteSpace(Text) && !string.IsNullOrWhiteSpace(typeface.Foreground))
                    Text = typeface.Foreground;
            }

            public void ApplyTitleStyle(ThemeTypeface typeface)
            {
                if (typeface == null) return;
                if (!string.IsNullOrWhiteSpace(typeface.FontFamily)) TitleFontFamily = typeface.FontFamily;
                if (!string.IsNullOrWhiteSpace(typeface.FontWeight)) TitleFontWeight = typeface.FontWeight;
            }

            public void ApplyMessageStyle(ThemeTypeface typeface)
            {
                if (typeface == null) return;
                if (!string.IsNullOrWhiteSpace(typeface.FontFamily)) MessageFontFamily = typeface.FontFamily;
                if (!string.IsNullOrWhiteSpace(typeface.FontWeight)) MessageFontWeight = typeface.FontWeight;
            }
        }

        internal sealed class ThemeTypeface
        {
            public string FontFamily { get; set; }
            public string FontWeight { get; set; }
            public string Foreground { get; set; }
            public bool HasAny
            {
                get
                {
                    return !string.IsNullOrWhiteSpace(FontFamily) ||
                        !string.IsNullOrWhiteSpace(FontWeight) ||
                        !string.IsNullOrWhiteSpace(Foreground);
                }
            }
        }
    }
}
