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
        public const string Aniki = "Aniki";
        public const string Helium = "Helium";

        public static readonly string[] PluginPresets = { Soft, Compact, Bold, Arcade, Minimal };
        public static string[] CreatorPresets
        {
            get { return new[] { Aniki, Helium }.Concat(CreatorThemeCatalog.GetPresetIds("overlay"))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); }
        }
        public static string[] NamedPresets { get { return PluginPresets.Concat(CreatorPresets).ToArray(); } }

        public static bool IsCreatorPreset(string presetId)
        {
            var preset = Normalize(presetId);
            return string.Equals(preset, Aniki, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(preset, Helium, StringComparison.OrdinalIgnoreCase) ||
                CreatorThemeCatalog.Contains(preset, "overlay");
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

            return Soft;
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
                case Aniki:
                    if (CreatorThemeCatalog.Contains(Aniki, "overlay"))
                    {
                        ApplyValues(settings, 100, "#96000000", "#F4161A22", "#FF5BA3E8", "#FFF5F7FA", "#FFF0B14A",
                            30, 22, 19, 15, 30, 18, true, true, true, "Left", 34, 14, true, 3, 13);
                        ApplyTypography(settings, NotificationFontCatalog.Inter, "SemiBold");
                        ApplyEnhancements(settings, true, true, true, true, true, "Center", "FadeScale", "Full", 620, true);
                        CreatorThemeCatalog.TryApply(settings, Aniki, "overlay");
                        break;
                    }
                    ApplyValues(settings, 100, "#A8000000", "#FA0C1118", "#FFD6B16F", "#FFF5F2EC", "#FFFFC857",
                        34, 22, 18, 15, 42, 18, true, true, true, "Left", 40, 12, true, 2, 15);
                    ApplyTypography(settings, NotificationFontCatalog.Exo2, "SemiBold");
                    ApplyEnhancements(settings, true, true, true, true, true,
                        "Center", "FadeScale", "Full", 720, true);
                    settings.OverlayContentAlignment = "Center";
                    settings.OverlayScreenMargin = 46;
                    settings.OverlayUseGradient = true;
                    settings.OverlayGradientColor = "#FF151D26";
                    settings.OverlayGradientAngle = 135;
                    settings.OverlayUseBorderGradient = true;
                    settings.OverlayBorderGradientStartColor = "#B3FFFFFF";
                    settings.OverlayBorderGradientEndColor = "#FFD6B16F";
                    settings.OverlayBorderGradientAngle = 45;
                    settings.OverlayShowBorderGlow = true;
                    settings.OverlayBorderGlowColor = "#FFE9C48A";
                    settings.OverlayBorderGlowBlur = 32;
                    settings.OverlayBorderGlowOpacity = 90;
                    settings.OverlayShowControllerContainer = true;
                    settings.OverlayControllerContainerColor = "#33111820";
                    settings.OverlayControllerContainerBorderColor = "#55D6B16F";
                    settings.OverlayControllerContainerBorderThickness = 1;
                    settings.OverlayControllerContainerCornerRadius = 12;
                    settings.OverlayControllerContainerPadding = 14;
                    settings.OverlayBlockOrder = "Controller,Title,Metadata,Instruction,Status";
                    settings.OverlayMetadataOrientation = "Horizontal";
                    break;
                case Helium:
                    if (CreatorThemeCatalog.Contains(Helium, "overlay"))
                    {
                        ApplyValues(settings, 100, "#96000000", "#F4161A22", "#FF5BA3E8", "#FFF5F7FA", "#FFF0B14A",
                            30, 22, 19, 15, 30, 18, true, true, true, "Left", 34, 14, true, 3, 13);
                        ApplyTypography(settings, NotificationFontCatalog.Inter, "SemiBold");
                        ApplyEnhancements(settings, true, true, true, true, true, "Center", "FadeScale", "Full", 620, true);
                        CreatorThemeCatalog.TryApply(settings, Helium, "overlay");
                        break;
                    }
                    ApplyValues(settings, 100, "#A0000000", "#F225282E", "#FF1A9FFF", "#FFDDE3E6", "#FFFFA500",
                        30, 20, 16, 14, 32, 16, true, true, true, "Left", 32, 10, true, 2, 3);
                    ApplyTypography(settings, NotificationFontCatalog.Trebuchet, "SemiBold");
                    ApplyEnhancements(settings, true, true, true, true, true,
                        "Center", "FadeScale", "Full", 620, true);
                    settings.OverlayContentAlignment = "Left";
                    settings.OverlayScreenMargin = 36;
                    settings.OverlayUseGradient = true;
                    settings.OverlayGradientColor = "#F23C4047";
                    settings.OverlayGradientAngle = 90;
                    settings.OverlayShowControllerContainer = true;
                    settings.OverlayControllerContainerColor = "#99212124";
                    settings.OverlayControllerContainerBorderColor = "#663E6184";
                    settings.OverlayControllerContainerBorderThickness = 1;
                    settings.OverlayControllerContainerCornerRadius = 3;
                    settings.OverlayControllerContainerPadding = 12;
                    settings.OverlayBlockOrder = "Title,Instruction,Controller,Metadata,Status";
                    settings.OverlayUseIndependentBorders = true;
                    settings.OverlayBorderLeftThickness = 4;
                    settings.OverlayBorderTopThickness = 1;
                    settings.OverlayBorderRightThickness = 1;
                    settings.OverlayBorderBottomThickness = 1;
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
