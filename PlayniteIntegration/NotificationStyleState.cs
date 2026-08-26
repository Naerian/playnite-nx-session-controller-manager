using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>
    /// Captures only notification appearance. Values are strings so Playnite can keep the
    /// saved Custom style forward-compatible without duplicating a large settings object.
    /// </summary>
    public static class NotificationStyleState
    {
        private static readonly string[] StyleSuffixes =
        {
            "Width", "ScalePercent", "DurationMilliseconds", "Position",
            "BackgroundColor", "UseGradient", "GradientColor", "GradientAngle",
            "UseBackgroundImage", "BackgroundImagePath",
            "BackgroundImageStretch", "BackgroundImageHorizontalAlignment",
            "BackgroundImageVerticalAlignment", "BackgroundImageOpacity",
            "BackgroundImageTintOpacity", "TextColor", "SecondaryTextColor",
            "ConnectedColor", "DisconnectedColor", "WarningColor", "LowBatteryColor",
            "TitleFontSize", "MessageFontSize", "IconSize", "ShowIconContainer",
            "IconContainerColor", "IconContainerBorderColor", "IconContainerBorderThickness",
            "IconContainerCornerRadius", "IconContainerPadding", "IconPosition", "Padding",
            "ElementSpacing", "IconSpacing", "ShowBorder", "BorderPosition",
            "BorderThickness", "UseBorderGradient", "UseStateBorderColors",
            "ConnectedBorderColor", "DisconnectedBorderColor", "WarningBorderColor", "LowBatteryBorderColor", "BorderGradientStartColor",
            "BorderGradientEndColor", "BorderGradientAngle", "ShowBorderGlow",
            "BorderGlowColor", "BorderGlowBlur", "BorderGlowOpacity",
            "CornerRadius", "ShowConnectionBadge", "ScreenMargin",
            "ShowShadow", "FontFamily", "FontWeight", "TextAlignment", "AccentMode",
            "Animation", "ShowTitle", "TitleFontFamily", "TitleFontWeight",
            "MessageFontFamily", "MessageFontWeight", "MessageMaxLines", "BadgePosition",
            "UppercaseTitle", "TextOrder", "UseIndependentBorders",
            "BorderLeftThickness", "BorderTopThickness", "BorderRightThickness",
            "BorderBottomThickness", "UseStateBackgroundColors",
            "ConnectedBackgroundColor", "DisconnectedBackgroundColor",
            "WarningBackgroundColor", "LowBatteryBackgroundColor"
        };

        private static readonly string[] PropertyNames = StyleSuffixes
            .SelectMany(a => new[] { "Notification" + a, "DesktopNotification" + a })
            .Concat(new[]
            {
                "ShowControllerNameInNotifications",
                "ShowControllerNameInDesktopNotifications"
            })
            .ToArray();

        public static Dictionary<string, string> Capture(ControllerSessionManagerSettings settings)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (settings == null)
            {
                return result;
            }

            foreach (var name in PropertyNames)
            {
                var property = GetProperty(name);
                if (property == null)
                {
                    continue;
                }
                var value = property.GetValue(settings, null);
                result[name] = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
            return result;
        }

        public static Dictionary<string, string> CaptureFullscreen(ControllerSessionManagerSettings settings)
        {
            return CaptureSurface(settings, "Notification", "ShowControllerNameInNotifications");
        }

        public static Dictionary<string, string> CaptureDesktop(ControllerSessionManagerSettings settings)
        {
            return CaptureSurface(settings, "DesktopNotification", "ShowControllerNameInDesktopNotifications");
        }

        public static void Apply(ControllerSessionManagerSettings settings,
            IDictionary<string, string> values)
        {
            if (settings == null || values == null)
            {
                return;
            }

            foreach (var item in values)
            {
                var property = GetProperty(item.Key);
                if (property == null || !property.CanWrite)
                {
                    continue;
                }

                object value;
                if (property.PropertyType == typeof(bool))
                {
                    bool parsed;
                    if (!bool.TryParse(item.Value, out parsed)) continue;
                    value = parsed;
                }
                else if (property.PropertyType == typeof(int))
                {
                    int parsed;
                    if (!int.TryParse(item.Value, NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out parsed)) continue;
                    value = parsed;
                }
                else if (property.PropertyType == typeof(double))
                {
                    double parsed;
                    if (!double.TryParse(item.Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out parsed)) continue;
                    value = parsed;
                }
                else
                {
                    value = item.Value ?? string.Empty;
                }
                property.SetValue(settings, value, null);
            }
        }

        public static bool Matches(ControllerSessionManagerSettings settings,
            IDictionary<string, string> values)
        {
            if (values == null || values.Count == 0)
            {
                return false;
            }
            var current = Capture(settings);
            return current.Count == values.Count && current.All(a =>
                values.ContainsKey(a.Key) && string.Equals(values[a.Key], a.Value,
                    StringComparison.Ordinal));
        }

        public static bool MatchesFullscreen(ControllerSessionManagerSettings settings,
            IDictionary<string, string> values)
        {
            return MatchesSurface(CaptureFullscreen(settings), values);
        }

        public static bool MatchesDesktop(ControllerSessionManagerSettings settings,
            IDictionary<string, string> values)
        {
            return MatchesSurface(CaptureDesktop(settings), values);
        }

        public static void ApplyFullscreen(ControllerSessionManagerSettings settings,
            IDictionary<string, string> values)
        {
            Apply(settings, FilterSurface(values, "Notification", "ShowControllerNameInNotifications"));
        }

        public static void ApplyDesktop(ControllerSessionManagerSettings settings,
            IDictionary<string, string> values)
        {
            Apply(settings, FilterSurface(values, "DesktopNotification", "ShowControllerNameInDesktopNotifications"));
        }

        public static void CopyDesktopToFullscreen(ControllerSessionManagerSettings settings)
        {
            Copy(settings, "DesktopNotification", "Notification");
            settings.ShowControllerNameInNotifications = settings.ShowControllerNameInDesktopNotifications;
        }

        public static void CopyFullscreenToDesktop(ControllerSessionManagerSettings settings)
        {
            Copy(settings, "Notification", "DesktopNotification");
            settings.ShowControllerNameInDesktopNotifications = settings.ShowControllerNameInNotifications;
        }

        public static Dictionary<string, string> Clone(IDictionary<string, string> values)
        {
            return values == null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(values, StringComparer.Ordinal);
        }

        private static Dictionary<string, string> CaptureSurface(ControllerSessionManagerSettings settings,
            string prefix, string controllerNameProperty)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (settings == null) return result;
            foreach (var name in StyleSuffixes.Select(a => prefix + a).Concat(new[] { controllerNameProperty }))
            {
                var property = GetProperty(name);
                if (property == null) continue;
                result[name] = Convert.ToString(property.GetValue(settings, null),
                    CultureInfo.InvariantCulture) ?? string.Empty;
            }
            return result;
        }

        private static Dictionary<string, string> FilterSurface(IDictionary<string, string> values,
            string prefix, string controllerNameProperty)
        {
            return values == null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : values.Where(a => a.Key.StartsWith(prefix, StringComparison.Ordinal) ||
                        string.Equals(a.Key, controllerNameProperty, StringComparison.Ordinal))
                    .ToDictionary(a => a.Key, a => a.Value, StringComparer.Ordinal);
        }

        private static bool MatchesSurface(IDictionary<string, string> current,
            IDictionary<string, string> saved)
        {
            if (saved == null || saved.Count == 0) return false;
            return current.All(a => saved.ContainsKey(a.Key) &&
                string.Equals(saved[a.Key], a.Value, StringComparison.Ordinal));
        }

        private static void Copy(ControllerSessionManagerSettings settings, string sourcePrefix,
            string targetPrefix)
        {
            if (settings == null)
            {
                return;
            }
            foreach (var suffix in StyleSuffixes)
            {
                var source = GetProperty(sourcePrefix + suffix);
                var target = GetProperty(targetPrefix + suffix);
                if (source != null && target != null)
                {
                    target.SetValue(settings, source.GetValue(settings, null), null);
                }
            }
        }

        private static PropertyInfo GetProperty(string name)
        {
            return typeof(ControllerSessionManagerSettings).GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public);
        }
    }
}
