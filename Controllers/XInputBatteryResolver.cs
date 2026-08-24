using System;

namespace ControllerSessionManager.Controllers
{
    internal static class XInputBatteryResolver
    {
        private const byte BatteryTypeWired = 1;
        private const byte BatteryTypeAlkaline = 2;
        private const byte BatteryTypeNimh = 3;

        public static string Resolve(ControllerMetadata metadata, uint result, byte batteryType,
            byte batteryLevel, bool wiredUnknown, bool wireless)
        {
            if (metadata != null &&
                !string.IsNullOrWhiteSpace(metadata.BatteryLevel) &&
                metadata.BatteryLevel != "Unknown" && metadata.BatteryLevel != "Unavailable" &&
                (string.Equals(metadata.ConnectionType, "Bluetooth",
                    StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(metadata.BatteryProviderId, "Windows.BluetoothPnP",
                    StringComparison.OrdinalIgnoreCase) ||
                 WindowsBluetoothBatteryProvider.IsBluetoothPath(metadata.DevicePath)))
            {
                return metadata.BatteryLevel;
            }

            if (result != 0)
            {
                return "Unknown";
            }

            if (batteryType == BatteryTypeWired || wiredUnknown)
            {
                return "Unavailable";
            }

            string mapped;
            if (TryMapBatteryLevel(batteryLevel, out mapped) &&
                (wireless || (batteryType == 0xFF && IsLikelyWireless(metadata))))
            {
                return mapped;
            }

            return "Unknown";
        }

        internal static bool IsLikelyWireless(ControllerMetadata metadata)
        {
            if (metadata == null)
            {
                return false;
            }

            if (string.Equals(metadata.ConnectionType, "Wireless", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(metadata.ConnectionType, "WirelessReceiver", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(metadata.ConnectionType, "Bluetooth", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return metadata.VendorId == 0x045E &&
                ControllerDeviceIdentity.ContainsWirelessHint(metadata.DisplayName);
        }

        internal static bool TryMapBatteryLevel(byte rawLevel, out string mapped)
        {
            switch (rawLevel)
            {
                case 0:
                    mapped = "Empty";
                    return true;
                case 1:
                    mapped = "Low";
                    return true;
                case 2:
                    mapped = "Medium";
                    return true;
                case 3:
                    mapped = "Full";
                    return true;
                default:
                    mapped = null;
                    return false;
            }
        }

        internal static bool IsWirelessBatteryType(byte batteryType)
        {
            return batteryType == BatteryTypeAlkaline || batteryType == BatteryTypeNimh;
        }
    }
}
