using System;
using System.IO;
using System.Linq;

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
        public const string Aniki = "Aniki";
        public const string Helium = "Helium";
        public static readonly string[] PluginPresets = { Soft, Compact, Bold, Arcade, Minimal, Cinematic };
        public static string[] CreatorPresets
        {
            get { return new[] { Aniki, Helium }.Concat(CreatorThemeCatalog.GetPresetIds("notification"))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); }
        }
        public static string[] NamedPresets { get { return PluginPresets.Concat(CreatorPresets).ToArray(); } }

        public static bool IsCreatorPreset(string presetId)
        {
            var preset = Normalize(presetId);
            return string.Equals(preset, Aniki, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(preset, Helium, StringComparison.OrdinalIgnoreCase) ||
                CreatorThemeCatalog.Contains(preset, "notification");
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Soft;
            var trimmed = value.Trim();
            if (string.Equals(trimmed, Custom, StringComparison.OrdinalIgnoreCase)) return Custom;
            if (ImportedVisualProfileCatalog.Contains(trimmed)) return trimmed;
            // Presets removed before their public release become Custom so their complete
            // appearance survives in local development settings and exported profiles.
            if (string.Equals(trimmed, "Studio", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "NeonPulse", StringComparison.OrdinalIgnoreCase)) return Custom;
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
                case Aniki: return "LOCCSM_StylePresetAniki";
                case Helium: return "LOCCSM_StylePresetHelium";
                case Custom: return "LOCCSM_StylePresetCustom";
                default: return "LOCCSM_StylePresetSoft";
            }
        }

        public static string CreatorName(string preset)
        {
            var catalogAuthor = CreatorThemeCatalog.GetAuthor(preset);
            if (!string.IsNullOrWhiteSpace(catalogAuthor)) return catalogAuthor;
            switch (Normalize(preset))
            {
                case Aniki: return "Mike Aniki";
                case Helium: return "darklinkpower";
                default: return string.Empty;
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
                case Aniki:
                    if (CreatorThemeCatalog.Contains(Aniki, "notification"))
                    { ApplySoft(settings); CreatorThemeCatalog.TryApply(settings, Aniki, "notification"); }
                    else ApplyAniki(settings);
                    break;
                case Helium:
                    if (CreatorThemeCatalog.Contains(Helium, "notification"))
                    { ApplySoft(settings); CreatorThemeCatalog.TryApply(settings, Helium, "notification"); }
                    else ApplyHelium(settings);
                    break;
                default:
                    ApplySoft(settings);
                    CreatorThemeCatalog.TryApply(settings, preset, "notification");
                    break;
            }
            settings.NotificationStylePreset = preset;
        }

        public static void ApplyFullscreen(ControllerSessionManagerSettings settings, string presetId)
        {
            if (settings == null) return;
            var desktop = NotificationStyleState.CaptureDesktop(settings);
            var desktopPreset = settings.DesktopNotificationStylePreset;
            Apply(settings, presetId);
            NotificationStyleState.ApplyDesktop(settings, desktop);
            settings.DesktopNotificationStylePreset = desktopPreset;
            settings.NotificationStylePreset = Normalize(presetId);
        }

        public static void ApplyDesktop(ControllerSessionManagerSettings settings, string presetId)
        {
            if (settings == null) return;
            var fullscreen = NotificationStyleState.CaptureFullscreen(settings);
            var fullscreenPreset = settings.NotificationStylePreset;
            Apply(settings, presetId);
            NotificationStyleState.ApplyFullscreen(settings, fullscreen);
            settings.NotificationStylePreset = fullscreenPreset;
            settings.DesktopNotificationStylePreset = Normalize(presetId);
        }

        private static void DisableBackgroundImages(ControllerSessionManagerSettings s)
        {
            s.NotificationUseBackgroundImage = false;
            s.NotificationBackgroundImagePath = string.Empty;
            s.DesktopNotificationUseBackgroundImage = false;
            s.DesktopNotificationBackgroundImagePath = string.Empty;
            s.NotificationUseGradient = false;
            s.NotificationGradientColor = s.NotificationBackgroundColor;
            s.NotificationGradientAngle = 0;
            s.NotificationUppercaseTitle = false;
            s.DesktopNotificationUseGradient = false;
            s.DesktopNotificationGradientColor = s.DesktopNotificationBackgroundColor;
            s.DesktopNotificationGradientAngle = 0;
            s.DesktopNotificationUppercaseTitle = false;
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

        // Creator design by Mike Aniki, adapted from Aniki ReMake's dark-gold glass language.
        private static void ApplyAniki(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 560, 108, 5200, "TopRight", "#FA0C1118", "#FFF5F2EC", "#E6D8D4CC",
                "#FFD6B16F", "#FFE8C98F", "#FFFFC857", "#FFFF4D5A", 22, 16, 42, "Left",
                22, 9, true, "Full", 2, 15, true, 34, true,
                440, 100, 4200, "BottomRight", "#FA0C1118", "#FFF5F2EC", "#E6D8D4CC",
                "#FFD6B16F", "#FFE8C98F", "#FFFFC857", "#FFFF4D5A", 18, 14, 32, 16, 7, 8, 24);
            ApplyIdentity(s, NotificationFontCatalog.Exo2, "SemiBold", "Left", "IconAndBorder", "Slide",
                true, true, NotificationFontCatalog.Exo2, "SemiBold", "Left", "IconAndBorder", "Slide", true, true);
            s.NotificationMessageFontFamily = NotificationFontCatalog.Exo2;
            s.NotificationMessageFontWeight = "Regular";
            s.NotificationMessageMaxLines = 3;
            s.NotificationUseGradient = true;
            s.NotificationGradientColor = "#FF151D26";
            s.NotificationGradientAngle = 135;
            s.NotificationUseBorderGradient = true;
            s.NotificationUseStateBorderColors = true;
            s.NotificationConnectedBorderColor = "#FFD6B16F";
            s.NotificationDisconnectedBorderColor = "#FFE8C98F";
            s.NotificationWarningBorderColor = "#FFFFC857";
            s.NotificationLowBatteryBorderColor = "#FFFF4D5A";
            s.NotificationBorderGradientStartColor = "#B3FFFFFF";
            s.NotificationBorderGradientEndColor = "#FFD6B16F";
            s.NotificationBorderGradientAngle = 45;
            s.NotificationShowBorderGlow = true;
            s.NotificationBorderGlowColor = "#FFE9C48A";
            s.NotificationBorderGlowBlur = 26;
            s.NotificationBorderGlowOpacity = 90;
            s.NotificationShowIconContainer = true;
            s.NotificationIconContainerColor = "#33111820";
            s.NotificationIconContainerBorderColor = "#55D6B16F";
            s.NotificationIconContainerBorderThickness = 1;
            s.NotificationIconContainerCornerRadius = 10;
            s.NotificationIconContainerPadding = 10;
            s.DesktopNotificationMessageFontFamily = NotificationFontCatalog.Exo2;
            s.DesktopNotificationMessageFontWeight = "Regular";
            s.DesktopNotificationMessageMaxLines = 3;
            s.DesktopNotificationUseGradient = true;
            s.DesktopNotificationGradientColor = "#FF151D26";
            s.DesktopNotificationGradientAngle = 135;
            s.DesktopNotificationUseBorderGradient = true;
            s.DesktopNotificationUseStateBorderColors = true;
            s.DesktopNotificationConnectedBorderColor = "#FFD6B16F";
            s.DesktopNotificationDisconnectedBorderColor = "#FFE8C98F";
            s.DesktopNotificationWarningBorderColor = "#FFFFC857";
            s.DesktopNotificationLowBatteryBorderColor = "#FFFF4D5A";
            s.DesktopNotificationBorderGradientStartColor = "#B3FFFFFF";
            s.DesktopNotificationBorderGradientEndColor = "#FFD6B16F";
            s.DesktopNotificationBorderGradientAngle = 45;
            s.DesktopNotificationShowBorderGlow = true;
            s.DesktopNotificationBorderGlowColor = "#FFE9C48A";
            s.DesktopNotificationBorderGlowBlur = 24;
            s.DesktopNotificationBorderGlowOpacity = 85;
            s.DesktopNotificationShowIconContainer = true;
            s.DesktopNotificationIconContainerColor = "#33111820";
            s.DesktopNotificationIconContainerBorderColor = "#55D6B16F";
            s.DesktopNotificationIconContainerBorderThickness = 1;
            s.DesktopNotificationIconContainerCornerRadius = 8;
            s.DesktopNotificationIconContainerPadding = 8;
            s.NotificationUseStateBackgroundColors = true;
            s.NotificationConnectedBackgroundColor = "#FA0C1515";
            s.NotificationDisconnectedBackgroundColor = "#FA0C1118";
            s.NotificationWarningBackgroundColor = "#FA18140C";
            s.NotificationLowBatteryBackgroundColor = "#FA1B0D12";
            s.DesktopNotificationUseStateBackgroundColors = true;
            s.DesktopNotificationConnectedBackgroundColor = "#FA0C1515";
            s.DesktopNotificationDisconnectedBackgroundColor = "#FA0C1118";
            s.DesktopNotificationWarningBackgroundColor = "#FA18140C";
            s.DesktopNotificationLowBatteryBackgroundColor = "#FA1B0D12";
        }

        // Creator design by darklinkpower, adapted from Helium's graphite and Steam-blue UI.
        private static void ApplyHelium(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 500, 100, 4800, "TopRight", "#F2399AEC", "#FFFFFFFF", "#DFFFFFFF",
                "#FF7CC53F", "#FF1A9FFF", "#FFFFA500", "#FFFF6B6B", 20, 14, 34, "Left",
                18, 8, true, "Full", 1, 3, true, 28, true,
                400, 100, 3800, "BottomRight", "#F2399AEC", "#FFFFFFFF", "#DFFFFFFF",
                "#FF7CC53F", "#FF1A9FFF", "#FFFFA500", "#FFFF6B6B", 16, 13, 28, 14, 6, 3, 20);
            ApplyIdentity(s, NotificationFontCatalog.Trebuchet, "SemiBold", "Left", "TintedBackground", "Fade",
                true, true, NotificationFontCatalog.Trebuchet, "SemiBold", "Left", "TintedBackground", "Fade", true, true);
            s.NotificationUseGradient = true;
            s.NotificationGradientColor = "#F2235ECF";
            s.NotificationGradientAngle = 0;
            s.NotificationShowIconContainer = true;
            s.NotificationIconContainerColor = "#553E4047";
            s.NotificationIconContainerBorderColor = "#663E6184";
            s.NotificationIconContainerBorderThickness = 1;
            s.NotificationIconContainerCornerRadius = 3;
            s.NotificationIconContainerPadding = 8;
            s.DesktopNotificationUseGradient = true;
            s.DesktopNotificationGradientColor = "#F2235ECF";
            s.DesktopNotificationGradientAngle = 0;
            s.DesktopNotificationShowIconContainer = true;
            s.DesktopNotificationIconContainerColor = "#553E4047";
            s.DesktopNotificationIconContainerBorderColor = "#663E6184";
            s.DesktopNotificationIconContainerBorderThickness = 1;
            s.DesktopNotificationIconContainerCornerRadius = 3;
            s.DesktopNotificationIconContainerPadding = 7;
            s.NotificationUseIndependentBorders = true;
            s.NotificationBorderLeftThickness = 4; s.NotificationBorderTopThickness = 0;
            s.NotificationBorderRightThickness = 0; s.NotificationBorderBottomThickness = 0;
            s.DesktopNotificationUseIndependentBorders = true;
            s.DesktopNotificationBorderLeftThickness = 4; s.DesktopNotificationBorderTopThickness = 0;
            s.DesktopNotificationBorderRightThickness = 0; s.DesktopNotificationBorderBottomThickness = 0;
        }

        private static void ApplyIdentity(ControllerSessionManagerSettings s,
            string font, string weight, string alignment, string accentMode, string animation, bool showTitle, bool showName,
            string deskFont, string deskWeight, string deskAlignment, string deskAccentMode, string deskAnimation,
            bool deskShowTitle, bool deskShowName)
        {
            s.NotificationFontFamily = font; s.NotificationFontWeight = weight;
            s.NotificationTitleFontFamily = font; s.NotificationTitleFontWeight = weight;
            s.NotificationMessageFontFamily = font; s.NotificationMessageFontWeight = "Regular";
            s.NotificationMessageMaxLines = 2; s.NotificationBadgePosition = "TopRight";
            s.NotificationTextAlignment = alignment; s.NotificationAccentMode = accentMode;
            s.NotificationAnimation = animation; s.NotificationShowTitle = showTitle;
            s.ShowControllerNameInNotifications = showName;
            s.DesktopNotificationFontFamily = deskFont; s.DesktopNotificationFontWeight = deskWeight;
            s.DesktopNotificationTitleFontFamily = deskFont; s.DesktopNotificationTitleFontWeight = deskWeight;
            s.DesktopNotificationMessageFontFamily = deskFont; s.DesktopNotificationMessageFontWeight = "Regular";
            s.DesktopNotificationMessageMaxLines = 2; s.DesktopNotificationBadgePosition = "TopRight";
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
            s.NotificationUseGradient = false; s.NotificationGradientColor = bg;
            s.NotificationGradientAngle = 0; s.NotificationUppercaseTitle = false;
            s.NotificationSecondaryTextColor = secondary; s.NotificationConnectedColor = connected;
            s.NotificationDisconnectedColor = disconnected; s.NotificationWarningColor = warning;
            s.NotificationLowBatteryColor = lowBattery; s.NotificationTitleFontSize = titleSize;
            s.NotificationMessageFontSize = messageSize; s.NotificationIconSize = iconSize;
            s.NotificationIconPosition = iconPos; s.NotificationPadding = padding; s.NotificationElementSpacing = spacing;
            s.NotificationShowIconContainer = false;
            s.NotificationIconContainerColor = WithAlpha(connected, "28");
            s.NotificationIconContainerBorderColor = WithAlpha(connected, "70");
            s.NotificationIconContainerBorderThickness = 1;
            s.NotificationIconContainerCornerRadius = Math.Max(4, corner / 2);
            s.NotificationIconContainerPadding = 8;
            s.NotificationIconSpacing = Math.Max(0, Math.Max(spacing,
                (int)Math.Round(padding * 0.75)));
            s.NotificationShowBorder = showBorder; s.NotificationBorderPosition = borderPos;
            s.NotificationBorderThickness = borderThickness; s.NotificationCornerRadius = corner;
            s.NotificationUseBorderGradient = false; s.NotificationUseStateBorderColors = false;
            s.NotificationShowBorderGlow = false;
            s.NotificationShowConnectionBadge = showBadge; s.NotificationScreenMargin = margin; s.NotificationShowShadow = showShadow;
            s.DesktopNotificationWidth = deskWidth; s.DesktopNotificationScalePercent = deskScale;
            s.DesktopNotificationDurationMilliseconds = deskDuration; s.DesktopNotificationPosition = deskPosition;
            s.DesktopNotificationBackgroundColor = deskBg; s.DesktopNotificationTextColor = deskText;
            s.DesktopNotificationUseGradient = false; s.DesktopNotificationGradientColor = deskBg;
            s.DesktopNotificationGradientAngle = 0; s.DesktopNotificationUppercaseTitle = false;
            s.DesktopNotificationSecondaryTextColor = deskSecondary; s.DesktopNotificationConnectedColor = deskConnected;
            s.DesktopNotificationDisconnectedColor = deskDisconnected; s.DesktopNotificationWarningColor = deskWarning;
            s.DesktopNotificationLowBatteryColor = deskLowBattery; s.DesktopNotificationTitleFontSize = deskTitle;
            s.DesktopNotificationMessageFontSize = deskMessage; s.DesktopNotificationIconSize = deskIcon;
            s.DesktopNotificationIconPosition = iconPos; s.DesktopNotificationPadding = deskPadding;
            s.DesktopNotificationShowIconContainer = false;
            s.DesktopNotificationIconContainerColor = WithAlpha(deskConnected, "28");
            s.DesktopNotificationIconContainerBorderColor = WithAlpha(deskConnected, "70");
            s.DesktopNotificationIconContainerBorderThickness = 1;
            s.DesktopNotificationIconContainerCornerRadius = Math.Max(4, deskCorner / 2);
            s.DesktopNotificationIconContainerPadding = 7;
            s.DesktopNotificationElementSpacing = deskSpacing; s.DesktopNotificationShowBorder = showBorder;
            s.DesktopNotificationUseBorderGradient = false; s.DesktopNotificationUseStateBorderColors = false;
            s.DesktopNotificationShowBorderGlow = false;
            s.DesktopNotificationIconSpacing = Math.Max(0, Math.Max(deskSpacing,
                (int)Math.Round(deskPadding * 0.75)));
            s.DesktopNotificationBorderPosition = borderPos; s.DesktopNotificationBorderThickness = borderThickness;
            s.DesktopNotificationCornerRadius = deskCorner; s.DesktopNotificationShowConnectionBadge = showBadge;
            s.DesktopNotificationScreenMargin = deskMargin; s.DesktopNotificationShowShadow = showShadow;
            s.NotificationTextOrder = "TitleFirst";
            s.NotificationUseIndependentBorders = false;
            s.NotificationUseStateBackgroundColors = false;
            s.NotificationConnectedBackgroundColor = bg; s.NotificationDisconnectedBackgroundColor = bg;
            s.NotificationWarningBackgroundColor = bg; s.NotificationLowBatteryBackgroundColor = bg;
            s.DesktopNotificationTextOrder = "TitleFirst";
            s.DesktopNotificationUseIndependentBorders = false;
            s.DesktopNotificationUseStateBackgroundColors = false;
            s.DesktopNotificationConnectedBackgroundColor = deskBg;
            s.DesktopNotificationDisconnectedBackgroundColor = deskBg;
            s.DesktopNotificationWarningBackgroundColor = deskBg;
            s.DesktopNotificationLowBatteryBackgroundColor = deskBg;
        }

        private static string WithAlpha(string color, string alpha)
        {
            return !string.IsNullOrWhiteSpace(color) && color.Length == 9 && color[0] == '#'
                ? "#" + alpha + color.Substring(3)
                : color;
        }
    }
}
