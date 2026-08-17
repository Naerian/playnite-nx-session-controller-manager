using System;

namespace ControllerSessionManager.Controllers
{
    public static class ControllerDeviceIdentity
    {
        public static string GetDisplayName(string rawName, ushort vendorId, ushort productId)
        {
            if (vendorId == 0x2DC8)
            {
                switch (productId)
                {
                    case 0x310B:
                    case 0x6012:
                    case 0x3011:
                    case 0x3012:
                    case 0x3013:
                        return "8BitDo Ultimate 2 Wireless";
                    case 0x301B:
                    case 0x301C:
                    case 0x301D:
                        return "8BitDo Ultimate 2C Wireless";
                    case 0x6009:
                        return "8BitDo Pro 3";
                    case 0x3019:
                        return "8BitDo 64 Controller";
                    default:
                        return Contains(rawName, "8bitdo") ? rawName : "8BitDo Controller";
                }
            }

            if (vendorId == 0x045E)
            {
                switch (productId)
                {
                    case 0x02D1:
                    case 0x02DD:
                    case 0x02E0:
                    case 0x02EA:
                        return "Xbox One Controller";
                    case 0x02E3:
                        return "Xbox Elite Controller";
                    case 0x0B05:
                        return "Xbox Elite Wireless Controller Series 2";
                    case 0x0B12:
                    case 0x0B13:
                        return "Xbox Series Controller";
                    default:
                        return "Xbox Controller";
                }
            }

            if (vendorId == 0x054C)
            {
                switch (productId)
                {
                    case 0x05C4:
                    case 0x09CC:
                        return "DualShock 4";
                    case 0x0CE6:
                        return "DualSense";
                    case 0x0DF2:
                        return "DualSense Edge";
                }
            }

            if (vendorId == 0x057E && productId == 0x2009)
            {
                return "Nintendo Switch Pro Controller";
            }

            if (vendorId == 0x28DE)
            {
                return "Steam Controller";
            }

            return IsGenericName(rawName) ? "Game Controller" : rawName;
        }

        public static string GetConnectionType(string deviceName, ushort vendorId, ushort productId,
            string devicePath = null)
        {
            if (Contains(devicePath, "bthenum") || Contains(devicePath, "bthle") ||
                Contains(devicePath, "bluetooth") ||
                Contains(devicePath, "00001812-0000-1000-8000-00805f9b34fb"))
            {
                return "Bluetooth";
            }

            if (Contains(devicePath, "&mi_") || Contains(devicePath, "usb#") || Contains(devicePath, "usb\\"))
            {
                return "Wired";
            }

            if (vendorId == 0x054C && (productId == 0x05C4 || productId == 0x09CC ||
                productId == 0x0CE6 || productId == 0x0DF2))
            {
                return HidDiagnosticsService.HasUsbInterface(vendorId, productId) ? "Wired" : "Bluetooth";
            }

            if (Contains(deviceName, "bluetooth"))
            {
                return "Bluetooth";
            }

            if (Contains(deviceName, "wireless"))
            {
                return "Wireless";
            }

            return "Unknown";
        }

        private static bool IsGenericName(string rawName)
        {
            return string.IsNullOrWhiteSpace(rawName) || Contains(rawName, "xinput controller") ||
                string.Equals(rawName, "controller", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(rawName, "game controller", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(rawName, "usb gamepad", StringComparison.OrdinalIgnoreCase);
        }

        private static bool Contains(string value, string fragment)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
