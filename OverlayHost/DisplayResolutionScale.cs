using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace ControllerSessionManager.OverlayHost
{
    /// <summary>
    /// Scales Fullscreen toasts and the disconnect overlay against logical (DIP) display
    /// height so 4K @ 100% DPI grows ~2× while 4K @ 200% stays ~1× (Windows already enlarged DIPs).
    /// </summary>
    internal static class DisplayResolutionScale
    {
        public const double BaselineDipHeight = 1080.0;
        public const double MinimumScale = 0.85;
        public const double MaximumScale = 2.5;

        public static double FromPhysicalHeight(int physicalHeightPixels, double dpiScaleY)
        {
            if (physicalHeightPixels <= 0)
            {
                return 1.0;
            }

            if (dpiScaleY <= 0.0)
            {
                dpiScaleY = 1.0;
            }

            var dipHeight = physicalHeightPixels / dpiScaleY;
            return Clamp(dipHeight / BaselineDipHeight, MinimumScale, MaximumScale);
        }

        public static double FromScreen(Forms.Screen screen, double dpiScaleY)
        {
            var height = (screen ?? Forms.Screen.PrimaryScreen).Bounds.Height;
            return FromPhysicalHeight(height, dpiScaleY);
        }

        public static double GetDpiScaleY(Visual visual)
        {
            if (visual != null)
            {
                var source = PresentationSource.FromVisual(visual);
                if (source != null && source.CompositionTarget != null)
                {
                    return source.CompositionTarget.TransformToDevice.M22;
                }

                var window = visual as Window;
                if (window != null)
                {
                    var helper = new WindowInteropHelper(window);
                    if (helper.Handle == IntPtr.Zero)
                    {
                        helper.EnsureHandle();
                    }

                    var hwndSource = HwndSource.FromHwnd(helper.Handle);
                    if (hwndSource != null && hwndSource.CompositionTarget != null)
                    {
                        return hwndSource.CompositionTarget.TransformToDevice.M22;
                    }
                }
            }

            try
            {
                using (var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    return graphics.DpiY / 96.0;
                }
            }
            catch
            {
                return 1.0;
            }
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }
    }
}
