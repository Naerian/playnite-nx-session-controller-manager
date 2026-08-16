using System;
using System.Collections.Generic;
using System.Linq;

namespace ControllerSessionManager.Controllers
{
    public sealed class DiagnosticEventEntry
    {
        public DateTime TimestampUtc { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
    }

    public sealed class DiagnosticEventBuffer
    {
        private readonly object sync = new object();
        private readonly Queue<DiagnosticEventEntry> entries = new Queue<DiagnosticEventEntry>();
        private readonly int capacity;

        public DiagnosticEventBuffer(int maximumEntries = 200)
        {
            capacity = Math.Max(20, maximumEntries);
        }

        public void Add(string category, string message)
        {
            lock (sync)
            {
                entries.Enqueue(new DiagnosticEventEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    Category = Normalize(category, "general"),
                    Message = Normalize(message, "no details")
                });
                while (entries.Count > capacity)
                {
                    entries.Dequeue();
                }
            }
        }

        public IReadOnlyList<DiagnosticEventEntry> Snapshot()
        {
            lock (sync)
            {
                return entries.Select(a => new DiagnosticEventEntry
                {
                    TimestampUtc = a.TimestampUtc,
                    Category = a.Category,
                    Message = a.Message
                }).ToList();
            }
        }

        private static string Normalize(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return normalized.Length <= 500 ? normalized : normalized.Substring(0, 500);
        }
    }
}
