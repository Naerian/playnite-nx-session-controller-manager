using System;
using System.IO;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>
    /// Portable bundle of notification, overlay and sound appearance settings.
    /// </summary>
    public sealed class VisualProfileSnapshot
    {
        public const int CurrentVersion = 13;
        public const string FileExtension = ".pcvisual";

        public int Version { get; set; }
        public string Name { get; set; }
        public string ExportedUtc { get; set; }

        public string NotificationStylePreset { get; set; }
        public string DesktopNotificationStylePreset { get; set; }
        public string OverlayStylePreset { get; set; }

        public bool EnableNotificationSounds { get; set; }
        public bool EnableDesktopNotificationSounds { get; set; }
        public bool EnableFullscreenNotificationSounds { get; set; }
        public string NotificationSoundPack { get; set; }
        public bool PlaySoundOnConnected { get; set; }
        public bool PlaySoundOnDisconnected { get; set; }
        public bool PlaySoundOnLowBattery { get; set; }
        public bool PlaySoundOnWarning { get; set; }
        public double NotificationSoundVolume { get; set; }
        public string CustomConnectedSoundData { get; set; }
        public string CustomConnectedSoundExtension { get; set; }
        public string CustomDisconnectedSoundData { get; set; }
        public string CustomDisconnectedSoundExtension { get; set; }
        public string CustomLowBatterySoundData { get; set; }
        public string CustomLowBatterySoundExtension { get; set; }
        public string CustomWarningSoundData { get; set; }
        public string CustomWarningSoundExtension { get; set; }

        public int NotificationWidth { get; set; }
        public int NotificationScalePercent { get; set; }
        public int NotificationDurationMilliseconds { get; set; }
        public string NotificationPosition { get; set; }
        public string NotificationBackgroundColor { get; set; }
        public bool NotificationUseGradient { get; set; }
        public string NotificationGradientColor { get; set; }
        public int NotificationGradientAngle { get; set; }
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
        public bool NotificationShowIconContainer { get; set; }
        public string NotificationIconContainerColor { get; set; }
        public string NotificationIconContainerBorderColor { get; set; }
        public int NotificationIconContainerBorderThickness { get; set; }
        public int NotificationIconContainerCornerRadius { get; set; }
        public int NotificationIconContainerPadding { get; set; }
        public string NotificationIconPosition { get; set; }
        public int NotificationPadding { get; set; }
        public int NotificationElementSpacing { get; set; }
        public int NotificationIconSpacing { get; set; }
        public bool NotificationShowBorder { get; set; }
        public string NotificationBorderPosition { get; set; }
        public int NotificationBorderThickness { get; set; }
        public bool NotificationUseBorderGradient { get; set; }
        public bool NotificationUseStateBorderColors { get; set; }
        public string NotificationConnectedBorderColor { get; set; }
        public string NotificationDisconnectedBorderColor { get; set; }
        public string NotificationWarningBorderColor { get; set; }
        public string NotificationLowBatteryBorderColor { get; set; }
        public string NotificationBorderGradientStartColor { get; set; }
        public string NotificationBorderGradientEndColor { get; set; }
        public int NotificationBorderGradientAngle { get; set; }
        public bool NotificationShowBorderGlow { get; set; }
        public string NotificationBorderGlowColor { get; set; }
        public int NotificationBorderGlowBlur { get; set; }
        public int NotificationBorderGlowOpacity { get; set; }
        public int NotificationCornerRadius { get; set; }
        public bool NotificationShowConnectionBadge { get; set; }
        public int NotificationScreenMargin { get; set; }
        public bool NotificationShowShadow { get; set; }
        public string NotificationFontFamily { get; set; }
        public string NotificationFontWeight { get; set; }
        public string NotificationTitleFontFamily { get; set; }
        public string NotificationTitleFontWeight { get; set; }
        public string NotificationMessageFontFamily { get; set; }
        public string NotificationMessageFontWeight { get; set; }
        public int NotificationMessageMaxLines { get; set; }
        public string NotificationBadgePosition { get; set; }
        public string NotificationTextAlignment { get; set; }
        public string NotificationAccentMode { get; set; }
        public string NotificationAnimation { get; set; }
        public bool NotificationShowTitle { get; set; }
        public bool NotificationUppercaseTitle { get; set; }
        public string NotificationTextOrder { get; set; }
        public bool NotificationUseIndependentBorders { get; set; }
        public int NotificationBorderLeftThickness { get; set; }
        public int NotificationBorderTopThickness { get; set; }
        public int NotificationBorderRightThickness { get; set; }
        public int NotificationBorderBottomThickness { get; set; }
        public bool NotificationUseStateBackgroundColors { get; set; }
        public string NotificationConnectedBackgroundColor { get; set; }
        public string NotificationDisconnectedBackgroundColor { get; set; }
        public string NotificationWarningBackgroundColor { get; set; }
        public string NotificationLowBatteryBackgroundColor { get; set; }

        public int DesktopNotificationWidth { get; set; }
        public int DesktopNotificationScalePercent { get; set; }
        public int DesktopNotificationDurationMilliseconds { get; set; }
        public string DesktopNotificationPosition { get; set; }
        public string DesktopNotificationBackgroundColor { get; set; }
        public bool DesktopNotificationUseGradient { get; set; }
        public string DesktopNotificationGradientColor { get; set; }
        public int DesktopNotificationGradientAngle { get; set; }
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
        public bool DesktopNotificationShowIconContainer { get; set; }
        public string DesktopNotificationIconContainerColor { get; set; }
        public string DesktopNotificationIconContainerBorderColor { get; set; }
        public int DesktopNotificationIconContainerBorderThickness { get; set; }
        public int DesktopNotificationIconContainerCornerRadius { get; set; }
        public int DesktopNotificationIconContainerPadding { get; set; }
        public string DesktopNotificationIconPosition { get; set; }
        public int DesktopNotificationPadding { get; set; }
        public int DesktopNotificationElementSpacing { get; set; }
        public int DesktopNotificationIconSpacing { get; set; }
        public bool DesktopNotificationShowBorder { get; set; }
        public string DesktopNotificationBorderPosition { get; set; }
        public int DesktopNotificationBorderThickness { get; set; }
        public bool DesktopNotificationUseBorderGradient { get; set; }
        public bool DesktopNotificationUseStateBorderColors { get; set; }
        public string DesktopNotificationConnectedBorderColor { get; set; }
        public string DesktopNotificationDisconnectedBorderColor { get; set; }
        public string DesktopNotificationWarningBorderColor { get; set; }
        public string DesktopNotificationLowBatteryBorderColor { get; set; }
        public string DesktopNotificationBorderGradientStartColor { get; set; }
        public string DesktopNotificationBorderGradientEndColor { get; set; }
        public int DesktopNotificationBorderGradientAngle { get; set; }
        public bool DesktopNotificationShowBorderGlow { get; set; }
        public string DesktopNotificationBorderGlowColor { get; set; }
        public int DesktopNotificationBorderGlowBlur { get; set; }
        public int DesktopNotificationBorderGlowOpacity { get; set; }
        public int DesktopNotificationCornerRadius { get; set; }
        public bool DesktopNotificationShowConnectionBadge { get; set; }
        public int DesktopNotificationScreenMargin { get; set; }
        public bool DesktopNotificationShowShadow { get; set; }
        public string DesktopNotificationFontFamily { get; set; }
        public string DesktopNotificationFontWeight { get; set; }
        public string DesktopNotificationTitleFontFamily { get; set; }
        public string DesktopNotificationTitleFontWeight { get; set; }
        public string DesktopNotificationMessageFontFamily { get; set; }
        public string DesktopNotificationMessageFontWeight { get; set; }
        public int DesktopNotificationMessageMaxLines { get; set; }
        public string DesktopNotificationBadgePosition { get; set; }
        public string DesktopNotificationTextAlignment { get; set; }
        public string DesktopNotificationAccentMode { get; set; }
        public string DesktopNotificationAnimation { get; set; }
        public bool DesktopNotificationShowTitle { get; set; }
        public bool DesktopNotificationUppercaseTitle { get; set; }
        public string DesktopNotificationTextOrder { get; set; }
        public bool DesktopNotificationUseIndependentBorders { get; set; }
        public int DesktopNotificationBorderLeftThickness { get; set; }
        public int DesktopNotificationBorderTopThickness { get; set; }
        public int DesktopNotificationBorderRightThickness { get; set; }
        public int DesktopNotificationBorderBottomThickness { get; set; }
        public bool DesktopNotificationUseStateBackgroundColors { get; set; }
        public string DesktopNotificationConnectedBackgroundColor { get; set; }
        public string DesktopNotificationDisconnectedBackgroundColor { get; set; }
        public string DesktopNotificationWarningBackgroundColor { get; set; }
        public string DesktopNotificationLowBatteryBackgroundColor { get; set; }

        public int OverlayScalePercent { get; set; }
        public string OverlayDimColor { get; set; }
        public string OverlayCardColor { get; set; }
        public bool OverlayUseGradient { get; set; }
        public string OverlayGradientColor { get; set; }
        public int OverlayGradientAngle { get; set; }
        public bool OverlayUseBackgroundImage { get; set; }
        public string OverlayBackgroundImageStretch { get; set; }
        public string OverlayBackgroundImageHorizontalAlignment { get; set; }
        public string OverlayBackgroundImageVerticalAlignment { get; set; }
        public int OverlayBackgroundImageOpacity { get; set; }
        public int OverlayBackgroundImageTintOpacity { get; set; }
        public string OverlayBackgroundImageData { get; set; }
        public string OverlayBackgroundImageExtension { get; set; }
        public string OverlayAccentColor { get; set; }
        public string OverlayTextColor { get; set; }
        public string OverlayWarningColor { get; set; }
        public int OverlayTitleFontSize { get; set; }
        public int OverlayControllerFontSize { get; set; }
        public int OverlayInstructionFontSize { get; set; }
        public int OverlayStatusFontSize { get; set; }
        public int OverlayControllerIconSize { get; set; }
        public bool OverlayShowControllerContainer { get; set; }
        public string OverlayControllerContainerColor { get; set; }
        public string OverlayControllerContainerBorderColor { get; set; }
        public int OverlayControllerContainerBorderThickness { get; set; }
        public int OverlayControllerContainerCornerRadius { get; set; }
        public int OverlayControllerContainerPadding { get; set; }
        public int OverlayStatusIconSize { get; set; }
        public bool OverlayShowControllerIcon { get; set; }
        public bool OverlayShowStatusIcon { get; set; }
        public bool OverlayShowControllerName { get; set; }
        public bool OverlayShowConnectionBadge { get; set; }
        public bool OverlayShowBatteryBadge { get; set; }
        public bool OverlayShowTitle { get; set; }
        public bool OverlayUppercaseTitle { get; set; }
        public bool OverlayShowInstruction { get; set; }
        public bool OverlayShowPauseStatus { get; set; }
        public string OverlayControllerIconPosition { get; set; }
        public string OverlayCardPosition { get; set; }
        public string OverlayLayoutMode { get; set; }
        public string OverlayContentAlignment { get; set; }
        public int OverlayScreenMargin { get; set; }
        public string OverlayAnimation { get; set; }
        public string OverlayBorderPosition { get; set; }
        public int OverlayCardWidth { get; set; }
        public int OverlayPadding { get; set; }
        public int OverlayElementSpacing { get; set; }
        public bool OverlayShowBorder { get; set; }
        public bool OverlayShowShadow { get; set; }
        public int OverlayBorderThickness { get; set; }
        public bool OverlayUseBorderGradient { get; set; }
        public string OverlayBorderGradientStartColor { get; set; }
        public string OverlayBorderGradientEndColor { get; set; }
        public int OverlayBorderGradientAngle { get; set; }
        public bool OverlayShowBorderGlow { get; set; }
        public string OverlayBorderGlowColor { get; set; }
        public int OverlayBorderGlowBlur { get; set; }
        public int OverlayBorderGlowOpacity { get; set; }
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
        public string OverlayBlockOrder { get; set; }
        public string OverlayMetadataOrientation { get; set; }
        public bool OverlayUseIndependentBorders { get; set; }
        public int OverlayBorderLeftThickness { get; set; }
        public int OverlayBorderTopThickness { get; set; }
        public int OverlayBorderRightThickness { get; set; }
        public int OverlayBorderBottomThickness { get; set; }

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
                DesktopNotificationStylePreset = settings.DesktopNotificationStylePreset,
                OverlayStylePreset = settings.OverlayStylePreset,
                EnableNotificationSounds = settings.EnableNotificationSounds,
                EnableDesktopNotificationSounds = settings.EnableDesktopNotificationSounds,
                EnableFullscreenNotificationSounds = settings.EnableFullscreenNotificationSounds,
                NotificationSoundPack = settings.NotificationSoundPack,
                PlaySoundOnConnected = settings.PlaySoundOnConnected,
                PlaySoundOnDisconnected = settings.PlaySoundOnDisconnected,
                PlaySoundOnLowBattery = settings.PlaySoundOnLowBattery,
                PlaySoundOnWarning = settings.PlaySoundOnWarning,
                NotificationSoundVolume = settings.NotificationSoundVolume,
                CustomConnectedSoundData = ReadSoundData(settings.CustomConnectedSoundPath),
                CustomConnectedSoundExtension = SoundExtension(settings.CustomConnectedSoundPath),
                CustomDisconnectedSoundData = ReadSoundData(settings.CustomDisconnectedSoundPath),
                CustomDisconnectedSoundExtension = SoundExtension(settings.CustomDisconnectedSoundPath),
                CustomLowBatterySoundData = ReadSoundData(settings.CustomLowBatterySoundPath),
                CustomLowBatterySoundExtension = SoundExtension(settings.CustomLowBatterySoundPath),
                CustomWarningSoundData = ReadSoundData(settings.CustomWarningSoundPath),
                CustomWarningSoundExtension = SoundExtension(settings.CustomWarningSoundPath),
                NotificationWidth = settings.NotificationWidth,
                NotificationScalePercent = settings.NotificationScalePercent,
                NotificationDurationMilliseconds = settings.NotificationDurationMilliseconds,
                NotificationPosition = settings.NotificationPosition,
                NotificationBackgroundColor = settings.NotificationBackgroundColor,
                NotificationUseGradient = settings.NotificationUseGradient,
                NotificationGradientColor = settings.NotificationGradientColor,
                NotificationGradientAngle = settings.NotificationGradientAngle,
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
                NotificationShowIconContainer = settings.NotificationShowIconContainer,
                NotificationIconContainerColor = settings.NotificationIconContainerColor,
                NotificationIconContainerBorderColor = settings.NotificationIconContainerBorderColor,
                NotificationIconContainerBorderThickness = settings.NotificationIconContainerBorderThickness,
                NotificationIconContainerCornerRadius = settings.NotificationIconContainerCornerRadius,
                NotificationIconContainerPadding = settings.NotificationIconContainerPadding,
                NotificationIconPosition = settings.NotificationIconPosition,
                NotificationPadding = settings.NotificationPadding,
                NotificationElementSpacing = settings.NotificationElementSpacing,
                NotificationIconSpacing = settings.NotificationIconSpacing,
                NotificationShowBorder = settings.NotificationShowBorder,
                NotificationBorderPosition = settings.NotificationBorderPosition,
                NotificationBorderThickness = settings.NotificationBorderThickness,
                NotificationUseBorderGradient = settings.NotificationUseBorderGradient,
                NotificationUseStateBorderColors = settings.NotificationUseStateBorderColors,
                NotificationConnectedBorderColor = settings.NotificationConnectedBorderColor,
                NotificationDisconnectedBorderColor = settings.NotificationDisconnectedBorderColor,
                NotificationWarningBorderColor = settings.NotificationWarningBorderColor,
                NotificationLowBatteryBorderColor = settings.NotificationLowBatteryBorderColor,
                NotificationBorderGradientStartColor = settings.NotificationBorderGradientStartColor,
                NotificationBorderGradientEndColor = settings.NotificationBorderGradientEndColor,
                NotificationBorderGradientAngle = settings.NotificationBorderGradientAngle,
                NotificationShowBorderGlow = settings.NotificationShowBorderGlow,
                NotificationBorderGlowColor = settings.NotificationBorderGlowColor,
                NotificationBorderGlowBlur = settings.NotificationBorderGlowBlur,
                NotificationBorderGlowOpacity = settings.NotificationBorderGlowOpacity,
                NotificationCornerRadius = settings.NotificationCornerRadius,
                NotificationShowConnectionBadge = settings.NotificationShowConnectionBadge,
                NotificationScreenMargin = settings.NotificationScreenMargin,
                NotificationShowShadow = settings.NotificationShowShadow,
                NotificationFontFamily = settings.NotificationFontFamily,
                NotificationFontWeight = settings.NotificationFontWeight,
                NotificationTitleFontFamily = settings.NotificationTitleFontFamily,
                NotificationTitleFontWeight = settings.NotificationTitleFontWeight,
                NotificationMessageFontFamily = settings.NotificationMessageFontFamily,
                NotificationMessageFontWeight = settings.NotificationMessageFontWeight,
                NotificationMessageMaxLines = settings.NotificationMessageMaxLines,
                NotificationBadgePosition = settings.NotificationBadgePosition,
                NotificationTextAlignment = settings.NotificationTextAlignment,
                NotificationAccentMode = settings.NotificationAccentMode,
                NotificationAnimation = settings.NotificationAnimation,
                NotificationShowTitle = settings.NotificationShowTitle,
                NotificationUppercaseTitle = settings.NotificationUppercaseTitle,
                NotificationTextOrder = settings.NotificationTextOrder,
                NotificationUseIndependentBorders = settings.NotificationUseIndependentBorders,
                NotificationBorderLeftThickness = settings.NotificationBorderLeftThickness,
                NotificationBorderTopThickness = settings.NotificationBorderTopThickness,
                NotificationBorderRightThickness = settings.NotificationBorderRightThickness,
                NotificationBorderBottomThickness = settings.NotificationBorderBottomThickness,
                NotificationUseStateBackgroundColors = settings.NotificationUseStateBackgroundColors,
                NotificationConnectedBackgroundColor = settings.NotificationConnectedBackgroundColor,
                NotificationDisconnectedBackgroundColor = settings.NotificationDisconnectedBackgroundColor,
                NotificationWarningBackgroundColor = settings.NotificationWarningBackgroundColor,
                NotificationLowBatteryBackgroundColor = settings.NotificationLowBatteryBackgroundColor,
                DesktopNotificationWidth = settings.DesktopNotificationWidth,
                DesktopNotificationScalePercent = settings.DesktopNotificationScalePercent,
                DesktopNotificationDurationMilliseconds = settings.DesktopNotificationDurationMilliseconds,
                DesktopNotificationPosition = settings.DesktopNotificationPosition,
                DesktopNotificationBackgroundColor = settings.DesktopNotificationBackgroundColor,
                DesktopNotificationUseGradient = settings.DesktopNotificationUseGradient,
                DesktopNotificationGradientColor = settings.DesktopNotificationGradientColor,
                DesktopNotificationGradientAngle = settings.DesktopNotificationGradientAngle,
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
                DesktopNotificationShowIconContainer = settings.DesktopNotificationShowIconContainer,
                DesktopNotificationIconContainerColor = settings.DesktopNotificationIconContainerColor,
                DesktopNotificationIconContainerBorderColor = settings.DesktopNotificationIconContainerBorderColor,
                DesktopNotificationIconContainerBorderThickness = settings.DesktopNotificationIconContainerBorderThickness,
                DesktopNotificationIconContainerCornerRadius = settings.DesktopNotificationIconContainerCornerRadius,
                DesktopNotificationIconContainerPadding = settings.DesktopNotificationIconContainerPadding,
                DesktopNotificationIconPosition = settings.DesktopNotificationIconPosition,
                DesktopNotificationPadding = settings.DesktopNotificationPadding,
                DesktopNotificationElementSpacing = settings.DesktopNotificationElementSpacing,
                DesktopNotificationIconSpacing = settings.DesktopNotificationIconSpacing,
                DesktopNotificationShowBorder = settings.DesktopNotificationShowBorder,
                DesktopNotificationBorderPosition = settings.DesktopNotificationBorderPosition,
                DesktopNotificationBorderThickness = settings.DesktopNotificationBorderThickness,
                DesktopNotificationUseBorderGradient = settings.DesktopNotificationUseBorderGradient,
                DesktopNotificationUseStateBorderColors = settings.DesktopNotificationUseStateBorderColors,
                DesktopNotificationConnectedBorderColor = settings.DesktopNotificationConnectedBorderColor,
                DesktopNotificationDisconnectedBorderColor = settings.DesktopNotificationDisconnectedBorderColor,
                DesktopNotificationWarningBorderColor = settings.DesktopNotificationWarningBorderColor,
                DesktopNotificationLowBatteryBorderColor = settings.DesktopNotificationLowBatteryBorderColor,
                DesktopNotificationBorderGradientStartColor = settings.DesktopNotificationBorderGradientStartColor,
                DesktopNotificationBorderGradientEndColor = settings.DesktopNotificationBorderGradientEndColor,
                DesktopNotificationBorderGradientAngle = settings.DesktopNotificationBorderGradientAngle,
                DesktopNotificationShowBorderGlow = settings.DesktopNotificationShowBorderGlow,
                DesktopNotificationBorderGlowColor = settings.DesktopNotificationBorderGlowColor,
                DesktopNotificationBorderGlowBlur = settings.DesktopNotificationBorderGlowBlur,
                DesktopNotificationBorderGlowOpacity = settings.DesktopNotificationBorderGlowOpacity,
                DesktopNotificationCornerRadius = settings.DesktopNotificationCornerRadius,
                DesktopNotificationShowConnectionBadge = settings.DesktopNotificationShowConnectionBadge,
                DesktopNotificationScreenMargin = settings.DesktopNotificationScreenMargin,
                DesktopNotificationShowShadow = settings.DesktopNotificationShowShadow,
                DesktopNotificationFontFamily = settings.DesktopNotificationFontFamily,
                DesktopNotificationFontWeight = settings.DesktopNotificationFontWeight,
                DesktopNotificationTitleFontFamily = settings.DesktopNotificationTitleFontFamily,
                DesktopNotificationTitleFontWeight = settings.DesktopNotificationTitleFontWeight,
                DesktopNotificationMessageFontFamily = settings.DesktopNotificationMessageFontFamily,
                DesktopNotificationMessageFontWeight = settings.DesktopNotificationMessageFontWeight,
                DesktopNotificationMessageMaxLines = settings.DesktopNotificationMessageMaxLines,
                DesktopNotificationBadgePosition = settings.DesktopNotificationBadgePosition,
                DesktopNotificationTextAlignment = settings.DesktopNotificationTextAlignment,
                DesktopNotificationAccentMode = settings.DesktopNotificationAccentMode,
                DesktopNotificationAnimation = settings.DesktopNotificationAnimation,
                DesktopNotificationShowTitle = settings.DesktopNotificationShowTitle,
                DesktopNotificationUppercaseTitle = settings.DesktopNotificationUppercaseTitle,
                DesktopNotificationTextOrder = settings.DesktopNotificationTextOrder,
                DesktopNotificationUseIndependentBorders = settings.DesktopNotificationUseIndependentBorders,
                DesktopNotificationBorderLeftThickness = settings.DesktopNotificationBorderLeftThickness,
                DesktopNotificationBorderTopThickness = settings.DesktopNotificationBorderTopThickness,
                DesktopNotificationBorderRightThickness = settings.DesktopNotificationBorderRightThickness,
                DesktopNotificationBorderBottomThickness = settings.DesktopNotificationBorderBottomThickness,
                DesktopNotificationUseStateBackgroundColors = settings.DesktopNotificationUseStateBackgroundColors,
                DesktopNotificationConnectedBackgroundColor = settings.DesktopNotificationConnectedBackgroundColor,
                DesktopNotificationDisconnectedBackgroundColor = settings.DesktopNotificationDisconnectedBackgroundColor,
                DesktopNotificationWarningBackgroundColor = settings.DesktopNotificationWarningBackgroundColor,
                DesktopNotificationLowBatteryBackgroundColor = settings.DesktopNotificationLowBatteryBackgroundColor,
                OverlayScalePercent = settings.OverlayScalePercent,
                OverlayDimColor = settings.OverlayDimColor,
                OverlayCardColor = settings.OverlayCardColor,
                OverlayUseGradient = settings.OverlayUseGradient,
                OverlayGradientColor = settings.OverlayGradientColor,
                OverlayGradientAngle = settings.OverlayGradientAngle,
                OverlayUseBackgroundImage = settings.OverlayUseBackgroundImage,
                OverlayBackgroundImageStretch = settings.OverlayBackgroundImageStretch,
                OverlayBackgroundImageHorizontalAlignment = settings.OverlayBackgroundImageHorizontalAlignment,
                OverlayBackgroundImageVerticalAlignment = settings.OverlayBackgroundImageVerticalAlignment,
                OverlayBackgroundImageOpacity = settings.OverlayBackgroundImageOpacity,
                OverlayBackgroundImageTintOpacity = settings.OverlayBackgroundImageTintOpacity,
                OverlayBackgroundImageData = ReadImageData(settings.OverlayBackgroundImagePath),
                OverlayBackgroundImageExtension = ImageExtension(settings.OverlayBackgroundImagePath),
                OverlayAccentColor = settings.OverlayAccentColor,
                OverlayTextColor = settings.OverlayTextColor,
                OverlayWarningColor = settings.OverlayWarningColor,
                OverlayTitleFontSize = settings.OverlayTitleFontSize,
                OverlayControllerFontSize = settings.OverlayControllerFontSize,
                OverlayInstructionFontSize = settings.OverlayInstructionFontSize,
                OverlayStatusFontSize = settings.OverlayStatusFontSize,
                OverlayControllerIconSize = settings.OverlayControllerIconSize,
                OverlayShowControllerContainer = settings.OverlayShowControllerContainer,
                OverlayControllerContainerColor = settings.OverlayControllerContainerColor,
                OverlayControllerContainerBorderColor = settings.OverlayControllerContainerBorderColor,
                OverlayControllerContainerBorderThickness = settings.OverlayControllerContainerBorderThickness,
                OverlayControllerContainerCornerRadius = settings.OverlayControllerContainerCornerRadius,
                OverlayControllerContainerPadding = settings.OverlayControllerContainerPadding,
                OverlayStatusIconSize = settings.OverlayStatusIconSize,
                OverlayShowControllerIcon = settings.OverlayShowControllerIcon,
                OverlayShowStatusIcon = settings.OverlayShowStatusIcon,
                OverlayShowControllerName = settings.OverlayShowControllerName,
                OverlayShowConnectionBadge = settings.OverlayShowConnectionBadge,
                OverlayShowBatteryBadge = settings.OverlayShowBatteryBadge,
                OverlayShowTitle = settings.OverlayShowTitle,
                OverlayUppercaseTitle = settings.OverlayUppercaseTitle,
                OverlayShowInstruction = settings.OverlayShowInstruction,
                OverlayShowPauseStatus = settings.OverlayShowPauseStatus,
                OverlayControllerIconPosition = settings.OverlayControllerIconPosition,
                OverlayCardPosition = settings.OverlayCardPosition,
                OverlayLayoutMode = settings.OverlayLayoutMode,
                OverlayContentAlignment = settings.OverlayContentAlignment,
                OverlayScreenMargin = settings.OverlayScreenMargin,
                OverlayAnimation = settings.OverlayAnimation,
                OverlayBorderPosition = settings.OverlayBorderPosition,
                OverlayCardWidth = settings.OverlayCardWidth,
                OverlayPadding = settings.OverlayPadding,
                OverlayElementSpacing = settings.OverlayElementSpacing,
                OverlayShowBorder = settings.OverlayShowBorder,
                OverlayShowShadow = settings.OverlayShowShadow,
                OverlayBorderThickness = settings.OverlayBorderThickness,
                OverlayUseBorderGradient = settings.OverlayUseBorderGradient,
                OverlayBorderGradientStartColor = settings.OverlayBorderGradientStartColor,
                OverlayBorderGradientEndColor = settings.OverlayBorderGradientEndColor,
                OverlayBorderGradientAngle = settings.OverlayBorderGradientAngle,
                OverlayShowBorderGlow = settings.OverlayShowBorderGlow,
                OverlayBorderGlowColor = settings.OverlayBorderGlowColor,
                OverlayBorderGlowBlur = settings.OverlayBorderGlowBlur,
                OverlayBorderGlowOpacity = settings.OverlayBorderGlowOpacity,
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
                OverlayBatteryBadgeEmptyColor = settings.OverlayBatteryBadgeEmptyColor,
                OverlayBlockOrder = settings.OverlayBlockOrder,
                OverlayMetadataOrientation = settings.OverlayMetadataOrientation,
                OverlayUseIndependentBorders = settings.OverlayUseIndependentBorders,
                OverlayBorderLeftThickness = settings.OverlayBorderLeftThickness,
                OverlayBorderTopThickness = settings.OverlayBorderTopThickness,
                OverlayBorderRightThickness = settings.OverlayBorderRightThickness,
                OverlayBorderBottomThickness = settings.OverlayBorderBottomThickness
            };
        }

        public void ApplyTo(ControllerSessionManagerSettings settings)
        {
            ApplyTo(settings, null, null);
        }

        public void ApplyTo(ControllerSessionManagerSettings settings, string imageDirectory)
        {
            ApplyTo(settings, imageDirectory, null);
        }

        public void ApplyTo(ControllerSessionManagerSettings settings, string imageDirectory,
            string soundDirectory)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            settings.NotificationStylePreset = NotificationStylePresets.Normalize(NotificationStylePreset);
            settings.DesktopNotificationStylePreset = Version >= 12
                ? NotificationStylePresets.Normalize(DesktopNotificationStylePreset)
                : settings.NotificationStylePreset;
            settings.OverlayStylePreset = OverlayStylePresets.Normalize(OverlayStylePreset);
            settings.EnableNotificationSounds = true;
            settings.NotificationSoundPack = NotificationSoundCatalog.Normalize(NotificationSoundPack);
            settings.PlaySoundOnConnected = PlaySoundOnConnected;
            settings.PlaySoundOnDisconnected = PlaySoundOnDisconnected;
            settings.PlaySoundOnLowBattery = PlaySoundOnLowBattery;
            settings.PlaySoundOnWarning = PlaySoundOnWarning;
            settings.NotificationSoundVolume = NotificationSoundVolume;
            if (Version >= 6)
            {
                settings.EnableDesktopNotificationSounds = EnableNotificationSounds &&
                    EnableDesktopNotificationSounds;
                settings.EnableFullscreenNotificationSounds = EnableNotificationSounds &&
                    EnableFullscreenNotificationSounds;
                settings.CustomConnectedSoundPath = RestoreSound(CustomConnectedSoundData,
                    CustomConnectedSoundExtension, soundDirectory, "connected", settings.CustomConnectedSoundPath);
                settings.CustomDisconnectedSoundPath = RestoreSound(CustomDisconnectedSoundData,
                    CustomDisconnectedSoundExtension, soundDirectory, "disconnected", settings.CustomDisconnectedSoundPath);
                settings.CustomLowBatterySoundPath = RestoreSound(CustomLowBatterySoundData,
                    CustomLowBatterySoundExtension, soundDirectory, "low-battery", settings.CustomLowBatterySoundPath);
                settings.CustomWarningSoundPath = RestoreSound(CustomWarningSoundData,
                    CustomWarningSoundExtension, soundDirectory, "warning", settings.CustomWarningSoundPath);
            }
            else
            {
                settings.EnableDesktopNotificationSounds = EnableNotificationSounds;
                settings.EnableFullscreenNotificationSounds = EnableNotificationSounds;
            }
            settings.NotificationWidth = NotificationWidth;
            settings.NotificationScalePercent = NotificationScalePercent;
            settings.NotificationDurationMilliseconds = NotificationDurationMilliseconds;
            settings.NotificationPosition = NotificationPosition;
            settings.NotificationBackgroundColor = NotificationBackgroundColor;
            settings.NotificationUseGradient = Version >= 8 && NotificationUseGradient;
            settings.NotificationGradientColor = Version < 8 ? NotificationBackgroundColor : NotificationGradientColor;
            settings.NotificationGradientAngle = Version < 8 ? 0 : NotificationGradientAngle;
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
            settings.NotificationShowIconContainer = Version >= 9 && NotificationShowIconContainer;
            if (Version >= 9)
            {
                settings.NotificationIconContainerColor = NotificationIconContainerColor;
                settings.NotificationIconContainerBorderColor = NotificationIconContainerBorderColor;
                settings.NotificationIconContainerBorderThickness = NotificationIconContainerBorderThickness;
                settings.NotificationIconContainerCornerRadius = NotificationIconContainerCornerRadius;
                settings.NotificationIconContainerPadding = NotificationIconContainerPadding;
            }
            settings.NotificationIconPosition = NotificationIconPosition;
            settings.NotificationPadding = NotificationPadding;
            settings.NotificationElementSpacing = NotificationElementSpacing;
            settings.NotificationIconSpacing = Version >= 6
                ? NotificationIconSpacing
                : LegacyIconSpacing(NotificationPadding, NotificationElementSpacing);
            settings.NotificationShowBorder = NotificationShowBorder;
            settings.NotificationBorderPosition = NotificationBorderPosition;
            settings.NotificationBorderThickness = NotificationBorderThickness;
            if (Version >= 11)
            {
                settings.NotificationUseBorderGradient = NotificationUseBorderGradient;
                if (Version >= 13)
                {
                    settings.NotificationUseStateBorderColors = NotificationUseStateBorderColors;
                    settings.NotificationConnectedBorderColor = NotificationConnectedBorderColor;
                    settings.NotificationDisconnectedBorderColor = NotificationDisconnectedBorderColor;
                    settings.NotificationWarningBorderColor = NotificationWarningBorderColor;
                    settings.NotificationLowBatteryBorderColor = NotificationLowBatteryBorderColor;
                }
                settings.NotificationBorderGradientStartColor = NotificationBorderGradientStartColor;
                settings.NotificationBorderGradientEndColor = NotificationBorderGradientEndColor;
                settings.NotificationBorderGradientAngle = NotificationBorderGradientAngle;
                settings.NotificationShowBorderGlow = NotificationShowBorderGlow;
                settings.NotificationBorderGlowColor = NotificationBorderGlowColor;
                settings.NotificationBorderGlowBlur = NotificationBorderGlowBlur;
                settings.NotificationBorderGlowOpacity = NotificationBorderGlowOpacity;
            }
            settings.NotificationCornerRadius = NotificationCornerRadius;
            settings.NotificationShowConnectionBadge = NotificationShowConnectionBadge;
            settings.NotificationScreenMargin = NotificationScreenMargin;
            settings.NotificationShowShadow = NotificationShowShadow;
            settings.NotificationFontFamily = NotificationFontFamily;
            settings.NotificationFontWeight = NotificationFontWeight;
            settings.NotificationTitleFontFamily = Version < 7 ? NotificationFontFamily : NotificationTitleFontFamily;
            settings.NotificationTitleFontWeight = Version < 7 ? NotificationFontWeight : NotificationTitleFontWeight;
            settings.NotificationMessageFontFamily = Version < 7 ? NotificationFontFamily : NotificationMessageFontFamily;
            settings.NotificationMessageFontWeight = Version < 7 ? NotificationFontWeight : NotificationMessageFontWeight;
            settings.NotificationMessageMaxLines = Version < 7 ? 2 : NotificationMessageMaxLines;
            settings.NotificationBadgePosition = Version < 7 ? "TopRight" : NotificationBadgePosition;
            settings.NotificationTextAlignment = NotificationTextAlignment;
            settings.NotificationAccentMode = NotificationAccentMode;
            settings.NotificationAnimation = NotificationAnimation;
            settings.NotificationShowTitle = Version < 2 || NotificationShowTitle;
            settings.NotificationUppercaseTitle = Version >= 8 && NotificationUppercaseTitle;
            if (Version >= 10)
            {
                settings.NotificationTextOrder = NotificationTextOrder;
                settings.NotificationUseIndependentBorders = NotificationUseIndependentBorders;
                settings.NotificationBorderLeftThickness = NotificationBorderLeftThickness;
                settings.NotificationBorderTopThickness = NotificationBorderTopThickness;
                settings.NotificationBorderRightThickness = NotificationBorderRightThickness;
                settings.NotificationBorderBottomThickness = NotificationBorderBottomThickness;
                settings.NotificationUseStateBackgroundColors = NotificationUseStateBackgroundColors;
                settings.NotificationConnectedBackgroundColor = NotificationConnectedBackgroundColor;
                settings.NotificationDisconnectedBackgroundColor = NotificationDisconnectedBackgroundColor;
                settings.NotificationWarningBackgroundColor = NotificationWarningBackgroundColor;
                settings.NotificationLowBatteryBackgroundColor = NotificationLowBatteryBackgroundColor;
            }
            settings.DesktopNotificationWidth = DesktopNotificationWidth;
            settings.DesktopNotificationScalePercent = DesktopNotificationScalePercent;
            settings.DesktopNotificationDurationMilliseconds = DesktopNotificationDurationMilliseconds;
            settings.DesktopNotificationPosition = DesktopNotificationPosition;
            settings.DesktopNotificationBackgroundColor = DesktopNotificationBackgroundColor;
            settings.DesktopNotificationUseGradient = Version >= 8 && DesktopNotificationUseGradient;
            settings.DesktopNotificationGradientColor = Version < 8 ? DesktopNotificationBackgroundColor : DesktopNotificationGradientColor;
            settings.DesktopNotificationGradientAngle = Version < 8 ? 0 : DesktopNotificationGradientAngle;
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
            settings.DesktopNotificationShowIconContainer = Version >= 9 && DesktopNotificationShowIconContainer;
            if (Version >= 9)
            {
                settings.DesktopNotificationIconContainerColor = DesktopNotificationIconContainerColor;
                settings.DesktopNotificationIconContainerBorderColor = DesktopNotificationIconContainerBorderColor;
                settings.DesktopNotificationIconContainerBorderThickness = DesktopNotificationIconContainerBorderThickness;
                settings.DesktopNotificationIconContainerCornerRadius = DesktopNotificationIconContainerCornerRadius;
                settings.DesktopNotificationIconContainerPadding = DesktopNotificationIconContainerPadding;
            }
            settings.DesktopNotificationIconPosition = DesktopNotificationIconPosition;
            settings.DesktopNotificationPadding = DesktopNotificationPadding;
            settings.DesktopNotificationElementSpacing = DesktopNotificationElementSpacing;
            settings.DesktopNotificationIconSpacing = Version >= 6
                ? DesktopNotificationIconSpacing
                : LegacyIconSpacing(DesktopNotificationPadding, DesktopNotificationElementSpacing);
            settings.DesktopNotificationShowBorder = DesktopNotificationShowBorder;
            settings.DesktopNotificationBorderPosition = DesktopNotificationBorderPosition;
            settings.DesktopNotificationBorderThickness = DesktopNotificationBorderThickness;
            if (Version >= 11)
            {
                settings.DesktopNotificationUseBorderGradient = DesktopNotificationUseBorderGradient;
                if (Version >= 13)
                {
                    settings.DesktopNotificationUseStateBorderColors = DesktopNotificationUseStateBorderColors;
                    settings.DesktopNotificationConnectedBorderColor = DesktopNotificationConnectedBorderColor;
                    settings.DesktopNotificationDisconnectedBorderColor = DesktopNotificationDisconnectedBorderColor;
                    settings.DesktopNotificationWarningBorderColor = DesktopNotificationWarningBorderColor;
                    settings.DesktopNotificationLowBatteryBorderColor = DesktopNotificationLowBatteryBorderColor;
                }
                settings.DesktopNotificationBorderGradientStartColor = DesktopNotificationBorderGradientStartColor;
                settings.DesktopNotificationBorderGradientEndColor = DesktopNotificationBorderGradientEndColor;
                settings.DesktopNotificationBorderGradientAngle = DesktopNotificationBorderGradientAngle;
                settings.DesktopNotificationShowBorderGlow = DesktopNotificationShowBorderGlow;
                settings.DesktopNotificationBorderGlowColor = DesktopNotificationBorderGlowColor;
                settings.DesktopNotificationBorderGlowBlur = DesktopNotificationBorderGlowBlur;
                settings.DesktopNotificationBorderGlowOpacity = DesktopNotificationBorderGlowOpacity;
            }
            settings.DesktopNotificationCornerRadius = DesktopNotificationCornerRadius;
            settings.DesktopNotificationShowConnectionBadge = DesktopNotificationShowConnectionBadge;
            settings.DesktopNotificationScreenMargin = DesktopNotificationScreenMargin;
            settings.DesktopNotificationShowShadow = DesktopNotificationShowShadow;
            settings.DesktopNotificationFontFamily = DesktopNotificationFontFamily;
            settings.DesktopNotificationFontWeight = DesktopNotificationFontWeight;
            settings.DesktopNotificationTitleFontFamily = Version < 7 ? DesktopNotificationFontFamily : DesktopNotificationTitleFontFamily;
            settings.DesktopNotificationTitleFontWeight = Version < 7 ? DesktopNotificationFontWeight : DesktopNotificationTitleFontWeight;
            settings.DesktopNotificationMessageFontFamily = Version < 7 ? DesktopNotificationFontFamily : DesktopNotificationMessageFontFamily;
            settings.DesktopNotificationMessageFontWeight = Version < 7 ? DesktopNotificationFontWeight : DesktopNotificationMessageFontWeight;
            settings.DesktopNotificationMessageMaxLines = Version < 7 ? 2 : DesktopNotificationMessageMaxLines;
            settings.DesktopNotificationBadgePosition = Version < 7 ? "TopRight" : DesktopNotificationBadgePosition;
            settings.DesktopNotificationTextAlignment = DesktopNotificationTextAlignment;
            settings.DesktopNotificationAccentMode = DesktopNotificationAccentMode;
            settings.DesktopNotificationAnimation = DesktopNotificationAnimation;
            settings.DesktopNotificationShowTitle = Version < 2 || DesktopNotificationShowTitle;
            settings.DesktopNotificationUppercaseTitle = Version >= 8 && DesktopNotificationUppercaseTitle;
            if (Version >= 10)
            {
                settings.DesktopNotificationTextOrder = DesktopNotificationTextOrder;
                settings.DesktopNotificationUseIndependentBorders = DesktopNotificationUseIndependentBorders;
                settings.DesktopNotificationBorderLeftThickness = DesktopNotificationBorderLeftThickness;
                settings.DesktopNotificationBorderTopThickness = DesktopNotificationBorderTopThickness;
                settings.DesktopNotificationBorderRightThickness = DesktopNotificationBorderRightThickness;
                settings.DesktopNotificationBorderBottomThickness = DesktopNotificationBorderBottomThickness;
                settings.DesktopNotificationUseStateBackgroundColors = DesktopNotificationUseStateBackgroundColors;
                settings.DesktopNotificationConnectedBackgroundColor = DesktopNotificationConnectedBackgroundColor;
                settings.DesktopNotificationDisconnectedBackgroundColor = DesktopNotificationDisconnectedBackgroundColor;
                settings.DesktopNotificationWarningBackgroundColor = DesktopNotificationWarningBackgroundColor;
                settings.DesktopNotificationLowBatteryBackgroundColor = DesktopNotificationLowBatteryBackgroundColor;
            }
            settings.OverlayScalePercent = OverlayScalePercent;
            settings.OverlayDimColor = OverlayDimColor;
            settings.OverlayCardColor = OverlayCardColor;
            settings.OverlayUseGradient = Version >= 8 && OverlayUseGradient;
            settings.OverlayGradientColor = Version < 8 ? OverlayCardColor : OverlayGradientColor;
            settings.OverlayGradientAngle = Version < 8 ? 0 : OverlayGradientAngle;
            settings.OverlayUseBackgroundImage = Version >= 9 && OverlayUseBackgroundImage;
            if (Version >= 9)
            {
                settings.OverlayBackgroundImageStretch = OverlayBackgroundImageStretch;
                settings.OverlayBackgroundImageHorizontalAlignment = OverlayBackgroundImageHorizontalAlignment;
                settings.OverlayBackgroundImageVerticalAlignment = OverlayBackgroundImageVerticalAlignment;
                settings.OverlayBackgroundImageOpacity = OverlayBackgroundImageOpacity;
                settings.OverlayBackgroundImageTintOpacity = OverlayBackgroundImageTintOpacity;
                settings.OverlayBackgroundImagePath = RestoreImage(
                    OverlayBackgroundImageData, OverlayBackgroundImageExtension,
                    imageDirectory, "overlay", settings.OverlayBackgroundImagePath);
            }
            settings.OverlayAccentColor = OverlayAccentColor;
            settings.OverlayTextColor = OverlayTextColor;
            settings.OverlayWarningColor = OverlayWarningColor;
            settings.OverlayTitleFontSize = OverlayTitleFontSize;
            settings.OverlayControllerFontSize = OverlayControllerFontSize;
            settings.OverlayInstructionFontSize = OverlayInstructionFontSize;
            settings.OverlayStatusFontSize = OverlayStatusFontSize;
            settings.OverlayControllerIconSize = OverlayControllerIconSize;
            settings.OverlayShowControllerContainer = Version >= 9 && OverlayShowControllerContainer;
            if (Version >= 9)
            {
                settings.OverlayControllerContainerColor = OverlayControllerContainerColor;
                settings.OverlayControllerContainerBorderColor = OverlayControllerContainerBorderColor;
                settings.OverlayControllerContainerBorderThickness = OverlayControllerContainerBorderThickness;
                settings.OverlayControllerContainerCornerRadius = OverlayControllerContainerCornerRadius;
                settings.OverlayControllerContainerPadding = OverlayControllerContainerPadding;
            }
            settings.OverlayStatusIconSize = OverlayStatusIconSize;
            settings.OverlayShowControllerIcon = OverlayShowControllerIcon;
            settings.OverlayShowStatusIcon = OverlayShowStatusIcon;
            settings.OverlayShowControllerName = OverlayShowControllerName;
            settings.OverlayShowConnectionBadge = Version < 3 || OverlayShowConnectionBadge;
            settings.OverlayShowBatteryBadge = Version < 3 || OverlayShowBatteryBadge;
            settings.OverlayShowTitle = Version < 3 || OverlayShowTitle;
            settings.OverlayUppercaseTitle = Version >= 8 && OverlayUppercaseTitle;
            settings.OverlayShowInstruction = Version < 3 || OverlayShowInstruction;
            settings.OverlayShowPauseStatus = Version < 3 || OverlayShowPauseStatus;
            settings.OverlayControllerIconPosition = OverlayControllerIconPosition;
            settings.OverlayCardPosition = Version < 3 || string.IsNullOrWhiteSpace(OverlayCardPosition)
                ? "Center" : OverlayCardPosition;
            settings.OverlayLayoutMode = Version < 9 ? "Standard" : OverlayLayoutMode;
            settings.OverlayContentAlignment = Version < 7 ? "Center" : OverlayContentAlignment;
            settings.OverlayScreenMargin = Version < 7 ? 42 : OverlayScreenMargin;
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
            if (Version >= 11)
            {
                settings.OverlayUseBorderGradient = OverlayUseBorderGradient;
                settings.OverlayBorderGradientStartColor = OverlayBorderGradientStartColor;
                settings.OverlayBorderGradientEndColor = OverlayBorderGradientEndColor;
                settings.OverlayBorderGradientAngle = OverlayBorderGradientAngle;
                settings.OverlayShowBorderGlow = OverlayShowBorderGlow;
                settings.OverlayBorderGlowColor = OverlayBorderGlowColor;
                settings.OverlayBorderGlowBlur = OverlayBorderGlowBlur;
                settings.OverlayBorderGlowOpacity = OverlayBorderGlowOpacity;
            }
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
            if (Version >= 10)
            {
                settings.OverlayBlockOrder = OverlayBlockOrder;
                settings.OverlayMetadataOrientation = OverlayMetadataOrientation;
                settings.OverlayUseIndependentBorders = OverlayUseIndependentBorders;
                settings.OverlayBorderLeftThickness = OverlayBorderLeftThickness;
                settings.OverlayBorderTopThickness = OverlayBorderTopThickness;
                settings.OverlayBorderRightThickness = OverlayBorderRightThickness;
                settings.OverlayBorderBottomThickness = OverlayBorderBottomThickness;
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

        private static string ReadSoundData(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
                var bytes = File.ReadAllBytes(path);
                return bytes.Length == 0 || bytes.Length > 5 * 1024 * 1024
                    ? null : Convert.ToBase64String(bytes);
            }
            catch
            {
                return null;
            }
        }

        private static string SoundExtension(string path)
        {
            var extension = (Path.GetExtension(path) ?? string.Empty).ToLowerInvariant();
            return extension == ".mp3" || extension == ".wma" ? extension : ".wav";
        }

        private static string RestoreSound(string data, string extension, string directory,
            string prefix, string fallback)
        {
            if (string.IsNullOrWhiteSpace(data)) return string.Empty;
            if (string.IsNullOrWhiteSpace(directory)) return fallback ?? string.Empty;
            try
            {
                var bytes = Convert.FromBase64String(data);
                if (bytes.Length == 0 || bytes.Length > 5 * 1024 * 1024) return string.Empty;
                Directory.CreateDirectory(directory);
                var normalized = (extension ?? string.Empty).ToLowerInvariant();
                var safeExtension = normalized == ".mp3" || normalized == ".wma" ? normalized : ".wav";
                var path = Path.Combine(directory, prefix + "-" + Guid.NewGuid().ToString("N") + safeExtension);
                File.WriteAllBytes(path, bytes);
                return path;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int LegacyIconSpacing(int padding, int elementSpacing)
        {
            return Math.Max(8, Math.Max(elementSpacing, (int)Math.Round(padding * 0.75)));
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
