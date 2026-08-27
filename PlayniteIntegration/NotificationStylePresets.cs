using System;
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
        public static readonly string[] PluginPresets = { Soft, Compact, Bold, Arcade, Minimal, Cinematic };
        public static string[] CreatorPresets
        {
            get { return CreatorThemeCatalog.GetPresetIds("notification"); }
        }
        public static string[] NamedPresets { get { return PluginPresets.Concat(CreatorPresets).ToArray(); } }

        public static bool IsCreatorPreset(string presetId)
        {
            var preset = Normalize(presetId);
            return CreatorThemeCatalog.Contains(preset, "notification");
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
            // A missing creator/imported preset must preserve its already-materialized
            // appearance instead of silently applying an unrelated plugin preset.
            return Custom;
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

        public static string CreatorName(string preset)
        {
            var catalogAuthor = CreatorThemeCatalog.GetAuthor(preset);
            if (!string.IsNullOrWhiteSpace(catalogAuthor)) return catalogAuthor;
            return string.Empty;
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

        // Dark glass toast with a state-colored left rail and Inter hierarchy.
        private static void ApplySoft(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 480, 104, 4600, "TopRight", "#F2161A22", "#FFF4F6F8", "#FF9AA7B6",
                "#FF2FA36A", "#FF5B93C9", "#FFD9A24A", "#FFD24A5A", 18, 14, 42, "Left",
                16, 8, true, "Full", 1, 14, true, 24, true,
                400, 98, 3800, "BottomRight", "#F4181C24", "#FFF4F6F8", "#FF9AA7B6",
                "#FF2FA36A", "#FF5B93C9", "#FFD9A24A", "#FFD24A5A", 16, 13, 36, 14, 6, 12, 22);
            ApplyIdentity(s, NotificationFontCatalog.Inter, "SemiBold", "Left", "TintedBackground", "Fade",
                true, true, NotificationFontCatalog.Inter, "SemiBold", "Left", "TintedBackground", "Fade", true, true);
            ApplyTypeHierarchy(s, NotificationFontCatalog.Inter, "SemiBold",
                NotificationFontCatalog.Inter, "Regular");
            s.NotificationUseGradient = true;
            s.NotificationGradientColor = "#F2222832";
            s.NotificationGradientAngle = 168;
            s.DesktopNotificationUseGradient = true;
            s.DesktopNotificationGradientColor = "#F2242A34";
            s.DesktopNotificationGradientAngle = 168;
            ApplyAccentRail(s, 3, 1, 1, 1, true);
        }

        // Broadcast ticker: Rajdhani, hard left rail, slide in, no extra chrome.
        private static void ApplyCompact(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 360, 92, 2800, "TopRight", "#F20A0C12", "#FFF2F5FA", "#FF8E9BB0",
                "#FF2BB673", "#FF4F8FDB", "#FFD9A24A", "#FFE05252", 16, 12, 20, "Left",
                16, 6, true, "Left", 0, 4, false, 16, false,
                320, 88, 2400, "BottomRight", "#F30B0D14", "#FFF2F5FA", "#FF8E9BB0",
                "#FF2BB673", "#FF4F8FDB", "#FFD9A24A", "#FFE05252", 15, 11, 18, 14, 5, 4, 14);
            ApplyIdentity(s, NotificationFontCatalog.Rajdhani, "SemiBold", "Left", "IconAndBorder", "Slide",
                true, false, NotificationFontCatalog.Rajdhani, "SemiBold", "Left", "IconAndBorder", "Slide", true, false);
            ApplyTypeHierarchy(s, NotificationFontCatalog.Rajdhani, "SemiBold",
                NotificationFontCatalog.Rajdhani, "Regular");
            ApplyAccentRail(s, 4, 0, 0, 0, true);
            s.NotificationIconSpacing = 14;
            s.DesktopNotificationIconSpacing = 12;
        }

        // Event-colored poster with a framed icon and Outfit titles.
        private static void ApplyBold(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 560, 114, 5200, "TopLeft", "#F2181C28", "#FFFFFFFF", "#FFD7DEE8",
                "#FF14865A", "#FF1B73C4", "#FFC07A12", "#FFC92D45", 24, 16, 44, "Right",
                22, 10, true, "Full", 1, 16, true, 28, true,
                470, 106, 4400, "BottomLeft", "#F21A1E2A", "#FFFFFFFF", "#FFD7DEE8",
                "#FF14865A", "#FF1B73C4", "#FFC07A12", "#FFC92D45", 21, 15, 38, 18, 8, 14, 24);
            ApplyIdentity(s, NotificationFontCatalog.Outfit, "Bold", "Left", "SolidBackground", "Scale",
                true, true, NotificationFontCatalog.Outfit, "Bold", "Left", "SolidBackground", "Scale", true, true);
            ApplyTypeHierarchy(s, NotificationFontCatalog.Outfit, "Bold",
                NotificationFontCatalog.Outfit, "Regular");
            s.NotificationShowIconContainer = true;
            s.NotificationIconContainerColor = "#33FFFFFF";
            s.NotificationIconContainerBorderColor = "#66FFFFFF";
            s.NotificationIconContainerCornerRadius = 12;
            s.NotificationIconContainerPadding = 10;
            s.DesktopNotificationShowIconContainer = true;
            s.DesktopNotificationIconContainerColor = "#33FFFFFF";
            s.DesktopNotificationIconContainerBorderColor = "#66FFFFFF";
            s.DesktopNotificationIconContainerCornerRadius = 10;
            s.DesktopNotificationIconContainerPadding = 8;
            s.NotificationShowBorderGlow = true;
            s.NotificationBorderGlowColor = "#804F8FDB";
            s.NotificationBorderGlowBlur = 16;
            s.NotificationBorderGlowOpacity = 20;
            s.DesktopNotificationShowBorderGlow = true;
            s.DesktopNotificationBorderGlowColor = "#804F8FDB";
            s.DesktopNotificationBorderGlowBlur = 14;
            s.DesktopNotificationBorderGlowOpacity = 18;
        }

        // One neon accent on indigo, uppercase title, cabinet outline.
        private static void ApplyArcade(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 460, 108, 4400, "BottomRight", "#F1180C10", "#FFFFF4EA", "#FFFFC48A",
                "#FFFF9A3C", "#FFFF5A4A", "#FFFFD36A", "#FFFF4FA3", 18, 13, 36, "Top",
                18, 8, true, "Full", 2, 18, true, 22, true,
                390, 100, 3800, "BottomLeft", "#F11A0E12", "#FFFFF4EA", "#FFFFC48A",
                "#FFFF9A3C", "#FFFF5A4A", "#FFFFD36A", "#FFFF4FA3", 16, 12, 30, 16, 7, 16, 18);
            ApplyIdentity(s, NotificationFontCatalog.Orbitron, "SemiBold", "Center", "IconAndBorder", "Scale",
                true, true, NotificationFontCatalog.Orbitron, "SemiBold", "Center", "IconAndBorder", "Scale", true, true);
            ApplyTypeHierarchy(s, NotificationFontCatalog.Orbitron, "SemiBold",
                NotificationFontCatalog.Exo2, "Regular");
            s.NotificationUppercaseTitle = true;
            s.DesktopNotificationUppercaseTitle = true;
            s.NotificationUseBorderGradient = true;
            s.NotificationBorderGradientStartColor = "#FFFF9A3C";
            s.NotificationBorderGradientEndColor = "#FFFF5A4A";
            s.NotificationBorderGradientAngle = 125;
            s.DesktopNotificationUseBorderGradient = true;
            s.DesktopNotificationBorderGradientStartColor = "#FFFF9A3C";
            s.DesktopNotificationBorderGradientEndColor = "#FFFF5A4A";
            s.DesktopNotificationBorderGradientAngle = 125;
            s.NotificationShowBorderGlow = true;
            s.NotificationBorderGlowColor = "#90FF9A3C";
            s.NotificationBorderGlowBlur = 18;
            s.NotificationBorderGlowOpacity = 26;
            s.DesktopNotificationShowBorderGlow = true;
            s.DesktopNotificationBorderGlowColor = "#90FF9A3C";
            s.DesktopNotificationBorderGlowBlur = 16;
            s.DesktopNotificationBorderGlowOpacity = 22;
            ApplyStateSurfaces(s, true, "#F1281810", 145,
                "#C03A2208", "#C03A100C", "#C03A2808", "#C03A0818",
                "#FFFF9A3C", "#FFFF5A4A", "#FFFFD36A", "#FFFF4FA3");
        }

        // Quiet type-first card. Almost no chrome, name only.
        private static void ApplyMinimal(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 340, 92, 2400, "TopLeft", "#B8101318", "#FFEEF1F5", "#FF9AA6B4",
                "#FF6FB392", "#FF7A97B8", "#FFC8A66A", "#FFC98181", 15, 12, 16, "Hidden",
                14, 4, false, "Full", 0, 4, false, 16, false,
                300, 88, 2200, "TopLeft", "#C012151A", "#FFEEF1F5", "#FF9AA6B4",
                "#FF6FB392", "#FF7A97B8", "#FFC8A66A", "#FFC98181", 14, 11, 16, 12, 3, 4, 14);
            ApplyIdentity(s, NotificationFontCatalog.Poppins, "Regular", "Left", "IconOnly", "Fade",
                true, true, NotificationFontCatalog.Poppins, "Regular", "Left", "IconOnly", "Fade", true, true);
            ApplyTypeHierarchy(s, NotificationFontCatalog.Poppins, "Regular",
                NotificationFontCatalog.Poppins, "Regular");
        }

        // Letterbox toast: overlay scene gradient, no artwork.
        private static void ApplyCinematic(ControllerSessionManagerSettings s)
        {
            ApplyPair(s, 540, 104, 5000, "TopRight", "#FF050608", "#FFF7FBFC", "#FFB7CBD0",
                "#FF3DE0B5", "#FF57C7E8", "#FFFFC45C", "#FFFF657A", 20, 14, 34, "Left",
                18, 8, true, "Bottom", 2, 4, true, 24, true,
                440, 98, 4000, "BottomRight", "#FF050608", "#FFF7FBFC", "#FFB7CBD0",
                "#FF3DE0B5", "#FF57C7E8", "#FFFFC45C", "#FFFF657A", 18, 13, 28, 16, 7, 4, 22);
            ApplyIdentity(s, NotificationFontCatalog.Outfit, "SemiBold", "Center", "IconAndBorder", "Fade",
                true, true, NotificationFontCatalog.Outfit, "SemiBold", "Center", "IconAndBorder", "Fade", true, true);
            ApplyTypeHierarchy(s, NotificationFontCatalog.Outfit, "SemiBold",
                NotificationFontCatalog.Outfit, "Regular");
            ApplyAccentRail(s, 0, 0, 0, 2, false);
            s.NotificationUseGradient = true;
            s.NotificationGradientColor = "#FF101418";
            s.NotificationGradientAngle = 45;
            s.DesktopNotificationUseGradient = true;
            s.DesktopNotificationGradientColor = "#FF101418";
            s.DesktopNotificationGradientAngle = 45;
            s.NotificationUseBorderGradient = true;
            s.NotificationBorderGradientStartColor = "#FF57C7E8";
            s.NotificationBorderGradientEndColor = "#FFFFC45C";
            s.NotificationBorderGradientAngle = 90;
            s.DesktopNotificationUseBorderGradient = true;
            s.DesktopNotificationBorderGradientStartColor = "#FF57C7E8";
            s.DesktopNotificationBorderGradientEndColor = "#FFFFC45C";
            s.DesktopNotificationBorderGradientAngle = 90;
            s.NotificationShowBorderGlow = true;
            s.NotificationBorderGlowColor = "#6657C7E8";
            s.NotificationBorderGlowBlur = 14;
            s.NotificationBorderGlowOpacity = 18;
            s.DesktopNotificationShowBorderGlow = true;
            s.DesktopNotificationBorderGlowColor = "#6657C7E8";
            s.DesktopNotificationBorderGlowBlur = 12;
            s.DesktopNotificationBorderGlowOpacity = 16;
            ApplyStateSurfaces(s, true, "#FF101418", 45,
                "#FF0A1210", "#FF0A1018", "#FF161208", "#FF16080C",
                "#FF3DE0B5", "#FF57C7E8", "#FFFFC45C", "#FFFF657A");
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
            s.NotificationConnectedBorderColor = connected;
            s.NotificationDisconnectedBorderColor = disconnected;
            s.NotificationWarningBorderColor = warning;
            s.NotificationLowBatteryBorderColor = lowBattery;
            s.DesktopNotificationTextOrder = "TitleFirst";
            s.DesktopNotificationUseIndependentBorders = false;
            s.DesktopNotificationUseStateBackgroundColors = false;
            s.DesktopNotificationConnectedBackgroundColor = deskBg;
            s.DesktopNotificationDisconnectedBackgroundColor = deskBg;
            s.DesktopNotificationWarningBackgroundColor = deskBg;
            s.DesktopNotificationLowBatteryBackgroundColor = deskBg;
            s.DesktopNotificationConnectedBorderColor = deskConnected;
            s.DesktopNotificationDisconnectedBorderColor = deskDisconnected;
            s.DesktopNotificationWarningBorderColor = deskWarning;
            s.DesktopNotificationLowBatteryBorderColor = deskLowBattery;
        }

        private static void ApplyTypeHierarchy(
            ControllerSessionManagerSettings s,
            string titleFamily, string titleWeight,
            string messageFamily, string messageWeight)
        {
            s.NotificationTitleFontFamily = titleFamily;
            s.NotificationTitleFontWeight = titleWeight;
            s.NotificationMessageFontFamily = messageFamily;
            s.NotificationMessageFontWeight = messageWeight;
            s.DesktopNotificationTitleFontFamily = titleFamily;
            s.DesktopNotificationTitleFontWeight = titleWeight;
            s.DesktopNotificationMessageFontFamily = messageFamily;
            s.DesktopNotificationMessageFontWeight = messageWeight;
        }

        private static void ApplyAccentRail(
            ControllerSessionManagerSettings s, int left, int top, int right, int bottom, bool stateColors)
        {
            s.NotificationShowBorder = true;
            s.DesktopNotificationShowBorder = true;
            s.NotificationUseIndependentBorders = true;
            s.DesktopNotificationUseIndependentBorders = true;
            s.NotificationUseStateBorderColors = stateColors;
            s.DesktopNotificationUseStateBorderColors = stateColors;
            s.NotificationBorderLeftThickness = left;
            s.NotificationBorderTopThickness = top;
            s.NotificationBorderRightThickness = right;
            s.NotificationBorderBottomThickness = bottom;
            s.DesktopNotificationBorderLeftThickness = left;
            s.DesktopNotificationBorderTopThickness = top;
            s.DesktopNotificationBorderRightThickness = right;
            s.DesktopNotificationBorderBottomThickness = bottom;
        }

        private static void ApplyStateSurfaces(
            ControllerSessionManagerSettings s, bool useGradient, string gradient, int angle,
            string connectedBg, string disconnectedBg, string warningBg, string lowBatteryBg,
            string connectedBorder, string disconnectedBorder, string warningBorder, string lowBatteryBorder)
        {
            s.NotificationUseGradient = useGradient;
            s.DesktopNotificationUseGradient = useGradient;
            s.NotificationGradientColor = gradient;
            s.DesktopNotificationGradientColor = gradient;
            s.NotificationGradientAngle = angle;
            s.DesktopNotificationGradientAngle = angle;
            s.NotificationUseStateBackgroundColors = true;
            s.DesktopNotificationUseStateBackgroundColors = true;
            s.NotificationConnectedBackgroundColor = connectedBg;
            s.NotificationDisconnectedBackgroundColor = disconnectedBg;
            s.NotificationWarningBackgroundColor = warningBg;
            s.NotificationLowBatteryBackgroundColor = lowBatteryBg;
            s.DesktopNotificationConnectedBackgroundColor = connectedBg;
            s.DesktopNotificationDisconnectedBackgroundColor = disconnectedBg;
            s.DesktopNotificationWarningBackgroundColor = warningBg;
            s.DesktopNotificationLowBatteryBackgroundColor = lowBatteryBg;
            s.NotificationUseStateBorderColors = true;
            s.DesktopNotificationUseStateBorderColors = true;
            s.NotificationConnectedBorderColor = connectedBorder;
            s.NotificationDisconnectedBorderColor = disconnectedBorder;
            s.NotificationWarningBorderColor = warningBorder;
            s.NotificationLowBatteryBorderColor = lowBatteryBorder;
            s.DesktopNotificationConnectedBorderColor = connectedBorder;
            s.DesktopNotificationDisconnectedBorderColor = disconnectedBorder;
            s.DesktopNotificationWarningBorderColor = warningBorder;
            s.DesktopNotificationLowBatteryBorderColor = lowBatteryBorder;
        }

        private static string WithAlpha(string color, string alpha)
        {
            return !string.IsNullOrWhiteSpace(color) && color.Length == 9 && color[0] == '#'
                ? "#" + alpha + color.Substring(3)
                : color;
        }
    }
}
