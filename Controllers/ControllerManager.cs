using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK.Events;

namespace ControllerSessionManager.Controllers
{
    public sealed class ControllerManager
    {
        private const string ProviderId = ControllerSnapshotMerger.PlayniteProviderId;
        private readonly object syncRoot = new object();
        private readonly Dictionary<string, ControllerDeviceSnapshot> devices =
            new Dictionary<string, ControllerDeviceSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> missingInventoryObservations =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> missingProviderObservations =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> presentProviderObservations =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> providerFallbackDisconnected =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool playniteAuthorityInitialized;

        public event EventHandler SnapshotChanged;

        public IReadOnlyList<ControllerDeviceSnapshot> GetSnapshot()
        {
            lock (syncRoot)
            {
                return ControllerSnapshotMerger.Merge(devices.Values,
                    playniteAuthorityInitialized);
            }
        }

        public void Reconcile(IEnumerable<GamepadController> connectedControllers)
        {
            var now = DateTime.UtcNow;
            var observedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            lock (syncRoot)
            {
                playniteAuthorityInitialized = true;
                foreach (var controller in connectedControllers ?? Enumerable.Empty<GamepadController>())
                {
                    ushort vendorId;
                    ushort productId;
                    ControllerBridgeIdentity.TryGetVidPid(controller.Path, out vendorId, out productId);
                    if (!ControllerDeviceIdentity.ShouldAcceptPlayniteInventory(controller.Name,
                        controller.Path, vendorId, productId))
                    {
                        continue;
                    }

                    var key = GetProviderKey(controller);
                    observedKeys.Add(key);
                    missingInventoryObservations.Remove(key);
                    UpsertConnected(controller, key, now);
                }

                foreach (var pair in devices)
                {
                    if (pair.Value.ProviderId == ProviderId && !observedKeys.Contains(pair.Key))
                    {
                        int misses;
                        missingInventoryObservations.TryGetValue(pair.Key, out misses);
                        misses++;
                        missingInventoryObservations[pair.Key] = misses;

                        // SDK callbacks are authoritative and normally mark a disconnect
                        // immediately. Inventory absence is only a recovery path and requires
                        // two consecutive observations with no still-connected capability.
                        var capability = ControllerSnapshotMerger.FindCapability(pair.Value,
                            devices.Values.Where(a => a.ProviderId != ProviderId));
                        if (misses >= 2 && (capability == null || !capability.IsConnected))
                        {
                            pair.Value.IsConnected = false;
                        }
                    }
                }
            }

            RaiseSnapshotChanged();
        }

        public void ReconcileProvider(string providerId, IEnumerable<ControllerDeviceSnapshot> observations)
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                throw new ArgumentException("A provider id is required.", "providerId");
            }

            var now = DateTime.UtcNow;
            var observedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            lock (syncRoot)
            {
                foreach (var observation in observations ?? Enumerable.Empty<ControllerDeviceSnapshot>())
                {
                    var key = observation.ControllerId;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    observedKeys.Add(key);
                    ControllerDeviceSnapshot existing;
                    if (!devices.TryGetValue(key, out existing))
                    {
                        existing = observation.Clone();
                        devices[key] = existing;
                    }
                    else
                    {
                        existing.ProviderId = providerId;
                        existing.ProviderInstanceId = observation.ProviderInstanceId;
                        existing.Name = observation.Name;
                        existing.DetectedName = observation.DetectedName;
                        existing.HardwareId = observation.HardwareId;
                        existing.VendorId = observation.VendorId;
                        existing.ProductId = observation.ProductId;
                        existing.Path = observation.Path;
                        existing.IsConnected = observation.IsConnected;
                        existing.IsEnabled = observation.IsEnabled;
                        existing.ConnectionType = observation.ConnectionType;
                        existing.BatteryLevel = observation.BatteryLevel;
                        existing.BatteryProviderId = observation.BatteryProviderId;
                        if (observation.LastInputUtc.HasValue)
                        {
                            existing.LastInputUtc = observation.LastInputUtc;
                            existing.LastInputKind = observation.LastInputKind;
                        }
                        existing.IsInputNeutral = observation.IsInputNeutral;
                        existing.InputNeutralSinceUtc = observation.InputNeutralSinceUtc;
                    }

                    existing.LastSeenUtc = now;
                }

                foreach (var pair in devices)
                {
                    if (pair.Value.ProviderId == providerId && !observedKeys.Contains(pair.Key))
                    {
                        pair.Value.IsConnected = false;
                    }
                }

                ApplyProviderLifecycleFallback(providerId);
            }

