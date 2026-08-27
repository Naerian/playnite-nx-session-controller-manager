using System;

namespace ControllerSessionManager.Overlay
{
    /// <summary>
    /// Formats overlay disconnect elapsed time with scaling units (s / m / h / d).
    /// </summary>
    internal static class DisconnectDurationFormatter
    {
        public static string Format(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            if (elapsed.TotalDays >= 1)
            {
                var days = (int)elapsed.TotalDays;
                return elapsed.Hours > 0
                    ? days + "d " + elapsed.Hours + "h"
                    : days + "d";
            }

            if (elapsed.TotalHours >= 1)
            {
                var hours = (int)elapsed.TotalHours;
                return elapsed.Minutes > 0
                    ? hours + "h " + elapsed.Minutes + "m"
                    : hours + "h";
            }

            if (elapsed.TotalMinutes >= 1)
            {
                var minutes = (int)elapsed.TotalMinutes;
                return elapsed.Seconds > 0
                    ? minutes + "m " + elapsed.Seconds + "s"
                    : minutes + "m";
            }

            return Math.Max(0, (int)elapsed.TotalSeconds) + "s";
        }
    }
}
