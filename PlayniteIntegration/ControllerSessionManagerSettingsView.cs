using System;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControllerSessionManager.Controllers;
using Forms = System.Windows.Forms;

namespace ControllerSessionManager.PlayniteIntegration
{
    public partial class ControllerSessionManagerSettingsView : UserControl
    {
        private readonly ControllerSessionManagerPlugin plugin;

        public ControllerSessionManagerSettingsView(ControllerSessionManagerPlugin sourcePlugin)
        {
            plugin = sourcePlugin;
            InitializeComponent();
            try
            {
                OverlayPreviewControllerIcon.Data = Geometry.Parse(
                    SvgIconGeometryLoader.GetPathData("device-gamepad-4.svg"));
                OverlayPreviewStatusIcon.Data = Geometry.Parse(
                    SvgIconGeometryLoader.GetPathData("player-pause.svg"));
            }
            catch
            {
                OverlayPreviewControllerIcon.Data = null;
                OverlayPreviewStatusIcon.Data = null;
            }
            AboutVersionText.Text = string.Format(
                plugin.Loc("LOCCSM_VersionAuthorFormat"), GetInstalledVersion());
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            plugin.ControllerSnapshotChanged += OnControllerSnapshotChanged;
            ApplyPreferredWindowSize();
            RefreshOverview();
        }

        private void ApplyPreferredWindowSize()
        {
            var window = Window.GetWindow(this);
            if (window == null)
            {
                return;
            }

            window.SizeToContent = SizeToContent.Manual;
            if (window.MinWidth < 1000)
            {
                window.MinWidth = 1000;
            }
            if (window.MinHeight < 700)
            {
                window.MinHeight = 700;
            }
            if (window.ActualWidth < 1100 && window.Width < 1100)
            {
                window.Width = 1100;
            }
            if (window.ActualHeight < 780 && window.Height < 780)
            {
                window.Height = 780;
            }
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

        private void ExportHidDiagnosticsClick(object sender, RoutedEventArgs args)
        {
            plugin.ExportHidDiagnostics();
        }

        private void ExportSupportReportClick(object sender, RoutedEventArgs args)
        {
            plugin.ExportSupportReport();
        }

        private void PreviewNotificationClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            plugin.ShowNotificationPreview(button == null ? null : button.Tag as string);
        }

        private void SelectColorClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            var settings = DataContext as ControllerSessionManagerSettings;
            var propertyName = button == null ? null : button.Tag as string;
            var property = string.IsNullOrWhiteSpace(propertyName) || settings == null
                ? null : settings.GetType().GetProperty(propertyName);
            if (property == null || property.PropertyType != typeof(string))
            {
                return;
            }

