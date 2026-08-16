using System;

namespace ControllerSessionManager.Controllers
{
    public static class ControllerBridgeIdentity
    {
        public static int? GetXInputSlot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var normalized = path.Trim().Replace('/', '\\').ToUpperInvariant();
            const string marker = "XINPUT#";
            var markerIndex = normalized.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return null;
            }

            markerIndex += marker.Length;
            int slot;
            return markerIndex < normalized.Length &&
                int.TryParse(normalized.Substring(markerIndex, 1), out slot) && slot >= 0 && slot <= 3
                ? (int?)slot
                : null;
        }
    }
}
