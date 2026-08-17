using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ControllerSessionManager.Controllers;
using ControllerSessionManager.Sessions;

namespace ControllerSessionManager.PlayniteIntegration
{
    internal static class SupportReportService
    {
        public static string CreateReport(string version, string playniteMode,
            ControllerSessionManagerSettings settings, IEnumerable<ControllerDeviceSnapshot> controllers,
            Guid? activeGameId, int activeGameProcessId, SessionProtectionPolicy policy,
            GameSessionManager session, IEnumerable<DiagnosticEventEntry> recentEvents)
        {
            var output = new StringBuilder();
            var snapshot = (controllers ?? Enumerable.Empty<ControllerDeviceSnapshot>()).ToList();
            output.AppendLine("Controller Session Manager - Support report");
            output.AppendLine("Generated: " + DateTime.Now.ToString("O"));
            output.AppendLine("Privacy: no HID paths, serial numbers, user folders or Playnite log files are included");
            output.AppendLine();
            output.AppendLine("Environment");
            output.AppendLine("- Plugin version: " + Safe(version));
            output.AppendLine("- Playnite mode: " + Safe(playniteMode));
            output.AppendLine("- Windows: " + Environment.OSVersion.VersionString);
            output.AppendLine("- 64-bit OS/process: " + Environment.Is64BitOperatingSystem + "/" + Environment.Is64BitProcess);
            output.AppendLine("- CLR: " + Environment.Version);
            output.AppendLine("- Culture: " + CultureInfo.CurrentCulture.Name);
            output.AppendLine();
            AppendSettings(output, settings);
            AppendControllers(output, snapshot);
            AppendSession(output, activeGameId, activeGameProcessId, policy, session);
            AppendEvents(output, recentEvents);
            return output.ToString();
        }

        private static void AppendSettings(StringBuilder output, ControllerSessionManagerSettings settings)
        {
            output.AppendLine("Effective global settings");
            if (settings == null)
            {
                output.AppendLine("- unavailable");
            }
            else
            {
                output.AppendLine("- Monitoring/session tracking: " + settings.EnableMonitoring + "/" + settings.EnableSessionTracking);
                output.AppendLine("- Grace/reconciliation: " + settings.DisconnectGracePeriodMilliseconds + " ms/" + settings.ReconciliationIntervalSeconds + " s");
                output.AppendLine("- Overlay/Fullscreen notifications: " + settings.ShowDisconnectOverlay + "/" + settings.ShowFullscreenControllerNotifications);
                output.AppendLine("- Adaptive local multiplayer: " + !settings.ProtectAllActiveControllers);
                output.AppendLine("- Pause key/force-pause offline: " + settings.PauseGameOnDisconnect + "/" + settings.ForcePauseOfflineGames);
                output.AppendLine("- Pause key: " + Safe(settings.PauseKey));
                output.AppendLine("- Controller profiles/game overrides: " +
                    (settings.ControllerProfiles == null ? 0 : settings.ControllerProfiles.Count) + "/" +
                    (settings.GameSessionOverrides == null ? 0 : settings.GameSessionOverrides.Count));
                output.AppendLine("- Debug logging: " + settings.EnableDebugLogging);
            }
            output.AppendLine();
        }

        private static void AppendControllers(StringBuilder output, IList<ControllerDeviceSnapshot> controllers)
        {
            output.AppendLine("Controller snapshot (" + controllers.Count(a => a.IsConnected) + " connected, " + controllers.Count + " known)");
            foreach (var controller in controllers.OrderByDescending(a => a.IsConnected).ThenBy(a => a.Name))
            {
                output.AppendLine(string.Format("- {0} | connected={1} enabled={2} provider={3}:{4} VID={5:X4} PID={6:X4}",
                    Safe(controller.Name), controller.IsConnected, controller.IsEnabled,
                    Safe(string.IsNullOrWhiteSpace(controller.LifecycleProviderId)
                        ? controller.ProviderId
                        : controller.LifecycleProviderId + " + " + controller.ProviderId),
                    controller.ProviderInstanceId, controller.VendorId, controller.ProductId));
                output.AppendLine(string.Format("  identity={0} connection={1} battery={2} batteryProvider={3} input={4} lastSeen={5}",
                    Fingerprint(string.IsNullOrWhiteSpace(controller.HardwareId) ? controller.ControllerId : controller.HardwareId),
                    Safe(controller.ConnectionType), Safe(controller.BatteryLevel), Safe(controller.BatteryProviderId), Safe(controller.LastInputKind),
                    controller.LastSeenUtc.ToString("O")));
            }
            if (controllers.Count == 0)
            {
                output.AppendLine("- none");
            }
            output.AppendLine();
        }

        private static void AppendSession(StringBuilder output, Guid? gameId, int processId,
            SessionProtectionPolicy policy, GameSessionManager session)
        {
            output.AppendLine("Current session");
            output.AppendLine("- Active game fingerprint: " + (gameId.HasValue ? Fingerprint(gameId.Value.ToString("N")) : "none"));
            output.AppendLine("- Process present: " + (processId > 0));
            output.AppendLine("- Session running: " + (session != null && session.IsRunning));
            if (policy != null)
            {
                output.AppendLine(string.Format("- Policy: enabled={0} allActive={1} takeover={2} pauseKey={3} forcePause={4} override={5}/{6}",
                    policy.Enabled, policy.ProtectAllActiveControllers, policy.AllowControllerTakeover,
                    policy.PauseGameOnDisconnect, policy.ForcePauseOfflineGames,
                    policy.HasSessionOverride, policy.HasPauseOverride));
            }
            if (session != null && session.IsRunning)
            {
                output.AppendLine("- Active/suspected/confirmed: " + session.ActiveControllers.Count + "/" +
                    session.SuspectedDisconnectCount + "/" + session.ConfirmedDisconnectCount);
                foreach (var controller in session.ActiveControllers)
                {
                    output.AppendLine(string.Format("  - {0} key={1} missing={2} confirmed={3} evidence={4}",
                        Safe(controller.Name), Fingerprint(controller.ControllerKey), controller.MissingSinceUtc.HasValue,
                        controller.DisconnectConfirmed, Safe(controller.InputEvidence)));
                }
            }
            output.AppendLine();
        }

        private static void AppendEvents(StringBuilder output, IEnumerable<DiagnosticEventEntry> events)
        {
            var items = (events ?? Enumerable.Empty<DiagnosticEventEntry>()).OrderBy(a => a.TimestampUtc).ToList();
            output.AppendLine("Recent controller/session events (oldest first, " + items.Count + ")");
            foreach (var item in items)
            {
                output.AppendLine(string.Format("- {0} [{1}] {2}", item.TimestampUtc.ToString("O"),
                    Safe(item.Category), Safe(item.Message)));
            }
            if (items.Count == 0)
            {
                output.AppendLine("- none recorded in this Playnite process");
            }
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "unavailable" : value.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        internal static string Fingerprint(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unavailable";
            }

            unchecked
            {
                uint hash = 2166136261;
                foreach (var character in value.ToUpperInvariant())
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return "id-" + hash.ToString("X8");
            }
        }
    }
}
