using System;
using System.IO;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>Named visual bundles for fullscreen + desktop notifications.</summary>
    public static class NotificationStylePresets
    {
        public const string Custom = "Custom";
        public const string Soft = "Soft";
        public const string Compact = "Compact";
        public const string Bold = "Bold";
        public const string Arcade = "Arcade";
        public const string Minimal = "Minimal";
        public const string Cinematic = "Cinematic";
        public static readonly string[] NamedPresets = { Soft, Compact, Bold, Arcade, Minimal, Cinematic };

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Soft;
            var trimmed = value.Trim();
            if (string.Equals(trimmed, Custom, StringComparison.OrdinalIgnoreCase)) return Custom;
            foreach (var name in NamedPresets)
            {
                if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase)) return name;
            }
            return Soft;
        }

        public static string LocKey(string preset)
        {
            switch (Normalize(preset))
            {
                case Compact: return "LOCCSM_StylePresetCompact";
                case Bold: return "LOCCSM_StylePresetBold";
                case Arcade: return "LOCCSM_StylePresetArcade";
                case Minimal: return "LOCCSM_StylePresetMinimal";
                case Cinematic: return "LOCCSM_StylePresetCinematic";
                case Custom: return "LOCCSM_StylePresetCustom";
                default: return "LOCCSM_StylePresetSoft";
            }
        }

        public static void Apply(ControllerSessionManagerSettings settings, string presetId)
        {
            if (settings == null) return;
            var preset = Normalize(presetId);
            if (preset == Custom) { settings.NotificationStylePreset = Custom; return; }
            DisableBackgroundImages(settings);
            switch (preset)
            {
                case Compact: ApplyCompact(settings); break;
                case Bold: ApplyBold(settings); break;
                case Arcade: ApplyArcade(settings); break;
                case Minimal: ApplyMinimal(settings); break;
                case Cinematic: ApplyCinematic(settings); break;
                default: ApplySoft(settings); break;
            }
            settings.NotificationStylePreset = preset;
        }

        private static void DisableBackgroundImages(ControllerSessionManagerSettings s)
        {
            s.NotificationUseBackgroundImage = false;
            s.NotificationBackgroundImagePath = string.Empty;
            s.DesktopNotificationUseBackgroundImage = false;
            s.DesktopNotificationBackgroundImagePath = string.Empty;
        }

        public static void ResetToDefault(ControllerSessionManagerSettings settings) { Apply(settings, Soft); }

        // Bright frosted card, subtle semantic tint and calm typography.
        private static void ApplySoft(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 520, 108, 5000, "TopRight", "#F2F4F7FB", "#FF172033", "#FF596579",
                "#FF087A4B", "#FF005FAF", "#FF945000", "#FFB42335", 19, 15, 32, "Left",
                18, 8, false, "Bottom", 0, 16, true, 28, true,
                420, 100, 4000, "BottomRight", "#F4F5F8FC", "#FF172033", "#FF596579",
                "#FF087A4B", "#FF005FAF", "#FF945000", "#FFB42335", 17, 14, 28, 14, 6, 14, 28);
            ApplyIdentity(s, NotificationFontCatalog.Inter, "SemiBold", "Left", "TintedBackground", "Fade",
                true, true, NotificationFontCatalog.Inter, "SemiBold", "Left", "TintedBackground", "Fade", true, true);
        }

        // One-line HUD with a hard left rail and quick slide motion.
        private static void ApplyCompact(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 340, 92, 3000, "TopRight", "#ED090C12", "#FFF1F5FA", "#FF96A2B2",
                "#FF27C07D", "#FF4A8FE0", "#FFE0A22E", "#FFE05252", 16, 12, 20, "Left",
                10, 3, true, "Left", 4, 2, false, 14, false,
                300, 88, 2600, "BottomRight", "#EF070A0F", "#FFF1F5FA", "#FF96A2B2",
                "#FF27C07D", "#FF4A8FE0", "#FFE0A22E", "#FFE05252", 15, 11, 18, 8, 2, 2, 12);
            ApplyIdentity(s, NotificationFontCatalog.Rajdhani, "SemiBold", "Left", "IconAndBorder", "Slide",
                true, false, NotificationFontCatalog.Rajdhani, "SemiBold", "Left", "IconAndBorder", "Slide", true, false);
        }

        // The event color becomes the card; large right icon and punchy scale motion.
        private static void ApplyBold(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 590, 120, 5600, "TopLeft", "#F01A1F2A", "#FFFFFFFF", "#FFF2F5FA",
                "#FF118C57", "#FF176FC1", "#FFB96800", "#FFC92D45", 25, 17, 46, "Right",
                24, 12, false, "Full", 0, 8, false, 34, true,
                490, 112, 4700, "BottomLeft", "#F01A1F2A", "#FFFFFFFF", "#FFF2F5FA",
                "#FF118C57", "#FF176FC1", "#FFB96800", "#FFC92D45", 22, 16, 40, 20, 10, 8, 30);
            ApplyIdentity(s, NotificationFontCatalog.Outfit, "Bold", "Left", "SolidBackground", "Scale",
                true, true, NotificationFontCatalog.Outfit, "Bold", "Left", "SolidBackground", "Scale", true, true);
        }

        // Centered neon cabinet card with a top icon and full outline.
        private static void ApplyArcade(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 470, 112, 4800, "BottomRight", "#F20A0828", "#FFF1F5FF", "#FFB7C5FF",
                "#FF00FFC6", "#FF00D4FF", "#FFFFE566", "#FFFF2E9D", 20, 14, 42, "Top",
                20, 10, true, "Full", 2, 22, true, 22, true,
                390, 103, 4000, "BottomLeft", "#F20A0828", "#FFF1F5FF", "#FFB7C5FF",
                "#FF00FFC6", "#FF00D4FF", "#FFFFE566", "#FFFF2E9D", 18, 13, 34, 16, 8, 18, 20);
            ApplyIdentity(s, NotificationFontCatalog.Orbitron, "SemiBold", "Center", "IconAndBorder", "Scale",
                true, true, NotificationFontCatalog.Orbitron, "SemiBold", "Center", "IconAndBorder", "Scale", true, true);
        }

        // Square, text-first card with an explicit connection badge and device name.
        private static void ApplyMinimal(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 350, 92, 2600, "TopLeft", "#C20A0D12", "#FFF2F4F7", "#FFA7AFBB",
                "#FF7FC79F", "#FF83A6C8", "#FFD0AD6A", "#FFC98181", 15, 11, 16, "Hidden",
                15, 2, false, "Bottom", 0, 0, true, 12, false,
                310, 88, 2300, "TopLeft", "#C20A0D12", "#FFF2F4F7", "#FFA7AFBB",
                "#FF7FC79F", "#FF83A6C8", "#FFD0AD6A", "#FFC98181", 14, 11, 16, 15, 2, 0, 10);
            ApplyIdentity(s, NotificationFontCatalog.Poppins, "Regular", "Left", "IconOnly", "Fade",
                true, true, NotificationFontCatalog.Poppins, "Regular", "Left", "IconOnly", "Fade", true, true);
        }

        // Dark cinematic artwork with restrained cyan and gold event accents.
        private static void ApplyCinematic(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 560, 108, 5200, "TopRight", "#E4070B0D", "#FFFFFFFF", "#FFD6E9E9",
                "#FF3DE0B5", "#FF57BFEF", "#FFFFC857", "#FFFF657A", 21, 15, 38, "Left",
                20, 9, true, "Bottom", 2, 18, true, 26, true,
                450, 100, 4200, "BottomRight", "#E4070B0D", "#FFFFFFFF", "#FFD6E9E9",
                "#FF3DE0B5", "#FF57BFEF", "#FFFFC857", "#FFFF657A", 18, 14, 32, 16, 7, 16, 24);
            ApplyIdentity(s, NotificationFontCatalog.Outfit, "SemiBold", "Left", "IconAndBorder", "Fade",
                true, true, NotificationFontCatalog.Outfit, "SemiBold", "Left", "IconAndBorder", "Fade", true, true);

            var imagePath = Path.Combine(
                Path.GetDirectoryName(typeof(NotificationStylePresets).Assembly.Location) ?? string.Empty,
                "Images", "NotifyBackgrounds", "bg1.jpg");
            s.NotificationUseBackgroundImage = true;
            s.NotificationBackgroundImagePath = imagePath;
            s.NotificationBackgroundImageStretch = "UniformToFill";
            s.NotificationBackgroundImageHorizontalAlignment = "Center";
            s.NotificationBackgroundImageVerticalAlignment = "Center";
            s.NotificationBackgroundImageOpacity = 82;
            s.NotificationBackgroundImageTintOpacity = 48;
            s.DesktopNotificationUseBackgroundImage = true;
            s.DesktopNotificationBackgroundImagePath = imagePath;
            s.DesktopNotificationBackgroundImageStretch = "UniformToFill";
            s.DesktopNotificationBackgroundImageHorizontalAlignment = "Center";
            s.DesktopNotificationBackgroundImageVerticalAlignment = "Center";
            s.DesktopNotificationBackgroundImageOpacity = 82;
            s.DesktopNotificationBackgroundImageTintOpacity = 48;
        }

        private static void ApplyIdentity(ControllerSessionManagerSettings s,
            string font, string weight, string alignment, string accentMode, string animation, bool showTitle, bool showName,
            string deskFont, string deskWeight, string deskAlignment, string deskAccentMode, string deskAnimation,
            bool deskShowTitle, bool deskShowName)
        {
            s.NotificationFontFamily = font; s.NotificationFontWeight = weight;
            s.NotificationTextAlignment = alignment; s.NotificationAccentMode = accentMode;
            s.NotificationAnimation = animation; s.NotificationShowTitle = showTitle;
            s.ShowControllerNameInNotifications = showName;
            s.DesktopNotificationFontFamily = deskFont; s.DesktopNotificationFontWeight = deskWeight;
            s.DesktopNotificationTextAlignment = deskAlignment; s.DesktopNotificationAccentMode = deskAccentMode;
            s.DesktopNotificationAnimation = deskAnimation; s.DesktopNotificationShowTitle = deskShowTitle;
            s.ShowControllerNameInDesktopNotifications = deskShowName;
        }

        private static void ApplyPair(ControllerSessionManagerSettings s,
            int width, int scale, int duration, string position, string bg, string text, string secondary,
            string connected, string disconnected, string warning, string lowBattery,
            int titleSize, int messageSize, int iconSize, string iconPos, int padding, int spacing,
            bool showBorder, string borderPos, int borderThickness, int corner, bool showBadge, int margin, bool showShadow,
            int deskWidth, int deskScale, int deskDuration, string deskPosition, string deskBg, string deskText,
            string deskSecondary, string deskConnected, string deskDisconnected, string deskWarning, string deskLowBattery,
            int deskTitle, int deskMessage, int deskIcon, int deskPadding, int deskSpacing, int deskCorner, int deskMargin)
        {
            s.NotificationWidth = width; s.NotificationScalePercent = scale; s.NotificationDurationMilliseconds = duration;
            s.NotificationPosition = position; s.NotificationBackgroundColor = bg; s.NotificationTextColor = text;
            s.NotificationSecondaryTextColor = secondary; s.NotificationConnectedColor = connected;
            s.NotificationDisconnectedColor = disconnected; s.NotificationWarningColor = warning;
            s.NotificationLowBatteryColor = lowBattery; s.NotificationTitleFontSize = titleSize;
            s.NotificationMessageFontSize = messageSize; s.NotificationIconSize = iconSize;
            s.NotificationIconPosition = iconPos; s.NotificationPadding = padding; s.NotificationElementSpacing = spacing;
            s.NotificationShowBorder = showBorder; s.NotificationBorderPosition = borderPos;
            s.NotificationBorderThickness = borderThickness; s.NotificationCornerRadius = corner;
            s.NotificationShowConnectionBadge = showBadge; s.NotificationScreenMargin = margin; s.NotificationShowShadow = showShadow;
            s.DesktopNotificationWidth = deskWidth; s.DesktopNotificationScalePercent = deskScale;
            s.DesktopNotificationDurationMilliseconds = deskDuration; s.DesktopNotificationPosition = deskPosition;
            s.DesktopNotificationBackgroundColor = deskBg; s.DesktopNotificationTextColor = deskText;
            s.DesktopNotificationSecondaryTextColor = deskSecondary; s.DesktopNotificationConnectedColor = deskConnected;
            s.DesktopNotificationDisconnectedColor = deskDisconnected; s.DesktopNotificationWarningColor = deskWarning;
            s.DesktopNotificationLowBatteryColor = deskLowBattery; s.DesktopNotificationTitleFontSize = deskTitle;
            s.DesktopNotificationMessageFontSize = deskMessage; s.DesktopNotificationIconSize = deskIcon;
            s.DesktopNotificationIconPosition = iconPos; s.DesktopNotificationPadding = deskPadding;
            s.DesktopNotificationElementSpacing = deskSpacing; s.DesktopNotificationShowBorder = showBorder;
            s.DesktopNotificationBorderPosition = borderPos; s.DesktopNotificationBorderThickness = borderThickness;
            s.DesktopNotificationCornerRadius = deskCorner; s.DesktopNotificationShowConnectionBadge = showBadge;
            s.DesktopNotificationScreenMargin = deskMargin; s.DesktopNotificationShowShadow = showShadow;
        }
    }
}
