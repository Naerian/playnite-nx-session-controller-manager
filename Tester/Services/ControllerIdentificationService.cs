using ControllerSessionManager.Tester.Models;

namespace ControllerSessionManager.Tester.Services
{
    public static class ControllerIdentificationService
    {
        public static GamepadLayout DetectLayout(string controllerName, ushort vendorId, ushort productId)
        {
            var name = (controllerName ?? string.Empty).ToLowerInvariant();

            if (vendorId == 0x2DC8 || name.Contains("8bitdo"))
            {
                return GamepadLayout.EightBitDo;
            }

            if ((vendorId == 0x057E && productId == 0x2009) || name.Contains("switch pro") || name.Contains("nintendo switch pro"))
            {
                return GamepadLayout.SwitchPro;
            }

            if (name.Contains("xbox") || name.Contains("xinput"))
            {
                return GamepadLayout.Xbox;
            }

            if (name.Contains("dualshock") || name.Contains("dualsense") || name.Contains("playstation") ||
                (name.Contains("wireless controller") && (vendorId == 0 || vendorId == 0x054C)))
            {
                return GamepadLayout.PlayStation;
            }

            return GamepadLayout.Generic;
        }

        public static EightBitDoModel DetectEightBitDoModel(string controllerName, ushort vendorId, ushort productId)
        {
            var name = (controllerName ?? string.Empty).ToLowerInvariant();

            if (vendorId != 0x2DC8 && !name.Contains("8bitdo"))
            {
                return EightBitDoModel.Unknown;
            }

            if (productId == 0x3019 || name.Contains("8bitdo 64"))
            {
                return EightBitDoModel.Controller64;
            }

            if (name.Contains("pro 2") || name.Contains("pro2"))
            {
                return EightBitDoModel.Pro2;
            }

            if (productId == 0x6009 || name.Contains("pro 3") || name.Contains("pro3"))
            {
                return EightBitDoModel.Pro3;
            }

            if (productId == 0x301B || productId == 0x301C || productId == 0x301D || name.Contains("ultimate 2c"))
            {
                return EightBitDoModel.Ultimate2CWireless;
            }

            if (productId == 0x202F || productId == 0x310B || productId == 0x6012 || productId == 0x6013 ||
                productId == 0x3011 || productId == 0x3012 || productId == 0x3013 ||
                name.Contains("ultimate 2") || name.Contains("ultimate"))
            {
                return EightBitDoModel.Ultimate2Wireless;
            }

            return EightBitDoModel.Unknown;
        }
    }
}
