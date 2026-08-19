using System;
using System.Collections.Generic;
using System.Linq;

namespace ControllerSessionManager.Controllers
{
    /// <summary>
    /// Holds the last settled connected pad for UI surfaces while 8BitDo-style
    /// Wireless/Bluetooth switches bounce through empty, VID-less, or alternate
    /// hardware identities. Overlay and session tracking still use the live snapshot.
    /// </summary>
    public sealed class ControllerDisplayHold
    {
        public static readonly TimeSpan HoldDuration = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan StableDuration = TimeSpan.FromSeconds(1);

        private IReadOnlyList<ControllerDeviceSnapshot> held = new ControllerDeviceSnapshot[0];
        private DateTime lastPresenceUtc;
        private string pendingSignature;
        private DateTime pendingSinceUtc;

        public static bool HasSettledIdentity(ControllerDeviceSnapshot controller)
        {
            return controller != null &&
                controller.IsConnected &&
                controller.VendorId != 0 &&
                !ControllerBridgeIdentity.IsVolatileHardwareId(controller.HardwareId);
        }

        public static bool ShouldSyncProfile(ControllerDeviceSnapshot controller)
        {
            return HasSettledIdentity(controller);
        }

        public static string IdentitySignature(IEnumerable<ControllerDeviceSnapshot> pads)
        {
            return string.Join("|", (pads ?? Enumerable.Empty<ControllerDeviceSnapshot>())
                .Where(a => a != null)
                .Select(a => a.HardwareId ?? a.ControllerId ?? string.Empty)
                .OrderBy(a => a, StringComparer.OrdinalIgnoreCase));
        }

        public IReadOnlyList<ControllerDeviceSnapshot> Apply(
            IReadOnlyList<ControllerDeviceSnapshot> live, DateTime nowUtc)
        {
            var connected = (live ?? Enumerable.Empty<ControllerDeviceSnapshot>())
                .Where(a => a != null && a.IsConnected)
                .Select(a => a.Clone())
                .ToList();
            var settled = connected.Where(HasSettledIdentity).ToList();
            if (settled.Count > 0)
            {
                lastPresenceUtc = nowUtc;
                var signature = IdentitySignature(settled);
                var heldSignature = IdentitySignature(held);
                if (held.Count == 0 ||
                    string.Equals(signature, heldSignature, StringComparison.OrdinalIgnoreCase))
                {
                    held = CloneAll(settled);
                    pendingSignature = null;
                    return CloneAll(held);
                }

                if (!string.Equals(pendingSignature, signature, StringComparison.OrdinalIgnoreCase))
                {
                    pendingSignature = signature;
                    pendingSinceUtc = nowUtc;
                }

                if (nowUtc - pendingSinceUtc >= StableDuration)
                {
                    held = CloneAll(settled);
                    pendingSignature = null;
                    return CloneAll(held);
                }

                return CloneAll(held);
            }

            pendingSignature = null;
            if (connected.Count > 0)
            {
                lastPresenceUtc = nowUtc;
            }

            if (held.Count > 0 && nowUtc - lastPresenceUtc < HoldDuration)
            {
                return CloneAll(held);
            }

            held = new ControllerDeviceSnapshot[0];
            return connected;
        }

        private static IReadOnlyList<ControllerDeviceSnapshot> CloneAll(
            IEnumerable<ControllerDeviceSnapshot> source)
        {
            return source.Select(a => a.Clone()).ToList();
        }
    }
}
