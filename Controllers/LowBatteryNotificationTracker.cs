using System;
using System.Collections.Generic;
using System.Linq;

namespace ControllerSessionManager.Controllers
{
    /// <summary>
    /// One toast per low-battery episode. Clears only after two consecutive
    /// non-low samples (or disconnect) to survive a temporary HID read miss.
    /// </summary>
    public sealed class LowBatteryNotificationTracker
    {
        public const string ThresholdLow = "Low";
        public const string ThresholdEmpty = "Empty";
        private const int RecoverSamplesRequired = 2;

        private readonly Dictionary<string, Entry> entries =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public static string NormalizeThreshold(string value)
        {
            return string.Equals(value, ThresholdEmpty, StringComparison.OrdinalIgnoreCase)
                ? ThresholdEmpty
                : ThresholdLow;
        }

        public static bool IsAtOrBelowThreshold(string batteryLevel, string threshold)
        {
            if (string.IsNullOrWhiteSpace(batteryLevel) ||
                string.Equals(batteryLevel, "Unknown", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(batteryLevel, "Unavailable", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(batteryLevel, "Empty", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(NormalizeThreshold(threshold), ThresholdLow, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(batteryLevel, "Low", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Seeds currently-low controllers as already notified (startup / settings apply).
        /// </summary>
        public void SeedWithoutNotify(IEnumerable<string> lowControllerKeys)
        {
            if (lowControllerKeys == null)
            {
                return;
            }

            foreach (var key in lowControllerKeys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                entries[key] = new Entry { Latched = true };
            }
        }

        public void Clear()
        {
            entries.Clear();
        }

        public void RetainOnly(IEnumerable<string> connectedKeys)
        {
            var keep = new HashSet<string>(
                connectedKeys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var key in entries.Keys.ToList())
            {
                if (!keep.Contains(key))
                {
                    entries.Remove(key);
                }
            }
        }

        public bool ShouldShow(string controllerKey, string batteryLevel, string threshold, bool connected)
        {
            if (string.IsNullOrWhiteSpace(controllerKey))
            {
                return false;
            }

            if (!connected)
            {
                entries.Remove(controllerKey);
                return false;
            }

            var normalizedThreshold = NormalizeThreshold(threshold);
            Entry entry;
            if (!entries.TryGetValue(controllerKey, out entry))
            {
                entry = new Entry();
                entries[controllerKey] = entry;
            }

            if (IsAtOrBelowThreshold(batteryLevel, normalizedThreshold))
            {
                entry.RecoverSamples = 0;
                if (entry.Latched)
                {
                    return false;
                }

                entry.Latched = true;
                return true;
            }

            if (!entry.Latched)
            {
                return false;
            }

            entry.RecoverSamples++;
            if (entry.RecoverSamples >= RecoverSamplesRequired)
            {
                entry.Latched = false;
                entry.RecoverSamples = 0;
            }

            return false;
        }

        private sealed class Entry
        {
            public bool Latched { get; set; }
            public int RecoverSamples { get; set; }
        }
    }
}
