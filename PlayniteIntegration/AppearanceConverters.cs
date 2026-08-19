using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class HexColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value as string));
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class PreviewScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var percentage = value is int ? (int)value : 100;
            var scale = Math.Max(0.8, Math.Min(1.4, percentage / 100.0));
            return new ScaleTransform(scale, scale);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class NumberToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var number = value is int ? (int)value : 0;
            var mode = parameter as string;
            if (string.Equals(mode, "Top", StringComparison.OrdinalIgnoreCase))
            {
                return new Thickness(0, number, 0, 0);
            }

            if (string.Equals(mode, "Right", StringComparison.OrdinalIgnoreCase))
            {
                return new Thickness(0, 0, number, 0);
            }

            return new Thickness(number);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class NumberToCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var number = value is int ? (int)value : 0;
            return new CornerRadius(number);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class EqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var left = value == null ? string.Empty : value.ToString();
            var right = parameter == null ? string.Empty : parameter.ToString();
            return string.Equals(left, right, StringComparison.Ordinal)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class OptionalBorderThicknessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var enabled = values != null && values.Length > 0 && values[0] is bool && (bool)values[0];
            var number = values != null && values.Length > 1 && values[1] is int ? (int)values[1] : 0;
            return new Thickness(enabled ? number : 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
