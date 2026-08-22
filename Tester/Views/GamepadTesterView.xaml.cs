using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ControllerSessionManager.Tester.ViewModels;

namespace ControllerSessionManager.Tester.Views
{
    public partial class GamepadTesterView : UserControl
    {
        private const double GeneralBottomStackBreakpoint = 780;
        private const double SticksTabStackBreakpoint = 780;

        private ScrollViewer hostScrollViewer;
        private FrameworkElement fillHost;
        private Window hostWindow;
        private bool? generalBottomStacked;
        private int generalBottomLayoutKey = -1;
        private int sticksDiagnosticsLayoutKey = -1;
        private int sticksCalibrationLayoutKey = -1;

        public GamepadTesterView()
        {
            InitializeComponent();
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSelfSizeChanged;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            StretchFillLayout();
            UpdateResponsiveLayouts();
            Dispatcher.BeginInvoke(new Action(StretchFillLayout), DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(StretchFillLayout), DispatcherPriority.ApplicationIdle);
            Dispatcher.BeginInvoke(new Action(UpdateResponsiveLayouts), DispatcherPriority.Loaded);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            UpdateResponsiveLayouts();
        }

        private void GeneralBottomRow_SizeChanged(object sender, SizeChangedEventArgs args)
        {
            UpdateGeneralBottomLayout();
        }

        private void SticksDiagnosticsRow_SizeChanged(object sender, SizeChangedEventArgs args)
        {
            UpdateSticksDiagnosticsLayout();
        }

        private void SticksCalibrationRow_SizeChanged(object sender, SizeChangedEventArgs args)
        {
            UpdateSticksCalibrationLayout();
        }

        private void UpdateResponsiveLayouts()
        {
            UpdateGeneralBottomLayout();
            UpdateSticksDiagnosticsLayout();
            UpdateSticksCalibrationLayout();
        }

        private void UpdateGeneralBottomLayout()
        {
            if (GeneralBottomRow == null || GeneralSticksPanel == null || GeneralRumblePanel == null)
            {
                return;
            }

            var rumbleVisible = IsRumblePanelExpectedVisible();
            var stack = !rumbleVisible || GeneralBottomRow.ActualWidth < GeneralBottomStackBreakpoint;
            var layoutKey = (stack ? 1 : 0) | (rumbleVisible ? 2 : 0);
            if (generalBottomStacked.HasValue && generalBottomLayoutKey == layoutKey)
            {
                return;
            }

            generalBottomStacked = stack;
            generalBottomLayoutKey = layoutKey;

            if (!rumbleVisible)
            {
                ApplyGeneralBottomSideBySide(fullWidthSticks: true);
                return;
            }

            if (stack)
            {
                ApplyGeneralBottomStacked();
            }
            else
            {
                ApplyGeneralBottomSideBySide(fullWidthSticks: false);
            }
        }

        private void UpdateSticksDiagnosticsLayout()
        {
            ApplyTwoPanelResponsiveLayout(
                SticksDiagnosticsRow,
                SticksLeftPanel,
                SticksRightPanel,
                ref sticksDiagnosticsLayoutKey,
                SticksTabStackBreakpoint,
                stackedSecondMinHeight: 0,
                sideBySideStarRows: true);
        }

        private void UpdateSticksCalibrationLayout()
        {
            ApplyTwoPanelResponsiveLayout(
                SticksCalibrationRow,
                SticksCenterCalibrationPanel,
                SticksRangeCalibrationPanel,
                ref sticksCalibrationLayoutKey,
                SticksTabStackBreakpoint,
                stackedSecondMinHeight: 0,
                sideBySideStarRows: false);
        }

        private static void ApplyTwoPanelResponsiveLayout(
            Grid row,
            FrameworkElement left,
            FrameworkElement right,
            ref int layoutKey,
            double breakpoint,
            double stackedSecondMinHeight,
            bool sideBySideStarRows)
        {
            if (row == null || left == null || right == null || row.ActualWidth < 1)
            {
                return;
            }

            var stack = row.ActualWidth < breakpoint;
            var key = stack ? 1 : 0;
            if (layoutKey == key)
            {
                return;
            }

            layoutKey = key;
            if (stack)
            {
                row.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                row.ColumnDefinitions[1].Width = new GridLength(0);
                row.RowDefinitions[0].Height = sideBySideStarRows
                    ? new GridLength(1, GridUnitType.Star)
                    : GridLength.Auto;
                row.RowDefinitions[1].Height = GridLength.Auto;

                Grid.SetRow(left, 0);
                Grid.SetColumn(left, 0);
                Grid.SetColumnSpan(left, 2);
                left.Margin = new Thickness(0, 0, 0, 12);
                left.ClearValue(FrameworkElement.MinHeightProperty);

                Grid.SetRow(right, 1);
                Grid.SetColumn(right, 0);
                Grid.SetColumnSpan(right, 2);
                right.Margin = new Thickness(0);
                if (stackedSecondMinHeight > 0)
                {
                    right.MinHeight = stackedSecondMinHeight;
                }
                else
                {
                    right.ClearValue(FrameworkElement.MinHeightProperty);
                }
            }
            else
            {
                row.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                row.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                row.RowDefinitions[0].Height = sideBySideStarRows
                    ? new GridLength(1, GridUnitType.Star)
                    : GridLength.Auto;
                row.RowDefinitions[1].Height = new GridLength(0);

                Grid.SetRow(left, 0);
                Grid.SetColumn(left, 0);
                Grid.SetColumnSpan(left, 1);
                left.Margin = new Thickness(0, 0, 12, 0);
                left.ClearValue(FrameworkElement.MinHeightProperty);

                Grid.SetRow(right, 0);
                Grid.SetColumn(right, 1);
                Grid.SetColumnSpan(right, 1);
                right.Margin = new Thickness(0);
                right.ClearValue(FrameworkElement.MinHeightProperty);
            }
        }

