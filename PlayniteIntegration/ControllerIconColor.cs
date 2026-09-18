using System;
using System.Windows.Media;

namespace ControllerSessionManager.PlayniteIntegration
{
    internal static class ControllerIconColor
    {
        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(value.Trim());
                return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
            }
            catch
            {
                return null;
            }
        }

        public static Brush ToBrush(string value)
        {
            var hex = Normalize(value);
            if (hex == null)
            {
                return null;
            }

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(color);
                if (brush.CanFreeze)
                {
                    brush.Freeze();
                }

                return brush;
            }
            catch
            {
                return null;
            }
        }
    }
}
