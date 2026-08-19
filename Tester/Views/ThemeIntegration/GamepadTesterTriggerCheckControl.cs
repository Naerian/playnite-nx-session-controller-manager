using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Views.ThemeIntegration
{
    public sealed class GamepadTesterTriggerCheckControl : GamepadTesterThemeControlBase
    {
        public GamepadTesterTriggerCheckControl(GamepadTesterSettings settings, Func<string, string> localizer)
            : base(settings, localizer)
        {
            var root = new StackPanel();
            var header = Text(L("LOCCSM_Tester_Triggers", "Triggers"), 14, FontWeights.SemiBold);
            header.Margin = new Thickness(0, 0, 0, 10);
            root.Children.Add(header);

            var meters = new Grid();
            meters.ColumnDefinitions.Add(new ColumnDefinition());
            meters.ColumnDefinitions.Add(new ColumnDefinition());

            var left = CreateMeter("LiveLeftTriggerLabel", "LiveLeftTriggerPercent");
            Grid.SetColumn(left, 0);
            meters.Children.Add(left);

            var right = CreateMeter("LiveRightTriggerLabel", "LiveRightTriggerPercent");
            right.Margin = new Thickness(20, 0, 0, 0);
            Grid.SetColumn(right, 1);
            meters.Children.Add(right);

            root.Children.Add(meters);
            Content = root;
        }

        private FrameworkElement CreateMeter(string labelPath, string valuePath)
        {
            var panel = new StackPanel();
            var label = Text(string.Empty, 13, FontWeights.SemiBold);
            label.SetBinding(TextBlock.TextProperty, Bind(labelPath));
            panel.Children.Add(label);

            var progress = new ProgressBar
            {
                Height = 8,
                Minimum = 0,
                Maximum = 100,
                Margin = new Thickness(0, 7, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(66, 190, 245)),
                BorderThickness = new Thickness(0)
            };
            SetThemeResource(progress, Control.BackgroundProperty, ButtonBackgroundBrushKey);
            progress.SetBinding(RangeBase.ValueProperty, new Binding(valuePath)
            {
                Mode = BindingMode.OneWay
            });
            panel.Children.Add(progress);
            return panel;
        }
    }
}