            RaiseSnapshotChanged();
        }

        public void RecordConnected(GamepadController controller)
        {
            if (controller == null)
            {
                return;
            }

            ushort vendorId;
            ushort productId;
            ControllerBridgeIdentity.TryGetVidPid(controller.Path, out vendorId, out productId);
            if (!ControllerDeviceIdentity.ShouldAcceptPlayniteInventory(controller.Name,
                controller.Path, vendorId, productId))
            {
                return;
            }

            lock (syncRoot)
            {
                playniteAuthorityInitialized = true;
                var key = GetProviderKey(controller);
                missingInventoryObservations.Remove(key);
                UpsertConnected(controller, key, DateTime.UtcNow);
                providerFallbackDisconnected.Remove(key);
                ClearProviderMissing(key);
            }

            RaiseSnapshotChanged();
        }

        public void RecordDisconnected(GamepadController controller)
        {
            if (controller == null)
            {
                return;
            }

            lock (syncRoot)
            {
                playniteAuthorityInitialized = true;
                var key = GetProviderKey(controller);
                ControllerDeviceSnapshot device;
                if (!devices.TryGetValue(key, out device))
                {
                    var incoming = CreateSnapshot(controller, key, DateTime.UtcNow);
                    device = ControllerSnapshotMerger.FindAuthoritativeEventTarget(incoming,
                        devices.Values.Where(a => a.ProviderId == ProviderId));
                    if (device == null)
                    {
                        device = incoming;
                        devices[key] = device;
                    }
                    else
                    {
                        key = devices.First(a => object.ReferenceEquals(a.Value, device)).Key;
                    }
                }

                var capability = ControllerSnapshotMerger.FindCapability(device,
                    devices.Values.Where(a => a.ProviderId != ProviderId));
                var vendorId = GetVendorId(device);
                var anotherSameVendorConnected = devices.Values.Any(a =>
                    a != null &&
                    !object.ReferenceEquals(a, device) &&
                    string.Equals(a.ProviderId, ProviderId, StringComparison.OrdinalIgnoreCase) &&
                    a.IsConnected &&
                    GetVendorId(a) != 0 &&
                    GetVendorId(a) == vendorId);
                if (!ProviderLifecyclePolicy.ShouldHonorSdkDisconnect(
                    capability != null && capability.IsConnected,
                    ControllerBridgeIdentity.IsXInputWrapperPath(device.Path),
                    anotherSameVendorConnected))
                {
                    return;
                }

                device.IsConnected = false;
                device.LastSeenUtc = DateTime.UtcNow;
                missingInventoryObservations[key] = 2;
                providerFallbackDisconnected.Remove(key);
                ClearProviderMissing(key);
            }

            RaiseSnapshotChanged();
        }

        public void RecordInput(GamepadController controller)
        {
            if (controller == null)
            {
                return;
            }

            ushort vendorId;
            ushort productId;
            ControllerBridgeIdentity.TryGetVidPid(controller.Path, out vendorId, out productId);
            if (!ControllerDeviceIdentity.ShouldAcceptPlayniteInventory(controller.Name,
                controller.Path, vendorId, productId))
            {
                return;
            }

            lock (syncRoot)
            {
                var now = DateTime.UtcNow;
                var providerMatch = FindProviderMatch(controller);
                if (providerMatch != null)
                {
                    providerMatch.LastInputUtc = now;
                    providerMatch.LastInputKind = InputEvidenceKind.PlayniteButton.ToString();
                    providerMatch.IsInputNeutral = false;
                    providerMatch.InputNeutralSinceUtc = null;
                    providerMatch.LastSeenUtc = now;
                }
                var key = GetProviderKey(controller);
                missingInventoryObservations.Remove(key);
                UpsertConnected(controller, key, now);
                devices[key].LastInputUtc = now;
                devices[key].LastInputKind = InputEvidenceKind.PlayniteButton.ToString();
                devices[key].IsInputNeutral = false;
                devices[key].InputNeutralSinceUtc = null;
                playniteAuthorityInitialized = true;
                providerFallbackDisconnected.Remove(key);
                ClearProviderMissing(key);
            }

            RaiseSnapshotChanged();
        }

        private void UpsertConnected(GamepadController controller, string key, DateTime now)
        {
            ControllerDeviceSnapshot device;
            if (!devices.TryGetValue(key, out device))
            {
                device = CreateSnapshot(controller, key, now);
                devices[key] = device;
            }

            device.Name = ResolvePlayniteName(controller);
            device.Path = controller.Path ?? string.Empty;
            device.ProviderInstanceId = controller.InstanceId;
            device.IsEnabled = controller.Enabled;
            device.IsConnected = true;
            device.LifecycleProviderId = ProviderId;
            device.LastSeenUtc = now;
        }

        private static ControllerDeviceSnapshot CreateSnapshot(GamepadController controller, string key, DateTime now)
        {
            ushort vendorId;
            ushort productId;
            ControllerBridgeIdentity.TryGetVidPid(controller.Path, out vendorId, out productId);
            return new ControllerDeviceSnapshot
            {
                ControllerId = key,
                ProviderId = ProviderId,
                LifecycleProviderId = ProviderId,
                ProviderInstanceId = controller.InstanceId,
                Name = ResolvePlayniteName(controller),
                DetectedName = ResolvePlayniteName(controller),
                HardwareId = key,
                VendorId = vendorId,
                ProductId = productId,
                Path = controller.Path ?? string.Empty,
                IsConnected = true,
                IsEnabled = controller.Enabled,
                ConnectionType = ControllerDeviceIdentity.GetConnectionType(controller.Name,
                    vendorId, productId, controller.Path),
                BatteryLevel = "Unknown",
                BatteryProviderId = "None",
                LastSeenUtc = now
            };
        }

        private static string ResolvePlayniteName(GamepadController controller)
        {
            ushort vendorId;
            ushort productId;
            ControllerBridgeIdentity.TryGetVidPid(controller.Path, out vendorId, out productId);
            return ControllerDeviceIdentity.ResolvePlayniteDisplayName(controller.Name,
                vendorId, productId);
        }

        private static ushort GetVendorId(ControllerDeviceSnapshot device)
        {
            if (device == null)
            {
                return 0;
            }

            if (device.VendorId != 0)
            {
                return device.VendorId;
            }

            ushort vendorId;
            ushort productId;
            return ControllerBridgeIdentity.TryGetVidPid(device.Path, out vendorId, out productId)
                ? vendorId
                : (ushort)0;
        }

        private static string GetProviderKey(GamepadController controller)
        {
            var path = NormalizePath(controller.Path);
            return !string.IsNullOrEmpty(path)
                ? string.Format("playnite:path:{0}", path)
                : string.Format("playnite:instance:{0}", controller.InstanceId);
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Trim().Replace('/', '\\').ToUpperInvariant();
        }

        private ControllerDeviceSnapshot FindProviderMatch(GamepadController controller)
        {
            var authority = CreateSnapshot(controller, GetProviderKey(controller), DateTime.UtcNow);
            return ControllerSnapshotMerger.FindCapability(authority,
                devices.Values.Where(a => a.ProviderId != ProviderId));
        }

        public void ConfirmProviderLifecycle()
        {
            var changed = false;
            lock (syncRoot)
            {
                changed |= ApplyProviderLifecycleFallback("XInput");
                changed |= ApplyProviderLifecycleFallback("SDL");
            }
            if (changed)
            {
                RaiseSnapshotChanged();
            }
        }
        private bool ApplyProviderLifecycleFallback(string providerId)
        {
            var changed = false;
            var capabilities = devices.Values.Where(a => string.Equals(a.ProviderId, providerId,
                StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var authorityPair in devices.Where(a => string.Equals(a.Value.ProviderId,
                ProviderId, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                var authorityKey = authorityPair.Key;
                var authority = authorityPair.Value;
                var capability = ControllerSnapshotMerger.FindCapability(authority, capabilities);
                if (capability == null)
                {
                    continue;
                }

                var evidenceKey = authorityKey + "|" + providerId;
                if (capability.IsConnected)
                {
                    missingProviderObservations.Remove(evidenceKey);
                    if (authority.IsConnected)
                    {
                        presentProviderObservations.Remove(evidenceKey);
                        continue;
                    }

                    int presents;
                    presentProviderObservations.TryGetValue(evidenceKey, out presents);
                    presents++;
                    presentProviderObservations[evidenceKey] = presents;
                    if (ProviderLifecyclePolicy.ShouldRestorePlayniteRow(false, true,
                        providerFallbackDisconnected.Contains(authorityKey), presents))
                    {
                        authority.IsConnected = true;
                        authority.LastSeenUtc = DateTime.UtcNow;
                        authority.LifecycleProviderId = ProviderId + "+" + providerId + " recovery";
                        providerFallbackDisconnected.Remove(authorityKey);
                        presentProviderObservations.Remove(evidenceKey);
                        changed = true;
                    }
                    continue;
                }

                presentProviderObservations.Remove(evidenceKey);
                if (!authority.IsConnected)
                {
                    missingProviderObservations.Remove(evidenceKey);
                    continue;
                }

                int misses;
                missingProviderObservations.TryGetValue(evidenceKey, out misses);
                misses++;
                missingProviderObservations[evidenceKey] = misses;
                if (ProviderLifecyclePolicy.ShouldMarkDisconnected(misses))
                {
                    authority.IsConnected = false;
                    authority.LastSeenUtc = DateTime.UtcNow;
                    authority.LifecycleProviderId = ProviderId + "+" + providerId + " recovery";
                    providerFallbackDisconnected.Add(authorityKey);
                    changed = true;
                }
            }
            return changed;
        }

        private void ClearProviderMissing(string authorityKey)
        {
            var prefix = authorityKey + "|";
            foreach (var key in missingProviderObservations.Keys.Where(a =>
                a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                missingProviderObservations.Remove(key);
            }
            foreach (var key in presentProviderObservations.Keys.Where(a =>
                a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                presentProviderObservations.Remove(key);
            }
        }
        private void RaiseSnapshotChanged()
        {
            var handler = SnapshotChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
