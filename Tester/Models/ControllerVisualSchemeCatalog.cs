using System;
using System.Collections.Generic;

namespace ControllerSessionManager.Tester.Models
{
    public static class ControllerVisualSchemeCatalog
    {
        private const double StandardTestWidth = 600d;
        private const double StandardTestHeight = 390d;
        private const double StandardGuidedWidth = 500d;
        private const double StandardGuidedHeight = 322d;

        public const string Universal = "Universal";
        public const string XboxSeries = "XboxSeries";
        public const string XboxOne = "XboxOne";
        public const string Xbox360 = "Xbox360";
        public const string XboxControllerS = "XboxControllerS";
        public const string PlayStation3 = "PlayStation3";
        public const string PlayStation = "PlayStation";
        public const string DualSense = "DualSense";
        public const string SwitchPro = "SwitchPro";
        public const string WiiUPro = "WiiUPro";
        public const string Stadia = "Stadia";
        public const string EightBitDoUltimate = "EightBitDoUltimate";
        public const string EightBitDoUltimate3 = "EightBitDoUltimate3";
        public const string EightBitDoPro = "EightBitDoPro";
        public const string SteamController = "SteamController";
        public const string SteamControllerV2 = "SteamControllerV2";

        public static IEnumerable<ControllerVisualSchemeOption> CreateOptions(Func<string, string, string> localize)
        {
            foreach (var definition in CreateDefinitions(localize))
            {
                yield return definition.ToOption();
            }
        }

        public static IEnumerable<ControllerVisualSchemeDefinition> CreateDefinitions(Func<string, string, string> localize)
        {
            yield return CreateDefinition(Universal, localize, "LOCCSM_Tester_VisualSchemeUniversal", "Universal");
            yield return CreateDefinition(XboxControllerS, "Xbox Controller S");
            yield return CreateDefinition(Xbox360, "Xbox 360");
            yield return CreateDefinition(XboxSeries, "Xbox Series X / S");
            yield return CreateDefinition(XboxOne, "Xbox One");
            yield return CreateDefinition(PlayStation3, "DualShock 3");
            yield return CreateDefinition(PlayStation, localize, "LOCCSM_Tester_VisualSchemePlayStation", "PlayStation");
            yield return CreateDefinition(DualSense, localize, "LOCCSM_Tester_VisualSchemeDualSense", "DualSense");
            yield return CreateDefinition(SwitchPro, localize, "LOCCSM_Tester_VisualSchemeSwitchPro", "Switch Pro");
            yield return CreateDefinition(WiiUPro, "Wii U Pro");
            yield return CreateDefinition(Stadia, "Stadia");
            yield return CreateDefinition(EightBitDoUltimate, "8BitDo Ultimate");
            yield return CreateDefinition(EightBitDoUltimate3, "8BitDo Ultimate 3");
            yield return CreateDefinition(EightBitDoPro, "8BitDo Pro");
            yield return CreateDefinition(SteamController, "Steam Controller");
            yield return CreateDefinition(SteamControllerV2, "Steam Controller 2");
        }

        public static ControllerVisualSchemeDefinition GetDefinition(string key, Func<string, string, string> localize)
        {
            foreach (var definition in CreateDefinitions(localize))
            {
                if (definition.Key == key)
                {
                    return definition;
                }
            }

            return CreateDefinition(Universal, localize, "LOCCSM_Tester_VisualSchemeUniversal", "Universal");
        }

