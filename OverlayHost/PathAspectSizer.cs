using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ControllerSessionManager.OverlayHost
{
    internal static class PathAspectSizer
    {
        public static void FitToMaxSize(Path path, double maxSize)
        {
            if (path == null)
            {
                return;
            }

            maxSize = Math.Max(1, maxSize);
            var data = path.Data;
            if (data == null)
            {
                path.Width = maxSize;
                path.Height = maxSize;
                return;
            }

            var bounds = GetTightBounds(data);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                path.Width = maxSize;
                path.Height = maxSize;
                return;
            }

            var aspect = bounds.Width / bounds.Height;
            if (aspect >= 1.0)
            {
                path.Width = maxSize;
                path.Height = maxSize / aspect;
            }
            else
            {
                path.Height = maxSize;
                path.Width = maxSize * aspect;
            }

            // Fill the fitted box so Bezier control-point padding in Geometry.Bounds
            // does not leave letterboxed empty bands inside the Path.
            path.Stretch = Stretch.Fill;
        }

        private static Rect GetTightBounds(Geometry data)
        {
            try
            {
                var flattened = data.GetFlattenedPathGeometry(0.25, ToleranceType.Absolute);
                if (flattened != null && !flattened.Bounds.IsEmpty &&
                    flattened.Bounds.Width > 0 && flattened.Bounds.Height > 0)
                {
                    return flattened.Bounds;
                }
            }
            catch
            {
            }

            return data.Bounds;
        }
    }
}
