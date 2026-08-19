using System;
using System.Collections.Generic;
using System.Linq;

namespace ControllerSessionManager.Controllers
{
    /// <summary>
    /// Projects a stable UI pad while Playnite/XInput bounce through dongle and Bluetooth
    /// identities. 8BitDo Wireless and Bluetooth of the same model are one card whose
    /// connection type updates in place, like Windows.
    /// Overlay and session tracking still use the live snapshot.
    /// Mandos shows every distinct connected pad (1..N); transport aliases of the same
    /// physical controller stay collapsed to one card.
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
                .Select(a => ControllerDeviceIdentity.GetModelKey(a))
                .OrderBy(a => a, StringComparer.OrdinalIgnoreCase));
        }

        public IReadOnlyList<ControllerDeviceSnapshot> Apply(
            IReadOnlyList<ControllerDeviceSnapshot> live, DateTime nowUtc)
        {
            var connected = (live ?? Enumerable.Empty<ControllerDeviceSnapshot>())
                .Where(a => a != null && a.IsConnected)
                .Select(a => a.Clone())
                .ToList();
            // VID-less ghost XInput slots are not displayable; every pad with a real vendor
            // id is listed so Mandos matches the physical set (1..N controllers).
            var identified = CollapseTransportAliases(connected.Where(a => a.VendorId != 0).ToList());
            if (identified.Count > 0)
            {
                lastPresenceUtc = nowUtc;
                if (TryApplySameModelUpdate(identified))
                {
                    pendingSignature = null;
                    return CloneAll(held);
                }

                var signature = IdentitySignature(identified);
                var heldSignature = IdentitySignature(held);
                if (held.Count == 0 ||
                    string.Equals(signature, heldSignature, StringComparison.OrdinalIgnoreCase) ||
                    IsIdentitySuperset(identified, held))
                {
                    // Newly connected pads appear immediately; only shrink/replace waits.
                    held = CloneAll(identified);
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
                    held = CloneAll(identified);
                    pendingSignature = null;
                    return CloneAll(held);
                }

                return CloneAll(held);
            }

            pendingSignature = null;
            if (connected.Count > 0)
            {
                lastPresenceUtc = nowUtc;
                if (TryApplySameModelUpdate(CollapseTransportAliases(connected)))
                {
                    return CloneAll(held);
                }
            }

            if (held.Count > 0 && nowUtc - lastPresenceUtc < HoldDuration)
            {
                return CloneAll(held);
            }

            held = new ControllerDeviceSnapshot[0];
            return connected;
        }

        internal static List<ControllerDeviceSnapshot> CollapseTransportAliases(
            IList<ControllerDeviceSnapshot> settled)
        {
            var result = new List<ControllerDeviceSnapshot>();
            if (settled == null)
            {
                return result;
            }

            var remaining = settled.Where(a => a != null).ToList();
            while (remaining.Count > 0)
            {
                var current = remaining[0];
                remaining.RemoveAt(0);
                var aliases = remaining.Where(a =>
                    ControllerTransportPolicy.ShouldCollapse(current, a)).ToList();
                foreach (var alias in aliases)
                {
                    remaining.Remove(alias);
                }

                if (aliases.Count == 0)
                {
                    result.Add(current);
                    continue;
                }

                aliases.Add(current);
                result.Add(ControllerTransportPolicy.SelectCanonical(aliases) ?? aliases[0]);
            }

            return result;
        }

        private static bool IsIdentitySuperset(IList<ControllerDeviceSnapshot> candidate,
            IReadOnlyList<ControllerDeviceSnapshot> current)
        {
            if (candidate == null || current == null || candidate.Count <= current.Count)
            {
                return false;
            }

            var heldKeys = new HashSet<string>(
                current.Where(a => a != null).Select(a => ControllerDeviceIdentity.GetModelKey(a)),
                StringComparer.OrdinalIgnoreCase);
            heldKeys.Remove(string.Empty);
            if (heldKeys.Count == 0)
            {
                return true;
            }

            var nextKeys = new HashSet<string>(
                candidate.Where(a => a != null).Select(a => ControllerDeviceIdentity.GetModelKey(a)),
                StringComparer.OrdinalIgnoreCase);
            nextKeys.Remove(string.Empty);
            return heldKeys.IsSubsetOf(nextKeys);
        }

        private bool TryApplySameModelUpdate(IList<ControllerDeviceSnapshot> settled)
        {
            if (held.Count != 1 || settled == null || settled.Count != 1)
            {
                return false;
            }

            var current = held[0];
            var incoming = settled[0];
            if (current.VendorId == 0 || incoming.VendorId == 0 ||
                current.VendorId != incoming.VendorId)
            {
                return false;
            }

            var next = incoming.Clone();
            next.HardwareId = current.HardwareId;
            if (ControllerDeviceIdentity.IsGenericDisplayName(next.Name) &&
                !ControllerDeviceIdentity.IsGenericDisplayName(current.Name))
            {
                next.Name = current.Name;
            }
            if (ControllerDeviceIdentity.IsGenericDisplayName(next.DetectedName) &&
                !ControllerDeviceIdentity.IsGenericDisplayName(current.DetectedName))
            {
                next.DetectedName = current.DetectedName;
            }

            held = new[] { next };
            return true;
        }

        private static IReadOnlyList<ControllerDeviceSnapshot> CloneAll(
            IEnumerable<ControllerDeviceSnapshot> source)
        {
            return source.Select(a => a.Clone()).ToList();
        }
    }
}
