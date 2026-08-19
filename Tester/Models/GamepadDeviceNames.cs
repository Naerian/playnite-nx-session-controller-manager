namespace ControllerSessionManager.Tester.Models
{
    public static class GamepadDeviceNames
    {
        public static string GetDisplayName(string rawName, ushort vendorId, ushort productId, GamepadLayout layout, EightBitDoModel eightBitDoModel)
        {
            if (layout == GamepadLayout.EightBitDo)
            {
                return GetEightBitDoName(eightBitDoModel);
            }

            var detectedName = GetKnownDeviceName(vendorId, productId, layout);
            if (!string.IsNullOrEmpty(detectedName))
            {
                return detectedName;
            }

            if (!IsGenericName(rawName))
            {
                return rawName;
            }

            return GetLayoutFallbackName(layout);
        }

        public static string GetKnownDeviceName(ushort vendorId, ushort productId, GamepadLayout layout)
        {
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
                        return layout == GamepadLayout.Xbox ? "Xbox Controller" : null;
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
                    default:
                        return layout == GamepadLayout.PlayStation ? "PlayStation Controller" : null;
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

            return null;
        }

        public static bool IsDualSense(ushort vendorId, ushort productId)
        {
            return vendorId == 0x054C &&
                (productId == 0x0CE6 || productId == 0x0DF2);
        }

        public static bool IsSteamController(string rawName, ushort vendorId)
        {
            return vendorId == 0x28DE ||
                (!string.IsNullOrWhiteSpace(rawName) &&
                 rawName.ToLowerInvariant().Contains("steam controller"));
        }

        public static bool IsXboxSeriesOrElite(string rawName, ushort vendorId, ushort productId)
        {
            if (vendorId == 0x045E)
            {
                switch (productId)
                {
                    case 0x02E3:
                    case 0x0B05:
                    case 0x0B12:
                    case 0x0B13:
                        return true;
                }
            }

            var normalizedName = NormalizeDeviceName(rawName);
            return normalizedName.Contains("XBOXSERIES") ||
                   normalizedName.Contains("XBOXELITE") ||
                   normalizedName.Contains("MICROSOFTXBOXELITE");
        }

        public static string GetEightBitDoName(EightBitDoModel model)
        {
            switch (model)
            {
                case EightBitDoModel.Ultimate2Wireless:
                    return "8BitDo Ultimate 2 Wireless";
                case EightBitDoModel.Ultimate2CWireless:
                    return "8BitDo Ultimate 2C Wireless";
                case EightBitDoModel.Pro2:
                    return "8BitDo Pro 2";
                case EightBitDoModel.Pro3:
                    return "8BitDo Pro 3";
                case EightBitDoModel.Controller64:
                    return "8BitDo 64 Controller";
                default:
                    return "8BitDo Controller";
            }
        }

        public static string GetLayoutFallbackName(GamepadLayout layout)
        {
            switch (layout)
            {
                case GamepadLayout.Xbox:
                    return "Xbox Controller";
                case GamepadLayout.PlayStation:
                    return "PlayStation Controller";
                case GamepadLayout.SwitchPro:
                    return "Nintendo Switch Pro Controller";
                case GamepadLayout.EightBitDo:
                    return "8BitDo Controller";
                case GamepadLayout.Generic:
                    return "Game Controller";
                default:
                    return "Unknown Controller";
            }
        }

        private static bool IsGenericName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return true;
            }

            var normalized = rawName.Trim().ToLowerInvariant();
            return normalized == "xinput controller" ||
                   normalized == "controller" ||
                   normalized == "game controller" ||
                   normalized == "wireless controller" ||
                   normalized == "usb gamepad" ||
                   normalized == "hid-compliant game controller";
        }

        private static string NormalizeDeviceName(string rawName)
        {
            return string.IsNullOrWhiteSpace(rawName)
                ? string.Empty
                : rawName.Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
        }
    }
}
