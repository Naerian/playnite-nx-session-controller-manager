using System;
using System.Collections.Generic;
using System.Linq;
using ControllerSessionManager.Controllers;

namespace ControllerSessionManager.Sessions
{
    /// <summary>
    /// Session keys follow merged HardwareId. Dongle/cable reconnects often emit a volatile
    /// xinput:slot:N id until HID metadata returns; a unique VID/PID alias still recovers that
    /// single-pad case when the slot matches the hardware ordinal. Two pads of the same model
    /// keep distinct ordinals/slots (same rule as Mandos).
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

            var candidates = (connected ?? new Dictionary<string, ControllerDeviceSnapshot>())
                .Where(a => (claimedKeys == null || !claimedKeys.Contains(a.Key)) &&
                    IsCompatibleAlias(sessionKey, a.Value))
                .Select(a => a.Value)
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            return Disambiguate(sessionKey, candidates);
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

        /// <summary>
        /// VID/PID aliases are accepted only when they do not conflict with a known ordinal/slot.
        /// </summary>
        internal static bool IsCompatibleAlias(string sessionKey, ControllerDeviceSnapshot device)
        {
            if (!RefersTo(sessionKey, device))
            {
                return false;
            }

            ushort vendorId;
            ushort productId;
            int ordinal;
            if (!ControllerTransportPolicy.TryReadHardwareOrdinal(sessionKey, out vendorId,
                out productId, out ordinal))
            {
                return true;
            }

            if (MatchesHardwareOrdinal(device, ordinal) ||
                MatchesXInputSlot(device, ordinal - 1))
            {
                return true;
            }

            ushort ignoredVendor;
            ushort ignoredProduct;
            int deviceOrdinal;
            if (ControllerTransportPolicy.TryReadHardwareOrdinal(GetDeviceKey(device),
                out ignoredVendor, out ignoredProduct, out deviceOrdinal) &&
                deviceOrdinal > 0 && deviceOrdinal != ordinal)
            {
                return false;
            }

            int slot;
            if (TryGetXInputSlot(device, out slot) && slot != ordinal - 1)
            {
                return false;
            }

            return true;
        }

        private static ControllerDeviceSnapshot Disambiguate(string sessionKey,
            IList<ControllerDeviceSnapshot> candidates)
        {
            ushort vendorId;
            ushort productId;
            int ordinal;
            if (ControllerTransportPolicy.TryReadHardwareOrdinal(sessionKey, out vendorId,
                out productId, out ordinal))
            {
                var byOrdinal = candidates.Where(a => MatchesHardwareOrdinal(a, ordinal)).ToList();
                if (byOrdinal.Count == 1)
                {
                    return byOrdinal[0];
                }

                var bySlot = candidates.Where(a => MatchesXInputSlot(a, ordinal - 1)).ToList();
                if (bySlot.Count == 1)
                {
                    return bySlot[0];
                }
            }

            int sessionSlot;
            if (TryParseXInputSlot(sessionKey, out sessionSlot))
            {
                var bySlot = candidates.Where(a => MatchesXInputSlot(a, sessionSlot)).ToList();
                if (bySlot.Count == 1)
                {
                    return bySlot[0];
                }

                var byOrdinal = candidates.Where(a => MatchesHardwareOrdinal(a, sessionSlot + 1))
                    .ToList();
                if (byOrdinal.Count == 1)
                {
                    return byOrdinal[0];
                }
            }

            return null;
        }

        private static bool MatchesHardwareOrdinal(ControllerDeviceSnapshot device, int ordinal)
        {
            if (device == null || ordinal <= 0)
            {
                return false;
            }

            ushort vendorId;
            ushort productId;
            int deviceOrdinal;
            return ControllerTransportPolicy.TryReadHardwareOrdinal(GetDeviceKey(device), out vendorId,
                out productId, out deviceOrdinal) &&
                deviceOrdinal == ordinal;
        }

        private static bool MatchesXInputSlot(ControllerDeviceSnapshot device, int slot)
        {
            int parsed;
            return TryGetXInputSlot(device, out parsed) && parsed == slot;
        }

        private static bool TryGetXInputSlot(ControllerDeviceSnapshot device, out int slot)
        {
            slot = -1;
            if (device == null)
            {
                return false;
            }

            if (string.Equals(device.ProviderId, "XInput", StringComparison.OrdinalIgnoreCase) &&
                device.ProviderInstanceId >= 0 && device.ProviderInstanceId <= 3)
            {
                slot = device.ProviderInstanceId;
                return true;
            }

            return TryParseXInputSlot(device.HardwareId, out slot) ||
                TryParseXInputSlot(device.ControllerId, out slot);
        }

        private static string GetDeviceKey(ControllerDeviceSnapshot device)
        {
            return string.IsNullOrWhiteSpace(device.HardwareId) ? device.ControllerId : device.HardwareId;
        }

        internal static bool TryParseXInputSlot(string value, out int slot)
        {
            slot = -1;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            const string prefix = "xinput:slot:";
            if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int parsed;
            if (!int.TryParse(value.Substring(prefix.Length),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out parsed) ||
                parsed < 0 || parsed > 3)
            {
                return false;
            }

            slot = parsed;
            return true;
        }
    }
}
