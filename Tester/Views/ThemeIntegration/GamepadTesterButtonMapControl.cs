using ControllerSessionManager.Tester.Views.ControllerLayouts;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Views.ThemeIntegration
{
    public sealed class GamepadTesterButtonMapControl : GamepadTesterThemeControlBase
    {
        private readonly Button actionButton;

        public GamepadTesterButtonMapControl(GamepadTesterSettings settings, Func<string, string> localizer)
            : base(settings, localizer)
        {
            var panel = Panel();
            var root = new Grid
            {
                MinWidth = 420,
                MinHeight = 280
            };

            var content = new StackPanel();
            var host = new Grid
            {
                Width = 620,
                Height = 390,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            AddLayout(host, new DualSenseControllerTesterView(), "IsDualSenseLayout");
            AddLayout(host, new XboxSeriesControllerTesterView(), "IsXboxSeriesVisualScheme");
            AddLayout(host, new XboxControllerTesterView(), "IsXboxOneVisualScheme");
            AddLayout(host, new SteamControllerTesterView(), "IsSteamControllerVisualScheme");
            AddLayout(host, new PlayStationControllerTesterView(), "IsPlayStationVisualScheme");
            AddLayout(host, new SwitchProControllerTesterView(), "IsSwitchProVisualScheme");
            AddLayout(host, new EightBitDoUltimateTesterView(), "IsEightBitDoUltimateVisualScheme");
            AddLayout(host, new EightBitDoProTesterView(), "IsEightBitDoProVisualScheme");
            AddLayout(host, new UniversalControllerTesterView(), "IsUniversalControllerArtwork");

            content.Children.Add(new Viewbox
            {
                Stretch = Stretch.Uniform,
                MaxHeight = 285,
                Child = host
            });

            var actionArea = new Grid
            {
                MinHeight = 54,
                Margin = new Thickness(0, 12, 0, 0)
            };

            actionButton = new Button
            {
                Name = "GamepadTester_ButtonTestAction",
                MinWidth = 210,
                MinHeight = 48,
                Padding = new Thickness(18, 8, 18, 8),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            SetThemeResource(actionButton, Control.ForegroundProperty, TextBrushKey);
            SetThemeResource(actionButton, Control.BackgroundProperty, ButtonBackgroundBrushKey);
            SetThemeResource(actionButton, Control.BorderBrushProperty, ControlBorderBrushKey);
            actionButton.SetBinding(Button.CommandProperty, new Binding("StartButtonCaptureCommand"));
            actionButton.SetBinding(ContentControl.ContentProperty, new Binding("ButtonCaptureButtonLabel"));
            actionButton.SetBinding(VisibilityProperty, Bind("IsButtonCaptureRunning", InverseBoolToVisibility()));
            actionArea.Children.Add(actionButton);

            var exitHint = new Border
            {
                MinWidth = 310,
                MinHeight = 48,
                Padding = new Thickness(18, 8, 18, 8),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            SetThemeResource(exitHint, Border.BackgroundProperty, ButtonBackgroundBrushKey);
            SetThemeResource(exitHint, Border.BorderBrushProperty, ControlBorderBrushKey);
            exitHint.SetBinding(VisibilityProperty, Bind("IsButtonCaptureRunning", BoolToVisibility()));
            var exitHintText = Text(string.Empty, 13, FontWeights.SemiBold);
            exitHintText.HorizontalAlignment = HorizontalAlignment.Center;
            exitHintText.TextAlignment = TextAlignment.Center;
            exitHintText.SetBinding(TextBlock.TextProperty, new Binding("CaptureExitHintLabel"));
            exitHint.Child = exitHintText;
            actionArea.Children.Add(exitHint);
            content.Children.Add(actionArea);

            root.Children.Add(content);

            var noController = CreateEmptyState();
            noController.SetBinding(VisibilityProperty, Bind("HasController", InverseBoolToVisibility()));
            root.Children.Add(noController);

            panel.Child = root;
            Content = panel;

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            Unloaded += OnControlUnloaded;
        }

        public bool IsTestRunning
        {
            get { return ViewModel.IsButtonCaptureRunning; }
        }

        public void StopButtonCapture()
        {
            if (IsTestRunning && ViewModel.StartButtonCaptureCommand.CanExecute(null))
            {
                ViewModel.StartButtonCaptureCommand.Execute(null);
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "IsButtonCaptureRunning" && !IsTestRunning)
            {
                FocusActionButton();
            }
        }

        private void FocusActionButton()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsVisible && actionButton.IsEnabled)
                {
                    actionButton.Focus();
                    Keyboard.Focus(actionButton);
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        public void FocusStartButton()
        {
            FocusActionButton();
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs args)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            Unloaded -= OnControlUnloaded;
        }

        private static void AddLayout(Panel host, FrameworkElement layout, string visibilityPath)
        {
            layout.HorizontalAlignment = HorizontalAlignment.Center;
            layout.VerticalAlignment = VerticalAlignment.Center;
            layout.SetBinding(WidthProperty, Bind("ControllerVisualWidth"));
            layout.SetBinding(HeightProperty, Bind("ControllerVisualHeight"));
            layout.SetBinding(VisibilityProperty, Bind(visibilityPath, BoolToVisibility()));
            host.Children.Add(layout);
        }

        private Border CreateEmptyState()
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Opacity = 0.96,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            SetThemeResource(border, Border.BackgroundProperty, ControlBackgroundBrushKey);

            var message = Text(L("LOCCSM_NoControllers", "No controller connected"), 22, FontWeights.SemiBold);
            message.HorizontalAlignment = HorizontalAlignment.Center;
            message.VerticalAlignment = VerticalAlignment.Center;
            border.Child = message;

            return border;
        }
    }
}
