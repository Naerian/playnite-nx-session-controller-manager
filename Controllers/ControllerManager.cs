using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK.Events;

namespace ControllerSessionManager.Controllers
{
    public sealed class ControllerManager
    {
        private const string ProviderId = "Playnite";
        private readonly object syncRoot = new object();
        private readonly Dictionary<string, ControllerDeviceSnapshot> devices =
            new Dictionary<string, ControllerDeviceSnapshot>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler SnapshotChanged;

        public IReadOnlyList<ControllerDeviceSnapshot> GetSnapshot()
        {
            lock (syncRoot)
            {
                return devices.Values
                    .Where(a => !IsRedundantPlayniteBridge(a))
                    .OrderByDescending(a => a.IsConnected)
                    .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(a => a.Clone())
                    .ToList();
            }
        }

        public void Reconcile(IEnumerable<GamepadController> connectedControllers)
        {
            var now = DateTime.UtcNow;
            var observedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            lock (syncRoot)
            {
                foreach (var controller in connectedControllers ?? Enumerable.Empty<GamepadController>())
                {
                    if (IsXInputBridge(controller))
                    {
                        // XInputProvider is the authoritative source for these devices. The
                        // Playnite bridge can disappear and return with a different InstanceId
                        // during one hot-plug, which otherwise creates generic duplicate rows
                        // and inverted connection notifications.
                        continue;
                    }
                    var key = GetProviderKey(controller);
                    observedKeys.Add(key);
                    UpsertConnected(controller, key, now);
                }

                foreach (var pair in devices)
                {
                    if (pair.Value.ProviderId == ProviderId && !observedKeys.Contains(pair.Key))
                    {
                        pair.Value.IsConnected = false;
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
            }

            RaiseSnapshotChanged();
        }

        public void RecordConnected(GamepadController controller)
        {
            if (controller == null || IsXInputBridge(controller))
            {
                return;
            }

            lock (syncRoot)
            {
                if (FindProviderMatch(controller) != null)
                {
                    return;
                }
                UpsertConnected(controller, GetProviderKey(controller), DateTime.UtcNow);
            }

            RaiseSnapshotChanged();
        }

        public void RecordDisconnected(GamepadController controller)
        {
            if (controller == null || IsXInputBridge(controller))
            {
                return;
            }

            lock (syncRoot)
            {
                if (FindProviderMatch(controller) != null)
                {
                    return;
                }
                var key = GetProviderKey(controller);
                ControllerDeviceSnapshot device;
                if (!devices.TryGetValue(key, out device))
                {
                    device = CreateSnapshot(controller, key, DateTime.UtcNow);
                    devices[key] = device;
                }

                device.IsConnected = false;
                device.LastSeenUtc = DateTime.UtcNow;
            }

            RaiseSnapshotChanged();
        }

        public void RecordInput(GamepadController controller)
        {
            if (controller == null)
            {
                return;
            }

            lock (syncRoot)
            {
                var now = DateTime.UtcNow;
                var providerMatch = FindProviderMatch(controller);
                if (providerMatch == null && IsXInputBridge(controller))
                {
                    return;
                }
                if (providerMatch != null)
                {
                    providerMatch.LastInputUtc = now;
                    providerMatch.LastInputKind = InputEvidenceKind.PlayniteButton.ToString();
                    providerMatch.IsInputNeutral = false;
                    providerMatch.InputNeutralSinceUtc = null;
                    providerMatch.LastSeenUtc = now;
                }
                else
                {
                    var key = GetProviderKey(controller);
                    UpsertConnected(controller, key, now);
                    devices[key].LastInputUtc = now;
                    devices[key].LastInputKind = InputEvidenceKind.PlayniteButton.ToString();
                }
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

            device.Name = string.IsNullOrWhiteSpace(controller.Name) ? "Unknown controller" : controller.Name;
            device.Path = controller.Path ?? string.Empty;
            device.ProviderInstanceId = controller.InstanceId;
            device.IsEnabled = controller.Enabled;
            device.IsConnected = true;
            device.LastSeenUtc = now;
        }

        private static ControllerDeviceSnapshot CreateSnapshot(GamepadController controller, string key, DateTime now)
        {
            return new ControllerDeviceSnapshot
            {
                ControllerId = key,
                ProviderId = ProviderId,
                ProviderInstanceId = controller.InstanceId,
                Name = string.IsNullOrWhiteSpace(controller.Name) ? "Unknown controller" : controller.Name,
                DetectedName = string.IsNullOrWhiteSpace(controller.Name) ? "Unknown controller" : controller.Name,
                HardwareId = key,
                Path = controller.Path ?? string.Empty,
                IsConnected = true,
                IsEnabled = controller.Enabled,
                ConnectionType = "Unknown",
                BatteryLevel = "Unknown",
                BatteryProviderId = "None",
                LastSeenUtc = now
            };
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

        private static bool IsXInputBridge(GamepadController controller)
        {
            return ControllerBridgeIdentity.GetXInputSlot(controller == null ? null : controller.Path).HasValue;
        }

        private ControllerDeviceSnapshot FindProviderMatch(GamepadController controller)
        {
            var slot = ControllerBridgeIdentity.GetXInputSlot(controller == null ? null : controller.Path);
            if (slot.HasValue)
            {
                return devices.Values.FirstOrDefault(a => a.IsConnected &&
                    string.Equals(a.ProviderId, XInputProvider.ProviderId, StringComparison.OrdinalIgnoreCase) &&
                    a.ProviderInstanceId == slot.Value);
            }

            return devices.Values.FirstOrDefault(a => a.ProviderId != ProviderId && a.IsConnected &&
                a.ProviderInstanceId == controller.InstanceId);
        }

        private bool IsRedundantPlayniteBridge(ControllerDeviceSnapshot snapshot)
        {
            if (snapshot == null || !string.Equals(snapshot.ProviderId, ProviderId,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var slot = ControllerBridgeIdentity.GetXInputSlot(snapshot.Path);
            return slot.HasValue && devices.Values.Any(a => a.IsConnected &&
                string.Equals(a.ProviderId, XInputProvider.ProviderId, StringComparison.OrdinalIgnoreCase) &&
                a.ProviderInstanceId == slot.Value);
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
