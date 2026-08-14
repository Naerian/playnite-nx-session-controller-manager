using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using ControllerSessionManager.Controllers;
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
        private ResourceDictionary englishFallbackResources;
        private ControllerSessionManagerSettings settings;
        private bool disposed;
        private bool playniteBridgeAvailable = true;
        private bool playniteBridgeWarningLogged;
        private string lastXInputSignature;
        private Guid? activeGameId;

        public override Guid Id
        {
            get { return Guid.Parse("6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc"); }
        }

        public ControllerThemeApi Theme { get; private set; }

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
                    "PrimaryController"
                }
            });
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "ControllerSessionManager",
                SettingsRoot = "Theme"
            });

            settings = new ControllerSessionManagerSettings(this);
            ApplySettings();
            logger.Info("Controller Session Manager 0.1.1 initialized.");
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
            return controllerManager.GetSnapshot();
        }

        public void RefreshControllers()
        {
            if (disposed || settings == null || !settings.EnableMonitoring)
            {
                return;
            }

            PollXInput();

            if (!playniteBridgeAvailable)
            {
                return;
            }

            try
            {
                controllerManager.Reconcile(PlayniteApi.GetConnectedControllers());
            }
            catch (NullReferenceException ex)
            {
                playniteBridgeAvailable = false;
                if (!playniteBridgeWarningLogged)
                {
                    playniteBridgeWarningLogged = true;
                    logger.Warn(ex, "Playnite controller enumeration is unavailable in this application mode. XInput monitoring remains active.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to reconcile Playnite controllers.");
            }
        }

        public void ApplySettings()
        {
            reconciliationTimer.Stop();
            xInputTimer.Stop();
            if (settings == null || !settings.EnableMonitoring || disposed)
            {
                UpdateThemeApi();
                return;
            }

            var seconds = Math.Max(2, Math.Min(60, settings.ReconciliationIntervalSeconds));
            reconciliationTimer.Interval = TimeSpan.FromSeconds(seconds);
            reconciliationTimer.Start();
            xInputTimer.Interval = TimeSpan.FromMilliseconds(250);
            xInputTimer.Start();
            RefreshControllers();
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new ControllerSessionManagerSettingsView(this);
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

            return null;
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                MenuSection = "@Controller Session Manager",
                Description = Loc("LOCCSM_MenuDiagnostics"),
                Action = delegate { ShowDiagnostics(); }
            };
            yield return new MainMenuItem
            {
                MenuSection = "@Controller Session Manager",
                Description = Loc("LOCCSM_MenuRefresh"),
                Action = delegate { RefreshControllers(); }
            };
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            RefreshControllers();
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            StopMonitoring();
        }

        public override void OnControllerConnected(OnControllerConnectedArgs args)
        {
            if (settings.EnableMonitoring && args != null)
            {
                controllerManager.RecordConnected(args.Controller);
                logger.Info(string.Format("Controller connected: {0}", SafeName(args.Controller)));
            }
        }

        public override void OnControllerDisconnected(OnControllerDisconnectedArgs args)
        {
            if (settings.EnableMonitoring && args != null)
            {
                controllerManager.RecordDisconnected(args.Controller);
                logger.Info(string.Format("Controller disconnected: {0}", SafeName(args.Controller)));
            }
        }

        public override void OnControllerButtonStateChanged(OnControllerButtonStateChangedArgs args)
        {
            RecordControllerInput(args);
        }

        public override void OnDesktopControllerButtonStateChanged(OnControllerButtonStateChangedArgs args)
        {
            RecordControllerInput(args);
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            activeGameId = args == null || args.Game == null ? (Guid?)null : args.Game.Id;
            if (settings.EnableDebugLogging && args != null && args.Game != null)
            {
                logger.Debug(string.Format("Session foundation started for {0} ({1}); PID={2}.",
                    args.Game.Name, args.Game.Id, args.StartedProcessId));
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (settings.EnableDebugLogging && activeGameId.HasValue)
            {
                logger.Debug(string.Format("Session foundation stopped for {0}.", activeGameId.Value));
            }

            activeGameId = null;
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
            controllerManager.SnapshotChanged -= OnManagerSnapshotChanged;
            base.Dispose();
        }

        private void RecordControllerInput(OnControllerButtonStateChangedArgs args)
        {
            if (!settings.EnableMonitoring || args == null || args.Controller == null)
            {
                return;
            }

            controllerManager.RecordInput(args.Controller);
            if (settings.EnableDebugLogging && args.State == ControllerInputState.Pressed)
            {
                logger.Debug(string.Format("Controller input: {0}, {1}.", SafeName(args.Controller), args.Button));
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

        private void PollXInput()
        {
            if (disposed || settings == null || !settings.EnableMonitoring)
            {
                return;
            }

            var observations = xInputProvider.Poll();
            var signature = string.Join("|", observations.Select(a => string.Format(
                "{0}:{1}:{2}:{3}:{4}", a.ControllerId, a.IsConnected, a.ConnectionType,
                a.BatteryLevel, a.LastInputUtc.HasValue ? a.LastInputUtc.Value.Ticks : 0)));
            if (signature == lastXInputSignature)
            {
                return;
            }

            lastXInputSignature = signature;
            controllerManager.ReconcileProvider(XInputProvider.ProviderId, observations);
        }

        private void OnManagerSnapshotChanged(object sender, EventArgs args)
        {
            if (PlayniteApi.MainView != null && !PlayniteApi.MainView.UIDispatcher.CheckAccess())
            {
                PlayniteApi.MainView.UIDispatcher.BeginInvoke(new Action(UpdateAndPublishSnapshot));
                return;
            }

            UpdateAndPublishSnapshot();
        }

        private void UpdateAndPublishSnapshot()
        {
            UpdateThemeApi();
            var handler = ControllerSnapshotChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void UpdateThemeApi()
        {
            var connected = controllerManager.GetSnapshot().Where(a => a.IsConnected).ToList();
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
            activeGameId = null;
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
                    .Any(a => a.Contains("LOCCSM_PluginName") && Equals(a["LOCCSM_PluginName"], "Controller Session Manager"));
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
            return controller == null || string.IsNullOrWhiteSpace(controller.Name)
                ? "Unknown controller"
                : controller.Name;
        }
    }
}
