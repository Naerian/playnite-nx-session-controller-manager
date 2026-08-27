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
        public static readonly string[] PluginPresets = { Soft, Compact, Bold, Arcade, Minimal };
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
                    ApplyValues(settings, 88, "#A0000000", "#E0080A0E", "#FF3D7ECC", "#FFE8EEF6", "#FFC9922A",
                        22, 16, 14, 12, 22, 12, true, true, true, "Left", 18, 8, true, 3, 6);
                    ApplyTypography(settings, NotificationFontCatalog.Rajdhani, "SemiBold");
                    ApplyEnhancements(settings, true, false, false, true, true,
                        "Center", "Slide", "Top", 500, true);
                    settings.OverlayContentAlignment = "Left";
                    settings.OverlayScreenMargin = 24;
                    break;
                case Bold:
                    ApplyValues(settings, 100, "#B0000000", "#F01C202C", "#FF4DA3FF", "#FFFFFFFF", "#FFFFB000",
                        36, 28, 22, 17, 40, 22, true, true, true, "Left", 44, 18, true, 5, 10);
                    ApplyTypography(settings, NotificationFontCatalog.Outfit, "Bold");
                    ApplyEnhancements(settings, true, true, true, true, true,
                        "Center", "FadeScale", "Full", 760, true);
                    settings.OverlayContentAlignment = "Left";
                    settings.OverlayScreenMargin = 42;
                    break;
                case Arcade:
                    ApplyValues(settings, 110, "#C0080A28", "#F0080A28", "#FF00D4FF", "#FFE6F0FF", "#FFFFE566",
                        32, 24, 18, 15, 36, 18, true, true, true, "Top", 36, 16, true, 2, 22);
                    ApplyTypography(settings, NotificationFontCatalog.Orbitron, "SemiBold");
                    ApplyEnhancements(settings, true, true, true, true, true,
                        "Center", "FadeScale", "Full", 680, true);
                    settings.OverlayContentAlignment = "Center";
                    settings.OverlayScreenMargin = 48;
                    break;
                case Minimal:
                    ApplyValues(settings, 92, "#70000000", "#990A0C10", "#FF6A849E", "#FFE6E8EE", "#FFB8965A",
                        24, 16, 14, 12, 20, 12, false, false, true, "Left", 16, 6, false, 0, 4);
                    ApplyTypography(settings, NotificationFontCatalog.Poppins, "Regular");
                    ApplyEnhancements(settings, true, false, false, true, false,
                        "Center", "Fade", "Full", 500, false);
                    settings.OverlayContentAlignment = "Right";
                    settings.OverlayScreenMargin = 28;
                    break;
                default:
                    ApplyValues(settings, 100, "#96000000", "#F4161A22", "#FF5BA3E8", "#FFF5F7FA", "#FFF0B14A",
                        30, 22, 19, 15, 30, 18, true, true, true, "Left", 34, 14, true, 3, 13);
                    ApplyTypography(settings, NotificationFontCatalog.Inter, "SemiBold");
                    ApplyEnhancements(settings, true, true, true, true, true,
                        "Center", "FadeScale", "Full", 620, true);
                    settings.OverlayContentAlignment = "Center";
                    settings.OverlayScreenMargin = 42;
                    CreatorThemeCatalog.TryApply(settings, preset, "overlay");
                    break;
            }

            settings.OverlayStylePreset = preset;
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
            s.OverlayIncidentBadgeTextColor = text;
            s.OverlayIncidentBadgeBackgroundColor = WithAlpha(accent, "30");
            s.OverlayIncidentBadgeBorderColor = WithAlpha(accent, "70");
            s.OverlayIncidentBadgeBorderThickness = 1;
            s.OverlayIncidentBadgeCornerRadius = Math.Min(10, Math.Max(0, corner / 2));
            s.OverlayIncidentBadgeTextSize = 12;
            s.OverlayStatusInMetadata = false;
            s.OverlayInstructionColor = accent;
            s.OverlayControllerIconColor = text;
            s.OverlayUseBackgroundImage = false;
            s.OverlayBackgroundImagePath = string.Empty;
            s.OverlayShowControllerContainer = false;
            s.OverlayControllerContainerColor = WithAlpha(accent, "28");
            s.OverlayControllerContainerBorderColor = WithAlpha(accent, "70");
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
            s.OverlayShowBorderGlow = false;
            s.OverlayConnectionBadgeTextColor = text;
            s.OverlayConnectionBadgeIconColor = text;
            s.OverlayConnectionBadgeBackgroundColor = WithAlpha(accent, "30");
            s.OverlayConnectionBadgeBorderColor = WithAlpha(accent, "70");
            s.OverlayConnectionBadgeBorderThickness = 1;
            s.OverlayConnectionBadgeCornerRadius = Math.Min(10, Math.Max(0, corner / 2));
            s.OverlayConnectionBadgeIconSize = 14;
            s.OverlayConnectionBadgeTextSize = 13;
            s.OverlayBatteryBadgeTextColor = warning;
            s.OverlayBatteryBadgeIconColor = warning;
            s.OverlayBatteryBadgeBackgroundColor = WithAlpha(warning, "30");
            s.OverlayBatteryBadgeBorderColor = WithAlpha(warning, "70");
            s.OverlayBatteryBadgeBorderThickness = 1;
            s.OverlayBatteryBadgeCornerRadius = Math.Min(10, Math.Max(0, corner / 2));
            s.OverlayBatteryBadgeIconSize = 14;
            s.OverlayBatteryBadgeTextSize = 13;
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

        private static void ApplyTypography(ControllerSessionManagerSettings settings, string family, string weight)
        {
            settings.OverlayFontFamily = family;
            settings.OverlayFontWeight = weight;
            settings.OverlayTitleFontFamily = family;
            settings.OverlayTitleFontWeight = weight;
            settings.OverlayControllerFontFamily = family;
            settings.OverlayControllerFontWeight = weight;
            settings.OverlayInstructionFontFamily = family;
            settings.OverlayInstructionFontWeight = weight;
            settings.OverlayStatusFontFamily = family;
            settings.OverlayStatusFontWeight = weight;
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
