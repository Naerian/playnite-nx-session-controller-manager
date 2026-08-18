using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class IconGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var data = value as string;
            if (string.IsNullOrWhiteSpace(data))
            {
                return Geometry.Empty;
            }

            try
            {
                var geometry = Geometry.Parse(data);
                var bounds = geometry.Bounds;
                if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
                {
                    return geometry;
                }

                const double canvasSize = 24;
                const double contentSize = 20;
                var scale = Math.Min(contentSize / bounds.Width, contentSize / bounds.Height);
                var width = bounds.Width * scale;
                var height = bounds.Height * scale;
                var offsetX = ((canvasSize - width) / 2) - (bounds.X * scale);
                var offsetY = ((canvasSize - height) / 2) - (bounds.Y * scale);
                var normalized = geometry.Clone();
                normalized.Transform = new MatrixTransform(scale, 0, 0, scale, offsetX, offsetY);
                return normalized;
            }
            catch (Exception)
            {
                return Geometry.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
