using System;
using System.Windows;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Models
{
    public sealed class StickDiagnosticsTracker
    {
        public const int MaximumSamples = 1800;
        private const int BucketCount = 72;
        private const int MaxPathPoints = 420;
        private const double Threshold = 0.85d;
        private const double Center = 140d;
        private const double PathRadius = 108d;
        private const double InnerRingRadius = 116d;
        private const double OuterRingRadius = 128d;
        private readonly double[] bucketMaxima;
        private readonly bool[] exploredBuckets;
        private double magnitudeSum;
        private bool hasSamples;

        public StickDiagnosticsTracker()
        {
            bucketMaxima = new double[BucketCount];
            exploredBuckets = new bool[BucketCount];
            PathPoints = new PointCollection();
            PathGeometry = Geometry.Empty;
            CoverageGeometry = Geometry.Empty;
        }

        public PointCollection PathPoints { get; private set; }
        public Geometry PathGeometry { get; private set; }
        public Geometry CoverageGeometry { get; private set; }
        public int CoveragePercent { get; private set; }
        public int CoveredSectors { get; private set; }
        public int ExploredSectors { get; private set; }
        public double MaxMagnitude { get; private set; }
        public double AverageMagnitude { get; private set; }
        public int SampleCount { get; private set; }
        public bool HasReachedSamplingLimit { get { return SampleCount >= MaximumSamples; } }
        public double MinX { get; private set; }
        public double MaxX { get; private set; }
        public double MinY { get; private set; }
        public double MaxY { get; private set; }

        public void AddSample(StickState stick)
        {
            if (HasReachedSamplingLimit)
            {
                return;
            }

            var magnitude = Math.Min(1d, stick.Magnitude);
            MaxMagnitude = Math.Max(MaxMagnitude, magnitude);
            SampleCount++;
            magnitudeSum += magnitude;
            AverageMagnitude = magnitudeSum / SampleCount;
            UpdateAxisRange(stick);

            AppendPathPoint(stick);
            UpdateCircularCoverage(stick, magnitude);
        }

        public void Reset()
        {
            Array.Clear(bucketMaxima, 0, bucketMaxima.Length);
            Array.Clear(exploredBuckets, 0, exploredBuckets.Length);
            PathPoints = new PointCollection();
            PathGeometry = Geometry.Empty;
            CoverageGeometry = Geometry.Empty;
            CoveragePercent = 0;
            CoveredSectors = 0;
            ExploredSectors = 0;
            MaxMagnitude = 0d;
            AverageMagnitude = 0d;
            SampleCount = 0;
            magnitudeSum = 0d;
            hasSamples = false;
            MinX = 0d;
            MaxX = 0d;
            MinY = 0d;
            MaxY = 0d;
        }

        private void AppendPathPoint(StickState stick)
        {
            PathPoints.Add(new Point(
                Center + (stick.X * PathRadius),
                Center - (stick.Y * PathRadius)));

            while (PathPoints.Count > MaxPathPoints)
            {
                PathPoints.RemoveAt(0);
            }

            RebuildPathGeometry();
        }

        private void UpdateAxisRange(StickState stick)
        {
            if (!hasSamples)
            {
                MinX = stick.X;
                MaxX = stick.X;
                MinY = stick.Y;
                MaxY = stick.Y;
                hasSamples = true;
                return;
            }

            MinX = Math.Min(MinX, stick.X);
            MaxX = Math.Max(MaxX, stick.X);
            MinY = Math.Min(MinY, stick.Y);
            MaxY = Math.Max(MaxY, stick.Y);
        }

        private void RebuildPathGeometry()
        {
            if (PathPoints.Count < 2)
            {
                PathGeometry = Geometry.Empty;
                return;
            }

            var figure = new PathFigure
            {
                StartPoint = PathPoints[0],
                IsClosed = false,
                IsFilled = false
            };

            var segment = new PolyLineSegment();
            for (var index = 1; index < PathPoints.Count; index++)
            {
                segment.Points.Add(PathPoints[index]);
            }

            figure.Segments.Add(segment);
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            PathGeometry = geometry;
        }

        private void UpdateCircularCoverage(StickState stick, double magnitude)
        {
            if (magnitude < 0.08d)
            {
                return;
            }

            var angle = Math.Atan2(stick.Y, stick.X);
            if (angle < 0d)
            {
                angle += Math.PI * 2d;
            }

            var bucket = Math.Min(BucketCount - 1, (int)Math.Floor(angle / (Math.PI * 2d) * BucketCount));
            if (magnitude >= 0.25d && !exploredBuckets[bucket])
            {
                exploredBuckets[bucket] = true;
                ExploredSectors++;
            }

            if (magnitude > bucketMaxima[bucket])
            {
                bucketMaxima[bucket] = magnitude;
                RebuildCoverageGeometry();
            }
        }

        private void RebuildCoverageGeometry()
        {
            var group = new GeometryGroup();
            var covered = 0;

            for (var index = 0; index < BucketCount; index++)
            {
                if (bucketMaxima[index] < Threshold)
                {
                    continue;
                }

                covered++;
                group.Children.Add(CreateSectorGeometry(index));
            }

            CoveredSectors = covered;
            CoveragePercent = (int)Math.Round(covered * 100d / BucketCount);
            CoverageGeometry = group;
        }

        private static Geometry CreateSectorGeometry(int index)
        {
            var startAngle = ((Math.PI * 2d) / BucketCount) * index;
            var endAngle = ((Math.PI * 2d) / BucketCount) * (index + 1);
            var startOuter = PointFromAngle(startAngle, OuterRingRadius);
            var endOuter = PointFromAngle(endAngle, OuterRingRadius);
            var startInner = PointFromAngle(startAngle, InnerRingRadius);
            var endInner = PointFromAngle(endAngle, InnerRingRadius);

            var figure = new PathFigure
            {
                StartPoint = startOuter,
                IsClosed = true,
                IsFilled = true
            };

            figure.Segments.Add(new ArcSegment(endOuter, new Size(OuterRingRadius, OuterRingRadius), 0d, false, SweepDirection.Counterclockwise, true));
            figure.Segments.Add(new LineSegment(endInner, true));
            figure.Segments.Add(new ArcSegment(startInner, new Size(InnerRingRadius, InnerRingRadius), 0d, false, SweepDirection.Clockwise, true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }

        private static Point PointFromAngle(double angle, double radius)
        {
            return new Point(
                Center + (Math.Cos(angle) * radius),
                Center - (Math.Sin(angle) * radius));
        }
    }
}
