using System;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ControllerSessionManager.Controllers;
using ControllerSessionManager.Tester;
using ControllerSessionManager.Tester.ViewModels;

namespace ControllerSessionManager.PlayniteIntegration
{
    public partial class ControllerSessionManagerSettingsView : UserControl
    {
        private readonly ControllerSessionManagerPlugin plugin;
        private readonly bool themeStandaloneWindow;
        private string lastControllerListSignature;

        public ControllerSessionManagerSettingsView(ControllerSessionManagerPlugin sourcePlugin)
            : this(sourcePlugin, false)
        {
        }

        public ControllerSessionManagerSettingsView(ControllerSessionManagerPlugin sourcePlugin,
            bool sourceThemeStandaloneWindow)
        {
            plugin = sourcePlugin;
            themeStandaloneWindow = sourceThemeStandaloneWindow;
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
                    SvgIconGeometryLoader.GetPathData(ControllerIconCatalog.DefaultFileName));
                OverlayPreviewStatusIcon.Data = Geometry.Parse(
                    SvgIconGeometryLoader.GetPathData("player-pause.svg"));
                OverlayPreviewConnectionIcon.Data = Geometry.Parse(
                    SvgIconGeometryLoader.GetPathData("bluetooth.svg"));
                OverlayPreviewBatteryIcon.Data = Geometry.Parse(
                    SvgIconGeometryLoader.GetPathData("battery.svg"));
            }
            catch
            {
                OverlayPreviewControllerIcon.Data = null;
                OverlayPreviewStatusIcon.Data = null;
                OverlayPreviewConnectionIcon.Data = null;
                OverlayPreviewBatteryIcon.Data = null;
            }
            AboutVersionText.Text = plugin == null
                ? GetInstalledVersion()
                : string.Format(plugin.Loc("LOCCSM_VersionAuthorFormat"), GetInstalledVersion());
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnSettingsDataContextChanged;
        }

        private ControllerSessionManagerSettings boundSettings;

        private void OnSettingsDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (boundSettings != null)
            {
                boundSettings.PropertyChanged -= OnBoundSettingsPropertyChanged;
            }

            boundSettings = args.NewValue as ControllerSessionManagerSettings;
            if (boundSettings != null)
            {
                boundSettings.PropertyChanged += OnBoundSettingsPropertyChanged;
            }

            ApplyAppearancePreset();
            BuildAppearancePresetChips();
            BuildNotificationStylePresetChips();
            BuildOverlayStylePresetChips();
            BuildNotificationSoundPackChips();
            RefreshOverlayPreviewControllerLayout();
        }

        private void OnBoundSettingsPropertyChanged(object sender,
            System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args != null && args.PropertyName == "AppearancePreset")
            {
                ApplyAppearancePreset();
            }

            if (args != null && args.PropertyName == "NotificationStylePreset")
            {
                RefreshNotificationStylePresetChips();
            }

            if (args != null && args.PropertyName == "OverlayStylePreset")
            {
                RefreshOverlayStylePresetChips();
            }

            if (!suppressingStylePresetMark &&
                args != null &&
                !string.IsNullOrEmpty(args.PropertyName) &&
                boundSettings != null)
            {
                if (NotificationStylePropertyNames.Contains(args.PropertyName) &&
                    !string.Equals(boundSettings.NotificationStylePreset, NotificationStylePresets.Custom,
                        StringComparison.OrdinalIgnoreCase))
                {
                    boundSettings.NotificationStylePreset = NotificationStylePresets.Custom;
                }

                if (OverlayStylePropertyNames.Contains(args.PropertyName) &&
                    !string.Equals(boundSettings.OverlayStylePreset, OverlayStylePresets.Custom,
                        StringComparison.OrdinalIgnoreCase))
                {
                    boundSettings.OverlayStylePreset = OverlayStylePresets.Custom;
                }
            }

            if (args == null ||
                string.IsNullOrEmpty(args.PropertyName) ||
                args.PropertyName == "OverlayControllerIconPosition" ||
                args.PropertyName == "OverlayElementSpacing" ||
                args.PropertyName == "OverlayShowControllerIcon" ||
                args.PropertyName == "OverlayShowControllerName" ||
                args.PropertyName == "OverlayControllerIconSize")
            {
                RefreshOverlayPreviewControllerLayout();
            }
        }

        private void RefreshOverlayPreviewControllerLayout()
        {
            if (OverlayPreviewControllerHost == null || OverlayPreviewControllerIcon == null ||
                OverlayPreviewControllerName == null)
            {
                return;
            }

            var settings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            var gap = settings == null ? 0 : Math.Max(0, settings.OverlayElementSpacing);
            var showIcon = settings == null || settings.OverlayShowControllerIcon;
            var showName = settings == null || settings.OverlayShowControllerName;
            var position = !showName
                ? "Center"
                : (settings == null || string.IsNullOrWhiteSpace(settings.OverlayControllerIconPosition)
                    ? "Left" : settings.OverlayControllerIconPosition);

            OverlayPreviewControllerHost.Children.Clear();
            OverlayPreviewControllerHost.RowDefinitions.Clear();
            OverlayPreviewControllerHost.ColumnDefinitions.Clear();
            OverlayPreviewControllerIcon.Margin = new Thickness(0);
            OverlayPreviewControllerName.Margin = new Thickness(0);
            Grid.SetRow(OverlayPreviewControllerIcon, 0);
            Grid.SetColumn(OverlayPreviewControllerIcon, 0);
            Grid.SetRow(OverlayPreviewControllerName, 0);
            Grid.SetColumn(OverlayPreviewControllerName, 0);

            if (!showIcon && !showName)
            {
                OverlayPreviewControllerHost.Visibility = Visibility.Collapsed;
                return;
            }

            OverlayPreviewControllerHost.Visibility = Visibility.Visible;
            var both = showIcon && showName;

            if (both &&
                (string.Equals(position, "Top", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(position, "Bottom", StringComparison.OrdinalIgnoreCase)))
            {
                OverlayPreviewControllerHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                OverlayPreviewControllerHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var iconFirst = string.Equals(position, "Top", StringComparison.OrdinalIgnoreCase);
                Grid.SetRow(OverlayPreviewControllerIcon, iconFirst ? 0 : 1);
                Grid.SetColumn(OverlayPreviewControllerIcon, 0);
                Grid.SetRow(OverlayPreviewControllerName, iconFirst ? 1 : 0);
                Grid.SetColumn(OverlayPreviewControllerName, 0);
                OverlayPreviewControllerIcon.Margin = iconFirst
                    ? new Thickness(0, 0, 0, gap) : new Thickness(0, gap, 0, 0);
            }
            else if (both)
            {
                OverlayPreviewControllerHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                OverlayPreviewControllerHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var iconFirst = !string.Equals(position, "Right", StringComparison.OrdinalIgnoreCase);
                Grid.SetRow(OverlayPreviewControllerIcon, 0);
                Grid.SetColumn(OverlayPreviewControllerIcon, iconFirst ? 0 : 1);
                Grid.SetRow(OverlayPreviewControllerName, 0);
                Grid.SetColumn(OverlayPreviewControllerName, iconFirst ? 1 : 0);
                OverlayPreviewControllerIcon.Margin = iconFirst
                    ? new Thickness(0, 0, gap, 0) : new Thickness(gap, 0, 0, 0);
            }
            else
            {
                OverlayPreviewControllerHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                OverlayPreviewControllerHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            if (showIcon)
            {
                OverlayPreviewControllerHost.Children.Add(OverlayPreviewControllerIcon);
            }

            if (showName)
            {
                OverlayPreviewControllerHost.Children.Add(OverlayPreviewControllerName);
            }

            FitPreviewControllerIcon(settings == null ? 30 : settings.OverlayControllerIconSize);
        }

        private void FitPreviewControllerIcon(double maxSize)
        {
            if (OverlayPreviewControllerIcon == null)
            {
                return;
            }

            maxSize = Math.Max(1, maxSize);
            var data = OverlayPreviewControllerIcon.Data;
            if (data == null)
            {
                OverlayPreviewControllerIcon.Width = maxSize;
                OverlayPreviewControllerIcon.Height = maxSize;
                return;
            }

            var bounds = data.Bounds;
            try
            {
                var flattened = data.GetFlattenedPathGeometry(0.25, ToleranceType.Absolute);
                if (flattened != null && !flattened.Bounds.IsEmpty &&
                    flattened.Bounds.Width > 0 && flattened.Bounds.Height > 0)
                {
                    bounds = flattened.Bounds;
                }
            }
            catch
            {
            }

            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                OverlayPreviewControllerIcon.Width = maxSize;
                OverlayPreviewControllerIcon.Height = maxSize;
                return;
            }

            var aspect = bounds.Width / bounds.Height;
            if (aspect >= 1.0)
            {
                OverlayPreviewControllerIcon.Width = maxSize;
                OverlayPreviewControllerIcon.Height = maxSize / aspect;
            }
            else
            {
                OverlayPreviewControllerIcon.Height = maxSize;
                OverlayPreviewControllerIcon.Width = maxSize * aspect;
            }

            OverlayPreviewControllerIcon.Stretch = Stretch.Fill;
        }

        private bool suppressingStylePresetMark;
        private ScrollViewer hostScrollViewer;
        private Window hostWindow;
        private GamepadTesterViewModel testerViewModel;
        private static readonly string[] NotificationStylePropertyNames =
        {
            "NotificationWidth", "NotificationScalePercent", "NotificationDurationMilliseconds",
            "NotificationPosition", "NotificationBackgroundColor", "NotificationTextColor",
            "NotificationSecondaryTextColor", "NotificationConnectedColor", "NotificationDisconnectedColor",
            "NotificationWarningColor", "NotificationLowBatteryColor", "NotificationTitleFontSize",
            "NotificationMessageFontSize", "NotificationIconSize", "NotificationIconPosition",
            "NotificationPadding", "NotificationElementSpacing", "NotificationShowBorder",
            "NotificationBorderPosition", "NotificationBorderThickness", "NotificationCornerRadius",
            "NotificationShowConnectionBadge", "NotificationScreenMargin", "NotificationShowShadow",
            "NotificationFontFamily", "NotificationFontWeight", "NotificationTextAlignment",
            "NotificationAccentMode", "NotificationAnimation", "NotificationShowTitle",
            "DesktopNotificationWidth", "DesktopNotificationScalePercent", "DesktopNotificationDurationMilliseconds",
            "DesktopNotificationPosition", "DesktopNotificationBackgroundColor", "DesktopNotificationTextColor",
            "DesktopNotificationSecondaryTextColor", "DesktopNotificationConnectedColor",
            "DesktopNotificationDisconnectedColor", "DesktopNotificationWarningColor",
            "DesktopNotificationLowBatteryColor", "DesktopNotificationTitleFontSize",
            "DesktopNotificationMessageFontSize", "DesktopNotificationIconSize",
            "DesktopNotificationIconPosition", "DesktopNotificationPadding",
            "DesktopNotificationElementSpacing", "DesktopNotificationShowBorder",
            "DesktopNotificationBorderPosition", "DesktopNotificationBorderThickness",
            "DesktopNotificationCornerRadius",
            "DesktopNotificationShowConnectionBadge", "DesktopNotificationScreenMargin",
            "DesktopNotificationShowShadow", "DesktopNotificationFontFamily",
            "DesktopNotificationFontWeight", "DesktopNotificationTextAlignment",
            "DesktopNotificationAccentMode", "DesktopNotificationAnimation", "DesktopNotificationShowTitle",
            "ShowControllerNameInNotifications", "ShowControllerNameInDesktopNotifications"
        };

        private static readonly string[] OverlayStylePropertyNames =
        {
            "OverlayScalePercent", "OverlayDimColor", "OverlayCardColor", "OverlayAccentColor",
            "OverlayTextColor", "OverlayWarningColor", "OverlayTitleFontSize", "OverlayControllerFontSize",
            "OverlayInstructionFontSize", "OverlayStatusFontSize", "OverlayControllerIconSize",
            "OverlayStatusIconSize", "OverlayShowControllerIcon", "OverlayShowStatusIcon",
            "OverlayShowControllerName", "OverlayShowConnectionBadge", "OverlayShowBatteryBadge",
            "OverlayShowTitle", "OverlayShowInstruction", "OverlayShowPauseStatus",
            "OverlayControllerIconPosition", "OverlayCardPosition", "OverlayAnimation",
            "OverlayBorderPosition", "OverlayCardWidth", "OverlayPadding",
            "OverlayElementSpacing", "OverlayShowBorder", "OverlayBorderThickness", "OverlayCornerRadius",
            "OverlayShowShadow", "OverlayFontFamily", "OverlayFontWeight",
            "OverlayTitleFontFamily", "OverlayTitleFontWeight", "OverlayControllerFontFamily",
            "OverlayControllerFontWeight", "OverlayInstructionFontFamily", "OverlayInstructionFontWeight",
            "OverlayStatusFontFamily", "OverlayStatusFontWeight", "OverlayConnectionBadgeTextColor",
            "OverlayConnectionBadgeIconColor", "OverlayConnectionBadgeBackgroundColor",
            "OverlayConnectionBadgeBorderColor", "OverlayConnectionBadgeBorderThickness",
            "OverlayConnectionBadgeCornerRadius", "OverlayConnectionBadgeIconSize",
            "OverlayConnectionBadgeTextSize", "OverlayBatteryBadgeTextColor",
            "OverlayBatteryBadgeIconColor", "OverlayBatteryBadgeBackgroundColor",
            "OverlayBatteryBadgeBorderColor", "OverlayBatteryBadgeBorderThickness",
            "OverlayBatteryBadgeCornerRadius", "OverlayBatteryBadgeIconSize",
            "OverlayBatteryBadgeTextSize", "OverlayBatteryBadgeUseStateColors",
            "OverlayBatteryBadgeFullColor", "OverlayBatteryBadgeMediumColor",
            "OverlayBatteryBadgeLowColor", "OverlayBatteryBadgeEmptyColor"
        };

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (plugin == null)
            {
                return;
            }

            plugin.ControllerSnapshotChanged += OnControllerSnapshotChanged;
            ApplyAppearancePreset();
            BuildAppearancePresetChips();
            BuildNotificationStylePresetChips();
            BuildOverlayStylePresetChips();
            BuildNotificationSoundPackChips();
            ApplyPreferredWindowSize();
            AttachToHost();
            ApplyLegacyTesterWarning();
            ApplyPendingTesterOpen();
            if (TesterTab != null && TesterTab.IsSelected)
            {
                AttachTesterView();
            }
            Dispatcher.BeginInvoke(new Action(AttachToHost), DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(AttachToHost), DispatcherPriority.ApplicationIdle);
            Dispatcher.BeginInvoke(new Action(FillSelectedContentHosts), DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(FillSelectedContentHosts), DispatcherPriority.ApplicationIdle);
            RefreshOverview();
        }

        private void ApplyAppearancePreset()
        {
            var settings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            var preset = settings != null ? settings.AppearancePreset : SettingsAppearance.Midnight;
            SettingsAppearance.Apply(this, preset);
            if (themeStandaloneWindow)
            {
                SettingsAppearance.ApplyWindow(Window.GetWindow(this), preset);
            }

            RefreshAppearancePresetChips();
            RefreshNotificationStylePresetChips();
            RefreshOverlayStylePresetChips();
            RefreshNotificationSoundPackChips();
        }

        private void BuildAppearancePresetChips()
        {
            if (AppearancePresetChips == null)
            {
                return;
            }

            AppearancePresetChips.Children.Clear();
            var options = new[]
            {
                Tuple.Create(SettingsAppearance.Midnight, "LOCCSM_PresetMidnight", "Midnight"),
                Tuple.Create(SettingsAppearance.Paper, "LOCCSM_PresetPaper", "Paper"),
                Tuple.Create(SettingsAppearance.Oled, "LOCCSM_PresetOled", "OLED"),
                Tuple.Create(SettingsAppearance.Ocean, "LOCCSM_PresetOcean", "Ocean"),
                Tuple.Create(SettingsAppearance.Ember, "LOCCSM_PresetEmber", "Ember")
            };

            foreach (var option in options)
            {
                var label = plugin == null ? option.Item3 : plugin.Loc(option.Item2);
                if (string.IsNullOrWhiteSpace(label) || label == option.Item2)
                {
                    label = option.Item3;
                }

                var button = CreatePresetChipButton(label, option.Item1);
                button.Click += AppearancePresetChip_OnClick;
                AppearancePresetChips.Children.Add(button);
            }

            RefreshAppearancePresetChips();
        }

        private static ControlTemplate CreateAppearanceChipTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "Bd";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.SnapsToDevicePixelsProperty, true);
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
            });
            border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
            });
            border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
            });
            border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
            });
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetBinding(TextElement.ForegroundProperty, new System.Windows.Data.Binding("Foreground")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
            });
            border.AppendChild(presenter);
            template.VisualTree = border;

            var pressed = new Trigger
            {
                Property = Button.IsPressedProperty,
                Value = true
            };
            pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.88));
            template.Triggers.Add(pressed);
            return template;
        }

        private SettingsAppearance.Palette GetCurrentAppearancePalette()
        {
            var settings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            var selected = settings != null ? settings.AppearancePreset : SettingsAppearance.Midnight;
            return SettingsAppearance.GetPalette(selected);
        }

        private void AppearancePresetChip_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var preset = button == null ? null : button.Tag as string;
            var settings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            if (settings == null || string.IsNullOrWhiteSpace(preset))
            {
                return;
            }

            settings.AppearancePreset = preset;
            ApplyAppearancePreset();
        }

        private void RefreshAppearancePresetChips()
        {
            RefreshChipSelection(
                AppearancePresetChips,
                boundSettings == null
                    ? SettingsAppearance.Midnight
                    : SettingsAppearance.Normalize(boundSettings.AppearancePreset));
        }

        private void BuildNotificationStylePresetChips()
        {
            BuildNamedPresetChips(
                NotificationStylePresetChips,
                NotificationStylePresets.NamedPresets.Concat(new[] { NotificationStylePresets.Custom }),
                NotificationStylePresets.LocKey,
                NotificationStylePresetChip_OnClick);
            RefreshNotificationStylePresetChips();
        }

        private void BuildOverlayStylePresetChips()
        {
            BuildNamedPresetChips(
                OverlayStylePresetChips,
                OverlayStylePresets.NamedPresets.Concat(new[] { OverlayStylePresets.Custom }),
                OverlayStylePresets.LocKey,
                OverlayStylePresetChip_OnClick);
            RefreshOverlayStylePresetChips();
        }

        private void BuildNotificationSoundPackChips()
        {
            if (NotificationSoundPackSelector == null)
            {
                return;
            }

            var options = new System.Collections.Generic.List<NotificationSoundPackOption>();
            foreach (var pack in NotificationSoundCatalog.AllPacks)
            {
                var label = plugin == null
                    ? NotificationSoundCatalog.DisplayName(pack)
                    : plugin.Loc(NotificationSoundCatalog.LocKey(pack));
                if (string.IsNullOrWhiteSpace(label) || label == NotificationSoundCatalog.LocKey(pack))
                {
                    label = NotificationSoundCatalog.DisplayName(pack);
                }

                options.Add(new NotificationSoundPackOption { Key = pack, DisplayName = label });
            }

            NotificationSoundPackSelector.ItemsSource = options;
            RefreshNotificationSoundPackChips();
        }

        private void BuildNamedPresetChips(
            WrapPanel panel,
            System.Collections.Generic.IEnumerable<string> presets,
            Func<string, string> locKey,
            RoutedEventHandler onClick)
        {
            if (panel == null)
            {
                return;
            }

            panel.Children.Clear();
            foreach (var preset in presets)
            {
                var key = locKey(preset);
                var label = plugin == null ? preset : plugin.Loc(key);
                if (string.IsNullOrWhiteSpace(label) || label == key)
                {
                    label = preset;
                }

                var button = CreatePresetChipButton(label, preset);
                button.Click += onClick;
                panel.Children.Add(button);
            }
        }

        private Button CreatePresetChipButton(string label, string tag)
        {
            var button = new Button
            {
                Content = label,
                Tag = tag,
                MinHeight = 36,
                Height = 36,
                MinWidth = 88,
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(0, 0, 8, 8),
                Cursor = Cursors.Hand,
                Focusable = true,
                FocusVisualStyle = null,
                OverridesDefaultStyle = true,
                BorderThickness = new Thickness(1),
                FontSize = 14,
                Template = CreateAppearanceChipTemplate()
            };
            button.MouseEnter += PresetChip_OnMouseEnter;
            button.MouseLeave += PresetChip_OnMouseLeave;
            return button;
        }

        private void PresetChip_OnMouseEnter(object sender, MouseEventArgs e)
        {
            var button = sender as Button;
            if (button == null || IsPresetChipSelected(button))
            {
                return;
            }

            var palette = GetCurrentAppearancePalette();
            button.Background = new SolidColorBrush(palette.Hover);
            button.Opacity = 1;
        }

        private void PresetChip_OnMouseLeave(object sender, MouseEventArgs e)
        {
            var button = sender as Button;
            if (button == null || IsPresetChipSelected(button))
            {
                return;
            }

            var palette = GetCurrentAppearancePalette();
            button.Background = new SolidColorBrush(palette.BadgeBg);
            button.Opacity = 1;
        }

        private bool IsPresetChipSelected(Button button)
        {
            if (button == null)
            {
                return false;
            }

            var selected = GetSelectedValueForChipPanel(button.Parent as WrapPanel);
            return string.Equals(button.Tag as string, selected, StringComparison.OrdinalIgnoreCase);
        }

        private string GetSelectedValueForChipPanel(WrapPanel panel)
        {
            var settings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            if (panel == null || settings == null)
            {
                return string.Empty;
            }

            if (panel == AppearancePresetChips)
            {
                return SettingsAppearance.Normalize(settings.AppearancePreset);
            }

            if (panel == NotificationStylePresetChips)
            {
                return NotificationStylePresets.Normalize(settings.NotificationStylePreset);
            }

            if (panel == OverlayStylePresetChips)
            {
                return OverlayStylePresets.Normalize(settings.OverlayStylePreset);
            }

            return string.Empty;
        }

        private void NotificationStylePresetChip_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var preset = button == null ? null : button.Tag as string;
            var settings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            if (settings == null || string.IsNullOrWhiteSpace(preset))
            {
                return;
            }

            suppressingStylePresetMark = true;
            try
            {
                NotificationStylePresets.Apply(settings, preset);
            }
            finally
            {
                suppressingStylePresetMark = false;
            }

            RefreshNotificationStylePresetChips();
        }

        private void OverlayStylePresetChip_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var preset = button == null ? null : button.Tag as string;
            var settings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            if (settings == null || string.IsNullOrWhiteSpace(preset))
            {
                return;
            }

            suppressingStylePresetMark = true;
            try
            {
                OverlayStylePresets.Apply(settings, preset);
            }
            finally
            {
                suppressingStylePresetMark = false;
            }

            RefreshOverlayStylePresetChips();
            RefreshOverlayPreviewControllerLayout();
        }

        private void NotificationSoundPackChip_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var pack = button == null ? null : button.Tag as string;
            var settings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            if (settings == null || string.IsNullOrWhiteSpace(pack))
            {
                return;
            }

            settings.NotificationSoundPack = pack;
            RefreshNotificationSoundPackChips();
        }

        private void RefreshNotificationStylePresetChips()
        {
            RefreshChipSelection(
                NotificationStylePresetChips,
                boundSettings == null
                    ? NotificationStylePresets.Soft
                    : boundSettings.NotificationStylePreset);
        }

        private void RefreshOverlayStylePresetChips()
        {
            RefreshChipSelection(
                OverlayStylePresetChips,
                boundSettings == null
                    ? OverlayStylePresets.Soft
                    : boundSettings.OverlayStylePreset);
        }

        private void RefreshNotificationSoundPackChips()
        {
            if (NotificationSoundPackSelector != null)
            {
                NotificationSoundPackSelector.SelectedValue = boundSettings == null
                    ? NotificationSoundCatalog.ModernCrystal
                    : NotificationSoundCatalog.Normalize(boundSettings.NotificationSoundPack);
            }
        }

        private void RefreshChipSelection(WrapPanel panel, string selected)
        {
            if (panel == null)
            {
                return;
            }

            var palette = GetCurrentAppearancePalette();
            var accent = new SolidColorBrush(palette.Accent);
            var accentOn = new SolidColorBrush(palette.AccentOn);
            var badgeBg = new SolidColorBrush(palette.BadgeBg);
            var text = new SolidColorBrush(palette.Text);
            accent.Freeze();
            accentOn.Freeze();
            badgeBg.Freeze();
            text.Freeze();

            foreach (var child in panel.Children)
            {
                var button = child as Button;
                if (button == null)
                {
                    continue;
                }

                var isSelected = string.Equals(button.Tag as string, selected, StringComparison.OrdinalIgnoreCase);
                button.Background = isSelected ? accent : badgeBg;
                button.Foreground = isSelected ? accentOn : text;
                button.BorderBrush = isSelected ? accent : new SolidColorBrush(palette.Border);
                button.BorderThickness = new Thickness(1);
                button.FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal;
                button.Opacity = 1;
            }
        }

        private void PreviewNotificationSoundClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            plugin.PlayNotificationSoundPreview(button == null ? null : button.Tag as string);
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
            if (TesterTab != null && TesterTab.IsSelected)
            {
                AttachTesterView();
            }

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
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            ClearValue(WidthProperty);
            ClearValue(HeightProperty);

            FillSelectedContentHosts();
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
            if (boundSettings != null)
            {
                boundSettings.PropertyChanged -= OnBoundSettingsPropertyChanged;
                boundSettings = null;
            }

            if (plugin != null)
            {
                plugin.ControllerSnapshotChanged -= OnControllerSnapshotChanged;
            }
            DetachFromHost();
            DisposeTesterView();
        }

        private void AttachTesterView()
        {
            if (testerViewModel != null || TesterPane == null)
            {
                return;
            }

            testerViewModel = null;
            var view = plugin.CreateTesterView(out testerViewModel);
            if (view == null)
            {
                return;
            }

            TesterPane.DataContext = testerViewModel;
        }

        private void DisposeTesterView()
        {
            if (testerViewModel == null)
            {
                return;
            }

            testerViewModel.Dispose();
            testerViewModel = null;
            if (TesterPane != null)
            {
                TesterPane.DataContext = null;
            }
        }

        private void ApplyLegacyTesterWarning()
        {
            var visible = plugin.IsLegacyGamepadTesterInstalled() ? Visibility.Visible : Visibility.Collapsed;
            if (LegacyGamepadTesterWarning != null)
            {
                LegacyGamepadTesterWarning.Visibility = visible;
            }

            if (AboutLegacyGamepadTesterWarning != null)
            {
                AboutLegacyGamepadTesterWarning.Visibility = visible;
            }
        }

        private void ApplyPendingTesterOpen()
        {
            if (!TesterIntegration.PendingOpenSettingsTab &&
                TesterIntegration.PendingVendorId == 0 && TesterIntegration.PendingProductId == 0)
            {
                return;
            }

            RootTabs.SelectedItem = TesterTab;
            AttachTesterView();
            if (testerViewModel != null)
            {
                testerViewModel.SelectedTabIndex = TesterIntegration.PendingTabIndex;
                testerViewModel.RequestControllerSelection(
                    TesterIntegration.PendingVendorId,
                    TesterIntegration.PendingProductId,
                    TesterIntegration.PendingControllerName);
            }

            TesterIntegration.PendingOpenSettingsTab = false;
            TesterIntegration.PendingTabIndex = 0;
            TesterIntegration.PendingVendorId = 0;
            TesterIntegration.PendingProductId = 0;
            TesterIntegration.PendingControllerName = null;
        }

        private void OpenTesterClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            var row = button == null ? null : button.DataContext as ControllerRow;
            if (row == null || !row.InteractionsEnabled || row.Controller == null)
            {
                if (row != null && !row.InteractionsEnabled)
                {
                    return;
                }

                if (TesterTab != null)
                {
                    TesterTab.IsSelected = true;
                }

                AttachTesterView();
                return;
            }

            TesterIntegration.PendingTabIndex = 0;
            TesterIntegration.RequestController(
                row.Controller.VendorId, row.Controller.ProductId, row.Controller.Name);
            if (TesterTab != null)
            {
                TesterTab.IsSelected = true;
            }

            AttachTesterView();
            if (testerViewModel != null)
            {
                testerViewModel.SelectedTabIndex = 0;
                testerViewModel.RequestControllerSelection(
                    row.Controller.VendorId, row.Controller.ProductId, row.Controller.Name);
            }

            TesterIntegration.PendingOpenSettingsTab = false;
            TesterIntegration.PendingTabIndex = 0;
            TesterIntegration.PendingVendorId = 0;
            TesterIntegration.PendingProductId = 0;
            TesterIntegration.PendingControllerName = null;
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

        private void ExpanderChevronButton_OnClick(object sender, RoutedEventArgs e)
        {
            for (var parent = VisualTreeHelper.GetParent(sender as DependencyObject);
                 parent != null;
                 parent = VisualTreeHelper.GetParent(parent))
            {
                var expander = parent as Expander;
                if (expander == null)
                {
                    continue;
                }

                expander.IsExpanded = !expander.IsExpanded;
                e.Handled = true;
                return;
            }
        }

        private void ExportHidDiagnosticsClick(object sender, RoutedEventArgs args)
        {
            plugin.ExportHidDiagnostics();
        }

        private void ExportSupportReportClick(object sender, RoutedEventArgs args)
        {
            plugin.ExportSupportReport();
        }

        private void ExportVisualProfileClick(object sender, RoutedEventArgs args)
        {
            var settings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            if (settings == null || plugin == null)
            {
                return;
            }

            plugin.ExportVisualProfile(settings);
        }

        private void ImportVisualProfileClick(object sender, RoutedEventArgs args)
        {
            var settings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            if (settings == null || plugin == null)
            {
                return;
            }

            plugin.ImportVisualProfile(settings, RefreshVisualProfileUi);
        }

        private void RefreshVisualProfileUi()
        {
            RefreshNotificationStylePresetChips();
            RefreshOverlayStylePresetChips();
            RefreshNotificationSoundPackChips();
        }

        private void OpenSetupWizardClick(object sender, RoutedEventArgs args)
        {
            plugin.OpenSetupWizard();
        }

        private void PreviewNotificationClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            plugin.ShowNotificationPreview(button == null ? null : button.Tag as string);
        }

        private void SelectNotificationBackgroundImageClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            var targetSettings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            if (plugin == null || targetSettings == null)
            {
                return;
            }

            plugin.SelectNotificationBackgroundImage(
                targetSettings,
                string.Equals(button == null ? null : button.Tag as string, "Desktop", StringComparison.OrdinalIgnoreCase));
        }

        private void ClearNotificationBackgroundImageClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            var targetSettings = boundSettings ?? DataContext as ControllerSessionManagerSettings;
            if (plugin == null || targetSettings == null)
            {
                return;
            }

            plugin.ClearNotificationBackgroundImage(
                targetSettings,
                string.Equals(button == null ? null : button.Tag as string, "Desktop", StringComparison.OrdinalIgnoreCase));
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

            var appearanceSettings = boundSettings ?? settings;
            SettingsAppearance.ApplyWindow(
                dialog,
                appearanceSettings != null
                    ? appearanceSettings.AppearancePreset
                    : SettingsAppearance.Midnight);

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
            var connected = plugin.GetDisplayControllerSnapshot().Where(a => a.IsConnected).ToList();
            var settings = DataContext as ControllerSessionManagerSettings;
            if (settings != null)
            {
                settings.SyncControllerProfiles(connected);
                connected = plugin.GetDisplayControllerSnapshot().Where(a => a.IsConnected).ToList();
            }
            ConnectedCountText.Text = connected.Count.ToString(CultureInfo.CurrentCulture);
            PrimaryControllerText.Text = plugin.GetPrimaryControllerText();
            XInputStatusText.Text = connected.Count > 0
                ? plugin.Loc("LOCCSM_ProviderActive")
                : plugin.Loc("LOCCSM_ProviderReady");
            XInputStatusPillText.Text = connected.Count > 0
                ? plugin.Loc("LOCCSM_BadgeActive")
                : plugin.Loc("LOCCSM_BadgeReady");
            ApplyStatusBadgeAppearance(XInputStatusPillText, "PositiveRatingBrush", "Narian.BadgeSuccessBg");
            LastRefreshText.Text = DateTime.Now.ToString("T", CultureInfo.CurrentCulture);
            SessionStatusText.Text = plugin.GetSessionStatusText();
            SessionStatusPillText.Text = plugin.GetSessionStatusBadge();
            ApplySessionStatusBadgeAppearance();
            ActiveSessionControllersText.Text = plugin.GetActiveSessionControllersText();
            var listSignature = ControllerDisplayHold.IdentitySignature(connected);
            var rows = connected.Select(CreateRow).ToList();
            var existing = ControllerList.ItemsSource as System.Collections.IList;
            if (listSignature == lastControllerListSignature &&
                existing != null && existing.Count == rows.Count)
            {
                for (var index = 0; index < rows.Count; index++)
                {
                    var current = existing[index] as ControllerRow;
                    if (current != null)
                    {
                        current.CopyFrom(rows[index]);
                    }
                }
            }
            else
            {
                lastControllerListSignature = listSignature;
                ControllerList.ItemsSource = rows;
            }

            EmptyControllersText.Visibility = connected.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplySessionStatusBadgeAppearance()
        {
            if (SessionStatusPillText == null)
            {
                return;
            }

            var badge = SessionStatusPillText.Text ?? string.Empty;
            if (badge == plugin.Loc("LOCCSM_BadgeAlert") ||
                badge == plugin.Loc("LOCCSM_BadgeWaiting"))
            {
                ApplyStatusBadgeAppearance(SessionStatusPillText, "WarningBrush", "Narian.BadgeWarningBg");
                return;
            }

            if (badge == plugin.Loc("LOCCSM_BadgeIdle"))
            {
                ApplyStatusBadgeAppearance(SessionStatusPillText, "GlyphBrush", "Narian.BadgeMutedBg");
                return;
            }

            ApplyStatusBadgeAppearance(SessionStatusPillText, "PositiveRatingBrush", "Narian.BadgeSuccessBg");
        }

        private static void ApplyStatusBadgeAppearance(TextBlock textBlock, string brushKey, string backgroundKey)
        {
            if (textBlock == null || string.IsNullOrWhiteSpace(brushKey))
            {
                return;
            }

            textBlock.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
            textBlock.Opacity = 1.0;

            var badge = textBlock.Parent as Border;
            if (badge == null)
            {
                for (var parent = VisualTreeHelper.GetParent(textBlock);
                     parent != null;
                     parent = VisualTreeHelper.GetParent(parent))
                {
                    badge = parent as Border;
                    if (badge != null)
                    {
                        break;
                    }
                }
            }

            if (badge == null)
            {
                return;
            }

            badge.BorderThickness = new Thickness(0);
            badge.Opacity = 1.0;
            if (!string.IsNullOrWhiteSpace(backgroundKey))
            {
                badge.SetResourceReference(Border.BackgroundProperty, backgroundKey);
            }
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
                ConnectionBrush = GetConnectionBrush(controller.ConnectionType),
                InteractionsEnabled = !IsUnknownConnection(controller.ConnectionType),
                Battery = battery,
                BatteryTooltip = LabeledTooltip("LOCCSM_Battery", battery),
                BatteryBrush = GetBatteryBrush(controller.BatteryLevel),
                IconGeometry = GetControllerIconGeometry(controller, profile),
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

        private Brush GetConnectionBrush(string connectionType)
        {
            if (IsUnknownConnection(connectionType))
            {
                return BatteryUnknownBrush;
            }

            var themeBrush = TryFindResource("TextBrush") as Brush;
            return themeBrush ?? CreateFrozenBrush(220, 225, 230);
        }

        private static bool IsUnknownConnection(string connectionType)
        {
            return ControllerDeviceIdentity.IsUnknownConnection(connectionType);
        }

        private static string GetConnectionIconGeometry(string connectionType)
        {
            return ControllerConnectionIcons.GetPathData(connectionType);
        }

        private void VibrateControllerClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            var row = button == null ? null : button.DataContext as ControllerRow;
            if (row == null || !row.InteractionsEnabled || row.Controller == null ||
                !plugin.TryVibrateController(row.Controller))
            {
                if (row != null && !row.InteractionsEnabled)
                {
                    return;
                }

                plugin.ShowVibrationUnavailable();
            }
        }

        private void ControllerIconSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var selector = sender as ComboBox;
            var row = selector == null ? null : selector.DataContext as ControllerRow;
            var option = selector == null ? null : selector.SelectedItem as ControllerIconOption;
            if (row == null || !row.InteractionsEnabled || row.Profile == null || option == null)
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

        private static string GetControllerIconGeometry(ControllerDeviceSnapshot controller,
            ControllerProfile profile)
        {
            return SvgIconGeometryLoader.GetPathData(ControllerIconCatalog.ResolveFileName(
                controller, profile == null ? null : profile.IconId));
        }

        private sealed class ControllerRow : System.ComponentModel.INotifyPropertyChanged
        {
            private string name;
            private string detectedName;
            private string provider;
            private string providerTooltip;
            private string connection;
            private string connectionTooltip;
            private string connectionIconGeometry;
            private string connectionFallback;
            private Brush connectionBrush;
            private bool interactionsEnabled = true;
            private string battery;
            private string batteryTooltip;
            private Brush batteryBrush;
            private string lastInput;
            private string iconGeometry;
            private string actionIconGeometry;

            public string Name
            {
                get { return name; }
                set { SetField(ref name, value, "Name"); }
            }

            public string DetectedName
            {
                get { return detectedName; }
                set { SetField(ref detectedName, value, "DetectedName"); }
            }

            public ControllerProfile Profile { get; set; }

            public string Provider
            {
                get { return provider; }
                set { SetField(ref provider, value, "Provider"); }
            }

            public string ProviderTooltip
            {
                get { return providerTooltip; }
                set { SetField(ref providerTooltip, value, "ProviderTooltip"); }
            }

            public string Connection
            {
                get { return connection; }
                set { SetField(ref connection, value, "Connection"); }
            }

            public string ConnectionTooltip
            {
                get { return connectionTooltip; }
                set { SetField(ref connectionTooltip, value, "ConnectionTooltip"); }
            }

            public string ConnectionIconGeometry
            {
                get { return connectionIconGeometry; }
                set { SetField(ref connectionIconGeometry, value, "ConnectionIconGeometry"); }
            }

            public string ConnectionFallback
            {
                get { return connectionFallback; }
                set { SetField(ref connectionFallback, value, "ConnectionFallback"); }
            }

            public Brush ConnectionBrush
            {
                get { return connectionBrush; }
                set { SetField(ref connectionBrush, value, "ConnectionBrush"); }
            }

            public bool InteractionsEnabled
            {
                get { return interactionsEnabled; }
                set { SetField(ref interactionsEnabled, value, "InteractionsEnabled"); }
            }

            public string Battery
            {
                get { return battery; }
                set { SetField(ref battery, value, "Battery"); }
            }

            public string BatteryTooltip
            {
                get { return batteryTooltip; }
                set { SetField(ref batteryTooltip, value, "BatteryTooltip"); }
            }

            public Brush BatteryBrush
            {
                get { return batteryBrush; }
                set { SetField(ref batteryBrush, value, "BatteryBrush"); }
            }

            public string LastInput
            {
                get { return lastInput; }
                set { SetField(ref lastInput, value, "LastInput"); }
            }

            public ControllerDeviceSnapshot Controller { get; set; }

            public string ActionIconGeometry
            {
                get { return actionIconGeometry; }
                set { SetField(ref actionIconGeometry, value, "ActionIconGeometry"); }
            }

            public string IconGeometry
            {
                get { return iconGeometry; }
                set { SetField(ref iconGeometry, value, "IconGeometry"); }
            }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

            public void CopyFrom(ControllerRow source)
            {
                if (source == null)
                {
                    return;
                }

                Name = source.Name;
                DetectedName = source.DetectedName;
                Profile = source.Profile;
                Provider = source.Provider;
                ProviderTooltip = source.ProviderTooltip;
                Connection = source.Connection;
                ConnectionTooltip = source.ConnectionTooltip;
                ConnectionIconGeometry = source.ConnectionIconGeometry;
                ConnectionFallback = source.ConnectionFallback;
                ConnectionBrush = source.ConnectionBrush;
                InteractionsEnabled = source.InteractionsEnabled;
                Battery = source.Battery;
                BatteryTooltip = source.BatteryTooltip;
                BatteryBrush = source.BatteryBrush;
                LastInput = source.LastInput;
                Controller = source.Controller;
                ActionIconGeometry = source.ActionIconGeometry;
                IconGeometry = source.IconGeometry;
            }

            private void SetField<T>(ref T field, T value, string propertyName)
            {
                if (object.Equals(field, value))
                {
                    return;
                }

                field = value;
                var handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
                }
            }
        }
        private sealed class NotificationSoundPackOption
        {
            public string Key { get; set; }
            public string DisplayName { get; set; }
        }
    }
}
