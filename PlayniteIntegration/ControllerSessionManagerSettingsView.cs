using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ControllerSessionManager.Controllers;

namespace ControllerSessionManager.PlayniteIntegration
{
    public partial class ControllerSessionManagerSettingsView : UserControl
    {
        private readonly ControllerSessionManagerPlugin plugin;

        public ControllerSessionManagerSettingsView(ControllerSessionManagerPlugin sourcePlugin)
        {
            plugin = sourcePlugin;
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            plugin.ControllerSnapshotChanged += OnControllerSnapshotChanged;
            RefreshOverview();
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            plugin.ControllerSnapshotChanged -= OnControllerSnapshotChanged;
        }

        private void OnControllerSnapshotChanged(object sender, EventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(RefreshOverview));
        }

        private void RefreshControllersClick(object sender, RoutedEventArgs args)
        {
            plugin.RefreshControllers();
            RefreshOverview();
        }

        private void RefreshOverview()
        {
            var connected = plugin.GetControllerSnapshot().Where(a => a.IsConnected).ToList();
            var primary = connected
                .OrderByDescending(a => a.LastInputUtc.HasValue)
                .ThenByDescending(a => a.LastInputUtc)
                .FirstOrDefault();

            ConnectedCountText.Text = connected.Count.ToString(CultureInfo.CurrentCulture);
            PrimaryControllerText.Text = primary == null ? plugin.Loc("LOCCSM_NoControllers") : primary.Name;
            XInputStatusText.Text = connected.Any(a => a.ProviderId == XInputProvider.ProviderId)
                ? plugin.Loc("LOCCSM_ProviderActive")
                : plugin.Loc("LOCCSM_ProviderReady");
            LastRefreshText.Text = DateTime.Now.ToString("T", CultureInfo.CurrentCulture);
            ControllerList.ItemsSource = connected.Select(CreateRow).ToList();
            EmptyControllersText.Visibility = connected.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private ControllerRow CreateRow(ControllerDeviceSnapshot controller)
        {
            return new ControllerRow
            {
                Name = controller.Name,
                Provider = controller.ProviderId,
                Connection = LocalizeValue(controller.ConnectionType),
                Battery = LocalizeValue(controller.BatteryLevel),
                LastInput = controller.LastInputUtc.HasValue
                    ? controller.LastInputUtc.Value.ToLocalTime().ToString("T", CultureInfo.CurrentCulture)
                    : plugin.Loc("LOCCSM_NoInputYet")
            };
        }

        private string LocalizeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return plugin.Loc("LOCCSM_Unknown");
            }

            var key = "LOCCSM_Value" + value;
            var localized = plugin.Loc(key);
            return localized == key ? value : localized;
        }

        private sealed class ControllerRow
        {
            public string Name { get; set; }
            public string Provider { get; set; }
            public string Connection { get; set; }
            public string Battery { get; set; }
            public string LastInput { get; set; }
        }
    }
}
