using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Win32;
using ControllerSessionManager.Controllers;
using ControllerSessionManager.Overlay;
using ControllerSessionManager.Sessions;
using ControllerSessionManager.Tester;
using ControllerSessionManager.Tester.ViewModels;
using ControllerSessionManager.Tester.Views;
using ControllerSessionManager.Tester.Views.ThemeIntegration;
using ControllerSessionManager.Tester.Services;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class ControllerSessionManagerPlugin : GenericPlugin
    {
        private readonly ILogger logger;
        private readonly ControllerManager controllerManager;
        private readonly XInputProvider xInputProvider;
        private readonly DispatcherTimer reconciliationTimer;
        private readonly DispatcherTimer xInputTimer;
        private readonly DispatcherTimer sessionTimer;
        private readonly DispatcherTimer creatorThemeUpdateTimer;
        private readonly GameSessionManager sessionManager;
        private readonly GamePauseService gamePauseService;
        private readonly OnlineSessionDetector onlineSessionDetector;
        private readonly AdaptiveSessionScopeDetector adaptiveSessionScopeDetector;
        private readonly PauseAttemptGate pauseAttemptGate;
        private readonly OverlayClient overlayClient;
        private readonly NotificationAudioService notificationAudio;
        private int notificationSoundCleanupGeneration;
        private readonly DiagnosticEventBuffer diagnosticEvents;
        private readonly ControllerMappingDatabaseUpdater controllerDatabaseUpdater;
        private readonly CreatorThemeUpdater creatorThemeUpdater;
        private ResourceDictionary englishFallbackResources;
        private ControllerSessionManagerSettings settings;
        private bool disposed;
        private bool playniteBridgeAvailable = true;
        private bool playniteBridgeWarningLogged;
        private string lastXInputSignature;
        private string lastDisplaySignature;
        private readonly ControllerDisplayHold displayHold = new ControllerDisplayHold();
        private DateTime sdlQuietUntilUtc;
        private static readonly TimeSpan SdlHotPlugQuietPeriod = TimeSpan.FromSeconds(2);
        private Guid? activeGameId;
        private Guid activeSessionId;
        private Guid? activeDisconnectIncidentId;
        private PauseReceipt activePauseReceipt;
        private bool activeForcePauseRequested;
        private bool activeOnlineNotificationOnly;
        private bool activeNetworkSafetyDetected;
        private int activeGameProcessId;
        private readonly Guid notificationSessionId = Guid.NewGuid();
        private readonly Dictionary<string, ControllerToastIdentity> connectedToastControllers =
            new Dictionary<string, ControllerToastIdentity>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ControllerToastCandidate> pendingToastControllers =
            new Dictionary<string, ControllerToastCandidate>(StringComparer.OrdinalIgnoreCase);
        private int followUpToastSoundDelayMilliseconds;
        private readonly LowBatteryNotificationTracker lowBatteryToastTracker =
            new LowBatteryNotificationTracker();
        private bool connectionToastStateInitialized;
        private bool lowBatteryToastStateInitialized;
        private readonly DateTime pluginStartedUtc = DateTime.UtcNow;
        private static readonly TimeSpan ToastStartupGracePeriod = TimeSpan.FromSeconds(8);
        private List<string> activeGameOnlineMetadata = new List<string>();
        private bool adaptiveLocalScopeLogged;
        private SessionProtectionPolicy activeSessionPolicy;
        private TopPanelItem controllerTopPanelItem;
        private TesterIntegration testerIntegration;
        private bool openingStandaloneSettings;
        private bool automaticCreatorThemeStartupCheckCompleted;
        private DispatcherTimer overlayPreviewHideTimer;
        private DateTime lastFullscreenLaunchUtc = DateTime.MinValue;
        private DateTime? guideButtonPressedUtc;
        private static readonly TimeSpan FullscreenLaunchCooldown = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan FullscreenRestoreSettleDelay = TimeSpan.FromMilliseconds(200);
        // Fire on release only, in this window: long holds (power-off) and short taps are ignored.
        private static readonly TimeSpan GuideFullscreenMinHold = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan GuideFullscreenMaxHold = TimeSpan.FromSeconds(2);

        public override Guid Id
        {
            get { return Guid.Parse("6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc"); }
        }

        public ControllerThemeApi Theme { get; private set; }

        public GamepadTesterThemeIntegration TesterTheme
        {
            get { return testerIntegration == null ? null : testerIntegration.ThemeIntegration; }
        }

        public event EventHandler ControllerSnapshotChanged;

        public ControllerSessionManagerPlugin(IPlayniteAPI playniteApi) : base(playniteApi)
        {
            logger = LogManager.GetLogger();
            var pluginDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
            var userDataDirectory = GetPluginUserDataPath();
            CreatorThemeCatalog.Configure(pluginDirectory, userDataDirectory);
            creatorThemeUpdater = new CreatorThemeUpdater(CreatorThemeCatalog.DownloadedRoot);
            ImportedVisualProfileCatalog.Configure(userDataDirectory);
            controllerDatabaseUpdater = new ControllerMappingDatabaseUpdater(
                Path.Combine(pluginDirectory, "gamecontrollerdb.txt"), userDataDirectory);
            if (!controllerDatabaseUpdater.ConfigureActiveDatabase())
            {
                logger.Warn("Controller mapping database was unavailable; SDL built-in mappings will be used.");
            }
            TesterHostClient.ConfigureMappingDatabase(controllerDatabaseUpdater.ActivePath);
            controllerManager = new ControllerManager();
            controllerManager.SnapshotChanged += OnManagerSnapshotChanged;
            xInputProvider = new XInputProvider();
            reconciliationTimer = new DispatcherTimer(DispatcherPriority.Background);
            reconciliationTimer.Tick += OnReconciliationTimerTick;
            xInputTimer = new DispatcherTimer(DispatcherPriority.Background);
            xInputTimer.Tick += OnXInputTimerTick;
            sessionTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            sessionTimer.Tick += OnSessionTimerTick;
            creatorThemeUpdateTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromHours(1)
            };
            creatorThemeUpdateTimer.Tick += OnCreatorThemeUpdateTimerTick;
            sessionManager = new GameSessionManager();
            sessionManager.EventOccurred += OnSessionEventOccurred;
            gamePauseService = new GamePauseService();
            onlineSessionDetector = new OnlineSessionDetector();
            adaptiveSessionScopeDetector = new AdaptiveSessionScopeDetector();
            pauseAttemptGate = new PauseAttemptGate();
            overlayClient = new OverlayClient(logger);
            notificationAudio = new NotificationAudioService(
                logger, Path.GetDirectoryName(GetType().Assembly.Location), PlayniteApi);
            diagnosticEvents = new DiagnosticEventBuffer(200);
            Theme = new ControllerThemeApi();

            Properties = new GenericPluginProperties { HasSettings = true };
            EnsureEnglishFallbackResources();
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                SourceName = "ControllerSessionManager",
                ElementList = new List<string>
                {
                    "ControllerStatus",
                    "ControllerCount",
                    "PrimaryController",
                    "ControllerIcon",
                    "TopPanelIcon",
                    "ControllerBatteryText",
                    "ControllerBatteryDot",
                    "TesterLauncher",
                    "TesterStatusBadge",
                    "TesterButtonMap",
                    "TesterStickCheck",
                    "TesterTriggerCheck",
                    "TesterRumblePad",
                    "TesterLatencyMini"
                }
            });
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                SourceName = "GamepadTester",
                ElementList = new List<string>(GamepadTesterThemeContract.BlockNames)
            });
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "ControllerSessionManager",
                SettingsRoot = "Theme"
            });
            AddConvertersSupport(new AddConvertersSupportArgs
            {
                SourceName = "ControllerSessionManager",
                Converters = new List<System.Windows.Data.IValueConverter>
                {
                    new IconGeometryConverter()
                }
            });
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "GamepadTester",
                SettingsRoot = "TesterTheme"
            });

            settings = new ControllerSessionManagerSettings(this);
            testerIntegration = new TesterIntegration(PlayniteApi, logger, settings.Tester, Loc,
                OpenTesterSettings, () => settings != null && settings.EnableDebugLogging,
                () => settings != null ? settings.AppearancePreset : SettingsAppearance.Midnight);
            ApplySettings();
            logger.Info(string.Format("Controller Manager {0} initialized.",
                GetType().Assembly.GetName().Version.ToString(3)));
            diagnosticEvents.Add("lifecycle", "Plugin initialized in " + PlayniteApi.ApplicationInfo.Mode + " mode");
        }

        public string Loc(string key)
        {
            var value = PlayniteApi.Resources.GetString(key);
            if (!string.IsNullOrWhiteSpace(value) && value != key)
            {
                return value;
            }

            return GetEnglishFallbackString(key) ?? key;
        }

        public IReadOnlyList<ControllerDeviceSnapshot> GetControllerSnapshot()
        {
            var snapshot = controllerManager.GetSnapshot();
            foreach (var controller in snapshot)
            {
                var profile = settings == null ? null : settings.GetControllerProfile(
                    string.IsNullOrWhiteSpace(controller.HardwareId) ? controller.ControllerId : controller.HardwareId);
                if (profile == null)
                {
                    controller.IconId = ControllerIconCatalog.Suggest(controller);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(profile.CustomName))
                {
                    controller.Name = profile.CustomName.Trim();
                }

                controller.IconId = ControllerIconCatalog.ResolveId(controller,
                    profile.IconId);
            }

            return snapshot;
        }

        public IReadOnlyList<ControllerDeviceSnapshot> GetDisplayControllerSnapshot()
        {
            return FilterUnknownConnections(displayHold.Apply(GetControllerSnapshot(), DateTime.UtcNow));
        }

        private static IReadOnlyList<ControllerDeviceSnapshot> FilterUnknownConnections(
            IEnumerable<ControllerDeviceSnapshot> source)
        {
            // Charging docks often stay enumerated as Unknown while the pad is off; keep those
            // passive HID rows out of Mandos, TopBar and connect/disconnect toasts. A confirmed
            // XInput slot remains actionable even when its driver hides the physical transport.
            var candidates = (source ?? Enumerable.Empty<ControllerDeviceSnapshot>()).ToList();
            return candidates
                .Where(ControllerDeviceIdentity.ShouldDisplayController)
                .Where(a => !ControllerDeviceIdentity.IsLikelyPassiveChargingDock(a, candidates))
                .ToList();
        }

        public void RefreshControllers()
        {
            if (disposed || settings == null || !settings.EnableMonitoring)
            {
                return;
            }

            if (playniteBridgeAvailable && DateTime.UtcNow >= sdlQuietUntilUtc)
            {
                try
                {
                    // Establish the authoritative SDK inventory before supplemental polling can
                    // publish a startup transition or stale transport observation.
                    // Skip this during the hot-plug quiet window: Playnite's controller API is
                    // SDL-backed, and enumerating joysticks while a pad is appearing or vanishing
                    // can abort the process with no managed exception.
                    controllerManager.Reconcile(PlayniteApi.GetConnectedControllers());
                }
                catch (NullReferenceException ex)
                {
                    playniteBridgeAvailable = false;
                    if (!playniteBridgeWarningLogged)
                    {
                        playniteBridgeWarningLogged = true;
                        logger.Warn(ex, "Playnite controller enumeration is unavailable in this application mode. XInput remains a startup fallback.");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to reconcile Playnite controllers.");
                }
            }

            PollXInput();
        }

        public bool TryVibrateController(ControllerDeviceSnapshot controller)
        {
            if (controller == null || !controller.IsConnected)
            {
                return false;
            }

            try
            {
                RefreshControllers();
                var connected = GetControllerSnapshot().Where(a => a.IsConnected).ToList();
                var current = connected.FirstOrDefault(a =>
                    string.Equals(a.ControllerId, controller.ControllerId, StringComparison.OrdinalIgnoreCase)) ??
                    connected.FirstOrDefault(a => !string.IsNullOrWhiteSpace(controller.HardwareId) &&
                        string.Equals(a.HardwareId, controller.HardwareId, StringComparison.OrdinalIgnoreCase));
                if (current == null)
                {
                    var sameName = connected.Where(a =>
                        string.Equals(a.DetectedName, controller.DetectedName,
                            StringComparison.CurrentCultureIgnoreCase)).ToList();
                    current = sameName.Count == 1 ? sameName[0] : null;
                }

                if (current == null)
                {
                    return false;
                }

                if (settings.Tester != null && !settings.Tester.EnableRumbleTests)
                {
                    return false;
                }

                var vendorId = current.VendorId;
                var productId = current.ProductId;
                var providerId = current.ProviderId;
                var instanceId = current.ProviderInstanceId;
                Task.Run(delegate
                {
                    try
                    {
                        if (testerIntegration != null && testerIntegration.TryStandardRumble(vendorId, productId))
                        {
                            xInputProvider.StopVibrate(providerId, instanceId);
                            return;
                        }

                        xInputProvider.TryVibrate(providerId, instanceId);
                    }
                    catch (Exception rumbleEx)
                    {
                        logger.Warn(rumbleEx, "Controller vibration test failed.");
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Controller vibration test failed.");
                return false;
            }
        }

        public string GetSessionStatusText()
        {
            if (!sessionManager.IsRunning)
            {
                return Loc("LOCCSM_SessionIdle");
            }

            if (sessionManager.ConfirmedDisconnectCount > 0)
            {
                var disconnected = sessionManager.ActiveControllers
                    .Where(a => a.DisconnectConfirmed)
                    .Select(a => a.Name);
                return string.Format(Loc("LOCCSM_SessionDisconnectConfirmed"),
                    string.Join(", ", disconnected));
            }

            if (sessionManager.SuspectedDisconnectCount > 0)
            {
                return Loc("LOCCSM_SessionGracePeriod");
            }

            if (adaptiveSessionScopeDetector.IsLocalMultiplayer)
            {
                return string.Format(Loc("LOCCSM_SessionAdaptiveLocal"),
                    sessionManager.ActiveControllers.Count);
            }

            return sessionManager.ActiveControllers.Count == 0
                ? Loc("LOCCSM_SessionWaitingForInput")
                : Loc("LOCCSM_SessionTracking");
        }

        public string GetSessionStatusBadge()
        {
            if (!sessionManager.IsRunning)
            {
                return Loc("LOCCSM_BadgeIdle");
            }

            if (sessionManager.ConfirmedDisconnectCount > 0)
            {
                return Loc("LOCCSM_BadgeAlert");
            }

            if (sessionManager.SuspectedDisconnectCount > 0)
            {
                return Loc("LOCCSM_BadgeWaiting");
            }

            if (adaptiveSessionScopeDetector.IsLocalMultiplayer)
            {
                return Loc("LOCCSM_BadgeLocal");
            }

            return sessionManager.ActiveControllers.Count == 0
                ? Loc("LOCCSM_BadgeWaiting")
                : Loc("LOCCSM_BadgeWatching");
        }

        public string GetActiveSessionControllersText()
        {
            var active = sessionManager.ActiveControllers;
            return active.Count == 0
                ? Loc("LOCCSM_NoActiveSessionControllers")
                : string.Join(", ", active.Select(a => a.DisconnectConfirmed
                    ? string.Format("{0}. {1}", a.Name, Loc("LOCCSM_Disconnected"))
                    : a.Name));
        }

        public string GetPrimaryControllerText()
        {
            var sessionPrimary = sessionManager.IsRunning
                ? sessionManager.ActiveControllers.OrderByDescending(a => a.LastInputUtc).FirstOrDefault()
                : null;
            if (sessionPrimary != null)
            {
                return sessionPrimary.DisconnectConfirmed
                    ? string.Format("{0}. {1}", sessionPrimary.Name, Loc("LOCCSM_Disconnected"))
                    : sessionPrimary.Name;
            }

            var primary = GetDisplayControllerSnapshot().Where(a => a.IsConnected)
                .OrderByDescending(a => a.LastInputUtc.HasValue)
                .ThenByDescending(a => a.LastInputUtc)
                .FirstOrDefault();
            return primary == null ? Loc("LOCCSM_NoControllers") : primary.Name;
        }

        public void ShowVibrationUnavailable()
        {
            PlayniteApi.Dialogs.ShowMessage(Loc("LOCCSM_VibrationUnavailable"), Loc("LOCCSM_TestVibration"));
        }

        public void ShowNotificationPreview(string kind, bool playSound = true)
        {
            var isLowBattery = string.Equals(kind, "lowbattery", StringComparison.OrdinalIgnoreCase);
            var previewKind = string.Equals(kind, "disconnected", StringComparison.OrdinalIgnoreCase)
                ? "disconnected"
                : string.Equals(kind, "warning", StringComparison.OrdinalIgnoreCase)
                    ? "warning"
                    : isLowBattery
                        ? "lowbattery"
                        : "connected";
            var isWarning = previewKind == "warning";
            var title = isLowBattery
                ? Loc("LOCCSM_ControllerLowBatteryToast")
                : isWarning
                    ? Loc("LOCCSM_OnlineFallbackToastTitle")
                    : Loc(previewKind == "connected" ? "LOCCSM_ControllerConnectedToast" : "LOCCSM_ControllerDisconnectedToast");
            var message = isLowBattery
                ? (settings.ShowControllerNameInNotifications
                    ? Loc("LOCCSM_NotificationPreviewMessage") + " · " + Loc("LOCCSM_ValueLow")
                    : Loc("LOCCSM_ValueLow"))
                : isWarning
                    ? Loc("LOCCSM_OnlineFallbackToastMessage")
                    : settings.ShowControllerNameInNotifications ? Loc("LOCCSM_NotificationPreviewMessage") : string.Empty;
            var iconFile = ControllerIconCatalog.DefaultFileName;
            overlayClient.ShowToastPreview(notificationSessionId, GetToastTargetProcessId(), previewKind, title, message,
                SvgIconGeometryLoader.GetPathData(iconFile),
                settings.NotificationDurationMilliseconds, GetToastStylePayload(),
                GetToastBadgeIconGeometry(previewKind, "Wireless"),
                GetPreviewTargetWindowHandle());
            if (playSound)
            {
                PlayNotificationSound(SoundKindFromToast(previewKind), preview: true);
            }
        }

        public void ShowOverlayPreview()
        {
            if (settings == null || overlayClient == null) return;
            if (activeDisconnectIncidentId.HasValue) return;

            var iconFile = ControllerIconCatalog.DefaultFileName;
            overlayClient.Show(notificationSessionId, Guid.NewGuid(), GetToastTargetProcessId(),
                Loc("LOCCSM_OverlayDisconnectTitle"),
                Loc("LOCCSM_PreviewControllerName"),
                Loc("LOCCSM_OverlayAllowTakeover"),
                Loc("LOCCSM_OverlayPauseDisabled"), "ok",
                SvgIconGeometryLoader.GetPathData("player-pause.svg"),
                SvgIconGeometryLoader.GetPathData(iconFile), false, 0,
                string.Empty, "warning",
                SvgIconGeometryLoader.GetPathData("alert-triangle.svg"), GetOverlayStylePayload(),
                Loc("LOCCSM_ValueBluetooth"), Loc("LOCCSM_ValueFull"),
                ControllerConnectionIcons.GetPathData("Bluetooth"),
                SvgIconGeometryLoader.GetPathData("battery.svg"), "Full",
                Loc("LOCCSM_Disconnected"), Loc("LOCCSM_OverlayDisconnectTimerFormat"));

            var duration = settings.NotificationDurationMilliseconds;
            if (duration < 4000) duration = 4000;
            if (duration > 12000) duration = 12000;
            var dispatcher = GetUiDispatcher();
            if (dispatcher == null) return;
            dispatcher.BeginInvoke(new Action(() =>
            {
                if (overlayPreviewHideTimer == null)
                {
                    overlayPreviewHideTimer = new DispatcherTimer(DispatcherPriority.Background)
                    {
                        Interval = TimeSpan.FromMilliseconds(duration)
                    };
                    overlayPreviewHideTimer.Tick += delegate
                    {
                        overlayPreviewHideTimer.Stop();
                        overlayClient.HideAll(notificationSessionId);
                    };
                }
                overlayPreviewHideTimer.Stop();
                overlayPreviewHideTimer.Interval = TimeSpan.FromMilliseconds(duration);
                overlayPreviewHideTimer.Start();
            }));
        }

        public async Task<CreatorThemeUpdateResult> UpdateCreatorThemesAsync(
            CancellationToken cancellationToken)
        {
            var result = await creatorThemeUpdater.CheckForUpdatesAsync(cancellationToken);
            if (result != null && result.Succeeded && settings != null)
            {
                settings.CreatorThemeLastUpdateUtc = DateTime.UtcNow.ToString("o");
                SavePluginSettings(settings);
            }
            return result;
        }

        public void ShowCreatorThemeUpdateResult(CreatorThemeUpdateResult result)
        {
            if (result == null)
            {
                return;
            }
            if (!result.Succeeded)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(result.Error,
                    Loc("LOCCSM_CreatorThemesUpdateTitle"));
                return;
            }

            var message = result.CatalogCurrent
                ? Loc("LOCCSM_CreatorThemesCurrent")
                : string.Format(Loc("LOCCSM_CreatorThemesUpdated"), result.Installed,
                    result.Updated, result.Incompatible);
            PlayniteApi.Dialogs.ShowMessage(message, Loc("LOCCSM_CreatorThemesUpdateTitle"));
        }

        public MessageBoxResult ConfirmReplaceUnsavedNotificationStyle()
        {
            return PlayniteApi.Dialogs.ShowMessage(
                Loc("LOCCSM_UnsavedNotificationStyleMessage"),
                Loc("LOCCSM_UnsavedNotificationStyleTitle"),
                MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        }

        public bool ConfirmCopyNotificationStyle(bool desktopToFullscreen)
        {
            return PlayniteApi.Dialogs.ShowMessage(
                Loc(desktopToFullscreen
                    ? "LOCCSM_CopyDesktopStyleConfirm"
                    : "LOCCSM_CopyFullscreenStyleConfirm"),
                Loc("LOCCSM_CopyNotificationStyleTitle"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public void ShowNotificationPresetPreview()
        {
            ShowNotificationPresetPreview(PlayniteApi.ApplicationInfo.Mode != ApplicationMode.Fullscreen);
        }

        public void ShowNotificationPresetPreview(bool desktop)
        {
            if (desktop) ShowDesktopNotificationPreview("connected", true);
            else ShowNotificationPreview("connected", true);
        }

        public void PlayNotificationSoundPreview(string kind)
        {
            PlayNotificationSound(SoundKindFromToast(kind), preview: true);
        }

        private void PlayNotificationSound(NotificationSoundKind kind, bool preview = false,
            NotificationSoundScope scope = NotificationSoundScope.Fullscreen, int delayMilliseconds = 0)
        {
            if (notificationAudio == null || settings == null)
            {
                return;
            }

            if (preview)
            {
                notificationAudio.PlayPreview(kind, settings);
                return;
            }

            if (delayMilliseconds > 0)
            {
                var dispatcher = GetUiDispatcher();
                if (dispatcher != null)
                {
                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(delayMilliseconds)
                    };
                    timer.Tick += (sender, args) =>
                    {
                        timer.Stop();
                        notificationAudio.Play(kind, settings, scope);
                    };
                    timer.Start();
                    return;
                }
            }

            notificationAudio.Play(kind, settings, scope);
        }

        private static NotificationSoundKind SoundKindFromToast(string kind)
        {
            if (string.Equals(kind, "disconnected", StringComparison.OrdinalIgnoreCase))
            {
                return NotificationSoundKind.Disconnected;
            }

            if (string.Equals(kind, "warning", StringComparison.OrdinalIgnoreCase))
            {
                return NotificationSoundKind.Warning;
            }

            if (string.Equals(kind, "lowbattery", StringComparison.OrdinalIgnoreCase))
            {
                return NotificationSoundKind.LowBattery;
            }

            return NotificationSoundKind.Connected;
        }

        public void ExportHidDiagnostics()
        {
            try
            {
                RefreshControllers();
                var report = HidDiagnosticsService.CreateReport(GetControllerSnapshot());
                var dialog = new SaveFileDialog
                {
                    Title = Loc("LOCCSM_ExportHidDiagnostics"),
                    Filter = "Text files (*.txt)|*.txt",
                    FileName = "ControllerSessionManager_HID_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt"
                };
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                File.WriteAllText(dialog.FileName, report, Encoding.UTF8);
                PlayniteApi.Dialogs.ShowMessage(
                    string.Format(Loc("LOCCSM_HidDiagnosticsExported"), dialog.FileName),
                    Loc("LOCCSM_HidDiagnostics"));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to export HID diagnostics.");
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, Loc("LOCCSM_HidDiagnostics"));
            }
        }

        public void ExportSupportReport()
        {
            try
            {
                RefreshControllers();
                var report = SupportReportService.CreateReport(
                    GetType().Assembly.GetName().Version.ToString(3),
                    PlayniteApi.ApplicationInfo.Mode.ToString(), settings, GetControllerSnapshot(),
                    activeGameId, activeGameProcessId, activeSessionPolicy, sessionManager,
                    diagnosticEvents.Snapshot());
                var dialog = new SaveFileDialog
                {
                    Title = Loc("LOCCSM_ExportSupportReport"),
                    Filter = "Text files (*.txt)|*.txt",
                    FileName = "ControllerSessionManager_Support_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt"
                };
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                File.WriteAllText(dialog.FileName, report, Encoding.UTF8);
                PlayniteApi.Dialogs.ShowMessage(
                    string.Format(Loc("LOCCSM_SupportReportExported"), dialog.FileName),
                    Loc("LOCCSM_SupportReport"));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to export the support report.");
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, Loc("LOCCSM_SupportReport"));
            }
        }

        public void ExportVisualProfile(ControllerSessionManagerSettings targetSettings)
        {
            try
            {
                if (targetSettings == null)
                {
                    return;
                }

                var nameResult = PlayniteApi.Dialogs.SelectString(
                    Loc("LOCCSM_VisualProfileNamePrompt"),
                    Loc("LOCCSM_ExportVisualProfile"),
                    "Controller Manager");
                if (!nameResult.Result || string.IsNullOrWhiteSpace(nameResult.SelectedString))
                {
                    return;
                }
                var profileName = nameResult.SelectedString.Trim();
                var invalid = Path.GetInvalidFileNameChars();
                var safeFileName = new string(profileName.Select(a => invalid.Contains(a) ? '_' : a).ToArray());
                if (string.IsNullOrWhiteSpace(safeFileName)) safeFileName = "ControllerManager_Visual";

                var dialog = new SaveFileDialog
                {
                    Title = Loc("LOCCSM_ExportVisualProfile"),
                    Filter = Loc("LOCCSM_VisualProfileFileFilter"),
                    FileName = safeFileName + VisualProfileSnapshot.FileExtension,
                    DefaultExt = VisualProfileSnapshot.FileExtension.TrimStart('.')
                };
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var snapshot = VisualProfileSnapshot.FromSettings(targetSettings, profileName);
                VisualProfilePortableStore.Export(snapshot, dialog.FileName);
                PlayniteApi.Dialogs.ShowMessage(
                    string.Format(Loc("LOCCSM_VisualProfileExported"), dialog.FileName),
                    Loc("LOCCSM_VisualProfileTitle"));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to export visual profile.");
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, Loc("LOCCSM_VisualProfileTitle"));
            }
        }

        public void ImportVisualProfile(ControllerSessionManagerSettings targetSettings, Action onApplied)
        {
            try
            {
                if (targetSettings == null)
                {
                    return;
                }

                var dialog = new OpenFileDialog
                {
                    Title = Loc("LOCCSM_ImportVisualProfile"),
                    Filter = Loc("LOCCSM_VisualProfileFileFilter")
                };
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var snapshot = VisualProfilePortableStore.Import(dialog.FileName);
                var profileName = string.IsNullOrWhiteSpace(snapshot.Name)
                    ? Path.GetFileNameWithoutExtension(dialog.FileName)
                    : snapshot.Name;
                if (PlayniteApi.Dialogs.ShowMessage(
                        string.Format(Loc("LOCCSM_VisualProfileImportConfirm"), profileName),
                        Loc("LOCCSM_ImportVisualProfile"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                var importedId = ImportedVisualProfileCatalog.Import(dialog.FileName);
                ApplyImportedVisualProfile(targetSettings, importedId, null);
                if (onApplied != null)
                {
                    onApplied();
                }

                PlayniteApi.Dialogs.ShowMessage(
                    string.Format(Loc("LOCCSM_VisualProfileImported"), profileName),
                    Loc("LOCCSM_VisualProfileTitle"));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to import visual profile.");
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, Loc("LOCCSM_VisualProfileTitle"));
            }
        }

        public void ApplySettings()
        {
            reconciliationTimer.Stop();
            xInputTimer.Stop();
            sessionTimer.Stop();
            if (settings == null || !settings.EnableMonitoring || disposed)
            {
                EndDisconnectIncident();
                sessionManager.Stop();
                UpdateThemeApi();
                return;
            }

            var seconds = Math.Max(2, Math.Min(60, settings.ReconciliationIntervalSeconds));
            reconciliationTimer.Interval = TimeSpan.FromSeconds(seconds);
            reconciliationTimer.Start();
            xInputTimer.Interval = TimeSpan.FromMilliseconds(250);
            xInputTimer.Start();
            if (activeGameId.HasValue)
            {
                activeSessionPolicy = settings.GetSessionPolicy(activeGameId.Value);
            }
            if (activeGameId.HasValue && activeSessionPolicy != null && activeSessionPolicy.Enabled)
            {
                if (!sessionManager.IsRunning)
                {
                    sessionManager.Start(activeGameId.Value, DateTime.UtcNow);
                }
                sessionTimer.Start();
            }
            else
            {
                EndDisconnectIncident();
                sessionManager.Stop();
            }
            if (!settings.ShowDisconnectOverlay)
            {
                HideOverlayWindow();
            }
            else if (sessionManager.IsRunning && sessionManager.ConfirmedDisconnectCount > 0)
            {
                RefreshDisconnectOverlay();
            }
            RefreshControllers();
            UpdateThemeApi();
            if (testerIntegration != null)
            {
                testerIntegration.UpdateSettings(settings.Tester);
            }
        }

        /// <summary>
        /// Playnite only rebuilds sidebar items on startup. Use the same restart
        /// prompt as Playnite's own settings (LOCSettingsRestart*).
        /// </summary>
        public void OfferPlayniteRestartForSidebarChange()
        {
            try
            {
                var message = ResolvePlayniteString(
                    "LOCSettingsRestartAskMessage",
                    "Playnite needs to be restarted to apply new settings. Restart now?");
                var title = ResolvePlayniteString(
                    "LOCSettingsRestartTitle",
                    "Restart Playnite?");
                if (PlayniteApi.Dialogs.ShowMessage(
                        message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                    != MessageBoxResult.Yes)
                {
                    return;
                }

                // Defer so EndEdit / settings dialog can finish closing first.
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(RestartPlayniteApplication),
                    DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to offer Playnite restart after sidebar setting change.");
            }
        }

        private string ResolvePlayniteString(string key, string fallback)
        {
            try
            {
                var value = PlayniteApi.Resources.GetString(key);
                if (!string.IsNullOrWhiteSpace(value) &&
                    !string.Equals(value, key, StringComparison.Ordinal))
                {
                    return value;
                }
            }
            catch
            {
            }

            return fallback;
        }

        private void RestartPlayniteApplication()
        {
            try
            {
                var appType = Type.GetType("Playnite.PlayniteApplication, Playnite", false);
                if (appType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (!string.Equals(assembly.GetName().Name, "Playnite", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        appType = assembly.GetType("Playnite.PlayniteApplication", false);
                        if (appType != null)
                        {
                            break;
                        }
                    }
                }

                if (appType == null)
                {
                    logger.Error("PlayniteApplication type was not found; cannot restart.");
                    return;
                }

                var current = appType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
                var instance = current == null ? null : current.GetValue(null, null);
                if (instance == null)
                {
                    logger.Error("PlayniteApplication.Current was null; cannot restart.");
                    return;
                }

                var restartWithBool = instance.GetType().GetMethod(
                    "Restart", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
                if (restartWithBool != null)
                {
                    restartWithBool.Invoke(instance, new object[] { true });
                    return;
                }

                var restart = instance.GetType().GetMethod(
                    "Restart", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (restart != null)
                {
                    restart.Invoke(instance, null);
                    return;
                }

                logger.Error("PlayniteApplication.Restart method was not found.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to restart Playnite after sidebar setting change.");
            }
        }

        public GamepadTesterView CreateTesterView(out GamepadTesterViewModel viewModel)
        {
            viewModel = null;
            if (testerIntegration == null)
            {
                return null;
            }

            return testerIntegration.CreateEmbeddedView(out viewModel);
        }

        public void OpenTesterSettings()
        {
            TesterIntegration.PendingOpenSettingsTab = true;
            OpenStandaloneSettingsView();
        }

        public void OpenTesterForController(ushort vendorId, ushort productId, string name)
        {
            TesterIntegration.PendingTabIndex = 0;
            TesterIntegration.PendingOpenSettingsTab = true;
            TesterIntegration.RequestController(vendorId, productId, name);
            OpenTesterSettings();
        }

        public bool IsLegacyGamepadTesterInstalled()
        {
            var roots = new List<string>();
            try
            {
                if (PlayniteApi != null && PlayniteApi.Paths != null &&
                    !string.IsNullOrWhiteSpace(PlayniteApi.Paths.ApplicationPath))
                {
                    roots.Add(Path.Combine(PlayniteApi.Paths.ApplicationPath, "Extensions"));
                }
            }
            catch
            {
            }

            roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Playnite", "Extensions"));
            foreach (var root in roots)
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                {
                    continue;
                }

                foreach (var directory in Directory.GetDirectories(root, "GamepadTester*"))
                {
                    if (Directory.Exists(directory))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            try
            {
                return new ControllerSessionManagerSettingsView(this, openingStandaloneSettings);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to create the settings view.");
                throw;
            }
        }

        private bool OpenStandaloneSettingsView()
        {
            openingStandaloneSettings = true;
            try
            {
                return OpenSettingsView();
            }
            finally
            {
                openingStandaloneSettings = false;
            }
        }

        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args == null)
            {
                return null;
            }

            if (args.Name == "ControllerStatus" || args.Name == "ControllerCount" ||
                args.Name == "PrimaryController" || args.Name == "ControllerIcon" ||
                args.Name == "TopPanelIcon" || args.Name == "ControllerBatteryText" ||
                args.Name == "ControllerBatteryDot")
            {
                return new ControllerThemeControl(Theme, args.Name);
            }

            if (testerIntegration != null)
            {
                var testerControl = testerIntegration.GetGameViewControl(args);
                if (testerControl != null)
                {
                    return testerControl;
                }
            }

            return null;
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            if (controllerTopPanelItem == null)
            {
                controllerTopPanelItem = new TopPanelItem
                {
                    Icon = new ControllerTopPanelControl(this),
                    Activated = delegate { OpenStandaloneSettingsView(); }
                };
            }

            RefreshTopPanelItem();
            yield return controllerTopPanelItem;
            if (testerIntegration != null)
            {
                foreach (var item in testerIntegration.GetTopPanelItems())
                {
                    yield return item;
                }
            }
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            if (testerIntegration == null)
            {
                yield break;
            }

            foreach (var item in testerIntegration.GetSidebarItems())
            {
                yield return item;
            }
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                MenuSection = "Controller Manager",
                Description = Loc("LOCCSM_OpenTester"),
                Action = delegate { OpenTesterSettings(); }
            };
            yield return new MainMenuItem
            {
                MenuSection = "Controller Manager",
                Description = Loc("LOCCSM_MenuPreviewNotification"),
                Action = delegate { ShowNotificationPreview("connected", true); }
            };
            yield return new MainMenuItem
            {
                MenuSection = "Controller Manager",
                Description = Loc("LOCCSM_MenuPreviewOverlay"),
                Action = delegate { ShowOverlayPreview(); }
            };
            yield return new MainMenuItem
            {
                MenuSection = "Controller Manager",
                Description = Loc("LOCCSM_MenuDiagnostics"),
                Action = delegate { ShowDiagnostics(); }
            };
            yield return new MainMenuItem
            {
                MenuSection = "Controller Manager",
                Description = Loc("LOCCSM_MenuExportSupport"),
                Action = delegate { ExportSupportReport(); }
            };
            yield return new MainMenuItem
            {
                MenuSection = "Controller Manager",
                Description = Loc("LOCCSM_MenuRefresh"),
                Action = delegate { RefreshControllers(); }
            };
            if (PlayniteApi.ApplicationInfo.Mode != ApplicationMode.Fullscreen)
            {
                yield return new MainMenuItem
                {
                    MenuSection = "Controller Manager",
                    Description = "-"
                };
                yield return new MainMenuItem
                {
                    MenuSection = "Controller Manager",
                    Description = Loc("LOCCSM_MenuSettings"),
                    Action = delegate { OpenStandaloneSettingsView(); }
                };
            }
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            if (args == null || args.Games == null || args.Games.Count == 0)
            {
                yield break;
            }

            var policies = args.Games.Select(a => settings.GetSessionPolicy(a.Id)).ToList();
            var sessionSection = "Controller Manager|" + Loc("LOCCSM_GamePolicySessionSection");
            var pauseSection = "Controller Manager|" + Loc("LOCCSM_GamePolicyPauseSection");

            yield return CreateGamePolicyMenuItem(sessionSection,
                CheckedLabel(policies.All(a => !a.HasSessionOverride), Loc("LOCCSM_GamePolicyUseGlobal")), args,
                delegate(Guid id, string name) { settings.UseGlobalSessionPolicy(id); });
            yield return CreateGamePolicyMenuItem(sessionSection,
                CheckedLabel(policies.All(a => a.HasSessionOverride && !a.Enabled),
                    Loc("LOCCSM_GamePolicyDisable")), args,
                delegate(Guid id, string name) { settings.SetGameOverride(id, name, false, false); });
            yield return CreateGamePolicyMenuItem(sessionSection,
                CheckedLabel(policies.All(a => a.HasSessionOverride && a.Enabled &&
                    !a.ProtectAllActiveControllers),
                    Loc("LOCCSM_GamePolicyRequireSame")), args,
                delegate(Guid id, string name) { settings.SetGameOverride(id, name, true, false); });
            yield return CreateGamePolicyMenuItem(sessionSection,
                CheckedLabel(policies.All(a => a.HasSessionOverride && a.Enabled &&
                    a.ProtectAllActiveControllers), Loc("LOCCSM_GamePolicyLocalMultiplayer")), args,
                delegate(Guid id, string name) { settings.SetGameOverride(id, name, true, true); });
            yield return CreateGamePolicyMenuItem(pauseSection,
                CheckedLabel(policies.All(a => !a.HasPauseOverride),
                    Loc("LOCCSM_GamePolicyPauseUseGlobal")), args,
                delegate(Guid id, string name) { settings.UseGlobalPausePolicy(id); });
            yield return CreateGamePolicyMenuItem(pauseSection,
                CheckedLabel(policies.All(a => a.HasPauseOverride && !a.PauseGameOnDisconnect &&
                    !a.ForcePauseOfflineGames),
                    Loc("LOCCSM_GamePolicyOverlayOnly")), args,
                delegate(Guid id, string name) { settings.SetGamePauseOverride(id, name, "None"); });
            yield return CreateGamePolicyMenuItem(pauseSection,
                CheckedLabel(policies.All(a => a.HasPauseOverride && a.ForcePauseOfflineGames),
                    Loc("LOCCSM_GamePolicyForcePauseOffline")), args,
                delegate(Guid id, string name) { settings.SetGamePauseOverride(id, name, "OfflineOnly"); });
            yield return CreateGamePolicyMenuItem(pauseSection,
                CheckedLabel(policies.All(a => a.HasPauseOverride && a.PauseGameOnDisconnect &&
                    !a.ForcePauseOfflineGames),
                    Loc("LOCCSM_GamePolicySuspendProcess")), args,
                delegate(Guid id, string name) { settings.SetGamePauseOverride(id, name, "Always"); });
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            diagnosticEvents.Add("lifecycle", "Playnite application started");
            RefreshControllers();
            BeginControllerDatabaseUpdate(false, false);
            creatorThemeUpdateTimer.Start();
            BeginAutomaticCreatorThemeUpdate();
            TryOfferFirstRunSetupWizard();
        }

        private void OnCreatorThemeUpdateTimerTick(object sender, EventArgs args)
        {
            BeginAutomaticCreatorThemeUpdate();
        }

        private async void BeginAutomaticCreatorThemeUpdate()
        {
            if (settings == null || disposed ||
                string.Equals(settings.CreatorThemeUpdatePolicy,
                    ControllerSessionManagerSettings.CreatorThemeUpdatePolicyManual,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.Equals(settings.CreatorThemeUpdatePolicy,
                ControllerSessionManagerSettings.CreatorThemeUpdatePolicyDaily,
                StringComparison.OrdinalIgnoreCase))
            {
                DateTime lastUpdate;
                if (DateTime.TryParse(settings.CreatorThemeLastUpdateUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out lastUpdate) &&
                    DateTime.UtcNow - lastUpdate.ToUniversalTime() < TimeSpan.FromHours(24))
                {
                    return;
                }
            }
            else if (!string.Equals(settings.CreatorThemeUpdatePolicy,
                ControllerSessionManagerSettings.CreatorThemeUpdatePolicyStartup,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            else
            {
                if (automaticCreatorThemeStartupCheckCompleted)
                {
                    return;
                }
                automaticCreatorThemeStartupCheckCompleted = true;
            }

            try
            {
                var result = await UpdateCreatorThemesAsync(CancellationToken.None);
                if (result == null || !result.Succeeded)
                {
                    LogDiagnostic("Automatic creator-theme update failed: " +
                        (result == null ? "No result." : result.Error));
                    return;
                }

                CreatorThemeCatalog.Reload();
                settings.RefreshCreatorThemeState();
                diagnosticEvents.Add("creator-themes", result.CatalogCurrent
                    ? "Automatic design check completed; catalog is current"
                    : string.Format("Automatic design update completed: {0} installed, {1} updated",
                        result.Installed, result.Updated));
            }
            catch (Exception ex)
            {
                LogDiagnostic("Automatic creator-theme update failed: " + ex.Message);
            }
        }

        public void CheckControllerDatabaseUpdates()
        {
            BeginControllerDatabaseUpdate(true, true);
        }

        private void BeginControllerDatabaseUpdate(bool force, bool showResult)
        {
            if (controllerDatabaseUpdater == null || settings == null ||
                (!force && !settings.AutoUpdateControllerDatabase))
            {
                return;
            }

            var dispatcher = Application.Current == null
                ? Dispatcher.CurrentDispatcher : Application.Current.Dispatcher;
            controllerDatabaseUpdater.CheckForUpdateAsync(force).ContinueWith(task =>
            {
                var result = task.Status == TaskStatus.RanToCompletion ? task.Result : null;
                dispatcher.BeginInvoke(new Action(() =>
                {
                    if (result == null || !result.Succeeded)
                    {
                        var error = result == null ? "Update task failed." : result.Error;
                        logger.Warn("Controller mapping database update failed: " + error);
                        diagnosticEvents.Add("controller-db", "Update failed; using last known good database");
                        if (showResult)
                        {
                            PlayniteApi.Dialogs.ShowMessage(Loc("LOCCSM_ControllerDatabaseUpdateFailed"),
                                Loc("LOCCSM_ControllerDatabaseTitle"));
                        }
                        return;
                    }

                    TesterHostClient.ConfigureMappingDatabase(result.ActivePath);
                    diagnosticEvents.Add("controller-db", result.Updated
                        ? "Controller mapping database updated"
                        : "Controller mapping database is current");
                    if (result.Updated)
                    {
                        RefreshControllers();
                    }
                    if (showResult)
                    {
                        PlayniteApi.Dialogs.ShowMessage(
                            Loc(result.Updated ? "LOCCSM_ControllerDatabaseUpdated" :
                                "LOCCSM_ControllerDatabaseCurrent"),
                            Loc("LOCCSM_ControllerDatabaseTitle"));
                    }
                }), DispatcherPriority.Background);
            });
        }

        public void OpenSetupWizard()
        {
            if (PlayniteApi.ApplicationInfo.Mode != ApplicationMode.Desktop)
            {
                PlayniteApi.Dialogs.ShowMessage(
                    Loc("LOCCSM_SetupWizardDesktopOnly"),
                    Loc("LOCCSM_SetupWizardTitle"));
                return;
            }

            if (settings == null)
            {
                return;
            }

            var draft = settings.CloneForWizard();
            var window = new SetupWizardWindow(this, draft);
            var owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            if (owner != null)
            {
                window.Owner = owner;
            }

            SettingsAppearance.ApplyWindow(window, settings.AppearancePreset);
            var result = window.ShowDialog();
            if (result == true)
            {
                ApplyWizardDraft(draft);
                PlayniteApi.Dialogs.ShowMessage(
                    Loc("LOCCSM_SetupWizardSaved"),
                    Loc("LOCCSM_SetupWizardTitle"));
                return;
            }

            // Skip, Escape, or close without finishing: do not keep prompting.
            settings.SetupWizardCompleted = true;
            SavePluginSettings(settings);
            settings.RefreshEditingCloneAfterExternalChange();
        }

        private void TryOfferFirstRunSetupWizard()
        {
            try
            {
                if (settings == null || settings.SetupWizardCompleted)
                {
                    return;
                }

                if (PlayniteApi.ApplicationInfo.Mode != ApplicationMode.Desktop)
                {
                    return;
                }

                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(OpenSetupWizard),
                    DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to offer the first-run setup wizard.");
            }
        }

        private void ApplyWizardDraft(ControllerSessionManagerSettings draft)
        {
            if (draft == null || settings == null)
            {
                return;
            }

            settings.AutoPauseMode = draft.AutoPauseMode;
            settings.ShowDisconnectOverlay = draft.ShowDisconnectOverlay;
            settings.ShowFullscreenControllerNotifications = draft.ShowFullscreenControllerNotifications;
            settings.ShowDesktopControllerNotifications = draft.ShowDesktopControllerNotifications;
            settings.TopPanelControllerMode = draft.TopPanelControllerMode;
            settings.LaunchFullscreenOnGuideButton = draft.LaunchFullscreenOnGuideButton;
            settings.EnableMonitoring = draft.EnableMonitoring;
            settings.EnableSessionTracking = draft.EnableSessionTracking;
            if (settings.Tester != null && draft.Tester != null)
            {
                settings.Tester.ShowSidebarItem = draft.Tester.ShowSidebarItem;
            }

            settings.SetupWizardCompleted = true;
            SavePluginSettings(settings);
            ApplySettings();
            settings.RefreshEditingCloneAfterExternalChange();
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            creatorThemeUpdateTimer.Stop();
            StopMonitoring();
        }

        public override void OnControllerConnected(OnControllerConnectedArgs args)
        {
            try
            {
                if (settings == null || !settings.EnableMonitoring || args == null)
                {
                    return;
                }

                QuiesceSdl(true);
                controllerManager.RecordConnected(args.Controller);
                LogDiagnostic(string.Format("Controller connected: {0}", SafeName(args.Controller)));
                diagnosticEvents.Add("controller", "Connected: " + SafeName(args.Controller));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to handle a controller connection.");
            }
        }

        public override void OnControllerDisconnected(OnControllerDisconnectedArgs args)
        {
            try
            {
                if (settings == null || !settings.EnableMonitoring || args == null)
                {
                    return;
                }

                QuiesceSdl(true);
                controllerManager.RecordDisconnected(args.Controller);
                LogDiagnostic(string.Format("Controller disconnected: {0}", SafeName(args.Controller)));
                diagnosticEvents.Add("controller", "Disconnected: " + SafeName(args.Controller));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to handle a controller disconnection.");
            }
        }

        public override void OnControllerButtonStateChanged(OnControllerButtonStateChangedArgs args)
        {
            RecordControllerInput(args);
            if (testerIntegration != null)
            {
                testerIntegration.HandleControllerInput(args);
            }
        }

        public override void OnDesktopControllerButtonStateChanged(OnControllerButtonStateChangedArgs args)
        {
            HandleGuideFullscreenGesture(args);
            RecordControllerInput(args);
            if (testerIntegration != null)
            {
                testerIntegration.HandleControllerInput(args);
            }
        }

        private void HandleGuideFullscreenGesture(OnControllerButtonStateChangedArgs args)
        {
            if (settings == null || !settings.LaunchFullscreenOnGuideButton || args == null ||
                args.Controller == null ||
                !string.Equals(args.Button.ToString(), "Guide", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (PlayniteApi.ApplicationInfo.Mode != ApplicationMode.Desktop || activeGameId.HasValue)
            {
                guideButtonPressedUtc = null;
                return;
            }

            if (args.State == ControllerInputState.Pressed)
            {
                guideButtonPressedUtc = DateTime.UtcNow;
                return;
            }

            if (args.State != ControllerInputState.Released || !guideButtonPressedUtc.HasValue)
            {
                return;
            }

            var heldFor = DateTime.UtcNow - guideButtonPressedUtc.Value;
            guideButtonPressedUtc = null;

            // Controllers power off with a long Guide hold; only a mid-length hold+release switches mode.
            if (heldFor < GuideFullscreenMinHold || heldFor > GuideFullscreenMaxHold)
            {
                return;
            }

            TryLaunchFullscreenFromGuide();
        }

        private void TryLaunchFullscreenFromGuide()
        {
            try
            {
                if (settings == null || !settings.LaunchFullscreenOnGuideButton ||
                    PlayniteApi.ApplicationInfo.Mode != ApplicationMode.Desktop ||
                    activeGameId.HasValue)
                {
                    return;
                }

                if (Process.GetProcessesByName("Playnite.FullscreenApp").Length > 0)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                if (now - lastFullscreenLaunchUtc < FullscreenLaunchCooldown)
                {
                    return;
                }

                lastFullscreenLaunchUtc = now;
                // Call Playnite's own SwitchAppMode on the UI thread (same as menu/F11).
                // Do not keybd_event(F11) — that hits whichever window is focused.
                // Do not Process.Start(--startfullscreen) — a second DesktopApp process is a
                // known way to leave the Windows taskbar stuck above Fullscreen.
                var wasMinimized = TryRestoreDesktopIfMinimized();
                var dispatcher = PlayniteApi.MainView == null ? GetUiDispatcher() : PlayniteApi.MainView.UIDispatcher;
                if (dispatcher == null)
                {
                    logger.Warn("Guide→Fullscreen: UI dispatcher unavailable.");
                    return;
                }

                Action sendSwitch = () =>
                {
                    try
                    {
                        // DesktopApp shuts down during SwitchAppMode, so activation must run in
                        // a detached helper that outlives this process.
                        StartFullscreenFocusHelper();
                        if (TrySwitchToFullscreenViaPlaynite())
                        {
                            diagnosticEvents.Add("desktop", "Guide press invoked Playnite SwitchAppMode(Fullscreen)");
                            return;
                        }

                        logger.Warn("Guide→Fullscreen: Playnite SwitchAppMode was not available.");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Guide→Fullscreen SwitchAppMode path failed.");
                    }
                };

                if (wasMinimized)
                {
                    var settle = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
                    {
                        Interval = FullscreenRestoreSettleDelay
                    };
                    settle.Tick += (s, e) =>
                    {
                        settle.Stop();
                        sendSwitch();
                    };
                    settle.Start();
                }
                else
                {
                    dispatcher.BeginInvoke(DispatcherPriority.Normal, sendSwitch);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to switch Playnite to Fullscreen from Guide.");
            }
        }

        /// <summary>
        /// Invokes DesktopApplication.Current.SwitchAppMode(Fullscreen) — the same path as F11.
        /// </summary>
        private bool TrySwitchToFullscreenViaPlaynite()
        {
            Type desktopAppType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    desktopAppType = assembly.GetType("Playnite.DesktopApp.DesktopApplication", false);
                }
                catch
                {
                    desktopAppType = null;
                }

                if (desktopAppType != null)
                {
                    break;
                }
            }

            if (desktopAppType == null)
            {
                return false;
            }

            var currentProp = desktopAppType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
            var current = currentProp == null ? null : currentProp.GetValue(null, null);
            if (current == null)
            {
                return false;
            }

            var switchMethod = desktopAppType.GetMethod(
                "SwitchAppMode",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(ApplicationMode) },
                null);
            if (switchMethod == null)
            {
                return false;
            }

            switchMethod.Invoke(current, new object[] { ApplicationMode.Fullscreen });
            return true;
        }

        private void StartFullscreenFocusHelper()
        {
            try
            {
                var directory = Path.GetDirectoryName(GetType().Assembly.Location);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    return;
                }

                var executable = Path.Combine(directory, "ControllerSessionManager.OverlayHost.exe");
                if (!File.Exists(executable))
                {
                    logger.Warn("Guide→Fullscreen: focus helper not found at " + executable);
                    return;
                }

                var helper = Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "--focus-fullscreen",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                if (helper != null)
                {
                    // Let the helper steal focus after Desktop exits / Fullscreen starts.
                    AllowSetForegroundWindow(helper.Id);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to start Guide→Fullscreen focus helper.");
            }
        }

        /// <returns>True when Desktop was minimized and a restore was requested.</returns>
        private bool TryRestoreDesktopIfMinimized()
        {
            var restored = false;
            try
            {
                var main = Application.Current == null ? null : Application.Current.MainWindow;
                if (main != null && main.WindowState == WindowState.Minimized)
                {
                    var helper = new WindowInteropHelper(main);
                    if (helper.Handle != IntPtr.Zero && IsIconic(helper.Handle))
                    {
                        // SW_RESTORE from minimized restores the previous state (incl. maximized).
                        ShowWindow(helper.Handle, SwRestore);
                        restored = true;
                    }
                    else
                    {
                        main.WindowState = WindowState.Normal;
                        restored = true;
                    }

                    main.Activate();
                }
            }
            catch
            {
                // Fall through to process-handle check.
            }

            foreach (var process in Process.GetProcessesByName("Playnite.DesktopApp"))
            {
                try
                {
                    var handle = process.MainWindowHandle;
                    if (handle == IntPtr.Zero || !IsIconic(handle))
                    {
                        continue;
                    }

                    ShowWindow(handle, SwRestore);
                    restored = true;
                }
                catch
                {
                    // Best-effort.
                }
            }

            return restored;
        }

        private const int SwRestore = 9;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            TesterHostClient.SuspendShared();
            EndDisconnectIncident();
            activeGameId = args == null || args.Game == null ? (Guid?)null : args.Game.Id;
            activeGameProcessId = args == null ? 0 : args.StartedProcessId;
            activeSessionId = Guid.NewGuid();
            activeDisconnectIncidentId = null;
            activePauseReceipt = null;
            activeForcePauseRequested = false;
            activeOnlineNotificationOnly = false;
            activeNetworkSafetyDetected = false;
            pauseAttemptGate.Reset();
            activeGameOnlineMetadata = GetOnlineMetadata(args == null ? null : args.Game);
            adaptiveSessionScopeDetector.Reset(DateTime.UtcNow);
            adaptiveLocalScopeLogged = false;
            activeSessionPolicy = activeGameId.HasValue ? settings.GetSessionPolicy(activeGameId.Value) : null;
            if (activeGameId.HasValue && activeSessionPolicy.Enabled)
            {
                var sessionStartedUtc = DateTime.UtcNow;
                sessionManager.Start(activeGameId.Value, sessionStartedUtc);
                var seededInitialController = sessionManager.SeedInitialController(
                    GetControllerSnapshot(), sessionStartedUtc);
                overlayClient.Prepare(activeSessionId);
                sessionTimer.Start();
                LogDiagnostic(string.Format("session.started game={0} scope={1} takeover={2} pause={3} forcePauseOffline={4}",
                    activeGameId.Value,
                    activeSessionPolicy.ProtectAllActiveControllers ? "all-active" : "most-recent",
                    activeSessionPolicy.AllowControllerTakeover,
                    activeSessionPolicy.PauseGameOnDisconnect,
                    activeSessionPolicy.ForcePauseOfflineGames));
                if (!seededInitialController)
                {
                    LogDiagnostic("session.initialControllerPending reason=no-connected-controller");
                }
                diagnosticEvents.Add("session", string.Format(
                    "Started game={0} scope={1} takeover={2} pause={3} forcePause={4}",
                    SupportReportService.Fingerprint(activeGameId.Value.ToString("N")),
                    activeSessionPolicy.ProtectAllActiveControllers ? "all-active" : "adaptive",
                    activeSessionPolicy.AllowControllerTakeover,
                    activeSessionPolicy.PauseGameOnDisconnect,
                    activeSessionPolicy.ForcePauseOfflineGames));
                PublishControllerSnapshotChanged();
            }
            if (settings.EnableDebugLogging && args != null && args.Game != null)
            {
                logger.Debug(string.Format("Session foundation started for {0} ({1}); PID={2}.",
                    args.Game.Name, args.Game.Id, args.StartedProcessId));
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            diagnosticEvents.Add("session", "Game stopped; active incident and session state cleared");
            if (settings.EnableDebugLogging && activeGameId.HasValue)
            {
                logger.Debug(string.Format("Session foundation stopped for {0}.", activeGameId.Value));
            }

            activeGameId = null;
            EndDisconnectIncident();
            activeGameProcessId = 0;
            activeSessionPolicy = null;
            activeGameOnlineMetadata.Clear();
            sessionTimer.Stop();
            sessionManager.Stop();
            TesterHostClient.ResumeShared();
            PublishControllerSnapshotChanged();
        }

        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            StopMonitoring();
            reconciliationTimer.Tick -= OnReconciliationTimerTick;
            xInputTimer.Tick -= OnXInputTimerTick;
            sessionTimer.Tick -= OnSessionTimerTick;
            sessionManager.EventOccurred -= OnSessionEventOccurred;
            controllerManager.SnapshotChanged -= OnManagerSnapshotChanged;
            xInputProvider.Dispose();
            overlayClient.Dispose();
            if (overlayPreviewHideTimer != null)
            {
                overlayPreviewHideTimer.Stop();
                overlayPreviewHideTimer = null;
            }
            if (notificationAudio != null)
            {
                notificationAudio.Dispose();
            }
            if (testerIntegration != null)
            {
                testerIntegration.Shutdown();
            }
            base.Dispose();
        }

        private void RecordControllerInput(OnControllerButtonStateChangedArgs args)
        {
            try
            {
                if (settings == null || !settings.EnableMonitoring || args == null || args.Controller == null ||
                    args.State != ControllerInputState.Pressed ||
                    string.Equals(args.Button.ToString(), "Guide", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                controllerManager.RecordInput(args.Controller);
                if (settings.EnableDebugLogging && args.State == ControllerInputState.Pressed)
                {
                    logger.Debug(string.Format("Controller input: {0}, {1}.", SafeName(args.Controller), args.Button));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to record controller input.");
            }
        }

        private void OnReconciliationTimerTick(object sender, EventArgs args)
        {
            RefreshControllers();
        }

        private void OnXInputTimerTick(object sender, EventArgs args)
        {
            PollXInput();
        }

        private void OnSessionTimerTick(object sender, EventArgs args)
        {
            var activeBefore = sessionManager.ActiveControllers.Count;
            var suspectedBefore = sessionManager.SuspectedDisconnectCount;
            var confirmedBefore = sessionManager.ConfirmedDisconnectCount;
            var now = DateTime.UtcNow;
            var snapshot = GetControllerSnapshot();
            sessionManager.Update(snapshot, now,
                activeSessionPolicy != null && activeSessionPolicy.AllowControllerTakeover,
                GetEffectiveProtectAllControllers(snapshot, now));
            sessionManager.Tick(now,
                TimeSpan.FromMilliseconds(activeSessionPolicy == null
                    ? settings.DisconnectGracePeriodMilliseconds
                    : activeSessionPolicy.GracePeriodMilliseconds));
            UpdateInputPollingInterval();
            if (sessionManager.IsRunning && activeSessionId != Guid.Empty)
            {
                overlayClient.Heartbeat(activeSessionId);
            }
            if (activeBefore != sessionManager.ActiveControllers.Count ||
                suspectedBefore != sessionManager.SuspectedDisconnectCount ||
                confirmedBefore != sessionManager.ConfirmedDisconnectCount)
            {
                PublishControllerSnapshotChanged();
            }
        }

        private void OnSessionEventOccurred(object sender, SessionEventArgs args)
        {
            LogDiagnostic(string.Format("session.{0} controller={1} name={2} replacement={3} replacementName={4} evidence={5}",
                args.Type.ToString().ToLowerInvariant(), args.ControllerKey, args.ControllerName,
                args.ReplacementControllerKey, args.ReplacementControllerName, args.InputEvidence));
            diagnosticEvents.Add("incident", string.Format("{0}: {1}; replacement={2}; evidence={3}",
                args.Type, args.ControllerName, args.ReplacementControllerName, args.InputEvidence));
            if (args.Type == SessionEventType.DisconnectConfirmed)
            {
                EnsureDisconnectIncident();
                TryPauseForCurrentIncident();
            }
            if (PlayniteApi.MainView != null)
            {
                PlayniteApi.MainView.UIDispatcher.BeginInvoke(new Action(RefreshDisconnectOverlay));
            }
        }

        private void PollXInput()
        {
            if (disposed || settings == null || !settings.EnableMonitoring)
            {
                return;
            }

            try
            {
                PollXInputCore();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Controller provider polling failed and was safely contained.");
            }
        }

        private void QuiesceSdl(bool abandonHandles)
        {
            sdlQuietUntilUtc = DateTime.UtcNow.Add(SdlHotPlugQuietPeriod);
            if (abandonHandles)
            {
                xInputProvider.AbandonSdlHandles();
            }
        }

        private void PollXInputCore()
        {
            // Never call SDL from this plugin. Playnite's "game controller API" setting owns a
            // process-wide SDL loop; a second initiator (InitSubSystem/PumpEvents/JoystickOpen)
            // has been observed to terminate Desktop and Fullscreen on hot-plug with no managed
            // exception. XInput and Playnite connect/disconnect callbacks remain in use.
            var observations = xInputProvider.Poll(false);
            if (xInputProvider.LastPollXInputTopologyChanged)
            {
                QuiesceSdl(true);
            }
            if (settings.SyncControllerProfiles(observations.Where(a =>
                a.IsConnected &&
                ControllerDisplayHold.ShouldSyncProfile(a) &&
                !string.Equals(a.ProviderId, XInputProvider.HidProviderId,
                    StringComparison.OrdinalIgnoreCase))))
            {
                // Persist the friendly Desktop identity and its XInput slot so the separate
                // Fullscreen process can reuse it without initializing SDL.
                SavePluginSettings(settings);
            }
            var signature = string.Join("|", observations.Select(a => string.Format(
                "{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}:{12}", a.ControllerId, a.IsConnected, a.ConnectionType,
                a.BatteryLevel, a.LastInputUtc.HasValue ? a.LastInputUtc.Value.Ticks : 0,
                a.HardwareId, a.DetectedName, a.ProviderId, a.LastInputKind, a.IsInputNeutral,
                a.InputNeutralSinceUtc.HasValue ? a.InputNeutralSinceUtc.Value.Ticks : 0,
                a.BatteryProviderId, a.Path)));
            if (signature == lastXInputSignature)
            {
                // Playnite may not deliver controller callbacks while a launched game owns the
                // foreground. Repeated provider samples confirm only fallback-owned transitions.
                controllerManager.ConfirmProviderLifecycle();
                // A pending toast needs its own short stability clock. Do not wait for the
                // five-second reconciliation timer merely because the hardware signature did
                // not change again after the initial connection transition.
                if (pendingToastControllers.Count > 0)
                {
                    UpdateConnectionNotifications(GetControllerSnapshot());
                }

                // Low-battery recover debounce needs consecutive samples even when the
                // hardware signature is unchanged (level already Medium/Full).
                UpdateLowBatteryNotifications(GetControllerSnapshot());
                return;
            }

            lastXInputSignature = signature;
            controllerManager.ReconcileProvider(XInputProvider.ProviderId,
                observations.Where(a => a.ProviderId == XInputProvider.ProviderId));
            controllerManager.ReconcileProvider(XInputProvider.HidProviderId,
                observations.Where(a => a.ProviderId == XInputProvider.HidProviderId));
            controllerManager.ReconcileProvider(XInputProvider.SdlProviderId,
                observations.Where(a => a.ProviderId == XInputProvider.SdlProviderId));
            // Dongle reconnects often change the HID signature before Playnite fires Connected.
            // Count those samples in the same poll so the overlay can recover without waiting
            // for a later unchanged tick.
            controllerManager.ConfirmProviderLifecycle();
        }

        private void OnManagerSnapshotChanged(object sender, EventArgs args)
        {
            var dispatcher = GetUiDispatcher();
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                try
                {
                    dispatcher.BeginInvoke(new Action(SafeUpdateAndPublishSnapshot));
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to schedule a controller snapshot update.");
                }
                return;
            }

            SafeUpdateAndPublishSnapshot();
        }

        private static Dispatcher GetUiDispatcher()
        {
            return Application.Current == null ? null : Application.Current.Dispatcher;
        }

        private void SafeUpdateAndPublishSnapshot()
        {
            try
            {
                UpdateAndPublishSnapshot();
            }
            catch (Exception ex)
            {
                // A hardware notification must never be able to terminate Playnite's UI thread.
                logger.Error(ex, "Controller snapshot processing failed and was safely contained.");
            }
        }

        private void UpdateAndPublishSnapshot()
        {
            var snapshot = GetControllerSnapshot();
            UpdateConnectionNotifications(snapshot);
            UpdateLowBatteryNotifications(snapshot);
            if (sessionManager.IsRunning)
            {
                sessionManager.Update(snapshot, DateTime.UtcNow,
                    activeSessionPolicy != null && activeSessionPolicy.AllowControllerTakeover,
                    GetEffectiveProtectAllControllers(snapshot, DateTime.UtcNow));
            }
            UpdateInputPollingInterval();
            var display = FilterUnknownConnections(displayHold.Apply(snapshot, DateTime.UtcNow));
            UpdateThemeApi(display);
            var signature = GetDisplaySignature(display);
            if (signature == lastDisplaySignature)
            {
                return;
            }

            lastDisplaySignature = signature;
            PublishControllerSnapshotChanged();
        }

        private void UpdateInputPollingInterval()
        {
            var desired = InputPollingPolicy.GetInterval(sessionManager.IsRunning);
            if (xInputTimer.Interval != desired)
            {
                xInputTimer.Interval = desired;
            }
        }

        private bool GetEffectiveProtectAllControllers(IReadOnlyList<ControllerDeviceSnapshot> snapshot,
            DateTime nowUtc)
        {
            if (activeSessionPolicy == null || activeSessionPolicy.ProtectAllActiveControllers)
            {
                return true;
            }

            var promoted = adaptiveSessionScopeDetector.Observe(snapshot, nowUtc);
            if (promoted && !adaptiveLocalScopeLogged)
            {
                adaptiveLocalScopeLogged = true;
                LogDiagnostic("session.scopePromoted scope=local-multiplayer evidence=alternating-controllers");
            }
            return promoted;
        }

        private void PublishControllerSnapshotChanged()
        {
            var handler = ControllerSnapshotChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void UpdateThemeApi()
        {
            UpdateThemeApi(GetDisplayControllerSnapshot());
        }

        private void UpdateThemeApi(IReadOnlyList<ControllerDeviceSnapshot> display)
        {
            var connected = FilterUnknownConnections(display).ToList();
            var primary = connected
                .OrderByDescending(a => a.LastInputUtc.HasValue)
                .ThenByDescending(a => a.LastInputUtc)
                .ThenBy(a => a.Name)
                .FirstOrDefault();
            var primaryName = primary == null ? Loc("LOCCSM_NoControllers") : primary.Name;
            var status = connected.Count == 0
                ? Loc("LOCCSM_NoControllers")
                : string.Format(Loc("LOCCSM_StatusFormat"), primaryName, connected.Count);
            Theme.Update(connected.Count, primaryName, status);
            Theme.UpdateSettingsMirrors(
                settings == null
                    ? ControllerSessionManagerSettings.TopPanelControllerModeHidden
                    : settings.TopPanelControllerMode,
                settings != null && settings.ColorTopPanelIndicatorByBattery,
                settings != null && settings.IsTopPanelButtonVisible,
                SvgIconGeometryLoader.GetPathData("gamepad-tester.svg"));
            var primaryIcon = SvgIconGeometryLoader.GetPathData(ResolveControllerIconFileName(primary));
            var topPanelIcon = ResolveTopPanelIconGeometry(primary);
            var batteryAvailable = primary != null && primary.BatteryLevel != "Unknown" &&
                primary.BatteryLevel != "Unavailable";
            Theme.UpdatePrimaryPresentation(
                primaryIcon,
                topPanelIcon,
                batteryAvailable ? Loc("LOCCSM_Value" + primary.BatteryLevel) : string.Empty,
                batteryAvailable ? primary.BatteryLevel : string.Empty,
                GetBatteryBrush(primary == null ? null : primary.BatteryLevel),
                batteryAvailable,
                settings != null && settings.ColorTopPanelIndicatorByBattery);
            RefreshTopPanelItem();
        }

        private string ResolveTopPanelIconGeometry(ControllerDeviceSnapshot primary)
        {
            if (settings != null &&
                string.Equals(settings.TopPanelControllerMode,
                    ControllerSessionManagerSettings.TopPanelControllerModeDefault,
                    StringComparison.OrdinalIgnoreCase))
            {
                return SvgIconGeometryLoader.GetPathData("gamepad-tester.svg");
            }

            return SvgIconGeometryLoader.GetPathData(ResolveControllerIconFileName(primary));
        }

        private static string GetDisplaySignature(IReadOnlyList<ControllerDeviceSnapshot> display)
        {
            return string.Join(";", (display ?? Enumerable.Empty<ControllerDeviceSnapshot>())
                .Where(a => a != null && a.IsConnected)
                .OrderBy(a => a.HardwareId ?? a.ControllerId, StringComparer.OrdinalIgnoreCase)
                .Select(a => string.Format("{0}:{1}:{2}",
                    a.HardwareId ?? a.ControllerId, a.ConnectionType, a.BatteryLevel)));
        }

        private string ResolveControllerIconFileName(ControllerDeviceSnapshot controller)
        {
            if (controller == null)
            {
                return ControllerIconCatalog.DefaultFileName;
            }

            var profile = settings == null ? null : settings.GetControllerProfile(
                string.IsNullOrWhiteSpace(controller.HardwareId) ? controller.ControllerId : controller.HardwareId);
            return ControllerIconCatalog.ResolveFileName(controller,
                profile == null ? controller.IconId : profile.IconId);
        }

        private void RefreshTopPanelItem()
        {
            if (controllerTopPanelItem == null)
            {
                return;
            }

            controllerTopPanelItem.Visible = settings != null && settings.IsTopPanelButtonVisible;
            controllerTopPanelItem.Title = Theme.PrimaryControllerTooltip;
        }

        private static readonly Brush BatteryEmptyBrush = CreateFrozenBrush(224, 82, 82);
        private static readonly Brush BatteryLowBrush = CreateFrozenBrush(242, 153, 74);
        private static readonly Brush BatteryMediumBrush = CreateFrozenBrush(242, 201, 76);
        private static readonly Brush BatteryFullBrush = CreateFrozenBrush(79, 194, 126);
        private static readonly Brush BatteryUnknownBrush = CreateFrozenBrush(138, 143, 152);

        private static Brush GetBatteryBrush(string value)
        {
            switch (value)
            {
                case "Empty": return BatteryEmptyBrush;
                case "Low": return BatteryLowBrush;
                case "Medium": return BatteryMediumBrush;
                case "Full": return BatteryFullBrush;
                default: return BatteryUnknownBrush;
            }
        }

        private static Brush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }

            return brush;
        }

        private void ShowDiagnostics()
        {
            RefreshControllers();
            var snapshot = controllerManager.GetSnapshot();
            var text = new StringBuilder();
            text.AppendLine(Loc("LOCCSM_DiagnosticsHeader"));
            text.AppendLine(string.Format(Loc("LOCCSM_DiagnosticsCount"), snapshot.Count(a => a.IsConnected)));
            text.AppendLine();
            foreach (var controller in snapshot)
            {
                text.AppendLine(string.Format("• {0}", controller.Name));
                text.AppendLine(string.Format("  {0}: {1}", Loc("LOCCSM_State"),
                    controller.IsConnected ? Loc("LOCCSM_Connected") : Loc("LOCCSM_Disconnected")));
                text.AppendLine(string.Format("  {0}: {1}", Loc("LOCCSM_Provider"), controller.ProviderId));
                text.AppendLine(string.Format("  Lifecycle: {0}",
                    string.IsNullOrWhiteSpace(controller.LifecycleProviderId)
                        ? controller.ProviderId : controller.LifecycleProviderId));
                text.AppendLine(string.Format("  Instance: {0}", controller.ProviderInstanceId));
                text.AppendLine(string.Format("  {0}: {1}", Loc("LOCCSM_Connection"), controller.ConnectionType));
                text.AppendLine(string.Format("  {0}: {1}", Loc("LOCCSM_Battery"), controller.BatteryLevel));
                if (controller.LastInputUtc.HasValue)
                {
                    text.AppendLine(string.Format("  {0}: {1}", Loc("LOCCSM_LastInput"),
                        controller.LastInputUtc.Value.ToLocalTime()));
                }
            }

            PlayniteApi.Dialogs.ShowMessage(text.ToString(), Loc("LOCCSM_DiagnosticsTitle"));
        }

        private void StopMonitoring()
        {
            reconciliationTimer.Stop();
            xInputTimer.Stop();
            sessionTimer.Stop();
            sessionManager.Stop();
            EndDisconnectIncident();
            activeGameId = null;
            activeGameProcessId = 0;
            activeSessionPolicy = null;
        }

        private void RefreshDisconnectOverlay()
        {
            var missing = sessionManager.ActiveControllers.Where(a => a.MissingSinceUtc.HasValue).ToList();
            if (settings == null || !sessionManager.IsRunning || missing.Count == 0 ||
                activeSessionId == Guid.Empty)
            {
                EndDisconnectIncident();
                return;
            }

            EnsureDisconnectIncident();
            if (activeOnlineNotificationOnly)
            {
                HideOverlayWindow();
                return;
            }
            if (!settings.ShowDisconnectOverlay)
            {
                HideOverlayWindow();
                return;
            }

            var names = string.Join(Environment.NewLine, missing.Select(a => a.Name));
            var instruction = Loc("LOCCSM_OverlayAllowTakeover");
            if (missing.Count > 1)
            {
                instruction = Loc("LOCCSM_OverlayReconnectControllers");
            }
            ushort vendorId;
            ushort productId;
            ControllerBridgeIdentity.TryParseHardwareVidPid(missing[0].ControllerKey, out vendorId, out productId);
            var iconFile = ResolveControllerIconFileName(new ControllerDeviceSnapshot
            {
                HardwareId = missing[0].ControllerKey,
                Name = missing[0].Name,
                VendorId = vendorId,
                ProductId = productId
            });
            var missingMetadata = missing.Count == 1
                ? GetControllerSnapshot().FirstOrDefault(a =>
                    SessionControllerIdentity.RefersTo(missing[0].ControllerKey, a))
                : null;
            var connectionType = missingMetadata == null ? string.Empty : missingMetadata.ConnectionType;
            var batteryLevel = missingMetadata == null ? string.Empty : missingMetadata.BatteryLevel;
            var connectionLabel = string.IsNullOrWhiteSpace(connectionType) ||
                string.Equals(connectionType, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? string.Empty : Loc("LOCCSM_Value" + connectionType);
            var batteryLabel = string.IsNullOrWhiteSpace(batteryLevel) ||
                string.Equals(batteryLevel, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? string.Empty : Loc("LOCCSM_Value" + batteryLevel);
            overlayClient.Show(activeSessionId, activeDisconnectIncidentId.Value, activeGameProcessId,
                missing.Count == 1 ? Loc("LOCCSM_OverlayDisconnectTitle") :
                    string.Format(Loc("LOCCSM_OverlayDisconnectTitlePlural"), missing.Count),
                names, instruction, GetOverlayPauseStatus(), GetOverlayPauseStatusKind(),
                SvgIconGeometryLoader.GetPathData(GetOverlayPauseStatusIcon()),
                SvgIconGeometryLoader.GetPathData(iconFile), activeForcePauseRequested,
                activePauseReceipt == null ? 0 : activePauseReceipt.TargetProcessId,
                Loc("LOCCSM_OverlayForcePauseFailed"), "warning",
                SvgIconGeometryLoader.GetPathData("alert-triangle.svg"), GetOverlayStylePayload(),
                connectionLabel, batteryLabel,
                string.IsNullOrWhiteSpace(connectionLabel) ? string.Empty :
                    ControllerConnectionIcons.GetPathData(connectionType),
                string.IsNullOrWhiteSpace(batteryLabel) ? string.Empty :
                    SvgIconGeometryLoader.GetPathData("battery.svg"), batteryLevel,
                Loc("LOCCSM_Disconnected"), Loc("LOCCSM_OverlayDisconnectTimerFormat"));
        }

        private void EnsureDisconnectIncident()
        {
            if (!activeDisconnectIncidentId.HasValue)
            {
                activeDisconnectIncidentId = Guid.NewGuid();
                activePauseReceipt = null;
                activeForcePauseRequested = false;
                activeOnlineNotificationOnly = false;
                activeNetworkSafetyDetected = false;
                pauseAttemptGate.Reset();
                if (settings != null && settings.ShowDisconnectOverlay)
                {
                    PlayNotificationSound(NotificationSoundKind.Warning);
                }
            }
        }

        private void TryPauseForCurrentIncident()
        {
            if (activeSessionPolicy == null || !pauseAttemptGate.TryBegin())
            {
                return;
            }

            // Both PauseGameOnDisconnect and ForcePauseOfflineGames use NtSuspendProcess.
            // ForcePauseOfflineGames additionally checks for network activity before suspending.
            if (!activeSessionPolicy.PauseGameOnDisconnect && !activeSessionPolicy.ForcePauseOfflineGames)
            {
                return;
            }

            activePauseReceipt = gamePauseService.ResolveForegroundTarget(activeGameProcessId, DateTime.UtcNow);
            if (activePauseReceipt.Status != PauseAttemptStatus.Sent)
            {
                LogDiagnostic(string.Format("session.suspendPause targetUnavailable={0}", activePauseReceipt.Status));
                diagnosticEvents.Add("pause", "Suspend target unavailable: " + activePauseReceipt.Status);
                return;
            }

            if (activeSessionPolicy.ForcePauseOfflineGames)
            {
                var online = onlineSessionDetector.Detect(activeGameOnlineMetadata,
                    gamePauseService.GetProcessTree(activeGameProcessId));
                LogDiagnostic(string.Format("session.onlineDetection evidence={0} detail={1}",
                    online.Evidence, online.Detail ?? string.Empty));
                diagnosticEvents.Add("online", string.Format("likely={0} evidence={1} detail={2}",
                    online.IsOnlineLikely, online.Evidence, online.Detail));
                if (online.IsOnlineLikely)
                {
                    activeNetworkSafetyDetected = true;
                    activeOnlineNotificationOnly = online.IsNotificationOnlySafe;
                    if (activeOnlineNotificationOnly)
                    {
                        overlayClient.ShowToast(activeSessionId,
                            GetToastTargetProcessId(activePauseReceipt.TargetProcessId), "warning",
                            Loc("LOCCSM_OnlineFallbackToastTitle"), Loc("LOCCSM_OnlineFallbackToastMessage"),
                            SvgIconGeometryLoader.GetPathData(ControllerIconCatalog.DefaultFileName),
                            settings.NotificationDurationMilliseconds, GetToastStylePayload(),
                            GetToastBadgeIconGeometry("warning"));
                        PlayNotificationSound(NotificationSoundKind.Warning);
                        diagnosticEvents.Add("pause", "Suspend skipped: strong online session detected");
                    }
                    else
                    {
                        diagnosticEvents.Add("pause", "Suspend skipped: weak network evidence; disconnect overlay retained");
                    }
                    return;
                }
            }

            activeForcePauseRequested = true;
            diagnosticEvents.Add("pause", "Process suspension requested");
        }

        private string GetOverlayPauseStatus()
        {
            if (sessionManager.ActiveControllers.Any(a =>
                a.MissingSinceUtc.HasValue && !a.DisconnectConfirmed))
            {
                return Loc("LOCCSM_SessionGracePeriod");
            }
            var wantsSuspend = activeSessionPolicy != null &&
                (activeSessionPolicy.PauseGameOnDisconnect || activeSessionPolicy.ForcePauseOfflineGames);
            if (!wantsSuspend)
            {
                return Loc("LOCCSM_OverlayPauseDisabled");
            }
            if (activeNetworkSafetyDetected)
            {
                return Loc("LOCCSM_OnlineFallbackToastMessage");
            }
            return activeForcePauseRequested
                ? Loc("LOCCSM_OverlayForcePaused")
                : Loc("LOCCSM_OverlayForcePauseFailed");
        }

        private string GetOverlayPauseStatusKind()
        {
            if (sessionManager.ActiveControllers.Any(a =>
                a.MissingSinceUtc.HasValue && !a.DisconnectConfirmed))
            {
                return "neutral";
            }
            var wantsSuspend = activeSessionPolicy != null &&
                (activeSessionPolicy.PauseGameOnDisconnect || activeSessionPolicy.ForcePauseOfflineGames);
            if (!wantsSuspend)
            {
                return "neutral";
            }
            return activeForcePauseRequested && !activeNetworkSafetyDetected ? "pause" : "warning";
        }

        private string GetOverlayPauseStatusIcon()
        {
            return GetOverlayPauseStatusKind() == "warning" ? "alert-triangle.svg" : "player-pause.svg";
        }

        private void HideOverlayWindow()
        {
            if (activeSessionId != Guid.Empty)
            {
                overlayClient.HideAll(activeSessionId);
            }
        }

        private void EndDisconnectIncident()
        {
            HideOverlayWindow();
            activeDisconnectIncidentId = null;
            activePauseReceipt = null;
            activeForcePauseRequested = false;
            activeOnlineNotificationOnly = false;
            activeNetworkSafetyDetected = false;
            pauseAttemptGate.Reset();
        }

        private void UpdateConnectionNotifications(IReadOnlyList<ControllerDeviceSnapshot> snapshot)
        {
            followUpToastSoundDelayMilliseconds = 0;
            var now = DateTime.UtcNow;
            var current = (snapshot ?? new List<ControllerDeviceSnapshot>())
                .Where(a => a.IsConnected && !ControllerDeviceIdentity.IsUnknownConnection(a))
                .GroupBy(GetToastControllerKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(a => a.Key, a => CreateToastIdentity(a.First()), StringComparer.OrdinalIgnoreCase);
            if (!connectionToastStateInitialized || (DateTime.UtcNow - pluginStartedUtc) < ToastStartupGracePeriod)
            {
                connectedToastControllers.Clear();
                foreach (var item in current)
                {
                    connectedToastControllers[item.Key] = item.Value;
                }
                connectionToastStateInitialized = true;
                pendingToastControllers.Clear();
                return;
            }

            var show = settings != null && settings.ShowFullscreenControllerNotifications &&
                PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
            var showDesktop = settings != null && settings.ShowDesktopControllerNotifications &&
                PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop;
            var keys = new HashSet<string>(connectedToastControllers.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(current.Keys);
            keys.UnionWith(pendingToastControllers.Keys);
            foreach (var key in keys.ToList())
            {
                var isStableConnected = connectedToastControllers.ContainsKey(key);
                var isCurrentlyConnected = current.ContainsKey(key);
                if (isStableConnected == isCurrentlyConnected)
                {
                    pendingToastControllers.Remove(key);
                    if (isCurrentlyConnected)
                    {
                        connectedToastControllers[key] = current[key];
                    }
                    continue;
                }

                ControllerToastCandidate candidate;
                if (!pendingToastControllers.TryGetValue(key, out candidate) ||
                    candidate.IsConnected != isCurrentlyConnected)
                {
                    candidate = new ControllerToastCandidate
                    {
                        IsConnected = isCurrentlyConnected,
                        SinceUtc = now,
                        Identity = isCurrentlyConnected ? current[key] : connectedToastControllers[key]
                    };
                    pendingToastControllers[key] = candidate;
                    continue;
                }

                if (isCurrentlyConnected)
                {
                    candidate.Identity = current[key];
                }
                if (now - candidate.SinceUtc < TimeSpan.FromMilliseconds(300))
                {
                    continue;
                }

                var toastProcessId = GetToastTargetProcessId();
                if (show)
                {
                    overlayClient.ShowToast(notificationSessionId, toastProcessId,
                        isCurrentlyConnected ? "connected" : "disconnected",
                        Loc(isCurrentlyConnected ? "LOCCSM_ControllerConnectedToast" : "LOCCSM_ControllerDisconnectedToast"),
                        GetToastControllerName(candidate.Identity.Name), candidate.Identity.IconGeometry,
                        settings.NotificationDurationMilliseconds, GetToastStylePayload(),
                        candidate.Identity.ConnectionIconGeometry);
                }

                if (showDesktop)
                {
                    overlayClient.ShowToast(notificationSessionId, toastProcessId,
                        isCurrentlyConnected ? "connected" : "disconnected",
                        Loc(isCurrentlyConnected ? "LOCCSM_ControllerConnectedToast" : "LOCCSM_ControllerDisconnectedToast"),
                        settings.ShowControllerNameInDesktopNotifications
                            ? GetToastControllerName(candidate.Identity.Name) : string.Empty,
                        candidate.Identity.IconGeometry,
                        settings.DesktopNotificationDurationMilliseconds, GetDesktopToastStylePayload(),
                        candidate.Identity.ConnectionIconGeometry);
                }

                if (show || showDesktop)
                {
                    PlayNotificationSound(isCurrentlyConnected
                        ? NotificationSoundKind.Connected
                        : NotificationSoundKind.Disconnected, false,
                        showDesktop ? NotificationSoundScope.Desktop : NotificationSoundScope.Fullscreen);
                    if (isCurrentlyConnected)
                    {
                        var duration = 0;
                        if (show) duration = Math.Max(duration, settings.NotificationDurationMilliseconds);
                        if (showDesktop) duration = Math.Max(duration,
                            settings.DesktopNotificationDurationMilliseconds);
                        followUpToastSoundDelayMilliseconds = duration + 1500;
                    }
                }

                if (isCurrentlyConnected)
                {
                    connectedToastControllers[key] = candidate.Identity;
                }
                else
                {
                    connectedToastControllers.Remove(key);
                }
                pendingToastControllers.Remove(key);
            }
        }

        private void UpdateLowBatteryNotifications(IReadOnlyList<ControllerDeviceSnapshot> snapshot)
        {
            var controllers = (snapshot ?? new List<ControllerDeviceSnapshot>())
                .Where(a => a.IsConnected && !ControllerDeviceIdentity.IsUnknownConnection(a))
                .GroupBy(GetToastControllerKey, StringComparer.OrdinalIgnoreCase)
                .Select(a => a.First())
                .ToList();
            var threshold = settings == null
                ? LowBatteryNotificationTracker.ThresholdLow
                : settings.LowBatteryNotificationThreshold;
            var presentKeys = controllers.Select(GetToastControllerKey).ToList();

            if (!lowBatteryToastStateInitialized ||
                (DateTime.UtcNow - pluginStartedUtc) < ToastStartupGracePeriod)
            {
                lowBatteryToastTracker.Clear();
                lowBatteryToastTracker.SeedWithoutNotify(
                    controllers
                        .Where(a => LowBatteryNotificationTracker.IsAtOrBelowThreshold(
                            a.BatteryLevel, threshold))
                        .Select(GetToastControllerKey));
                lowBatteryToastStateInitialized = true;
                return;
            }

            lowBatteryToastTracker.RetainOnly(presentKeys);

            var showFullscreen = settings != null &&
                settings.ShowFullscreenLowBatteryNotifications &&
                PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
            var showDesktop = settings != null &&
                settings.ShowDesktopLowBatteryNotifications &&
                PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop;

            foreach (var controller in controllers)
            {
                var key = GetToastControllerKey(controller);
                if (!connectedToastControllers.ContainsKey(key) ||
                    pendingToastControllers.ContainsKey(key))
                {
                    continue;
                }

                if (!lowBatteryToastTracker.ShouldShow(
                    key, controller.BatteryLevel, threshold, true))
                {
                    continue;
                }

                if (!showFullscreen && !showDesktop)
                {
                    continue;
                }

                var title = Loc("LOCCSM_ControllerLowBatteryToast");
                var levelLabel = Loc("LOCCSM_Value" + controller.BatteryLevel);
                var icon = SvgIconGeometryLoader.GetPathData(
                    ResolveControllerIconFileName(controller));
                var badgeIcon = GetToastBadgeIconGeometry("lowbattery");

                var toastProcessId = GetToastTargetProcessId();
                if (showFullscreen)
                {
                    var name = GetToastControllerName(controller.Name);
                    var message = string.IsNullOrWhiteSpace(name)
                        ? levelLabel
                        : name + " · " + levelLabel;
                    overlayClient.ShowToast(
                        notificationSessionId, toastProcessId, "lowbattery", title, message, icon,
                        settings.NotificationDurationMilliseconds, GetToastStylePayload(),
                        badgeIcon);
                }

                if (showDesktop)
                {
                    var message = settings.ShowControllerNameInDesktopNotifications &&
                        !string.IsNullOrWhiteSpace(controller.Name)
                        ? controller.Name + " · " + levelLabel
                        : levelLabel;
                    overlayClient.ShowToast(
                        notificationSessionId, toastProcessId, "lowbattery", title, message, icon,
                        settings.DesktopNotificationDurationMilliseconds,
                        GetDesktopToastStylePayload(),
                        badgeIcon);
                }

                PlayNotificationSound(NotificationSoundKind.LowBattery, false,
                    showDesktop ? NotificationSoundScope.Desktop : NotificationSoundScope.Fullscreen,
                    followUpToastSoundDelayMilliseconds);
            }
        }

        private ControllerToastIdentity CreateToastIdentity(ControllerDeviceSnapshot controller)
        {
            return new ControllerToastIdentity
            {
                Name = controller.Name,
                IconGeometry = SvgIconGeometryLoader.GetPathData(ResolveControllerIconFileName(controller)),
                ConnectionIconGeometry = ControllerConnectionIcons.GetPathData(controller.ConnectionType)
            };
        }

        private static string GetToastControllerKey(ControllerDeviceSnapshot controller)
        {
            if (string.Equals(controller.ProviderId, XInputProvider.ProviderId,
                StringComparison.OrdinalIgnoreCase) && controller.ProviderInstanceId >= 0)
            {
                return string.Format("xinput:slot:{0}", controller.ProviderInstanceId);
            }
            return string.IsNullOrWhiteSpace(controller.HardwareId)
                ? controller.ControllerId
                : controller.HardwareId;
        }

        private static List<string> GetOnlineMetadata(Playnite.SDK.Models.Game game)
        {
            var result = new List<string>();
            if (game == null)
            {
                return result;
            }
            if (game.Features != null) result.AddRange(game.Features.Select(a => a.Name));
            if (game.Tags != null) result.AddRange(game.Tags.Select(a => a.Name));
            if (game.Categories != null) result.AddRange(game.Categories.Select(a => a.Name));
            return result.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
        }

        private ControllerSessionManagerSettings ResolveAppearanceSettings(ThemeAppearanceSurface surface)
        {
            if (settings == null) return null;
            if (!settings.IsThemeAppearanceEnabled(surface)) return settings;
            ControllerSessionManagerSettings themed;
            if (!ThemeEmbeddedAppearanceCatalog.TryCreateThemedAppearance(PlayniteApi, surface, out themed))
                return settings;
            return themed;
        }

        internal bool HasEmbeddedThemeDesign(ThemeAppearanceSurface surface)
        {
            return ThemeEmbeddedAppearanceCatalog.HasEmbeddedLayout(PlayniteApi, surface);
        }

        private static string CoalesceHex(string live, string fallback)
        {
            return IsUsableHex(live) ? live : fallback;
        }

        private static bool IsUsableHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex) || hex[0] != '#' || hex.Length < 7) return false;
            if (hex.Length >= 9 &&
                string.Equals(hex.Substring(1, 2), "00", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        private void ApplyLiveNotificationColors(ThemeAppearanceSurface surface,
            ref string background, ref string text, ref string secondary,
            ref string connected, ref string warning, ref string lowBattery,
            ref bool useGradient, ref string gradientColor,
            ref bool useBorderGradient, ref string borderStart, ref string borderEnd)
        {
            if (settings == null || !settings.IsThemeAppearanceEnabled(surface)) return;
            var live = ThemeAppearanceBridge.Resolve(PlayniteApi, surface);
            if (live == null || !live.HasAny) return;
            background = CoalesceHex(live.Background, background);
            text = CoalesceHex(live.Text, text);
            secondary = CoalesceHex(live.SecondaryText, secondary);
            connected = CoalesceHex(live.Accent, connected);
            warning = CoalesceHex(live.Warning, warning);
            lowBattery = CoalesceHex(live.Warning, lowBattery);
            if (IsUsableHex(live.Gradient))
            {
                useGradient = true;
                gradientColor = live.Gradient;
            }
            ApplyLiveBorderColors(live, ref useBorderGradient, ref borderStart, ref borderEnd);
        }

        private static void ApplyLiveBorderColors(ThemeAppearanceBridge.ThemeAppearanceColors live,
            ref bool useBorderGradient, ref string borderStart, ref string borderEnd)
        {
            if (live == null || !IsUsableHex(live.Border)) return;
            borderStart = live.Border;
            borderEnd = IsUsableHex(live.BorderEnd) ? live.BorderEnd : live.Border;
            if (!string.Equals(borderStart, borderEnd, StringComparison.OrdinalIgnoreCase))
                useBorderGradient = true;
        }

        private void ApplyLiveTypeface(ThemeAppearanceSurface surface,
            ref string fontFamily, ref string fontWeight,
            ref string titleFamily, ref string titleWeight,
            ref string messageFamily, ref string messageWeight)
        {
            if (settings == null || !settings.IsThemeAppearanceEnabled(surface)) return;
            var live = ThemeAppearanceBridge.Resolve(PlayniteApi, surface);
            if (live == null || !live.HasAny) return;
            fontFamily = CoalesceFamily(live.FontFamily, null, fontFamily);
            fontWeight = CoalesceFamily(live.FontWeight, null, fontWeight);
            titleFamily = CoalesceFamily(live.TitleFontFamily, live.FontFamily, titleFamily);
            titleWeight = CoalesceFamily(live.TitleFontWeight, live.FontWeight, titleWeight);
            messageFamily = CoalesceFamily(live.MessageFontFamily, live.FontFamily, messageFamily);
            messageWeight = CoalesceFamily(live.MessageFontWeight, live.FontWeight, messageWeight);
        }

        private static string CoalesceFamily(string live, string shared, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(live)) return live;
            if (!string.IsNullOrWhiteSpace(shared)) return shared;
            return fallback;
        }

        private string GetToastStylePayload()
        {
            var appearance = ResolveAppearanceSettings(ThemeAppearanceSurface.FullscreenNotification) ?? settings;
            var background = appearance.NotificationBackgroundColor;
            var text = appearance.NotificationTextColor;
            var secondary = appearance.NotificationSecondaryTextColor;
            var connected = appearance.NotificationConnectedColor;
            var warning = appearance.NotificationWarningColor;
            var lowBattery = appearance.NotificationLowBatteryColor;
            var useGradient = appearance.NotificationUseGradient;
            var gradientColor = appearance.NotificationGradientColor;
            var useBorderGradient = appearance.NotificationUseBorderGradient;
            var borderStart = appearance.NotificationBorderGradientStartColor;
            var borderEnd = appearance.NotificationBorderGradientEndColor;
            ApplyLiveNotificationColors(ThemeAppearanceSurface.FullscreenNotification,
                ref background, ref text, ref secondary, ref connected, ref warning, ref lowBattery,
                ref useGradient, ref gradientColor, ref useBorderGradient, ref borderStart, ref borderEnd);
            var fontFamily = appearance.NotificationFontFamily;
            var fontWeight = appearance.NotificationFontWeight;
            var titleFamily = appearance.NotificationTitleFontFamily;
            var titleWeight = appearance.NotificationTitleFontWeight;
            var messageFamily = appearance.NotificationMessageFontFamily;
            var messageWeight = appearance.NotificationMessageFontWeight;
            ApplyLiveTypeface(ThemeAppearanceSurface.FullscreenNotification,
                ref fontFamily, ref fontWeight, ref titleFamily, ref titleWeight,
                ref messageFamily, ref messageWeight);
            return string.Join(";", new[]
            {
                appearance.NotificationWidth.ToString(), appearance.NotificationScalePercent.ToString(),
                appearance.NotificationPosition ?? "TopRight", background,
                text, secondary,
                connected, appearance.NotificationDisconnectedColor,
                warning, appearance.NotificationTitleFontSize.ToString(),
                appearance.NotificationMessageFontSize.ToString(), appearance.NotificationIconSize.ToString(),
                appearance.NotificationPadding.ToString(), appearance.NotificationShowBorder.ToString(),
                appearance.NotificationBorderPosition ?? "Bottom",
                appearance.NotificationBorderThickness.ToString(), appearance.NotificationCornerRadius.ToString(),
                appearance.NotificationIconPosition ?? "Left",
                appearance.NotificationElementSpacing.ToString(),
                lowBattery,
                appearance.NotificationShowConnectionBadge.ToString(),
                appearance.NotificationScreenMargin.ToString(),
                appearance.NotificationShowShadow.ToString(),
                fontFamily, fontWeight,
                appearance.NotificationTextAlignment, appearance.NotificationAccentMode,
                appearance.NotificationAnimation, appearance.NotificationShowTitle.ToString(),
                appearance.NotificationUseBackgroundImage.ToString(), EncodeStyleValue(appearance.NotificationBackgroundImagePath),
                appearance.NotificationBackgroundImageStretch, appearance.NotificationBackgroundImageHorizontalAlignment,
                appearance.NotificationBackgroundImageVerticalAlignment, appearance.NotificationBackgroundImageOpacity.ToString(),
                appearance.NotificationBackgroundImageTintOpacity.ToString(),
                appearance.NotificationIconSpacing.ToString(),
                titleFamily, titleWeight,
                messageFamily, messageWeight,
                appearance.NotificationMessageMaxLines.ToString(), appearance.NotificationBadgePosition,
                useGradient.ToString(), gradientColor,
                appearance.NotificationGradientAngle.ToString(), appearance.NotificationUppercaseTitle.ToString(),
                appearance.NotificationShowIconContainer.ToString(), appearance.NotificationIconContainerColor,
                appearance.NotificationIconContainerBorderColor,
                appearance.NotificationIconContainerBorderThickness.ToString(),
                appearance.NotificationIconContainerCornerRadius.ToString(),
                appearance.NotificationIconContainerPadding.ToString(),
                appearance.NotificationTextOrder, appearance.NotificationUseIndependentBorders.ToString(),
                appearance.NotificationBorderLeftThickness.ToString(), appearance.NotificationBorderTopThickness.ToString(),
                appearance.NotificationBorderRightThickness.ToString(), appearance.NotificationBorderBottomThickness.ToString(),
                appearance.NotificationUseStateBackgroundColors.ToString(),
                appearance.NotificationConnectedBackgroundColor, appearance.NotificationDisconnectedBackgroundColor,
                appearance.NotificationWarningBackgroundColor, appearance.NotificationLowBatteryBackgroundColor,
                useBorderGradient.ToString(), borderStart,
                borderEnd, appearance.NotificationBorderGradientAngle.ToString(),
                appearance.NotificationShowBorderGlow.ToString(), appearance.NotificationBorderGlowColor,
                appearance.NotificationBorderGlowBlur.ToString(), appearance.NotificationBorderGlowOpacity.ToString(),
                appearance.NotificationUseStateBorderColors.ToString(), appearance.NotificationConnectedBorderColor,
                appearance.NotificationDisconnectedBorderColor, appearance.NotificationWarningBorderColor,
                appearance.NotificationLowBatteryBorderColor
            });
        }

        private string GetDesktopToastStylePayload()
        {
            var appearance = ResolveAppearanceSettings(ThemeAppearanceSurface.DesktopNotification) ?? settings;
            var background = appearance.DesktopNotificationBackgroundColor;
            var text = appearance.DesktopNotificationTextColor;
            var secondary = appearance.DesktopNotificationSecondaryTextColor;
            var connected = appearance.DesktopNotificationConnectedColor;
            var warning = appearance.DesktopNotificationWarningColor;
            var lowBattery = appearance.DesktopNotificationLowBatteryColor;
            var useGradient = appearance.DesktopNotificationUseGradient;
            var gradientColor = appearance.DesktopNotificationGradientColor;
            var useBorderGradient = appearance.DesktopNotificationUseBorderGradient;
            var borderStart = appearance.DesktopNotificationBorderGradientStartColor;
            var borderEnd = appearance.DesktopNotificationBorderGradientEndColor;
            ApplyLiveNotificationColors(ThemeAppearanceSurface.DesktopNotification,
                ref background, ref text, ref secondary, ref connected, ref warning, ref lowBattery,
                ref useGradient, ref gradientColor, ref useBorderGradient, ref borderStart, ref borderEnd);
            var fontFamily = appearance.DesktopNotificationFontFamily;
            var fontWeight = appearance.DesktopNotificationFontWeight;
            var titleFamily = appearance.DesktopNotificationTitleFontFamily;
            var titleWeight = appearance.DesktopNotificationTitleFontWeight;
            var messageFamily = appearance.DesktopNotificationMessageFontFamily;
            var messageWeight = appearance.DesktopNotificationMessageFontWeight;
            ApplyLiveTypeface(ThemeAppearanceSurface.DesktopNotification,
                ref fontFamily, ref fontWeight, ref titleFamily, ref titleWeight,
                ref messageFamily, ref messageWeight);
            return string.Join(";", new[]
            {
                appearance.DesktopNotificationWidth.ToString(), appearance.DesktopNotificationScalePercent.ToString(),
                appearance.DesktopNotificationPosition ?? "BottomRight", background,
                text, secondary,
                connected, appearance.DesktopNotificationDisconnectedColor,
                warning, appearance.DesktopNotificationTitleFontSize.ToString(),
                appearance.DesktopNotificationMessageFontSize.ToString(), appearance.DesktopNotificationIconSize.ToString(),
                appearance.DesktopNotificationPadding.ToString(), appearance.DesktopNotificationShowBorder.ToString(),
                appearance.DesktopNotificationBorderPosition ?? "Bottom",
                appearance.DesktopNotificationBorderThickness.ToString(), appearance.DesktopNotificationCornerRadius.ToString(),
                appearance.DesktopNotificationIconPosition ?? "Left",
                appearance.DesktopNotificationElementSpacing.ToString(),
                lowBattery,
                appearance.DesktopNotificationShowConnectionBadge.ToString(),
                appearance.DesktopNotificationScreenMargin.ToString(),
                appearance.DesktopNotificationShowShadow.ToString(),
                fontFamily, fontWeight,
                appearance.DesktopNotificationTextAlignment, appearance.DesktopNotificationAccentMode,
                appearance.DesktopNotificationAnimation, appearance.DesktopNotificationShowTitle.ToString(),
                appearance.DesktopNotificationUseBackgroundImage.ToString(), EncodeStyleValue(appearance.DesktopNotificationBackgroundImagePath),
                appearance.DesktopNotificationBackgroundImageStretch, appearance.DesktopNotificationBackgroundImageHorizontalAlignment,
                appearance.DesktopNotificationBackgroundImageVerticalAlignment, appearance.DesktopNotificationBackgroundImageOpacity.ToString(),
                appearance.DesktopNotificationBackgroundImageTintOpacity.ToString(),
                appearance.DesktopNotificationIconSpacing.ToString(),
                titleFamily, titleWeight,
                messageFamily, messageWeight,
                appearance.DesktopNotificationMessageMaxLines.ToString(), appearance.DesktopNotificationBadgePosition,
                useGradient.ToString(), gradientColor,
                appearance.DesktopNotificationGradientAngle.ToString(), appearance.DesktopNotificationUppercaseTitle.ToString(),
                appearance.DesktopNotificationShowIconContainer.ToString(),
                appearance.DesktopNotificationIconContainerColor,
                appearance.DesktopNotificationIconContainerBorderColor,
                appearance.DesktopNotificationIconContainerBorderThickness.ToString(),
                appearance.DesktopNotificationIconContainerCornerRadius.ToString(),
                appearance.DesktopNotificationIconContainerPadding.ToString(),
                appearance.DesktopNotificationTextOrder, appearance.DesktopNotificationUseIndependentBorders.ToString(),
                appearance.DesktopNotificationBorderLeftThickness.ToString(), appearance.DesktopNotificationBorderTopThickness.ToString(),
                appearance.DesktopNotificationBorderRightThickness.ToString(), appearance.DesktopNotificationBorderBottomThickness.ToString(),
                appearance.DesktopNotificationUseStateBackgroundColors.ToString(),
                appearance.DesktopNotificationConnectedBackgroundColor, appearance.DesktopNotificationDisconnectedBackgroundColor,
                appearance.DesktopNotificationWarningBackgroundColor, appearance.DesktopNotificationLowBatteryBackgroundColor,
                useBorderGradient.ToString(), borderStart,
                borderEnd, appearance.DesktopNotificationBorderGradientAngle.ToString(),
                appearance.DesktopNotificationShowBorderGlow.ToString(), appearance.DesktopNotificationBorderGlowColor,
                appearance.DesktopNotificationBorderGlowBlur.ToString(), appearance.DesktopNotificationBorderGlowOpacity.ToString(),
                appearance.DesktopNotificationUseStateBorderColors.ToString(), appearance.DesktopNotificationConnectedBorderColor,
                appearance.DesktopNotificationDisconnectedBorderColor, appearance.DesktopNotificationWarningBorderColor,
                appearance.DesktopNotificationLowBatteryBorderColor
            });
        }

        private static string EncodeStyleValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        public void SelectNotificationBackgroundImage(ControllerSessionManagerSettings targetSettings, bool desktop)
        {
            if (targetSettings == null)
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = Loc("LOCCSM_NotificationBackgroundImageSelect"),
                Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var info = new FileInfo(dialog.FileName);
                if (!info.Exists || info.Length <= 0 || info.Length > 10 * 1024 * 1024)
                {
                    throw new InvalidDataException(Loc("LOCCSM_NotificationBackgroundImageInvalid"));
                }

                System.Windows.Media.Imaging.BitmapSource image;
                using (var stream = File.OpenRead(info.FullName))
                {
                    var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                        stream,
                        System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                    if (decoder.Frames.Count == 0)
                    {
                        throw new InvalidDataException(Loc("LOCCSM_NotificationBackgroundImageInvalid"));
                    }

                    image = decoder.Frames[0];
                }

                var directory = GetNotificationBackgroundDirectory();
                Directory.CreateDirectory(directory);
                var extension = string.Equals(info.Extension, ".png", StringComparison.OrdinalIgnoreCase)
                    ? ".png" : ".jpg";
                var destination = Path.Combine(directory,
                    (desktop ? "desktop-" : "fullscreen-") + Guid.NewGuid().ToString("N") + extension);
                SaveOptimizedNotificationBackground(image, destination, extension);
                if (desktop)
                {
                    targetSettings.DesktopNotificationBackgroundImagePath = destination;
                    targetSettings.DesktopNotificationUseBackgroundImage = true;
                }
                else
                {
                    targetSettings.NotificationBackgroundImagePath = destination;
                    targetSettings.NotificationUseBackgroundImage = true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to select notification background image.");
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, Loc("LOCCSM_NotificationBackgroundImage"));
            }
        }

        public bool ApplyImportedVisualProfile(ControllerSessionManagerSettings targetSettings,
            string profileId, Action onApplied)
        {
            VisualProfileSnapshot snapshot;
            if (targetSettings == null ||
                !ImportedVisualProfileCatalog.TryGetSnapshot(profileId, out snapshot)) return false;
            snapshot.ApplyTo(targetSettings, GetNotificationBackgroundDirectory(),
                GetNotificationSoundDirectory());
            targetSettings.NotificationStylePreset = profileId;
            targetSettings.DesktopNotificationStylePreset = profileId;
            targetSettings.OverlayStylePreset = profileId;
            targetSettings.RefreshCreatorThemeState();
            if (onApplied != null) onApplied();
            return true;
        }

        public bool DeleteImportedVisualProfile(ControllerSessionManagerSettings targetSettings,
            string profileId)
        {
            if (!ImportedVisualProfileCatalog.Contains(profileId)) return false;
            var name = ImportedVisualProfileCatalog.GetName(profileId);
            if (PlayniteApi.Dialogs.ShowMessage(
                    string.Format(Loc("LOCCSM_DeleteImportedDesignConfirm"), name),
                    Loc("LOCCSM_ImportedDesigns"), MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes) return false;
            if (!ImportedVisualProfileCatalog.Delete(profileId)) return false;
            RestoreDefaultPluginLooks(targetSettings, profileId);
            return true;
        }

        public bool DeleteUserInstalledCreatorTheme(ControllerSessionManagerSettings targetSettings,
            string themeId)
        {
            if (string.IsNullOrWhiteSpace(themeId)) return false;
            if (themeId.StartsWith(NotificationSoundCatalog.CreatorPackPrefix, StringComparison.OrdinalIgnoreCase))
                themeId = themeId.Substring(NotificationSoundCatalog.CreatorPackPrefix.Length);
            if (!CreatorThemeCatalog.IsUserInstalled(themeId)) return false;
            var name = CreatorThemeCatalog.GetName(themeId);
            if (PlayniteApi.Dialogs.ShowMessage(
                    string.Format(Loc("LOCCSM_DeleteCreatorDesignConfirm"), name),
                    Loc("LOCCSM_PresetGroupCreators"), MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes) return false;
            if (!CreatorThemeCatalog.TryRemoveUserInstalled(themeId)) return false;
            RestoreDefaultPluginLooks(targetSettings, themeId);
            return true;
        }

        private static void RestoreDefaultPluginLooks(ControllerSessionManagerSettings targetSettings,
            string removedId)
        {
            if (targetSettings == null || string.IsNullOrWhiteSpace(removedId)) return;
            var fallback = NotificationStylePresets.PluginPresets[0];
            var fullscreen = targetSettings.FullscreenLookIs(removedId);
            var desktop = targetSettings.DesktopLookIs(removedId);
            if (fullscreen && desktop)
            {
                NotificationStylePresets.Apply(targetSettings, fallback);
                targetSettings.DesktopNotificationStylePreset = fallback;
            }
            else if (fullscreen)
                NotificationStylePresets.ApplyFullscreen(targetSettings, fallback);
            else if (desktop)
                NotificationStylePresets.ApplyDesktop(targetSettings, fallback);
            if (targetSettings.OverlayLookIs(removedId))
                OverlayStylePresets.Apply(targetSettings, OverlayStylePresets.PluginPresets[0]);
            var soundPack = NotificationSoundCatalog.CreatorPackPrefix + removedId;
            if (string.Equals(targetSettings.NotificationSoundPack, soundPack,
                StringComparison.OrdinalIgnoreCase))
                targetSettings.NotificationSoundPack = NotificationSoundCatalog.ModernCrystal;
            targetSettings.RefreshCreatorThemeState();
        }

        private static void SaveOptimizedNotificationBackground(
            System.Windows.Media.Imaging.BitmapSource source, string destination, string extension)
        {
            const double maxWidth = 1920.0;
            const double maxHeight = 1080.0;
            var scale = Math.Min(1.0,
                Math.Min(maxWidth / Math.Max(1, source.PixelWidth),
                    maxHeight / Math.Max(1, source.PixelHeight)));
            System.Windows.Media.Imaging.BitmapSource output = source;
            if (scale < 1.0)
            {
                output = new System.Windows.Media.Imaging.TransformedBitmap(
                    source, new ScaleTransform(scale, scale));
            }

            System.Windows.Media.Imaging.BitmapEncoder encoder;
            if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
            {
                encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            }
            else
            {
                encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder { QualityLevel = 88 };
            }

            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(output));
            using (var stream = File.Create(destination))
            {
                encoder.Save(stream);
            }
        }

        public void ClearNotificationBackgroundImage(ControllerSessionManagerSettings targetSettings, bool desktop)
        {
            if (targetSettings == null)
            {
                return;
            }

            if (desktop)
            {
                targetSettings.DesktopNotificationUseBackgroundImage = false;
                targetSettings.DesktopNotificationBackgroundImagePath = string.Empty;
            }
            else
            {
                targetSettings.NotificationUseBackgroundImage = false;
                targetSettings.NotificationBackgroundImagePath = string.Empty;
            }
        }

        public void SelectOverlayBackgroundImage(ControllerSessionManagerSettings targetSettings)
        {
            SelectOverlayBackgroundImage(targetSettings, false);
        }

        public void SelectOverlaySceneBackgroundImage(ControllerSessionManagerSettings targetSettings)
        {
            SelectOverlayBackgroundImage(targetSettings, true);
        }

        private void SelectOverlayBackgroundImage(
            ControllerSessionManagerSettings targetSettings, bool sceneBackground)
        {
            if (targetSettings == null) return;
            var dialog = new OpenFileDialog
            {
                Title = Loc("LOCCSM_OverlayBackgroundImageSelect"),
                Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dialog.ShowDialog() != true) return;
            try
            {
                var info = new FileInfo(dialog.FileName);
                if (!info.Exists || info.Length <= 0 || info.Length > 10 * 1024 * 1024)
                {
                    throw new InvalidDataException(Loc("LOCCSM_NotificationBackgroundImageInvalid"));
                }
                System.Windows.Media.Imaging.BitmapSource image;
                using (var stream = File.OpenRead(info.FullName))
                {
                    var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(stream,
                        System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                    if (decoder.Frames.Count == 0)
                    {
                        throw new InvalidDataException(Loc("LOCCSM_NotificationBackgroundImageInvalid"));
                    }
                    image = decoder.Frames[0];
                }
                var directory = GetNotificationBackgroundDirectory();
                Directory.CreateDirectory(directory);
                var extension = string.Equals(info.Extension, ".png", StringComparison.OrdinalIgnoreCase)
                    ? ".png" : ".jpg";
                var destination = Path.Combine(directory,
                    (sceneBackground ? "overlay-scene-" : "overlay-") +
                    Guid.NewGuid().ToString("N") + extension);
                SaveOptimizedNotificationBackground(image, destination, extension);
                if (sceneBackground)
                {
                    targetSettings.OverlaySceneBackgroundImagePath = destination;
                    targetSettings.OverlaySceneUseBackgroundImage = true;
                }
                else
                {
                    targetSettings.OverlayBackgroundImagePath = destination;
                    targetSettings.OverlayUseBackgroundImage = true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to select overlay background image.");
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, Loc("LOCCSM_OverlayBackgroundImage"));
            }
        }

        public void ClearOverlayBackgroundImage(ControllerSessionManagerSettings targetSettings)
        {
            if (targetSettings == null) return;
            targetSettings.OverlayUseBackgroundImage = false;
            targetSettings.OverlayBackgroundImagePath = string.Empty;
        }

        public void ClearOverlaySceneBackgroundImage(ControllerSessionManagerSettings targetSettings)
        {
            if (targetSettings == null) return;
            targetSettings.OverlaySceneUseBackgroundImage = false;
            targetSettings.OverlaySceneBackgroundImagePath = string.Empty;
        }

        internal string GetNotificationBackgroundDirectory()
        {
            return Path.Combine(GetPluginUserDataPath(), "NotificationBackgrounds");
        }

        internal string GetNotificationSoundDirectory()
        {
            return Path.Combine(GetPluginUserDataPath(), "NotificationSounds");
        }

        public async Task SelectCustomNotificationSoundAsync(
            ControllerSessionManagerSettings targetSettings, string kind,
            CancellationToken cancellationToken,
            Action processingStarted = null)
        {
            if (targetSettings == null)
            {
                return;
            }

            string destination = null;
            var committed = false;
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = Loc("LOCCSM_SelectCustomSound"),
                    Filter = Loc("LOCCSM_CustomSoundFileFilter")
                };
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var source = new FileInfo(dialog.FileName);
                var extension = source.Extension.ToLowerInvariant();
                if (source.Length <= 0 || source.Length > 5 * 1024 * 1024 ||
                    (extension != ".wav" && extension != ".mp3" && extension != ".wma"))
                {
                    throw new InvalidDataException(Loc("LOCCSM_CustomSoundInvalid"));
                }

                var normalizedKind = NormalizeNotificationSoundKind(kind);
                var directory = GetNotificationSoundDirectory();
                destination = Path.Combine(directory, normalizedKind + "-" +
                    Guid.NewGuid().ToString("N") + extension);
                if (processingStarted != null)
                {
                    processingStarted();
                }

                // Close any preview first, then perform disk work away from Playnite's UI thread.
                Interlocked.Increment(ref notificationSoundCleanupGeneration);
                if (notificationAudio != null)
                {
                    notificationAudio.Stop();
                }
                await Task.WhenAll(
                    Task.Run(() => CopyFileWithCancellation(
                        source.FullName, destination, cancellationToken), cancellationToken),
                    Task.Delay(600, cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();
                SetCustomNotificationSoundPath(targetSettings, normalizedKind, destination);
                committed = true;
                QueueCustomNotificationSoundCleanup(targetSettings);
            }
            catch (OperationCanceledException)
            {
                // The staged copy is discarded below; the configured sound remains unchanged.
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to select a custom notification sound.");
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, Loc("LOCCSM_CustomSoundsTitle"));
            }
            finally
            {
                if (!committed && !string.IsNullOrWhiteSpace(destination))
                {
                    TryDeleteFile(destination);
                }
            }
        }

        public async Task ClearCustomNotificationSoundAsync(
            ControllerSessionManagerSettings targetSettings, string kind,
            CancellationToken cancellationToken,
            Action processingStarted = null)
        {
            if (targetSettings == null)
            {
                return;
            }

            var normalizedKind = NormalizeNotificationSoundKind(kind);
            if (string.IsNullOrWhiteSpace(
                GetCustomNotificationSoundPath(targetSettings, normalizedKind)))
            {
                return;
            }
            if (processingStarted != null)
            {
                processingStarted();
            }
            try
            {
                await Task.Delay(600, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                SetCustomNotificationSoundPath(targetSettings, normalizedKind, string.Empty);
                QueueCustomNotificationSoundCleanup(targetSettings);
            }
            catch (OperationCanceledException)
            {
                // Keep the selected sound when the user cancels the removal.
            }
        }

        private static void CopyFileWithCancellation(string sourcePath, string destinationPath,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            var buffer = new byte[64 * 1024];
            using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read,
                FileShare.Read, buffer.Length, FileOptions.SequentialScan))
            using (var destination = new FileStream(destinationPath, FileMode.CreateNew,
                FileAccess.Write, FileShare.None, buffer.Length, FileOptions.SequentialScan))
            {
                int count;
                while ((count = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    destination.Write(buffer, 0, count);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>
        /// Removes imported sounds that are no longer referenced after settings are saved or
        /// cancelled. Cleanup is asynchronous so closing the settings view never blocks Playnite.
        /// </summary>
        internal void QueueCustomNotificationSoundCleanup(
            ControllerSessionManagerSettings targetSettings)
        {
            CleanupCustomNotificationSoundsAsync(targetSettings);
        }

        private Task CleanupCustomNotificationSoundsAsync(
            ControllerSessionManagerSettings targetSettings)
        {
            if (targetSettings == null)
            {
                return Task.FromResult(true);
            }

            var retainedPaths = new[]
            {
                targetSettings.CustomConnectedSoundPath,
                targetSettings.CustomDisconnectedSoundPath,
                targetSettings.CustomLowBatterySoundPath,
                targetSettings.CustomWarningSoundPath
            }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(TryGetFullPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
            var directory = GetNotificationSoundDirectory();
            var cleanupToken = Interlocked.Increment(ref notificationSoundCleanupGeneration);

            if (notificationAudio != null)
            {
                notificationAudio.Stop();
            }
            return Task.Run(async () =>
            {
                // Media Foundation and antivirus scanners can retain a handle briefly. Retry
                // without ever making Playnite's settings window wait for the cleanup.
                for (var attempt = 0; attempt < 4; attempt++)
                {
                    if (CleanupObsoleteCustomSounds(directory, retainedPaths,
                        () => cleanupToken == Volatile.Read(
                            ref notificationSoundCleanupGeneration)))
                    {
                        return;
                    }
                    await Task.Delay(250 * (attempt + 1));
                }
            });
        }

        private static string NormalizeNotificationSoundKind(string kind)
        {
            if (string.Equals(kind, "disconnected", StringComparison.OrdinalIgnoreCase))
                return "disconnected";
            if (string.Equals(kind, "lowbattery", StringComparison.OrdinalIgnoreCase))
                return "lowbattery";
            if (string.Equals(kind, "warning", StringComparison.OrdinalIgnoreCase))
                return "warning";
            return "connected";
        }

        private static void SetCustomNotificationSoundPath(
            ControllerSessionManagerSettings targetSettings, string kind, string path)
        {
            if (kind == "disconnected") targetSettings.CustomDisconnectedSoundPath = path;
            else if (kind == "lowbattery") targetSettings.CustomLowBatterySoundPath = path;
            else if (kind == "warning") targetSettings.CustomWarningSoundPath = path;
            else targetSettings.CustomConnectedSoundPath = path;
        }

        private static string GetCustomNotificationSoundPath(
            ControllerSessionManagerSettings targetSettings, string kind)
        {
            if (kind == "disconnected") return targetSettings.CustomDisconnectedSoundPath;
            if (kind == "lowbattery") return targetSettings.CustomLowBatterySoundPath;
            if (kind == "warning") return targetSettings.CustomWarningSoundPath;
            return targetSettings.CustomConnectedSoundPath;
        }

        private static string TryGetFullPath(string path)
        {
            try { return Path.GetFullPath(path); }
            catch (ArgumentException) { return string.Empty; }
            catch (NotSupportedException) { return string.Empty; }
            catch (PathTooLongException) { return string.Empty; }
        }

        private static bool CleanupObsoleteCustomSounds(string directory,
            IEnumerable<string> retainedPaths, Func<bool> isCurrentCleanup)
        {
            var complete = true;
            try
            {
                if (isCurrentCleanup != null && !isCurrentCleanup())
                {
                    return true;
                }
                if (!Directory.Exists(directory))
                {
                    return true;
                }
                var retained = new HashSet<string>(retainedPaths ?? Enumerable.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase);
                foreach (var path in Directory.GetFiles(directory, "*.*"))
                {
                    if (isCurrentCleanup != null && !isCurrentCleanup())
                    {
                        return true;
                    }
                    if (retained.Contains(Path.GetFullPath(path)))
                    {
                        continue;
                    }
                    var extension = Path.GetExtension(path).ToLowerInvariant();
                    if (extension != ".wav" && extension != ".mp3" && extension != ".wma")
                    {
                        continue;
                    }
                    try { File.Delete(path); }
                    catch (IOException) { complete = false; }
                    catch (UnauthorizedAccessException) { complete = false; }
                }
            }
            catch (IOException) { complete = false; }
            catch (UnauthorizedAccessException) { complete = false; }
            return complete;
        }

        public void ShowDesktopNotificationPreview(string kind, bool playSound = true)
        {
            var isLowBattery = string.Equals(kind, "lowbattery", StringComparison.OrdinalIgnoreCase);
            var previewKind = string.Equals(kind, "disconnected", StringComparison.OrdinalIgnoreCase)
                ? "disconnected"
                : string.Equals(kind, "warning", StringComparison.OrdinalIgnoreCase)
                    ? "warning"
                    : isLowBattery
                        ? "lowbattery"
                        : "connected";
            var isWarning = previewKind == "warning";
            var title = isLowBattery
                ? Loc("LOCCSM_ControllerLowBatteryToast")
                : isWarning
                    ? Loc("LOCCSM_OnlineFallbackToastTitle")
                    : Loc(previewKind == "connected" ? "LOCCSM_ControllerConnectedToast" : "LOCCSM_ControllerDisconnectedToast");
            var message = isLowBattery
                ? (settings.ShowControllerNameInDesktopNotifications
                    ? Loc("LOCCSM_NotificationPreviewMessage") + " · " + Loc("LOCCSM_ValueLow")
                    : Loc("LOCCSM_ValueLow"))
                : isWarning
                    ? Loc("LOCCSM_OnlineFallbackToastMessage")
                    : settings.ShowControllerNameInDesktopNotifications ? Loc("LOCCSM_NotificationPreviewMessage") : string.Empty;
            var iconFile = ControllerIconCatalog.DefaultFileName;
            overlayClient.ShowToastPreview(notificationSessionId, GetToastTargetProcessId(), previewKind, title, message,
                SvgIconGeometryLoader.GetPathData(iconFile),
                settings.DesktopNotificationDurationMilliseconds, GetDesktopToastStylePayload(),
                GetToastBadgeIconGeometry(previewKind, "Bluetooth"),
                GetPreviewTargetWindowHandle());
            if (playSound)
            {
                PlayNotificationSound(SoundKindFromToast(previewKind), preview: true);
            }
        }

        private int GetToastTargetProcessId(int preferredProcessId = 0)
        {
            if (preferredProcessId > 0)
            {
                return preferredProcessId;
            }

            if (activeGameProcessId > 0)
            {
                return activeGameProcessId;
            }

            return Process.GetCurrentProcess().Id;
        }

        private IntPtr GetPreviewTargetWindowHandle()
        {
            try
            {
                var app = Application.Current;
                if (app != null)
                {
                    Window active = null;
                    Window fallback = null;
                    foreach (Window window in app.Windows)
                    {
                        if (window == null || !window.IsVisible)
                        {
                            continue;
                        }

                        if (window.IsActive)
                        {
                            active = window;
                            break;
                        }

                        if (fallback == null &&
                            (app.MainWindow == null || !ReferenceEquals(window, app.MainWindow)))
                        {
                            fallback = window;
                        }
                    }

                    var target = active ?? fallback;
                    if (target != null)
                    {
                        var helper = new WindowInteropHelper(target);
                        var handle = helper.Handle;
                        if (handle == IntPtr.Zero)
                        {
                            handle = helper.EnsureHandle();
                        }

                        if (handle != IntPtr.Zero)
                        {
                            return handle;
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                return GetForegroundWindow();
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static string GetToastBadgeIconGeometry(string kind, string connectionType = null)
        {
            if (string.Equals(kind, "warning", StringComparison.OrdinalIgnoreCase))
            {
                return SvgIconGeometryLoader.GetPathData("alert-triangle.svg");
            }

            if (string.Equals(kind, "lowbattery", StringComparison.OrdinalIgnoreCase))
            {
                return SvgIconGeometryLoader.GetPathData("battery.svg");
            }

            return ControllerConnectionIcons.GetPathData(connectionType);
        }

        private string GetToastControllerName(string name)
        {
            return settings != null && settings.ShowControllerNameInNotifications ? name : string.Empty;
        }

        private string GetOverlayStylePayload()
        {
            var appearance = ResolveAppearanceSettings(ThemeAppearanceSurface.Overlay) ?? settings;
            var card = appearance.OverlayCardColor;
            var accent = appearance.OverlayAccentColor;
            var text = appearance.OverlayTextColor;
            var warning = appearance.OverlayWarningColor;
            var useGradient = appearance.OverlayUseGradient;
            var gradientColor = appearance.OverlayGradientColor;
            var useBorderGradient = appearance.OverlayUseBorderGradient;
            var borderStart = appearance.OverlayBorderGradientStartColor;
            var borderEnd = appearance.OverlayBorderGradientEndColor;
            var fontFamily = appearance.OverlayFontFamily;
            var fontWeight = appearance.OverlayFontWeight;
            var titleFamily = appearance.OverlayTitleFontFamily;
            var titleWeight = appearance.OverlayTitleFontWeight;
            var controllerFamily = appearance.OverlayControllerFontFamily;
            var controllerWeight = appearance.OverlayControllerFontWeight;
            var instructionFamily = appearance.OverlayInstructionFontFamily;
            var instructionWeight = appearance.OverlayInstructionFontWeight;
            var statusFamily = appearance.OverlayStatusFontFamily;
            var statusWeight = appearance.OverlayStatusFontWeight;
            if (appearance.IsThemeAppearanceEnabled(ThemeAppearanceSurface.Overlay))
            {
                var live = ThemeAppearanceBridge.Resolve(PlayniteApi, ThemeAppearanceSurface.Overlay);
                if (live != null && live.HasAny)
                {
                    card = CoalesceHex(live.Background, card);
                    accent = CoalesceHex(live.Accent, accent);
                    text = CoalesceHex(live.Text, text);
                    warning = CoalesceHex(live.Warning, warning);
                    if (IsUsableHex(live.Gradient))
                    {
                        useGradient = true;
                        gradientColor = live.Gradient;
                    }
                    ApplyLiveBorderColors(live, ref useBorderGradient, ref borderStart, ref borderEnd);
                    fontFamily = CoalesceFamily(live.FontFamily, null, fontFamily);
                    fontWeight = CoalesceFamily(live.FontWeight, null, fontWeight);
                    titleFamily = CoalesceFamily(live.TitleFontFamily, live.FontFamily, titleFamily);
                    titleWeight = CoalesceFamily(live.TitleFontWeight, live.FontWeight, titleWeight);
                    controllerFamily = CoalesceFamily(live.MessageFontFamily, live.FontFamily, controllerFamily);
                    controllerWeight = CoalesceFamily(live.MessageFontWeight, live.FontWeight, controllerWeight);
                    instructionFamily = CoalesceFamily(live.MessageFontFamily, live.FontFamily, instructionFamily);
                    instructionWeight = CoalesceFamily(live.MessageFontWeight, live.FontWeight, instructionWeight);
                    statusFamily = CoalesceFamily(live.MessageFontFamily, live.FontFamily, statusFamily);
                    statusWeight = CoalesceFamily(live.MessageFontWeight, live.FontWeight, statusWeight);
                }
            }
            return string.Join(";", new[]
            {
                appearance.OverlayScalePercent.ToString(), appearance.OverlayDimColor,
                card, accent,
                text, warning,
                appearance.OverlayTitleFontSize.ToString(), appearance.OverlayControllerFontSize.ToString(),
                appearance.OverlayInstructionFontSize.ToString(), appearance.OverlayStatusFontSize.ToString(),
                appearance.OverlayControllerIconSize.ToString(), appearance.OverlayStatusIconSize.ToString(),
                appearance.OverlayPadding.ToString(), appearance.OverlayShowBorder.ToString(),
                appearance.OverlayBorderThickness.ToString(), appearance.OverlayCornerRadius.ToString(),
                appearance.OverlayShowControllerIcon.ToString(), appearance.OverlayShowStatusIcon.ToString(),
                appearance.OverlayElementSpacing.ToString(),
                appearance.OverlayShowControllerName
                    ? (appearance.OverlayControllerIconPosition ?? "Left")
                    : "Center",
                appearance.OverlayShowControllerName.ToString(),
                fontFamily, fontWeight,
                appearance.OverlayShowConnectionBadge.ToString(),
                appearance.OverlayShowBatteryBadge.ToString(),
                appearance.OverlayShowTitle.ToString(), appearance.OverlayShowInstruction.ToString(),
                appearance.OverlayShowPauseStatus.ToString(), appearance.OverlayCardPosition,
                appearance.OverlayAnimation, appearance.OverlayBorderPosition,
                appearance.OverlayCardWidth.ToString(), appearance.OverlayShowShadow.ToString(),
                titleFamily, titleWeight,
                controllerFamily, controllerWeight,
                instructionFamily, instructionWeight,
                statusFamily, statusWeight,
                appearance.OverlayConnectionBadgeTextColor, appearance.OverlayConnectionBadgeIconColor,
                appearance.OverlayConnectionBadgeBackgroundColor, appearance.OverlayConnectionBadgeBorderColor,
                appearance.OverlayConnectionBadgeBorderThickness.ToString(),
                appearance.OverlayConnectionBadgeCornerRadius.ToString(),
                appearance.OverlayConnectionBadgeIconSize.ToString(), appearance.OverlayConnectionBadgeTextSize.ToString(),
                appearance.OverlayBatteryBadgeTextColor, appearance.OverlayBatteryBadgeIconColor,
                appearance.OverlayBatteryBadgeBackgroundColor, appearance.OverlayBatteryBadgeBorderColor,
                appearance.OverlayBatteryBadgeBorderThickness.ToString(),
                appearance.OverlayBatteryBadgeCornerRadius.ToString(),
                appearance.OverlayBatteryBadgeIconSize.ToString(), appearance.OverlayBatteryBadgeTextSize.ToString(),
                appearance.OverlayBatteryBadgeUseStateColors.ToString(), appearance.OverlayBatteryBadgeFullColor,
                appearance.OverlayBatteryBadgeMediumColor, appearance.OverlayBatteryBadgeLowColor,
                appearance.OverlayBatteryBadgeEmptyColor,
                appearance.OverlayContentAlignment, appearance.OverlayScreenMargin.ToString(),
                useGradient.ToString(), gradientColor,
                appearance.OverlayGradientAngle.ToString(), appearance.OverlayUppercaseTitle.ToString(),
                appearance.OverlayLayoutMode, appearance.OverlayUseBackgroundImage.ToString(),
                EncodeStyleValue(appearance.OverlayBackgroundImagePath),
                appearance.OverlayBackgroundImageStretch,
                appearance.OverlayBackgroundImageHorizontalAlignment,
                appearance.OverlayBackgroundImageVerticalAlignment,
                appearance.OverlayBackgroundImageOpacity.ToString(),
                appearance.OverlayBackgroundImageTintOpacity.ToString(),
                appearance.OverlayShowControllerContainer.ToString(),
                appearance.OverlayControllerContainerColor,
                appearance.OverlayControllerContainerBorderColor,
                appearance.OverlayControllerContainerBorderThickness.ToString(),
                appearance.OverlayControllerContainerCornerRadius.ToString(),
                appearance.OverlayControllerContainerPadding.ToString(),
                appearance.OverlayBlockOrder, appearance.OverlayMetadataOrientation,
                appearance.OverlayUseIndependentBorders.ToString(),
                appearance.OverlayBorderLeftThickness.ToString(), appearance.OverlayBorderTopThickness.ToString(),
                appearance.OverlayBorderRightThickness.ToString(), appearance.OverlayBorderBottomThickness.ToString(),
                useBorderGradient.ToString(), borderStart,
                borderEnd, appearance.OverlayBorderGradientAngle.ToString(),
                appearance.OverlayShowBorderGlow.ToString(), appearance.OverlayBorderGlowColor,
                appearance.OverlayBorderGlowBlur.ToString(), appearance.OverlayBorderGlowOpacity.ToString(),
                appearance.OverlaySceneUseGradient.ToString(), appearance.OverlaySceneGradientColor,
                appearance.OverlaySceneGradientAngle.ToString(),
                appearance.OverlaySceneUseBackgroundImage.ToString(),
                EncodeStyleValue(appearance.OverlaySceneBackgroundImagePath),
                appearance.OverlaySceneBackgroundImageStretch,
                appearance.OverlaySceneBackgroundImageHorizontalAlignment,
                appearance.OverlaySceneBackgroundImageVerticalAlignment,
                appearance.OverlaySceneBackgroundImageOpacity.ToString(),
                appearance.OverlaySceneUseAmbientGlows.ToString(),
                appearance.OverlaySceneGlow1Color, appearance.OverlaySceneGlow1X.ToString(),
                appearance.OverlaySceneGlow1Y.ToString(), appearance.OverlaySceneGlow1Radius.ToString(),
                appearance.OverlaySceneGlow2Color, appearance.OverlaySceneGlow2X.ToString(),
                appearance.OverlaySceneGlow2Y.ToString(), appearance.OverlaySceneGlow2Radius.ToString(),
                appearance.OverlaySceneGlow3Color, appearance.OverlaySceneGlow3X.ToString(),
                appearance.OverlaySceneGlow3Y.ToString(), appearance.OverlaySceneGlow3Radius.ToString(),
                appearance.OverlaySceneShowGrid.ToString(), appearance.OverlaySceneGridColor,
                appearance.OverlaySceneGridSize.ToString(), appearance.OverlaySplitControllerSide,
                appearance.OverlayShowSplitDivider.ToString(), appearance.OverlaySplitDividerColor,
                appearance.OverlaySplitDividerThickness.ToString(),
                appearance.OverlayShowIncidentBadge.ToString(), appearance.OverlayIncidentBadgeTextColor,
                appearance.OverlayIncidentBadgeBackgroundColor, appearance.OverlayIncidentBadgeBorderColor,
                appearance.OverlayIncidentBadgeBorderThickness.ToString(),
                appearance.OverlayIncidentBadgeCornerRadius.ToString(),
                appearance.OverlayIncidentBadgeTextSize.ToString(),
                appearance.OverlayStatusInMetadata.ToString(),
                appearance.OverlayInstructionColor, appearance.OverlayControllerIconColor,
                appearance.OverlayShowDisconnectTimer.ToString()
            });
        }

        private sealed class ControllerToastIdentity
        {
            public string Name { get; set; }
            public string IconGeometry { get; set; }
            public string ConnectionIconGeometry { get; set; }
        }

        private sealed class ControllerToastCandidate
        {
            public bool IsConnected { get; set; }
            public DateTime SinceUtc { get; set; }
            public ControllerToastIdentity Identity { get; set; }
        }

        private static string CheckedLabel(bool isChecked, string description)
        {
            return isChecked ? "✓ " + description : description;
        }

        private GameMenuItem CreateGamePolicyMenuItem(string menuSection, string description,
            GetGameMenuItemsArgs request,
            Action<Guid, string> change)
        {
            return new GameMenuItem
            {
                MenuSection = menuSection,
                Description = description,
                Action = delegate(GameMenuItemActionArgs actionArgs)
                {
                    var games = actionArgs == null || actionArgs.Games == null ? request.Games : actionArgs.Games;
                    foreach (var game in games)
                    {
                        change(game.Id, game.Name);
                    }
                    SavePluginSettings(settings);
                    ApplySettings();
                }
            };
        }

        private void EnsureEnglishFallbackResources()
        {
            try
            {
                englishFallbackResources = LoadEnglishFallbackResources();
                if (englishFallbackResources == null || Application.Current == null || Application.Current.Resources == null)
                {
                    return;
                }

                var loaded = Application.Current.Resources.MergedDictionaries
                    .OfType<ResourceDictionary>()
                    .Any(a => a.Contains("LOCCSM_PluginName") && Equals(a["LOCCSM_PluginName"], "Controller Manager"));
                if (!loaded)
                {
                    Application.Current.Resources.MergedDictionaries.Insert(0, englishFallbackResources);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load English fallback resources.");
            }
        }

        private ResourceDictionary LoadEnglishFallbackResources()
        {
            var directory = Path.GetDirectoryName(GetType().Assembly.Location);
            var path = Path.Combine(directory, "Localization", "en_US.xaml");
            if (!File.Exists(path))
            {
                return null;
            }

            using (var stream = File.OpenRead(path))
            {
                return XamlReader.Load(stream) as ResourceDictionary;
            }
        }

        private string GetEnglishFallbackString(string key)
        {
            if (englishFallbackResources == null)
            {
                englishFallbackResources = LoadEnglishFallbackResources();
            }

            return englishFallbackResources != null && englishFallbackResources.Contains(key)
                ? Convert.ToString(englishFallbackResources[key])
                : null;
        }

        private void LogDiagnostic(string message)
        {
            if (settings != null && settings.EnableDebugLogging)
            {
                logger.Debug(message);
            }
        }

        private static string SafeName(GamepadController controller)
        {
            try
            {
                return controller == null || string.IsNullOrWhiteSpace(controller.Name)
                    ? "Unknown controller"
                    : controller.Name;
            }
            catch (Exception)
            {
                return "Unknown controller";
            }
        }
    }
}