        public static string Detect(GamepadState state)
        {
            if (state == null || !state.IsConnected)
            {
                return Universal;
            }

            if (GamepadDeviceNames.IsSteamController(state.ControllerName, state.VendorId))
            {
                var steamName = (state.ControllerName ?? string.Empty).ToLowerInvariant();
                return steamName.Contains("controller 2") || steamName.Contains("controller v2")
                    ? SteamControllerV2
                    : SteamController;
            }

            if (state.VendorId == 0x18D1 || (state.ControllerName ?? string.Empty).ToLowerInvariant().Contains("stadia"))
            {
                return Stadia;
            }

            if (state.VendorId == 0x057E && state.ProductId == 0x0330)
            {
                return WiiUPro;
            }

            if (state.VendorId == 0x054C && state.ProductId == 0x0268)
            {
                return PlayStation3;
            }

            if (state.VendorId == 0x045E && state.ProductId == 0x0285)
            {
                return XboxControllerS;
            }

            if (state.VendorId == 0x045E &&
                (state.ProductId == 0x028E || state.ProductId == 0x028F || state.ProductId == 0x0719))
            {
                return Xbox360;
            }

            if (state.Layout == GamepadLayout.PlayStation && GamepadDeviceNames.IsDualSense(state.VendorId, state.ProductId))
            {
                return DualSense;
            }

            if (state.Layout == GamepadLayout.PlayStation)
            {
                return PlayStation;
            }

            if (state.Layout == GamepadLayout.SwitchPro)
            {
                return SwitchPro;
            }

            if (state.Layout == GamepadLayout.EightBitDo)
            {
                if ((state.ControllerName ?? string.Empty).ToLowerInvariant().Contains("ultimate 3"))
                {
                    return EightBitDoUltimate3;
                }

                if (state.EightBitDoModel == EightBitDoModel.Controller64)
                {
                    return Universal;
                }

                if (state.EightBitDoModel == EightBitDoModel.Pro2 ||
                    state.EightBitDoModel == EightBitDoModel.Pro3)
                {
                    return EightBitDoPro;
                }

                return EightBitDoUltimate;
            }

            if (state.Layout == GamepadLayout.Xbox)
            {
                return GamepadDeviceNames.IsXboxSeriesOrElite(state.ControllerName, state.VendorId, state.ProductId)
                    ? XboxSeries
                    : XboxOne;
            }

            return Universal;
        }

        public static bool UsesPlayStationLabels(string visualSchemeKey)
        {
            return visualSchemeKey == DualSense || visualSchemeKey == PlayStation ||
                visualSchemeKey == PlayStation3;
        }

        public static bool UsesSwitchProLabels(string visualSchemeKey)
        {
            return visualSchemeKey == SwitchPro || visualSchemeKey == WiiUPro;
        }

        private static ControllerVisualSchemeDefinition CreateDefinition(string key, string displayName)
        {
            return CreateDefinition(key, displayName, StandardTestWidth, StandardTestHeight, StandardGuidedWidth, StandardGuidedHeight);
        }

        private static ControllerVisualSchemeDefinition CreateDefinition(string key, string displayName, double testWidth, double testHeight, double guidedWidth, double guidedHeight)
        {
            return new ControllerVisualSchemeDefinition
            {
                Key = key,
                DisplayName = displayName,
                TestWidth = testWidth,
                TestHeight = testHeight,
                GuidedWidth = guidedWidth,
                GuidedHeight = guidedHeight,
                SvgFileName = GetSvgFileName(key),
                InteractiveLayoutKey = GetInteractiveLayoutKey(key),
                ThumbnailScale = key == Universal ? 0.82d : 1d
            };
        }

        private static string GetSvgFileName(string key)
        {
            switch (key)
            {
                case XboxSeries:
                    return "xbox-series-x.svg";
                case XboxOne:
                    return "xbox-one.svg";
                case Xbox360:
                    return "xbox-360.svg";
                case XboxControllerS:
                    return "xbox-controller-s.svg";
                case PlayStation3:
                    return "ps3.svg";
                case PlayStation:
                    return "ps4.svg";
                case DualSense:
                    return "ps5.svg";
                case SwitchPro:
                    return "switch-pro.svg";
                case WiiUPro:
                    return "wii-u-pro.svg";
                case Stadia:
                    return "stadia.svg";
                case EightBitDoUltimate:
                    return "8bitdo-ultimate-2.svg";
                case EightBitDoUltimate3:
                    return "8bitdo-ultimate-3.svg";
                case EightBitDoPro:
                    return "8bitdo-pro.svg";
                case SteamController:
                    return "steam-controller.svg";
                case SteamControllerV2:
                    return "steam-controller-v2.svg";
                default:
                    return "default.svg";
            }
        }

        private static string GetInteractiveLayoutKey(string key)
        {
            switch (key)
            {
                case XboxControllerS:
                case Xbox360:
                case Stadia:
                    return XboxOne;
                case PlayStation3:
                    return PlayStation;
                case WiiUPro:
                    return SwitchPro;
                case EightBitDoUltimate3:
                    return EightBitDoUltimate;
                case SteamControllerV2:
                    return SteamController;
                default:
                    return key;
            }
        }

        private static ControllerVisualSchemeDefinition CreateDefinition(string key, Func<string, string, string> localize, string localizationKey, string fallback)
        {
            return CreateDefinition(key, localize == null ? fallback : localize(localizationKey, fallback));
        }
    }
}
