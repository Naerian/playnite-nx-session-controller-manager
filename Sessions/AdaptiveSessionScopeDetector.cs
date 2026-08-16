using System;
using System.Collections.Generic;
using System.Linq;
using ControllerSessionManager.Controllers;

namespace ControllerSessionManager.Sessions
{
    internal sealed class AdaptiveSessionScopeDetector
    {
        private readonly Dictionary<string, DateTime> lastObservedInput =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly List<InputSample> samples = new List<InputSample>();
        private DateTime sessionStartedUtc;

        public bool IsLocalMultiplayer { get; private set; }

        public void Reset(DateTime startedUtc)
        {
            lastObservedInput.Clear();
            samples.Clear();
            sessionStartedUtc = startedUtc;
            IsLocalMultiplayer = false;
        }

        public bool Observe(IEnumerable<ControllerDeviceSnapshot> controllers, DateTime nowUtc)
        {
            if (IsLocalMultiplayer)
            {
                return true;
            }

            foreach (var controller in (controllers ?? Enumerable.Empty<ControllerDeviceSnapshot>())
                .Where(a => a.IsConnected && a.LastInputUtc.HasValue &&
                    a.LastInputUtc.Value >= sessionStartedUtc)
                .GroupBy(GetKey, StringComparer.OrdinalIgnoreCase)
                .Select(a => a.OrderByDescending(b => b.LastInputUtc).First()))
            {
                var key = GetKey(controller);
                DateTime previous;
                if (lastObservedInput.TryGetValue(key, out previous) &&
                    controller.LastInputUtc.Value <= previous)
                {
                    continue;
                }

                lastObservedInput[key] = controller.LastInputUtc.Value;
                samples.Add(new InputSample { ControllerKey = key, TimestampUtc = controller.LastInputUtc.Value });
            }

            var cutoff = nowUtc - TimeSpan.FromSeconds(20);
            samples.RemoveAll(a => a.TimestampUtc < cutoff);
            var compact = Compact(samples.OrderBy(a => a.TimestampUtc));
            var participants = compact.GroupBy(a => a.ControllerKey, StringComparer.OrdinalIgnoreCase)
                .Where(a => a.Count() >= 2).Select(a => a.Key).ToList();
            if (participants.Count < 2)
            {
                return false;
            }

            var relevant = compact.Where(a => participants.Contains(a.ControllerKey,
                StringComparer.OrdinalIgnoreCase)).ToList();
            var transitions = 0;
            for (var index = 1; index < relevant.Count; index++)
            {
                if (!string.Equals(relevant[index - 1].ControllerKey, relevant[index].ControllerKey,
                    StringComparison.OrdinalIgnoreCase))
                {
                    transitions++;
                }
            }

            IsLocalMultiplayer = transitions >= 3;
            return IsLocalMultiplayer;
        }

        private static List<InputSample> Compact(IEnumerable<InputSample> source)
        {
            var result = new List<InputSample>();
            foreach (var sample in source)
            {
                var previous = result.LastOrDefault(a => string.Equals(a.ControllerKey,
                    sample.ControllerKey, StringComparison.OrdinalIgnoreCase));
                if (previous != null && sample.TimestampUtc - previous.TimestampUtc <
                    TimeSpan.FromMilliseconds(180))
                {
                    continue;
                }
                result.Add(sample);
            }
            return result;
        }

        private static string GetKey(ControllerDeviceSnapshot controller)
        {
            return string.IsNullOrWhiteSpace(controller.HardwareId)
                ? controller.ControllerId
                : controller.HardwareId;
        }

        private sealed class InputSample
        {
            public string ControllerKey { get; set; }
            public DateTime TimestampUtc { get; set; }
        }
    }
}
