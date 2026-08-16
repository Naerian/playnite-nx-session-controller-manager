using System;
using System.Collections.Generic;

namespace ControllerSessionManager.Controllers
{
    public sealed class PlayStationHidBatteryProvider : IControllerBatteryProvider
    {
        private const ushort SonyVendorId = 0x054C;
        private static readonly HashSet<ushort> DualSenseProductIds = new HashSet<ushort>
            { 0x0CE6, 0x0DF2 };
        private static readonly HashSet<ushort> DualShock4ProductIds = new HashSet<ushort>
            { 0x05C4, 0x09CC, 0x0BA0 };
        private readonly Dictionary<string, CachedReading> cache =
            new Dictionary<string, CachedReading>(StringComparer.OrdinalIgnoreCase);

        public string Id
        {
            get { return "PlayStationHID"; }
        }

        public bool Supports(ControllerMetadata controller)
        {
            return controller != null && controller.VendorId == SonyVendorId &&
                (DualSenseProductIds.Contains(controller.ProductId) ||
                 DualShock4ProductIds.Contains(controller.ProductId));
        }

        public bool TryGetBatteryLevel(ControllerMetadata controller, out string level)
        {
            level = null;
            if (!Supports(controller))
            {
                return false;
            }

            var key = controller.VendorId.ToString("X4") + ":" + controller.ProductId.ToString("X4");
            CachedReading cached;
            if (cache.TryGetValue(key, out cached) &&
                DateTime.UtcNow - cached.TimestampUtc < TimeSpan.FromSeconds(cached.Success ? 15 : 5))
            {
                level = cached.Level;
                return cached.Success;
            }

            byte[] report;
            var success = HidDiagnosticsService.TryReadInputReport(
                controller.VendorId, controller.ProductId, 120, out report) &&
                TryParseReport(controller.ProductId, report, out level);
            cache[key] = new CachedReading
            {
                TimestampUtc = DateTime.UtcNow,
                Success = success,
                Level = level
            };
            return success;
        }

        public static bool TryParseReport(ushort productId, byte[] report, out string level)
        {
            level = null;
            int capacity;
            if (DualSenseProductIds.Contains(productId))
            {
                if (!TryParseDualSense(report, out capacity))
                {
                    return false;
                }
            }
            else if (DualShock4ProductIds.Contains(productId))
            {
                if (!TryParseDualShock4(report, out capacity))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            level = ToLevel(capacity);
            return true;
        }

        private static bool TryParseDualSense(byte[] report, out int capacity)
        {
            capacity = 0;
            int statusOffset;
            if (report != null && report.Length == 64 && report[0] == 0x01)
            {
                statusOffset = 53;
            }
            else if (report != null && report.Length == 78 && report[0] == 0x31 && HasValidBluetoothCrc(report))
            {
                statusOffset = 54;
            }
            else
            {
                return false;
            }

            var batteryData = report[statusOffset] & 0x0F;
            var chargingStatus = (report[statusOffset] >> 4) & 0x0F;
            if (chargingStatus == 0 || chargingStatus == 1)
            {
                capacity = Math.Min(batteryData * 10 + 5, 100);
                return batteryData <= 10;
            }
            if (chargingStatus == 2)
            {
                capacity = 100;
                return true;
            }
            return false;
        }

        private static bool TryParseDualShock4(byte[] report, out int capacity)
        {
            capacity = 0;
            int statusOffset;
            if (report != null && report.Length == 64 && report[0] == 0x01)
            {
                statusOffset = 30;
            }
            else if (report != null && report.Length == 78 && report[0] == 0x11 && HasValidBluetoothCrc(report))
            {
                statusOffset = 32;
            }
            else
            {
                return false;
            }

            var batteryData = report[statusOffset] & 0x0F;
            var cableConnected = (report[statusOffset] & 0x10) != 0;
            if (batteryData <= 10)
            {
                capacity = batteryData == 10 ? 100 : batteryData * 10 + 5;
                return true;
            }
            if (cableConnected && batteryData == 11)
            {
                capacity = 100;
                return true;
            }
            return false;
        }

        private static bool HasValidBluetoothCrc(byte[] report)
        {
            if (report == null || report.Length < 8)
            {
                return false;
            }

            var expected = (uint)(report[report.Length - 4] |
                report[report.Length - 3] << 8 |
                report[report.Length - 2] << 16 |
                report[report.Length - 1] << 24);
            var crc = UpdateCrc(0xFFFFFFFF, 0xA1);
            for (var index = 0; index < report.Length - 4; index++)
            {
                crc = UpdateCrc(crc, report[index]);
            }
            return ~crc == expected;
        }

        private static uint UpdateCrc(uint crc, byte value)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
            return crc;
        }

        private static string ToLevel(int capacity)
        {
            if (capacity <= 10) return "Empty";
            if (capacity <= 30) return "Low";
            if (capacity <= 70) return "Medium";
            return "Full";
        }

        private sealed class CachedReading
        {
            public DateTime TimestampUtc { get; set; }
            public bool Success { get; set; }
            public string Level { get; set; }
        }
    }
}
