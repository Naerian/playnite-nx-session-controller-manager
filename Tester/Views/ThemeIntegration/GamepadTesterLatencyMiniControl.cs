using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Views.ThemeIntegration
{
    public sealed class GamepadTesterLatencyMiniControl : GamepadTesterThemeControlBase
    {
        private readonly Button actionButton;
        private readonly Border captureHint;

        public GamepadTesterLatencyMiniControl(GamepadTesterSettings settings, Func<string, string> localizer)
            : base(settings, localizer)
        {
            var panel = Panel();
            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition());
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new StackPanel();
            var header = Text(L("LOCCSM_Tester_LatencyTitle", "Polling latency"), 14, FontWeights.SemiBold);
            header.Margin = new Thickness(0, 0, 0, 8);
            info.Children.Add(header);

            var rate = Text(string.Empty, 40, FontWeights.Bold);
            rate.SetBinding(TextBlock.TextProperty, Bind("PollingRateCurrentLabel"));
            info.Children.Add(rate);

            var stats = Text(string.Empty, 12, FontWeights.Normal);
            stats.Opacity = 0.72;
            stats.SetBinding(TextBlock.TextProperty, Bind("PollingRateAverageValueLabel"));
            info.Children.Add(stats);

            var samples = Text(string.Empty, 12, FontWeights.Normal);
            samples.Opacity = 0.72;
            samples.SetBinding(TextBlock.TextProperty, Bind("LatencySampleCountLabel"));
            info.Children.Add(samples);

            root.Children.Add(info);

            actionButton = new Button
            {
                MinWidth = 140,
                MinHeight = 48,
                Margin = new Thickness(18, 0, 0, 0),
                Padding = new Thickness(14, 8, 14, 8),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center
            };
            SetThemeResource(actionButton, Control.ForegroundProperty, TextBrushKey);
            SetThemeResource(actionButton, Control.BackgroundProperty, ButtonBackgroundBrushKey);
            SetThemeResource(actionButton, Control.BorderBrushProperty, ControlBorderBrushKey);
            actionButton.SetBinding(Button.CommandProperty, new Binding("StartLatencyTestCommand"));
            actionButton.SetBinding(ContentControl.ContentProperty, new Binding("StartLatencyButtonLabel"));
            actionButton.SetBinding(VisibilityProperty, Bind("IsLatencyTestRunning", InverseBoolToVisibility()));
            Grid.SetColumn(actionButton, 1);
            root.Children.Add(actionButton);

            this.captureHint = new Border
            {
                MinWidth = 230,
                MinHeight = 48,
                Margin = new Thickness(18, 0, 0, 0),
                Padding = new Thickness(14, 8, 14, 8),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center
            };
            SetThemeResource(this.captureHint, Border.BackgroundProperty, ButtonBackgroundBrushKey);
            SetThemeResource(this.captureHint, Border.BorderBrushProperty, ControlBorderBrushKey);
            this.captureHint.SetBinding(VisibilityProperty, Bind("IsLatencyTestRunning", BoolToVisibility()));
            var captureHintText = Text(string.Empty, 12, FontWeights.SemiBold);
            captureHintText.TextAlignment = TextAlignment.Center;
            captureHintText.HorizontalAlignment = HorizontalAlignment.Center;
            captureHintText.SetBinding(TextBlock.TextProperty, Bind("CaptureExitHintLabel"));
            this.captureHint.Child = captureHintText;
            Grid.SetColumn(this.captureHint, 1);
            root.Children.Add(this.captureHint);

            panel.Child = root;
            Content = panel;

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            Unloaded += OnControlUnloaded;
            UpdateCaptureMode();
        }

        public bool IsTestRunning
        {
            get { return ViewModel.IsLatencyTestRunning; }
        }

        public void StopLatencyTest()
        {
            if (!IsTestRunning || !ViewModel.StartLatencyTestCommand.CanExecute(null))
            {
                return;
            }

            ViewModel.StartLatencyTestCommand.Execute(null);
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "IsLatencyTestRunning")
            {
                UpdateCaptureMode();
            }
        }

        private void UpdateCaptureMode()
        {
            KeyboardNavigation.SetDirectionalNavigation(
                this,
                IsTestRunning ? KeyboardNavigationMode.None : KeyboardNavigationMode.Continue);
            KeyboardNavigation.SetTabNavigation(
                this,
                IsTestRunning ? KeyboardNavigationMode.None : KeyboardNavigationMode.Continue);

            if (!IsTestRunning)
            {
                FocusActionButton();
            }
        }

        private void FocusActionButton()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsTestRunning && actionButton.IsEnabled)
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
    }
}
