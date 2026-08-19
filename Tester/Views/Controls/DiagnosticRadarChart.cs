using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Views.Controls
{
    public sealed class DiagnosticRadarChart : FrameworkElement
    {
        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            "Values",
            typeof(IEnumerable),
            typeof(DiagnosticRadarChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
            "Labels",
            typeof(IEnumerable),
            typeof(DiagnosticRadarChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
            "AccentBrush",
            typeof(Brush),
            typeof(DiagnosticRadarChart),
            new FrameworkPropertyMetadata(Brushes.MediumSeaGreen, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register(
            "GridBrush",
            typeof(Brush),
            typeof(DiagnosticRadarChart),
            new FrameworkPropertyMetadata(Brushes.DimGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LabelBrushProperty = DependencyProperty.Register(
            "LabelBrush",
            typeof(Brush),
            typeof(DiagnosticRadarChart),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValueBrushProperty = DependencyProperty.Register(
            "ValueBrush",
            typeof(Brush),
            typeof(DiagnosticRadarChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public IEnumerable Values
        {
            get { return (IEnumerable)GetValue(ValuesProperty); }
            set { SetValue(ValuesProperty, value); }
        }

        public IEnumerable Labels
        {
            get { return (IEnumerable)GetValue(LabelsProperty); }
            set { SetValue(LabelsProperty, value); }
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

        public Brush ValueBrush
        {
            get { return (Brush)GetValue(ValueBrushProperty); }
            set { SetValue(ValueBrushProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var values = ReadValues();
            var labels = ReadLabels();
            var axisCount = Math.Min(values.Count, labels.Count);
            if (axisCount < 3 || ActualWidth < 240d || ActualHeight < 190d)
            {
                return;
            }

            values = values.Take(axisCount).ToList();
            labels = labels.Take(axisCount).ToList();
            var center = new Point(ActualWidth / 2d, (ActualHeight / 2d) + 4d);
            var radius = Math.Max(30d, Math.Min(ActualWidth, ActualHeight) / 2d - 54d);
            var gridColor = GetBrushColor(GridBrush, Colors.DimGray);
            var accentColor = GetBrushColor(AccentBrush, Color.FromRgb(49, 196, 141));

            DrawGrid(drawingContext, center, radius, axisCount, gridColor);
            DrawData(drawingContext, center, radius, values, accentColor);
            DrawLabels(drawingContext, center, radius, values, labels);
        }

        private void DrawGrid(DrawingContext drawingContext, Point center, double radius, int axisCount, Color color)
        {
            for (var level = 5; level >= 1; level--)
            {
                var opacity = level == 5 ? (byte)92 : (byte)50;
                var pen = new Pen(new SolidColorBrush(Color.FromArgb(opacity, color.R, color.G, color.B)), level == 5 ? 1.25d : 0.8d);
                drawingContext.DrawGeometry(
                    level % 2 == 0 ? new SolidColorBrush(Color.FromArgb(10, color.R, color.G, color.B)) : null,
                    pen,
                    CreatePolygon(center, radius * level / 5d, axisCount));
            }

            var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(48, color.R, color.G, color.B)), 0.8d);
            for (var index = 0; index < axisCount; index++)
            {
                drawingContext.DrawLine(axisPen, center, PointOnAxis(center, radius, index, axisCount));
            }
        }

        private void DrawData(DrawingContext drawingContext, Point center, double radius, IList<double> values, Color accentColor)
        {
            var points = new List<Point>(values.Count);
            for (var index = 0; index < values.Count; index++)
            {
                var normalized = Math.Max(0d, Math.Min(1d, values[index] / 100d));
                points.Add(PointOnAxis(center, radius * normalized, index, values.Count));
            }

            var geometry = CreatePolygon(points);
            var fill = new RadialGradientBrush(
                Color.FromArgb(22, accentColor.R, accentColor.G, accentColor.B),
                Color.FromArgb(88, accentColor.R, accentColor.G, accentColor.B));
            drawingContext.DrawGeometry(fill, new Pen(AccentBrush, 2.4d), geometry);

            foreach (var point in points)
            {
                drawingContext.DrawEllipse(
                    new SolidColorBrush(Color.FromArgb(48, accentColor.R, accentColor.G, accentColor.B)),
                    null,
                    point,
                    7d,
                    7d);
                drawingContext.DrawEllipse(AccentBrush, null, point, 3.5d, 3.5d);
            }
        }

        private void DrawLabels(DrawingContext drawingContext, Point center, double radius, IList<double> values, IList<string> labels)
        {
            for (var index = 0; index < labels.Count; index++)
            {
                var anchor = PointOnAxis(center, radius + 26d, index, labels.Count);
                var label = CreateLabel(labels[index]);
                label.MaxTextWidth = 112d;
                label.TextAlignment = TextAlignment.Center;
                var value = CreateValueLabel(string.Format(CultureInfo.CurrentCulture, "{0:0}%", values[index]));
                var directionX = anchor.X - center.X;
                var directionY = anchor.Y - center.Y;
                var x = Math.Abs(directionX) < 4d ? anchor.X - 56d : directionX < 0d ? anchor.X - 112d : anchor.X;
                var totalHeight = label.Height + value.Height + 2d;
                var y = Math.Abs(directionY) < 4d
                    ? anchor.Y - (totalHeight / 2d)
                    : directionY < 0d ? anchor.Y - totalHeight : anchor.Y;
                x = Math.Max(3d, Math.Min(ActualWidth - 115d, x));
                y = Math.Max(3d, Math.Min(ActualHeight - totalHeight - 3d, y));
                drawingContext.DrawText(label, new Point(x, y));
                var valueX = x + ((112d - value.WidthIncludingTrailingWhitespace) / 2d);
                drawingContext.DrawText(value, new Point(valueX, y + label.Height + 2d));
            }
        }

        private FormattedText CreateLabel(string text)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                12d,
                LabelBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private FormattedText CreateValueLabel(string text)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                12d,
                ValueBrush ?? AccentBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private List<double> ReadValues()
        {
            return Values == null
                ? new List<double>()
                : Values.Cast<object>()
                    .Select(value => Convert.ToDouble(value, CultureInfo.InvariantCulture))
                    .Where(value => !double.IsNaN(value) && !double.IsInfinity(value))
                    .ToList();
        }

        private List<string> ReadLabels()
        {
            return Labels == null
                ? new List<string>()
                : Labels.Cast<object>().Select(value => Convert.ToString(value, CultureInfo.CurrentCulture)).ToList();
        }

        private static Geometry CreatePolygon(Point center, double radius, int count)
        {
            return CreatePolygon(Enumerable.Range(0, count).Select(index => PointOnAxis(center, radius, index, count)).ToList());
        }

        private static Geometry CreatePolygon(IList<Point> points)
        {
            var figure = new PathFigure
            {
                StartPoint = points[0],
                IsClosed = true,
                IsFilled = true
            };

            for (var index = 1; index < points.Count; index++)
            {
                figure.Segments.Add(new LineSegment(points[index], true));
            }

            return new PathGeometry(new[] { figure });
        }

        private static Point PointOnAxis(Point center, double radius, int index, int count)
        {
            var angle = (-Math.PI / 2d) + (Math.PI * 2d * index / count);
            return new Point(center.X + (Math.Cos(angle) * radius), center.Y + (Math.Sin(angle) * radius));
        }

        private static Color GetBrushColor(Brush brush, Color fallback)
        {
            var solid = brush as SolidColorBrush;
            return solid == null ? fallback : solid.Color;
        }
    }
}
