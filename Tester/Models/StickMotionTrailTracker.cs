using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ControllerSessionManager.Tester.Models
{
    public sealed class StickMotionTrailTracker
    {
        public static readonly TimeSpan Retention = TimeSpan.FromMilliseconds(1500);
        private const double Center = 140d;
        private const double PathRadius = 108d;
        private const double MinimumMovePixelsSquared = 0.64d;
        private const double RecentMaxAgeMilliseconds = 500d;
        private const double MidMaxAgeMilliseconds = 1000d;
        private readonly List<TrailSample> samples = new List<TrailSample>();

        public Geometry RecentGeometry { get; private set; }
        public Geometry MidGeometry { get; private set; }
        public Geometry FadeGeometry { get; private set; }
        public int SampleCount { get { return samples.Count; } }

        public StickMotionTrailTracker()
        {
            ClearGeometries();
        }

        public void Update(StickState stick, DateTime timestamp)
        {
            Expire(timestamp);
            TryAppend(stick, timestamp);
            Rebuild(timestamp);
        }

        public void Reset()
        {
            samples.Clear();
            ClearGeometries();
        }

        private void TryAppend(StickState stick, DateTime timestamp)
        {
            if (stick == null)
            {
                return;
            }

            var position = new Point(
                Center + (stick.X * PathRadius),
                Center - (stick.Y * PathRadius));

            if (samples.Count > 0)
            {
                var last = samples[samples.Count - 1].Position;
                var deltaX = position.X - last.X;
                var deltaY = position.Y - last.Y;
                if ((deltaX * deltaX) + (deltaY * deltaY) < MinimumMovePixelsSquared)
                {
                    return;
                }
            }
            else if (stick.Magnitude < 0.02f)
            {
                return;
            }

            samples.Add(new TrailSample
            {
                Position = position,
                Timestamp = timestamp
            });
        }

        private void Expire(DateTime timestamp)
        {
            var cutoff = timestamp - Retention;
            var removeCount = 0;
            while (removeCount < samples.Count && samples[removeCount].Timestamp < cutoff)
            {
                removeCount++;
            }

            if (removeCount > 0)
            {
                samples.RemoveRange(0, removeCount);
            }
        }

        private void Rebuild(DateTime timestamp)
        {
            FadeGeometry = BuildGeometry(timestamp, MidMaxAgeMilliseconds, Retention.TotalMilliseconds);
            MidGeometry = BuildGeometry(timestamp, RecentMaxAgeMilliseconds, MidMaxAgeMilliseconds);
            RecentGeometry = BuildGeometry(timestamp, 0d, RecentMaxAgeMilliseconds);
        }

        private Geometry BuildGeometry(DateTime timestamp, double minAgeMilliseconds, double maxAgeMilliseconds)
        {
            var points = new List<Point>(samples.Count);
            for (var index = 0; index < samples.Count; index++)
            {
                var age = (timestamp - samples[index].Timestamp).TotalMilliseconds;
                if (age > maxAgeMilliseconds)
                {
                    continue;
                }

                if (age < minAgeMilliseconds)
                {
                    if (points.Count > 0)
                    {
                        points.Add(samples[index].Position);
                    }

                    break;
                }

                if (points.Count == 0 && index > 0)
                {
                    points.Add(samples[index - 1].Position);
                }

                points.Add(samples[index].Position);
            }

            if (points.Count < 2)
            {
                return Geometry.Empty;
            }

            var figure = new PathFigure
            {
                StartPoint = points[0],
                IsClosed = false,
                IsFilled = false
            };

            var segment = new PolyLineSegment();
            for (var index = 1; index < points.Count; index++)
            {
                segment.Points.Add(points[index]);
            }

            figure.Segments.Add(segment);
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            if (geometry.CanFreeze)
            {
                geometry.Freeze();
            }

            return geometry;
        }

        private void ClearGeometries()
        {
            RecentGeometry = Geometry.Empty;
            MidGeometry = Geometry.Empty;
            FadeGeometry = Geometry.Empty;
        }

        private struct TrailSample
        {
            public Point Position;
            public DateTime Timestamp;
        }
    }
}
