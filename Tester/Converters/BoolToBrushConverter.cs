using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Converters
{
    public sealed class BoolToBrushConverter : IValueConverter
    {
        public Brush FalseBrush { get; set; }
        public Brush TrueBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && (bool)value ? TrueBrush : FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
