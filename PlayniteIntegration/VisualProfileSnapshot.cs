using System;
using System.IO;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>
    /// Portable bundle of notification, overlay and sound appearance settings.
    /// </summary>
    public sealed class VisualProfileSnapshot
    {
        public const int CurrentVersion = 5;
        public const string FileExtension = ".pcvisual";

        public int Version { get; set; }
        public string Name { get; set; }
        public string ExportedUtc { get; set; }

        public string NotificationStylePreset { get; set; }
        public string OverlayStylePreset { get; set; }

        public bool EnableNotificationSounds { get; set; }
        public string NotificationSoundPack { get; set; }
        public bool PlaySoundOnConnected { get; set; }
        public bool PlaySoundOnDisconnected { get; set; }
        public bool PlaySoundOnLowBattery { get; set; }
        public bool PlaySoundOnWarning { get; set; }
        public double NotificationSoundVolume { get; set; }

        public int NotificationWidth { get; set; }
        public int NotificationScalePercent { get; set; }
        public int NotificationDurationMilliseconds { get; set; }
        public string NotificationPosition { get; set; }
        public string NotificationBackgroundColor { get; set; }
        public bool NotificationUseBackgroundImage { get; set; }
        public string NotificationBackgroundImageStretch { get; set; }
        public string NotificationBackgroundImageHorizontalAlignment { get; set; }
        public string NotificationBackgroundImageVerticalAlignment { get; set; }
        public int NotificationBackgroundImageOpacity { get; set; }
        public int NotificationBackgroundImageTintOpacity { get; set; }
        public string NotificationBackgroundImageData { get; set; }
        public string NotificationBackgroundImageExtension { get; set; }
        public string NotificationTextColor { get; set; }
        public string NotificationSecondaryTextColor { get; set; }
        public string NotificationConnectedColor { get; set; }
        public string NotificationDisconnectedColor { get; set; }
        public string NotificationWarningColor { get; set; }
        public string NotificationLowBatteryColor { get; set; }
        public int NotificationTitleFontSize { get; set; }
        public int NotificationMessageFontSize { get; set; }
        public int NotificationIconSize { get; set; }
        public string NotificationIconPosition { get; set; }
        public int NotificationPadding { get; set; }
        public int NotificationElementSpacing { get; set; }
        public bool NotificationShowBorder { get; set; }
        public string NotificationBorderPosition { get; set; }
        public int NotificationBorderThickness { get; set; }
        public int NotificationCornerRadius { get; set; }
        public bool NotificationShowConnectionBadge { get; set; }
        public int NotificationScreenMargin { get; set; }
        public bool NotificationShowShadow { get; set; }
        public string NotificationFontFamily { get; set; }
        public string NotificationFontWeight { get; set; }
        public string NotificationTextAlignment { get; set; }
        public string NotificationAccentMode { get; set; }
        public string NotificationAnimation { get; set; }
        public bool NotificationShowTitle { get; set; }

        public int DesktopNotificationWidth { get; set; }
        public int DesktopNotificationScalePercent { get; set; }
        public int DesktopNotificationDurationMilliseconds { get; set; }
        public string DesktopNotificationPosition { get; set; }
        public string DesktopNotificationBackgroundColor { get; set; }
        public bool DesktopNotificationUseBackgroundImage { get; set; }
        public string DesktopNotificationBackgroundImageStretch { get; set; }
        public string DesktopNotificationBackgroundImageHorizontalAlignment { get; set; }
        public string DesktopNotificationBackgroundImageVerticalAlignment { get; set; }
        public int DesktopNotificationBackgroundImageOpacity { get; set; }
        public int DesktopNotificationBackgroundImageTintOpacity { get; set; }
        public string DesktopNotificationBackgroundImageData { get; set; }
        public string DesktopNotificationBackgroundImageExtension { get; set; }
        public string DesktopNotificationTextColor { get; set; }
        public string DesktopNotificationSecondaryTextColor { get; set; }
        public string DesktopNotificationConnectedColor { get; set; }
        public string DesktopNotificationDisconnectedColor { get; set; }
        public string DesktopNotificationWarningColor { get; set; }
        public string DesktopNotificationLowBatteryColor { get; set; }
        public int DesktopNotificationTitleFontSize { get; set; }
        public int DesktopNotificationMessageFontSize { get; set; }
        public int DesktopNotificationIconSize { get; set; }
        public string DesktopNotificationIconPosition { get; set; }
        public int DesktopNotificationPadding { get; set; }
        public int DesktopNotificationElementSpacing { get; set; }
        public bool DesktopNotificationShowBorder { get; set; }
        public string DesktopNotificationBorderPosition { get; set; }
        public int DesktopNotificationBorderThickness { get; set; }
        public int DesktopNotificationCornerRadius { get; set; }
        public bool DesktopNotificationShowConnectionBadge { get; set; }
        public int DesktopNotificationScreenMargin { get; set; }
        public bool DesktopNotificationShowShadow { get; set; }
        public string DesktopNotificationFontFamily { get; set; }
        public string DesktopNotificationFontWeight { get; set; }
        public string DesktopNotificationTextAlignment { get; set; }
        public string DesktopNotificationAccentMode { get; set; }
        public string DesktopNotificationAnimation { get; set; }
        public bool DesktopNotificationShowTitle { get; set; }

        public int OverlayScalePercent { get; set; }
        public string OverlayDimColor { get; set; }
        public string OverlayCardColor { get; set; }
        public string OverlayAccentColor { get; set; }
        public string OverlayTextColor { get; set; }
        public string OverlayWarningColor { get; set; }
        public int OverlayTitleFontSize { get; set; }
        public int OverlayControllerFontSize { get; set; }
        public int OverlayInstructionFontSize { get; set; }
        public int OverlayStatusFontSize { get; set; }
        public int OverlayControllerIconSize { get; set; }
        public int OverlayStatusIconSize { get; set; }
        public bool OverlayShowControllerIcon { get; set; }
        public bool OverlayShowStatusIcon { get; set; }
        public bool OverlayShowControllerName { get; set; }
        public bool OverlayShowConnectionBadge { get; set; }
        public bool OverlayShowBatteryBadge { get; set; }
        public bool OverlayShowTitle { get; set; }
        public bool OverlayShowInstruction { get; set; }
        public bool OverlayShowPauseStatus { get; set; }
        public string OverlayControllerIconPosition { get; set; }
        public string OverlayCardPosition { get; set; }
        public string OverlayAnimation { get; set; }
        public string OverlayBorderPosition { get; set; }
        public int OverlayCardWidth { get; set; }
        public int OverlayPadding { get; set; }
        public int OverlayElementSpacing { get; set; }
        public bool OverlayShowBorder { get; set; }
        public bool OverlayShowShadow { get; set; }
        public int OverlayBorderThickness { get; set; }
        public int OverlayCornerRadius { get; set; }
        public string OverlayFontFamily { get; set; }
        public string OverlayFontWeight { get; set; }
        public string OverlayTitleFontFamily { get; set; }
        public string OverlayTitleFontWeight { get; set; }
        public string OverlayControllerFontFamily { get; set; }
        public string OverlayControllerFontWeight { get; set; }
        public string OverlayInstructionFontFamily { get; set; }
        public string OverlayInstructionFontWeight { get; set; }
        public string OverlayStatusFontFamily { get; set; }
        public string OverlayStatusFontWeight { get; set; }
        public string OverlayConnectionBadgeTextColor { get; set; }
        public string OverlayConnectionBadgeIconColor { get; set; }
        public string OverlayConnectionBadgeBackgroundColor { get; set; }
        public string OverlayConnectionBadgeBorderColor { get; set; }
        public int OverlayConnectionBadgeBorderThickness { get; set; }
        public int OverlayConnectionBadgeCornerRadius { get; set; }
        public int OverlayConnectionBadgeIconSize { get; set; }
        public int OverlayConnectionBadgeTextSize { get; set; }
        public string OverlayBatteryBadgeTextColor { get; set; }
        public string OverlayBatteryBadgeIconColor { get; set; }
        public string OverlayBatteryBadgeBackgroundColor { get; set; }
        public string OverlayBatteryBadgeBorderColor { get; set; }
        public int OverlayBatteryBadgeBorderThickness { get; set; }
        public int OverlayBatteryBadgeCornerRadius { get; set; }
        public int OverlayBatteryBadgeIconSize { get; set; }
        public int OverlayBatteryBadgeTextSize { get; set; }
        public bool OverlayBatteryBadgeUseStateColors { get; set; }
        public string OverlayBatteryBadgeFullColor { get; set; }
        public string OverlayBatteryBadgeMediumColor { get; set; }
        public string OverlayBatteryBadgeLowColor { get; set; }
        public string OverlayBatteryBadgeEmptyColor { get; set; }

        public static VisualProfileSnapshot FromSettings(ControllerSessionManagerSettings settings, string name)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            return new VisualProfileSnapshot
            {
                Version = CurrentVersion,
                Name = string.IsNullOrWhiteSpace(name) ? "Visual profile" : name.Trim(),
                ExportedUtc = DateTime.UtcNow.ToString("O"),
                NotificationStylePreset = settings.NotificationStylePreset,
                OverlayStylePreset = settings.OverlayStylePreset,
                EnableNotificationSounds = settings.EnableNotificationSounds,
                NotificationSoundPack = settings.NotificationSoundPack,
                PlaySoundOnConnected = settings.PlaySoundOnConnected,
                PlaySoundOnDisconnected = settings.PlaySoundOnDisconnected,
                PlaySoundOnLowBattery = settings.PlaySoundOnLowBattery,
                PlaySoundOnWarning = settings.PlaySoundOnWarning,
                NotificationSoundVolume = settings.NotificationSoundVolume,
                NotificationWidth = settings.NotificationWidth,
                NotificationScalePercent = settings.NotificationScalePercent,
                NotificationDurationMilliseconds = settings.NotificationDurationMilliseconds,
                NotificationPosition = settings.NotificationPosition,
                NotificationBackgroundColor = settings.NotificationBackgroundColor,
                NotificationUseBackgroundImage = settings.NotificationUseBackgroundImage,
                NotificationBackgroundImageStretch = settings.NotificationBackgroundImageStretch,
                NotificationBackgroundImageHorizontalAlignment = settings.NotificationBackgroundImageHorizontalAlignment,
                NotificationBackgroundImageVerticalAlignment = settings.NotificationBackgroundImageVerticalAlignment,
                NotificationBackgroundImageOpacity = settings.NotificationBackgroundImageOpacity,
                NotificationBackgroundImageTintOpacity = settings.NotificationBackgroundImageTintOpacity,
                NotificationBackgroundImageData = ReadImageData(settings.NotificationBackgroundImagePath),
                NotificationBackgroundImageExtension = ImageExtension(settings.NotificationBackgroundImagePath),
                NotificationTextColor = settings.NotificationTextColor,
                NotificationSecondaryTextColor = settings.NotificationSecondaryTextColor,
                NotificationConnectedColor = settings.NotificationConnectedColor,
                NotificationDisconnectedColor = settings.NotificationDisconnectedColor,
                NotificationWarningColor = settings.NotificationWarningColor,
                NotificationLowBatteryColor = settings.NotificationLowBatteryColor,
                NotificationTitleFontSize = settings.NotificationTitleFontSize,
                NotificationMessageFontSize = settings.NotificationMessageFontSize,
                NotificationIconSize = settings.NotificationIconSize,
                NotificationIconPosition = settings.NotificationIconPosition,
                NotificationPadding = settings.NotificationPadding,
                NotificationElementSpacing = settings.NotificationElementSpacing,
                NotificationShowBorder = settings.NotificationShowBorder,
                NotificationBorderPosition = settings.NotificationBorderPosition,
                NotificationBorderThickness = settings.NotificationBorderThickness,
                NotificationCornerRadius = settings.NotificationCornerRadius,
                NotificationShowConnectionBadge = settings.NotificationShowConnectionBadge,
                NotificationScreenMargin = settings.NotificationScreenMargin,
                NotificationShowShadow = settings.NotificationShowShadow,
                NotificationFontFamily = settings.NotificationFontFamily,
                NotificationFontWeight = settings.NotificationFontWeight,
                NotificationTextAlignment = settings.NotificationTextAlignment,
                NotificationAccentMode = settings.NotificationAccentMode,
                NotificationAnimation = settings.NotificationAnimation,
                NotificationShowTitle = settings.NotificationShowTitle,
                DesktopNotificationWidth = settings.DesktopNotificationWidth,
                DesktopNotificationScalePercent = settings.DesktopNotificationScalePercent,
                DesktopNotificationDurationMilliseconds = settings.DesktopNotificationDurationMilliseconds,
                DesktopNotificationPosition = settings.DesktopNotificationPosition,
                DesktopNotificationBackgroundColor = settings.DesktopNotificationBackgroundColor,
                DesktopNotificationUseBackgroundImage = settings.DesktopNotificationUseBackgroundImage,
                DesktopNotificationBackgroundImageStretch = settings.DesktopNotificationBackgroundImageStretch,
                DesktopNotificationBackgroundImageHorizontalAlignment = settings.DesktopNotificationBackgroundImageHorizontalAlignment,
                DesktopNotificationBackgroundImageVerticalAlignment = settings.DesktopNotificationBackgroundImageVerticalAlignment,
                DesktopNotificationBackgroundImageOpacity = settings.DesktopNotificationBackgroundImageOpacity,
                DesktopNotificationBackgroundImageTintOpacity = settings.DesktopNotificationBackgroundImageTintOpacity,
                DesktopNotificationBackgroundImageData = ReadImageData(settings.DesktopNotificationBackgroundImagePath),
                DesktopNotificationBackgroundImageExtension = ImageExtension(settings.DesktopNotificationBackgroundImagePath),
                DesktopNotificationTextColor = settings.DesktopNotificationTextColor,
                DesktopNotificationSecondaryTextColor = settings.DesktopNotificationSecondaryTextColor,
                DesktopNotificationConnectedColor = settings.DesktopNotificationConnectedColor,
                DesktopNotificationDisconnectedColor = settings.DesktopNotificationDisconnectedColor,
                DesktopNotificationWarningColor = settings.DesktopNotificationWarningColor,
                DesktopNotificationLowBatteryColor = settings.DesktopNotificationLowBatteryColor,
                DesktopNotificationTitleFontSize = settings.DesktopNotificationTitleFontSize,
                DesktopNotificationMessageFontSize = settings.DesktopNotificationMessageFontSize,
                DesktopNotificationIconSize = settings.DesktopNotificationIconSize,
                DesktopNotificationIconPosition = settings.DesktopNotificationIconPosition,
                DesktopNotificationPadding = settings.DesktopNotificationPadding,
                DesktopNotificationElementSpacing = settings.DesktopNotificationElementSpacing,
                DesktopNotificationShowBorder = settings.DesktopNotificationShowBorder,
                DesktopNotificationBorderPosition = settings.DesktopNotificationBorderPosition,
                DesktopNotificationBorderThickness = settings.DesktopNotificationBorderThickness,
                DesktopNotificationCornerRadius = settings.DesktopNotificationCornerRadius,
                DesktopNotificationShowConnectionBadge = settings.DesktopNotificationShowConnectionBadge,
                DesktopNotificationScreenMargin = settings.DesktopNotificationScreenMargin,
                DesktopNotificationShowShadow = settings.DesktopNotificationShowShadow,
                DesktopNotificationFontFamily = settings.DesktopNotificationFontFamily,
                DesktopNotificationFontWeight = settings.DesktopNotificationFontWeight,
                DesktopNotificationTextAlignment = settings.DesktopNotificationTextAlignment,
                DesktopNotificationAccentMode = settings.DesktopNotificationAccentMode,
                DesktopNotificationAnimation = settings.DesktopNotificationAnimation,
                DesktopNotificationShowTitle = settings.DesktopNotificationShowTitle,
                OverlayScalePercent = settings.OverlayScalePercent,
                OverlayDimColor = settings.OverlayDimColor,
                OverlayCardColor = settings.OverlayCardColor,
                OverlayAccentColor = settings.OverlayAccentColor,
                OverlayTextColor = settings.OverlayTextColor,
                OverlayWarningColor = settings.OverlayWarningColor,
                OverlayTitleFontSize = settings.OverlayTitleFontSize,
                OverlayControllerFontSize = settings.OverlayControllerFontSize,
                OverlayInstructionFontSize = settings.OverlayInstructionFontSize,
                OverlayStatusFontSize = settings.OverlayStatusFontSize,
                OverlayControllerIconSize = settings.OverlayControllerIconSize,
                OverlayStatusIconSize = settings.OverlayStatusIconSize,
                OverlayShowControllerIcon = settings.OverlayShowControllerIcon,
                OverlayShowStatusIcon = settings.OverlayShowStatusIcon,
                OverlayShowControllerName = settings.OverlayShowControllerName,
                OverlayShowConnectionBadge = settings.OverlayShowConnectionBadge,
                OverlayShowBatteryBadge = settings.OverlayShowBatteryBadge,
                OverlayShowTitle = settings.OverlayShowTitle,
                OverlayShowInstruction = settings.OverlayShowInstruction,
                OverlayShowPauseStatus = settings.OverlayShowPauseStatus,
                OverlayControllerIconPosition = settings.OverlayControllerIconPosition,
                OverlayCardPosition = settings.OverlayCardPosition,
                OverlayAnimation = settings.OverlayAnimation,
                OverlayBorderPosition = settings.OverlayBorderPosition,
                OverlayCardWidth = settings.OverlayCardWidth,
                OverlayPadding = settings.OverlayPadding,
                OverlayElementSpacing = settings.OverlayElementSpacing,
                OverlayShowBorder = settings.OverlayShowBorder,
                OverlayShowShadow = settings.OverlayShowShadow,
                OverlayBorderThickness = settings.OverlayBorderThickness,
                OverlayCornerRadius = settings.OverlayCornerRadius,
                OverlayFontFamily = settings.OverlayFontFamily,
                OverlayFontWeight = settings.OverlayFontWeight,
                OverlayTitleFontFamily = settings.OverlayTitleFontFamily,
                OverlayTitleFontWeight = settings.OverlayTitleFontWeight,
                OverlayControllerFontFamily = settings.OverlayControllerFontFamily,
                OverlayControllerFontWeight = settings.OverlayControllerFontWeight,
                OverlayInstructionFontFamily = settings.OverlayInstructionFontFamily,
                OverlayInstructionFontWeight = settings.OverlayInstructionFontWeight,
                OverlayStatusFontFamily = settings.OverlayStatusFontFamily,
                OverlayStatusFontWeight = settings.OverlayStatusFontWeight,
                OverlayConnectionBadgeTextColor = settings.OverlayConnectionBadgeTextColor,
                OverlayConnectionBadgeIconColor = settings.OverlayConnectionBadgeIconColor,
                OverlayConnectionBadgeBackgroundColor = settings.OverlayConnectionBadgeBackgroundColor,
                OverlayConnectionBadgeBorderColor = settings.OverlayConnectionBadgeBorderColor,
                OverlayConnectionBadgeBorderThickness = settings.OverlayConnectionBadgeBorderThickness,
                OverlayConnectionBadgeCornerRadius = settings.OverlayConnectionBadgeCornerRadius,
                OverlayConnectionBadgeIconSize = settings.OverlayConnectionBadgeIconSize,
                OverlayConnectionBadgeTextSize = settings.OverlayConnectionBadgeTextSize,
                OverlayBatteryBadgeTextColor = settings.OverlayBatteryBadgeTextColor,
                OverlayBatteryBadgeIconColor = settings.OverlayBatteryBadgeIconColor,
                OverlayBatteryBadgeBackgroundColor = settings.OverlayBatteryBadgeBackgroundColor,
                OverlayBatteryBadgeBorderColor = settings.OverlayBatteryBadgeBorderColor,
                OverlayBatteryBadgeBorderThickness = settings.OverlayBatteryBadgeBorderThickness,
                OverlayBatteryBadgeCornerRadius = settings.OverlayBatteryBadgeCornerRadius,
                OverlayBatteryBadgeIconSize = settings.OverlayBatteryBadgeIconSize,
                OverlayBatteryBadgeTextSize = settings.OverlayBatteryBadgeTextSize,
                OverlayBatteryBadgeUseStateColors = settings.OverlayBatteryBadgeUseStateColors,
                OverlayBatteryBadgeFullColor = settings.OverlayBatteryBadgeFullColor,
                OverlayBatteryBadgeMediumColor = settings.OverlayBatteryBadgeMediumColor,
                OverlayBatteryBadgeLowColor = settings.OverlayBatteryBadgeLowColor,
                OverlayBatteryBadgeEmptyColor = settings.OverlayBatteryBadgeEmptyColor
            };
        }

        public void ApplyTo(ControllerSessionManagerSettings settings)
        {
            ApplyTo(settings, null);
        }

        public void ApplyTo(ControllerSessionManagerSettings settings, string imageDirectory)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            settings.NotificationStylePreset = NotificationStylePresets.Normalize(NotificationStylePreset);
            settings.OverlayStylePreset = OverlayStylePresets.Normalize(OverlayStylePreset);
            settings.EnableNotificationSounds = EnableNotificationSounds;
            settings.NotificationSoundPack = NotificationSoundCatalog.Normalize(NotificationSoundPack);
            settings.PlaySoundOnConnected = PlaySoundOnConnected;
            settings.PlaySoundOnDisconnected = PlaySoundOnDisconnected;
            settings.PlaySoundOnLowBattery = PlaySoundOnLowBattery;
            settings.PlaySoundOnWarning = PlaySoundOnWarning;
            settings.NotificationSoundVolume = NotificationSoundVolume;
            settings.NotificationWidth = NotificationWidth;
            settings.NotificationScalePercent = NotificationScalePercent;
            settings.NotificationDurationMilliseconds = NotificationDurationMilliseconds;
            settings.NotificationPosition = NotificationPosition;
            settings.NotificationBackgroundColor = NotificationBackgroundColor;
            if (Version >= 5)
            {
                settings.NotificationUseBackgroundImage = NotificationUseBackgroundImage;
                settings.NotificationBackgroundImageStretch = NotificationBackgroundImageStretch;
                settings.NotificationBackgroundImageHorizontalAlignment = NotificationBackgroundImageHorizontalAlignment;
                settings.NotificationBackgroundImageVerticalAlignment = NotificationBackgroundImageVerticalAlignment;
                settings.NotificationBackgroundImageOpacity = NotificationBackgroundImageOpacity;
                settings.NotificationBackgroundImageTintOpacity = NotificationBackgroundImageTintOpacity;
                settings.NotificationBackgroundImagePath = RestoreImage(
                    NotificationBackgroundImageData, NotificationBackgroundImageExtension,
                    imageDirectory, "fullscreen", settings.NotificationBackgroundImagePath);
            }
            settings.NotificationTextColor = NotificationTextColor;
            settings.NotificationSecondaryTextColor = NotificationSecondaryTextColor;
            settings.NotificationConnectedColor = NotificationConnectedColor;
            settings.NotificationDisconnectedColor = NotificationDisconnectedColor;
            settings.NotificationWarningColor = NotificationWarningColor;
            settings.NotificationLowBatteryColor = NotificationLowBatteryColor;
            settings.NotificationTitleFontSize = NotificationTitleFontSize;
            settings.NotificationMessageFontSize = NotificationMessageFontSize;
            settings.NotificationIconSize = NotificationIconSize;
            settings.NotificationIconPosition = NotificationIconPosition;
            settings.NotificationPadding = NotificationPadding;
            settings.NotificationElementSpacing = NotificationElementSpacing;
            settings.NotificationShowBorder = NotificationShowBorder;
            settings.NotificationBorderPosition = NotificationBorderPosition;
            settings.NotificationBorderThickness = NotificationBorderThickness;
            settings.NotificationCornerRadius = NotificationCornerRadius;
            settings.NotificationShowConnectionBadge = NotificationShowConnectionBadge;
            settings.NotificationScreenMargin = NotificationScreenMargin;
            settings.NotificationShowShadow = NotificationShowShadow;
            settings.NotificationFontFamily = NotificationFontFamily;
            settings.NotificationFontWeight = NotificationFontWeight;
            settings.NotificationTextAlignment = NotificationTextAlignment;
            settings.NotificationAccentMode = NotificationAccentMode;
            settings.NotificationAnimation = NotificationAnimation;
            settings.NotificationShowTitle = Version < 2 || NotificationShowTitle;
            settings.DesktopNotificationWidth = DesktopNotificationWidth;
            settings.DesktopNotificationScalePercent = DesktopNotificationScalePercent;
            settings.DesktopNotificationDurationMilliseconds = DesktopNotificationDurationMilliseconds;
            settings.DesktopNotificationPosition = DesktopNotificationPosition;
            settings.DesktopNotificationBackgroundColor = DesktopNotificationBackgroundColor;
            if (Version >= 5)
            {
                settings.DesktopNotificationUseBackgroundImage = DesktopNotificationUseBackgroundImage;
                settings.DesktopNotificationBackgroundImageStretch = DesktopNotificationBackgroundImageStretch;
                settings.DesktopNotificationBackgroundImageHorizontalAlignment = DesktopNotificationBackgroundImageHorizontalAlignment;
                settings.DesktopNotificationBackgroundImageVerticalAlignment = DesktopNotificationBackgroundImageVerticalAlignment;
                settings.DesktopNotificationBackgroundImageOpacity = DesktopNotificationBackgroundImageOpacity;
                settings.DesktopNotificationBackgroundImageTintOpacity = DesktopNotificationBackgroundImageTintOpacity;
                settings.DesktopNotificationBackgroundImagePath = RestoreImage(
                    DesktopNotificationBackgroundImageData, DesktopNotificationBackgroundImageExtension,
                    imageDirectory, "desktop", settings.DesktopNotificationBackgroundImagePath);
            }
            settings.DesktopNotificationTextColor = DesktopNotificationTextColor;
            settings.DesktopNotificationSecondaryTextColor = DesktopNotificationSecondaryTextColor;
            settings.DesktopNotificationConnectedColor = DesktopNotificationConnectedColor;
            settings.DesktopNotificationDisconnectedColor = DesktopNotificationDisconnectedColor;
            settings.DesktopNotificationWarningColor = DesktopNotificationWarningColor;
            settings.DesktopNotificationLowBatteryColor = DesktopNotificationLowBatteryColor;
            settings.DesktopNotificationTitleFontSize = DesktopNotificationTitleFontSize;
            settings.DesktopNotificationMessageFontSize = DesktopNotificationMessageFontSize;
            settings.DesktopNotificationIconSize = DesktopNotificationIconSize;
            settings.DesktopNotificationIconPosition = DesktopNotificationIconPosition;
            settings.DesktopNotificationPadding = DesktopNotificationPadding;
            settings.DesktopNotificationElementSpacing = DesktopNotificationElementSpacing;
            settings.DesktopNotificationShowBorder = DesktopNotificationShowBorder;
            settings.DesktopNotificationBorderPosition = DesktopNotificationBorderPosition;
            settings.DesktopNotificationBorderThickness = DesktopNotificationBorderThickness;
            settings.DesktopNotificationCornerRadius = DesktopNotificationCornerRadius;
            settings.DesktopNotificationShowConnectionBadge = DesktopNotificationShowConnectionBadge;
            settings.DesktopNotificationScreenMargin = DesktopNotificationScreenMargin;
            settings.DesktopNotificationShowShadow = DesktopNotificationShowShadow;
            settings.DesktopNotificationFontFamily = DesktopNotificationFontFamily;
            settings.DesktopNotificationFontWeight = DesktopNotificationFontWeight;
            settings.DesktopNotificationTextAlignment = DesktopNotificationTextAlignment;
            settings.DesktopNotificationAccentMode = DesktopNotificationAccentMode;
            settings.DesktopNotificationAnimation = DesktopNotificationAnimation;
            settings.DesktopNotificationShowTitle = Version < 2 || DesktopNotificationShowTitle;
            settings.OverlayScalePercent = OverlayScalePercent;
            settings.OverlayDimColor = OverlayDimColor;
            settings.OverlayCardColor = OverlayCardColor;
            settings.OverlayAccentColor = OverlayAccentColor;
            settings.OverlayTextColor = OverlayTextColor;
            settings.OverlayWarningColor = OverlayWarningColor;
            settings.OverlayTitleFontSize = OverlayTitleFontSize;
            settings.OverlayControllerFontSize = OverlayControllerFontSize;
            settings.OverlayInstructionFontSize = OverlayInstructionFontSize;
            settings.OverlayStatusFontSize = OverlayStatusFontSize;
            settings.OverlayControllerIconSize = OverlayControllerIconSize;
            settings.OverlayStatusIconSize = OverlayStatusIconSize;
            settings.OverlayShowControllerIcon = OverlayShowControllerIcon;
            settings.OverlayShowStatusIcon = OverlayShowStatusIcon;
            settings.OverlayShowControllerName = OverlayShowControllerName;
            settings.OverlayShowConnectionBadge = Version < 3 || OverlayShowConnectionBadge;
            settings.OverlayShowBatteryBadge = Version < 3 || OverlayShowBatteryBadge;
            settings.OverlayShowTitle = Version < 3 || OverlayShowTitle;
            settings.OverlayShowInstruction = Version < 3 || OverlayShowInstruction;
            settings.OverlayShowPauseStatus = Version < 3 || OverlayShowPauseStatus;
            settings.OverlayControllerIconPosition = OverlayControllerIconPosition;
            settings.OverlayCardPosition = Version < 3 || string.IsNullOrWhiteSpace(OverlayCardPosition)
                ? "Center" : OverlayCardPosition;
            settings.OverlayAnimation = Version < 3 || string.IsNullOrWhiteSpace(OverlayAnimation)
                ? "FadeScale" : OverlayAnimation;
            settings.OverlayBorderPosition = Version < 3 || string.IsNullOrWhiteSpace(OverlayBorderPosition)
                ? "Full" : OverlayBorderPosition;
            settings.OverlayCardWidth = Version < 3 || OverlayCardWidth <= 0 ? 620 : OverlayCardWidth;
            settings.OverlayPadding = OverlayPadding;
            settings.OverlayElementSpacing = OverlayElementSpacing;
            settings.OverlayShowBorder = OverlayShowBorder;
            settings.OverlayShowShadow = Version < 3 || OverlayShowShadow;
            settings.OverlayBorderThickness = OverlayBorderThickness;
            settings.OverlayCornerRadius = OverlayCornerRadius;
            settings.OverlayFontFamily = OverlayFontFamily;
            settings.OverlayFontWeight = OverlayFontWeight;
            settings.OverlayTitleFontFamily = Version < 4 ? OverlayFontFamily : OverlayTitleFontFamily;
            settings.OverlayTitleFontWeight = Version < 4 ? OverlayFontWeight : OverlayTitleFontWeight;
            settings.OverlayControllerFontFamily = Version < 4 ? OverlayFontFamily : OverlayControllerFontFamily;
            settings.OverlayControllerFontWeight = Version < 4 ? OverlayFontWeight : OverlayControllerFontWeight;
            settings.OverlayInstructionFontFamily = Version < 4 ? OverlayFontFamily : OverlayInstructionFontFamily;
            settings.OverlayInstructionFontWeight = Version < 4 ? OverlayFontWeight : OverlayInstructionFontWeight;
            settings.OverlayStatusFontFamily = Version < 4 ? OverlayFontFamily : OverlayStatusFontFamily;
            settings.OverlayStatusFontWeight = Version < 4 ? OverlayFontWeight : OverlayStatusFontWeight;
            if (Version >= 4)
            {
                settings.OverlayConnectionBadgeTextColor = OverlayConnectionBadgeTextColor;
                settings.OverlayConnectionBadgeIconColor = OverlayConnectionBadgeIconColor;
                settings.OverlayConnectionBadgeBackgroundColor = OverlayConnectionBadgeBackgroundColor;
                settings.OverlayConnectionBadgeBorderColor = OverlayConnectionBadgeBorderColor;
                settings.OverlayConnectionBadgeBorderThickness = OverlayConnectionBadgeBorderThickness;
                settings.OverlayConnectionBadgeCornerRadius = OverlayConnectionBadgeCornerRadius;
                settings.OverlayConnectionBadgeIconSize = OverlayConnectionBadgeIconSize;
                settings.OverlayConnectionBadgeTextSize = OverlayConnectionBadgeTextSize;
                settings.OverlayBatteryBadgeTextColor = OverlayBatteryBadgeTextColor;
                settings.OverlayBatteryBadgeIconColor = OverlayBatteryBadgeIconColor;
                settings.OverlayBatteryBadgeBackgroundColor = OverlayBatteryBadgeBackgroundColor;
                settings.OverlayBatteryBadgeBorderColor = OverlayBatteryBadgeBorderColor;
                settings.OverlayBatteryBadgeBorderThickness = OverlayBatteryBadgeBorderThickness;
                settings.OverlayBatteryBadgeCornerRadius = OverlayBatteryBadgeCornerRadius;
                settings.OverlayBatteryBadgeIconSize = OverlayBatteryBadgeIconSize;
                settings.OverlayBatteryBadgeTextSize = OverlayBatteryBadgeTextSize;
                settings.OverlayBatteryBadgeUseStateColors = OverlayBatteryBadgeUseStateColors;
                settings.OverlayBatteryBadgeFullColor = OverlayBatteryBadgeFullColor;
                settings.OverlayBatteryBadgeMediumColor = OverlayBatteryBadgeMediumColor;
                settings.OverlayBatteryBadgeLowColor = OverlayBatteryBadgeLowColor;
                settings.OverlayBatteryBadgeEmptyColor = OverlayBatteryBadgeEmptyColor;
            }
        }

        private static string ReadImageData(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
                var bytes = File.ReadAllBytes(path);
                return bytes.Length == 0 || bytes.Length > 10 * 1024 * 1024
                    ? null : Convert.ToBase64String(bytes);
            }
            catch
            {
                return null;
            }
        }

        private static string ImageExtension(string path)
        {
            return string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase)
                ? ".png" : ".jpg";
        }

        private static string RestoreImage(string data, string extension, string directory,
            string prefix, string fallback)
        {
            if (string.IsNullOrWhiteSpace(data)) return string.Empty;
            if (string.IsNullOrWhiteSpace(directory)) return fallback ?? string.Empty;
            try
            {
                var bytes = Convert.FromBase64String(data);
                if (bytes.Length == 0 || bytes.Length > 10 * 1024 * 1024) return string.Empty;
                Directory.CreateDirectory(directory);
                var safeExtension = string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                    ? ".png" : ".jpg";
                var path = Path.Combine(directory, prefix + "-" + Guid.NewGuid().ToString("N") + safeExtension);
                File.WriteAllBytes(path, bytes);
                return path;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
