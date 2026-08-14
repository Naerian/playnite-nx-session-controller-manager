using System;

namespace ControllerSessionManager.Controllers
{
    public sealed class ControllerDeviceSnapshot
    {
        public string ControllerId { get; set; }

        public string ProviderId { get; set; }

        public int ProviderInstanceId { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public bool IsConnected { get; set; }

        public bool IsEnabled { get; set; }

        public string ConnectionType { get; set; }

        public string BatteryLevel { get; set; }

        public DateTime LastSeenUtc { get; set; }

        public DateTime? LastInputUtc { get; set; }

        public ControllerDeviceSnapshot Clone()
        {
            return (ControllerDeviceSnapshot)MemberwiseClone();
        }
    }
}