        private bool IsRumblePanelExpectedVisible()
        {
            var vm = DataContext as GamepadTesterViewModel;
            if (vm != null)
            {
                return vm.IsFullTesterMode;
            }

            return GeneralRumblePanel.Visibility == Visibility.Visible;
        }

        private void ApplyGeneralBottomSideBySide(bool fullWidthSticks)
        {
            GeneralBottomRow.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            GeneralBottomRow.ColumnDefinitions[1].Width = fullWidthSticks
                ? new GridLength(0)
                : new GridLength(1, GridUnitType.Star);
            GeneralBottomRow.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            GeneralBottomRow.RowDefinitions[1].Height = new GridLength(0);

            Grid.SetRow(GeneralSticksPanel, 0);
            Grid.SetColumn(GeneralSticksPanel, 0);
            Grid.SetColumnSpan(GeneralSticksPanel, fullWidthSticks ? 2 : 1);
            GeneralSticksPanel.Margin = fullWidthSticks
                ? new Thickness(0)
                : new Thickness(0, 0, 12, 0);

            Grid.SetRow(GeneralRumblePanel, 0);
            Grid.SetColumn(GeneralRumblePanel, 1);
            Grid.SetColumnSpan(GeneralRumblePanel, 1);
            GeneralRumblePanel.Margin = new Thickness(0);
            GeneralRumblePanel.MinHeight = 0;
        }

        private void ApplyGeneralBottomStacked()
        {
            GeneralBottomRow.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            GeneralBottomRow.ColumnDefinitions[1].Width = new GridLength(0);
            GeneralBottomRow.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            GeneralBottomRow.RowDefinitions[1].Height = GridLength.Auto;

            Grid.SetRow(GeneralSticksPanel, 0);
            Grid.SetColumn(GeneralSticksPanel, 0);
            Grid.SetColumnSpan(GeneralSticksPanel, 2);
            GeneralSticksPanel.Margin = new Thickness(0, 0, 0, 12);

            Grid.SetRow(GeneralRumblePanel, 1);
            Grid.SetColumn(GeneralRumblePanel, 0);
            Grid.SetColumnSpan(GeneralRumblePanel, 2);
            GeneralRumblePanel.Margin = new Thickness(0);
            GeneralRumblePanel.MinHeight = 220;
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            DetachFillHost();
            DetachHostScrollViewer();
            DetachHostWindow();
        }

        private void OnSelfSizeChanged(object sender, SizeChangedEventArgs args)
        {
            StretchSelectedContent(this);
            UpdateResponsiveLayouts();
        }

        private void TesterTabsSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            generalBottomStacked = null;
            generalBottomLayoutKey = -1;
            sticksDiagnosticsLayoutKey = -1;
            sticksCalibrationLayoutKey = -1;
            Dispatcher.BeginInvoke(new Action(StretchFillLayout), DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(UpdateResponsiveLayouts), DispatcherPriority.Loaded);
        }

        private void StretchFillLayout()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            StretchHostContentAlignment();
            StretchSelectedContent(this);

            if (IsInsideSettingsView())
            {
                DetachFillHost();
                DetachHostScrollViewer();
                DetachHostWindow();
                ClearValue(WidthProperty);
                ClearValue(HeightProperty);
                return;
            }

