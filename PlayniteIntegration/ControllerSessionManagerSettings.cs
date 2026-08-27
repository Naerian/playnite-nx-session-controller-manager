using System.Collections.Generic;
using System.Linq;
using ControllerSessionManager.Controllers;
using ControllerSessionManager.Sessions;
using ControllerSessionManager.Tester;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class ControllerSessionManagerSettings : ObservableObject, ISettings
    {
        private ControllerSessionManagerPlugin plugin;
        private ControllerSessionManagerSettings editingClone;
        private bool enableMonitoring = true;
        private bool enableDebugLogging;
        private bool autoUpdateControllerDatabase = true;
        private string creatorThemeUpdatePolicy = CreatorThemeUpdatePolicyStartup;
        private string creatorThemeLastUpdateUtc = string.Empty;
        private bool showPrimaryControllerInTopPanel;
        private string topPanelControllerMode = TopPanelControllerModeHidden;
        private bool colorTopPanelIndicatorByBattery = true;
        private bool launchFullscreenOnGuideButton;
        private bool setupWizardCompleted;
        private bool enableSessionTracking = true;
        private bool showDisconnectOverlay = true;
        private bool showFullscreenControllerNotifications = true;
        private bool showDesktopControllerNotifications = true;
        private bool showFullscreenLowBatteryNotifications = true;
        private bool showDesktopLowBatteryNotifications = true;
        private string lowBatteryNotificationThreshold = Controllers.LowBatteryNotificationTracker.ThresholdLow;
        private bool forcePauseOfflineGames = true;
        private int notificationWidth = 520;
        private int notificationScalePercent = 110;
        private int notificationDurationMilliseconds = 5000;
        private string notificationPosition = "TopRight";
        private string notificationBackgroundColor = "#F4121418";
        private bool notificationUseGradient;
        private string notificationGradientColor = "#F4121418";
        private int notificationGradientAngle;
        private bool notificationUseBackgroundImage;
        private string notificationBackgroundImagePath = string.Empty;
        private string notificationBackgroundImageStretch = "UniformToFill";
        private string notificationBackgroundImageHorizontalAlignment = "Center";
        private string notificationBackgroundImageVerticalAlignment = "Center";
        private int notificationBackgroundImageOpacity = 45;
        private int notificationBackgroundImageTintOpacity = 45;
        private string notificationTextColor = "#FFFFFFFF";
        private string notificationSecondaryTextColor = "#FFC6CBD4";
        private string notificationConnectedColor = "#FF4FC27E";
        private string notificationDisconnectedColor = "#FF50AAFF";
        private string notificationWarningColor = "#FFF5B542";
        private string notificationLowBatteryColor = "#FFE05252";
        private int notificationTitleFontSize = 19;
        private int notificationMessageFontSize = 15;
        private int notificationIconSize = 32;
        private bool notificationShowIconContainer;
        private string notificationIconContainerColor = "#20000000";
        private string notificationIconContainerBorderColor = "#00000000";
        private int notificationIconContainerBorderThickness;
        private int notificationIconContainerCornerRadius = 10;
        private int notificationIconContainerPadding = 8;
        private string notificationIconPosition = "Left";
        private int notificationPadding = 18;
        private int notificationElementSpacing = 8;
        private int notificationIconSpacing = 14;
        private bool notificationShowBorder = true;
        private string notificationBorderPosition = "Bottom";
        private int notificationBorderThickness = 3;
        private int notificationCornerRadius = 10;
        private bool showControllerNameInNotifications = true;
        private bool showControllerNameInDesktopNotifications = true;
        private bool notificationShowConnectionBadge = true;
        private bool desktopNotificationShowConnectionBadge = true;
        private int notificationScreenMargin = 28;
        private int desktopNotificationScreenMargin = 28;
        private bool notificationShowShadow = true;
        private bool desktopNotificationShowShadow = true;
        private string notificationFontFamily = NotificationFontCatalog.SystemDefault;
        private string notificationFontWeight = "SemiBold";
        private string notificationTitleFontFamily = NotificationFontCatalog.SystemDefault;
        private string notificationTitleFontWeight = "SemiBold";
        private string notificationMessageFontFamily = NotificationFontCatalog.SystemDefault;
        private string notificationMessageFontWeight = "Regular";
        private int notificationMessageMaxLines = 2;
        private string notificationBadgePosition = "TopRight";
        private string notificationTextAlignment = "Left";
        private string notificationAccentMode = "IconAndBorder";
        private string notificationAnimation = "Fade";
        private bool notificationShowTitle = true;
        private bool notificationUppercaseTitle;
        private string notificationTextOrder = "TitleFirst";
        private bool notificationUseIndependentBorders;
        private int notificationBorderLeftThickness;
        private int notificationBorderTopThickness;
        private int notificationBorderRightThickness;
        private int notificationBorderBottomThickness = 3;
        private bool notificationUseStateBackgroundColors;
        private string notificationConnectedBackgroundColor = "#F4121418";
        private string notificationDisconnectedBackgroundColor = "#F4121418";
        private string notificationWarningBackgroundColor = "#F4121418";
        private string notificationLowBatteryBackgroundColor = "#F4121418";
        private bool notificationUseBorderGradient;
        private bool notificationUseStateBorderColors;
        private string notificationConnectedBorderColor = "#FF4FC27E";
        private string notificationDisconnectedBorderColor = "#FF50AAFF";
        private string notificationWarningBorderColor = "#FFF5B542";
        private string notificationLowBatteryBorderColor = "#FFE05252";
        private string notificationBorderGradientStartColor = "#FFFFFFFF";
        private string notificationBorderGradientEndColor = "#FF50AAFF";
        private int notificationBorderGradientAngle = 45;
        private bool notificationShowBorderGlow;
        private string notificationBorderGlowColor = "#8050AAFF";
        private int notificationBorderGlowBlur = 12;
        private int notificationBorderGlowOpacity = 30;
        private int desktopNotificationWidth = 420;
        private int desktopNotificationScalePercent = 100;
        private int desktopNotificationDurationMilliseconds = 4000;
        private string desktopNotificationPosition = "BottomRight";
        private string desktopNotificationBackgroundColor = "#F4121418";
        private bool desktopNotificationUseGradient;
        private string desktopNotificationGradientColor = "#F4121418";
        private int desktopNotificationGradientAngle;
        private bool desktopNotificationUseBackgroundImage;
        private string desktopNotificationBackgroundImagePath = string.Empty;
        private string desktopNotificationBackgroundImageStretch = "UniformToFill";
        private string desktopNotificationBackgroundImageHorizontalAlignment = "Center";
        private string desktopNotificationBackgroundImageVerticalAlignment = "Center";
        private int desktopNotificationBackgroundImageOpacity = 45;
        private int desktopNotificationBackgroundImageTintOpacity = 45;
        private string desktopNotificationTextColor = "#FFFFFFFF";
        private string desktopNotificationSecondaryTextColor = "#FFC6CBD4";
        private string desktopNotificationConnectedColor = "#FF4FC27E";
        private string desktopNotificationDisconnectedColor = "#FF50AAFF";
        private string desktopNotificationWarningColor = "#FFF5B542";
        private string desktopNotificationLowBatteryColor = "#FFE05252";
        private int desktopNotificationTitleFontSize = 17;
        private int desktopNotificationMessageFontSize = 14;
        private int desktopNotificationIconSize = 28;
        private bool desktopNotificationShowIconContainer;
        private string desktopNotificationIconContainerColor = "#20000000";
        private string desktopNotificationIconContainerBorderColor = "#00000000";
        private int desktopNotificationIconContainerBorderThickness;
        private int desktopNotificationIconContainerCornerRadius = 8;
        private int desktopNotificationIconContainerPadding = 7;
        private string desktopNotificationIconPosition = "Left";
        private int desktopNotificationPadding = 14;
        private int desktopNotificationElementSpacing = 6;
        private int desktopNotificationIconSpacing = 11;
        private bool desktopNotificationShowBorder = true;
        private string desktopNotificationBorderPosition = "Bottom";
        private int desktopNotificationBorderThickness = 3;
        private int desktopNotificationCornerRadius = 8;
        private string desktopNotificationFontFamily = NotificationFontCatalog.SystemDefault;
        private string desktopNotificationFontWeight = "SemiBold";
        private string desktopNotificationTitleFontFamily = NotificationFontCatalog.SystemDefault;
        private string desktopNotificationTitleFontWeight = "SemiBold";
        private string desktopNotificationMessageFontFamily = NotificationFontCatalog.SystemDefault;
        private string desktopNotificationMessageFontWeight = "Regular";
        private int desktopNotificationMessageMaxLines = 2;
        private string desktopNotificationBadgePosition = "TopRight";
        private string desktopNotificationTextAlignment = "Left";
        private string desktopNotificationAccentMode = "IconAndBorder";
        private string desktopNotificationAnimation = "Fade";
        private bool desktopNotificationShowTitle = true;
        private bool desktopNotificationUppercaseTitle;
        private string desktopNotificationTextOrder = "TitleFirst";
        private bool desktopNotificationUseIndependentBorders;
        private int desktopNotificationBorderLeftThickness;
        private int desktopNotificationBorderTopThickness;
        private int desktopNotificationBorderRightThickness;
        private int desktopNotificationBorderBottomThickness = 3;
        private bool desktopNotificationUseStateBackgroundColors;
        private string desktopNotificationConnectedBackgroundColor = "#F4121418";
        private string desktopNotificationDisconnectedBackgroundColor = "#F4121418";
        private string desktopNotificationWarningBackgroundColor = "#F4121418";
        private string desktopNotificationLowBatteryBackgroundColor = "#F4121418";
        private bool desktopNotificationUseBorderGradient;
        private bool desktopNotificationUseStateBorderColors;
        private string desktopNotificationConnectedBorderColor = "#FF4FC27E";
        private string desktopNotificationDisconnectedBorderColor = "#FF50AAFF";
        private string desktopNotificationWarningBorderColor = "#FFF5B542";
        private string desktopNotificationLowBatteryBorderColor = "#FFE05252";
        private string desktopNotificationBorderGradientStartColor = "#FFFFFFFF";
        private string desktopNotificationBorderGradientEndColor = "#FF50AAFF";
        private int desktopNotificationBorderGradientAngle = 45;
        private bool desktopNotificationShowBorderGlow;
        private string desktopNotificationBorderGlowColor = "#8050AAFF";
        private int desktopNotificationBorderGlowBlur = 12;
        private int desktopNotificationBorderGlowOpacity = 30;
        private int overlayScalePercent = 100;
        private string overlayDimColor = "#96000000";
        private string overlayCardColor = "#EB121418";
        private bool overlayUseGradient;
        private string overlayGradientColor = "#EB121418";
        private int overlayGradientAngle;
        private bool overlaySceneUseGradient;
        private string overlaySceneGradientColor = "#FF05060A";
        private int overlaySceneGradientAngle = 160;
        private bool overlaySceneUseBackgroundImage;
        private string overlaySceneBackgroundImagePath = string.Empty;
        private string overlaySceneBackgroundImageStretch = "UniformToFill";
        private string overlaySceneBackgroundImageHorizontalAlignment = "Center";
        private string overlaySceneBackgroundImageVerticalAlignment = "Center";
        private int overlaySceneBackgroundImageOpacity = 100;
        private bool overlaySceneUseAmbientGlows;
        private string overlaySceneGlow1Color = "#293FE0E8";
        private int overlaySceneGlow1X = 20;
        private int overlaySceneGlow1Y = 25;
        private int overlaySceneGlow1Radius = 60;
        private string overlaySceneGlow2Color = "#24B18CFF";
        private int overlaySceneGlow2X = 85;
        private int overlaySceneGlow2Y = 20;
        private int overlaySceneGlow2Radius = 60;
        private string overlaySceneGlow3Color = "#196EE7A0";
        private int overlaySceneGlow3X = 75;
        private int overlaySceneGlow3Y = 85;
        private int overlaySceneGlow3Radius = 65;
        private bool overlaySceneShowGrid;
        private string overlaySceneGridColor = "#09FFFFFF";
        private int overlaySceneGridSize = 44;
        private bool overlayUseBackgroundImage;
        private string overlayBackgroundImagePath = string.Empty;
        private string overlayBackgroundImageStretch = "UniformToFill";
        private string overlayBackgroundImageHorizontalAlignment = "Center";
        private string overlayBackgroundImageVerticalAlignment = "Center";
        private int overlayBackgroundImageOpacity = 70;
        private int overlayBackgroundImageTintOpacity = 45;
        private string overlayAccentColor = "#FF2391FF";
        private string overlayInstructionColor = "#FF2391FF";
        private string overlayControllerIconColor = "#FFFFFFFF";
        private string overlayTextColor = "#FFFFFFFF";
        private string overlayWarningColor = "#FFF5B542";
        private int overlayTitleFontSize = 30;
        private int overlayControllerFontSize = 22;
        private int overlayInstructionFontSize = 19;
        private int overlayStatusFontSize = 15;
        private int overlayControllerIconSize = 30;
        private bool overlayShowControllerContainer;
        private string overlayControllerContainerColor = "#20000000";
        private string overlayControllerContainerBorderColor = "#00000000";
        private int overlayControllerContainerBorderThickness;
        private int overlayControllerContainerCornerRadius = 12;
        private int overlayControllerContainerPadding = 12;
        private int overlayStatusIconSize = 18;
        private bool overlayShowControllerIcon = true;
        private bool overlayShowStatusIcon = true;
        private bool overlayShowControllerName = true;
        private bool overlayShowConnectionBadge = true;
        private bool overlayShowBatteryBadge = true;
        private bool overlayShowTitle = true;
        private bool overlayUppercaseTitle;
        private bool overlayShowInstruction = true;
        private bool overlayShowPauseStatus = true;
        private bool overlayShowDisconnectTimer;
        private string overlayControllerIconPosition = "Left";
        private string overlayCardPosition = "Center";
        private string overlayLayoutMode = "Standard";
        private string overlaySplitControllerSide = "Left";
        private bool overlayShowSplitDivider;
        private string overlaySplitDividerColor = "#18FFFFFF";
        private int overlaySplitDividerThickness = 1;
        private bool overlayShowIncidentBadge;
        private string overlayIncidentBadgeTextColor = "#FFFFB454";
        private string overlayIncidentBadgeBackgroundColor = "#26FFB454";
        private string overlayIncidentBadgeBorderColor = "#00FFFFFF";
        private int overlayIncidentBadgeBorderThickness;
        private int overlayIncidentBadgeCornerRadius = 12;
        private int overlayIncidentBadgeTextSize = 11;
        private bool overlayStatusInMetadata;
        private string overlayContentAlignment = "Center";
        private int overlayScreenMargin = 42;
        private string overlayAnimation = "FadeScale";
        private string overlayBorderPosition = "Full";
        private int overlayCardWidth = 620;
        private int overlayPadding = 34;
        private int overlayElementSpacing = 14;
        private bool overlayShowBorder = true;
        private bool overlayShowShadow = true;
        private int overlayBorderThickness = 3;
        private int overlayCornerRadius = 13;
        private string overlayFontFamily = NotificationFontCatalog.SystemDefault;
        private string overlayFontWeight = "SemiBold";
        private string overlayTitleFontFamily = NotificationFontCatalog.SystemDefault;
        private string overlayTitleFontWeight = "SemiBold";
        private string overlayControllerFontFamily = NotificationFontCatalog.SystemDefault;
        private string overlayControllerFontWeight = "SemiBold";
        private string overlayInstructionFontFamily = NotificationFontCatalog.SystemDefault;
        private string overlayInstructionFontWeight = "SemiBold";
        private string overlayStatusFontFamily = NotificationFontCatalog.SystemDefault;
        private string overlayStatusFontWeight = "SemiBold";
        private string overlayConnectionBadgeTextColor = "#FFFFFFFF";
        private string overlayConnectionBadgeIconColor = "#FFFFFFFF";
        private string overlayConnectionBadgeBackgroundColor = "#302391FF";
        private string overlayConnectionBadgeBorderColor = "#602391FF";
        private int overlayConnectionBadgeBorderThickness;
        private int overlayConnectionBadgeCornerRadius = 5;
        private int overlayConnectionBadgeIconSize = 14;
        private int overlayConnectionBadgeTextSize = 13;
        private string overlayBatteryBadgeTextColor = "#FFF5B542";
        private string overlayBatteryBadgeIconColor = "#FFF5B542";
        private string overlayBatteryBadgeBackgroundColor = "#30F5B542";
        private string overlayBatteryBadgeBorderColor = "#60F5B542";
        private int overlayBatteryBadgeBorderThickness;
        private int overlayBatteryBadgeCornerRadius = 5;
        private int overlayBatteryBadgeIconSize = 14;
        private int overlayBatteryBadgeTextSize = 13;
        private bool overlayBatteryBadgeUseStateColors = true;
        private string overlayBatteryBadgeFullColor = "#FF4FC27E";
        private string overlayBatteryBadgeMediumColor = "#FFF5B542";
        private string overlayBatteryBadgeLowColor = "#FFE05252";
        private string overlayBatteryBadgeEmptyColor = "#FFC92D45";
        private string overlayBlockOrder = "Title,Controller,Metadata,Instruction,Status";
        private string overlayMetadataOrientation = "Horizontal";
        private bool overlayUseIndependentBorders;
        private int overlayBorderLeftThickness = 3;
        private int overlayBorderTopThickness = 3;
        private int overlayBorderRightThickness = 3;
        private int overlayBorderBottomThickness = 3;
        private bool overlayUseBorderGradient;
        private string overlayBorderGradientStartColor = "#FFFFFFFF";
        private string overlayBorderGradientEndColor = "#FF2391FF";
        private int overlayBorderGradientAngle = 45;
        private bool overlayShowBorderGlow;
        private string overlayBorderGlowColor = "#802391FF";
        private int overlayBorderGlowBlur = 16;
        private int overlayBorderGlowOpacity = 30;
        private bool allowControllerTakeover = true;
        private bool protectAllActiveControllers;
        private int settingsSchemaVersion;
        private string appearancePreset = SettingsAppearance.Midnight;
        private string notificationStylePreset = NotificationStylePresets.Soft;
        private string desktopNotificationStylePreset = NotificationStylePresets.Soft;
        private string overlayStylePreset = OverlayStylePresets.Soft;
        private bool enableNotificationSounds = true;
        private bool enableDesktopNotificationSounds = true;
        private bool enableFullscreenNotificationSounds = true;
        private bool notificationPreviewWithSound;
        private string notificationSoundPack = NotificationSoundCatalog.ModernCrystal;
        private bool playSoundOnConnected;
        private bool playSoundOnDisconnected;
        private bool playSoundOnLowBattery = true;
        private bool playSoundOnWarning = true;
        private string customConnectedSoundPath = string.Empty;
        private string customDisconnectedSoundPath = string.Empty;
        private string customLowBatterySoundPath = string.Empty;
        private string customWarningSoundPath = string.Empty;
        private double notificationSoundVolume = 0.7;
        private Dictionary<string, string> savedCustomNotificationStyle =
            new Dictionary<string, string>();
        private Dictionary<string, string> savedCustomDesktopNotificationStyle =
            new Dictionary<string, string>();
        private bool pauseGameOnDisconnect;
        private int disconnectGracePeriodMilliseconds = 1500;
        private int reconciliationIntervalSeconds = 5;
        private List<ControllerProfile> controllerProfiles = new List<ControllerProfile>();
        private List<GameSessionOverride> gameSessionOverrides = new List<GameSessionOverride>();
        private GamepadTesterSettings tester = new GamepadTesterSettings();

        public const string TopPanelControllerModeHidden = "Hidden";
        public const string TopPanelControllerModeDefault = "Default";
        public const string TopPanelControllerModePrimary = "Primary";
        public const string CreatorThemeUpdatePolicyStartup = "Startup";
        public const string CreatorThemeUpdatePolicyDaily = "Daily";
        public const string CreatorThemeUpdatePolicyManual = "Manual";

        public ControllerSessionManagerSettings()
        {
        }

        internal ControllerSessionManagerSettings(ControllerSessionManagerPlugin sourcePlugin)
        {
            plugin = sourcePlugin;
            var saved = sourcePlugin.LoadPluginSettings<ControllerSessionManagerSettings>();
            if (saved != null)
            {
                CopyFrom(saved);
            }

            MigrateSettings();
        }

        public bool EnableMonitoring
        {
            get { return enableMonitoring; }
            set { SetValue(ref enableMonitoring, value); }
        }

        public int SettingsSchemaVersion
        {
            get { return settingsSchemaVersion; }
            set { SetValue(ref settingsSchemaVersion, value); }
        }

        public string AppearancePreset
        {
            get { return SettingsAppearance.Normalize(appearancePreset); }
            set { SetValue(ref appearancePreset, SettingsAppearance.Normalize(value)); }
        }

        public string NotificationStylePreset
        {
            get { return NotificationStylePresets.Normalize(notificationStylePreset); }
            set
            {
                SetValue(ref notificationStylePreset, NotificationStylePresets.Normalize(value));
                NotifyCreatorThemeStateChanged();
            }
        }

        public string DesktopNotificationStylePreset
        {
            get { return NotificationStylePresets.Normalize(desktopNotificationStylePreset); }
            set
            {
                SetValue(ref desktopNotificationStylePreset, NotificationStylePresets.Normalize(value));
                NotifyCreatorThemeStateChanged();
            }
        }

        public string OverlayStylePreset
        {
            get { return OverlayStylePresets.Normalize(overlayStylePreset); }
            set
            {
                SetValue(ref overlayStylePreset, OverlayStylePresets.Normalize(value));
                NotifyCreatorThemeStateChanged();
            }
        }

        public bool IsFullscreenNotificationCreatorThemeActive
        {
            get { return NotificationStylePresets.IsCreatorPreset(NotificationStylePreset); }
        }

        public bool IsDesktopNotificationCreatorThemeActive
        {
            get { return NotificationStylePresets.IsCreatorPreset(DesktopNotificationStylePreset); }
        }

        public bool IsCreatorNotificationThemeActive
        {
            get { return IsFullscreenNotificationCreatorThemeActive || IsDesktopNotificationCreatorThemeActive; }
        }

        public bool CanEditFullscreenNotificationStyle { get { return !IsFullscreenNotificationCreatorThemeActive; } }
        public bool CanEditDesktopNotificationStyle { get { return !IsDesktopNotificationCreatorThemeActive; } }
        public bool IsOverlayCreatorThemeActive
        {
            get { return OverlayStylePresets.IsCreatorPreset(OverlayStylePreset); }
        }
        public bool CanEditOverlayStyle { get { return !IsOverlayCreatorThemeActive; } }
        public bool CanEditNotificationAudio { get { return true; } }
        public bool CanCopyNotificationStyles { get { return !IsCreatorNotificationThemeActive; } }

        public bool EnableNotificationSounds
        {
            get { return enableNotificationSounds; }
            set { SetValue(ref enableNotificationSounds, value); }
        }

        public bool EnableDesktopNotificationSounds
        {
            get { return enableDesktopNotificationSounds; }
            set { SetValue(ref enableDesktopNotificationSounds, value); }
        }

        public bool EnableFullscreenNotificationSounds
        {
            get { return enableFullscreenNotificationSounds; }
            set { SetValue(ref enableFullscreenNotificationSounds, value); }
        }

        public bool NotificationPreviewWithSound
        {
            get { return notificationPreviewWithSound; }
            set { SetValue(ref notificationPreviewWithSound, value); }
        }

        public string NotificationSoundPack
        {
            get { return NotificationSoundCatalog.Normalize(notificationSoundPack); }
            set { SetValue(ref notificationSoundPack, NotificationSoundCatalog.Normalize(value)); }
        }

        public bool PlaySoundOnConnected
        {
            get { return playSoundOnConnected; }
            set { SetValue(ref playSoundOnConnected, value); }
        }

        public bool PlaySoundOnDisconnected
        {
            get { return playSoundOnDisconnected; }
            set { SetValue(ref playSoundOnDisconnected, value); }
        }

        public bool PlaySoundOnLowBattery
        {
            get { return playSoundOnLowBattery; }
            set { SetValue(ref playSoundOnLowBattery, value); }
        }

        public bool PlaySoundOnWarning
        {
            get { return playSoundOnWarning; }
            set { SetValue(ref playSoundOnWarning, value); }
        }

        public string CustomConnectedSoundPath
        {
            get { return customConnectedSoundPath ?? string.Empty; }
            set { SetValue(ref customConnectedSoundPath, value ?? string.Empty); }
        }

        public string CustomDisconnectedSoundPath
        {
            get { return customDisconnectedSoundPath ?? string.Empty; }
            set { SetValue(ref customDisconnectedSoundPath, value ?? string.Empty); }
        }

        public string CustomLowBatterySoundPath
        {
            get { return customLowBatterySoundPath ?? string.Empty; }
            set { SetValue(ref customLowBatterySoundPath, value ?? string.Empty); }
        }

        public string CustomWarningSoundPath
        {
            get { return customWarningSoundPath ?? string.Empty; }
            set { SetValue(ref customWarningSoundPath, value ?? string.Empty); }
        }

        public Dictionary<string, string> SavedCustomNotificationStyle
        {
            get { return savedCustomNotificationStyle; }
            set { savedCustomNotificationStyle = NotificationStyleState.Clone(value); }
        }

        public Dictionary<string, string> SavedCustomDesktopNotificationStyle
        {
            get { return savedCustomDesktopNotificationStyle; }
            set { savedCustomDesktopNotificationStyle = NotificationStyleState.Clone(value); }
        }

        public double NotificationSoundVolume
        {
            get { return notificationSoundVolume; }
            set
            {
                var volume = value;
                if (double.IsNaN(volume) || double.IsInfinity(volume))
                {
                    volume = 0.7;
                }
                else if (volume < 0)
                {
                    volume = 0;
                }
                else if (volume > 1)
                {
                    volume = 1;
                }

                SetValue(ref notificationSoundVolume, volume);
            }
        }

        public bool EnableDebugLogging
        {
            get { return enableDebugLogging; }
            set { SetValue(ref enableDebugLogging, value); }
        }

        public bool AutoUpdateControllerDatabase
        {
            get { return autoUpdateControllerDatabase; }
            set { SetValue(ref autoUpdateControllerDatabase, value); }
        }

        public string CreatorThemeUpdatePolicy
        {
            get { return NormalizeCreatorThemeUpdatePolicy(creatorThemeUpdatePolicy); }
            set { SetValue(ref creatorThemeUpdatePolicy, NormalizeCreatorThemeUpdatePolicy(value)); }
        }

        public string CreatorThemeLastUpdateUtc
        {
            get { return creatorThemeLastUpdateUtc ?? string.Empty; }
            set { SetValue(ref creatorThemeLastUpdateUtc, value ?? string.Empty); }
        }

        public string TopPanelControllerMode
        {
            get { return NormalizeTopPanelControllerMode(topPanelControllerMode); }
            set
            {
                var normalized = NormalizeTopPanelControllerMode(value);
                SetValue(ref topPanelControllerMode, normalized);
                OnPropertyChanged("IsTopPanelButtonVisible");
                OnPropertyChanged("ShowPrimaryControllerInTopPanel");
            }
        }

        /// <summary>
        /// Legacy setting kept for deserialize/migration. Prefer <see cref="TopPanelControllerMode"/>.
        /// </summary>
        public bool ShowPrimaryControllerInTopPanel
        {
            get { return IsTopPanelButtonVisible; }
            set { showPrimaryControllerInTopPanel = value; }
        }

        public bool IsTopPanelButtonVisible
        {
            get
            {
                return !string.Equals(TopPanelControllerMode, TopPanelControllerModeHidden,
                    System.StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool ColorTopPanelIndicatorByBattery
        {
            get { return colorTopPanelIndicatorByBattery; }
            set { SetValue(ref colorTopPanelIndicatorByBattery, value); }
        }

        /// <summary>
        /// Desktop-only: hold Guide/PS/Home briefly (then release) to switch to Fullscreen. Off by default.
        /// </summary>
        public bool LaunchFullscreenOnGuideButton
        {
            get { return launchFullscreenOnGuideButton; }
            set { SetValue(ref launchFullscreenOnGuideButton, value); }
        }

        public bool SetupWizardCompleted
        {
            get { return setupWizardCompleted; }
            set { SetValue(ref setupWizardCompleted, value); }
        }

        public int ReconciliationIntervalSeconds
        {
            get { return reconciliationIntervalSeconds; }
            set { SetValue(ref reconciliationIntervalSeconds, value); }
        }

        public bool EnableSessionTracking
        {
            get { return enableSessionTracking; }
            set { SetValue(ref enableSessionTracking, value); }
        }

        public int DisconnectGracePeriodMilliseconds
        {
            get { return disconnectGracePeriodMilliseconds; }
            set { SetValue(ref disconnectGracePeriodMilliseconds, value); }
        }

        public bool AllowControllerTakeover
        {
            get { return allowControllerTakeover; }
            set { SetValue(ref allowControllerTakeover, value); }
        }

        public bool ShowDisconnectOverlay
        {
            get { return showDisconnectOverlay; }
            set { SetValue(ref showDisconnectOverlay, value); }
        }

        public bool ShowFullscreenControllerNotifications
        {
            get { return showFullscreenControllerNotifications; }
            set { SetValue(ref showFullscreenControllerNotifications, value); }
        }

        public bool ShowDesktopControllerNotifications
        {
            get { return showDesktopControllerNotifications; }
            set { SetValue(ref showDesktopControllerNotifications, value); }
        }

        public bool ShowFullscreenLowBatteryNotifications
        {
            get { return showFullscreenLowBatteryNotifications; }
            set { SetValue(ref showFullscreenLowBatteryNotifications, value); }
        }

        public bool ShowDesktopLowBatteryNotifications
        {
            get { return showDesktopLowBatteryNotifications; }
            set { SetValue(ref showDesktopLowBatteryNotifications, value); }
        }

        /// <summary>
        /// Notify when battery is at or below this coarse level: "Low" (Empty+Low) or "Empty".
        /// </summary>
        public string LowBatteryNotificationThreshold
        {
            get
            {
                return Controllers.LowBatteryNotificationTracker.NormalizeThreshold(
                    lowBatteryNotificationThreshold);
            }
            set
            {
                SetValue(
                    ref lowBatteryNotificationThreshold,
                    Controllers.LowBatteryNotificationTracker.NormalizeThreshold(value));
            }
        }

        public bool ForcePauseOfflineGames
        {
            get { return forcePauseOfflineGames; }
            set { SetValue(ref forcePauseOfflineGames, value); }
        }

        public int NotificationWidth { get { return notificationWidth; } set { SetValue(ref notificationWidth, value); } }
        public int NotificationScalePercent { get { return notificationScalePercent; } set { SetValue(ref notificationScalePercent, value); } }
        public int NotificationDurationMilliseconds { get { return notificationDurationMilliseconds; } set { SetValue(ref notificationDurationMilliseconds, value); } }
        public string NotificationPosition { get { return notificationPosition; } set { SetValue(ref notificationPosition, value); } }
        public string NotificationBackgroundColor { get { return notificationBackgroundColor; } set { SetValue(ref notificationBackgroundColor, value); } }
        public bool NotificationUseGradient { get { return notificationUseGradient; } set { SetValue(ref notificationUseGradient, value); } }
        public string NotificationGradientColor { get { return notificationGradientColor; } set { SetValue(ref notificationGradientColor, value); } }
        public int NotificationGradientAngle { get { return notificationGradientAngle; } set { SetValue(ref notificationGradientAngle, NormalizeAngle(value)); } }
        public bool NotificationUseBackgroundImage { get { return notificationUseBackgroundImage; } set { SetValue(ref notificationUseBackgroundImage, value); } }
        public string NotificationBackgroundImagePath { get { return notificationBackgroundImagePath; } set { SetValue(ref notificationBackgroundImagePath, value ?? string.Empty); } }
        public string NotificationBackgroundImageStretch { get { return notificationBackgroundImageStretch; } set { SetValue(ref notificationBackgroundImageStretch, value ?? "UniformToFill"); } }
        public string NotificationBackgroundImageHorizontalAlignment { get { return notificationBackgroundImageHorizontalAlignment; } set { SetValue(ref notificationBackgroundImageHorizontalAlignment, value ?? "Center"); } }
        public string NotificationBackgroundImageVerticalAlignment { get { return notificationBackgroundImageVerticalAlignment; } set { SetValue(ref notificationBackgroundImageVerticalAlignment, value ?? "Center"); } }
        public int NotificationBackgroundImageOpacity { get { return notificationBackgroundImageOpacity; } set { SetValue(ref notificationBackgroundImageOpacity, ClampPercent(value)); } }
        public int NotificationBackgroundImageTintOpacity { get { return notificationBackgroundImageTintOpacity; } set { SetValue(ref notificationBackgroundImageTintOpacity, ClampPercent(value)); } }
        public string NotificationTextColor { get { return notificationTextColor; } set { SetValue(ref notificationTextColor, value); } }
        public string NotificationSecondaryTextColor { get { return notificationSecondaryTextColor; } set { SetValue(ref notificationSecondaryTextColor, value); } }
        public string NotificationConnectedColor { get { return notificationConnectedColor; } set { SetValue(ref notificationConnectedColor, value); } }
        public string NotificationDisconnectedColor { get { return notificationDisconnectedColor; } set { SetValue(ref notificationDisconnectedColor, value); } }
        public string NotificationWarningColor { get { return notificationWarningColor; } set { SetValue(ref notificationWarningColor, value); } }
        public string NotificationLowBatteryColor { get { return notificationLowBatteryColor; } set { SetValue(ref notificationLowBatteryColor, value); } }
        public int NotificationTitleFontSize { get { return notificationTitleFontSize; } set { SetValue(ref notificationTitleFontSize, value); } }
        public int NotificationMessageFontSize { get { return notificationMessageFontSize; } set { SetValue(ref notificationMessageFontSize, value); } }
        public int NotificationIconSize { get { return notificationIconSize; } set { SetValue(ref notificationIconSize, value); } }
        public bool NotificationShowIconContainer { get { return notificationShowIconContainer; } set { SetValue(ref notificationShowIconContainer, value); } }
        public string NotificationIconContainerColor { get { return notificationIconContainerColor; } set { SetValue(ref notificationIconContainerColor, value); } }
        public string NotificationIconContainerBorderColor { get { return notificationIconContainerBorderColor; } set { SetValue(ref notificationIconContainerBorderColor, value); } }
        public int NotificationIconContainerBorderThickness { get { return notificationIconContainerBorderThickness; } set { SetValue(ref notificationIconContainerBorderThickness, System.Math.Max(0, System.Math.Min(8, value))); } }
        public int NotificationIconContainerCornerRadius { get { return notificationIconContainerCornerRadius; } set { SetValue(ref notificationIconContainerCornerRadius, System.Math.Max(0, System.Math.Min(40, value))); } }
        public int NotificationIconContainerPadding { get { return notificationIconContainerPadding; } set { SetValue(ref notificationIconContainerPadding, System.Math.Max(0, System.Math.Min(24, value))); } }
        public string NotificationIconPosition { get { return notificationIconPosition; } set { SetValue(ref notificationIconPosition, value); } }
        public int NotificationPadding { get { return notificationPadding; } set { SetValue(ref notificationPadding, value); } }
        public int NotificationElementSpacing { get { return notificationElementSpacing; } set { SetValue(ref notificationElementSpacing, value); } }
        public int NotificationIconSpacing { get { return notificationIconSpacing; } set { SetValue(ref notificationIconSpacing, value); } }
        public bool NotificationShowBorder { get { return notificationShowBorder; } set { SetValue(ref notificationShowBorder, value); } }
        public string NotificationBorderPosition { get { return notificationBorderPosition; } set { SetValue(ref notificationBorderPosition, value); } }
        public int NotificationBorderThickness { get { return notificationBorderThickness; } set { SetValue(ref notificationBorderThickness, value); } }
        public int NotificationCornerRadius { get { return notificationCornerRadius; } set { SetValue(ref notificationCornerRadius, value); } }
        public bool ShowControllerNameInNotifications { get { return showControllerNameInNotifications; } set { SetValue(ref showControllerNameInNotifications, value); } }
        public bool ShowControllerNameInDesktopNotifications { get { return showControllerNameInDesktopNotifications; } set { SetValue(ref showControllerNameInDesktopNotifications, value); } }
        public bool NotificationShowConnectionBadge { get { return notificationShowConnectionBadge; } set { SetValue(ref notificationShowConnectionBadge, value); } }
        public bool DesktopNotificationShowConnectionBadge { get { return desktopNotificationShowConnectionBadge; } set { SetValue(ref desktopNotificationShowConnectionBadge, value); } }
        public int NotificationScreenMargin { get { return notificationScreenMargin; } set { SetValue(ref notificationScreenMargin, value); } }
        public int DesktopNotificationScreenMargin { get { return desktopNotificationScreenMargin; } set { SetValue(ref desktopNotificationScreenMargin, value); } }
        public bool NotificationShowShadow { get { return notificationShowShadow; } set { SetValue(ref notificationShowShadow, value); } }
        public bool DesktopNotificationShowShadow { get { return desktopNotificationShowShadow; } set { SetValue(ref desktopNotificationShowShadow, value); } }
        public string NotificationFontFamily { get { return NotificationFontCatalog.Normalize(notificationFontFamily); } set { SetValue(ref notificationFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string NotificationFontWeight { get { return NotificationFontCatalog.NormalizeWeight(notificationFontWeight); } set { SetValue(ref notificationFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public string NotificationTitleFontFamily { get { return NotificationFontCatalog.Normalize(notificationTitleFontFamily); } set { SetValue(ref notificationTitleFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string NotificationTitleFontWeight { get { return NotificationFontCatalog.NormalizeWeight(notificationTitleFontWeight); } set { SetValue(ref notificationTitleFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public string NotificationMessageFontFamily { get { return NotificationFontCatalog.Normalize(notificationMessageFontFamily); } set { SetValue(ref notificationMessageFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string NotificationMessageFontWeight { get { return NotificationFontCatalog.NormalizeWeight(notificationMessageFontWeight); } set { SetValue(ref notificationMessageFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public int NotificationMessageMaxLines { get { return notificationMessageMaxLines; } set { SetValue(ref notificationMessageMaxLines, System.Math.Max(1, System.Math.Min(6, value))); } }
        public string NotificationBadgePosition { get { return NormalizeBadgePosition(notificationBadgePosition); } set { SetValue(ref notificationBadgePosition, NormalizeBadgePosition(value)); } }
        public string NotificationTextAlignment { get { return NotificationFontCatalog.NormalizeAlignment(notificationTextAlignment); } set { SetValue(ref notificationTextAlignment, NotificationFontCatalog.NormalizeAlignment(value)); } }
        public string NotificationAccentMode { get { return NotificationFontCatalog.NormalizeAccentMode(notificationAccentMode); } set { SetValue(ref notificationAccentMode, NotificationFontCatalog.NormalizeAccentMode(value)); } }
        public string NotificationAnimation { get { return NotificationFontCatalog.NormalizeAnimation(notificationAnimation); } set { SetValue(ref notificationAnimation, NotificationFontCatalog.NormalizeAnimation(value)); } }
        public bool NotificationShowTitle { get { return notificationShowTitle; } set { SetValue(ref notificationShowTitle, value); } }
        public bool NotificationUppercaseTitle { get { return notificationUppercaseTitle; } set { SetValue(ref notificationUppercaseTitle, value); } }
        public string NotificationTextOrder { get { return notificationTextOrder ?? "TitleFirst"; } set { SetValue(ref notificationTextOrder, value ?? "TitleFirst"); } }
        public bool NotificationUseIndependentBorders { get { return notificationUseIndependentBorders; } set { SetValue(ref notificationUseIndependentBorders, value); } }
        public int NotificationBorderLeftThickness { get { return notificationBorderLeftThickness; } set { SetValue(ref notificationBorderLeftThickness, ClampBorder(value)); } }
        public int NotificationBorderTopThickness { get { return notificationBorderTopThickness; } set { SetValue(ref notificationBorderTopThickness, ClampBorder(value)); } }
        public int NotificationBorderRightThickness { get { return notificationBorderRightThickness; } set { SetValue(ref notificationBorderRightThickness, ClampBorder(value)); } }
        public int NotificationBorderBottomThickness { get { return notificationBorderBottomThickness; } set { SetValue(ref notificationBorderBottomThickness, ClampBorder(value)); } }
        public bool NotificationUseStateBackgroundColors { get { return notificationUseStateBackgroundColors; } set { SetValue(ref notificationUseStateBackgroundColors, value); } }
        public string NotificationConnectedBackgroundColor { get { return notificationConnectedBackgroundColor; } set { SetValue(ref notificationConnectedBackgroundColor, value); } }
        public string NotificationDisconnectedBackgroundColor { get { return notificationDisconnectedBackgroundColor; } set { SetValue(ref notificationDisconnectedBackgroundColor, value); } }
        public string NotificationWarningBackgroundColor { get { return notificationWarningBackgroundColor; } set { SetValue(ref notificationWarningBackgroundColor, value); } }
        public string NotificationLowBatteryBackgroundColor { get { return notificationLowBatteryBackgroundColor; } set { SetValue(ref notificationLowBatteryBackgroundColor, value); } }
        public bool NotificationUseBorderGradient { get { return notificationUseBorderGradient; } set { SetValue(ref notificationUseBorderGradient, value); } }
        public bool NotificationUseStateBorderColors { get { return notificationUseStateBorderColors; } set { SetValue(ref notificationUseStateBorderColors, value); } }
        public string NotificationConnectedBorderColor { get { return notificationConnectedBorderColor; } set { SetValue(ref notificationConnectedBorderColor, value); } }
        public string NotificationDisconnectedBorderColor { get { return notificationDisconnectedBorderColor; } set { SetValue(ref notificationDisconnectedBorderColor, value); } }
        public string NotificationWarningBorderColor { get { return notificationWarningBorderColor; } set { SetValue(ref notificationWarningBorderColor, value); } }
        public string NotificationLowBatteryBorderColor { get { return notificationLowBatteryBorderColor; } set { SetValue(ref notificationLowBatteryBorderColor, value); } }
        public string NotificationBorderGradientStartColor { get { return notificationBorderGradientStartColor; } set { SetValue(ref notificationBorderGradientStartColor, value); } }
        public string NotificationBorderGradientEndColor { get { return notificationBorderGradientEndColor; } set { SetValue(ref notificationBorderGradientEndColor, value); } }
        public int NotificationBorderGradientAngle { get { return notificationBorderGradientAngle; } set { SetValue(ref notificationBorderGradientAngle, NormalizeAngle(value)); } }
        public bool NotificationShowBorderGlow { get { return notificationShowBorderGlow; } set { SetValue(ref notificationShowBorderGlow, value); } }
        public string NotificationBorderGlowColor { get { return notificationBorderGlowColor; } set { SetValue(ref notificationBorderGlowColor, value); } }
        public int NotificationBorderGlowBlur { get { return notificationBorderGlowBlur; } set { SetValue(ref notificationBorderGlowBlur, System.Math.Max(0, System.Math.Min(40, value))); } }
        public int NotificationBorderGlowOpacity { get { return notificationBorderGlowOpacity; } set { SetValue(ref notificationBorderGlowOpacity, ClampPercent(value)); } }
        public int DesktopNotificationWidth { get { return desktopNotificationWidth; } set { SetValue(ref desktopNotificationWidth, value); } }
        public int DesktopNotificationScalePercent { get { return desktopNotificationScalePercent; } set { SetValue(ref desktopNotificationScalePercent, value); } }
        public int DesktopNotificationDurationMilliseconds { get { return desktopNotificationDurationMilliseconds; } set { SetValue(ref desktopNotificationDurationMilliseconds, value); } }
        public string DesktopNotificationPosition { get { return desktopNotificationPosition; } set { SetValue(ref desktopNotificationPosition, value); } }
        public string DesktopNotificationBackgroundColor { get { return desktopNotificationBackgroundColor; } set { SetValue(ref desktopNotificationBackgroundColor, value); } }
        public bool DesktopNotificationUseGradient { get { return desktopNotificationUseGradient; } set { SetValue(ref desktopNotificationUseGradient, value); } }
        public string DesktopNotificationGradientColor { get { return desktopNotificationGradientColor; } set { SetValue(ref desktopNotificationGradientColor, value); } }
        public int DesktopNotificationGradientAngle { get { return desktopNotificationGradientAngle; } set { SetValue(ref desktopNotificationGradientAngle, NormalizeAngle(value)); } }
        public bool DesktopNotificationUseBackgroundImage { get { return desktopNotificationUseBackgroundImage; } set { SetValue(ref desktopNotificationUseBackgroundImage, value); } }
        public string DesktopNotificationBackgroundImagePath { get { return desktopNotificationBackgroundImagePath; } set { SetValue(ref desktopNotificationBackgroundImagePath, value ?? string.Empty); } }
        public string DesktopNotificationBackgroundImageStretch { get { return desktopNotificationBackgroundImageStretch; } set { SetValue(ref desktopNotificationBackgroundImageStretch, value ?? "UniformToFill"); } }
        public string DesktopNotificationBackgroundImageHorizontalAlignment { get { return desktopNotificationBackgroundImageHorizontalAlignment; } set { SetValue(ref desktopNotificationBackgroundImageHorizontalAlignment, value ?? "Center"); } }
        public string DesktopNotificationBackgroundImageVerticalAlignment { get { return desktopNotificationBackgroundImageVerticalAlignment; } set { SetValue(ref desktopNotificationBackgroundImageVerticalAlignment, value ?? "Center"); } }
        public int DesktopNotificationBackgroundImageOpacity { get { return desktopNotificationBackgroundImageOpacity; } set { SetValue(ref desktopNotificationBackgroundImageOpacity, ClampPercent(value)); } }
        public int DesktopNotificationBackgroundImageTintOpacity { get { return desktopNotificationBackgroundImageTintOpacity; } set { SetValue(ref desktopNotificationBackgroundImageTintOpacity, ClampPercent(value)); } }
        public string DesktopNotificationTextColor { get { return desktopNotificationTextColor; } set { SetValue(ref desktopNotificationTextColor, value); } }
        public string DesktopNotificationSecondaryTextColor { get { return desktopNotificationSecondaryTextColor; } set { SetValue(ref desktopNotificationSecondaryTextColor, value); } }
        public string DesktopNotificationConnectedColor { get { return desktopNotificationConnectedColor; } set { SetValue(ref desktopNotificationConnectedColor, value); } }
        public string DesktopNotificationDisconnectedColor { get { return desktopNotificationDisconnectedColor; } set { SetValue(ref desktopNotificationDisconnectedColor, value); } }
        public string DesktopNotificationWarningColor { get { return desktopNotificationWarningColor; } set { SetValue(ref desktopNotificationWarningColor, value); } }
        public string DesktopNotificationLowBatteryColor { get { return desktopNotificationLowBatteryColor; } set { SetValue(ref desktopNotificationLowBatteryColor, value); } }
        public int DesktopNotificationTitleFontSize { get { return desktopNotificationTitleFontSize; } set { SetValue(ref desktopNotificationTitleFontSize, value); } }
        public int DesktopNotificationMessageFontSize { get { return desktopNotificationMessageFontSize; } set { SetValue(ref desktopNotificationMessageFontSize, value); } }
        public int DesktopNotificationIconSize { get { return desktopNotificationIconSize; } set { SetValue(ref desktopNotificationIconSize, value); } }
        public bool DesktopNotificationShowIconContainer { get { return desktopNotificationShowIconContainer; } set { SetValue(ref desktopNotificationShowIconContainer, value); } }
        public string DesktopNotificationIconContainerColor { get { return desktopNotificationIconContainerColor; } set { SetValue(ref desktopNotificationIconContainerColor, value); } }
        public string DesktopNotificationIconContainerBorderColor { get { return desktopNotificationIconContainerBorderColor; } set { SetValue(ref desktopNotificationIconContainerBorderColor, value); } }
        public int DesktopNotificationIconContainerBorderThickness { get { return desktopNotificationIconContainerBorderThickness; } set { SetValue(ref desktopNotificationIconContainerBorderThickness, System.Math.Max(0, System.Math.Min(8, value))); } }
        public int DesktopNotificationIconContainerCornerRadius { get { return desktopNotificationIconContainerCornerRadius; } set { SetValue(ref desktopNotificationIconContainerCornerRadius, System.Math.Max(0, System.Math.Min(40, value))); } }
        public int DesktopNotificationIconContainerPadding { get { return desktopNotificationIconContainerPadding; } set { SetValue(ref desktopNotificationIconContainerPadding, System.Math.Max(0, System.Math.Min(24, value))); } }
        public string DesktopNotificationIconPosition { get { return desktopNotificationIconPosition; } set { SetValue(ref desktopNotificationIconPosition, value); } }
        public int DesktopNotificationPadding { get { return desktopNotificationPadding; } set { SetValue(ref desktopNotificationPadding, value); } }
        public int DesktopNotificationElementSpacing { get { return desktopNotificationElementSpacing; } set { SetValue(ref desktopNotificationElementSpacing, value); } }
        public int DesktopNotificationIconSpacing { get { return desktopNotificationIconSpacing; } set { SetValue(ref desktopNotificationIconSpacing, value); } }
        public bool DesktopNotificationShowBorder { get { return desktopNotificationShowBorder; } set { SetValue(ref desktopNotificationShowBorder, value); } }
        public string DesktopNotificationBorderPosition { get { return desktopNotificationBorderPosition; } set { SetValue(ref desktopNotificationBorderPosition, value); } }
        public int DesktopNotificationBorderThickness { get { return desktopNotificationBorderThickness; } set { SetValue(ref desktopNotificationBorderThickness, value); } }
        public int DesktopNotificationCornerRadius { get { return desktopNotificationCornerRadius; } set { SetValue(ref desktopNotificationCornerRadius, value); } }
        public string DesktopNotificationFontFamily { get { return NotificationFontCatalog.Normalize(desktopNotificationFontFamily); } set { SetValue(ref desktopNotificationFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string DesktopNotificationFontWeight { get { return NotificationFontCatalog.NormalizeWeight(desktopNotificationFontWeight); } set { SetValue(ref desktopNotificationFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public string DesktopNotificationTitleFontFamily { get { return NotificationFontCatalog.Normalize(desktopNotificationTitleFontFamily); } set { SetValue(ref desktopNotificationTitleFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string DesktopNotificationTitleFontWeight { get { return NotificationFontCatalog.NormalizeWeight(desktopNotificationTitleFontWeight); } set { SetValue(ref desktopNotificationTitleFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public string DesktopNotificationMessageFontFamily { get { return NotificationFontCatalog.Normalize(desktopNotificationMessageFontFamily); } set { SetValue(ref desktopNotificationMessageFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string DesktopNotificationMessageFontWeight { get { return NotificationFontCatalog.NormalizeWeight(desktopNotificationMessageFontWeight); } set { SetValue(ref desktopNotificationMessageFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public int DesktopNotificationMessageMaxLines { get { return desktopNotificationMessageMaxLines; } set { SetValue(ref desktopNotificationMessageMaxLines, System.Math.Max(1, System.Math.Min(6, value))); } }
        public string DesktopNotificationBadgePosition { get { return NormalizeBadgePosition(desktopNotificationBadgePosition); } set { SetValue(ref desktopNotificationBadgePosition, NormalizeBadgePosition(value)); } }
        public string DesktopNotificationTextAlignment { get { return NotificationFontCatalog.NormalizeAlignment(desktopNotificationTextAlignment); } set { SetValue(ref desktopNotificationTextAlignment, NotificationFontCatalog.NormalizeAlignment(value)); } }
        public string DesktopNotificationAccentMode { get { return NotificationFontCatalog.NormalizeAccentMode(desktopNotificationAccentMode); } set { SetValue(ref desktopNotificationAccentMode, NotificationFontCatalog.NormalizeAccentMode(value)); } }
        public string DesktopNotificationAnimation { get { return NotificationFontCatalog.NormalizeAnimation(desktopNotificationAnimation); } set { SetValue(ref desktopNotificationAnimation, NotificationFontCatalog.NormalizeAnimation(value)); } }
        public bool DesktopNotificationShowTitle { get { return desktopNotificationShowTitle; } set { SetValue(ref desktopNotificationShowTitle, value); } }
        public bool DesktopNotificationUppercaseTitle { get { return desktopNotificationUppercaseTitle; } set { SetValue(ref desktopNotificationUppercaseTitle, value); } }
        public string DesktopNotificationTextOrder { get { return desktopNotificationTextOrder ?? "TitleFirst"; } set { SetValue(ref desktopNotificationTextOrder, value ?? "TitleFirst"); } }
        public bool DesktopNotificationUseIndependentBorders { get { return desktopNotificationUseIndependentBorders; } set { SetValue(ref desktopNotificationUseIndependentBorders, value); } }
        public int DesktopNotificationBorderLeftThickness { get { return desktopNotificationBorderLeftThickness; } set { SetValue(ref desktopNotificationBorderLeftThickness, ClampBorder(value)); } }
        public int DesktopNotificationBorderTopThickness { get { return desktopNotificationBorderTopThickness; } set { SetValue(ref desktopNotificationBorderTopThickness, ClampBorder(value)); } }
        public int DesktopNotificationBorderRightThickness { get { return desktopNotificationBorderRightThickness; } set { SetValue(ref desktopNotificationBorderRightThickness, ClampBorder(value)); } }
        public int DesktopNotificationBorderBottomThickness { get { return desktopNotificationBorderBottomThickness; } set { SetValue(ref desktopNotificationBorderBottomThickness, ClampBorder(value)); } }
        public bool DesktopNotificationUseStateBackgroundColors { get { return desktopNotificationUseStateBackgroundColors; } set { SetValue(ref desktopNotificationUseStateBackgroundColors, value); } }
        public string DesktopNotificationConnectedBackgroundColor { get { return desktopNotificationConnectedBackgroundColor; } set { SetValue(ref desktopNotificationConnectedBackgroundColor, value); } }
        public string DesktopNotificationDisconnectedBackgroundColor { get { return desktopNotificationDisconnectedBackgroundColor; } set { SetValue(ref desktopNotificationDisconnectedBackgroundColor, value); } }
        public string DesktopNotificationWarningBackgroundColor { get { return desktopNotificationWarningBackgroundColor; } set { SetValue(ref desktopNotificationWarningBackgroundColor, value); } }
        public string DesktopNotificationLowBatteryBackgroundColor { get { return desktopNotificationLowBatteryBackgroundColor; } set { SetValue(ref desktopNotificationLowBatteryBackgroundColor, value); } }
        public bool DesktopNotificationUseBorderGradient { get { return desktopNotificationUseBorderGradient; } set { SetValue(ref desktopNotificationUseBorderGradient, value); } }
        public bool DesktopNotificationUseStateBorderColors { get { return desktopNotificationUseStateBorderColors; } set { SetValue(ref desktopNotificationUseStateBorderColors, value); } }
        public string DesktopNotificationConnectedBorderColor { get { return desktopNotificationConnectedBorderColor; } set { SetValue(ref desktopNotificationConnectedBorderColor, value); } }
        public string DesktopNotificationDisconnectedBorderColor { get { return desktopNotificationDisconnectedBorderColor; } set { SetValue(ref desktopNotificationDisconnectedBorderColor, value); } }
        public string DesktopNotificationWarningBorderColor { get { return desktopNotificationWarningBorderColor; } set { SetValue(ref desktopNotificationWarningBorderColor, value); } }
        public string DesktopNotificationLowBatteryBorderColor { get { return desktopNotificationLowBatteryBorderColor; } set { SetValue(ref desktopNotificationLowBatteryBorderColor, value); } }
        public string DesktopNotificationBorderGradientStartColor { get { return desktopNotificationBorderGradientStartColor; } set { SetValue(ref desktopNotificationBorderGradientStartColor, value); } }
        public string DesktopNotificationBorderGradientEndColor { get { return desktopNotificationBorderGradientEndColor; } set { SetValue(ref desktopNotificationBorderGradientEndColor, value); } }
        public int DesktopNotificationBorderGradientAngle { get { return desktopNotificationBorderGradientAngle; } set { SetValue(ref desktopNotificationBorderGradientAngle, NormalizeAngle(value)); } }
        public bool DesktopNotificationShowBorderGlow { get { return desktopNotificationShowBorderGlow; } set { SetValue(ref desktopNotificationShowBorderGlow, value); } }
        public string DesktopNotificationBorderGlowColor { get { return desktopNotificationBorderGlowColor; } set { SetValue(ref desktopNotificationBorderGlowColor, value); } }
        public int DesktopNotificationBorderGlowBlur { get { return desktopNotificationBorderGlowBlur; } set { SetValue(ref desktopNotificationBorderGlowBlur, System.Math.Max(0, System.Math.Min(40, value))); } }
        public int DesktopNotificationBorderGlowOpacity { get { return desktopNotificationBorderGlowOpacity; } set { SetValue(ref desktopNotificationBorderGlowOpacity, ClampPercent(value)); } }
        public int OverlayScalePercent { get { return overlayScalePercent; } set { SetValue(ref overlayScalePercent, value); } }
        public string OverlayDimColor { get { return overlayDimColor; } set { SetValue(ref overlayDimColor, value); } }
        public string OverlayCardColor { get { return overlayCardColor; } set { SetValue(ref overlayCardColor, value); } }
        public bool OverlayUseGradient { get { return overlayUseGradient; } set { SetValue(ref overlayUseGradient, value); } }
        public string OverlayGradientColor { get { return overlayGradientColor; } set { SetValue(ref overlayGradientColor, value); } }
        public int OverlayGradientAngle { get { return overlayGradientAngle; } set { SetValue(ref overlayGradientAngle, NormalizeAngle(value)); } }
        public bool OverlaySceneUseGradient { get { return overlaySceneUseGradient; } set { SetValue(ref overlaySceneUseGradient, value); } }
        public string OverlaySceneGradientColor { get { return overlaySceneGradientColor; } set { SetValue(ref overlaySceneGradientColor, value); } }
        public int OverlaySceneGradientAngle { get { return overlaySceneGradientAngle; } set { SetValue(ref overlaySceneGradientAngle, NormalizeAngle(value)); } }
        public bool OverlaySceneUseBackgroundImage { get { return overlaySceneUseBackgroundImage; } set { SetValue(ref overlaySceneUseBackgroundImage, value); } }
        public string OverlaySceneBackgroundImagePath { get { return overlaySceneBackgroundImagePath; } set { SetValue(ref overlaySceneBackgroundImagePath, value ?? string.Empty); } }
        public string OverlaySceneBackgroundImageStretch { get { return overlaySceneBackgroundImageStretch; } set { SetValue(ref overlaySceneBackgroundImageStretch, value ?? "UniformToFill"); } }
        public string OverlaySceneBackgroundImageHorizontalAlignment { get { return overlaySceneBackgroundImageHorizontalAlignment; } set { SetValue(ref overlaySceneBackgroundImageHorizontalAlignment, value ?? "Center"); } }
        public string OverlaySceneBackgroundImageVerticalAlignment { get { return overlaySceneBackgroundImageVerticalAlignment; } set { SetValue(ref overlaySceneBackgroundImageVerticalAlignment, value ?? "Center"); } }
        public int OverlaySceneBackgroundImageOpacity { get { return overlaySceneBackgroundImageOpacity; } set { SetValue(ref overlaySceneBackgroundImageOpacity, ClampPercent(value)); } }
        public bool OverlaySceneUseAmbientGlows { get { return overlaySceneUseAmbientGlows; } set { SetValue(ref overlaySceneUseAmbientGlows, value); } }
        public string OverlaySceneGlow1Color { get { return overlaySceneGlow1Color; } set { SetValue(ref overlaySceneGlow1Color, value); } }
        public int OverlaySceneGlow1X { get { return overlaySceneGlow1X; } set { SetValue(ref overlaySceneGlow1X, ClampPercent(value)); } }
        public int OverlaySceneGlow1Y { get { return overlaySceneGlow1Y; } set { SetValue(ref overlaySceneGlow1Y, ClampPercent(value)); } }
        public int OverlaySceneGlow1Radius { get { return overlaySceneGlow1Radius; } set { SetValue(ref overlaySceneGlow1Radius, System.Math.Max(10, System.Math.Min(100, value))); } }
        public string OverlaySceneGlow2Color { get { return overlaySceneGlow2Color; } set { SetValue(ref overlaySceneGlow2Color, value); } }
        public int OverlaySceneGlow2X { get { return overlaySceneGlow2X; } set { SetValue(ref overlaySceneGlow2X, ClampPercent(value)); } }
        public int OverlaySceneGlow2Y { get { return overlaySceneGlow2Y; } set { SetValue(ref overlaySceneGlow2Y, ClampPercent(value)); } }
        public int OverlaySceneGlow2Radius { get { return overlaySceneGlow2Radius; } set { SetValue(ref overlaySceneGlow2Radius, System.Math.Max(10, System.Math.Min(100, value))); } }
        public string OverlaySceneGlow3Color { get { return overlaySceneGlow3Color; } set { SetValue(ref overlaySceneGlow3Color, value); } }
        public int OverlaySceneGlow3X { get { return overlaySceneGlow3X; } set { SetValue(ref overlaySceneGlow3X, ClampPercent(value)); } }
        public int OverlaySceneGlow3Y { get { return overlaySceneGlow3Y; } set { SetValue(ref overlaySceneGlow3Y, ClampPercent(value)); } }
        public int OverlaySceneGlow3Radius { get { return overlaySceneGlow3Radius; } set { SetValue(ref overlaySceneGlow3Radius, System.Math.Max(10, System.Math.Min(100, value))); } }
        public bool OverlaySceneShowGrid { get { return overlaySceneShowGrid; } set { SetValue(ref overlaySceneShowGrid, value); } }
        public string OverlaySceneGridColor { get { return overlaySceneGridColor; } set { SetValue(ref overlaySceneGridColor, value); } }
        public int OverlaySceneGridSize { get { return overlaySceneGridSize; } set { SetValue(ref overlaySceneGridSize, System.Math.Max(12, System.Math.Min(160, value))); } }
        public bool OverlayUseBackgroundImage { get { return overlayUseBackgroundImage; } set { SetValue(ref overlayUseBackgroundImage, value); } }
        public string OverlayBackgroundImagePath { get { return overlayBackgroundImagePath; } set { SetValue(ref overlayBackgroundImagePath, value ?? string.Empty); } }
        public string OverlayBackgroundImageStretch { get { return overlayBackgroundImageStretch; } set { SetValue(ref overlayBackgroundImageStretch, value ?? "UniformToFill"); } }
        public string OverlayBackgroundImageHorizontalAlignment { get { return overlayBackgroundImageHorizontalAlignment; } set { SetValue(ref overlayBackgroundImageHorizontalAlignment, value ?? "Center"); } }
        public string OverlayBackgroundImageVerticalAlignment { get { return overlayBackgroundImageVerticalAlignment; } set { SetValue(ref overlayBackgroundImageVerticalAlignment, value ?? "Center"); } }
        public int OverlayBackgroundImageOpacity { get { return overlayBackgroundImageOpacity; } set { SetValue(ref overlayBackgroundImageOpacity, ClampPercent(value)); } }
        public int OverlayBackgroundImageTintOpacity { get { return overlayBackgroundImageTintOpacity; } set { SetValue(ref overlayBackgroundImageTintOpacity, ClampPercent(value)); } }
        public string OverlayAccentColor { get { return overlayAccentColor; } set { SetValue(ref overlayAccentColor, value); } }
        public string OverlayInstructionColor { get { return overlayInstructionColor; } set { SetValue(ref overlayInstructionColor, value); } }
        public string OverlayControllerIconColor { get { return overlayControllerIconColor; } set { SetValue(ref overlayControllerIconColor, value); } }
        public string OverlayTextColor { get { return overlayTextColor; } set { SetValue(ref overlayTextColor, value); } }
        public string OverlayWarningColor { get { return overlayWarningColor; } set { SetValue(ref overlayWarningColor, value); } }
        public int OverlayTitleFontSize { get { return overlayTitleFontSize; } set { SetValue(ref overlayTitleFontSize, value); } }
        public int OverlayControllerFontSize { get { return overlayControllerFontSize; } set { SetValue(ref overlayControllerFontSize, value); } }
        public int OverlayInstructionFontSize { get { return overlayInstructionFontSize; } set { SetValue(ref overlayInstructionFontSize, value); } }
        public int OverlayStatusFontSize { get { return overlayStatusFontSize; } set { SetValue(ref overlayStatusFontSize, value); } }
        public int OverlayControllerIconSize { get { return overlayControllerIconSize; } set { SetValue(ref overlayControllerIconSize, value); } }
        public bool OverlayShowControllerContainer { get { return overlayShowControllerContainer; } set { SetValue(ref overlayShowControllerContainer, value); } }
        public string OverlayControllerContainerColor { get { return overlayControllerContainerColor; } set { SetValue(ref overlayControllerContainerColor, value); } }
        public string OverlayControllerContainerBorderColor { get { return overlayControllerContainerBorderColor; } set { SetValue(ref overlayControllerContainerBorderColor, value); } }
        public int OverlayControllerContainerBorderThickness { get { return overlayControllerContainerBorderThickness; } set { SetValue(ref overlayControllerContainerBorderThickness, System.Math.Max(0, System.Math.Min(8, value))); } }
        public int OverlayControllerContainerCornerRadius { get { return overlayControllerContainerCornerRadius; } set { SetValue(ref overlayControllerContainerCornerRadius, System.Math.Max(0, System.Math.Min(40, value))); } }
        public int OverlayControllerContainerPadding { get { return overlayControllerContainerPadding; } set { SetValue(ref overlayControllerContainerPadding, System.Math.Max(0, System.Math.Min(32, value))); } }
        public int OverlayStatusIconSize { get { return overlayStatusIconSize; } set { SetValue(ref overlayStatusIconSize, value); } }
        public bool OverlayShowControllerIcon { get { return overlayShowControllerIcon; } set { SetValue(ref overlayShowControllerIcon, value); } }
        public bool OverlayShowStatusIcon { get { return overlayShowStatusIcon; } set { SetValue(ref overlayShowStatusIcon, value); } }
        public bool OverlayShowControllerName { get { return overlayShowControllerName; } set { SetValue(ref overlayShowControllerName, value); } }
        public bool OverlayShowConnectionBadge { get { return overlayShowConnectionBadge; } set { SetValue(ref overlayShowConnectionBadge, value); } }
        public bool OverlayShowBatteryBadge { get { return overlayShowBatteryBadge; } set { SetValue(ref overlayShowBatteryBadge, value); } }
        public bool OverlayShowTitle { get { return overlayShowTitle; } set { SetValue(ref overlayShowTitle, value); } }
        public bool OverlayUppercaseTitle { get { return overlayUppercaseTitle; } set { SetValue(ref overlayUppercaseTitle, value); } }
        public bool OverlayShowInstruction { get { return overlayShowInstruction; } set { SetValue(ref overlayShowInstruction, value); } }
        public bool OverlayShowPauseStatus { get { return overlayShowPauseStatus; } set { SetValue(ref overlayShowPauseStatus, value); } }
        public bool OverlayShowDisconnectTimer { get { return overlayShowDisconnectTimer; } set { SetValue(ref overlayShowDisconnectTimer, value); } }
        public string OverlayControllerIconPosition { get { return overlayControllerIconPosition; } set { SetValue(ref overlayControllerIconPosition, value); } }
        public string OverlayCardPosition { get { return overlayCardPosition; } set { SetValue(ref overlayCardPosition, value); } }
        public string OverlayLayoutMode { get { return NormalizeLayoutMode(overlayLayoutMode); } set { SetValue(ref overlayLayoutMode, NormalizeLayoutMode(value)); } }
        public string OverlaySplitControllerSide { get { return string.Equals(overlaySplitControllerSide, "Right", System.StringComparison.OrdinalIgnoreCase) ? "Right" : "Left"; } set { SetValue(ref overlaySplitControllerSide, string.Equals(value, "Right", System.StringComparison.OrdinalIgnoreCase) ? "Right" : "Left"); } }
        public bool OverlayShowSplitDivider { get { return overlayShowSplitDivider; } set { SetValue(ref overlayShowSplitDivider, value); } }
        public string OverlaySplitDividerColor { get { return overlaySplitDividerColor; } set { SetValue(ref overlaySplitDividerColor, value); } }
        public int OverlaySplitDividerThickness { get { return overlaySplitDividerThickness; } set { SetValue(ref overlaySplitDividerThickness, System.Math.Max(0, System.Math.Min(8, value))); } }
        public bool OverlayShowIncidentBadge { get { return overlayShowIncidentBadge; } set { SetValue(ref overlayShowIncidentBadge, value); } }
        public string OverlayIncidentBadgeTextColor { get { return overlayIncidentBadgeTextColor; } set { SetValue(ref overlayIncidentBadgeTextColor, value); } }
        public string OverlayIncidentBadgeBackgroundColor { get { return overlayIncidentBadgeBackgroundColor; } set { SetValue(ref overlayIncidentBadgeBackgroundColor, value); } }
        public string OverlayIncidentBadgeBorderColor { get { return overlayIncidentBadgeBorderColor; } set { SetValue(ref overlayIncidentBadgeBorderColor, value); } }
        public int OverlayIncidentBadgeBorderThickness { get { return overlayIncidentBadgeBorderThickness; } set { SetValue(ref overlayIncidentBadgeBorderThickness, System.Math.Max(0, System.Math.Min(8, value))); } }
        public int OverlayIncidentBadgeCornerRadius { get { return overlayIncidentBadgeCornerRadius; } set { SetValue(ref overlayIncidentBadgeCornerRadius, System.Math.Max(0, System.Math.Min(24, value))); } }
        public int OverlayIncidentBadgeTextSize { get { return overlayIncidentBadgeTextSize; } set { SetValue(ref overlayIncidentBadgeTextSize, System.Math.Max(9, System.Math.Min(30, value))); } }
        public bool OverlayStatusInMetadata { get { return overlayStatusInMetadata; } set { SetValue(ref overlayStatusInMetadata, value); } }
        public string OverlayContentAlignment { get { return NormalizeContentAlignment(overlayContentAlignment); } set { SetValue(ref overlayContentAlignment, NormalizeContentAlignment(value)); } }
        public int OverlayScreenMargin { get { return overlayScreenMargin; } set { SetValue(ref overlayScreenMargin, System.Math.Max(0, System.Math.Min(160, value))); } }
        public string OverlayAnimation { get { return overlayAnimation; } set { SetValue(ref overlayAnimation, value); } }
        public string OverlayBorderPosition { get { return overlayBorderPosition; } set { SetValue(ref overlayBorderPosition, value); } }
        public int OverlayCardWidth { get { return overlayCardWidth; } set { SetValue(ref overlayCardWidth, value); } }
        public int OverlayPadding { get { return overlayPadding; } set { SetValue(ref overlayPadding, value); } }
        public int OverlayElementSpacing { get { return overlayElementSpacing; } set { SetValue(ref overlayElementSpacing, value); } }
        public bool OverlayShowBorder { get { return overlayShowBorder; } set { SetValue(ref overlayShowBorder, value); } }
        public bool OverlayShowShadow { get { return overlayShowShadow; } set { SetValue(ref overlayShowShadow, value); } }
        public int OverlayBorderThickness { get { return overlayBorderThickness; } set { SetValue(ref overlayBorderThickness, value); } }
        public int OverlayCornerRadius { get { return overlayCornerRadius; } set { SetValue(ref overlayCornerRadius, value); } }
        public string OverlayFontFamily { get { return NotificationFontCatalog.Normalize(overlayFontFamily); } set { SetValue(ref overlayFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string OverlayFontWeight { get { return NotificationFontCatalog.NormalizeWeight(overlayFontWeight); } set { SetValue(ref overlayFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public string OverlayTitleFontFamily { get { return NotificationFontCatalog.Normalize(overlayTitleFontFamily); } set { SetValue(ref overlayTitleFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string OverlayTitleFontWeight { get { return NotificationFontCatalog.NormalizeWeight(overlayTitleFontWeight); } set { SetValue(ref overlayTitleFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public string OverlayControllerFontFamily { get { return NotificationFontCatalog.Normalize(overlayControllerFontFamily); } set { SetValue(ref overlayControllerFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string OverlayControllerFontWeight { get { return NotificationFontCatalog.NormalizeWeight(overlayControllerFontWeight); } set { SetValue(ref overlayControllerFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public string OverlayInstructionFontFamily { get { return NotificationFontCatalog.Normalize(overlayInstructionFontFamily); } set { SetValue(ref overlayInstructionFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string OverlayInstructionFontWeight { get { return NotificationFontCatalog.NormalizeWeight(overlayInstructionFontWeight); } set { SetValue(ref overlayInstructionFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public string OverlayStatusFontFamily { get { return NotificationFontCatalog.Normalize(overlayStatusFontFamily); } set { SetValue(ref overlayStatusFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string OverlayStatusFontWeight { get { return NotificationFontCatalog.NormalizeWeight(overlayStatusFontWeight); } set { SetValue(ref overlayStatusFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public string OverlayConnectionBadgeTextColor { get { return overlayConnectionBadgeTextColor; } set { SetValue(ref overlayConnectionBadgeTextColor, value); } }
        public string OverlayConnectionBadgeIconColor { get { return overlayConnectionBadgeIconColor; } set { SetValue(ref overlayConnectionBadgeIconColor, value); } }
        public string OverlayConnectionBadgeBackgroundColor { get { return overlayConnectionBadgeBackgroundColor; } set { SetValue(ref overlayConnectionBadgeBackgroundColor, value); } }
        public string OverlayConnectionBadgeBorderColor { get { return overlayConnectionBadgeBorderColor; } set { SetValue(ref overlayConnectionBadgeBorderColor, value); } }
        public int OverlayConnectionBadgeBorderThickness { get { return overlayConnectionBadgeBorderThickness; } set { SetValue(ref overlayConnectionBadgeBorderThickness, value); } }
        public int OverlayConnectionBadgeCornerRadius { get { return overlayConnectionBadgeCornerRadius; } set { SetValue(ref overlayConnectionBadgeCornerRadius, value); } }
        public int OverlayConnectionBadgeIconSize { get { return overlayConnectionBadgeIconSize; } set { SetValue(ref overlayConnectionBadgeIconSize, value); } }
        public int OverlayConnectionBadgeTextSize { get { return overlayConnectionBadgeTextSize; } set { SetValue(ref overlayConnectionBadgeTextSize, value); } }
        public string OverlayBatteryBadgeTextColor { get { return overlayBatteryBadgeTextColor; } set { SetValue(ref overlayBatteryBadgeTextColor, value); } }
        public string OverlayBatteryBadgeIconColor { get { return overlayBatteryBadgeIconColor; } set { SetValue(ref overlayBatteryBadgeIconColor, value); } }
        public string OverlayBatteryBadgeBackgroundColor { get { return overlayBatteryBadgeBackgroundColor; } set { SetValue(ref overlayBatteryBadgeBackgroundColor, value); } }
        public string OverlayBatteryBadgeBorderColor { get { return overlayBatteryBadgeBorderColor; } set { SetValue(ref overlayBatteryBadgeBorderColor, value); } }
        public int OverlayBatteryBadgeBorderThickness { get { return overlayBatteryBadgeBorderThickness; } set { SetValue(ref overlayBatteryBadgeBorderThickness, value); } }
        public int OverlayBatteryBadgeCornerRadius { get { return overlayBatteryBadgeCornerRadius; } set { SetValue(ref overlayBatteryBadgeCornerRadius, value); } }
        public int OverlayBatteryBadgeIconSize { get { return overlayBatteryBadgeIconSize; } set { SetValue(ref overlayBatteryBadgeIconSize, value); } }
        public int OverlayBatteryBadgeTextSize { get { return overlayBatteryBadgeTextSize; } set { SetValue(ref overlayBatteryBadgeTextSize, value); } }
        public bool OverlayBatteryBadgeUseStateColors { get { return overlayBatteryBadgeUseStateColors; } set { SetValue(ref overlayBatteryBadgeUseStateColors, value); } }
        public string OverlayBatteryBadgeFullColor { get { return overlayBatteryBadgeFullColor; } set { SetValue(ref overlayBatteryBadgeFullColor, value); } }
        public string OverlayBatteryBadgeMediumColor { get { return overlayBatteryBadgeMediumColor; } set { SetValue(ref overlayBatteryBadgeMediumColor, value); } }
        public string OverlayBatteryBadgeLowColor { get { return overlayBatteryBadgeLowColor; } set { SetValue(ref overlayBatteryBadgeLowColor, value); } }
        public string OverlayBatteryBadgeEmptyColor { get { return overlayBatteryBadgeEmptyColor; } set { SetValue(ref overlayBatteryBadgeEmptyColor, value); } }
        public string OverlayBlockOrder { get { return overlayBlockOrder ?? "Title,Controller,Metadata,Instruction,Status"; } set { SetValue(ref overlayBlockOrder, value ?? "Title,Controller,Metadata,Instruction,Status"); } }
        public string OverlayMetadataOrientation { get { return overlayMetadataOrientation ?? "Horizontal"; } set { SetValue(ref overlayMetadataOrientation, value ?? "Horizontal"); } }
        public bool OverlayUseIndependentBorders { get { return overlayUseIndependentBorders; } set { SetValue(ref overlayUseIndependentBorders, value); } }
        public int OverlayBorderLeftThickness { get { return overlayBorderLeftThickness; } set { SetValue(ref overlayBorderLeftThickness, ClampBorder(value)); } }
        public int OverlayBorderTopThickness { get { return overlayBorderTopThickness; } set { SetValue(ref overlayBorderTopThickness, ClampBorder(value)); } }
        public int OverlayBorderRightThickness { get { return overlayBorderRightThickness; } set { SetValue(ref overlayBorderRightThickness, ClampBorder(value)); } }
        public int OverlayBorderBottomThickness { get { return overlayBorderBottomThickness; } set { SetValue(ref overlayBorderBottomThickness, ClampBorder(value)); } }
        public bool OverlayUseBorderGradient { get { return overlayUseBorderGradient; } set { SetValue(ref overlayUseBorderGradient, value); } }
        public string OverlayBorderGradientStartColor { get { return overlayBorderGradientStartColor; } set { SetValue(ref overlayBorderGradientStartColor, value); } }
        public string OverlayBorderGradientEndColor { get { return overlayBorderGradientEndColor; } set { SetValue(ref overlayBorderGradientEndColor, value); } }
        public int OverlayBorderGradientAngle { get { return overlayBorderGradientAngle; } set { SetValue(ref overlayBorderGradientAngle, NormalizeAngle(value)); } }
        public bool OverlayShowBorderGlow { get { return overlayShowBorderGlow; } set { SetValue(ref overlayShowBorderGlow, value); } }
        public string OverlayBorderGlowColor { get { return overlayBorderGlowColor; } set { SetValue(ref overlayBorderGlowColor, value); } }
        public int OverlayBorderGlowBlur { get { return overlayBorderGlowBlur; } set { SetValue(ref overlayBorderGlowBlur, System.Math.Max(0, System.Math.Min(48, value))); } }
        public int OverlayBorderGlowOpacity { get { return overlayBorderGlowOpacity; } set { SetValue(ref overlayBorderGlowOpacity, ClampPercent(value)); } }

        public bool ProtectAllActiveControllers
        {
            get { return protectAllActiveControllers; }
            set { SetValue(ref protectAllActiveControllers, value); }
        }

        public bool PauseGameOnDisconnect
        {
            get { return pauseGameOnDisconnect; }
            set { SetValue(ref pauseGameOnDisconnect, value); }
        }

        // Computed: which auto-pause strategy is active. Derives from the two bool fields.
        // Not serialized — the underlying bools are persisted.
        public string AutoPauseMode
        {
            get
            {
                if (forcePauseOfflineGames) return "OfflineOnly";
                if (pauseGameOnDisconnect) return "Always";
                return "None";
            }
            set
            {
                forcePauseOfflineGames = value == "OfflineOnly";
                pauseGameOnDisconnect = value == "Always";
                OnPropertyChanged("ForcePauseOfflineGames");
                OnPropertyChanged("PauseGameOnDisconnect");
                OnPropertyChanged("AutoPauseMode");
                OnPropertyChanged("IsAutoPauseModeNone");
                OnPropertyChanged("IsAutoPauseModeAlways");
                OnPropertyChanged("IsAutoPauseModeOfflineOnly");
            }
        }

        public bool IsAutoPauseModeNone
        {
            get { return !forcePauseOfflineGames && !pauseGameOnDisconnect; }
            set { if (value) AutoPauseMode = "None"; }
        }

        public bool IsAutoPauseModeOfflineOnly
        {
            get { return forcePauseOfflineGames; }
            set { if (value) AutoPauseMode = "OfflineOnly"; }
        }

        public bool IsAutoPauseModeAlways
        {
            get { return pauseGameOnDisconnect && !forcePauseOfflineGames; }
            set { if (value) AutoPauseMode = "Always"; }
        }

        public List<GameSessionOverride> GameSessionOverrides
        {
            get { return gameSessionOverrides; }
            set { SetValue(ref gameSessionOverrides, value ?? new List<GameSessionOverride>()); }
        }

        public GamepadTesterSettings Tester
        {
            get { return tester ?? (tester = new GamepadTesterSettings()); }
            set { SetValue(ref tester, value ?? new GamepadTesterSettings()); }
        }

        public List<ControllerProfile> ControllerProfiles
        {
            get { return controllerProfiles; }
            set { SetValue(ref controllerProfiles, value ?? new List<ControllerProfile>()); }
        }

        [DontSerialize]
        public List<ControllerIconOption> IconOptions
        {
            get
            {
                return new List<ControllerIconOption>
                {
                    Icon(ControllerIconCatalog.DefaultId, "Generic", ControllerIconCatalog.DefaultFileName),
                    Icon(ControllerIconCatalog.XboxControllerSId, "Xbox Controller S", ControllerIconCatalog.GetFileName(ControllerIconCatalog.XboxControllerSId)),
                    Icon(ControllerIconCatalog.Xbox360Id, "Xbox 360", ControllerIconCatalog.GetFileName(ControllerIconCatalog.Xbox360Id)),
                    Icon(ControllerIconCatalog.XboxOneId, "Xbox One", ControllerIconCatalog.GetFileName(ControllerIconCatalog.XboxOneId)),
                    Icon(ControllerIconCatalog.XboxSeriesId, "Xbox Series", ControllerIconCatalog.GetFileName(ControllerIconCatalog.XboxSeriesId)),
                    Icon(ControllerIconCatalog.DualShock3Id, "DualShock 3", ControllerIconCatalog.GetFileName(ControllerIconCatalog.DualShock3Id)),
                    Icon(ControllerIconCatalog.DualShockId, "DualShock", ControllerIconCatalog.GetFileName(ControllerIconCatalog.DualShockId)),
                    Icon(ControllerIconCatalog.DualSenseId, "DualSense", ControllerIconCatalog.GetFileName(ControllerIconCatalog.DualSenseId)),
                    Icon(ControllerIconCatalog.SwitchProId, "Switch Pro", ControllerIconCatalog.GetFileName(ControllerIconCatalog.SwitchProId)),
                    Icon(ControllerIconCatalog.WiiUProId, "Wii U Pro", ControllerIconCatalog.GetFileName(ControllerIconCatalog.WiiUProId)),
                    Icon(ControllerIconCatalog.StadiaId, "Stadia", ControllerIconCatalog.GetFileName(ControllerIconCatalog.StadiaId)),
                    Icon(ControllerIconCatalog.EightBitDoProId, "8BitDo Pro", ControllerIconCatalog.GetFileName(ControllerIconCatalog.EightBitDoProId)),
                    Icon(ControllerIconCatalog.EightBitDoUltimateId, "8BitDo Ultimate 2", ControllerIconCatalog.GetFileName(ControllerIconCatalog.EightBitDoUltimateId)),
                    Icon(ControllerIconCatalog.EightBitDoUltimate3Id, "8BitDo Ultimate 3", ControllerIconCatalog.GetFileName(ControllerIconCatalog.EightBitDoUltimate3Id)),
                    Icon(ControllerIconCatalog.SteamId, "Steam Controller", ControllerIconCatalog.GetFileName(ControllerIconCatalog.SteamId)),
                    Icon(ControllerIconCatalog.SteamV2Id, "Steam Controller 2", ControllerIconCatalog.GetFileName(ControllerIconCatalog.SteamV2Id))
                };
            }
        }

        internal bool SyncControllerProfiles(IEnumerable<ControllerDeviceSnapshot> controllers)
        {
            var changed = false;
            foreach (var controller in controllers ?? Enumerable.Empty<ControllerDeviceSnapshot>())
            {
                if (!ControllerDisplayHold.ShouldSyncProfile(controller))
                {
                    continue;
                }

                var hardwareId = string.IsNullOrWhiteSpace(controller.HardwareId)
                    ? controller.ControllerId
                    : controller.HardwareId;
                var profile = ControllerProfiles.FirstOrDefault(a => a.HardwareId == hardwareId);
                if (profile == null)
                {
                    profile = new ControllerProfile
                    {
                        HardwareId = hardwareId,
                        DetectedName = controller.DetectedName ?? controller.Name,
                        CustomName = controller.DetectedName ?? controller.Name,
                        IconId = SuggestIcon(controller)
                    };
                    ControllerProfiles.Add(profile);
                    changed = true;
                }
                else
                {
                    var detectedName = controller.DetectedName ?? controller.Name;
                    if (!string.Equals(profile.DetectedName, detectedName,
                        System.StringComparison.Ordinal))
                    {
                        profile.DetectedName = detectedName;
                        changed = true;
                    }
                    if (profile.CustomName == null)
                    {
                        profile.CustomName = profile.DetectedName;
                        changed = true;
                    }

                    var nextIcon = ControllerIconCatalog.IsLegacy(profile.IconId)
                        ? ControllerIconCatalog.Suggest(controller)
                        : ControllerIconCatalog.Normalize(profile.IconId);
                    if (!string.Equals(profile.IconId, nextIcon, System.StringComparison.Ordinal))
                    {
                        profile.IconId = nextIcon;
                        changed = true;
                    }
                }

                if (string.Equals(controller.ProviderId, XInputProvider.ProviderId,
                    System.StringComparison.OrdinalIgnoreCase) && controller.ProviderInstanceId >= 0)
                {
                    foreach (var other in ControllerProfiles.Where(a => !object.ReferenceEquals(a, profile) &&
                        a.LastKnownXInputSlot == controller.ProviderInstanceId))
                    {
                        other.LastKnownXInputSlot = null;
                        changed = true;
                    }
                    if (profile.LastKnownXInputSlot != controller.ProviderInstanceId)
                    {
                        profile.LastKnownXInputSlot = controller.ProviderInstanceId;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        internal ControllerProfile GetControllerProfile(string hardwareId)
        {
            int xInputSlot;
            if (TryGetXInputSlot(hardwareId, out xInputSlot))
            {
                var slotProfile = ControllerProfiles.FirstOrDefault(a =>
                    a.LastKnownXInputSlot == xInputSlot && !IsSyntheticXInputProfile(a));
                if (slotProfile != null)
                {
                    return slotProfile;
                }
            }

            return ControllerProfiles.FirstOrDefault(a => string.Equals(a.HardwareId, hardwareId,
                System.StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryGetXInputSlot(string value, out int slot)
        {
            slot = -1;
            const string prefix = "xinput:slot:";
            return !string.IsNullOrWhiteSpace(value) &&
                value.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(value.Substring(prefix.Length), out slot) && slot >= 0 && slot < 4;
        }

        private static bool IsSyntheticXInputProfile(ControllerProfile profile)
        {
            int ignored;
            return profile != null && TryGetXInputSlot(profile.HardwareId, out ignored);
        }

        internal SessionProtectionPolicy GetSessionPolicy(System.Guid gameId)
        {
            var gameOverride = GameSessionOverrides.FirstOrDefault(a => a.GameId == gameId);
            var hasSessionOverride = HasSessionOverride(gameOverride);
            var hasPauseOverride = HasPauseOverride(gameOverride);
            return new SessionProtectionPolicy
            {
                Enabled = !hasSessionOverride ? EnableSessionTracking : gameOverride.EnableSessionTracking,
                GracePeriodMilliseconds = !hasSessionOverride ? DisconnectGracePeriodMilliseconds :
                    gameOverride.DisconnectGracePeriodMilliseconds,
                AllowControllerTakeover = true,
                ProtectAllActiveControllers = !hasSessionOverride ? ProtectAllActiveControllers :
                    gameOverride.ProtectAllActiveControllers,
                PauseGameOnDisconnect = !hasPauseOverride ? PauseGameOnDisconnect :
                    gameOverride.PauseGameOnDisconnect,
                ForcePauseOfflineGames = !hasPauseOverride ? ForcePauseOfflineGames :
                    gameOverride.ForcePauseOfflineGames,
                IsGameOverride = hasSessionOverride || hasPauseOverride,
                HasSessionOverride = hasSessionOverride,
                HasPauseOverride = hasPauseOverride
            };
        }

        internal void SetGameOverride(System.Guid gameId, string gameName, bool enabled,
            bool protectAllControllers)
        {
            var value = GameSessionOverrides.FirstOrDefault(a => a.GameId == gameId);
            if (value == null)
            {
                value = new GameSessionOverride
                {
                    GameId = gameId,
                    PauseGameOnDisconnect = enabled && PauseGameOnDisconnect,
                    OverrideSessionProtection = true,
                    OverridePauseProfile = false
                };
                GameSessionOverrides.Add(value);
            }

            value.GameName = gameName;
            value.EnableSessionTracking = enabled;
            value.DisconnectGracePeriodMilliseconds = DisconnectGracePeriodMilliseconds;
            value.AllowControllerTakeover = true;
            value.ProtectAllActiveControllers = protectAllControllers;
            value.OverrideSessionProtection = true;
        }

        internal void SetGamePauseOverride(System.Guid gameId, string gameName, string autoPauseMode)
        {
            var pauseGame = autoPauseMode == "Always";
            var forcePauseOffline = autoPauseMode == "OfflineOnly";
            var value = GameSessionOverrides.FirstOrDefault(a => a.GameId == gameId);
            if (value == null)
            {
                value = new GameSessionOverride
                {
                    GameId = gameId,
                    EnableSessionTracking = EnableSessionTracking,
                    DisconnectGracePeriodMilliseconds = DisconnectGracePeriodMilliseconds,
                    AllowControllerTakeover = AllowControllerTakeover,
                    ProtectAllActiveControllers = ProtectAllActiveControllers,
                    OverrideSessionProtection = false,
                    OverridePauseProfile = true
                };
                GameSessionOverrides.Add(value);
            }

            value.GameName = gameName;
            value.PauseGameOnDisconnect = pauseGame;
            value.ForcePauseOfflineGames = forcePauseOffline;
            value.OverridePauseProfile = true;
        }

        internal void UseGlobalSessionPolicy(System.Guid gameId)
        {
            var value = GameSessionOverrides.FirstOrDefault(a => a.GameId == gameId);
            if (value == null)
            {
                return;
            }

            value.OverrideSessionProtection = false;
            RemoveEmptyOverride(value);
        }

        internal void UseGlobalPausePolicy(System.Guid gameId)
        {
            var value = GameSessionOverrides.FirstOrDefault(a => a.GameId == gameId);
            if (value == null)
            {
                return;
            }

            value.OverridePauseProfile = false;
            RemoveEmptyOverride(value);
        }

        internal void RemoveGameOverride(System.Guid gameId)
        {
            GameSessionOverrides.RemoveAll(a => a.GameId == gameId);
        }

        private void RemoveEmptyOverride(GameSessionOverride value)
        {
            if (!HasSessionOverride(value) && !HasPauseOverride(value))
            {
                GameSessionOverrides.Remove(value);
            }
        }

        private void MigrateSettings()
        {
            var originalSchema = SettingsSchemaVersion;

            if (SettingsSchemaVersion < 3)
            {
                // Versions before 0.4 did not distinguish an intentional local-multiplayer choice
                // from the old protect-all default. Prefer automatic ownership for ambiguous
                // legacy values; explicit overrides written by 0.3.2 already carry flags.
                ProtectAllActiveControllers = false;
                AllowControllerTakeover = true;
                foreach (var value in GameSessionOverrides)
                {
                    value.AllowControllerTakeover = true;
                    if (!value.OverrideSessionProtection.HasValue)
                    {
                        value.ProtectAllActiveControllers = false;
                        value.OverrideSessionProtection = false;
                    }
                }
                SettingsSchemaVersion = 3;
            }

            if (SettingsSchemaVersion < 4)
            {
                // 0.5.1 makes the global scope adaptive. Preserve explicit per-game local
                // multiplayer overrides, but remove the now-hidden global force-local value.
                ProtectAllActiveControllers = false;
                SettingsSchemaVersion = 4;
            }

            if (SettingsSchemaVersion < 5)
            {
                topPanelControllerMode = showPrimaryControllerInTopPanel
                    ? TopPanelControllerModePrimary
                    : TopPanelControllerModeHidden;
                SettingsSchemaVersion = 5;
            }

            if (SettingsSchemaVersion < 6)
            {
                appearancePreset = SettingsAppearance.Normalize(appearancePreset);
                SettingsSchemaVersion = 6;
            }

            if (SettingsSchemaVersion < 7)
            {
                launchFullscreenOnGuideButton = false;
                SettingsSchemaVersion = 7;
            }

            if (originalSchema > 0 && originalSchema < 8)
            {
                // Existing installs already configured; don't force the first-run wizard.
                setupWizardCompleted = true;
            }

            if (SettingsSchemaVersion < 8)
            {
                SettingsSchemaVersion = 8;
            }

            if (SettingsSchemaVersion < 9)
            {
                enableNotificationSounds = true;
                notificationSoundPack = NotificationSoundCatalog.ModernCrystal;
                playSoundOnConnected = false;
                playSoundOnDisconnected = false;
                playSoundOnLowBattery = true;
                playSoundOnWarning = true;
                notificationSoundVolume = 0.7;
                // Existing installs keep current toast/overlay styling as Custom.
                if (originalSchema > 0)
                {
                    notificationStylePreset = NotificationStylePresets.Custom;
                    overlayStylePreset = OverlayStylePresets.Custom;
                }

                SettingsSchemaVersion = 9;
            }

            if (SettingsSchemaVersion < 10)
            {
                SettingsSchemaVersion = 10;
            }

            if (SettingsSchemaVersion < 11)
            {
                notificationFontFamily = NotificationFontCatalog.SystemDefault;
                desktopNotificationFontFamily = NotificationFontCatalog.SystemDefault;
                overlayFontFamily = NotificationFontCatalog.SystemDefault;
                notificationFontWeight = "SemiBold";
                desktopNotificationFontWeight = "SemiBold";
                overlayFontWeight = "SemiBold";
                notificationTextAlignment = "Left";
                desktopNotificationTextAlignment = "Left";
                notificationAccentMode = "IconAndBorder";
                desktopNotificationAccentMode = "IconAndBorder";
                notificationAnimation = "Fade";
                desktopNotificationAnimation = "Fade";
                notificationShowTitle = true;
                desktopNotificationShowTitle = true;
                if (originalSchema == 0)
                {
                    NotificationStylePresets.Apply(this, NotificationStylePresets.Soft);
                    OverlayStylePresets.Apply(this, OverlayStylePresets.Soft);
                }
                SettingsSchemaVersion = 11;
            }

            if (SettingsSchemaVersion < 12)
            {
                overlayTitleFontFamily = overlayFontFamily;
                overlayTitleFontWeight = overlayFontWeight;
                overlayControllerFontFamily = overlayFontFamily;
                overlayControllerFontWeight = overlayFontWeight;
                overlayInstructionFontFamily = overlayFontFamily;
                overlayInstructionFontWeight = overlayFontWeight;
                overlayStatusFontFamily = overlayFontFamily;
                overlayStatusFontWeight = overlayFontWeight;
                SettingsSchemaVersion = 12;
            }

            if (SettingsSchemaVersion < 13)
            {
                notificationUseBackgroundImage = false;
                desktopNotificationUseBackgroundImage = false;
                notificationBackgroundImageOpacity = 45;
                desktopNotificationBackgroundImageOpacity = 45;
                notificationBackgroundImageTintOpacity = 45;
                desktopNotificationBackgroundImageTintOpacity = 45;
                SettingsSchemaVersion = 13;
            }

            if (SettingsSchemaVersion < 14)
            {
                // Preserve the old visual gap while separating icon-to-content spacing from
                // title/message spacing. Existing sound behavior remains enabled in both modes.
                notificationIconSpacing = System.Math.Max(8,
                    System.Math.Max(notificationElementSpacing,
                        (int)System.Math.Round(notificationPadding * 0.75)));
                desktopNotificationIconSpacing = System.Math.Max(8,
                    System.Math.Max(desktopNotificationElementSpacing,
                        (int)System.Math.Round(desktopNotificationPadding * 0.75)));
                // The former master switch is represented by both explicit destinations now.
                // Keep the old property enabled internally for profile/settings compatibility.
                enableDesktopNotificationSounds = enableNotificationSounds;
                enableFullscreenNotificationSounds = enableNotificationSounds;
                enableNotificationSounds = true;
                notificationPreviewWithSound = false;
                SettingsSchemaVersion = 14;
            }

            if (SettingsSchemaVersion < 15)
            {
                // Split typography starts from the exact legacy face so upgrades render the
                // same title and message until the user opts into the advanced controls.
                notificationTitleFontFamily = notificationFontFamily;
                notificationTitleFontWeight = notificationFontWeight;
                notificationMessageFontFamily = notificationFontFamily;
                notificationMessageFontWeight = notificationFontWeight;
                desktopNotificationTitleFontFamily = desktopNotificationFontFamily;
                desktopNotificationTitleFontWeight = desktopNotificationFontWeight;
                desktopNotificationMessageFontFamily = desktopNotificationFontFamily;
                desktopNotificationMessageFontWeight = desktopNotificationFontWeight;
                notificationMessageMaxLines = 2;
                desktopNotificationMessageMaxLines = 2;
                notificationBadgePosition = "TopRight";
                desktopNotificationBadgePosition = "TopRight";
                overlayContentAlignment = "Center";
                overlayScreenMargin = 42;
                SettingsSchemaVersion = 15;
            }

            if (SettingsSchemaVersion < 16)
            {
                // A disabled gradient is pixel-compatible with every previous style.
                notificationUseGradient = false;
                notificationGradientColor = notificationBackgroundColor;
                notificationGradientAngle = 0;
                notificationUppercaseTitle = false;
                desktopNotificationUseGradient = false;
                desktopNotificationGradientColor = desktopNotificationBackgroundColor;
                desktopNotificationGradientAngle = 0;
                desktopNotificationUppercaseTitle = false;
                overlayUseGradient = false;
                overlayGradientColor = overlayCardColor;
                overlayGradientAngle = 0;
                overlayUppercaseTitle = false;
                SettingsSchemaVersion = 16;
            }

            if (SettingsSchemaVersion < 17)
            {
                notificationShowIconContainer = false;
                desktopNotificationShowIconContainer = false;
                overlayLayoutMode = "Standard";
                overlayUseBackgroundImage = false;
                overlayBackgroundImagePath = string.Empty;
                overlayShowControllerContainer = false;
                SettingsSchemaVersion = 17;
            }

            if (SettingsSchemaVersion < 18)
            {
                notificationTextOrder = "TitleFirst";
                desktopNotificationTextOrder = "TitleFirst";
                notificationConnectedBackgroundColor = notificationBackgroundColor;
                notificationDisconnectedBackgroundColor = notificationBackgroundColor;
                notificationWarningBackgroundColor = notificationBackgroundColor;
                notificationLowBatteryBackgroundColor = notificationBackgroundColor;
                desktopNotificationConnectedBackgroundColor = desktopNotificationBackgroundColor;
                desktopNotificationDisconnectedBackgroundColor = desktopNotificationBackgroundColor;
                desktopNotificationWarningBackgroundColor = desktopNotificationBackgroundColor;
                desktopNotificationLowBatteryBackgroundColor = desktopNotificationBackgroundColor;
                overlayBlockOrder = "Title,Controller,Metadata,Instruction,Status";
                overlayMetadataOrientation = "Horizontal";
                SettingsSchemaVersion = 18;
            }

            if (SettingsSchemaVersion < 19)
            {
                notificationUseBorderGradient = false;
                notificationShowBorderGlow = false;
                desktopNotificationUseBorderGradient = false;
                desktopNotificationShowBorderGlow = false;
                overlayUseBorderGradient = false;
                overlayShowBorderGlow = false;
                SettingsSchemaVersion = 19;
            }

            if (SettingsSchemaVersion < 20)
            {
                desktopNotificationStylePreset = notificationStylePreset;
                savedCustomDesktopNotificationStyle = new Dictionary<string, string>();
                SettingsSchemaVersion = 20;
            }

            if (SettingsSchemaVersion < 21)
            {
                // The new state-aware gradient is opt-in. Add its disabled value to saved
                // Custom snapshots so an upgrade does not create a false unsaved-style warning.
                notificationUseStateBorderColors = false;
                desktopNotificationUseStateBorderColors = false;
                notificationConnectedBorderColor = notificationConnectedColor;
                notificationDisconnectedBorderColor = notificationDisconnectedColor;
                notificationWarningBorderColor = notificationWarningColor;
                notificationLowBatteryBorderColor = notificationLowBatteryColor;
                desktopNotificationConnectedBorderColor = desktopNotificationConnectedColor;
                desktopNotificationDisconnectedBorderColor = desktopNotificationDisconnectedColor;
                desktopNotificationWarningBorderColor = desktopNotificationWarningColor;
                desktopNotificationLowBatteryBorderColor = desktopNotificationLowBatteryColor;
                if (savedCustomNotificationStyle != null && savedCustomNotificationStyle.Count > 0)
                {
                    savedCustomNotificationStyle["NotificationUseStateBorderColors"] = "False";
                    savedCustomNotificationStyle["NotificationConnectedBorderColor"] = notificationConnectedBorderColor;
                    savedCustomNotificationStyle["NotificationDisconnectedBorderColor"] = notificationDisconnectedBorderColor;
                    savedCustomNotificationStyle["NotificationWarningBorderColor"] = notificationWarningBorderColor;
                    savedCustomNotificationStyle["NotificationLowBatteryBorderColor"] = notificationLowBatteryBorderColor;
                }
                if (savedCustomDesktopNotificationStyle != null && savedCustomDesktopNotificationStyle.Count > 0)
                {
                    savedCustomDesktopNotificationStyle["DesktopNotificationUseStateBorderColors"] = "False";
                    savedCustomDesktopNotificationStyle["DesktopNotificationConnectedBorderColor"] = desktopNotificationConnectedBorderColor;
                    savedCustomDesktopNotificationStyle["DesktopNotificationDisconnectedBorderColor"] = desktopNotificationDisconnectedBorderColor;
                    savedCustomDesktopNotificationStyle["DesktopNotificationWarningBorderColor"] = desktopNotificationWarningBorderColor;
                    savedCustomDesktopNotificationStyle["DesktopNotificationLowBatteryBorderColor"] = desktopNotificationLowBatteryBorderColor;
                }
                SettingsSchemaVersion = 21;
            }

            if (SettingsSchemaVersion < 22)
            {
                overlayShowDisconnectTimer = false;
                SettingsSchemaVersion = 22;
            }

            topPanelControllerMode = NormalizeTopPanelControllerMode(topPanelControllerMode);
            creatorThemeUpdatePolicy = NormalizeCreatorThemeUpdatePolicy(creatorThemeUpdatePolicy);
            appearancePreset = SettingsAppearance.Normalize(appearancePreset);
            notificationStylePreset = NotificationStylePresets.Normalize(notificationStylePreset);
            desktopNotificationStylePreset = NotificationStylePresets.Normalize(desktopNotificationStylePreset);
            overlayStylePreset = OverlayStylePresets.Normalize(overlayStylePreset);
            notificationSoundPack = NotificationSoundCatalog.Normalize(notificationSoundPack);
            notificationFontFamily = NotificationFontCatalog.Normalize(notificationFontFamily);
            desktopNotificationFontFamily = NotificationFontCatalog.Normalize(desktopNotificationFontFamily);
            overlayFontFamily = NotificationFontCatalog.Normalize(overlayFontFamily);
            notificationFontWeight = NotificationFontCatalog.NormalizeWeight(notificationFontWeight);
            desktopNotificationFontWeight = NotificationFontCatalog.NormalizeWeight(desktopNotificationFontWeight);
            notificationTitleFontFamily = NotificationFontCatalog.Normalize(notificationTitleFontFamily);
            notificationTitleFontWeight = NotificationFontCatalog.NormalizeWeight(notificationTitleFontWeight);
            notificationMessageFontFamily = NotificationFontCatalog.Normalize(notificationMessageFontFamily);
            notificationMessageFontWeight = NotificationFontCatalog.NormalizeWeight(notificationMessageFontWeight);
            desktopNotificationTitleFontFamily = NotificationFontCatalog.Normalize(desktopNotificationTitleFontFamily);
            desktopNotificationTitleFontWeight = NotificationFontCatalog.NormalizeWeight(desktopNotificationTitleFontWeight);
            desktopNotificationMessageFontFamily = NotificationFontCatalog.Normalize(desktopNotificationMessageFontFamily);
            desktopNotificationMessageFontWeight = NotificationFontCatalog.NormalizeWeight(desktopNotificationMessageFontWeight);
            notificationBadgePosition = NormalizeBadgePosition(notificationBadgePosition);
            desktopNotificationBadgePosition = NormalizeBadgePosition(desktopNotificationBadgePosition);
            overlayContentAlignment = NormalizeContentAlignment(overlayContentAlignment);
            overlayLayoutMode = NormalizeLayoutMode(overlayLayoutMode);
            overlayFontWeight = NotificationFontCatalog.NormalizeWeight(overlayFontWeight);
            overlayTitleFontFamily = NotificationFontCatalog.Normalize(overlayTitleFontFamily);
            overlayTitleFontWeight = NotificationFontCatalog.NormalizeWeight(overlayTitleFontWeight);
            overlayControllerFontFamily = NotificationFontCatalog.Normalize(overlayControllerFontFamily);
            overlayControllerFontWeight = NotificationFontCatalog.NormalizeWeight(overlayControllerFontWeight);
            overlayInstructionFontFamily = NotificationFontCatalog.Normalize(overlayInstructionFontFamily);
            overlayInstructionFontWeight = NotificationFontCatalog.NormalizeWeight(overlayInstructionFontWeight);
            overlayStatusFontFamily = NotificationFontCatalog.Normalize(overlayStatusFontFamily);
            overlayStatusFontWeight = NotificationFontCatalog.NormalizeWeight(overlayStatusFontWeight);
            notificationTextAlignment = NotificationFontCatalog.NormalizeAlignment(notificationTextAlignment);
            desktopNotificationTextAlignment = NotificationFontCatalog.NormalizeAlignment(desktopNotificationTextAlignment);
            notificationAccentMode = NotificationFontCatalog.NormalizeAccentMode(notificationAccentMode);
            desktopNotificationAccentMode = NotificationFontCatalog.NormalizeAccentMode(desktopNotificationAccentMode);
            notificationAnimation = NotificationFontCatalog.NormalizeAnimation(notificationAnimation);
            desktopNotificationAnimation = NotificationFontCatalog.NormalizeAnimation(desktopNotificationAnimation);
            NotificationSoundVolume = notificationSoundVolume;
            if (savedCustomNotificationStyle == null)
                savedCustomNotificationStyle = new Dictionary<string, string>();
            if (savedCustomDesktopNotificationStyle == null)
                savedCustomDesktopNotificationStyle = new Dictionary<string, string>();
            // Capture only after normalization. Capturing earlier made the untouched Custom style
            // immediately look dirty when legacy font weights or alignments were normalized.
            if (notificationStylePreset == NotificationStylePresets.Custom &&
                savedCustomNotificationStyle.Count == 0)
                savedCustomNotificationStyle = NotificationStyleState.CaptureFullscreen(this);
            if (desktopNotificationStylePreset == NotificationStylePresets.Custom &&
                savedCustomDesktopNotificationStyle.Count == 0)
                savedCustomDesktopNotificationStyle = NotificationStyleState.CaptureDesktop(this);
            NotifyCreatorThemeStateChanged();
        }

        private static string NormalizeTopPanelControllerMode(string value)
        {
            if (string.Equals(value, TopPanelControllerModeDefault, System.StringComparison.OrdinalIgnoreCase))
            {
                return TopPanelControllerModeDefault;
            }

            if (string.Equals(value, TopPanelControllerModePrimary, System.StringComparison.OrdinalIgnoreCase))
            {
                return TopPanelControllerModePrimary;
            }

            return TopPanelControllerModeHidden;
        }

        private static string NormalizeCreatorThemeUpdatePolicy(string value)
        {
            if (string.Equals(value, CreatorThemeUpdatePolicyManual,
                System.StringComparison.OrdinalIgnoreCase))
                return CreatorThemeUpdatePolicyManual;
            if (string.Equals(value, CreatorThemeUpdatePolicyDaily,
                System.StringComparison.OrdinalIgnoreCase))
                return CreatorThemeUpdatePolicyDaily;
            return CreatorThemeUpdatePolicyStartup;
        }

        private static int ClampPercent(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }

        private static int ClampBorder(int value)
        {
            return System.Math.Max(0, System.Math.Min(12, value));
        }

        private static int NormalizeAngle(int value)
        {
            value %= 360;
            return value < 0 ? value + 360 : value;
        }

        private static string NormalizeBadgePosition(string value)
        {
            return string.Equals(value, "TopLeft", System.StringComparison.OrdinalIgnoreCase)
                ? "TopLeft" : "TopRight";
        }

        private static string NormalizeContentAlignment(string value)
        {
            if (string.Equals(value, "Left", System.StringComparison.OrdinalIgnoreCase)) return "Left";
            if (string.Equals(value, "Right", System.StringComparison.OrdinalIgnoreCase)) return "Right";
            return "Center";
        }

        private static string NormalizeLayoutMode(string value)
        {
            if (string.Equals(value, "Split", System.StringComparison.OrdinalIgnoreCase)) return "Split";
            if (string.Equals(value, "Hero", System.StringComparison.OrdinalIgnoreCase)) return "Hero";
            if (string.Equals(value, "Alert", System.StringComparison.OrdinalIgnoreCase)) return "Alert";
            return "Standard";
        }

        private static bool HasSessionOverride(GameSessionOverride value)
        {
            return value != null && (value.OverrideSessionProtection ?? true);
        }

        private static bool HasPauseOverride(GameSessionOverride value)
        {
            return value != null && (value.OverridePauseProfile ?? true);
        }

        internal void Attach(ControllerSessionManagerPlugin sourcePlugin)
        {
            plugin = sourcePlugin;
        }

        public void RefreshCreatorThemeState()
        {
            NotifyCreatorThemeStateChanged();
        }

        private void NotifyCreatorThemeStateChanged()
        {
            OnPropertyChanged("IsFullscreenNotificationCreatorThemeActive");
            OnPropertyChanged("IsDesktopNotificationCreatorThemeActive");
            OnPropertyChanged("IsCreatorNotificationThemeActive");
            OnPropertyChanged("CanEditFullscreenNotificationStyle");
            OnPropertyChanged("CanEditDesktopNotificationStyle");
            OnPropertyChanged("IsOverlayCreatorThemeActive");
            OnPropertyChanged("CanEditOverlayStyle");
            OnPropertyChanged("CanEditNotificationAudio");
            OnPropertyChanged("CanCopyNotificationStyles");
        }

        public bool HasSavedCustomNotificationStyle
        {
            get { return savedCustomNotificationStyle != null && savedCustomNotificationStyle.Count > 0; }
        }

        public bool HasUnsavedCustomNotificationStyle
        {
            get
            {
                return NotificationStylePreset == NotificationStylePresets.Custom &&
                    !NotificationStyleState.MatchesFullscreen(this, savedCustomNotificationStyle);
            }
        }

        public bool HasSavedCustomDesktopNotificationStyle
        {
            get { return savedCustomDesktopNotificationStyle != null && savedCustomDesktopNotificationStyle.Count > 0; }
        }

        public bool HasUnsavedCustomDesktopNotificationStyle
        {
            get
            {
                return DesktopNotificationStylePreset == NotificationStylePresets.Custom &&
                    !NotificationStyleState.MatchesDesktop(this, savedCustomDesktopNotificationStyle);
            }
        }

        public void SaveCurrentNotificationStyleAsCustom()
        {
            savedCustomNotificationStyle = NotificationStyleState.CaptureFullscreen(this);
            NotificationStylePreset = NotificationStylePresets.Custom;
        }

        public bool RestoreSavedCustomNotificationStyle()
        {
            if (!HasSavedCustomNotificationStyle)
            {
                return false;
            }
            NotificationStyleState.ApplyFullscreen(this, savedCustomNotificationStyle);
            NotificationStylePreset = NotificationStylePresets.Custom;
            return true;
        }

        public void SaveCurrentDesktopNotificationStyleAsCustom()
        {
            savedCustomDesktopNotificationStyle = NotificationStyleState.CaptureDesktop(this);
            DesktopNotificationStylePreset = NotificationStylePresets.Custom;
        }

        public bool RestoreSavedCustomDesktopNotificationStyle()
        {
            if (!HasSavedCustomDesktopNotificationStyle) return false;
            NotificationStyleState.ApplyDesktop(this, savedCustomDesktopNotificationStyle);
            DesktopNotificationStylePreset = NotificationStylePresets.Custom;
            return true;
        }

        public void BeginEdit()
        {
            editingClone = Clone();
            if (plugin != null)
            {
                plugin.RefreshControllers();
            }
        }

        public void CancelEdit()
        {
            if (editingClone != null)
            {
                CopyFrom(editingClone);
            }
            if (plugin != null)
            {
                plugin.QueueCustomNotificationSoundCleanup(this);
            }
        }

        public void EndEdit()
        {
            if (plugin != null)
            {
                var sidebarChanged = editingClone != null
                    && editingClone.Tester != null
                    && Tester != null
                    && Tester.ShowSidebarItem != editingClone.Tester.ShowSidebarItem;
                if (NotificationStylePreset == NotificationStylePresets.Custom)
                {
                    SaveCurrentNotificationStyleAsCustom();
                }
                if (DesktopNotificationStylePreset == NotificationStylePresets.Custom)
                {
                    SaveCurrentDesktopNotificationStyleAsCustom();
                }
                Tester.Normalize();
                plugin.SavePluginSettings(this);
                plugin.QueueCustomNotificationSoundCleanup(this);
                plugin.ApplySettings();
                if (sidebarChanged)
                {
                    plugin.OfferPlayniteRestartForSidebarChange();
                }
            }
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            if (ReconciliationIntervalSeconds < 2 || ReconciliationIntervalSeconds > 60)
            {
                errors.Add(plugin == null
                    ? "The reconciliation interval must be between 2 and 60 seconds."
                    : plugin.Loc("LOCCSM_ValidationInterval"));
            }

            if (DisconnectGracePeriodMilliseconds < 250 || DisconnectGracePeriodMilliseconds > 10000)
            {
                errors.Add(plugin == null
                    ? "The disconnect grace period must be between 250 and 10000 milliseconds."
                    : plugin.Loc("LOCCSM_ValidationGracePeriod"));
            }

            if (NotificationWidth < 300 || NotificationWidth > 900 ||
                NotificationScalePercent < 80 || NotificationScalePercent > 160 ||
                NotificationDurationMilliseconds < 2000 || NotificationDurationMilliseconds > 15000 ||
                NotificationTitleFontSize < 12 || NotificationTitleFontSize > 36 ||
                NotificationMessageFontSize < 10 || NotificationMessageFontSize > 30 ||
                NotificationIconSize < 16 || NotificationIconSize > 128 ||
                NotificationIconContainerBorderThickness < 0 || NotificationIconContainerBorderThickness > 8 ||
                NotificationIconContainerCornerRadius < 0 || NotificationIconContainerCornerRadius > 40 ||
                NotificationIconContainerPadding < 0 || NotificationIconContainerPadding > 24 ||
                NotificationPadding < 0 || NotificationPadding > 40 ||
                NotificationElementSpacing < 0 || NotificationElementSpacing > 40 ||
                NotificationIconSpacing < 0 || NotificationIconSpacing > 40 ||
                NotificationMessageMaxLines < 1 || NotificationMessageMaxLines > 6 ||
                NotificationBorderThickness < 0 || NotificationBorderThickness > 10 ||
                NotificationCornerRadius < 0 || NotificationCornerRadius > 40 ||
                NotificationScreenMargin < 8 || NotificationScreenMargin > 64 ||
                DesktopNotificationIconSize < 16 || DesktopNotificationIconSize > 128 ||
                DesktopNotificationIconContainerBorderThickness < 0 || DesktopNotificationIconContainerBorderThickness > 8 ||
                DesktopNotificationIconContainerCornerRadius < 0 || DesktopNotificationIconContainerCornerRadius > 40 ||
                DesktopNotificationIconContainerPadding < 0 || DesktopNotificationIconContainerPadding > 24 ||
                DesktopNotificationPadding < 0 || DesktopNotificationPadding > 40 ||
                DesktopNotificationElementSpacing < 0 || DesktopNotificationElementSpacing > 40 ||
                DesktopNotificationIconSpacing < 0 || DesktopNotificationIconSpacing > 40 ||
                DesktopNotificationMessageMaxLines < 1 || DesktopNotificationMessageMaxLines > 6 ||
                DesktopNotificationScreenMargin < 8 || DesktopNotificationScreenMargin > 64 ||
                OverlayScalePercent < 80 || OverlayScalePercent > 140 ||
                OverlayTitleFontSize < 18 || OverlayTitleFontSize > 64 ||
                OverlayControllerFontSize < 12 || OverlayControllerFontSize > 48 ||
                OverlayInstructionFontSize < 12 || OverlayInstructionFontSize > 40 ||
                OverlayStatusFontSize < 10 || OverlayStatusFontSize > 30 ||
                OverlayControllerIconSize < 16 || OverlayControllerIconSize > 128 ||
                OverlayControllerContainerBorderThickness < 0 || OverlayControllerContainerBorderThickness > 8 ||
                OverlayControllerContainerCornerRadius < 0 || OverlayControllerContainerCornerRadius > 40 ||
                OverlayControllerContainerPadding < 0 || OverlayControllerContainerPadding > 32 ||
                OverlayStatusIconSize < 12 || OverlayStatusIconSize > 48 ||
                OverlayCardWidth < 320 || OverlayCardWidth > 1000 ||
                OverlayPadding < 12 || OverlayPadding > 80 ||
                OverlayElementSpacing < 0 || OverlayElementSpacing > 48 ||
                OverlayScreenMargin < 0 || OverlayScreenMargin > 160 ||
                OverlayBorderThickness < 0 || OverlayBorderThickness > 10 ||
                OverlayCornerRadius < 0 || OverlayCornerRadius > 40 ||
                OverlayConnectionBadgeBorderThickness < 0 || OverlayConnectionBadgeBorderThickness > 8 ||
                OverlayConnectionBadgeCornerRadius < 0 || OverlayConnectionBadgeCornerRadius > 24 ||
                OverlayConnectionBadgeIconSize < 10 || OverlayConnectionBadgeIconSize > 40 ||
                OverlayConnectionBadgeTextSize < 9 || OverlayConnectionBadgeTextSize > 30 ||
                OverlayBatteryBadgeBorderThickness < 0 || OverlayBatteryBadgeBorderThickness > 8 ||
                OverlayBatteryBadgeCornerRadius < 0 || OverlayBatteryBadgeCornerRadius > 24 ||
                OverlayBatteryBadgeIconSize < 10 || OverlayBatteryBadgeIconSize > 40 ||
                OverlayBatteryBadgeTextSize < 9 || OverlayBatteryBadgeTextSize > 30)
            {
                errors.Add(plugin == null ? "Notification or overlay dimensions are outside the supported range."
                    : plugin.Loc("LOCCSM_ValidationAppearance"));
            }
            if (!IsSupportedPosition(NotificationPosition) || !IsSupportedBorderPosition(NotificationBorderPosition) ||
                !IsSupportedIconPosition(NotificationIconPosition) ||
                !IsSupportedOverlayIconPosition(OverlayControllerIconPosition) ||
                !IsSupportedOverlayCardPosition(OverlayCardPosition) ||
                !IsSupportedOverlayAnimation(OverlayAnimation) ||
                !IsSupportedBorderPosition(OverlayBorderPosition) ||
                !AppearanceColors().All(IsHexColor))
            {
                errors.Add(plugin == null ? "An appearance color or notification position is invalid."
                    : plugin.Loc("LOCCSM_ValidationAppearance"));
            }

            return errors.Count == 0;
        }

        public ControllerSessionManagerSettings CloneForWizard()
        {
            return Clone();
        }

        internal void RefreshEditingCloneAfterExternalChange()
        {
            if (editingClone != null)
            {
                editingClone = Clone();
            }
        }

        private ControllerSessionManagerSettings Clone()
        {
            return new ControllerSessionManagerSettings
            {
                EnableMonitoring = EnableMonitoring,
                SettingsSchemaVersion = SettingsSchemaVersion,
                AppearancePreset = AppearancePreset,
                NotificationStylePreset = NotificationStylePreset,
                DesktopNotificationStylePreset = DesktopNotificationStylePreset,
                OverlayStylePreset = OverlayStylePreset,
                EnableNotificationSounds = EnableNotificationSounds,
                EnableDesktopNotificationSounds = EnableDesktopNotificationSounds,
                EnableFullscreenNotificationSounds = EnableFullscreenNotificationSounds,
                NotificationPreviewWithSound = NotificationPreviewWithSound,
                NotificationSoundPack = NotificationSoundPack,
                PlaySoundOnConnected = PlaySoundOnConnected,
                PlaySoundOnDisconnected = PlaySoundOnDisconnected,
                PlaySoundOnLowBattery = PlaySoundOnLowBattery,
                PlaySoundOnWarning = PlaySoundOnWarning,
                CustomConnectedSoundPath = CustomConnectedSoundPath,
                CustomDisconnectedSoundPath = CustomDisconnectedSoundPath,
                CustomLowBatterySoundPath = CustomLowBatterySoundPath,
                CustomWarningSoundPath = CustomWarningSoundPath,
                SavedCustomNotificationStyle = NotificationStyleState.Clone(SavedCustomNotificationStyle),
                SavedCustomDesktopNotificationStyle = NotificationStyleState.Clone(SavedCustomDesktopNotificationStyle),
                NotificationSoundVolume = NotificationSoundVolume,
                EnableDebugLogging = EnableDebugLogging,
                AutoUpdateControllerDatabase = AutoUpdateControllerDatabase,
                CreatorThemeUpdatePolicy = CreatorThemeUpdatePolicy,
                CreatorThemeLastUpdateUtc = CreatorThemeLastUpdateUtc,
                ShowPrimaryControllerInTopPanel = ShowPrimaryControllerInTopPanel,
                TopPanelControllerMode = TopPanelControllerMode,
                ColorTopPanelIndicatorByBattery = ColorTopPanelIndicatorByBattery,
                LaunchFullscreenOnGuideButton = LaunchFullscreenOnGuideButton,
                SetupWizardCompleted = SetupWizardCompleted,
                EnableSessionTracking = EnableSessionTracking,
                ShowDisconnectOverlay = ShowDisconnectOverlay,
                ShowFullscreenControllerNotifications = ShowFullscreenControllerNotifications,
                ShowFullscreenLowBatteryNotifications = ShowFullscreenLowBatteryNotifications,
                ShowDesktopLowBatteryNotifications = ShowDesktopLowBatteryNotifications,
                LowBatteryNotificationThreshold = LowBatteryNotificationThreshold,
                ForcePauseOfflineGames = ForcePauseOfflineGames,
                NotificationWidth = NotificationWidth,
                NotificationScalePercent = NotificationScalePercent,
                NotificationDurationMilliseconds = NotificationDurationMilliseconds,
                NotificationPosition = NotificationPosition,
                NotificationBackgroundColor = NotificationBackgroundColor,
                NotificationUseGradient = NotificationUseGradient,
                NotificationGradientColor = NotificationGradientColor,
                NotificationGradientAngle = NotificationGradientAngle,
                NotificationUseBackgroundImage = NotificationUseBackgroundImage,
                NotificationBackgroundImagePath = NotificationBackgroundImagePath,
                NotificationBackgroundImageStretch = NotificationBackgroundImageStretch,
                NotificationBackgroundImageHorizontalAlignment = NotificationBackgroundImageHorizontalAlignment,
                NotificationBackgroundImageVerticalAlignment = NotificationBackgroundImageVerticalAlignment,
                NotificationBackgroundImageOpacity = NotificationBackgroundImageOpacity,
                NotificationBackgroundImageTintOpacity = NotificationBackgroundImageTintOpacity,
                NotificationTextColor = NotificationTextColor,
                NotificationSecondaryTextColor = NotificationSecondaryTextColor,
                NotificationConnectedColor = NotificationConnectedColor,
                NotificationDisconnectedColor = NotificationDisconnectedColor,
                NotificationWarningColor = NotificationWarningColor,
                NotificationLowBatteryColor = NotificationLowBatteryColor,
                NotificationTitleFontSize = NotificationTitleFontSize,
                NotificationMessageFontSize = NotificationMessageFontSize,
                NotificationIconSize = NotificationIconSize,
                NotificationShowIconContainer = NotificationShowIconContainer,
                NotificationIconContainerColor = NotificationIconContainerColor,
                NotificationIconContainerBorderColor = NotificationIconContainerBorderColor,
                NotificationIconContainerBorderThickness = NotificationIconContainerBorderThickness,
                NotificationIconContainerCornerRadius = NotificationIconContainerCornerRadius,
                NotificationIconContainerPadding = NotificationIconContainerPadding,
                NotificationIconPosition = NotificationIconPosition,
                NotificationPadding = NotificationPadding,
                NotificationElementSpacing = NotificationElementSpacing,
                NotificationIconSpacing = NotificationIconSpacing,
                NotificationShowBorder = NotificationShowBorder,
                NotificationBorderPosition = NotificationBorderPosition,
                NotificationBorderThickness = NotificationBorderThickness,
                NotificationCornerRadius = NotificationCornerRadius,
                ShowControllerNameInNotifications = ShowControllerNameInNotifications,
                NotificationShowConnectionBadge = NotificationShowConnectionBadge,
                NotificationScreenMargin = NotificationScreenMargin,
                NotificationShowShadow = NotificationShowShadow,
                NotificationFontFamily = NotificationFontFamily,
                NotificationFontWeight = NotificationFontWeight,
                NotificationTitleFontFamily = NotificationTitleFontFamily,
                NotificationTitleFontWeight = NotificationTitleFontWeight,
                NotificationMessageFontFamily = NotificationMessageFontFamily,
                NotificationMessageFontWeight = NotificationMessageFontWeight,
                NotificationMessageMaxLines = NotificationMessageMaxLines,
                NotificationBadgePosition = NotificationBadgePosition,
                NotificationTextAlignment = NotificationTextAlignment,
                NotificationAccentMode = NotificationAccentMode,
                NotificationAnimation = NotificationAnimation,
                NotificationShowTitle = NotificationShowTitle,
                NotificationUppercaseTitle = NotificationUppercaseTitle,
                NotificationTextOrder = NotificationTextOrder,
                NotificationUseIndependentBorders = NotificationUseIndependentBorders,
                NotificationBorderLeftThickness = NotificationBorderLeftThickness,
                NotificationBorderTopThickness = NotificationBorderTopThickness,
                NotificationBorderRightThickness = NotificationBorderRightThickness,
                NotificationBorderBottomThickness = NotificationBorderBottomThickness,
                NotificationUseStateBackgroundColors = NotificationUseStateBackgroundColors,
                NotificationConnectedBackgroundColor = NotificationConnectedBackgroundColor,
                NotificationDisconnectedBackgroundColor = NotificationDisconnectedBackgroundColor,
                NotificationWarningBackgroundColor = NotificationWarningBackgroundColor,
                NotificationLowBatteryBackgroundColor = NotificationLowBatteryBackgroundColor,
                NotificationUseBorderGradient = NotificationUseBorderGradient,
                NotificationUseStateBorderColors = NotificationUseStateBorderColors,
                NotificationConnectedBorderColor = NotificationConnectedBorderColor,
                NotificationDisconnectedBorderColor = NotificationDisconnectedBorderColor,
                NotificationWarningBorderColor = NotificationWarningBorderColor,
                NotificationLowBatteryBorderColor = NotificationLowBatteryBorderColor,
                NotificationBorderGradientStartColor = NotificationBorderGradientStartColor,
                NotificationBorderGradientEndColor = NotificationBorderGradientEndColor,
                NotificationBorderGradientAngle = NotificationBorderGradientAngle,
                NotificationShowBorderGlow = NotificationShowBorderGlow,
                NotificationBorderGlowColor = NotificationBorderGlowColor,
                NotificationBorderGlowBlur = NotificationBorderGlowBlur,
                NotificationBorderGlowOpacity = NotificationBorderGlowOpacity,
                ShowControllerNameInDesktopNotifications = ShowControllerNameInDesktopNotifications,
                ShowDesktopControllerNotifications = ShowDesktopControllerNotifications,
                DesktopNotificationWidth = DesktopNotificationWidth,
                DesktopNotificationScalePercent = DesktopNotificationScalePercent,
                DesktopNotificationDurationMilliseconds = DesktopNotificationDurationMilliseconds,
                DesktopNotificationPosition = DesktopNotificationPosition,
                DesktopNotificationBackgroundColor = DesktopNotificationBackgroundColor,
                DesktopNotificationUseGradient = DesktopNotificationUseGradient,
                DesktopNotificationGradientColor = DesktopNotificationGradientColor,
                DesktopNotificationGradientAngle = DesktopNotificationGradientAngle,
                DesktopNotificationUseBackgroundImage = DesktopNotificationUseBackgroundImage,
                DesktopNotificationBackgroundImagePath = DesktopNotificationBackgroundImagePath,
                DesktopNotificationBackgroundImageStretch = DesktopNotificationBackgroundImageStretch,
                DesktopNotificationBackgroundImageHorizontalAlignment = DesktopNotificationBackgroundImageHorizontalAlignment,
                DesktopNotificationBackgroundImageVerticalAlignment = DesktopNotificationBackgroundImageVerticalAlignment,
                DesktopNotificationBackgroundImageOpacity = DesktopNotificationBackgroundImageOpacity,
                DesktopNotificationBackgroundImageTintOpacity = DesktopNotificationBackgroundImageTintOpacity,
                DesktopNotificationTextColor = DesktopNotificationTextColor,
                DesktopNotificationSecondaryTextColor = DesktopNotificationSecondaryTextColor,
                DesktopNotificationConnectedColor = DesktopNotificationConnectedColor,
                DesktopNotificationDisconnectedColor = DesktopNotificationDisconnectedColor,
                DesktopNotificationWarningColor = DesktopNotificationWarningColor,
                DesktopNotificationLowBatteryColor = DesktopNotificationLowBatteryColor,
                DesktopNotificationTitleFontSize = DesktopNotificationTitleFontSize,
                DesktopNotificationMessageFontSize = DesktopNotificationMessageFontSize,
                DesktopNotificationIconSize = DesktopNotificationIconSize,
                DesktopNotificationShowIconContainer = DesktopNotificationShowIconContainer,
                DesktopNotificationIconContainerColor = DesktopNotificationIconContainerColor,
                DesktopNotificationIconContainerBorderColor = DesktopNotificationIconContainerBorderColor,
                DesktopNotificationIconContainerBorderThickness = DesktopNotificationIconContainerBorderThickness,
                DesktopNotificationIconContainerCornerRadius = DesktopNotificationIconContainerCornerRadius,
                DesktopNotificationIconContainerPadding = DesktopNotificationIconContainerPadding,
                DesktopNotificationIconPosition = DesktopNotificationIconPosition,
                DesktopNotificationPadding = DesktopNotificationPadding,
                DesktopNotificationElementSpacing = DesktopNotificationElementSpacing,
                DesktopNotificationIconSpacing = DesktopNotificationIconSpacing,
                DesktopNotificationShowBorder = DesktopNotificationShowBorder,
                DesktopNotificationBorderPosition = DesktopNotificationBorderPosition,
                DesktopNotificationBorderThickness = DesktopNotificationBorderThickness,
                DesktopNotificationCornerRadius = DesktopNotificationCornerRadius,
                DesktopNotificationShowConnectionBadge = DesktopNotificationShowConnectionBadge,
                DesktopNotificationScreenMargin = DesktopNotificationScreenMargin,
                DesktopNotificationShowShadow = DesktopNotificationShowShadow,
                DesktopNotificationFontFamily = DesktopNotificationFontFamily,
                DesktopNotificationFontWeight = DesktopNotificationFontWeight,
                DesktopNotificationTitleFontFamily = DesktopNotificationTitleFontFamily,
                DesktopNotificationTitleFontWeight = DesktopNotificationTitleFontWeight,
                DesktopNotificationMessageFontFamily = DesktopNotificationMessageFontFamily,
                DesktopNotificationMessageFontWeight = DesktopNotificationMessageFontWeight,
                DesktopNotificationMessageMaxLines = DesktopNotificationMessageMaxLines,
                DesktopNotificationBadgePosition = DesktopNotificationBadgePosition,
                DesktopNotificationTextAlignment = DesktopNotificationTextAlignment,
                DesktopNotificationAccentMode = DesktopNotificationAccentMode,
                DesktopNotificationAnimation = DesktopNotificationAnimation,
                DesktopNotificationShowTitle = DesktopNotificationShowTitle,
                DesktopNotificationUppercaseTitle = DesktopNotificationUppercaseTitle,
                DesktopNotificationTextOrder = DesktopNotificationTextOrder,
                DesktopNotificationUseIndependentBorders = DesktopNotificationUseIndependentBorders,
                DesktopNotificationBorderLeftThickness = DesktopNotificationBorderLeftThickness,
                DesktopNotificationBorderTopThickness = DesktopNotificationBorderTopThickness,
                DesktopNotificationBorderRightThickness = DesktopNotificationBorderRightThickness,
                DesktopNotificationBorderBottomThickness = DesktopNotificationBorderBottomThickness,
                DesktopNotificationUseStateBackgroundColors = DesktopNotificationUseStateBackgroundColors,
                DesktopNotificationConnectedBackgroundColor = DesktopNotificationConnectedBackgroundColor,
                DesktopNotificationDisconnectedBackgroundColor = DesktopNotificationDisconnectedBackgroundColor,
                DesktopNotificationWarningBackgroundColor = DesktopNotificationWarningBackgroundColor,
                DesktopNotificationLowBatteryBackgroundColor = DesktopNotificationLowBatteryBackgroundColor,
                DesktopNotificationUseBorderGradient = DesktopNotificationUseBorderGradient,
                DesktopNotificationUseStateBorderColors = DesktopNotificationUseStateBorderColors,
                DesktopNotificationConnectedBorderColor = DesktopNotificationConnectedBorderColor,
                DesktopNotificationDisconnectedBorderColor = DesktopNotificationDisconnectedBorderColor,
                DesktopNotificationWarningBorderColor = DesktopNotificationWarningBorderColor,
                DesktopNotificationLowBatteryBorderColor = DesktopNotificationLowBatteryBorderColor,
                DesktopNotificationBorderGradientStartColor = DesktopNotificationBorderGradientStartColor,
                DesktopNotificationBorderGradientEndColor = DesktopNotificationBorderGradientEndColor,
                DesktopNotificationBorderGradientAngle = DesktopNotificationBorderGradientAngle,
                DesktopNotificationShowBorderGlow = DesktopNotificationShowBorderGlow,
                DesktopNotificationBorderGlowColor = DesktopNotificationBorderGlowColor,
                DesktopNotificationBorderGlowBlur = DesktopNotificationBorderGlowBlur,
                DesktopNotificationBorderGlowOpacity = DesktopNotificationBorderGlowOpacity,
                OverlayScalePercent = OverlayScalePercent,
                OverlayDimColor = OverlayDimColor,
                OverlayCardColor = OverlayCardColor,
                OverlayUseGradient = OverlayUseGradient,
                OverlayGradientColor = OverlayGradientColor,
                OverlayGradientAngle = OverlayGradientAngle,
                OverlaySceneUseGradient = OverlaySceneUseGradient,
                OverlaySceneGradientColor = OverlaySceneGradientColor,
                OverlaySceneGradientAngle = OverlaySceneGradientAngle,
                OverlaySceneUseBackgroundImage = OverlaySceneUseBackgroundImage,
                OverlaySceneBackgroundImagePath = OverlaySceneBackgroundImagePath,
                OverlaySceneBackgroundImageStretch = OverlaySceneBackgroundImageStretch,
                OverlaySceneBackgroundImageHorizontalAlignment = OverlaySceneBackgroundImageHorizontalAlignment,
                OverlaySceneBackgroundImageVerticalAlignment = OverlaySceneBackgroundImageVerticalAlignment,
                OverlaySceneBackgroundImageOpacity = OverlaySceneBackgroundImageOpacity,
                OverlaySceneUseAmbientGlows = OverlaySceneUseAmbientGlows,
                OverlaySceneGlow1Color = OverlaySceneGlow1Color,
                OverlaySceneGlow1X = OverlaySceneGlow1X,
                OverlaySceneGlow1Y = OverlaySceneGlow1Y,
                OverlaySceneGlow1Radius = OverlaySceneGlow1Radius,
                OverlaySceneGlow2Color = OverlaySceneGlow2Color,
                OverlaySceneGlow2X = OverlaySceneGlow2X,
                OverlaySceneGlow2Y = OverlaySceneGlow2Y,
                OverlaySceneGlow2Radius = OverlaySceneGlow2Radius,
                OverlaySceneGlow3Color = OverlaySceneGlow3Color,
                OverlaySceneGlow3X = OverlaySceneGlow3X,
                OverlaySceneGlow3Y = OverlaySceneGlow3Y,
                OverlaySceneGlow3Radius = OverlaySceneGlow3Radius,
                OverlaySceneShowGrid = OverlaySceneShowGrid,
                OverlaySceneGridColor = OverlaySceneGridColor,
                OverlaySceneGridSize = OverlaySceneGridSize,
                OverlayUseBackgroundImage = OverlayUseBackgroundImage,
                OverlayBackgroundImagePath = OverlayBackgroundImagePath,
                OverlayBackgroundImageStretch = OverlayBackgroundImageStretch,
                OverlayBackgroundImageHorizontalAlignment = OverlayBackgroundImageHorizontalAlignment,
                OverlayBackgroundImageVerticalAlignment = OverlayBackgroundImageVerticalAlignment,
                OverlayBackgroundImageOpacity = OverlayBackgroundImageOpacity,
                OverlayBackgroundImageTintOpacity = OverlayBackgroundImageTintOpacity,
                OverlayAccentColor = OverlayAccentColor,
                OverlayInstructionColor = OverlayInstructionColor,
                OverlayControllerIconColor = OverlayControllerIconColor,
                OverlayTextColor = OverlayTextColor,
                OverlayWarningColor = OverlayWarningColor,
                OverlayTitleFontSize = OverlayTitleFontSize,
                OverlayControllerFontSize = OverlayControllerFontSize,
                OverlayInstructionFontSize = OverlayInstructionFontSize,
                OverlayStatusFontSize = OverlayStatusFontSize,
                OverlayControllerIconSize = OverlayControllerIconSize,
                OverlayShowControllerContainer = OverlayShowControllerContainer,
                OverlayControllerContainerColor = OverlayControllerContainerColor,
                OverlayControllerContainerBorderColor = OverlayControllerContainerBorderColor,
                OverlayControllerContainerBorderThickness = OverlayControllerContainerBorderThickness,
                OverlayControllerContainerCornerRadius = OverlayControllerContainerCornerRadius,
                OverlayControllerContainerPadding = OverlayControllerContainerPadding,
                OverlayStatusIconSize = OverlayStatusIconSize,
                OverlayShowControllerIcon = OverlayShowControllerIcon,
                OverlayShowStatusIcon = OverlayShowStatusIcon,
                OverlayShowControllerName = OverlayShowControllerName,
                OverlayShowConnectionBadge = OverlayShowConnectionBadge,
                OverlayShowBatteryBadge = OverlayShowBatteryBadge,
                OverlayShowTitle = OverlayShowTitle,
                OverlayUppercaseTitle = OverlayUppercaseTitle,
                OverlayShowInstruction = OverlayShowInstruction,
                OverlayShowPauseStatus = OverlayShowPauseStatus,
                OverlayShowDisconnectTimer = OverlayShowDisconnectTimer,
                OverlayControllerIconPosition = OverlayControllerIconPosition,
                OverlayCardPosition = OverlayCardPosition,
                OverlayLayoutMode = OverlayLayoutMode,
                OverlaySplitControllerSide = OverlaySplitControllerSide,
                OverlayShowSplitDivider = OverlayShowSplitDivider,
                OverlaySplitDividerColor = OverlaySplitDividerColor,
                OverlaySplitDividerThickness = OverlaySplitDividerThickness,
                OverlayShowIncidentBadge = OverlayShowIncidentBadge,
                OverlayIncidentBadgeTextColor = OverlayIncidentBadgeTextColor,
                OverlayIncidentBadgeBackgroundColor = OverlayIncidentBadgeBackgroundColor,
                OverlayIncidentBadgeBorderColor = OverlayIncidentBadgeBorderColor,
                OverlayIncidentBadgeBorderThickness = OverlayIncidentBadgeBorderThickness,
                OverlayIncidentBadgeCornerRadius = OverlayIncidentBadgeCornerRadius,
                OverlayIncidentBadgeTextSize = OverlayIncidentBadgeTextSize,
                OverlayStatusInMetadata = OverlayStatusInMetadata,
                OverlayContentAlignment = OverlayContentAlignment,
                OverlayScreenMargin = OverlayScreenMargin,
                OverlayAnimation = OverlayAnimation,
                OverlayBorderPosition = OverlayBorderPosition,
                OverlayCardWidth = OverlayCardWidth,
                OverlayPadding = OverlayPadding,
                OverlayElementSpacing = OverlayElementSpacing,
                OverlayShowBorder = OverlayShowBorder,
                OverlayShowShadow = OverlayShowShadow,
                OverlayBorderThickness = OverlayBorderThickness,
                OverlayCornerRadius = OverlayCornerRadius,
                OverlayFontFamily = OverlayFontFamily,
                OverlayFontWeight = OverlayFontWeight,
                OverlayTitleFontFamily = OverlayTitleFontFamily,
                OverlayTitleFontWeight = OverlayTitleFontWeight,
                OverlayControllerFontFamily = OverlayControllerFontFamily,
                OverlayControllerFontWeight = OverlayControllerFontWeight,
                OverlayInstructionFontFamily = OverlayInstructionFontFamily,
                OverlayInstructionFontWeight = OverlayInstructionFontWeight,
                OverlayStatusFontFamily = OverlayStatusFontFamily,
                OverlayStatusFontWeight = OverlayStatusFontWeight,
                OverlayConnectionBadgeTextColor = OverlayConnectionBadgeTextColor,
                OverlayConnectionBadgeIconColor = OverlayConnectionBadgeIconColor,
                OverlayConnectionBadgeBackgroundColor = OverlayConnectionBadgeBackgroundColor,
                OverlayConnectionBadgeBorderColor = OverlayConnectionBadgeBorderColor,
                OverlayConnectionBadgeBorderThickness = OverlayConnectionBadgeBorderThickness,
                OverlayConnectionBadgeCornerRadius = OverlayConnectionBadgeCornerRadius,
                OverlayConnectionBadgeIconSize = OverlayConnectionBadgeIconSize,
                OverlayConnectionBadgeTextSize = OverlayConnectionBadgeTextSize,
                OverlayBatteryBadgeTextColor = OverlayBatteryBadgeTextColor,
                OverlayBatteryBadgeIconColor = OverlayBatteryBadgeIconColor,
                OverlayBatteryBadgeBackgroundColor = OverlayBatteryBadgeBackgroundColor,
                OverlayBatteryBadgeBorderColor = OverlayBatteryBadgeBorderColor,
                OverlayBatteryBadgeBorderThickness = OverlayBatteryBadgeBorderThickness,
                OverlayBatteryBadgeCornerRadius = OverlayBatteryBadgeCornerRadius,
                OverlayBatteryBadgeIconSize = OverlayBatteryBadgeIconSize,
                OverlayBatteryBadgeTextSize = OverlayBatteryBadgeTextSize,
                OverlayBatteryBadgeUseStateColors = OverlayBatteryBadgeUseStateColors,
                OverlayBatteryBadgeFullColor = OverlayBatteryBadgeFullColor,
                OverlayBatteryBadgeMediumColor = OverlayBatteryBadgeMediumColor,
                OverlayBatteryBadgeLowColor = OverlayBatteryBadgeLowColor,
                OverlayBatteryBadgeEmptyColor = OverlayBatteryBadgeEmptyColor,
                OverlayBlockOrder = OverlayBlockOrder,
                OverlayMetadataOrientation = OverlayMetadataOrientation,
                OverlayUseIndependentBorders = OverlayUseIndependentBorders,
                OverlayBorderLeftThickness = OverlayBorderLeftThickness,
                OverlayBorderTopThickness = OverlayBorderTopThickness,
                OverlayBorderRightThickness = OverlayBorderRightThickness,
                OverlayBorderBottomThickness = OverlayBorderBottomThickness,
                OverlayUseBorderGradient = OverlayUseBorderGradient,
                OverlayBorderGradientStartColor = OverlayBorderGradientStartColor,
                OverlayBorderGradientEndColor = OverlayBorderGradientEndColor,
                OverlayBorderGradientAngle = OverlayBorderGradientAngle,
                OverlayShowBorderGlow = OverlayShowBorderGlow,
                OverlayBorderGlowColor = OverlayBorderGlowColor,
                OverlayBorderGlowBlur = OverlayBorderGlowBlur,
                OverlayBorderGlowOpacity = OverlayBorderGlowOpacity,
                AllowControllerTakeover = AllowControllerTakeover,
                ProtectAllActiveControllers = ProtectAllActiveControllers,
                PauseGameOnDisconnect = PauseGameOnDisconnect,
                DisconnectGracePeriodMilliseconds = DisconnectGracePeriodMilliseconds,
                ReconciliationIntervalSeconds = ReconciliationIntervalSeconds,
                ControllerProfiles = CloneProfiles(ControllerProfiles),
                GameSessionOverrides = CloneGameOverrides(GameSessionOverrides),
                Tester = Tester == null ? new GamepadTesterSettings() : Tester.Clone()
            };
        }

        private void CopyFrom(ControllerSessionManagerSettings source)
        {
            EnableMonitoring = source.EnableMonitoring;
            SettingsSchemaVersion = source.SettingsSchemaVersion;
            AppearancePreset = source.AppearancePreset;
            NotificationStylePreset = source.NotificationStylePreset;
            DesktopNotificationStylePreset = source.DesktopNotificationStylePreset;
            OverlayStylePreset = source.OverlayStylePreset;
            EnableNotificationSounds = source.EnableNotificationSounds;
            EnableDesktopNotificationSounds = source.EnableDesktopNotificationSounds;
            EnableFullscreenNotificationSounds = source.EnableFullscreenNotificationSounds;
            NotificationPreviewWithSound = source.NotificationPreviewWithSound;
            NotificationSoundPack = source.NotificationSoundPack;
            PlaySoundOnConnected = source.PlaySoundOnConnected;
            PlaySoundOnDisconnected = source.PlaySoundOnDisconnected;
            PlaySoundOnLowBattery = source.PlaySoundOnLowBattery;
            PlaySoundOnWarning = source.PlaySoundOnWarning;
            CustomConnectedSoundPath = source.CustomConnectedSoundPath;
            CustomDisconnectedSoundPath = source.CustomDisconnectedSoundPath;
            CustomLowBatterySoundPath = source.CustomLowBatterySoundPath;
            CustomWarningSoundPath = source.CustomWarningSoundPath;
            SavedCustomNotificationStyle = NotificationStyleState.Clone(source.SavedCustomNotificationStyle);
            SavedCustomDesktopNotificationStyle = NotificationStyleState.Clone(source.SavedCustomDesktopNotificationStyle);
            NotificationSoundVolume = source.NotificationSoundVolume;
            EnableDebugLogging = source.EnableDebugLogging;
            AutoUpdateControllerDatabase = source.AutoUpdateControllerDatabase;
            CreatorThemeUpdatePolicy = source.CreatorThemeUpdatePolicy;
            CreatorThemeLastUpdateUtc = source.CreatorThemeLastUpdateUtc;
            showPrimaryControllerInTopPanel = source.showPrimaryControllerInTopPanel;
            topPanelControllerMode = source.topPanelControllerMode;
            ColorTopPanelIndicatorByBattery = source.ColorTopPanelIndicatorByBattery;
            LaunchFullscreenOnGuideButton = source.LaunchFullscreenOnGuideButton;
            SetupWizardCompleted = source.SetupWizardCompleted;
            EnableSessionTracking = source.EnableSessionTracking;
            ShowDisconnectOverlay = source.ShowDisconnectOverlay;
            ShowFullscreenControllerNotifications = source.ShowFullscreenControllerNotifications;
            ShowDesktopControllerNotifications = source.ShowDesktopControllerNotifications;
            ShowFullscreenLowBatteryNotifications = source.ShowFullscreenLowBatteryNotifications;
            ShowDesktopLowBatteryNotifications = source.ShowDesktopLowBatteryNotifications;
            LowBatteryNotificationThreshold = source.LowBatteryNotificationThreshold;
            ForcePauseOfflineGames = source.ForcePauseOfflineGames;
            NotificationWidth = source.NotificationWidth;
            NotificationScalePercent = source.NotificationScalePercent;
            NotificationDurationMilliseconds = source.NotificationDurationMilliseconds;
            NotificationPosition = source.NotificationPosition;
            NotificationBackgroundColor = source.NotificationBackgroundColor;
            NotificationUseGradient = source.NotificationUseGradient;
            NotificationGradientColor = source.NotificationGradientColor;
            NotificationGradientAngle = source.NotificationGradientAngle;
            NotificationUseBackgroundImage = source.NotificationUseBackgroundImage;
            NotificationBackgroundImagePath = source.NotificationBackgroundImagePath;
            NotificationBackgroundImageStretch = source.NotificationBackgroundImageStretch;
            NotificationBackgroundImageHorizontalAlignment = source.NotificationBackgroundImageHorizontalAlignment;
            NotificationBackgroundImageVerticalAlignment = source.NotificationBackgroundImageVerticalAlignment;
            NotificationBackgroundImageOpacity = source.NotificationBackgroundImageOpacity;
            NotificationBackgroundImageTintOpacity = source.NotificationBackgroundImageTintOpacity;
            NotificationTextColor = source.NotificationTextColor;
            NotificationSecondaryTextColor = source.NotificationSecondaryTextColor;
            NotificationConnectedColor = source.NotificationConnectedColor;
            NotificationDisconnectedColor = source.NotificationDisconnectedColor;
            NotificationWarningColor = source.NotificationWarningColor;
            NotificationLowBatteryColor = source.NotificationLowBatteryColor;
            NotificationTitleFontSize = source.NotificationTitleFontSize;
            NotificationMessageFontSize = source.NotificationMessageFontSize;
            NotificationIconSize = source.NotificationIconSize;
            NotificationShowIconContainer = source.NotificationShowIconContainer;
            NotificationIconContainerColor = source.NotificationIconContainerColor;
            NotificationIconContainerBorderColor = source.NotificationIconContainerBorderColor;
            NotificationIconContainerBorderThickness = source.NotificationIconContainerBorderThickness;
            NotificationIconContainerCornerRadius = source.NotificationIconContainerCornerRadius;
            NotificationIconContainerPadding = source.NotificationIconContainerPadding;
            NotificationIconPosition = source.NotificationIconPosition;
            NotificationPadding = source.NotificationPadding;
            NotificationElementSpacing = source.NotificationElementSpacing;
            NotificationIconSpacing = source.NotificationIconSpacing;
            NotificationShowBorder = source.NotificationShowBorder;
            NotificationBorderPosition = source.NotificationBorderPosition;
            NotificationBorderThickness = source.NotificationBorderThickness;
            NotificationCornerRadius = source.NotificationCornerRadius;
            ShowControllerNameInNotifications = source.ShowControllerNameInNotifications;
            NotificationShowConnectionBadge = source.NotificationShowConnectionBadge;
            NotificationScreenMargin = source.NotificationScreenMargin;
            NotificationShowShadow = source.NotificationShowShadow;
            NotificationFontFamily = source.NotificationFontFamily;
            NotificationFontWeight = source.NotificationFontWeight;
            NotificationTitleFontFamily = source.NotificationTitleFontFamily;
            NotificationTitleFontWeight = source.NotificationTitleFontWeight;
            NotificationMessageFontFamily = source.NotificationMessageFontFamily;
            NotificationMessageFontWeight = source.NotificationMessageFontWeight;
            NotificationMessageMaxLines = source.NotificationMessageMaxLines;
            NotificationBadgePosition = source.NotificationBadgePosition;
            NotificationTextAlignment = source.NotificationTextAlignment;
            NotificationAccentMode = source.NotificationAccentMode;
            NotificationAnimation = source.NotificationAnimation;
            NotificationShowTitle = source.NotificationShowTitle;
            NotificationUppercaseTitle = source.NotificationUppercaseTitle;
            NotificationTextOrder = source.NotificationTextOrder;
            NotificationUseIndependentBorders = source.NotificationUseIndependentBorders;
            NotificationBorderLeftThickness = source.NotificationBorderLeftThickness;
            NotificationBorderTopThickness = source.NotificationBorderTopThickness;
            NotificationBorderRightThickness = source.NotificationBorderRightThickness;
            NotificationBorderBottomThickness = source.NotificationBorderBottomThickness;
            NotificationUseStateBackgroundColors = source.NotificationUseStateBackgroundColors;
            NotificationConnectedBackgroundColor = source.NotificationConnectedBackgroundColor;
            NotificationDisconnectedBackgroundColor = source.NotificationDisconnectedBackgroundColor;
            NotificationWarningBackgroundColor = source.NotificationWarningBackgroundColor;
            NotificationLowBatteryBackgroundColor = source.NotificationLowBatteryBackgroundColor;
            NotificationUseBorderGradient = source.NotificationUseBorderGradient;
            NotificationUseStateBorderColors = source.NotificationUseStateBorderColors;
            NotificationConnectedBorderColor = source.NotificationConnectedBorderColor;
            NotificationDisconnectedBorderColor = source.NotificationDisconnectedBorderColor;
            NotificationWarningBorderColor = source.NotificationWarningBorderColor;
            NotificationLowBatteryBorderColor = source.NotificationLowBatteryBorderColor;
            NotificationBorderGradientStartColor = source.NotificationBorderGradientStartColor;
            NotificationBorderGradientEndColor = source.NotificationBorderGradientEndColor;
            NotificationBorderGradientAngle = source.NotificationBorderGradientAngle;
            NotificationShowBorderGlow = source.NotificationShowBorderGlow;
            NotificationBorderGlowColor = source.NotificationBorderGlowColor;
            NotificationBorderGlowBlur = source.NotificationBorderGlowBlur;
            NotificationBorderGlowOpacity = source.NotificationBorderGlowOpacity;
            ShowControllerNameInDesktopNotifications = source.ShowControllerNameInDesktopNotifications;
            DesktopNotificationWidth = source.DesktopNotificationWidth;
            DesktopNotificationScalePercent = source.DesktopNotificationScalePercent;
            DesktopNotificationDurationMilliseconds = source.DesktopNotificationDurationMilliseconds;
            DesktopNotificationPosition = source.DesktopNotificationPosition;
            DesktopNotificationBackgroundColor = source.DesktopNotificationBackgroundColor;
            DesktopNotificationUseGradient = source.DesktopNotificationUseGradient;
            DesktopNotificationGradientColor = source.DesktopNotificationGradientColor;
            DesktopNotificationGradientAngle = source.DesktopNotificationGradientAngle;
            DesktopNotificationUseBackgroundImage = source.DesktopNotificationUseBackgroundImage;
            DesktopNotificationBackgroundImagePath = source.DesktopNotificationBackgroundImagePath;
            DesktopNotificationBackgroundImageStretch = source.DesktopNotificationBackgroundImageStretch;
            DesktopNotificationBackgroundImageHorizontalAlignment = source.DesktopNotificationBackgroundImageHorizontalAlignment;
            DesktopNotificationBackgroundImageVerticalAlignment = source.DesktopNotificationBackgroundImageVerticalAlignment;
            DesktopNotificationBackgroundImageOpacity = source.DesktopNotificationBackgroundImageOpacity;
            DesktopNotificationBackgroundImageTintOpacity = source.DesktopNotificationBackgroundImageTintOpacity;
            DesktopNotificationTextColor = source.DesktopNotificationTextColor;
            DesktopNotificationSecondaryTextColor = source.DesktopNotificationSecondaryTextColor;
            DesktopNotificationConnectedColor = source.DesktopNotificationConnectedColor;
            DesktopNotificationDisconnectedColor = source.DesktopNotificationDisconnectedColor;
            DesktopNotificationWarningColor = source.DesktopNotificationWarningColor;
            DesktopNotificationLowBatteryColor = source.DesktopNotificationLowBatteryColor;
            DesktopNotificationTitleFontSize = source.DesktopNotificationTitleFontSize;
            DesktopNotificationMessageFontSize = source.DesktopNotificationMessageFontSize;
            DesktopNotificationIconSize = source.DesktopNotificationIconSize;
            DesktopNotificationShowIconContainer = source.DesktopNotificationShowIconContainer;
            DesktopNotificationIconContainerColor = source.DesktopNotificationIconContainerColor;
            DesktopNotificationIconContainerBorderColor = source.DesktopNotificationIconContainerBorderColor;
            DesktopNotificationIconContainerBorderThickness = source.DesktopNotificationIconContainerBorderThickness;
            DesktopNotificationIconContainerCornerRadius = source.DesktopNotificationIconContainerCornerRadius;
            DesktopNotificationIconContainerPadding = source.DesktopNotificationIconContainerPadding;
            DesktopNotificationIconPosition = source.DesktopNotificationIconPosition;
            DesktopNotificationPadding = source.DesktopNotificationPadding;
            DesktopNotificationElementSpacing = source.DesktopNotificationElementSpacing;
            DesktopNotificationIconSpacing = source.DesktopNotificationIconSpacing;
            DesktopNotificationShowBorder = source.DesktopNotificationShowBorder;
            DesktopNotificationBorderPosition = source.DesktopNotificationBorderPosition;
            DesktopNotificationBorderThickness = source.DesktopNotificationBorderThickness;
            DesktopNotificationCornerRadius = source.DesktopNotificationCornerRadius;
            DesktopNotificationShowConnectionBadge = source.DesktopNotificationShowConnectionBadge;
            DesktopNotificationScreenMargin = source.DesktopNotificationScreenMargin;
            DesktopNotificationShowShadow = source.DesktopNotificationShowShadow;
            DesktopNotificationFontFamily = source.DesktopNotificationFontFamily;
            DesktopNotificationFontWeight = source.DesktopNotificationFontWeight;
            DesktopNotificationTitleFontFamily = source.DesktopNotificationTitleFontFamily;
            DesktopNotificationTitleFontWeight = source.DesktopNotificationTitleFontWeight;
            DesktopNotificationMessageFontFamily = source.DesktopNotificationMessageFontFamily;
            DesktopNotificationMessageFontWeight = source.DesktopNotificationMessageFontWeight;
            DesktopNotificationMessageMaxLines = source.DesktopNotificationMessageMaxLines;
            DesktopNotificationBadgePosition = source.DesktopNotificationBadgePosition;
            DesktopNotificationTextAlignment = source.DesktopNotificationTextAlignment;
            DesktopNotificationAccentMode = source.DesktopNotificationAccentMode;
            DesktopNotificationAnimation = source.DesktopNotificationAnimation;
            DesktopNotificationShowTitle = source.DesktopNotificationShowTitle;
            DesktopNotificationUppercaseTitle = source.DesktopNotificationUppercaseTitle;
            DesktopNotificationTextOrder = source.DesktopNotificationTextOrder;
            DesktopNotificationUseIndependentBorders = source.DesktopNotificationUseIndependentBorders;
            DesktopNotificationBorderLeftThickness = source.DesktopNotificationBorderLeftThickness;
            DesktopNotificationBorderTopThickness = source.DesktopNotificationBorderTopThickness;
            DesktopNotificationBorderRightThickness = source.DesktopNotificationBorderRightThickness;
            DesktopNotificationBorderBottomThickness = source.DesktopNotificationBorderBottomThickness;
            DesktopNotificationUseStateBackgroundColors = source.DesktopNotificationUseStateBackgroundColors;
            DesktopNotificationConnectedBackgroundColor = source.DesktopNotificationConnectedBackgroundColor;
            DesktopNotificationDisconnectedBackgroundColor = source.DesktopNotificationDisconnectedBackgroundColor;
            DesktopNotificationWarningBackgroundColor = source.DesktopNotificationWarningBackgroundColor;
            DesktopNotificationLowBatteryBackgroundColor = source.DesktopNotificationLowBatteryBackgroundColor;
            DesktopNotificationUseBorderGradient = source.DesktopNotificationUseBorderGradient;
            DesktopNotificationUseStateBorderColors = source.DesktopNotificationUseStateBorderColors;
            DesktopNotificationConnectedBorderColor = source.DesktopNotificationConnectedBorderColor;
            DesktopNotificationDisconnectedBorderColor = source.DesktopNotificationDisconnectedBorderColor;
            DesktopNotificationWarningBorderColor = source.DesktopNotificationWarningBorderColor;
            DesktopNotificationLowBatteryBorderColor = source.DesktopNotificationLowBatteryBorderColor;
            DesktopNotificationBorderGradientStartColor = source.DesktopNotificationBorderGradientStartColor;
            DesktopNotificationBorderGradientEndColor = source.DesktopNotificationBorderGradientEndColor;
            DesktopNotificationBorderGradientAngle = source.DesktopNotificationBorderGradientAngle;
            DesktopNotificationShowBorderGlow = source.DesktopNotificationShowBorderGlow;
            DesktopNotificationBorderGlowColor = source.DesktopNotificationBorderGlowColor;
            DesktopNotificationBorderGlowBlur = source.DesktopNotificationBorderGlowBlur;
            DesktopNotificationBorderGlowOpacity = source.DesktopNotificationBorderGlowOpacity;
            OverlayScalePercent = source.OverlayScalePercent;
            OverlayDimColor = source.OverlayDimColor;
            OverlayCardColor = source.OverlayCardColor;
            OverlayUseGradient = source.OverlayUseGradient;
            OverlayGradientColor = source.OverlayGradientColor;
            OverlayGradientAngle = source.OverlayGradientAngle;
            OverlaySceneUseGradient = source.OverlaySceneUseGradient;
            OverlaySceneGradientColor = source.OverlaySceneGradientColor;
            OverlaySceneGradientAngle = source.OverlaySceneGradientAngle;
            OverlaySceneUseBackgroundImage = source.OverlaySceneUseBackgroundImage;
            OverlaySceneBackgroundImagePath = source.OverlaySceneBackgroundImagePath;
            OverlaySceneBackgroundImageStretch = source.OverlaySceneBackgroundImageStretch;
            OverlaySceneBackgroundImageHorizontalAlignment = source.OverlaySceneBackgroundImageHorizontalAlignment;
            OverlaySceneBackgroundImageVerticalAlignment = source.OverlaySceneBackgroundImageVerticalAlignment;
            OverlaySceneBackgroundImageOpacity = source.OverlaySceneBackgroundImageOpacity;
            OverlaySceneUseAmbientGlows = source.OverlaySceneUseAmbientGlows;
            OverlaySceneGlow1Color = source.OverlaySceneGlow1Color;
            OverlaySceneGlow1X = source.OverlaySceneGlow1X;
            OverlaySceneGlow1Y = source.OverlaySceneGlow1Y;
            OverlaySceneGlow1Radius = source.OverlaySceneGlow1Radius;
            OverlaySceneGlow2Color = source.OverlaySceneGlow2Color;
            OverlaySceneGlow2X = source.OverlaySceneGlow2X;
            OverlaySceneGlow2Y = source.OverlaySceneGlow2Y;
            OverlaySceneGlow2Radius = source.OverlaySceneGlow2Radius;
            OverlaySceneGlow3Color = source.OverlaySceneGlow3Color;
            OverlaySceneGlow3X = source.OverlaySceneGlow3X;
            OverlaySceneGlow3Y = source.OverlaySceneGlow3Y;
            OverlaySceneGlow3Radius = source.OverlaySceneGlow3Radius;
            OverlaySceneShowGrid = source.OverlaySceneShowGrid;
            OverlaySceneGridColor = source.OverlaySceneGridColor;
            OverlaySceneGridSize = source.OverlaySceneGridSize;
            OverlayUseBackgroundImage = source.OverlayUseBackgroundImage;
            OverlayBackgroundImagePath = source.OverlayBackgroundImagePath;
            OverlayBackgroundImageStretch = source.OverlayBackgroundImageStretch;
            OverlayBackgroundImageHorizontalAlignment = source.OverlayBackgroundImageHorizontalAlignment;
            OverlayBackgroundImageVerticalAlignment = source.OverlayBackgroundImageVerticalAlignment;
            OverlayBackgroundImageOpacity = source.OverlayBackgroundImageOpacity;
            OverlayBackgroundImageTintOpacity = source.OverlayBackgroundImageTintOpacity;
            OverlayAccentColor = source.OverlayAccentColor;
            OverlayInstructionColor = source.OverlayInstructionColor;
            OverlayControllerIconColor = source.OverlayControllerIconColor;
            OverlayTextColor = source.OverlayTextColor;
            OverlayWarningColor = source.OverlayWarningColor;
            OverlayTitleFontSize = source.OverlayTitleFontSize;
            OverlayControllerFontSize = source.OverlayControllerFontSize;
            OverlayInstructionFontSize = source.OverlayInstructionFontSize;
            OverlayStatusFontSize = source.OverlayStatusFontSize;
            OverlayControllerIconSize = source.OverlayControllerIconSize;
            OverlayShowControllerContainer = source.OverlayShowControllerContainer;
            OverlayControllerContainerColor = source.OverlayControllerContainerColor;
            OverlayControllerContainerBorderColor = source.OverlayControllerContainerBorderColor;
            OverlayControllerContainerBorderThickness = source.OverlayControllerContainerBorderThickness;
            OverlayControllerContainerCornerRadius = source.OverlayControllerContainerCornerRadius;
            OverlayControllerContainerPadding = source.OverlayControllerContainerPadding;
            OverlayStatusIconSize = source.OverlayStatusIconSize;
            OverlayShowControllerIcon = source.OverlayShowControllerIcon;
            OverlayShowStatusIcon = source.OverlayShowStatusIcon;
            OverlayShowControllerName = source.OverlayShowControllerName;
            OverlayShowConnectionBadge = source.OverlayShowConnectionBadge;
            OverlayShowBatteryBadge = source.OverlayShowBatteryBadge;
            OverlayShowTitle = source.OverlayShowTitle;
            OverlayUppercaseTitle = source.OverlayUppercaseTitle;
            OverlayShowInstruction = source.OverlayShowInstruction;
            OverlayShowPauseStatus = source.OverlayShowPauseStatus;
            OverlayShowDisconnectTimer = source.OverlayShowDisconnectTimer;
            OverlayControllerIconPosition = source.OverlayControllerIconPosition;
            OverlayCardPosition = source.OverlayCardPosition;
            OverlayLayoutMode = source.OverlayLayoutMode;
            OverlaySplitControllerSide = source.OverlaySplitControllerSide;
            OverlayShowSplitDivider = source.OverlayShowSplitDivider;
            OverlaySplitDividerColor = source.OverlaySplitDividerColor;
            OverlaySplitDividerThickness = source.OverlaySplitDividerThickness;
            OverlayShowIncidentBadge = source.OverlayShowIncidentBadge;
            OverlayIncidentBadgeTextColor = source.OverlayIncidentBadgeTextColor;
            OverlayIncidentBadgeBackgroundColor = source.OverlayIncidentBadgeBackgroundColor;
            OverlayIncidentBadgeBorderColor = source.OverlayIncidentBadgeBorderColor;
            OverlayIncidentBadgeBorderThickness = source.OverlayIncidentBadgeBorderThickness;
            OverlayIncidentBadgeCornerRadius = source.OverlayIncidentBadgeCornerRadius;
            OverlayIncidentBadgeTextSize = source.OverlayIncidentBadgeTextSize;
            OverlayStatusInMetadata = source.OverlayStatusInMetadata;
            OverlayContentAlignment = source.OverlayContentAlignment;
            OverlayScreenMargin = source.OverlayScreenMargin;
            OverlayAnimation = source.OverlayAnimation;
            OverlayBorderPosition = source.OverlayBorderPosition;
            OverlayCardWidth = source.OverlayCardWidth;
            OverlayPadding = source.OverlayPadding;
            OverlayElementSpacing = source.OverlayElementSpacing;
            OverlayShowBorder = source.OverlayShowBorder;
            OverlayShowShadow = source.OverlayShowShadow;
            OverlayBorderThickness = source.OverlayBorderThickness;
            OverlayCornerRadius = source.OverlayCornerRadius;
            OverlayFontFamily = source.OverlayFontFamily;
            OverlayFontWeight = source.OverlayFontWeight;
            OverlayTitleFontFamily = source.OverlayTitleFontFamily;
            OverlayTitleFontWeight = source.OverlayTitleFontWeight;
            OverlayControllerFontFamily = source.OverlayControllerFontFamily;
            OverlayControllerFontWeight = source.OverlayControllerFontWeight;
            OverlayInstructionFontFamily = source.OverlayInstructionFontFamily;
            OverlayInstructionFontWeight = source.OverlayInstructionFontWeight;
            OverlayStatusFontFamily = source.OverlayStatusFontFamily;
            OverlayStatusFontWeight = source.OverlayStatusFontWeight;
            OverlayConnectionBadgeTextColor = source.OverlayConnectionBadgeTextColor;
            OverlayConnectionBadgeIconColor = source.OverlayConnectionBadgeIconColor;
            OverlayConnectionBadgeBackgroundColor = source.OverlayConnectionBadgeBackgroundColor;
            OverlayConnectionBadgeBorderColor = source.OverlayConnectionBadgeBorderColor;
            OverlayConnectionBadgeBorderThickness = source.OverlayConnectionBadgeBorderThickness;
            OverlayConnectionBadgeCornerRadius = source.OverlayConnectionBadgeCornerRadius;
            OverlayConnectionBadgeIconSize = source.OverlayConnectionBadgeIconSize;
            OverlayConnectionBadgeTextSize = source.OverlayConnectionBadgeTextSize;
            OverlayBatteryBadgeTextColor = source.OverlayBatteryBadgeTextColor;
            OverlayBatteryBadgeIconColor = source.OverlayBatteryBadgeIconColor;
            OverlayBatteryBadgeBackgroundColor = source.OverlayBatteryBadgeBackgroundColor;
            OverlayBatteryBadgeBorderColor = source.OverlayBatteryBadgeBorderColor;
            OverlayBatteryBadgeBorderThickness = source.OverlayBatteryBadgeBorderThickness;
            OverlayBatteryBadgeCornerRadius = source.OverlayBatteryBadgeCornerRadius;
            OverlayBatteryBadgeIconSize = source.OverlayBatteryBadgeIconSize;
            OverlayBatteryBadgeTextSize = source.OverlayBatteryBadgeTextSize;
            OverlayBatteryBadgeUseStateColors = source.OverlayBatteryBadgeUseStateColors;
            OverlayBatteryBadgeFullColor = source.OverlayBatteryBadgeFullColor;
            OverlayBatteryBadgeMediumColor = source.OverlayBatteryBadgeMediumColor;
            OverlayBatteryBadgeLowColor = source.OverlayBatteryBadgeLowColor;
            OverlayBatteryBadgeEmptyColor = source.OverlayBatteryBadgeEmptyColor;
            OverlayBlockOrder = source.OverlayBlockOrder;
            OverlayMetadataOrientation = source.OverlayMetadataOrientation;
            OverlayUseIndependentBorders = source.OverlayUseIndependentBorders;
            OverlayBorderLeftThickness = source.OverlayBorderLeftThickness;
            OverlayBorderTopThickness = source.OverlayBorderTopThickness;
            OverlayBorderRightThickness = source.OverlayBorderRightThickness;
            OverlayBorderBottomThickness = source.OverlayBorderBottomThickness;
            OverlayUseBorderGradient = source.OverlayUseBorderGradient;
            OverlayBorderGradientStartColor = source.OverlayBorderGradientStartColor;
            OverlayBorderGradientEndColor = source.OverlayBorderGradientEndColor;
            OverlayBorderGradientAngle = source.OverlayBorderGradientAngle;
            OverlayShowBorderGlow = source.OverlayShowBorderGlow;
            OverlayBorderGlowColor = source.OverlayBorderGlowColor;
            OverlayBorderGlowBlur = source.OverlayBorderGlowBlur;
            OverlayBorderGlowOpacity = source.OverlayBorderGlowOpacity;
            AllowControllerTakeover = source.AllowControllerTakeover;
            ProtectAllActiveControllers = source.ProtectAllActiveControllers;
            PauseGameOnDisconnect = source.PauseGameOnDisconnect;
            DisconnectGracePeriodMilliseconds = source.DisconnectGracePeriodMilliseconds;
            ReconciliationIntervalSeconds = source.ReconciliationIntervalSeconds;
            ControllerProfiles = CloneProfiles(source.ControllerProfiles);
            GameSessionOverrides = CloneGameOverrides(source.GameSessionOverrides);
            Tester = source.Tester == null ? new GamepadTesterSettings() : source.Tester.Clone();
            foreach (var profile in ControllerProfiles)
            {
                profile.IconId = ControllerIconCatalog.Normalize(profile.IconId);
            }
        }

        private static string SuggestIcon(ControllerDeviceSnapshot controller)
        {
            return ControllerIconCatalog.Suggest(controller);
        }

        private static ControllerIconOption Icon(string id, string name, string fileName)
        {
            return new ControllerIconOption { Id = id, Name = name, FileName = fileName };
        }

        private static List<ControllerProfile> CloneProfiles(IEnumerable<ControllerProfile> profiles)
        {
            return (profiles ?? Enumerable.Empty<ControllerProfile>()).Select(a => new ControllerProfile
            {
                HardwareId = a.HardwareId,
                LastKnownXInputSlot = a.LastKnownXInputSlot,
                DetectedName = a.DetectedName,
                CustomName = a.CustomName,
                IconId = a.IconId
            }).ToList();
        }

        private static List<GameSessionOverride> CloneGameOverrides(IEnumerable<GameSessionOverride> overrides)
        {
            return (overrides ?? Enumerable.Empty<GameSessionOverride>()).Select(a => new GameSessionOverride
            {
                GameId = a.GameId,
                GameName = a.GameName,
                EnableSessionTracking = a.EnableSessionTracking,
                DisconnectGracePeriodMilliseconds = a.DisconnectGracePeriodMilliseconds,
                AllowControllerTakeover = a.AllowControllerTakeover,
                ProtectAllActiveControllers = a.ProtectAllActiveControllers,
                PauseGameOnDisconnect = a.PauseGameOnDisconnect,
                ForcePauseOfflineGames = a.ForcePauseOfflineGames,
                OverrideSessionProtection = a.OverrideSessionProtection,
                OverridePauseProfile = a.OverridePauseProfile
            }).ToList();
        }

        private IEnumerable<string> AppearanceColors()
        {
            yield return NotificationBackgroundColor;
            yield return NotificationGradientColor;
            yield return NotificationIconContainerColor;
            yield return NotificationIconContainerBorderColor;
            yield return NotificationTextColor;
            yield return NotificationSecondaryTextColor;
            yield return NotificationConnectedColor;
            yield return NotificationDisconnectedColor;
            yield return NotificationWarningColor;
            yield return NotificationLowBatteryColor;
            yield return DesktopNotificationGradientColor;
            yield return DesktopNotificationIconContainerColor;
            yield return DesktopNotificationIconContainerBorderColor;
            yield return OverlayDimColor;
            yield return OverlayCardColor;
            yield return OverlayGradientColor;
            yield return OverlaySceneGradientColor;
            yield return OverlaySceneGlow1Color;
            yield return OverlaySceneGlow2Color;
            yield return OverlaySceneGlow3Color;
            yield return OverlaySceneGridColor;
            yield return OverlaySplitDividerColor;
            yield return OverlayIncidentBadgeTextColor;
            yield return OverlayIncidentBadgeBackgroundColor;
            yield return OverlayIncidentBadgeBorderColor;
            yield return OverlayControllerContainerColor;
            yield return OverlayControllerContainerBorderColor;
            yield return OverlayAccentColor;
            yield return OverlayInstructionColor;
            yield return OverlayControllerIconColor;
            yield return OverlayTextColor;
            yield return OverlayWarningColor;
            yield return OverlayConnectionBadgeTextColor;
            yield return OverlayConnectionBadgeIconColor;
            yield return OverlayConnectionBadgeBackgroundColor;
            yield return OverlayConnectionBadgeBorderColor;
            yield return OverlayBatteryBadgeTextColor;
            yield return OverlayBatteryBadgeIconColor;
            yield return OverlayBatteryBadgeBackgroundColor;
            yield return OverlayBatteryBadgeBorderColor;
            yield return OverlayBatteryBadgeFullColor;
            yield return OverlayBatteryBadgeMediumColor;
            yield return OverlayBatteryBadgeLowColor;
            yield return OverlayBatteryBadgeEmptyColor;
        }

        private static bool IsSupportedPosition(string value)
        {
            return value == "TopRight" || value == "TopLeft" ||
                value == "BottomRight" || value == "BottomLeft";
        }

        private static bool IsSupportedBorderPosition(string value)
        {
            return value == "Left" || value == "Top" || value == "Right" || value == "Bottom" || value == "Full";
        }

        private static bool IsSupportedIconPosition(string value)
        {
            return value == "Left" || value == "Right" || value == "Top" ||
                value == "Bottom" || value == "Hidden";
        }

        private static bool IsSupportedOverlayIconPosition(string value)
        {
            return value == "Left" || value == "Right" || value == "Top" || value == "Bottom";
        }

        private static bool IsSupportedOverlayCardPosition(string value)
        {
            return value == "Center" || value == "Top" || value == "Bottom" ||
                value == "TopLeft" || value == "TopRight" ||
                value == "BottomLeft" || value == "BottomRight";
        }

        private static bool IsSupportedOverlayAnimation(string value)
        {
            return value == "None" || value == "Fade" || value == "FadeScale" ||
                value == "Slide";
        }

        private static bool IsHexColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value[0] != '#' ||
                value.Length != 7 && value.Length != 9)
            {
                return false;
            }
            return value.Skip(1).All(a => (a >= '0' && a <= '9') ||
                (a >= 'a' && a <= 'f') || (a >= 'A' && a <= 'F'));
        }
    }
}
