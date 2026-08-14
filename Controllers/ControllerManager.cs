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

        public void RecordConnected(GamepadController controller)
        {
            if (controller == null)
            {
                return;
            }

            lock (syncRoot)
            {
                UpsertConnected(controller, GetProviderKey(controller), DateTime.UtcNow);
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
                var key = GetProviderKey(controller);
                UpsertConnected(controller, key, now);
                devices[key].LastInputUtc = now;
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
                Path = controller.Path ?? string.Empty,
                IsConnected = true,
                IsEnabled = controller.Enabled,
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