            var currentValue = property.GetValue(settings, null) as string;
            Color currentColor;
            try { currentColor = (Color)ColorConverter.ConvertFromString(currentValue); }
            catch { currentColor = Colors.White; }
            using (var dialog = new Forms.ColorDialog
            {
                FullOpen = true,
                AnyColor = true,
                Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B)
            })
            {
                if (dialog.ShowDialog() != Forms.DialogResult.OK)
                {
                    return;
                }

                var selected = dialog.Color;
                property.SetValue(settings, string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}",
                    currentColor.A, selected.R, selected.G, selected.B), null);
            }
        }

        private void RefreshOverview()
        {
            var connected = plugin.GetControllerSnapshot().Where(a => a.IsConnected).ToList();
            var settings = DataContext as ControllerSessionManagerSettings;
            if (settings != null)
            {
                settings.SyncControllerProfiles(connected);
                connected = plugin.GetControllerSnapshot().Where(a => a.IsConnected).ToList();
            }
            ConnectedCountText.Text = connected.Count.ToString(CultureInfo.CurrentCulture);
            PrimaryControllerText.Text = plugin.GetPrimaryControllerText();
            XInputStatusText.Text = connected.Count > 0
                ? plugin.Loc("LOCCSM_ProviderActive")
                : plugin.Loc("LOCCSM_ProviderReady");
            LastRefreshText.Text = DateTime.Now.ToString("T", CultureInfo.CurrentCulture);
            SessionStatusText.Text = plugin.GetSessionStatusText();
            ActiveSessionControllersText.Text = plugin.GetActiveSessionControllersText();
            ControllerList.ItemsSource = connected.Select(CreateRow).ToList();
            EmptyControllersText.Visibility = connected.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private ControllerRow CreateRow(ControllerDeviceSnapshot controller)
        {
            var currentSettings = DataContext as ControllerSessionManagerSettings;
            var profile = currentSettings == null ? null : currentSettings.GetControllerProfile(
                string.IsNullOrWhiteSpace(controller.HardwareId) ? controller.ControllerId : controller.HardwareId);
            return new ControllerRow
            {
                Name = controller.Name,
                DetectedName = controller.DetectedName ?? controller.Name,
                Profile = profile,
                Provider = controller.ProviderId,
                Connection = LocalizeValue(controller.ConnectionType),
                ConnectionIconGeometry = GetConnectionIconGeometry(controller.ConnectionType),
                ConnectionFallback = string.Equals(controller.ConnectionType, "Unknown", StringComparison.OrdinalIgnoreCase)
                    ? "?" : string.Empty,
                Battery = LocalizeValue(controller.BatteryLevel),
                BatteryBrush = GetBatteryBrush(controller.BatteryLevel),
                IconGeometry = GetControllerIconGeometry(profile),
                Controller = controller,
                ActionIconGeometry = SvgIconGeometryLoader.GetPathData("wave-sine.svg"),
                LastInput = controller.LastInputUtc.HasValue
                    ? controller.LastInputUtc.Value.ToLocalTime().ToString("T", CultureInfo.CurrentCulture)
                    : plugin.Loc("LOCCSM_NoInputYet")
            };
        }

        private static Brush GetBatteryBrush(string value)
        {
            switch (value)
            {
                case "Empty": return new SolidColorBrush(Color.FromRgb(224, 82, 82));
                case "Low": return new SolidColorBrush(Color.FromRgb(242, 153, 74));
                case "Medium": return new SolidColorBrush(Color.FromRgb(242, 201, 76));
                case "Full": return new SolidColorBrush(Color.FromRgb(79, 194, 126));
                default: return new SolidColorBrush(Color.FromRgb(138, 143, 152));
            }
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

        private static string GetConnectionIconGeometry(string connectionType)
        {
            switch (connectionType)
            {
                case "Wired": return SvgIconGeometryLoader.GetPathData("usb.svg");
                case "Bluetooth": return SvgIconGeometryLoader.GetPathData("bluetooth.svg");
                case "Wireless":
                case "WirelessReceiver": return SvgIconGeometryLoader.GetPathData("wifi.svg");
                default: return string.Empty;
            }
        }

        private void VibrateControllerClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            var row = button == null ? null : button.DataContext as ControllerRow;
            if (row == null || row.Controller == null || !plugin.TryVibrateController(row.Controller))
            {
                plugin.ShowVibrationUnavailable();
            }
        }

        private void ControllerIconSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var selector = sender as ComboBox;
            var row = selector == null ? null : selector.DataContext as ControllerRow;
            var option = selector == null ? null : selector.SelectedItem as ControllerIconOption;
            if (row == null || row.Profile == null || option == null)
            {
                return;
            }

            row.Profile.IconId = option.Id;
            row.IconGeometry = option.GeometryData;
        }

        private void PreviewDesktopNotificationClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            var kind = button == null ? "connected" : button.Tag as string ?? "connected";
            plugin.ShowDesktopNotificationPreview(kind);
        }

        private void OpenExternalButton(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            var url = button == null ? null : button.Tag as string;
            if (!string.IsNullOrWhiteSpace(url))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        private static string GetInstalledVersion()
        {
            try
            {
                var assemblyPath = typeof(ControllerSessionManagerSettingsView).Assembly.Location;
                var manifestPath = Path.Combine(Path.GetDirectoryName(assemblyPath), "extension.yaml");
                if (File.Exists(manifestPath))
                {
                    var versionLine = File.ReadLines(manifestPath)
                        .FirstOrDefault(a => a.StartsWith("Version:", StringComparison.OrdinalIgnoreCase));
                    if (versionLine != null)
                    {
                        return versionLine.Substring(versionLine.IndexOf(':') + 1).Trim();
                    }
                }
            }
            catch
            {
            }

            return typeof(ControllerSessionManagerSettingsView).Assembly.GetName().Version.ToString(3);
        }

        private static string GetControllerIconGeometry(ControllerProfile profile)
        {
            var iconId = profile == null ? "gamepad-4" : profile.IconId;
            string fileName;
            switch (iconId)
            {
                case "gamepad-2": fileName = "device-gamepad-2.svg"; break;
                case "gamepad-3": fileName = "device-gamepad-3.svg"; break;
                case "gamepad-4": fileName = "device-gamepad-4.svg"; break;
                case "nintendo": fileName = "device-nintendo.svg"; break;
                default: fileName = "device-gamepad.svg"; break;
            }
            return SvgIconGeometryLoader.GetPathData(fileName);
        }

        private sealed class ControllerRow : System.ComponentModel.INotifyPropertyChanged
        {
            private string iconGeometry;

            public string Name { get; set; }
            public string DetectedName { get; set; }
            public ControllerProfile Profile { get; set; }
            public string Provider { get; set; }
            public string Connection { get; set; }
            public string ConnectionIconGeometry { get; set; }
            public string ConnectionFallback { get; set; }
            public string Battery { get; set; }
            public Brush BatteryBrush { get; set; }
            public string LastInput { get; set; }
            public ControllerDeviceSnapshot Controller { get; set; }
            public string ActionIconGeometry { get; set; }

            public string IconGeometry
            {
                get { return iconGeometry; }
                set
                {
                    iconGeometry = value;
                    var handler = PropertyChanged;
                    if (handler != null)
                    {
                        handler(this, new System.ComponentModel.PropertyChangedEventArgs("IconGeometry"));
                    }
                }
            }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        }
    }
}
