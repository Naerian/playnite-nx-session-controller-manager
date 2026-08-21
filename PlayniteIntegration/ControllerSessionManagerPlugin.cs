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
        private readonly GameSessionManager sessionManager;
        private readonly GamePauseService gamePauseService;
        private readonly OnlineSessionDetector onlineSessionDetector;
        private readonly AdaptiveSessionScopeDetector adaptiveSessionScopeDetector;
        private readonly PauseAttemptGate pauseAttemptGate;
        private readonly OverlayClient overlayClient;
        private readonly DiagnosticEventBuffer diagnosticEvents;
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
        private bool connectionToastStateInitialized;
        private readonly DateTime pluginStartedUtc = DateTime.UtcNow;
        private static readonly TimeSpan ToastStartupGracePeriod = TimeSpan.FromSeconds(8);
        private List<string> activeGameOnlineMetadata = new List<string>();
        private bool adaptiveLocalScopeLogged;
        private SessionProtectionPolicy activeSessionPolicy;
        private TopPanelItem controllerTopPanelItem;
        private TesterIntegration testerIntegration;
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
            sessionManager = new GameSessionManager();
            sessionManager.EventOccurred += OnSessionEventOccurred;
            gamePauseService = new GamePauseService();
            onlineSessionDetector = new OnlineSessionDetector();
            adaptiveSessionScopeDetector = new AdaptiveSessionScopeDetector();
            pauseAttemptGate = new PauseAttemptGate();
            overlayClient = new OverlayClient(logger);
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
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "GamepadTester",
                SettingsRoot = "TesterTheme"
            });

            settings = new ControllerSessionManagerSettings(this);
            testerIntegration = new TesterIntegration(PlayniteApi, logger, settings.Tester, Loc, OpenTesterSettings);
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
            // Charging docks often stay enumerated as Unknown while the pad is off; keep them
            // out of Mandos, TopBar and connect/disconnect toasts.
            return (source ?? Enumerable.Empty<ControllerDeviceSnapshot>())
                .Where(a => a != null && a.IsConnected &&
                    !ControllerDeviceIdentity.IsUnknownConnection(a))
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

        public void ShowNotificationPreview(string kind)
        {
            var previewKind = string.Equals(kind, "disconnected", StringComparison.OrdinalIgnoreCase)
                ? "disconnected"
                : string.Equals(kind, "warning", StringComparison.OrdinalIgnoreCase) ? "warning" : "connected";
            var isWarning = previewKind == "warning";
            var title = isWarning
                ? Loc("LOCCSM_OnlineFallbackToastTitle")
                : Loc(previewKind == "connected" ? "LOCCSM_ControllerConnectedToast" : "LOCCSM_ControllerDisconnectedToast");
            var message = isWarning
                ? Loc("LOCCSM_OnlineFallbackToastMessage")
                : settings.ShowControllerNameInNotifications ? Loc("LOCCSM_NotificationPreviewMessage") : string.Empty;
            overlayClient.ShowToastPreview(notificationSessionId, 0, previewKind, title, message,
                SvgIconGeometryLoader.GetPathData(isWarning ? "alert-triangle.svg" : ControllerIconCatalog.DefaultFileName),
                settings.NotificationDurationMilliseconds, GetToastStylePayload(),
                isWarning ? string.Empty : ControllerConnectionIcons.GetPathData("Wireless"));
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
            PlayniteApi.MainView.OpenPluginSettings(Id);
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
                return new ControllerSessionManagerSettingsView(this);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to create the settings view.");
                throw;
            }
        }

        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args == null)
            {
                return null;
            }

            if (args.Name == "ControllerStatus" || args.Name == "ControllerCount" || args.Name == "PrimaryController")
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
                    Activated = delegate { PlayniteApi.MainView.OpenPluginSettings(Id); }
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
                    Action = delegate { PlayniteApi.MainView.OpenPluginSettings(Id); }
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
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
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
                logger.Info(string.Format("Controller connected: {0}", SafeName(args.Controller)));
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
                logger.Info(string.Format("Controller disconnected: {0}", SafeName(args.Controller)));
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
                logger.Info(string.Format("session.started game={0} scope={1} takeover={2} pause={3} forcePauseOffline={4}",
                    activeGameId.Value,
                    activeSessionPolicy.ProtectAllActiveControllers ? "all-active" : "most-recent",
                    activeSessionPolicy.AllowControllerTakeover,
                    activeSessionPolicy.PauseGameOnDisconnect,
                    activeSessionPolicy.ForcePauseOfflineGames));
                if (!seededInitialController)
                {
                    logger.Info("session.initialControllerPending reason=no-connected-controller");
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
            logger.Info(string.Format("session.{0} controller={1} name={2} replacement={3} replacementName={4} evidence={5}",
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
                logger.Info("session.scopePromoted scope=local-multiplayer evidence=alternating-controllers");
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
            var iconGeometry = ResolveTopPanelIconGeometry(primary);
            var batteryAvailable = primary != null && primary.BatteryLevel != "Unknown" &&
                primary.BatteryLevel != "Unavailable";
            Theme.UpdatePrimaryPresentation(
                iconGeometry,
                batteryAvailable ? Loc("LOCCSM_Value" + primary.BatteryLevel) : string.Empty,
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
            overlayClient.Show(activeSessionId, activeDisconnectIncidentId.Value, activeGameProcessId,
                missing.Count == 1 ? Loc("LOCCSM_OverlayDisconnectTitle") :
                    string.Format(Loc("LOCCSM_OverlayDisconnectTitlePlural"), missing.Count),
                names, instruction, GetOverlayPauseStatus(), GetOverlayPauseStatusKind(),
                SvgIconGeometryLoader.GetPathData(GetOverlayPauseStatusIcon()),
                SvgIconGeometryLoader.GetPathData(iconFile), activeForcePauseRequested,
                activePauseReceipt == null ? 0 : activePauseReceipt.TargetProcessId,
                Loc("LOCCSM_OverlayForcePauseFailed"), "warning",
                SvgIconGeometryLoader.GetPathData("alert-triangle.svg"), GetOverlayStylePayload());
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
                logger.Info(string.Format("session.suspendPause targetUnavailable={0}", activePauseReceipt.Status));
                diagnosticEvents.Add("pause", "Suspend target unavailable: " + activePauseReceipt.Status);
                return;
            }

            if (activeSessionPolicy.ForcePauseOfflineGames)
            {
                var online = onlineSessionDetector.Detect(activeGameOnlineMetadata,
                    gamePauseService.GetProcessTree(activeGameProcessId));
                logger.Info(string.Format("session.onlineDetection evidence={0} detail={1}",
                    online.Evidence, online.Detail ?? string.Empty));
                diagnosticEvents.Add("online", string.Format("likely={0} evidence={1} detail={2}",
                    online.IsOnlineLikely, online.Evidence, online.Detail));
                if (online.IsOnlineLikely)
                {
                    activeNetworkSafetyDetected = true;
                    activeOnlineNotificationOnly = online.IsNotificationOnlySafe;
                    if (activeOnlineNotificationOnly)
                    {
                        overlayClient.ShowToast(activeSessionId, activePauseReceipt.TargetProcessId, "warning",
                            Loc("LOCCSM_OnlineFallbackToastTitle"), Loc("LOCCSM_OnlineFallbackToastMessage"),
                            SvgIconGeometryLoader.GetPathData("alert-triangle.svg"),
                            settings.NotificationDurationMilliseconds, GetToastStylePayload());
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

                if (show)
                {
                    overlayClient.ShowToast(notificationSessionId, 0,
                        isCurrentlyConnected ? "connected" : "disconnected",
                        Loc(isCurrentlyConnected ? "LOCCSM_ControllerConnectedToast" : "LOCCSM_ControllerDisconnectedToast"),
                        GetToastControllerName(candidate.Identity.Name), candidate.Identity.IconGeometry,
                        settings.NotificationDurationMilliseconds, GetToastStylePayload(),
                        candidate.Identity.ConnectionIconGeometry);
                }

                if (showDesktop)
                {
                    overlayClient.ShowToast(notificationSessionId, 0,
                        isCurrentlyConnected ? "connected" : "disconnected",
                        Loc(isCurrentlyConnected ? "LOCCSM_ControllerConnectedToast" : "LOCCSM_ControllerDisconnectedToast"),
                        settings.ShowControllerNameInDesktopNotifications
                            ? GetToastControllerName(candidate.Identity.Name) : string.Empty,
                        candidate.Identity.IconGeometry,
                        settings.DesktopNotificationDurationMilliseconds, GetDesktopToastStylePayload(),
                        candidate.Identity.ConnectionIconGeometry);
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

        private string GetToastStylePayload()
        {
            return string.Join(";", new[]
            {
                settings.NotificationWidth.ToString(), settings.NotificationScalePercent.ToString(),
                settings.NotificationPosition ?? "TopRight", settings.NotificationBackgroundColor,
                settings.NotificationTextColor, settings.NotificationSecondaryTextColor,
                settings.NotificationConnectedColor, settings.NotificationDisconnectedColor,
                settings.NotificationWarningColor, settings.NotificationTitleFontSize.ToString(),
                settings.NotificationMessageFontSize.ToString(), settings.NotificationIconSize.ToString(),
                settings.NotificationPadding.ToString(), settings.NotificationShowBorder.ToString(),
                settings.NotificationBorderPosition ?? "Bottom",
                settings.NotificationBorderThickness.ToString(), settings.NotificationCornerRadius.ToString(),
                settings.NotificationIconPosition ?? "Left",
                settings.NotificationElementSpacing.ToString()
            });
        }

        private string GetDesktopToastStylePayload()
        {
            return string.Join(";", new[]
            {
                settings.DesktopNotificationWidth.ToString(), settings.DesktopNotificationScalePercent.ToString(),
                settings.DesktopNotificationPosition ?? "BottomRight", settings.DesktopNotificationBackgroundColor,
                settings.DesktopNotificationTextColor, settings.DesktopNotificationSecondaryTextColor,
                settings.DesktopNotificationConnectedColor, settings.DesktopNotificationDisconnectedColor,
                settings.DesktopNotificationWarningColor, settings.DesktopNotificationTitleFontSize.ToString(),
                settings.DesktopNotificationMessageFontSize.ToString(), settings.DesktopNotificationIconSize.ToString(),
                settings.DesktopNotificationPadding.ToString(), settings.DesktopNotificationShowBorder.ToString(),
                settings.DesktopNotificationBorderPosition ?? "Bottom",
                settings.DesktopNotificationBorderThickness.ToString(), settings.DesktopNotificationCornerRadius.ToString(),
                settings.DesktopNotificationIconPosition ?? "Left",
                settings.DesktopNotificationElementSpacing.ToString()
            });
        }

        public void ShowDesktopNotificationPreview(string kind)
        {
            var previewKind = string.Equals(kind, "disconnected", StringComparison.OrdinalIgnoreCase)
                ? "disconnected"
                : string.Equals(kind, "warning", StringComparison.OrdinalIgnoreCase) ? "warning" : "connected";
            var isWarning = previewKind == "warning";
            var title = isWarning
                ? Loc("LOCCSM_OnlineFallbackToastTitle")
                : Loc(previewKind == "connected" ? "LOCCSM_ControllerConnectedToast" : "LOCCSM_ControllerDisconnectedToast");
            var message = isWarning
                ? Loc("LOCCSM_OnlineFallbackToastMessage")
                : settings.ShowControllerNameInDesktopNotifications ? Loc("LOCCSM_NotificationPreviewMessage") : string.Empty;
            overlayClient.ShowToastPreview(notificationSessionId, 0, previewKind, title, message,
                SvgIconGeometryLoader.GetPathData(isWarning ? "alert-triangle.svg" : ControllerIconCatalog.DefaultFileName),
                settings.DesktopNotificationDurationMilliseconds, GetDesktopToastStylePayload(),
                isWarning ? string.Empty : ControllerConnectionIcons.GetPathData("Bluetooth"));
        }

        private string GetToastControllerName(string name)
        {
            return settings != null && settings.ShowControllerNameInNotifications ? name : string.Empty;
        }

        private string GetOverlayStylePayload()
        {
            return string.Join(";", new[]
            {
                settings.OverlayScalePercent.ToString(), settings.OverlayDimColor,
                settings.OverlayCardColor, settings.OverlayAccentColor,
                settings.OverlayTextColor, settings.OverlayWarningColor,
                settings.OverlayTitleFontSize.ToString(), settings.OverlayControllerFontSize.ToString(),
                settings.OverlayInstructionFontSize.ToString(), settings.OverlayStatusFontSize.ToString(),
                settings.OverlayControllerIconSize.ToString(), settings.OverlayStatusIconSize.ToString(),
                settings.OverlayPadding.ToString(), settings.OverlayShowBorder.ToString(),
                settings.OverlayBorderThickness.ToString(), settings.OverlayCornerRadius.ToString(),
                settings.OverlayShowControllerIcon.ToString(), settings.OverlayShowStatusIcon.ToString(),
                settings.OverlayElementSpacing.ToString(),
                settings.OverlayShowControllerName
                    ? (settings.OverlayControllerIconPosition ?? "Left")
                    : "Center",
                settings.OverlayShowControllerName.ToString()
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
