using System;
using System.Globalization;

namespace ControllerSessionManager.PlayniteIntegration
{
    internal static class ColorPickerMath
    {
        public static void RgbToHsv(byte red, byte green, byte blue,
            out double hue, out double saturation, out double value)
        {
            var r = red / 255.0;
            var g = green / 255.0;
            var b = blue / 255.0;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;
            value = max;
            saturation = max <= 0 ? 0 : delta / max;
            if (delta <= 0.000001)
            {
                hue = 0;
                return;
            }

            if (max == r)
            {
                hue = 60.0 * (((g - b) / delta) % 6.0);
            }
            else if (max == g)
            {
                hue = 60.0 * (((b - r) / delta) + 2.0);
            }
            else
            {
                hue = 60.0 * (((r - g) / delta) + 4.0);
            }

            if (hue < 0)
            {
                hue += 360.0;
            }
        }

        public static void HsvToRgb(double hue, double saturation, double value,
            out byte red, out byte green, out byte blue)
        {
            if (hue < 0)
            {
                hue = 0;
            }
            else if (hue >= 360.0)
            {
                hue = 0;
            }

            saturation = Clamp01(saturation);
            value = Clamp01(value);
            var chroma = value * saturation;
            var hueSector = hue / 60.0;
            var x = chroma * (1.0 - Math.Abs((hueSector % 2.0) - 1.0));
            double r1;
            double g1;
            double b1;
            if (hueSector < 1.0)
            {
                r1 = chroma;
                g1 = x;
                b1 = 0;
            }
            else if (hueSector < 2.0)
            {
                r1 = x;
                g1 = chroma;
                b1 = 0;
            }
            else if (hueSector < 3.0)
            {
                r1 = 0;
                g1 = chroma;
                b1 = x;
            }
            else if (hueSector < 4.0)
            {
                r1 = 0;
                g1 = x;
                b1 = chroma;
            }
            else if (hueSector < 5.0)
            {
                r1 = x;
                g1 = 0;
                b1 = chroma;
            }
            else
            {
                r1 = chroma;
                g1 = 0;
                b1 = x;
            }

            var match = value - chroma;
            red = ToByte(r1 + match);
            green = ToByte(g1 + match);
            blue = ToByte(b1 + match);
        }

        public static int AlphaToPercent(byte alpha)
        {
            return (int)Math.Round(alpha * 100.0 / 255.0);
        }

        public static byte PercentToAlpha(int percent)
        {
            if (percent < 0)
            {
                percent = 0;
            }
            else if (percent > 100)
            {
                percent = 100;
            }

            return (byte)Math.Round(percent * 255.0 / 100.0);
        }

        public static string ToHex(byte alpha, byte red, byte green, byte blue)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", alpha, red, green, blue);
        }

        public static bool TryParseHex(string value, out byte alpha, out byte red, out byte green,
            out byte blue)
        {
            alpha = 255;
            red = 0;
            green = 0;
            blue = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var hex = value.Trim();
            if (hex[0] == '#')
            {
                hex = hex.Substring(1);
            }

            if (hex.Length != 6 && hex.Length != 8)
            {
                return false;
            }

            uint packed;
            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out packed))
            {
                return false;
            }

            if (hex.Length == 6)
            {
                red = (byte)((packed >> 16) & 0xFF);
                green = (byte)((packed >> 8) & 0xFF);
                blue = (byte)(packed & 0xFF);
                return true;
            }

            alpha = (byte)((packed >> 24) & 0xFF);
            red = (byte)((packed >> 16) & 0xFF);
            green = (byte)((packed >> 8) & 0xFF);
            blue = (byte)(packed & 0xFF);
            return true;
        }

        private static double Clamp01(double value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 1 ? 1 : value;
        }

        private static byte ToByte(double value)
        {
            var rounded = (int)Math.Round(value * 255.0);
            if (rounded < 0)
            {
                return 0;
            }

            return rounded > 255 ? (byte)255 : (byte)rounded;
        }
    }
}
