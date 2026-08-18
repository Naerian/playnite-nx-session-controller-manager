using System;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ControllerSessionManager.Controllers;

namespace ControllerSessionManager.PlayniteIntegration
{
    public partial class ControllerSessionManagerSettingsView : UserControl
    {
        private readonly ControllerSessionManagerPlugin plugin;

        public ControllerSessionManagerSettingsView(ControllerSessionManagerPlugin sourcePlugin)
        {
            plugin = sourcePlugin;
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Content = new TextBlock
                {
                    Text = ex.ToString(),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(16)
                };
                return;
            }
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

        private ScrollViewer hostScrollViewer;
        private Window hostWindow;

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            plugin.ControllerSnapshotChanged += OnControllerSnapshotChanged;
            ApplyPreferredWindowSize();
            AttachToHost();
            Dispatcher.BeginInvoke(new Action(AttachToHost), DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(AttachToHost), DispatcherPriority.ApplicationIdle);
            Dispatcher.BeginInvoke(new Action(FillSelectedContentHosts), DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(FillSelectedContentHosts), DispatcherPriority.ApplicationIdle);
            RefreshOverview();
        }

        private void AttachToHost()
        {
            DetachFromHost();
            hostScrollViewer = FindAncestorScrollViewer();
            if (hostScrollViewer != null)
            {
                hostScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                hostScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                hostScrollViewer.SizeChanged += OnHostSizeChanged;
            }

            hostWindow = Window.GetWindow(this);
            if (hostWindow != null)
            {
                hostWindow.SizeChanged += OnHostSizeChanged;
            }

            ApplyViewportSize();
        }

        private void DetachFromHost()
        {
            if (hostScrollViewer != null)
            {
                hostScrollViewer.SizeChanged -= OnHostSizeChanged;
                hostScrollViewer = null;
            }

            if (hostWindow != null)
            {
                hostWindow.SizeChanged -= OnHostSizeChanged;
                hostWindow = null;
            }
        }

        private void OnHostSizeChanged(object sender, SizeChangedEventArgs args)
        {
            ApplyViewportSize();
            FillSelectedContentHosts();
        }

