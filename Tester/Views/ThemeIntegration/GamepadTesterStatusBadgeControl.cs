using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Views.ThemeIntegration
{
    public sealed class GamepadTesterStatusBadgeControl : GamepadTesterThemeControlBase
    {
        public GamepadTesterStatusBadgeControl(GamepadTesterSettings settings, Func<string, string> localizer)
            : base(settings, localizer)
        {
            var panel = Panel();
            panel.Padding = new Thickness(12, 9, 12, 9);
            panel.HorizontalAlignment = HorizontalAlignment.Left;

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            row.Children.Add(new Viewbox
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 10, 0),
                Child = CreateIcon()
            });

            var textColumn = new StackPanel();
            var title = Text(L("LOCCSM_Tester_PluginName", "Gamepad Tester"), 13, FontWeights.SemiBold);
            var device = Text(string.Empty, 12, FontWeights.Normal);
            device.Opacity = 0.72;
            device.SetBinding(TextBlock.TextProperty, Bind("DeviceModelLabel"));
            textColumn.Children.Add(title);
            textColumn.Children.Add(device);

            row.Children.Add(textColumn);
            panel.Child = row;
            Content = panel;
        }

        private static Canvas CreateIcon()
        {
            var canvas = new Canvas { Width = 24, Height = 24 };
            var outline = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M7.2 8.2H16.8C19.7 8.2 21.8 10.4 22.2 13.8L22.6 17.1C22.9 19.4 20.4 20.7 18.8 19L16.4 16.4H7.6L5.2 19C3.6 20.7 1.1 19.4 1.4 17.1L1.8 13.8C2.2 10.4 4.3 8.2 7.2 8.2Z"),
                Fill = Brushes.Transparent,
                StrokeThickness = 1.7,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            SetThemeResource(outline, System.Windows.Shapes.Shape.StrokeProperty, TextBrushKey);
            canvas.Children.Add(outline);

            var details = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M7 12V15M5.5 13.5H8.5M16.2 12.2H16.25M18.4 14.5H18.45"),
                Fill = Brushes.Transparent,
                StrokeThickness = 1.7,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            SetThemeResource(details, System.Windows.Shapes.Shape.StrokeProperty, TextBrushKey);
            canvas.Children.Add(details);
            return canvas;
        }
    }
}
