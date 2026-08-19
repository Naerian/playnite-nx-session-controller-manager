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
        public const string PlayStation = "PlayStation";
        public const string DualSense = "DualSense";
        public const string SwitchPro = "SwitchPro";
        public const string EightBitDoUltimate = "EightBitDoUltimate";
        public const string EightBitDoPro = "EightBitDoPro";
        public const string SteamController = "SteamController";

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
            yield return CreateDefinition(XboxSeries, "Xbox Series X / S");
            yield return CreateDefinition(XboxOne, "Xbox One");
            yield return CreateDefinition(PlayStation, localize, "LOCCSM_Tester_VisualSchemePlayStation", "PlayStation");
            yield return CreateDefinition(DualSense, localize, "LOCCSM_Tester_VisualSchemeDualSense", "DualSense");
            yield return CreateDefinition(SwitchPro, localize, "LOCCSM_Tester_VisualSchemeSwitchPro", "Switch Pro");
            yield return CreateDefinition(EightBitDoUltimate, "8BitDo Ultimate");
            yield return CreateDefinition(EightBitDoPro, "8BitDo Pro");
            yield return CreateDefinition(SteamController, "Steam Controller");
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
                return SteamController;
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
            return visualSchemeKey == DualSense || visualSchemeKey == PlayStation;
        }

        public static bool UsesSwitchProLabels(string visualSchemeKey)
        {
            return visualSchemeKey == SwitchPro;
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
                GuidedHeight = guidedHeight
            };
        }

        private static ControllerVisualSchemeDefinition CreateDefinition(string key, Func<string, string, string> localize, string localizationKey, string fallback)
        {
            return CreateDefinition(key, localize == null ? fallback : localize(localizationKey, fallback));
        }
    }
}