        private void RootTabsSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(FillSelectedContentHosts), DispatcherPriority.Loaded);
        }

        private void FillSelectedContentHosts()
        {
            StretchSelectedContent(this);
        }

        private static void StretchSelectedContent(DependencyObject root)
        {
            if (root == null)
            {
                return;
            }

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var presenter = child as ContentPresenter;
                if (presenter != null && presenter.Name == "PART_SelectedContentHost")
                {
                    presenter.HorizontalAlignment = HorizontalAlignment.Stretch;
                    presenter.VerticalAlignment = VerticalAlignment.Stretch;
                    var content = presenter.Content as FrameworkElement;
                    if (content == null && VisualTreeHelper.GetChildrenCount(presenter) > 0)
                    {
                        content = VisualTreeHelper.GetChild(presenter, 0) as FrameworkElement;
                    }

                    if (content != null)
                    {
                        content.HorizontalAlignment = HorizontalAlignment.Stretch;
                        content.VerticalAlignment = VerticalAlignment.Stretch;
                        content.ClearValue(WidthProperty);
                        content.ClearValue(HeightProperty);
                    }
                }

                StretchSelectedContent(child);
            }
        }

        private void ApplyViewportSize()
        {
            double width = 0;
            double height = 0;
            if (hostScrollViewer != null)
            {
                width = hostScrollViewer.ViewportWidth > 8
                    ? hostScrollViewer.ViewportWidth
                    : hostScrollViewer.ActualWidth;
                height = hostScrollViewer.ViewportHeight > 8
                    ? hostScrollViewer.ViewportHeight
                    : hostScrollViewer.ActualHeight;
            }

            if (width < 8 || height < 8)
            {
                var slot = FindWindowGridSlot();
                if (slot.Width > 8)
                {
                    width = slot.Width;
                }
                if (slot.Height > 8)
                {
                    height = slot.Height;
                }
            }

            if ((width < 8 || height < 8) && hostWindow != null)
            {
                var content = hostWindow.Content as FrameworkElement;
                if (content != null)
                {
                    if (width < 8)
                    {
                        width = content.ActualWidth;
                    }
                    if (height < 8)
                    {
                        height = content.ActualHeight;
                    }
                }
            }

            if (width > 8 && Math.Abs(Width - width) > 1)
            {
                Width = width;
            }

            if (height > 8 && Math.Abs(Height - height) > 1)
            {
                Height = height;
            }

            FillSelectedContentHosts();
        }

        private Size FindWindowGridSlot()
        {
            for (var parent = VisualTreeHelper.GetParent(this);
                 parent != null;
                 parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent is Window)
                {
                    break;
                }

                var grid = parent as Grid;
                if (grid == null || grid.RowDefinitions.Count < 2 || grid.ActualWidth < 400)
                {
                    continue;
                }

                var rowHeight = grid.RowDefinitions[0].ActualHeight;
                if (rowHeight > 200)
                {
                    return new Size(grid.ActualWidth, rowHeight);
                }
            }

            return new Size(0, 0);
        }

        private ScrollViewer FindAncestorScrollViewer()
        {
            for (var parent = VisualTreeHelper.GetParent(this);
                 parent != null;
                 parent = VisualTreeHelper.GetParent(parent))
            {
                var scrollViewer = parent as ScrollViewer;
                if (scrollViewer != null)
                {
                    return scrollViewer;
                }

                if (parent is Window)
                {
                    return null;
                }
            }

            return null;
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
            DetachFromHost();
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
            var dialog = new ColorPickerDialog(currentColor, plugin.Loc);
            var owner = Window.GetWindow(this);
            if (owner != null)
            {
                dialog.Owner = owner;
            }

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var selected = dialog.SelectedColor;
            property.SetValue(settings, ColorPickerMath.ToHex(
                selected.A, selected.R, selected.G, selected.B), null);
        }

        private void RefreshOverview()
        {
            try
            {
                RefreshOverviewCore();
            }
            catch (Exception)
            {
                // A controller list rebind must not close Playnite's settings window.
            }
        }

        private void RefreshOverviewCore()
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
            var connection = LocalizeValue(controller.ConnectionType);
            var battery = LocalizeValue(controller.BatteryLevel);
            var provider = controller.ProviderId;
            return new ControllerRow
            {
                Name = controller.Name,
                DetectedName = controller.DetectedName ?? controller.Name,
                Profile = profile,
                Provider = provider,
                ProviderTooltip = LabeledTooltip("LOCCSM_Provider", provider),
                Connection = connection,
                ConnectionTooltip = LabeledTooltip("LOCCSM_Connection", connection),
                ConnectionIconGeometry = GetConnectionIconGeometry(controller.ConnectionType),
                ConnectionFallback = string.Equals(controller.ConnectionType, "Unknown", StringComparison.OrdinalIgnoreCase)
                    ? "?" : string.Empty,
                Battery = battery,
                BatteryTooltip = LabeledTooltip("LOCCSM_Battery", battery),
                BatteryBrush = GetBatteryBrush(controller.BatteryLevel),
                IconGeometry = GetControllerIconGeometry(profile),
                Controller = controller,
                ActionIconGeometry = SvgIconGeometryLoader.GetPathData("wave-sine.svg"),
                LastInput = controller.LastInputUtc.HasValue
                    ? controller.LastInputUtc.Value.ToLocalTime().ToString("T", CultureInfo.CurrentCulture)
                    : plugin.Loc("LOCCSM_NoInputYet")
            };
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

        private string LabeledTooltip(string labelKey, string value)
        {
            return plugin.Loc(labelKey) + ": " + value;
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
            public string ProviderTooltip { get; set; }
            public string Connection { get; set; }
            public string ConnectionTooltip { get; set; }
            public string ConnectionIconGeometry { get; set; }
            public string ConnectionFallback { get; set; }
            public string Battery { get; set; }
            public string BatteryTooltip { get; set; }
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
