using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ControllerSessionManager.Tester.Views
{
    public partial class GamepadTesterView : UserControl
    {
        private ScrollViewer hostScrollViewer;
        private FrameworkElement fillHost;
        private Window hostWindow;

        public GamepadTesterView()
        {
            InitializeComponent();
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSelfSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            StretchFillLayout();
            Dispatcher.BeginInvoke(new Action(StretchFillLayout), DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(StretchFillLayout), DispatcherPriority.ApplicationIdle);
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
        }

        private void TesterTabsSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(StretchFillLayout), DispatcherPriority.Loaded);
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