            AttachHostWindow();
            AttachHostScrollViewer();
            AttachFillHost();
            ApplyHostViewportSize();
        }

        private void StretchHostContentAlignment()
        {
            for (var parent = VisualTreeHelper.GetParent(this);
                 parent != null;
                 parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent is Window)
                {
                    break;
                }

                var contentControl = parent as ContentControl;
                if (contentControl != null)
                {
                    contentControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                    contentControl.VerticalContentAlignment = VerticalAlignment.Stretch;
                }

                var presenter = parent as ContentPresenter;
                if (presenter != null)
                {
                    presenter.HorizontalAlignment = HorizontalAlignment.Stretch;
                    presenter.VerticalAlignment = VerticalAlignment.Stretch;
                }

                var element = parent as FrameworkElement;
                if (element != null && element.Name == "PART_ContentView")
                {
                    break;
                }
            }
        }

        private void AttachFillHost()
        {
            var host = FindFillHost();
            if (ReferenceEquals(fillHost, host))
            {
                return;
            }

            DetachFillHost();
            fillHost = host;
            if (fillHost != null)
            {
                fillHost.SizeChanged += OnHostSizeChanged;
            }
        }

        private void DetachFillHost()
        {
            if (fillHost == null)
            {
                return;
            }

            fillHost.SizeChanged -= OnHostSizeChanged;
            fillHost = null;
        }

        private void AttachHostWindow()
        {
            var window = Window.GetWindow(this);
            if (ReferenceEquals(hostWindow, window))
            {
                return;
            }

            DetachHostWindow();
            hostWindow = window;
            if (hostWindow != null)
            {
                hostWindow.SizeChanged += OnHostSizeChanged;
            }
        }

        private void DetachHostWindow()
        {
            if (hostWindow == null)
            {
                return;
            }

            hostWindow.SizeChanged -= OnHostSizeChanged;
            hostWindow = null;
        }

        private void AttachHostScrollViewer()
        {
            // Only the Playnite/window host ScrollViewer is disabled so the tester can
            // fill the viewport. Tab content ScrollViewers (General test, Options, …)
            // are descendants of this view and must stay enabled.
            var scrollViewer = FindAncestorScrollViewer();
            if (ReferenceEquals(hostScrollViewer, scrollViewer))
            {
                return;
            }

            DetachHostScrollViewer();
            hostScrollViewer = scrollViewer;
            if (hostScrollViewer == null)
            {
                return;
            }

            hostScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            hostScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            hostScrollViewer.SizeChanged += OnHostSizeChanged;
        }

        private void DetachHostScrollViewer()
        {
            if (hostScrollViewer == null)
            {
                return;
            }

            hostScrollViewer.SizeChanged -= OnHostSizeChanged;
            hostScrollViewer = null;
        }

        private void OnHostSizeChanged(object sender, SizeChangedEventArgs args)
        {
            ApplyHostViewportSize();
            StretchSelectedContent(this);
        }

        private void ApplyHostViewportSize()
        {
            double width = 0;
            double height = 0;

            if (fillHost != null)
            {
                width = fillHost.ActualWidth;
                height = fillHost.ActualHeight;
            }

            if ((width < 8 || height < 8) && hostScrollViewer != null)
            {
                if (width < 8)
                {
                    width = hostScrollViewer.ViewportWidth > 8
                        ? hostScrollViewer.ViewportWidth
                        : hostScrollViewer.ActualWidth;
                }

                if (height < 8)
                {
                    height = hostScrollViewer.ViewportHeight > 8
                        ? hostScrollViewer.ViewportHeight
                        : hostScrollViewer.ActualHeight;
                }
            }

            if (width < 8 || height < 8)
            {
                var slot = FindLargestAncestorSlot();
                if (width < 8 && slot.Width > 8)
                {
                    width = slot.Width;
                }

                if (height < 8 && slot.Height > 8)
                {
                    height = slot.Height;
                }
            }

            ApplySizeIfNeeded(width, height);
        }

        private void ApplySizeIfNeeded(double width, double height)
        {
            if (width > 8 && (double.IsNaN(Width) || Math.Abs(Width - width) > 1))
            {
                Width = width;
            }

            if (height > 8 && (double.IsNaN(Height) || Math.Abs(Height - height) > 1))
            {
                Height = height;
            }
        }

        private FrameworkElement FindFillHost()
        {
            FrameworkElement fallback = null;
            for (var parent = VisualTreeHelper.GetParent(this);
                 parent != null;
                 parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent is Window)
                {
                    break;
                }

                var element = parent as FrameworkElement;
                if (element == null)
                {
                    continue;
                }

                if (element.Name == "PART_ContentView")
                {
                    return element;
                }

                if (parent is ContentControl && fallback == null)
                {
                    fallback = element;
                }
            }

            return fallback;
        }

        private Size FindLargestAncestorSlot()
        {
            var best = new Size(0, 0);
            for (var parent = VisualTreeHelper.GetParent(this);
                 parent != null;
                 parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent is Window)
                {
                    break;
                }

                var element = parent as FrameworkElement;
                if (element == null)
                {
                    continue;
                }

                if (element.ActualWidth > best.Width)
                {
                    best.Width = element.ActualWidth;
                }

                if (element.ActualHeight > best.Height)
                {
                    best.Height = element.ActualHeight;
                }
            }

            return best;
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

        private bool IsInsideSettingsView()
        {
            for (var parent = VisualTreeHelper.GetParent(this);
                 parent != null;
                 parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent.GetType().Name == "ControllerSessionManagerSettingsView")
                {
                    return true;
                }
            }

            return false;
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
    }
}
