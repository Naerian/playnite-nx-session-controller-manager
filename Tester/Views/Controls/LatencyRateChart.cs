using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Views.Controls
{
    public sealed class LatencyRateChart : FrameworkElement
    {
        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            "Values",
            typeof(IEnumerable),
            typeof(LatencyRateChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
            "AccentBrush",
            typeof(Brush),
            typeof(LatencyRateChart),
            new FrameworkPropertyMetadata(Brushes.MediumSeaGreen, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register(
            "GridBrush",
            typeof(Brush),
            typeof(LatencyRateChart),
            new FrameworkPropertyMetadata(Brushes.DimGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LabelBrushProperty = DependencyProperty.Register(
            "LabelBrush",
            typeof(Brush),
            typeof(LatencyRateChart),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PlotBackgroundBrushProperty = DependencyProperty.Register(
            "PlotBackgroundBrush",
            typeof(Brush),
            typeof(LatencyRateChart),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        public IEnumerable Values
        {
            get { return (IEnumerable)GetValue(ValuesProperty); }
            set { SetValue(ValuesProperty, value); }
        }

        public Brush AccentBrush
        {
            get { return (Brush)GetValue(AccentBrushProperty); }
            set { SetValue(AccentBrushProperty, value); }
        }

        public Brush GridBrush
        {
            get { return (Brush)GetValue(GridBrushProperty); }
            set { SetValue(GridBrushProperty, value); }
        }

        public Brush LabelBrush
        {
            get { return (Brush)GetValue(LabelBrushProperty); }
            set { SetValue(LabelBrushProperty, value); }
        }

        public Brush PlotBackgroundBrush
        {
            get { return (Brush)GetValue(PlotBackgroundBrushProperty); }
            set { SetValue(PlotBackgroundBrushProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (ActualWidth < 120d || ActualHeight < 48d)
            {
                return;
            }

            var plot = new Rect(54d, 12d, Math.Max(1d, ActualWidth - 70d), Math.Max(1d, ActualHeight - 42d));
            drawingContext.DrawRoundedRectangle(PlotBackgroundBrush, null, plot, 7d, 7d);

            var values = ReadValues();
            var scaleMaximum = GetScaleMaximum(values);
            DrawGrid(drawingContext, plot, scaleMaximum, values.Count);

            if (values.Count == 0)
            {
                return;
            }

            var points = CreatePoints(values, plot, scaleMaximum);
            var accentColor = GetBrushColor(AccentBrush, Color.FromRgb(49, 196, 141));
            var areaBrush = new LinearGradientBrush(
                Color.FromArgb(82, accentColor.R, accentColor.G, accentColor.B),
                Color.FromArgb(0, accentColor.R, accentColor.G, accentColor.B),
                new Point(0.5d, 0d),
                new Point(0.5d, 1d));

            drawingContext.PushClip(new RectangleGeometry(plot));
            if (points.Count > 1)
            {
                drawingContext.DrawGeometry(areaBrush, null, CreateSmoothGeometry(points, true, plot.Bottom));
                drawingContext.DrawGeometry(null, new Pen(AccentBrush, 2.8d), CreateSmoothGeometry(points, false, plot.Bottom));
            }

            var lastPoint = points[points.Count - 1];
            drawingContext.DrawEllipse(
                new SolidColorBrush(Color.FromArgb(54, accentColor.R, accentColor.G, accentColor.B)),
                null,
                lastPoint,
                8d,
                8d);
            drawingContext.DrawEllipse(AccentBrush, null, lastPoint, 4d, 4d);
            drawingContext.Pop();
        }

        private List<double> ReadValues()
        {
            if (Values == null)
            {
                return new List<double>();
            }

            return Values.Cast<object>()
                .Select(value => Convert.ToDouble(value, CultureInfo.InvariantCulture))
                .Where(value => value >= 0d && !double.IsNaN(value) && !double.IsInfinity(value))
                .ToList();
        }

        private void DrawGrid(DrawingContext drawingContext, Rect plot, double scaleMaximum, int sampleCount)
        {
            var gridPen = new Pen(GridBrush, 1d)
            {
                DashStyle = new DashStyle(new[] { 2d, 5d }, 0d)
            };

            for (var index = 0; index <= 4; index++)
            {
                var ratio = index / 4d;
                var y = plot.Top + (plot.Height * ratio);
                drawingContext.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));

                var value = scaleMaximum * (1d - ratio);
                var label = CreateLabel(value >= 10d ? string.Format("{0:0} Hz", value) : string.Format("{0:0.0} Hz", value));
                drawingContext.DrawText(label, new Point(plot.Left - label.WidthIncludingTrailingWhitespace - 9d, y - (label.Height / 2d)));
            }

            var verticalBrush = GridBrush.Clone();
            verticalBrush.Opacity *= 0.45d;
            var verticalPen = new Pen(verticalBrush, 0.8d);
            for (var index = 0; index <= 6; index++)
            {
                var x = plot.Left + (plot.Width * index / 6d);
                drawingContext.DrawLine(verticalPen, new Point(x, plot.Top), new Point(x, plot.Bottom));
            }

            if (sampleCount <= 0)
            {
                return;
            }

            DrawSampleLabel(drawingContext, "1", plot.Left, plot.Bottom + 8d, TextAlignment.Left);
            if (sampleCount > 2)
            {
                DrawSampleLabel(drawingContext, ((sampleCount + 1) / 2).ToString(CultureInfo.CurrentCulture), plot.Left + (plot.Width / 2d), plot.Bottom + 8d, TextAlignment.Center);
            }

            if (sampleCount > 1)
            {
                DrawSampleLabel(drawingContext, sampleCount.ToString(CultureInfo.CurrentCulture), plot.Right, plot.Bottom + 8d, TextAlignment.Right);
            }
        }

        private void DrawSampleLabel(DrawingContext drawingContext, string text, double x, double y, TextAlignment alignment)
        {
            var label = CreateLabel(text);
            if (alignment == TextAlignment.Center)
            {
                x -= label.WidthIncludingTrailingWhitespace / 2d;
            }
            else if (alignment == TextAlignment.Right)
            {
                x -= label.WidthIncludingTrailingWhitespace;
            }

            drawingContext.DrawText(label, new Point(x, y));
        }

        private FormattedText CreateLabel(string text)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10d,
                LabelBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private static List<Point> CreatePoints(IList<double> values, Rect plot, double scaleMaximum)
        {
            var points = new List<Point>(values.Count);
            var step = values.Count <= 1 ? 0d : plot.Width / (values.Count - 1d);
            for (var index = 0; index < values.Count; index++)
            {
                var normalized = Math.Max(0d, Math.Min(1d, values[index] / scaleMaximum));
                points.Add(new Point(plot.Left + (index * step), plot.Bottom - (normalized * plot.Height)));
            }

            return points;
        }

        private static Geometry CreateSmoothGeometry(IList<Point> points, bool closeToBaseline, double baseline)
        {
            var figure = new PathFigure
            {
                StartPoint = points[0],
                IsClosed = closeToBaseline,
                IsFilled = closeToBaseline
            };

            for (var index = 0; index < points.Count - 1; index++)
            {
                var previous = index == 0 ? points[index] : points[index - 1];
                var current = points[index];
                var next = points[index + 1];
                var following = index + 2 < points.Count ? points[index + 2] : next;
                var control1 = new Point(
                    current.X + ((next.X - previous.X) / 6d),
                    current.Y + ((next.Y - previous.Y) / 6d));
                var control2 = new Point(
                    next.X - ((following.X - current.X) / 6d),
                    next.Y - ((following.Y - current.Y) / 6d));
                figure.Segments.Add(new BezierSegment(control1, control2, next, true));
            }

            if (closeToBaseline)
            {
                figure.Segments.Add(new LineSegment(new Point(points[points.Count - 1].X, baseline), false));
                figure.Segments.Add(new LineSegment(new Point(points[0].X, baseline), false));
            }

            return new PathGeometry(new[] { figure });
        }

        private static double GetScaleMaximum(IList<double> values)
        {
            var maximum = values.Count == 0 ? 0d : values.Max();
            foreach (var candidate in new[] { 60d, 125d, 250d, 500d, 1000d, 2000d })
            {
                if (maximum <= candidate)
                {
                    return candidate;
                }
            }

            return Math.Ceiling(maximum / 500d) * 500d;
        }

        private static Color GetBrushColor(Brush brush, Color fallback)
        {
            var solid = brush as SolidColorBrush;
            return solid == null ? fallback : solid.Color;
        }
    }
}
