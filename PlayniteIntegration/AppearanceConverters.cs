using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class NotificationFontDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var name = NotificationFontCatalog.Normalize(value as string);
            if (name != NotificationFontCatalog.SystemDefault)
            {
                return name;
            }

            var localized = Application.Current == null
                ? null
                : Application.Current.TryFindResource("LOCCSM_FontSystemDefault") as string;
            return string.IsNullOrWhiteSpace(localized) ? "Playnite UI" : localized;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

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

    public sealed class NotificationFontFamilyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return NotificationFontCatalog.Resolve(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class NotificationFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return NotificationFontCatalog.ResolveWeight(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class NotificationTypefaceFamilyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return NotificationFontCatalog.Resolve(
                values != null && values.Length > 0 ? values[0] as string : null,
                values != null && values.Length > 1 ? values[1] as string : null);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class NotificationTypefaceWeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return NotificationFontCatalog.ResolveEffectiveWeight(
                values != null && values.Length > 0 ? values[0] as string : null,
                values != null && values.Length > 1 ? values[1] as string : null);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class BatteryBadgeBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var useState = values != null && values.Length > 0 && values[0] is bool && (bool)values[0];
            var value = values != null && values.Length > (useState ? 2 : 1)
                ? values[useState ? 2 : 1] as string : null;
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)); }
            catch { return Brushes.Transparent; }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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

    public sealed class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var left = value == null ? string.Empty : value.ToString();
            var right = parameter == null ? string.Empty : parameter.ToString();
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value)
            {
                return parameter == null ? string.Empty : parameter.ToString();
            }

            return Binding.DoNothing;
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
            if (!enabled)
            {
                return new Thickness(0);
            }

            var position = values != null && values.Length > 2 && values[2] != null
                ? values[2].ToString() : "Full";
            if (position == "Left") return new Thickness(number, 0, 0, 0);
            if (position == "Top") return new Thickness(0, number, 0, 0);
            if (position == "Right") return new Thickness(0, 0, number, 0);
            if (position == "Bottom") return new Thickness(0, 0, 0, number);
            return new Thickness(number);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class OverlayPositionHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var position = value as string ?? "Center";
            if (position.EndsWith("Left", StringComparison.OrdinalIgnoreCase)) return HorizontalAlignment.Left;
            if (position.EndsWith("Right", StringComparison.OrdinalIgnoreCase)) return HorizontalAlignment.Right;
            return HorizontalAlignment.Center;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class OverlayPositionVerticalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var position = value as string ?? "Center";
            if (position.StartsWith("Top", StringComparison.OrdinalIgnoreCase)) return VerticalAlignment.Top;
            if (position.StartsWith("Bottom", StringComparison.OrdinalIgnoreCase)) return VerticalAlignment.Bottom;
            return VerticalAlignment.Center;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = value is bool && (bool)value;
            return flag ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
