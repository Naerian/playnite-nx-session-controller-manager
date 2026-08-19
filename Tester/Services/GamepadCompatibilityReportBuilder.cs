using ControllerSessionManager.Tester.Models;
using System;
using System.Reflection;
using System.Text;

namespace ControllerSessionManager.Tester.Services
{
    public static class GamepadCompatibilityReportBuilder
    {
        public static string Build(
            GamepadState state,
            GamepadControllerInfo selectedController,
            string healthConfidence,
            string leftRangeConfidence,
            string rightRangeConfidence,
            string latencyConfidence)
        {
            state = state ?? new GamepadState();
            var report = new StringBuilder();
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            report.AppendLine("Gamepad Tester compatibility report");
            report.AppendLine("===================================");
            report.AppendLine(string.Format("Generated (UTC): {0:yyyy-MM-dd HH:mm:ss}", DateTime.UtcNow));
            report.AppendLine(string.Format("Extension version: {0}", version));
            report.AppendLine(string.Format("Operating system: {0}", Environment.OSVersion.VersionString));
            report.AppendLine(string.Format("Process architecture: {0}", Environment.Is64BitProcess ? "64-bit" : "32-bit"));
            report.AppendLine();

            report.AppendLine("[Device]");
            report.AppendLine(string.Format("Connected: {0}", state.IsConnected));
            report.AppendLine(string.Format("SDL name: {0}", Safe(state.ControllerName)));
            report.AppendLine(string.Format("Display name: {0}", selectedController == null ? Safe(state.ControllerName) : Safe(selectedController.DisplayName)));
            report.AppendLine(string.Format("Instance ID: {0}", selectedController == null ? "Unavailable" : selectedController.InstanceId.ToString()));
            report.AppendLine(string.Format("VID: {0:X4}", state.VendorId));
            report.AppendLine(string.Format("PID: {0:X4}", state.ProductId));
            report.AppendLine(string.Format("Detected layout: {0}", state.Layout));
            if (state.Layout == GamepadLayout.EightBitDo)
            {
                report.AppendLine(string.Format("8BitDo model: {0}", state.EightBitDoModel));
            }
            report.AppendLine();

            report.AppendLine("[SDL]");
            report.AppendLine("Backend: SDL GameController");
            report.AppendLine(string.Format("SDL runtime: {0}", Safe(state.SdlVersion)));
            report.AppendLine(string.Format("SDL GUID: {0}", Safe(state.SdlGuid)));
            report.AppendLine(string.Format("Mapping: {0}", Safe(state.SdlMapping)));
            report.AppendLine(string.Format("Axes exposed: {0}", state.AxisCount));
            report.AppendLine(string.Format("Buttons exposed: {0}", state.ButtonCount));
            report.AppendLine(string.Format("Hats exposed: {0}", state.HatCount));
            report.AppendLine(string.Format("Additional raw buttons: {0}", state.ExtraButtons == null ? 0 : state.ExtraButtons.Count));
            report.AppendLine("Rumble API: SDL_GameControllerRumble (device support must be verified with a vibration test)");
            report.AppendLine();

            var compatibility = GamepadCompatibilityService.Assess(state);
            report.AppendLine("[Compatibility assistant]");
            report.AppendLine(string.Format("Status: {0}", compatibility.Severity));
            report.AppendLine(string.Format("Input mode: {0}", compatibility.InputMode));
            report.AppendLine(string.Format("Mapping available: {0}", compatibility.HasMapping));
            report.AppendLine(string.Format("Standard mapping coverage: {0}%", compatibility.MappingCoveragePercent));
            report.AppendLine(string.Format(
                "Missing standard bindings: {0}",
                compatibility.MissingBindings.Count == 0
                    ? "None"
                    : string.Join(", ", compatibility.MissingBindings)));
            foreach (var finding in compatibility.Findings)
            {
                report.AppendLine(string.Format(
                    "Finding: {0} [{1}]{2}",
                    finding.Code,
                    finding.Severity,
                    string.IsNullOrWhiteSpace(finding.Evidence) ? string.Empty : " - " + finding.Evidence));
            }
            report.AppendLine();

            report.AppendLine("[Normalized state]");
            report.AppendLine(string.Format("Left stick: X {0:0.000}, Y {1:0.000}", state.LeftStick.X, state.LeftStick.Y));
            report.AppendLine(string.Format("Right stick: X {0:0.000}, Y {1:0.000}", state.RightStick.X, state.RightStick.Y));
            report.AppendLine(string.Format("Left trigger: {0:0.000}", state.LeftTrigger));
            report.AppendLine(string.Format("Right trigger: {0:0.000}", state.RightTrigger));
            report.AppendLine();

            report.AppendLine("[Diagnostic confidence]");
            report.AppendLine(string.Format("Health: {0}", Safe(healthConfidence)));
            report.AppendLine(string.Format("Left stick range: {0}", Safe(leftRangeConfidence)));
            report.AppendLine(string.Format("Right stick range: {0}", Safe(rightRangeConfidence)));
            report.AppendLine(string.Format("Latency: {0}", Safe(latencyConfidence)));
            report.AppendLine();
            report.AppendLine("This report contains controller and runtime diagnostics only. It does not include user library data.");
            return report.ToString();
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Unavailable" : value.Replace("\r", " ").Replace("\n", " ");
        }
    }
}
