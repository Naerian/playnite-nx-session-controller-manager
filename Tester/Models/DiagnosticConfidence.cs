using System;

namespace ControllerSessionManager.Tester.Models
{
    public enum DiagnosticStage
    {
        NotEvaluated,
        Collecting,
        Ready
    }

    public enum DiagnosticConfidenceLevel
    {
        None,
        Low,
        Medium,
        High
    }

    public sealed class DiagnosticConfidence
    {
        public DiagnosticStage Stage { get; set; }
        public DiagnosticConfidenceLevel Level { get; set; }
        public int ProgressPercent { get; set; }
        public int SampleCount { get; set; }

        public bool IsReady
        {
            get { return Stage == DiagnosticStage.Ready; }
        }
    }

    public static class DiagnosticConfidenceEvaluator
    {
        public const int MinimumRestSamples = 120;
        public const int HighConfidenceRestSamples = 300;
        public const int MinimumLatencySamples = 20;
        public const int HighConfidenceLatencySamples = 60;
        public const int MinimumRangeSamples = 120;
        public const int MinimumExploredSectors = 36;
        public const int HighConfidenceExploredSectors = 60;

        public static DiagnosticConfidence ForHealth(bool connected, int restSamples)
        {
            if (!connected)
            {
                return NotEvaluated(restSamples);
            }

            return FromSingleRequirement(restSamples, MinimumRestSamples, HighConfidenceRestSamples);
        }

        public static DiagnosticConfidence ForLatency(bool started, int samples)
        {
            if (!started)
            {
                return NotEvaluated(samples);
            }

            return FromSingleRequirement(samples, MinimumLatencySamples, HighConfidenceLatencySamples);
        }

        public static DiagnosticConfidence ForStickRange(int samples, int exploredSectors)
        {
            if (samples <= 0 && exploredSectors <= 0)
            {
                return NotEvaluated(0);
            }

            var sampleProgress = Percent(samples, MinimumRangeSamples);
            var directionProgress = Percent(exploredSectors, MinimumExploredSectors);
            var progress = Math.Min(sampleProgress, directionProgress);
            var ready = samples >= MinimumRangeSamples && exploredSectors >= MinimumExploredSectors;

            return new DiagnosticConfidence
            {
                Stage = ready ? DiagnosticStage.Ready : DiagnosticStage.Collecting,
                Level = !ready
                    ? DiagnosticConfidenceLevel.Low
                    : exploredSectors >= HighConfidenceExploredSectors
                        ? DiagnosticConfidenceLevel.High
                        : DiagnosticConfidenceLevel.Medium,
                ProgressPercent = ready ? 100 : progress,
                SampleCount = samples
            };
        }

        private static DiagnosticConfidence FromSingleRequirement(int samples, int minimum, int highConfidence)
        {
            var ready = samples >= minimum;
            return new DiagnosticConfidence
            {
                Stage = ready ? DiagnosticStage.Ready : DiagnosticStage.Collecting,
                Level = !ready
                    ? DiagnosticConfidenceLevel.Low
                    : samples >= highConfidence
                        ? DiagnosticConfidenceLevel.High
                        : DiagnosticConfidenceLevel.Medium,
                ProgressPercent = ready ? 100 : Percent(samples, minimum),
                SampleCount = samples
            };
        }

        private static DiagnosticConfidence NotEvaluated(int samples)
        {
            return new DiagnosticConfidence
            {
                Stage = DiagnosticStage.NotEvaluated,
                Level = DiagnosticConfidenceLevel.None,
                ProgressPercent = 0,
                SampleCount = samples
            };
        }

        private static int Percent(int value, int target)
        {
            return Math.Max(0, Math.Min(100, (int)Math.Round(value * 100d / Math.Max(1, target))));
        }
    }
}
