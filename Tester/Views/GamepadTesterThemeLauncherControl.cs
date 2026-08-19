using Playnite.SDK.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Views
{
    public class GamepadTesterThemeLauncherControl : PluginUserControl
    {
        public GamepadTesterThemeLauncherControl(Action openTester, Func<string, string> localizer)
        {
            var title = Localize(localizer, "LOCCSM_Tester_PluginName", "Gamepad Tester");
            var openText = Localize(localizer, "LOCCSM_Tester_OpenGamepadTester", "Open Gamepad Tester");

            var button = new Button
            {
                Cursor = Cursors.Hand,
                Padding = new Thickness(14, 10, 14, 10),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                ToolTip = openText,
                Content = CreateContent(title)
            };
            button.Click += (sender, args) =>
            {
                if (openTester != null)
                {
                    openTester();
                }
            };

            Content = button;
        }

        private static string Localize(Func<string, string> localizer, string key, string fallback)
        {
            if (localizer == null)
            {
                return fallback;
            }

            var value = localizer(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }

        private static UIElement CreateContent(string title)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new Viewbox
            {
                Width = 24,
                Height = 24,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 10, 0),
                Child = CreateIcon()
            });

            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });

            return panel;
        }

        private static Canvas CreateIcon()
        {
            var canvas = new Canvas
            {
                Width = 24,
                Height = 24
            };

            canvas.Children.Add(new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M7.2 8.2H16.8C19.7 8.2 21.8 10.4 22.2 13.8L22.6 17.1C22.9 19.4 20.4 20.7 18.8 19L16.4 16.4H7.6L5.2 19C3.6 20.7 1.1 19.4 1.4 17.1L1.8 13.8C2.2 10.4 4.3 8.2 7.2 8.2Z"),
                Fill = Brushes.Transparent,
                Stroke = Brushes.White,
                StrokeThickness = 1.7,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            });

            canvas.Children.Add(new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M7 12V15M5.5 13.5H8.5M16.2 12.2H16.25M18.4 14.5H18.45"),
                Fill = Brushes.Transparent,
                Stroke = Brushes.White,
                StrokeThickness = 1.7,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            });

            return canvas;
        }

    }
}
