using System;
using System.Linq;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>
    /// Named visual bundles for the in-game disconnect overlay.
    /// </summary>
    public static class OverlayStylePresets
    {
        public const string Custom = "Custom";
        public const string Soft = "Soft";
        public const string Compact = "Compact";
        public const string Bold = "Bold";
        public const string Arcade = "Arcade";
        public const string Minimal = "Minimal";
        public const string Cinematic = "Cinematic";
        public static readonly string[] PluginPresets =
            { Soft, Compact, Bold, Arcade, Minimal, Cinematic };
        public static string[] CreatorPresets
        {
            get { return CreatorThemeCatalog.GetPresetIds("overlay"); }
        }
        public static string[] NamedPresets { get { return PluginPresets.Concat(CreatorPresets).ToArray(); } }

        public static bool IsCreatorPreset(string presetId)
        {
            var preset = Normalize(presetId);
            return CreatorThemeCatalog.Contains(preset, "overlay");
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Soft;
            }

            var trimmed = value.Trim();
            if (string.Equals(trimmed, Custom, StringComparison.OrdinalIgnoreCase))
            {
                return Custom;
            }
            if (ImportedVisualProfileCatalog.Contains(trimmed)) return trimmed;
            if (string.Equals(trimmed, "Studio", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "NeonPulse", StringComparison.OrdinalIgnoreCase))
            {
                return Custom;
            }

            foreach (var name in NamedPresets)
            {
                if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }

            // A missing creator/imported preset must preserve its already-materialized
            // appearance instead of silently applying an unrelated plugin preset.
            return Custom;
        }

        public static string LocKey(string preset)
        {
            return NotificationStylePresets.LocKey(preset);
        }

        public static void Apply(ControllerSessionManagerSettings settings, string presetId)
        {
            if (settings == null)
            {
                return;
            }

            var preset = Normalize(presetId);
            if (preset == Custom)
            {
                settings.OverlayStylePreset = Custom;
                return;
            }

            switch (preset)
            {
                case Compact:
                    ApplyCompact(settings);
                    break;
                case Bold:
                    ApplyBold(settings);
                    break;
                case Arcade:
                    ApplyArcade(settings);
                    break;
                case Minimal:
                    ApplyMinimal(settings);
                    break;
                case Cinematic:
                    ApplyCinematic(settings);
                    break;
                default:
                    ApplySoft(settings);
                    CreatorThemeCatalog.TryApply(settings, preset, "overlay");
                    break;
            }

            settings.OverlayStylePreset = preset;
        }

        // Glass dashboard: split HUD, left accent rail, quiet ambient light.
        private static void ApplySoft(ControllerSessionManagerSettings s)
        {
            var accent = "#FF7AA4C8";
            ApplyValues(s, 100, "#A8090B10", "#F2141820", accent, "#FFF3F5F8", "#FFE2B45C",
                28, 13, 12, 13, 56, 12, true, true, true, "Top", 28, 12, true, 1, 16);
            ApplyTypography(s, NotificationFontCatalog.Inter, "SemiBold",
                NotificationFontCatalog.Inter, "Regular",
                NotificationFontCatalog.Inter, "Regular");
            ApplyEnhancements(s, true, true, true, true, true,
                "Center", "FadeScale", "Full", 760, true);
            s.OverlayInstructionColor = accent;
            s.OverlayUseGradient = true;
            s.OverlayGradientColor = "#F21C222C";
            s.OverlayGradientAngle = 168;
            s.OverlayLayoutMode = "Split";
            s.OverlayShowSplitDivider = true;
            s.OverlaySplitDividerColor = WithAlpha(accent, "38");
            s.OverlaySplitDividerThickness = 1;
            s.OverlayShowDisconnectTimer = true;
            s.OverlayShowControllerContainer = true;
            s.OverlayControllerContainerColor = WithAlpha(accent, "18");
            s.OverlayControllerContainerBorderColor = WithAlpha(accent, "40");
            s.OverlayControllerContainerPadding = 16;
            s.OverlayContentAlignment = "Left";
            s.OverlayScreenMargin = 40;
            s.OverlayBlockOrder = "Title,Controller,Metadata,Timer,Instruction,Status";
            ApplyAccentRail(s, 3, 1, 1, 1);
            ApplyScene(s, true, "#FF07090E", 162, true, false,
                "#185BA3E8", 18, 22, 70,
                "#14C9A45A", 82, 18, 64,
                "#10228C78", 78, 86, 72);
        }

        // Broadcast lower-third: tight type, hard left rail, no extra chrome.
        private static void ApplyCompact(ControllerSessionManagerSettings s)
        {
            var accent = "#FF4F8FDB";
            ApplyValues(s, 90, "#66000000", "#F20A0C12", accent, "#FFF1F4F8", "#FFD9A84A",
                20, 14, 13, 12, 28, 13, true, true, true, "Left", 14, 6, true, 0, 4);
            ApplyTypography(s, NotificationFontCatalog.Rajdhani, "SemiBold",
                NotificationFontCatalog.Rajdhani, "Regular",
                NotificationFontCatalog.Rajdhani, "Regular");
            ApplyEnhancements(s, true, true, true, true, false,
                "Center", "Slide", "Left", 520, false);
            s.OverlayInstructionColor = "#FF8E9BB0";
            s.OverlayShowDisconnectTimer = true;
            s.OverlayContentAlignment = "Center";
            s.OverlayScreenMargin = 22;
            s.OverlayBlockOrder = "Title,Controller,Timer,Metadata,Instruction,Status";
            ApplyAccentRail(s, 4, 0, 0, 0);
        }

        // Centered pause card: stacked hierarchy, no leftover hero offset.
        private static void ApplyBold(ControllerSessionManagerSettings s)
        {
            var accent = "#FF5B9FE8";
            ApplyValues(s, 100, "#B805070C", "#F6181C26", accent, "#FFFFFFFF", "#FFFFB45A",
                26, 16, 14, 13, 52, 13, true, true, true, "Top", 28, 14, true, 1, 14);
            ApplyTypography(s, NotificationFontCatalog.Outfit, "SemiBold",
                NotificationFontCatalog.Outfit, "Regular",
                NotificationFontCatalog.Outfit, "Regular");
            ApplyEnhancements(s, true, true, true, true, true,
                "Center", "FadeScale", "Full", 560, true);
            s.OverlayInstructionColor = "#FFC5D0DC";
            s.OverlayUseGradient = true;
            s.OverlayGradientColor = "#F6242A38";
            s.OverlayGradientAngle = 175;
            s.OverlayLayoutMode = "Standard";
            s.OverlayShowDisconnectTimer = true;
            s.OverlayShowControllerContainer = true;
            s.OverlayControllerContainerColor = WithAlpha(accent, "18");
            s.OverlayControllerContainerBorderColor = WithAlpha(accent, "40");
            s.OverlayControllerContainerPadding = 12;
            s.OverlayContentAlignment = "Center";
            s.OverlayScreenMargin = 40;
            s.OverlayBlockOrder = "Title,Controller,Metadata,Timer,Instruction,Status";
            s.OverlayShowBorderGlow = true;
            s.OverlayBorderGlowColor = WithAlpha(accent, "70");
            s.OverlayBorderGlowBlur = 16;
            s.OverlayBorderGlowOpacity = 18;
            ApplyAccentRail(s, 1, 1, 1, 1);
        }

        // Warm cabinet HUD: amber/coral instead of NarianUX cyan/violet.
        private static void ApplyArcade(ControllerSessionManagerSettings s)
        {
            var accent = "#FFFF9A3C";
            ApplyValues(s, 106, "#C012080A", "#F1180C10", accent, "#FFFFF4EA", "#FFFFD36A",
                30, 18, 14, 13, 64, 14, true, true, true, "Top", 26, 16, true, 2, 20);
            ApplyTypography(s, NotificationFontCatalog.Orbitron, "SemiBold",
                NotificationFontCatalog.Exo2, "Regular",
                NotificationFontCatalog.Exo2, "SemiBold");
            ApplyEnhancements(s, true, true, true, true, true,
                "Center", "FadeScale", "Full", 640, true);
            s.OverlayInstructionColor = "#FFFFC48A";
            s.OverlayUppercaseTitle = true;
            s.OverlayLayoutMode = "Alert";
            s.OverlayShowIncidentBadge = true;
            s.OverlayShowDisconnectTimer = true;
            s.OverlayContentAlignment = "Center";
            s.OverlayScreenMargin = 40;
            s.OverlayBlockOrder = "Incident,Title,ControllerName,Timer,Metadata,Instruction,Status";
            s.OverlayUseBorderGradient = true;
            s.OverlayBorderGradientStartColor = accent;
            s.OverlayBorderGradientEndColor = "#FFFF5A4A";
            s.OverlayBorderGradientAngle = 125;
            s.OverlayShowBorderGlow = true;
            s.OverlayBorderGlowColor = WithAlpha(accent, "90");
            s.OverlayBorderGlowBlur = 20;
            s.OverlayBorderGlowOpacity = 28;
            ApplyScene(s, true, "#FF12080A", 155, true, true,
                "#26FF9A3C", 50, 12, 58,
                "#22FF5A4A", 88, 78, 62,
                "#18FFD36A", 18, 82, 50);
        }

        // Quiet utility card: type-first, almost no chrome.
        private static void ApplyMinimal(ControllerSessionManagerSettings s)
        {
            var accent = "#FF8A97A8";
            ApplyValues(s, 92, "#4D000000", "#CC101318", accent, "#FFE8ECF2", "#FFC8A66A",
                22, 15, 13, 12, 22, 12, false, false, true, "Left", 18, 8, true, 2, 4);
            ApplyTypography(s, NotificationFontCatalog.Poppins, "Regular",
                NotificationFontCatalog.Poppins, "Regular",
                NotificationFontCatalog.Poppins, "Regular");
            ApplyEnhancements(s, false, false, true, true, true,
                "Center", "Fade", "Left", 420, false);
            s.OverlayAccentColor = "#FFE8ECF2";
            s.OverlayInstructionColor = "#FF9AA6B4";
            s.OverlayShowDisconnectTimer = true;
            s.OverlayContentAlignment = "Left";
            s.OverlayScreenMargin = 28;
            s.OverlayBlockOrder = "Title,Controller,Timer,Instruction,Status";
            ApplyAccentRail(s, 2, 0, 0, 0);
        }

        // Letterbox pause: wide card, gold/cyan scene wash, timer in the copy.
        private static void ApplyCinematic(ControllerSessionManagerSettings s)
        {
            var accent = "#FF57C7E8";
            ApplyValues(s, 100, "#D205070A", "#F20C1014", accent, "#FFF7FBFC", "#FFFFC45C",
                30, 18, 15, 13, 56, 16, true, true, true, "Left", 30, 12, true, 1, 4);
            ApplyTypography(s, NotificationFontCatalog.Outfit, "SemiBold",
                NotificationFontCatalog.Outfit, "Regular",
                NotificationFontCatalog.Outfit, "Regular");
            ApplyEnhancements(s, true, true, true, true, true,
                "Bottom", "Fade", "Bottom", 780, true);
            s.OverlayInstructionColor = "#FFBFD6DC";
            s.OverlayUseGradient = true;
            s.OverlayGradientColor = "#F2141A1E";
            s.OverlayGradientAngle = 180;
            s.OverlayShowDisconnectTimer = true;
            s.OverlayStatusInMetadata = true;
            s.OverlayShowControllerContainer = true;
            s.OverlayControllerContainerColor = "#22182228";
            s.OverlayControllerContainerBorderColor = WithAlpha(accent, "40");
            s.OverlayControllerContainerCornerRadius = 4;
            s.OverlayIncidentBadgeCornerRadius = 4;
            s.OverlayConnectionBadgeCornerRadius = 4;
            s.OverlayBatteryBadgeCornerRadius = 4;
            s.OverlayContentAlignment = "Center";
            s.OverlayScreenMargin = 48;
            s.OverlayBlockOrder = "Title,Controller,Metadata,Timer,Instruction,Status";
            s.OverlayUseIndependentBorders = true;
            s.OverlayBorderLeftThickness = 0;
            s.OverlayBorderTopThickness = 0;
            s.OverlayBorderRightThickness = 0;
            s.OverlayBorderBottomThickness = 2;
            s.OverlayUseBorderGradient = true;
            s.OverlayBorderGradientStartColor = accent;
            s.OverlayBorderGradientEndColor = "#FFFFC45C";
            s.OverlayBorderGradientAngle = 90;
            ApplyScene(s, true, "#FF050608", 170, true, false,
                "#2457C7E8", 22, 18, 72,
                "#22FFC45C", 80, 16, 58,
                "#163DE0B5", 70, 88, 68);
        }

        private static void ApplyValues(
            ControllerSessionManagerSettings s,
            int scale, string dim, string card, string accent, string text, string warning,
            int title, int controller, int instruction, int status, int controllerIcon, int statusIcon,
            bool showControllerIcon, bool showStatusIcon, bool showName, string iconPos,
            int padding, int spacing, bool showBorder, int borderThickness, int corner)
        {
            s.OverlayScalePercent = scale;
            s.OverlayDimColor = dim;
            s.OverlayCardColor = card;
            s.OverlayUseGradient = false;
            s.OverlayGradientColor = card;
            s.OverlayGradientAngle = 0;
            s.OverlayUppercaseTitle = false;
            s.OverlayLayoutMode = "Standard";
            s.OverlaySceneUseGradient = false;
            s.OverlaySceneGradientColor = "#FF05060A";
            s.OverlaySceneGradientAngle = 160;
            s.OverlaySceneUseBackgroundImage = false;
            s.OverlaySceneBackgroundImagePath = string.Empty;
            s.OverlaySceneBackgroundImageStretch = "UniformToFill";
            s.OverlaySceneBackgroundImageHorizontalAlignment = "Center";
            s.OverlaySceneBackgroundImageVerticalAlignment = "Center";
            s.OverlaySceneBackgroundImageOpacity = 100;
            s.OverlaySceneUseAmbientGlows = false;
            s.OverlaySceneGlow1Color = "#293FE0E8";
            s.OverlaySceneGlow1X = 20;
            s.OverlaySceneGlow1Y = 25;
            s.OverlaySceneGlow1Radius = 60;
            s.OverlaySceneGlow2Color = "#24B18CFF";
            s.OverlaySceneGlow2X = 85;
            s.OverlaySceneGlow2Y = 20;
            s.OverlaySceneGlow2Radius = 60;
            s.OverlaySceneGlow3Color = "#196EE7A0";
            s.OverlaySceneGlow3X = 75;
            s.OverlaySceneGlow3Y = 85;
            s.OverlaySceneGlow3Radius = 65;
            s.OverlaySceneShowGrid = false;
            s.OverlaySceneGridColor = "#09FFFFFF";
            s.OverlaySceneGridSize = 44;
            s.OverlaySplitControllerSide = "Left";
            s.OverlayShowSplitDivider = false;
            s.OverlaySplitDividerColor = WithAlpha(accent, "45");
            s.OverlaySplitDividerThickness = 1;
            s.OverlayShowIncidentBadge = false;
            s.OverlayShowDisconnectTimer = false;
            s.OverlayIncidentBadgeTextColor = text;
            s.OverlayIncidentBadgeBackgroundColor = WithAlpha(accent, "28");
            s.OverlayIncidentBadgeBorderColor = WithAlpha(accent, "55");
            s.OverlayIncidentBadgeBorderThickness = 1;
            s.OverlayIncidentBadgeCornerRadius = Math.Min(10, Math.Max(0, corner / 2));
            s.OverlayIncidentBadgeTextSize = 11;
            s.OverlayStatusInMetadata = false;
            s.OverlayInstructionColor = accent;
            s.OverlayControllerIconColor = text;
            s.OverlayUseBackgroundImage = false;
            s.OverlayBackgroundImagePath = string.Empty;
            s.OverlayBackgroundImageStretch = "UniformToFill";
            s.OverlayBackgroundImageHorizontalAlignment = "Center";
            s.OverlayBackgroundImageVerticalAlignment = "Center";
            s.OverlayBackgroundImageOpacity = 100;
            s.OverlayBackgroundImageTintOpacity = 40;
            s.OverlayShowControllerContainer = false;
            s.OverlayControllerContainerColor = WithAlpha(accent, "22");
            s.OverlayControllerContainerBorderColor = WithAlpha(accent, "55");
            s.OverlayControllerContainerBorderThickness = 1;
            s.OverlayControllerContainerCornerRadius = Math.Max(4, corner / 2);
            s.OverlayControllerContainerPadding = 12;
            s.OverlayAccentColor = accent;
            s.OverlayTextColor = text;
            s.OverlayWarningColor = warning;
            s.OverlayTitleFontSize = title;
            s.OverlayControllerFontSize = controller;
            s.OverlayInstructionFontSize = instruction;
            s.OverlayStatusFontSize = status;
            s.OverlayControllerIconSize = controllerIcon;
            s.OverlayStatusIconSize = statusIcon;
            s.OverlayShowControllerIcon = showControllerIcon;
            s.OverlayShowStatusIcon = showStatusIcon;
            s.OverlayShowControllerName = showName;
            s.OverlayControllerIconPosition = iconPos;
            s.OverlayPadding = padding;
            s.OverlayElementSpacing = spacing;
            s.OverlayShowBorder = showBorder;
            s.OverlayBorderThickness = borderThickness;
            s.OverlayCornerRadius = corner;
            s.OverlayUseBorderGradient = false;
            s.OverlayBorderGradientStartColor = accent;
            s.OverlayBorderGradientEndColor = text;
            s.OverlayBorderGradientAngle = 90;
            s.OverlayShowBorderGlow = false;
            s.OverlayBorderGlowColor = WithAlpha(accent, "70");
            s.OverlayBorderGlowBlur = 14;
            s.OverlayBorderGlowOpacity = 24;
            s.OverlayConnectionBadgeTextColor = text;
            s.OverlayConnectionBadgeIconColor = text;
            s.OverlayConnectionBadgeBackgroundColor = WithAlpha(text, "16");
            s.OverlayConnectionBadgeBorderColor = WithAlpha(text, "32");
            s.OverlayConnectionBadgeBorderThickness = 1;
            s.OverlayConnectionBadgeCornerRadius = Math.Min(10, Math.Max(0, corner / 2));
            s.OverlayConnectionBadgeIconSize = 13;
            s.OverlayConnectionBadgeTextSize = 12;
            s.OverlayBatteryBadgeTextColor = warning;
            s.OverlayBatteryBadgeIconColor = warning;
            s.OverlayBatteryBadgeBackgroundColor = WithAlpha(warning, "22");
            s.OverlayBatteryBadgeBorderColor = WithAlpha(warning, "48");
            s.OverlayBatteryBadgeBorderThickness = 1;
            s.OverlayBatteryBadgeCornerRadius = Math.Min(10, Math.Max(0, corner / 2));
            s.OverlayBatteryBadgeIconSize = 13;
            s.OverlayBatteryBadgeTextSize = 12;
            s.OverlayBatteryBadgeUseStateColors = true;
            s.OverlayBatteryBadgeFullColor = "#FF4FC27E";
            s.OverlayBatteryBadgeMediumColor = "#FFF0B14A";
            s.OverlayBatteryBadgeLowColor = "#FFE05252";
            s.OverlayBatteryBadgeEmptyColor = "#FFC92D45";
            s.OverlayBlockOrder = "Title,Controller,Metadata,Instruction,Status";
            s.OverlayMetadataOrientation = "Horizontal";
            s.OverlayUseIndependentBorders = false;
            s.OverlayBorderLeftThickness = borderThickness;
            s.OverlayBorderTopThickness = borderThickness;
            s.OverlayBorderRightThickness = borderThickness;
            s.OverlayBorderBottomThickness = borderThickness;
        }

        private static void ApplyTypography(
            ControllerSessionManagerSettings settings,
            string titleFamily, string titleWeight,
            string bodyFamily, string bodyWeight,
            string statusFamily, string statusWeight)
        {
            settings.OverlayFontFamily = titleFamily;
            settings.OverlayFontWeight = titleWeight;
            settings.OverlayTitleFontFamily = titleFamily;
            settings.OverlayTitleFontWeight = titleWeight;
            settings.OverlayControllerFontFamily = bodyFamily;
            settings.OverlayControllerFontWeight = bodyWeight;
            settings.OverlayInstructionFontFamily = bodyFamily;
            settings.OverlayInstructionFontWeight = bodyWeight;
            settings.OverlayStatusFontFamily = statusFamily;
            settings.OverlayStatusFontWeight = statusWeight;
        }

        private static void ApplyAccentRail(
            ControllerSessionManagerSettings s, int left, int top, int right, int bottom)
        {
            s.OverlayUseIndependentBorders = true;
            s.OverlayShowBorder = true;
            s.OverlayBorderLeftThickness = left;
            s.OverlayBorderTopThickness = top;
            s.OverlayBorderRightThickness = right;
            s.OverlayBorderBottomThickness = bottom;
        }

        private static void ApplyScene(
            ControllerSessionManagerSettings s,
            bool useGradient, string gradientColor, int angle,
            bool useGlows, bool showGrid,
            string glow1, int x1, int y1, int r1,
            string glow2, int x2, int y2, int r2,
            string glow3, int x3, int y3, int r3)
        {
            s.OverlaySceneUseGradient = useGradient;
            s.OverlaySceneGradientColor = gradientColor;
            s.OverlaySceneGradientAngle = angle;
            s.OverlaySceneUseAmbientGlows = useGlows;
            s.OverlaySceneGlow1Color = glow1;
            s.OverlaySceneGlow1X = x1;
            s.OverlaySceneGlow1Y = y1;
            s.OverlaySceneGlow1Radius = r1;
            s.OverlaySceneGlow2Color = glow2;
            s.OverlaySceneGlow2X = x2;
            s.OverlaySceneGlow2Y = y2;
            s.OverlaySceneGlow2Radius = r2;
            s.OverlaySceneGlow3Color = glow3;
            s.OverlaySceneGlow3X = x3;
            s.OverlaySceneGlow3Y = y3;
            s.OverlaySceneGlow3Radius = r3;
            s.OverlaySceneShowGrid = showGrid;
            s.OverlaySceneGridColor = "#0CFFFFFF";
            s.OverlaySceneGridSize = 48;
        }

        private static string WithAlpha(string color, string alpha)
        {
            return !string.IsNullOrWhiteSpace(color) && color.Length == 9 && color[0] == '#'
                ? "#" + alpha + color.Substring(3)
                : color;
        }

        private static void ApplyEnhancements(ControllerSessionManagerSettings settings,
            bool showConnection, bool showBattery, bool showTitle, bool showInstruction,
            bool showPauseStatus, string cardPosition, string animation, string borderPosition,
            int cardWidth, bool showShadow)
        {
            settings.OverlayShowConnectionBadge = showConnection;
            settings.OverlayShowBatteryBadge = showBattery;
            settings.OverlayShowTitle = showTitle;
            settings.OverlayShowInstruction = showInstruction;
            settings.OverlayShowPauseStatus = showPauseStatus;
            settings.OverlayCardPosition = cardPosition;
            settings.OverlayAnimation = animation;
            settings.OverlayBorderPosition = borderPosition;
            settings.OverlayCardWidth = cardWidth;
            settings.OverlayShowShadow = showShadow;
        }
    }
}
