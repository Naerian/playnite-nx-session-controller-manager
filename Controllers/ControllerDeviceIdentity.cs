using System;
using System.Collections.Generic;

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
                    case 0x202F:
                        return "8BitDo Ultimate 3";
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

            return IsGenericDisplayName(rawName) ? "Game Controller" : rawName;
        }

        public static bool IsGenericDisplayName(string rawName)
        {
            return string.IsNullOrWhiteSpace(rawName) || Contains(rawName, "xinput controller") ||
                string.Equals(rawName, "controller", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(rawName, "game controller", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(rawName, "usb gamepad", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(rawName, "unknown controller", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLikelyNonController(string name, string path)
        {
            return LooksLikePointerOrKeyboard(name) || LooksLikePointerOrKeyboard(path);
        }

        /// <summary>
        /// Unused HID interfaces may enrich a Playnite or XInput row (battery, Bluetooth path),
        /// but unknown USB HID collections must not become extra "Game Controller" inventory.
        /// </summary>
        public static bool IsPublishableHidCapability(string displayName, string devicePath)
        {
            if (IsLikelyNonController(displayName, devicePath))
            {
                return false;
            }

            if (WindowsBluetoothBatteryProvider.IsBluetoothPath(devicePath))
            {
                return true;
            }

            return !IsGenericDisplayName(displayName);
        }

        public static string ResolvePlayniteDisplayName(string rawName, ushort vendorId, ushort productId)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                rawName = "Unknown controller";
            }

            if (!IsGenericDisplayName(rawName))
            {
                return rawName;
            }

            var mapped = GetDisplayName(rawName, vendorId, productId);
            return IsGenericDisplayName(mapped) ? rawName : mapped;
        }

        public static bool ShouldAcceptPlayniteInventory(string rawName, string path,
            ushort vendorId, ushort productId)
        {
            if (IsLikelyNonController(rawName, path))
            {
                return false;
            }

            if (WindowsBluetoothBatteryProvider.IsBluetoothPath(path))
            {
                return true;
            }

            if (!IsGenericDisplayName(rawName))
            {
                return true;
            }

            return !IsGenericDisplayName(GetDisplayName(rawName, vendorId, productId));
        }

        public static string GetConnectionType(string deviceName, ushort vendorId, ushort productId,
            string devicePath = null)
        {
            if (string.IsNullOrWhiteSpace(devicePath))
            {
                // A fresh XInput slot has no HID path yet. Do not consult leftover BTHENUM
                // nodes from a previous Bluetooth pairing — that is how a dongle reconnect
                // was labelled Bluetooth while the receiver was still enumerating.
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

            if (Contains(devicePath, "&ig_"))
            {
                // XInput wrappers are almost always a cable or a 2.4 GHz dongle. Only Xbox
                // licensed pads also speak XInput over Bluetooth; other brands keep a separate
                // DInput/BLE HID path for that transport.
                if (vendorId == 0x045E && productId != 0 &&
                    HasBluetoothPresence(vendorId, productId))
                {
                    return "Bluetooth";
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

            if (vendorId == 0x054C && (productId == 0x05C4 || productId == 0x09CC ||
                productId == 0x0CE6 || productId == 0x0DF2))
            {
                return HidDiagnosticsService.HasUsbInterface(vendorId, productId) ? "Wired" : "Bluetooth";
            }

            // Non-wrapper HID paths can still hide the transport. Query BTHENUM/BTHLE for
            // this VID/PID (and known aliases) before falling back to the product name.
            if (vendorId != 0 && productId != 0 &&
                HasBluetoothPresence(vendorId, productId))
            {
                return "Bluetooth";
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

        internal static bool HasBluetoothPresence(ushort vendorId, ushort productId)
        {
            foreach (var alias in GetBluetoothAliasProductIds(vendorId, productId))
            {
                if (HidDiagnosticsService.HasBluetoothInterface(vendorId, alias))
                {
                    return true;
                }
            }

            return false;
        }

        internal static IEnumerable<ushort> GetBluetoothAliasProductIds(ushort vendorId, ushort productId)
        {
            yield return productId;
            if (vendorId != 0x2DC8)
            {
                yield break;
            }

            if (productId == 0x310A)
            {
                yield return 0x301B;
            }
            else if (productId == 0x301B)
            {
                yield return 0x310A;
            }
        }

        public static bool ContainsWirelessHint(string deviceName)
        {
            return Contains(deviceName, "wireless");
        }

        private static bool LooksLikePointerOrKeyboard(string value)
        {
            return Contains(value, "mouse") || Contains(value, "ratón") || Contains(value, "raton") ||
                Contains(value, "keyboard") || Contains(value, "teclado") || Contains(value, "touchpad") ||
                Contains(value, "trackpad") || Contains(value, "trackball") || Contains(value, "digitizer") ||
                Contains(value, "hid_device_system_mouse") || Contains(value, "hid_device_system_keyboard") ||
                Contains(value, "\\kbd") || Contains(value, "\\mou");
        }

        private static bool Contains(string value, string fragment)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
