using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ControllerSessionManager.PlayniteIntegration;

namespace ControllerSessionManager.Tester.Converters
{
    /// <summary>
    /// Converts a hex icon color to a brush, falling back to theme TextBrush when empty.
    /// </summary>
    public sealed class OptionalHexBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var brush = ControllerIconColor.ToBrush(value as string);
            if (brush != null)
            {
                return brush;
            }

            if (Application.Current != null)
            {
                var theme = Application.Current.TryFindResource("TextBrush") as Brush;
                if (theme != null)
                {
                    return theme;
                }
            }

            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
