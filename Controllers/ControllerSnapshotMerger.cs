using System;
using System.Collections.Generic;
using System.Linq;

namespace ControllerSessionManager.Controllers
{
    /// <summary>
    /// Projects provider observations onto Playnite's controller lifecycle. Provider rows can
    /// enrich identity and capabilities, but they cannot override Playnite's connected state.
    /// </summary>
    public static class ControllerSnapshotMerger
    {
        public const string PlayniteProviderId = "Playnite";
        private const string XInputProviderId = "XInput";
        private const string SdlProviderId = "SDL";

        public static IReadOnlyList<ControllerDeviceSnapshot> Merge(
            IEnumerable<ControllerDeviceSnapshot> devices, bool playniteAuthorityInitialized)
        {
            var source = (devices ?? Enumerable.Empty<ControllerDeviceSnapshot>()).
                Where(a => a != null).ToList();
            var playnite = source.Where(IsPlaynite).ToList();
            var supplemental = source.Where(a => !IsPlaynite(a)).ToList();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<ControllerDeviceSnapshot>();

            foreach (var authoritative in playnite)
            {
                var capability = FindCapability(authoritative, supplemental);
                if (capability != null)
                {
                    used.Add(capability.ControllerId ?? string.Empty);
                }
                result.Add(MergeOne(authoritative, capability));
            }

            // Before the SDK has supplied a usable controller record, provider observations
            // remain a degraded fallback (for example when Desktop controller support is off).
            // As soon as Playnite owns any lifecycle rows, unmatched observations stay hidden.
            if (!playniteAuthorityInitialized || playnite.Count == 0)
            {
                result.AddRange(supplemental.Where(a => !used.Contains(a.ControllerId ?? string.Empty))
                    .Select(a => a.Clone()));
            }

            return result.OrderByDescending(a => a.IsConnected)
                .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        internal static ControllerDeviceSnapshot FindCapability(ControllerDeviceSnapshot authoritative,
            IEnumerable<ControllerDeviceSnapshot> supplemental)
        {
            if (authoritative == null)
            {
                return null;
            }

            var candidates = (supplemental ?? Enumerable.Empty<ControllerDeviceSnapshot>()).ToList();
            var slot = ControllerBridgeIdentity.GetXInputSlot(authoritative.Path);
            if (slot.HasValue)
            {
                return candidates.FirstOrDefault(a =>
                    string.Equals(a.ProviderId, XInputProviderId,
                        StringComparison.OrdinalIgnoreCase) && a.ProviderInstanceId == slot.Value);
            }

            var pathMatch = candidates.FirstOrDefault(a =>
                ControllerBridgeIdentity.PathsReferToSameDevice(authoritative.Path, a.Path));
            if (pathMatch != null)
            {
                return pathMatch;
            }

            ushort vendorId;
            ushort productId;
            if (!ControllerBridgeIdentity.TryGetVidPid(authoritative.Path, out vendorId,
                out productId))
            {
                vendorId = authoritative.VendorId;
                productId = authoritative.ProductId;
            }
            if (vendorId != 0 && productId != 0)
            {
                var hardwareMatches = candidates.Where(a =>
                {
                    ushort candidateVendor;
                    ushort candidateProduct;
                    var hasPathIds = ControllerBridgeIdentity.TryGetVidPid(a.Path,
                        out candidateVendor, out candidateProduct);
                    return (hasPathIds ? candidateVendor : a.VendorId) == vendorId &&
                        (hasPathIds ? candidateProduct : a.ProductId) == productId;
                }).ToList();
                if (hardwareMatches.Count == 1)
                {
                    return hardwareMatches[0];
                }

                var namedHardwareMatches = hardwareMatches.Where(a =>
                    !string.IsNullOrWhiteSpace(authoritative.DetectedName) &&
                    string.Equals(a.DetectedName, authoritative.DetectedName,
                        StringComparison.CurrentCultureIgnoreCase)).ToList();
                if (namedHardwareMatches.Count == 1)
                {
                    return namedHardwareMatches[0];
                }
            }
            // Playnite's non-XInput controller instance IDs originate from its SDL registry.
            // Never compare an SDL instance ID with an XInput slot merely because both are ints.
            return candidates.FirstOrDefault(a =>
                string.Equals(a.ProviderId, SdlProviderId,
                    StringComparison.OrdinalIgnoreCase) &&
                authoritative.ProviderInstanceId >= 0 &&
                a.ProviderInstanceId == authoritative.ProviderInstanceId);
        }

        internal static ControllerDeviceSnapshot FindAuthoritativeEventTarget(
            ControllerDeviceSnapshot incoming, IEnumerable<ControllerDeviceSnapshot> authoritative)
        {
            if (incoming == null)
            {
                return null;
            }

            var candidates = (authoritative ?? Enumerable.Empty<ControllerDeviceSnapshot>())
                .Where(IsPlaynite).ToList();
            var exact = candidates.FirstOrDefault(a => string.Equals(a.ControllerId,
                incoming.ControllerId, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }

            var slot = ControllerBridgeIdentity.GetXInputSlot(incoming.Path);
            if (slot.HasValue)
            {
                return candidates.FirstOrDefault(a =>
                    ControllerBridgeIdentity.GetXInputSlot(a.Path) == slot);
            }

            var path = candidates.FirstOrDefault(a =>
                ControllerBridgeIdentity.PathsReferToSameDevice(incoming.Path, a.Path));
            if (path != null)
            {
                return path;
            }

            var instance = candidates.Where(a => incoming.ProviderInstanceId >= 0 &&
                a.ProviderInstanceId == incoming.ProviderInstanceId).ToList();
            if (instance.Count == 1)
            {
                return instance[0];
            }

            var name = candidates.Where(a => a.IsConnected &&
                !string.IsNullOrWhiteSpace(incoming.DetectedName) &&
                string.Equals(a.DetectedName, incoming.DetectedName,
                    StringComparison.CurrentCultureIgnoreCase)).ToList();
            return name.Count == 1 ? name[0] : null;
        }

        private static ControllerDeviceSnapshot MergeOne(ControllerDeviceSnapshot authoritative,
            ControllerDeviceSnapshot capability)
        {
            var result = authoritative.Clone();
            result.LifecycleProviderId = PlayniteProviderId;
            if (capability == null)
            {
                return result;
            }

            // ProviderId remains the actionable capability provider (rumble/profile slot), while
            // LifecycleProviderId records that connection state belongs to Playnite.
            result.ProviderId = capability.ProviderId;
            result.ProviderInstanceId = capability.ProviderInstanceId;
            result.Name = Prefer(capability.Name, result.Name);
            result.DetectedName = Prefer(capability.DetectedName, result.DetectedName);
            result.HardwareId = Prefer(capability.HardwareId, result.HardwareId);
            result.VendorId = capability.VendorId != 0 ? capability.VendorId : result.VendorId;
            result.ProductId = capability.ProductId != 0 ? capability.ProductId : result.ProductId;
            result.Path = Prefer(capability.Path, result.Path);
            result.ConnectionType = PreferKnown(capability.ConnectionType, result.ConnectionType);
            result.BatteryLevel = PreferKnown(capability.BatteryLevel, result.BatteryLevel);
            result.BatteryProviderId = PreferKnown(capability.BatteryProviderId,
                result.BatteryProviderId);
            result.LastSeenUtc = capability.LastSeenUtc > result.LastSeenUtc
                ? capability.LastSeenUtc : result.LastSeenUtc;

            if (capability.LastInputUtc.HasValue &&
                (!result.LastInputUtc.HasValue || capability.LastInputUtc > result.LastInputUtc))
            {
                result.LastInputUtc = capability.LastInputUtc;
                result.LastInputKind = capability.LastInputKind;
                result.IsInputNeutral = capability.IsInputNeutral;
                result.InputNeutralSinceUtc = capability.InputNeutralSinceUtc;
            }

            // Deliberately retained from the SDK record, regardless of provider polling.
            result.IsConnected = authoritative.IsConnected;
            result.IsEnabled = authoritative.IsEnabled;
            return result;
        }

        private static bool IsPlaynite(ControllerDeviceSnapshot value)
        {
            return value != null && string.Equals(value.ProviderId, PlayniteProviderId,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string Prefer(string candidate, string fallback)
        {
            return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
        }

        private static string PreferKnown(string candidate, string fallback)
        {
            return string.IsNullOrWhiteSpace(candidate) ||
                string.Equals(candidate, "Unknown", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate, "None", StringComparison.OrdinalIgnoreCase)
                ? fallback : candidate;
        }
    }
}
