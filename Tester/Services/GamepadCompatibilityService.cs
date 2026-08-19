using ControllerSessionManager.Tester.Models;
using System;
using System.Collections.Generic;

namespace ControllerSessionManager.Tester.Services
{
    public static class GamepadCompatibilityService
    {
        private static readonly string[] StandardBindings =
        {
            "a", "b", "x", "y",
            "back", "start",
            "leftstick", "rightstick",
            "leftshoulder", "rightshoulder",
            "dpup", "dpdown", "dpleft", "dpright",
            "leftx", "lefty", "rightx", "righty",
            "lefttrigger", "righttrigger"
        };

        public static GamepadCompatibilityAssessment Assess(GamepadState state)
        {
            state = state ?? new GamepadState();
            var result = new GamepadCompatibilityAssessment
            {
                InputMode = DetectInputMode(state),
                Severity = GamepadCompatibilitySeverity.Ready
            };

            if (!state.IsConnected)
            {
                result.Severity = GamepadCompatibilitySeverity.Limited;
                result.Findings.Add(Finding("NoController", GamepadCompatibilitySeverity.Limited));
                return result;
            }

            var bindings = ParseBindings(state.SdlMapping);
            result.HasMapping = bindings.Count > 0;
            if (!result.HasMapping)
            {
                result.Severity = GamepadCompatibilitySeverity.Warning;
                result.Findings.Add(Finding("MappingUnavailable", GamepadCompatibilitySeverity.Warning));
            }
            else
            {
                foreach (var expected in StandardBindings)
                {
                    if (!bindings.Contains(expected))
                    {
                        result.MissingBindings.Add(ToDisplayBinding(expected));
                    }
                }

                result.MappingCoveragePercent = (int)Math.Round(
                    (StandardBindings.Length - result.MissingBindings.Count) * 100d / StandardBindings.Length);

                if (result.MissingBindings.Count == 0)
                {
                    result.Findings.Add(Finding("MappingComplete", GamepadCompatibilitySeverity.Ready));
                }
                else
                {
                    result.Severity = result.MissingBindings.Count >= 5
                        ? GamepadCompatibilitySeverity.Limited
                        : GamepadCompatibilitySeverity.Warning;
                    result.Findings.Add(Finding(
                        "MissingBindings",
                        result.Severity,
                        string.Join(", ", result.MissingBindings)));
                }
            }

            if (state.AxisCount > 0 && state.AxisCount < 4)
            {
                Promote(result, GamepadCompatibilitySeverity.Limited);
                result.Findings.Add(Finding("InsufficientAxes", GamepadCompatibilitySeverity.Limited, state.AxisCount.ToString()));
            }
            else if (state.AxisCount >= 4)
            {
            }

            if (state.ButtonCount > 0 && state.ButtonCount < 8)
            {
                Promote(result, GamepadCompatibilitySeverity.Warning);
                result.Findings.Add(Finding("FewButtons", GamepadCompatibilitySeverity.Warning, state.ButtonCount.ToString()));
            }

            if (result.InputMode == GamepadInputMode.Unknown && state.Layout == GamepadLayout.EightBitDo)
            {
                Promote(result, GamepadCompatibilitySeverity.Info);
                result.Findings.Add(Finding("EightBitDoModeUnknown", GamepadCompatibilitySeverity.Info));
            }
            return result;
        }

        public static HashSet<string> ParseBindings(string mapping)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(mapping) ||
                string.Equals(mapping, "Unavailable", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapping, "Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            var parts = mapping.Split(',');
            for (var index = 2; index < parts.Length; index++)
            {
                var separator = parts[index].IndexOf(':');
                if (separator <= 0)
                {
                    continue;
                }

                var key = parts[index].Substring(0, separator).Trim();
                if (!string.IsNullOrWhiteSpace(key) && !string.Equals(key, "platform", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(key);
                }
            }

            return result;
        }

        private static GamepadInputMode DetectInputMode(GamepadState state)
        {
            var identity = ((state.ControllerName ?? string.Empty) + " " + (state.SdlMapping ?? string.Empty)).ToLowerInvariant();
            if (identity.Contains("xinput") || identity.Contains("x-box") || identity.Contains("xbox"))
            {
                return GamepadInputMode.XInput;
            }

            if (identity.Contains("directinput") || identity.Contains("dinput"))
            {
                return GamepadInputMode.DirectInput;
            }

            if (state.Layout == GamepadLayout.PlayStation ||
                state.Layout == GamepadLayout.SwitchPro ||
                identity.Contains("steam controller"))
            {
                return GamepadInputMode.NativeHid;
            }

            return GamepadInputMode.Unknown;
        }

        private static void Promote(GamepadCompatibilityAssessment assessment, GamepadCompatibilitySeverity severity)
        {
            if ((int)severity > (int)assessment.Severity)
            {
                assessment.Severity = severity;
            }
        }

        private static GamepadCompatibilityFinding Finding(
            string code,
            GamepadCompatibilitySeverity severity,
            string evidence = null)
        {
            return new GamepadCompatibilityFinding
            {
                Code = code,
                Severity = severity,
                Evidence = evidence
            };
        }

        private static string ToDisplayBinding(string binding)
        {
            switch (binding)
            {
                case "leftstick": return "LS click";
                case "rightstick": return "RS click";
                case "leftshoulder": return "LB";
                case "rightshoulder": return "RB";
                case "lefttrigger": return "LT";
                case "righttrigger": return "RT";
                case "leftx": return "LS X";
                case "lefty": return "LS Y";
                case "rightx": return "RS X";
                case "righty": return "RS Y";
                case "dpup": return "D-pad Up";
                case "dpdown": return "D-pad Down";
                case "dpleft": return "D-pad Left";
                case "dpright": return "D-pad Right";
                default: return binding.ToUpperInvariant();
            }
        }
    }
}
