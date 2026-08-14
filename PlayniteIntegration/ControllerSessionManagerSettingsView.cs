using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ControllerSessionManager.Controllers;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class ControllerSessionManagerSettingsView : UserControl
    {
        private readonly ControllerSessionManagerPlugin plugin;
        private readonly ListBox controllerList;

        public ControllerSessionManagerSettingsView(ControllerSessionManagerPlugin sourcePlugin)
        {
            plugin = sourcePlugin;
            controllerList = new ListBox { MinHeight = 110 };
            Content = BuildContent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private UIElement BuildContent()
        {
            var root = new StackPanel { Margin = new Thickness(16) };
            root.Children.Add(new TextBlock
            {
                Text = plugin.Loc("LOCCSM_SettingsTitle"),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            });
            root.Children.Add(CreateCheckBox("LOCCSM_EnableMonitoring", "EnableMonitoring"));
            root.Children.Add(CreateCheckBox("LOCCSM_EnableDebugLogging", "EnableDebugLogging"));

            var intervalPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 14) };
            intervalPanel.Children.Add(new TextBlock
            {
                Text = plugin.Loc("LOCCSM_ReconciliationInterval"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });
            var intervalBox = new TextBox { Width = 60 };
            intervalBox.SetBinding(TextBox.TextProperty, new Binding("ReconciliationIntervalSeconds")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            intervalPanel.Children.Add(intervalBox);
            root.Children.Add(intervalPanel);

            var header = new DockPanel { Margin = new Thickness(0, 8, 0, 6) };
            var refresh = new Button
            {
                Content = plugin.Loc("LOCCSM_Refresh"),
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            refresh.Click += delegate { plugin.RefreshControllers(); RefreshList(); };
            DockPanel.SetDock(refresh, Dock.Right);
            header.Children.Add(refresh);
            header.Children.Add(new TextBlock
            {
                Text = plugin.Loc("LOCCSM_ConnectedControllers"),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            root.Children.Add(header);
            root.Children.Add(controllerList);
            root.Children.Add(new TextBlock
            {
                Text = plugin.Loc("LOCCSM_FoundationNotice"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.72,
                Margin = new Thickness(0, 12, 0, 0)
            });
            return new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private CheckBox CreateCheckBox(string localizationKey, string bindingPath)
        {
            var checkBox = new CheckBox
            {
                Content = plugin.Loc(localizationKey),
                Margin = new Thickness(0, 4, 0, 4)
            };
            checkBox.SetBinding(CheckBox.IsCheckedProperty, new Binding(bindingPath)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            return checkBox;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            plugin.ControllerSnapshotChanged += OnControllerSnapshotChanged;
            RefreshList();
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            plugin.ControllerSnapshotChanged -= OnControllerSnapshotChanged;
        }

        private void OnControllerSnapshotChanged(object sender, EventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(RefreshList));
        }

        private void RefreshList()
        {
            var items = plugin.GetControllerSnapshot()
                .Where(a => a.IsConnected)
                .Select(FormatController)
                .ToList();
            if (items.Count == 0)
            {
                items.Add(plugin.Loc("LOCCSM_NoControllers"));
            }

            controllerList.ItemsSource = items;
        }

        private string FormatController(ControllerDeviceSnapshot controller)
        {
            var lastInput = controller.LastInputUtc.HasValue
                ? controller.LastInputUtc.Value.ToLocalTime().ToString("T", CultureInfo.CurrentCulture)
                : plugin.Loc("LOCCSM_NoInputYet");
            return string.Format("{0}  ·  {1}: {2}", controller.Name, plugin.Loc("LOCCSM_LastInput"), lastInput);
        }
    }
}

