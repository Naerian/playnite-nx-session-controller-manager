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
                    case 0x6013:
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

        public static bool IsUnknownConnection(string connectionType)
        {
            return string.IsNullOrWhiteSpace(connectionType) ||
                string.Equals(connectionType, "Unknown", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsUnknownConnection(ControllerDeviceSnapshot controller)
        {
            return controller == null || IsUnknownConnection(controller.ConnectionType);
        }

        public static string GetModelKey(ControllerDeviceSnapshot controller)
        {
            if (controller == null)
            {
                return string.Empty;
            }

            if (controller.VendorId == 0x2DC8)
            {
                var mapped = GetDisplayName(controller.DetectedName ?? controller.Name,
                    controller.VendorId, controller.ProductId);
                if (!IsGenericDisplayName(mapped))
                {
                    return string.Format("{0:X4}:{1}", controller.VendorId, mapped);
                }
            }

            return controller.HardwareId ?? controller.ControllerId ?? string.Empty;
        }

        public static bool AreTransportAliases(ControllerDeviceSnapshot left,
            ControllerDeviceSnapshot right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            var leftKey = GetModelKey(left);
            var rightKey = GetModelKey(right);
            return !string.IsNullOrWhiteSpace(leftKey) &&
                leftKey.StartsWith("2DC8:", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(leftKey, rightKey, StringComparison.OrdinalIgnoreCase);
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

                if (IsSonyWirelessController(deviceName, vendorId))
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
                // XInput wrappers drop the real transport. Prefer an honest Wireless/Unknown
                // result over inheriting Bluetooth from a sibling BLE node of the same brand
                // (dongle + leftover BT pairing). Product/name hints remain last-resort only.
                if (Contains(deviceName, "bluetooth"))
                {
                    return "Bluetooth";
                }

                if (IsSonyWirelessController(deviceName, vendorId))
                {
                    return "Bluetooth";
                }

                if (Contains(deviceName, "wireless"))
                {
                    return "Wireless";
                }

                return "Unknown";
            }

            // Path lacked explicit USB/BT markers. Prefer live Windows PnP evidence over
            // brand-specific PID tables or product-name guesses.
            if (vendorId != 0 && productId != 0)
            {
                if (HidDiagnosticsService.HasUsbInterface(vendorId, productId) &&
                    !HasBluetoothPresence(vendorId, productId))
                {
                    return "Wired";
                }

                if (HasBluetoothPresence(vendorId, productId))
                {
                    return "Bluetooth";
                }
            }

            if (Contains(deviceName, "bluetooth"))
            {
                return "Bluetooth";
            }

            if (IsSonyWirelessController(deviceName, vendorId))
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
            if (vendorId == 0 || productId == 0)
            {
                return false;
            }

            // Exact VID/PID only. Sibling BLE/HID PIDs are correlated later via Bluetooth
            // address / container when reading battery — not by maintaining brand alias tables.
            return HidDiagnosticsService.HasBluetoothInterface(vendorId, productId);
        }

        /// <summary>
        /// Yields the product ID itself. Kept for call sites that iterate "aliases"; behavior
        /// is intentionally generic (no hardcoded sibling PID lists).
        /// </summary>
        internal static IEnumerable<ushort> GetBluetoothAliasProductIds(ushort vendorId, ushort productId)
        {
            yield return productId;
        }

        public static bool ContainsWirelessHint(string deviceName)
        {
            return Contains(deviceName, "wireless");
        }

        private static bool IsSonyWirelessController(string deviceName, ushort vendorId)
        {
            return vendorId == 0x054C && ContainsWirelessHint(deviceName) &&
                (Contains(deviceName, "controller") || Contains(deviceName, "dualsense") ||
                 Contains(deviceName, "dualshock"));
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
