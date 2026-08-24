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
        private string notificationIconPosition = "Left";
        private int notificationPadding = 18;
        private int notificationElementSpacing = 8;
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
        private string notificationTextAlignment = "Left";
        private string notificationAccentMode = "IconAndBorder";
        private string notificationAnimation = "Fade";
        private bool notificationShowTitle = true;
        private int desktopNotificationWidth = 420;
        private int desktopNotificationScalePercent = 100;
        private int desktopNotificationDurationMilliseconds = 4000;
        private string desktopNotificationPosition = "BottomRight";
        private string desktopNotificationBackgroundColor = "#F4121418";
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
        private string desktopNotificationIconPosition = "Left";
        private int desktopNotificationPadding = 14;
        private int desktopNotificationElementSpacing = 6;
        private bool desktopNotificationShowBorder = true;
        private string desktopNotificationBorderPosition = "Bottom";
        private int desktopNotificationBorderThickness = 3;
        private int desktopNotificationCornerRadius = 8;
        private string desktopNotificationFontFamily = NotificationFontCatalog.SystemDefault;
        private string desktopNotificationFontWeight = "SemiBold";
        private string desktopNotificationTextAlignment = "Left";
        private string desktopNotificationAccentMode = "IconAndBorder";
        private string desktopNotificationAnimation = "Fade";
        private bool desktopNotificationShowTitle = true;
        private int overlayScalePercent = 100;
        private string overlayDimColor = "#96000000";
        private string overlayCardColor = "#EB121418";
        private string overlayAccentColor = "#FF2391FF";
        private string overlayTextColor = "#FFFFFFFF";
        private string overlayWarningColor = "#FFF5B542";
        private int overlayTitleFontSize = 30;
        private int overlayControllerFontSize = 22;
        private int overlayInstructionFontSize = 19;
        private int overlayStatusFontSize = 15;
        private int overlayControllerIconSize = 30;
        private int overlayStatusIconSize = 18;
        private bool overlayShowControllerIcon = true;
        private bool overlayShowStatusIcon = true;
        private bool overlayShowControllerName = true;
        private bool overlayShowConnectionBadge = true;
        private bool overlayShowBatteryBadge = true;
        private bool overlayShowTitle = true;
        private bool overlayShowInstruction = true;
        private bool overlayShowPauseStatus = true;
        private string overlayControllerIconPosition = "Left";
        private string overlayCardPosition = "Center";
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
        private bool allowControllerTakeover = true;
        private bool protectAllActiveControllers;
        private int settingsSchemaVersion;
        private string appearancePreset = SettingsAppearance.Midnight;
        private string notificationStylePreset = NotificationStylePresets.Soft;
        private string overlayStylePreset = OverlayStylePresets.Soft;
        private bool enableNotificationSounds = true;
        private string notificationSoundPack = NotificationSoundCatalog.ModernCrystal;
        private bool playSoundOnConnected;
        private bool playSoundOnDisconnected;
        private bool playSoundOnLowBattery = true;
        private bool playSoundOnWarning = true;
        private double notificationSoundVolume = 0.7;
        private bool pauseGameOnDisconnect;
        private int disconnectGracePeriodMilliseconds = 1500;
        private int reconciliationIntervalSeconds = 5;
        private List<ControllerProfile> controllerProfiles = new List<ControllerProfile>();
        private List<GameSessionOverride> gameSessionOverrides = new List<GameSessionOverride>();
        private GamepadTesterSettings tester = new GamepadTesterSettings();

        public const string TopPanelControllerModeHidden = "Hidden";
        public const string TopPanelControllerModeDefault = "Default";
        public const string TopPanelControllerModePrimary = "Primary";

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
            set { SetValue(ref notificationStylePreset, NotificationStylePresets.Normalize(value)); }
        }

        public string OverlayStylePreset
        {
            get { return OverlayStylePresets.Normalize(overlayStylePreset); }
            set { SetValue(ref overlayStylePreset, OverlayStylePresets.Normalize(value)); }
        }

        public bool EnableNotificationSounds
        {
            get { return enableNotificationSounds; }
            set { SetValue(ref enableNotificationSounds, value); }
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
        public string NotificationIconPosition { get { return notificationIconPosition; } set { SetValue(ref notificationIconPosition, value); } }
        public int NotificationPadding { get { return notificationPadding; } set { SetValue(ref notificationPadding, value); } }
        public int NotificationElementSpacing { get { return notificationElementSpacing; } set { SetValue(ref notificationElementSpacing, value); } }
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
        public string NotificationTextAlignment { get { return NotificationFontCatalog.NormalizeAlignment(notificationTextAlignment); } set { SetValue(ref notificationTextAlignment, NotificationFontCatalog.NormalizeAlignment(value)); } }
        public string NotificationAccentMode { get { return NotificationFontCatalog.NormalizeAccentMode(notificationAccentMode); } set { SetValue(ref notificationAccentMode, NotificationFontCatalog.NormalizeAccentMode(value)); } }
        public string NotificationAnimation { get { return NotificationFontCatalog.NormalizeAnimation(notificationAnimation); } set { SetValue(ref notificationAnimation, NotificationFontCatalog.NormalizeAnimation(value)); } }
        public bool NotificationShowTitle { get { return notificationShowTitle; } set { SetValue(ref notificationShowTitle, value); } }
        public int DesktopNotificationWidth { get { return desktopNotificationWidth; } set { SetValue(ref desktopNotificationWidth, value); } }
        public int DesktopNotificationScalePercent { get { return desktopNotificationScalePercent; } set { SetValue(ref desktopNotificationScalePercent, value); } }
        public int DesktopNotificationDurationMilliseconds { get { return desktopNotificationDurationMilliseconds; } set { SetValue(ref desktopNotificationDurationMilliseconds, value); } }
        public string DesktopNotificationPosition { get { return desktopNotificationPosition; } set { SetValue(ref desktopNotificationPosition, value); } }
        public string DesktopNotificationBackgroundColor { get { return desktopNotificationBackgroundColor; } set { SetValue(ref desktopNotificationBackgroundColor, value); } }
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
        public string DesktopNotificationIconPosition { get { return desktopNotificationIconPosition; } set { SetValue(ref desktopNotificationIconPosition, value); } }
        public int DesktopNotificationPadding { get { return desktopNotificationPadding; } set { SetValue(ref desktopNotificationPadding, value); } }
        public int DesktopNotificationElementSpacing { get { return desktopNotificationElementSpacing; } set { SetValue(ref desktopNotificationElementSpacing, value); } }
        public bool DesktopNotificationShowBorder { get { return desktopNotificationShowBorder; } set { SetValue(ref desktopNotificationShowBorder, value); } }
        public string DesktopNotificationBorderPosition { get { return desktopNotificationBorderPosition; } set { SetValue(ref desktopNotificationBorderPosition, value); } }
        public int DesktopNotificationBorderThickness { get { return desktopNotificationBorderThickness; } set { SetValue(ref desktopNotificationBorderThickness, value); } }
        public int DesktopNotificationCornerRadius { get { return desktopNotificationCornerRadius; } set { SetValue(ref desktopNotificationCornerRadius, value); } }
        public string DesktopNotificationFontFamily { get { return NotificationFontCatalog.Normalize(desktopNotificationFontFamily); } set { SetValue(ref desktopNotificationFontFamily, NotificationFontCatalog.Normalize(value)); } }
        public string DesktopNotificationFontWeight { get { return NotificationFontCatalog.NormalizeWeight(desktopNotificationFontWeight); } set { SetValue(ref desktopNotificationFontWeight, NotificationFontCatalog.NormalizeWeight(value)); } }
        public string DesktopNotificationTextAlignment { get { return NotificationFontCatalog.NormalizeAlignment(desktopNotificationTextAlignment); } set { SetValue(ref desktopNotificationTextAlignment, NotificationFontCatalog.NormalizeAlignment(value)); } }
        public string DesktopNotificationAccentMode { get { return NotificationFontCatalog.NormalizeAccentMode(desktopNotificationAccentMode); } set { SetValue(ref desktopNotificationAccentMode, NotificationFontCatalog.NormalizeAccentMode(value)); } }
        public string DesktopNotificationAnimation { get { return NotificationFontCatalog.NormalizeAnimation(desktopNotificationAnimation); } set { SetValue(ref desktopNotificationAnimation, NotificationFontCatalog.NormalizeAnimation(value)); } }
        public bool DesktopNotificationShowTitle { get { return desktopNotificationShowTitle; } set { SetValue(ref desktopNotificationShowTitle, value); } }
        public int OverlayScalePercent { get { return overlayScalePercent; } set { SetValue(ref overlayScalePercent, value); } }
        public string OverlayDimColor { get { return overlayDimColor; } set { SetValue(ref overlayDimColor, value); } }
        public string OverlayCardColor { get { return overlayCardColor; } set { SetValue(ref overlayCardColor, value); } }
        public string OverlayAccentColor { get { return overlayAccentColor; } set { SetValue(ref overlayAccentColor, value); } }
        public string OverlayTextColor { get { return overlayTextColor; } set { SetValue(ref overlayTextColor, value); } }
        public string OverlayWarningColor { get { return overlayWarningColor; } set { SetValue(ref overlayWarningColor, value); } }
        public int OverlayTitleFontSize { get { return overlayTitleFontSize; } set { SetValue(ref overlayTitleFontSize, value); } }
        public int OverlayControllerFontSize { get { return overlayControllerFontSize; } set { SetValue(ref overlayControllerFontSize, value); } }
        public int OverlayInstructionFontSize { get { return overlayInstructionFontSize; } set { SetValue(ref overlayInstructionFontSize, value); } }
        public int OverlayStatusFontSize { get { return overlayStatusFontSize; } set { SetValue(ref overlayStatusFontSize, value); } }
        public int OverlayControllerIconSize { get { return overlayControllerIconSize; } set { SetValue(ref overlayControllerIconSize, value); } }
        public int OverlayStatusIconSize { get { return overlayStatusIconSize; } set { SetValue(ref overlayStatusIconSize, value); } }
        public bool OverlayShowControllerIcon { get { return overlayShowControllerIcon; } set { SetValue(ref overlayShowControllerIcon, value); } }
        public bool OverlayShowStatusIcon { get { return overlayShowStatusIcon; } set { SetValue(ref overlayShowStatusIcon, value); } }
        public bool OverlayShowControllerName { get { return overlayShowControllerName; } set { SetValue(ref overlayShowControllerName, value); } }
        public bool OverlayShowConnectionBadge { get { return overlayShowConnectionBadge; } set { SetValue(ref overlayShowConnectionBadge, value); } }
        public bool OverlayShowBatteryBadge { get { return overlayShowBatteryBadge; } set { SetValue(ref overlayShowBatteryBadge, value); } }
        public bool OverlayShowTitle { get { return overlayShowTitle; } set { SetValue(ref overlayShowTitle, value); } }
        public bool OverlayShowInstruction { get { return overlayShowInstruction; } set { SetValue(ref overlayShowInstruction, value); } }
        public bool OverlayShowPauseStatus { get { return overlayShowPauseStatus; } set { SetValue(ref overlayShowPauseStatus, value); } }
        public string OverlayControllerIconPosition { get { return overlayControllerIconPosition; } set { SetValue(ref overlayControllerIconPosition, value); } }
        public string OverlayCardPosition { get { return overlayCardPosition; } set { SetValue(ref overlayCardPosition, value); } }
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

            topPanelControllerMode = NormalizeTopPanelControllerMode(topPanelControllerMode);
            appearancePreset = SettingsAppearance.Normalize(appearancePreset);
            notificationStylePreset = NotificationStylePresets.Normalize(notificationStylePreset);
            overlayStylePreset = OverlayStylePresets.Normalize(overlayStylePreset);
            notificationSoundPack = NotificationSoundCatalog.Normalize(notificationSoundPack);
            notificationFontFamily = NotificationFontCatalog.Normalize(notificationFontFamily);
            desktopNotificationFontFamily = NotificationFontCatalog.Normalize(desktopNotificationFontFamily);
            overlayFontFamily = NotificationFontCatalog.Normalize(overlayFontFamily);
            notificationFontWeight = NotificationFontCatalog.NormalizeWeight(notificationFontWeight);
            desktopNotificationFontWeight = NotificationFontCatalog.NormalizeWeight(desktopNotificationFontWeight);
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

        private static int ClampPercent(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
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
        }

        public void EndEdit()
        {
            if (plugin != null)
            {
                var sidebarChanged = editingClone != null
                    && editingClone.Tester != null
                    && Tester != null
                    && Tester.ShowSidebarItem != editingClone.Tester.ShowSidebarItem;
                Tester.Normalize();
                plugin.SavePluginSettings(this);
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
                NotificationPadding < 0 || NotificationPadding > 40 ||
                NotificationElementSpacing < 0 || NotificationElementSpacing > 40 ||
                NotificationBorderThickness < 0 || NotificationBorderThickness > 10 ||
                NotificationCornerRadius < 0 || NotificationCornerRadius > 40 ||
                NotificationScreenMargin < 8 || NotificationScreenMargin > 64 ||
                DesktopNotificationIconSize < 16 || DesktopNotificationIconSize > 128 ||
                DesktopNotificationPadding < 0 || DesktopNotificationPadding > 40 ||
                DesktopNotificationElementSpacing < 0 || DesktopNotificationElementSpacing > 40 ||
                DesktopNotificationScreenMargin < 8 || DesktopNotificationScreenMargin > 64 ||
                OverlayScalePercent < 80 || OverlayScalePercent > 140 ||
                OverlayTitleFontSize < 18 || OverlayTitleFontSize > 64 ||
                OverlayControllerFontSize < 12 || OverlayControllerFontSize > 48 ||
                OverlayInstructionFontSize < 12 || OverlayInstructionFontSize > 40 ||
                OverlayStatusFontSize < 10 || OverlayStatusFontSize > 30 ||
                OverlayControllerIconSize < 16 || OverlayControllerIconSize > 128 ||
                OverlayStatusIconSize < 12 || OverlayStatusIconSize > 48 ||
                OverlayCardWidth < 320 || OverlayCardWidth > 1000 ||
                OverlayPadding < 12 || OverlayPadding > 80 ||
                OverlayElementSpacing < 0 || OverlayElementSpacing > 48 ||
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
                OverlayStylePreset = OverlayStylePreset,
                EnableNotificationSounds = EnableNotificationSounds,
                NotificationSoundPack = NotificationSoundPack,
                PlaySoundOnConnected = PlaySoundOnConnected,
                PlaySoundOnDisconnected = PlaySoundOnDisconnected,
                PlaySoundOnLowBattery = PlaySoundOnLowBattery,
                PlaySoundOnWarning = PlaySoundOnWarning,
                NotificationSoundVolume = NotificationSoundVolume,
                EnableDebugLogging = EnableDebugLogging,
                AutoUpdateControllerDatabase = AutoUpdateControllerDatabase,
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
                NotificationIconPosition = NotificationIconPosition,
                NotificationPadding = NotificationPadding,
                NotificationElementSpacing = NotificationElementSpacing,
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
                NotificationTextAlignment = NotificationTextAlignment,
                NotificationAccentMode = NotificationAccentMode,
                NotificationAnimation = NotificationAnimation,
                NotificationShowTitle = NotificationShowTitle,
                ShowControllerNameInDesktopNotifications = ShowControllerNameInDesktopNotifications,
                ShowDesktopControllerNotifications = ShowDesktopControllerNotifications,
                DesktopNotificationWidth = DesktopNotificationWidth,
                DesktopNotificationScalePercent = DesktopNotificationScalePercent,
                DesktopNotificationDurationMilliseconds = DesktopNotificationDurationMilliseconds,
                DesktopNotificationPosition = DesktopNotificationPosition,
                DesktopNotificationBackgroundColor = DesktopNotificationBackgroundColor,
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
                DesktopNotificationIconPosition = DesktopNotificationIconPosition,
                DesktopNotificationPadding = DesktopNotificationPadding,
                DesktopNotificationElementSpacing = DesktopNotificationElementSpacing,
                DesktopNotificationShowBorder = DesktopNotificationShowBorder,
                DesktopNotificationBorderPosition = DesktopNotificationBorderPosition,
                DesktopNotificationBorderThickness = DesktopNotificationBorderThickness,
                DesktopNotificationCornerRadius = DesktopNotificationCornerRadius,
                DesktopNotificationShowConnectionBadge = DesktopNotificationShowConnectionBadge,
                DesktopNotificationScreenMargin = DesktopNotificationScreenMargin,
                DesktopNotificationShowShadow = DesktopNotificationShowShadow,
                DesktopNotificationFontFamily = DesktopNotificationFontFamily,
                DesktopNotificationFontWeight = DesktopNotificationFontWeight,
                DesktopNotificationTextAlignment = DesktopNotificationTextAlignment,
                DesktopNotificationAccentMode = DesktopNotificationAccentMode,
                DesktopNotificationAnimation = DesktopNotificationAnimation,
                DesktopNotificationShowTitle = DesktopNotificationShowTitle,
                OverlayScalePercent = OverlayScalePercent,
                OverlayDimColor = OverlayDimColor,
                OverlayCardColor = OverlayCardColor,
                OverlayAccentColor = OverlayAccentColor,
                OverlayTextColor = OverlayTextColor,
                OverlayWarningColor = OverlayWarningColor,
                OverlayTitleFontSize = OverlayTitleFontSize,
                OverlayControllerFontSize = OverlayControllerFontSize,
                OverlayInstructionFontSize = OverlayInstructionFontSize,
                OverlayStatusFontSize = OverlayStatusFontSize,
                OverlayControllerIconSize = OverlayControllerIconSize,
                OverlayStatusIconSize = OverlayStatusIconSize,
                OverlayShowControllerIcon = OverlayShowControllerIcon,
                OverlayShowStatusIcon = OverlayShowStatusIcon,
                OverlayShowControllerName = OverlayShowControllerName,
                OverlayShowConnectionBadge = OverlayShowConnectionBadge,
                OverlayShowBatteryBadge = OverlayShowBatteryBadge,
                OverlayShowTitle = OverlayShowTitle,
                OverlayShowInstruction = OverlayShowInstruction,
                OverlayShowPauseStatus = OverlayShowPauseStatus,
                OverlayControllerIconPosition = OverlayControllerIconPosition,
                OverlayCardPosition = OverlayCardPosition,
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
            OverlayStylePreset = source.OverlayStylePreset;
            EnableNotificationSounds = source.EnableNotificationSounds;
            NotificationSoundPack = source.NotificationSoundPack;
            PlaySoundOnConnected = source.PlaySoundOnConnected;
            PlaySoundOnDisconnected = source.PlaySoundOnDisconnected;
            PlaySoundOnLowBattery = source.PlaySoundOnLowBattery;
            PlaySoundOnWarning = source.PlaySoundOnWarning;
            NotificationSoundVolume = source.NotificationSoundVolume;
            EnableDebugLogging = source.EnableDebugLogging;
            AutoUpdateControllerDatabase = source.AutoUpdateControllerDatabase;
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
            NotificationIconPosition = source.NotificationIconPosition;
            NotificationPadding = source.NotificationPadding;
            NotificationElementSpacing = source.NotificationElementSpacing;
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
            NotificationTextAlignment = source.NotificationTextAlignment;
            NotificationAccentMode = source.NotificationAccentMode;
            NotificationAnimation = source.NotificationAnimation;
            NotificationShowTitle = source.NotificationShowTitle;
            ShowControllerNameInDesktopNotifications = source.ShowControllerNameInDesktopNotifications;
            DesktopNotificationWidth = source.DesktopNotificationWidth;
            DesktopNotificationScalePercent = source.DesktopNotificationScalePercent;
            DesktopNotificationDurationMilliseconds = source.DesktopNotificationDurationMilliseconds;
            DesktopNotificationPosition = source.DesktopNotificationPosition;
            DesktopNotificationBackgroundColor = source.DesktopNotificationBackgroundColor;
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
            DesktopNotificationIconPosition = source.DesktopNotificationIconPosition;
            DesktopNotificationPadding = source.DesktopNotificationPadding;
            DesktopNotificationElementSpacing = source.DesktopNotificationElementSpacing;
            DesktopNotificationShowBorder = source.DesktopNotificationShowBorder;
            DesktopNotificationBorderPosition = source.DesktopNotificationBorderPosition;
            DesktopNotificationBorderThickness = source.DesktopNotificationBorderThickness;
            DesktopNotificationCornerRadius = source.DesktopNotificationCornerRadius;
            DesktopNotificationShowConnectionBadge = source.DesktopNotificationShowConnectionBadge;
            DesktopNotificationScreenMargin = source.DesktopNotificationScreenMargin;
            DesktopNotificationShowShadow = source.DesktopNotificationShowShadow;
            DesktopNotificationFontFamily = source.DesktopNotificationFontFamily;
            DesktopNotificationFontWeight = source.DesktopNotificationFontWeight;
            DesktopNotificationTextAlignment = source.DesktopNotificationTextAlignment;
            DesktopNotificationAccentMode = source.DesktopNotificationAccentMode;
            DesktopNotificationAnimation = source.DesktopNotificationAnimation;
            DesktopNotificationShowTitle = source.DesktopNotificationShowTitle;
            OverlayScalePercent = source.OverlayScalePercent;
            OverlayDimColor = source.OverlayDimColor;
            OverlayCardColor = source.OverlayCardColor;
            OverlayAccentColor = source.OverlayAccentColor;
            OverlayTextColor = source.OverlayTextColor;
            OverlayWarningColor = source.OverlayWarningColor;
            OverlayTitleFontSize = source.OverlayTitleFontSize;
            OverlayControllerFontSize = source.OverlayControllerFontSize;
            OverlayInstructionFontSize = source.OverlayInstructionFontSize;
            OverlayStatusFontSize = source.OverlayStatusFontSize;
            OverlayControllerIconSize = source.OverlayControllerIconSize;
            OverlayStatusIconSize = source.OverlayStatusIconSize;
            OverlayShowControllerIcon = source.OverlayShowControllerIcon;
            OverlayShowStatusIcon = source.OverlayShowStatusIcon;
            OverlayShowControllerName = source.OverlayShowControllerName;
            OverlayShowConnectionBadge = source.OverlayShowConnectionBadge;
            OverlayShowBatteryBadge = source.OverlayShowBatteryBadge;
            OverlayShowTitle = source.OverlayShowTitle;
            OverlayShowInstruction = source.OverlayShowInstruction;
            OverlayShowPauseStatus = source.OverlayShowPauseStatus;
            OverlayControllerIconPosition = source.OverlayControllerIconPosition;
            OverlayCardPosition = source.OverlayCardPosition;
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
            yield return NotificationTextColor;
            yield return NotificationSecondaryTextColor;
            yield return NotificationConnectedColor;
            yield return NotificationDisconnectedColor;
            yield return NotificationWarningColor;
            yield return NotificationLowBatteryColor;
            yield return OverlayDimColor;
            yield return OverlayCardColor;
            yield return OverlayAccentColor;
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
