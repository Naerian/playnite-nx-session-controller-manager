namespace ControllerSessionManager.Controllers
{
    public static class ControllerIconCatalog
    {
        public const string DefaultId = "default";
        public const string DefaultFileName = "Default.svg";

        public const string XboxOneId = "xbox-one";
        public const string XboxSeriesId = "xbox-series";
        public const string DualShockId = "dualshock";
        public const string DualSenseId = "dualsense";
        public const string SwitchProId = "switch-pro";
        public const string EightBitDoProId = "8bitdo-pro";
        public const string EightBitDoUltimateId = "8bitdo-ultimate";
        public const string EightBitDoUltimate3Id = "8bitdo-ultimate-3";
        public const string SteamId = "steam";
        public const string SteamV2Id = "steam-v2";

        public static string GetFileName(string iconId)
        {
            switch (IsKnown(iconId) ? iconId : DefaultId)
            {
                case XboxOneId: return "XboxOne.svg";
                case XboxSeriesId: return "XboxSeries.svg";
                case DualShockId: return "DualShock.svg";
                case DualSenseId: return "DualSense.svg";
                case SwitchProId: return "SwitchPro.svg";
                case EightBitDoProId: return "8BitdoPro.svg";
                case EightBitDoUltimateId: return "8BitdoUltimate2.svg";
                case EightBitDoUltimate3Id: return "8BitdoUltimate3.svg";
                case SteamId: return "SteamController.svg";
                case SteamV2Id: return "SteamControllerV2.svg";
                default: return DefaultFileName;
            }
        }

        public static string ResolveId(ControllerDeviceSnapshot controller, string profileIconId)
        {
            if (!string.IsNullOrWhiteSpace(profileIconId) &&
                IsKnown(profileIconId) &&
                !IsLegacy(profileIconId))
            {
                return profileIconId;
            }

            var suggested = Suggest(controller);
            return suggested != DefaultId
                ? suggested
                : string.IsNullOrWhiteSpace(profileIconId) ? DefaultId : Normalize(profileIconId);
        }

        public static string ResolveFileName(ControllerDeviceSnapshot controller, string profileIconId)
        {
            return GetFileName(ResolveId(controller, profileIconId));
        }

        public static string Suggest(ControllerDeviceSnapshot controller)
        {
            if (controller == null)
            {
                return DefaultId;
            }

            return Suggest(controller.VendorId, controller.ProductId, controller.DetectedName ?? controller.Name);
        }

        public static string Suggest(ushort vendorId, ushort productId, string name)
        {
            var normalizedName = (name ?? string.Empty).ToLowerInvariant();

            if (vendorId == 0x2DC8)
            {
                return SuggestEightBitDo(productId, normalizedName);
            }

            if (vendorId == 0x057E)
            {
                return SwitchProId;
            }

            if (vendorId == 0x054C)
            {
                switch (productId)
                {
                    case 0x05C4:
                    case 0x09CC:
                        return DualShockId;
                    default:
                        return DualSenseId;
                }
            }

            if (vendorId == 0x28DE)
            {
                return normalizedName.Contains("v2") || normalizedName.Contains("steam controller 2")
                    ? SteamV2Id
                    : SteamId;
            }

            if (vendorId == 0x045E)
            {
                switch (productId)
                {
                    case 0x02D1:
                    case 0x02DD:
                    case 0x02E0:
                    case 0x02EA:
                    case 0x02E3:
                        return XboxOneId;
                    default:
                        return XboxSeriesId;
                }
            }

            return DefaultId;
        }

        public static string Normalize(string iconId)
        {
            if (IsKnown(iconId) || IsLegacy(iconId))
            {
                return iconId;
            }

            return DefaultId;
        }

        public static bool IsLegacy(string iconId)
        {
            switch (iconId)
            {
                case "gamepad":
                case "gamepad-2":
                case "gamepad-3":
                case "gamepad-4":
                case "nintendo":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsKnown(string iconId)
        {
            switch (iconId)
            {
                case DefaultId:
                case XboxOneId:
                case XboxSeriesId:
                case DualShockId:
                case DualSenseId:
                case SwitchProId:
                case EightBitDoProId:
                case EightBitDoUltimateId:
                case EightBitDoUltimate3Id:
                case SteamId:
                case SteamV2Id:
                    return true;
                default:
                    return false;
            }
        }

        private static string SuggestEightBitDo(ushort productId, string normalizedName)
        {
            if (productId == 0x3019 || normalizedName.Contains("8bitdo 64"))
            {
                return DefaultId;
            }

            if (normalizedName.Contains("pro 2") || normalizedName.Contains("pro2") ||
                productId == 0x6009 || normalizedName.Contains("pro 3") || normalizedName.Contains("pro3"))
            {
                return EightBitDoProId;
            }

            if (productId == 0x202F || normalizedName.Contains("ultimate 3") ||
                normalizedName.Contains("ultimate3"))
            {
                return EightBitDoUltimate3Id;
            }

            return EightBitDoUltimateId;
        }
    }
}
