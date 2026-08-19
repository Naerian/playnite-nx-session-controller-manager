using System;
using System.Collections.Generic;
using System.Linq;
using ControllerSessionManager.Controllers;

namespace ControllerSessionManager.Sessions
{
    /// <summary>
    /// Session keys follow merged HardwareId. Dongle/cable reconnects often emit a volatile
    /// xinput:slot:N id until HID metadata returns; treat the same VID/PID as the same pad.
    /// </summary>
    internal static class SessionControllerIdentity
    {
        public static ControllerDeviceSnapshot FindConnected(string sessionKey,
            IDictionary<string, ControllerDeviceSnapshot> connected,
            ISet<string> claimedKeys)
        {
            ControllerDeviceSnapshot exact;
            if (!string.IsNullOrWhiteSpace(sessionKey) &&
                connected != null &&
                connected.TryGetValue(sessionKey, out exact))
            {
                return exact;
            }

            var aliases = (connected ?? new Dictionary<string, ControllerDeviceSnapshot>())
                .Where(a => (claimedKeys == null || !claimedKeys.Contains(a.Key)) &&
                    RefersTo(sessionKey, a.Value))
                .Select(a => a.Value)
                .ToList();
            return aliases.Count == 1 ? aliases[0] : null;
        }

        internal static bool RefersTo(string sessionKey, ControllerDeviceSnapshot device)
        {
            if (device == null || string.IsNullOrWhiteSpace(sessionKey))
            {
                return false;
            }

            if (string.Equals(sessionKey, device.HardwareId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sessionKey, device.ControllerId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            ushort sessionVendor;
            ushort sessionProduct;
            if (!ControllerBridgeIdentity.TryParseHardwareVidPid(sessionKey, out sessionVendor,
                out sessionProduct))
            {
                return false;
            }

            if (device.VendorId == sessionVendor && device.ProductId == sessionProduct)
            {
                return true;
            }

            ushort deviceVendor;
            ushort deviceProduct;
            return ControllerBridgeIdentity.TryParseHardwareVidPid(device.HardwareId, out deviceVendor,
                out deviceProduct) &&
                deviceVendor == sessionVendor && deviceProduct == sessionProduct;
        }
    }
}
