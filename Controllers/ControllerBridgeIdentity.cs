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

        public static bool IsXInputWrapperPath(string path)
        {
            return GetXInputSlot(path).HasValue ||
                (!string.IsNullOrWhiteSpace(path) &&
                 path.IndexOf("&ig_", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static bool IsVolatileHardwareId(string hardwareId)
        {
            return !string.IsNullOrWhiteSpace(hardwareId) &&
                hardwareId.StartsWith("xinput:slot:", StringComparison.OrdinalIgnoreCase);
        }

        public static string PreferStableHardwareId(string candidate, string fallback)
        {
            if (IsVolatileHardwareId(candidate) && !IsVolatileHardwareId(fallback) &&
                !string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }

            return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
        }

        public static bool TryParseHardwareVidPid(string value, out ushort vendorId, out ushort productId)
        {
            vendorId = 0;
            productId = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var parts = value.Split(':');
            return parts.Length >= 3 &&
                string.Equals(parts[0], "hardware", StringComparison.OrdinalIgnoreCase) &&
                ushort.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out vendorId) &&
                ushort.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out productId) &&
                vendorId != 0 && productId != 0;
        }

        public static bool PathsReferToSameDevice(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }
            var first = TrimPathCollectionSuffix(Normalize(left));
            var second = TrimPathCollectionSuffix(Normalize(right));
            return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGetVidPid(string path, out ushort vendorId, out ushort productId)
        {
            vendorId = 0;
            productId = 0;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var normalized = Normalize(path);
            if (TryReadHexId(normalized, "VID_", out vendorId) &&
                TryReadHexId(normalized, "PID_", out productId))
            {
                return true;
            }

            return TryReadBluetoothHidId(normalized, "VID&", out vendorId) &&
                TryReadBluetoothHidId(normalized, "PID&", out productId);
        }

        private static bool TryReadBluetoothHidId(string value, string marker, out ushort id)
        {
            id = 0;
            var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            var start = index + marker.Length;
            var length = 0;
            while (start + length < value.Length && IsHexDigit(value[start + length]))
            {
                length++;
            }

            if (length < 4)
            {
                return false;
            }

            return ushort.TryParse(value.Substring(start + length - 4, 4),
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out id);
        }

        private static bool IsHexDigit(char value)
        {
            return (value >= '0' && value <= '9') ||
                (value >= 'A' && value <= 'F') ||
                (value >= 'a' && value <= 'f');
        }

        private static bool TryReadHexId(string value, string marker, out ushort id)
        {
            id = 0;
            var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0 || index + marker.Length + 4 > value.Length)
            {
                return false;
            }
            return ushort.TryParse(value.Substring(index + marker.Length, 4),
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out id);
        }
        private static string Normalize(string value)
        {
            return value.Trim().Replace('/', '\\').ToUpperInvariant();
        }

        private static string TrimPathCollectionSuffix(string value)
        {
            var normalized = value.Replace('#', '\\');
            if (normalized.EndsWith("\\KBD", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - 4);
            }
            var interfaceGuid = normalized.IndexOf("\\{4D1E55B2-F16F-11CF-88CB-001111000030}",
                StringComparison.OrdinalIgnoreCase);
            return interfaceGuid < 0 ? normalized : normalized.Substring(0, interfaceGuid);
        }
    }
}
