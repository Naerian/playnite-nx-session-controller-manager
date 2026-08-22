using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Playnite.SDK.Controls;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class ControllerThemeControl : PluginUserControl
    {
        private static readonly IconGeometryConverter IconConverter = new IconGeometryConverter();

        public ControllerThemeControl(ControllerThemeApi api, string elementName)
        {
            DataContext = api;
            Focusable = false;
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;

            switch (elementName)
            {
                case "ControllerIcon":
                    Content = CreateControllerIcon(useTopPanelGeometry: false);
                    return;
                case "TopPanelIcon":
                    Content = CreateControllerIcon(useTopPanelGeometry: true);
                    return;
                case "ControllerBatteryText":
                    Content = CreateBatteryText();
                    return;
                case "ControllerBatteryDot":
                    Content = CreateBatteryDot();
                    return;
                case "ControllerCount":
                    Content = CreateBound("ConnectedCount");
                    return;
                case "PrimaryController":
                    Content = CreateBound("PrimaryControllerName");
                    return;
                default:
                    Content = CreateBound("StatusText");
                    return;
            }
        }

        private static TextBlock CreateBound(string path)
        {
            var text = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            text.SetBinding(TextBlock.TextProperty, new Binding(path));
            return text;
        }

        private FrameworkElement CreateBatteryText()
        {
            var text = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            text.SetBinding(TextBlock.TextProperty, new Binding("PrimaryControllerBatteryLabel"));
            text.SetBinding(TextBlock.ToolTipProperty, new Binding("PrimaryControllerTooltip"));
            text.SetBinding(TextBlock.ForegroundProperty, CreateBrushBinding(preferBatteryAlways: true));
            text.SetBinding(UIElement.VisibilityProperty, new Binding("HasPrimaryControllerBattery")
            {
                Converter = new BooleanToVisibilityConverter()
            });
            return text;
        }

        private FrameworkElement CreateBatteryDot()
        {
            var dot = new Ellipse
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            dot.SetBinding(Shape.FillProperty, new Binding("PrimaryControllerBatteryBrush"));
            dot.SetBinding(FrameworkElement.ToolTipProperty, new Binding("PrimaryControllerTooltip"));
            dot.SetBinding(UIElement.VisibilityProperty, new Binding("HasPrimaryControllerBattery")
            {
                Converter = new BooleanToVisibilityConverter()
            });
            return new Viewbox
            {
                Stretch = Stretch.Uniform,
                Child = dot,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }

        private FrameworkElement CreateControllerIcon(bool useTopPanelGeometry)
        {
            var icon = new Path
            {
                Stretch = Stretch.Uniform,
                StrokeThickness = 0.45,
                StrokeLineJoin = PenLineJoin.Round,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            icon.SetBinding(Path.DataProperty, new Binding(
                useTopPanelGeometry ? "TopPanelIconGeometry" : "PrimaryControllerIconGeometry")
            {
                Converter = IconConverter
            });

            var brushBinding = CreateBrushBinding(preferBatteryAlways: false);
            icon.SetBinding(Shape.FillProperty, brushBinding);
            icon.SetBinding(Shape.StrokeProperty, brushBinding);

            var viewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                Child = icon,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            viewbox.SetBinding(FrameworkElement.ToolTipProperty, new Binding("PrimaryControllerTooltip"));
            return viewbox;
        }

        private static BindingBase CreateBrushBinding(bool preferBatteryAlways)
        {
            var multi = new MultiBinding { Converter = new ThemeIconBrushConverter(preferBatteryAlways) };
            multi.Bindings.Add(new Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ContentControl), 1)
            });
            multi.Bindings.Add(new Binding("PrimaryControllerBatteryBrush"));
            multi.Bindings.Add(new Binding("PrimaryControllerIconBrush"));
            multi.Bindings.Add(new Binding("UsePrimaryControllerBatteryColor"));
            return multi;
        }

        private sealed class ThemeIconBrushConverter : IMultiValueConverter
        {
            private readonly bool preferBatteryAlways;

            public ThemeIconBrushConverter(bool preferBatteryAlways)
            {
                this.preferBatteryAlways = preferBatteryAlways;
            }

            public object Convert(object[] values, System.Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
            {
                var themeForeground = values != null && values.Length > 0 ? values[0] as Brush : null;
                var batteryBrush = values != null && values.Length > 1 ? values[1] as Brush : null;
                var iconBrush = values != null && values.Length > 2 ? values[2] as Brush : null;
                var useColor = values != null && values.Length > 3 && values[3] is bool && (bool)values[3];

                if (preferBatteryAlways && batteryBrush != null)
                {
                    return batteryBrush;
                }

                if (useColor && iconBrush != null)
                {
                    return iconBrush;
                }

                return themeForeground ?? Brushes.White;
            }

            public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter,
                System.Globalization.CultureInfo culture)
            {
                return null;
            }
        }
    }
}
