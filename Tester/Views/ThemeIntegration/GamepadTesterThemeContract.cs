using System;
using System.Collections.Generic;

namespace ControllerSessionManager.Tester.Views.ThemeIntegration
{
    public static class GamepadTesterThemeContract
    {
        public const string Version = "1.1";

        private static readonly string[] blockNames =
        {
            "GamepadTesterLauncher",
            "StatusBadge",
            "ButtonMap",
            "StickCheck",
            "TriggerCheck",
            "RumblePad",
            "LatencyMini"
        };

        private static readonly string[] resourceKeys =
        {
            GamepadTesterThemeControlBase.ControlBackgroundBrushKey,
            GamepadTesterThemeControlBase.ButtonBackgroundBrushKey,
            GamepadTesterThemeControlBase.ControlBorderBrushKey,
            GamepadTesterThemeControlBase.StickGuideBrushKey,
            GamepadTesterThemeControlBase.TextBrushKey
        };

        public static IList<string> BlockNames
        {
            get { return Array.AsReadOnly(blockNames); }
        }

        public static IList<string> ResourceKeys
        {
            get { return Array.AsReadOnly(resourceKeys); }
        }

        public static bool SupportsBlock(string block)
        {
            if (string.IsNullOrWhiteSpace(block))
            {
                return false;
            }

            foreach (var candidate in blockNames)
            {
                if (string.Equals(candidate, block, StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(candidate, "GamepadTesterLauncher", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(block, "Launcher", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
