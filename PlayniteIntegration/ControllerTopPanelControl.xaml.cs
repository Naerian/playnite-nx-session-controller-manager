using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Playnite.SDK.Controls;

namespace ControllerSessionManager.PlayniteIntegration
{
    public partial class ControllerTopPanelControl : PluginUserControl
    {
        private const double CompactTopPanelWidth = 58d;
        private readonly ControllerSessionManagerPlugin plugin;
        private FrameworkElement topPanelContainer;

        public static readonly DependencyProperty IsCompactTopPanelLayoutProperty = DependencyProperty.Register(
            "IsCompactTopPanelLayout", typeof(bool), typeof(ControllerTopPanelControl),
            new PropertyMetadata(false));

        public bool IsCompactTopPanelLayout
        {
            get { return (bool)GetValue(IsCompactTopPanelLayoutProperty); }
            private set { SetValue(IsCompactTopPanelLayoutProperty, value); }
        }

        public ControllerTopPanelControl(ControllerSessionManagerPlugin plugin)
        {
            this.plugin = plugin;
            InitializeComponent();
            DataContext = plugin.Theme;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            plugin.RefreshControllers();
            Dispatcher.BeginInvoke(new Action(AttachTopPanelContainer), DispatcherPriority.Loaded);
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            DetachTopPanelContainer();
        }

        private void AttachTopPanelContainer()
        {
            var container = FindTopPanelContainer(this);
            if (ReferenceEquals(topPanelContainer, container))
            {
                UpdateCompactLayout();
                return;
            }

            DetachTopPanelContainer();
            topPanelContainer = container;
            if (topPanelContainer != null)
            {
                topPanelContainer.SizeChanged += OnTopPanelContainerSizeChanged;
            }

            UpdateCompactLayout();
        }

        private void DetachTopPanelContainer()
        {
            if (topPanelContainer != null)
            {
                topPanelContainer.SizeChanged -= OnTopPanelContainerSizeChanged;
                topPanelContainer = null;
            }
        }

        private void OnTopPanelContainerSizeChanged(object sender, SizeChangedEventArgs args)
        {
            UpdateCompactLayout();
        }

        private void UpdateCompactLayout()
        {
            if (topPanelContainer == null)
            {
                IsCompactTopPanelLayout = false;
                return;
            }

            var availableWidth = !double.IsNaN(topPanelContainer.Width) && topPanelContainer.Width > 0
                ? topPanelContainer.Width
                : topPanelContainer.ActualWidth;
            IsCompactTopPanelLayout = availableWidth > 0 && availableWidth < CompactTopPanelWidth;
        }

        private static FrameworkElement FindTopPanelContainer(DependencyObject child)
        {
            var current = child;
            while (current != null)
            {
                var element = current as FrameworkElement;
                if (element != null && string.Equals(element.GetType().Name, "TopPanelItem",
                    StringComparison.Ordinal))
                {
                    return element;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
