using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class NotificationFontDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var name = NotificationFontCatalog.Normalize(value as string);
            if (name.StartsWith("ExternalFont|", StringComparison.Ordinal))
            {
                return NotificationFontCatalog.DisplayName(name);
            }
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

    public sealed class OptionalGradientBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var start = ParseColor(values != null && values.Length > 1 ? values[1] : null,
                Color.FromArgb(235, 18, 20, 24));
            var enabled = values != null && values.Length > 0 && values[0] is bool && (bool)values[0];
            if (!enabled)
            {
                return new SolidColorBrush(start);
            }
            var end = ParseColor(values.Length > 2 ? values[2] : null, start);
            var angle = 0.0;
            if (values.Length > 3)
            {
                double.TryParse(System.Convert.ToString(values[3], CultureInfo.InvariantCulture),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out angle);
            }
            return new LinearGradientBrush(start, end, angle);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static Color ParseColor(object value, Color fallback)
        {
            try { return (Color)ColorConverter.ConvertFromString(value as string); }
            catch { return fallback; }
        }
    }

    public sealed class OptionalBorderGradientBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var enabled = values != null && values.Length > 0 && values[0] is bool && (bool)values[0];
            var fallback = ParseColor(values != null && values.Length > 4 ? values[4] : null,
                Color.FromRgb(35, 145, 255));
            if (!enabled)
            {
                return new SolidColorBrush(fallback);
            }

            var start = ParseColor(values.Length > 1 ? values[1] : null, fallback);
            var end = ParseColor(values.Length > 2 ? values[2] : null, fallback);
            double angle;
            if (!double.TryParse(System.Convert.ToString(values.Length > 3 ? values[3] : 45,
                CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out angle))
            {
                angle = 45;
            }
            return new LinearGradientBrush(start, end, angle);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static Color ParseColor(object value, Color fallback)
        {
            try { return (Color)ColorConverter.ConvertFromString(value as string); }
            catch { return fallback; }
        }
    }

    public sealed class OptionalBorderGlowEffectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var enabled = values != null && values.Length > 0 && values[0] is bool && (bool)values[0];
            if (!enabled)
            {
                return null;
            }

            var color = Colors.Transparent;
            try { color = (Color)ColorConverter.ConvertFromString(values.Length > 1 ? values[1] as string : null); }
            catch { }
            var blur = ToDouble(values, 2, 16);
            var opacity = ToDouble(values, 3, 30) / 100.0;
            return new DropShadowEffect
            {
                BlurRadius = Math.Max(0, blur),
                ShadowDepth = 0,
                Direction = 0,
                Opacity = Math.Max(0, Math.Min(1, opacity)),
                Color = color
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static double ToDouble(object[] values, int index, double fallback)
        {
            double parsed;
            return values != null && values.Length > index && double.TryParse(
                System.Convert.ToString(values[index], CultureInfo.InvariantCulture),
                NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }
    }

    public sealed class OptionalImageBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var enabled = values != null && values.Length > 0 && values[0] is bool && (bool)values[0];
            var path = values != null && values.Length > 1 ? values[1] as string : null;
            if (!enabled || string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                return Brushes.Transparent;
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                var brush = new ImageBrush(bitmap)
                {
                    Stretch = ParseStretch(values.Length > 2 ? values[2] as string : null),
                    AlignmentX = ParseAlignmentX(values.Length > 3 ? values[3] as string : null),
                    AlignmentY = ParseAlignmentY(values.Length > 4 ? values[4] as string : null),
                    Opacity = ParsePercent(values.Length > 5 ? values[5] : null) / 100.0
                };
                return brush;
            }
            catch { return Brushes.Transparent; }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        { throw new NotSupportedException(); }

        private static Stretch ParseStretch(string value)
        {
            if (string.Equals(value, "Uniform", StringComparison.OrdinalIgnoreCase)) return Stretch.Uniform;
            if (string.Equals(value, "Fill", StringComparison.OrdinalIgnoreCase)) return Stretch.Fill;
            return Stretch.UniformToFill;
        }

        private static AlignmentX ParseAlignmentX(string value)
        {
            if (string.Equals(value, "Left", StringComparison.OrdinalIgnoreCase)) return AlignmentX.Left;
            if (string.Equals(value, "Right", StringComparison.OrdinalIgnoreCase)) return AlignmentX.Right;
            return AlignmentX.Center;
        }

        private static AlignmentY ParseAlignmentY(string value)
        {
            if (string.Equals(value, "Top", StringComparison.OrdinalIgnoreCase)) return AlignmentY.Top;
            if (string.Equals(value, "Bottom", StringComparison.OrdinalIgnoreCase)) return AlignmentY.Bottom;
            return AlignmentY.Center;
        }

        private static double ParsePercent(object value)
        {
            double parsed;
            return double.TryParse(System.Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                ? Math.Max(0, Math.Min(100, parsed)) : 100;
        }
    }

    public sealed class OptionalTintBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var enabled = values != null && values.Length > 0 && values[0] is bool && (bool)values[0];
            var path = values != null && values.Length > 1 ? values[1] as string : null;
            if (!enabled || string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                return Brushes.Transparent;
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(values.Length > 2 ? values[2] as string : null);
                double opacity;
                if (!double.TryParse(System.Convert.ToString(values.Length > 3 ? values[3] : null,
                    CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out opacity))
                    opacity = 0;
                color.A = (byte)Math.Round(255 * Math.Max(0, Math.Min(100, opacity)) / 100.0);
                return new SolidColorBrush(color);
            }
            catch { return Brushes.Transparent; }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        { throw new NotSupportedException(); }
    }

    public sealed class OptionalColorBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var enabled = values != null && values.Length > 0 && values[0] is bool && (bool)values[0];
            if (!enabled) return Brushes.Transparent;
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(values[1] as string)); }
            catch { return Brushes.Transparent; }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        { throw new NotSupportedException(); }
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

    public sealed class TextAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var alignment = value as string ?? "Center";
            if (alignment.Equals("Left", StringComparison.OrdinalIgnoreCase)) return TextAlignment.Left;
            if (alignment.Equals("Right", StringComparison.OrdinalIgnoreCase)) return TextAlignment.Right;
            return TextAlignment.Center;
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
