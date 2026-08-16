using System;

namespace ControllerSessionManager.Controllers
{
    public sealed class ControllerMetadata
    {
        public int Index { get; set; }
        public int InstanceId { get; set; }
        public int PlayerIndex { get; set; }
        public string RawName { get; set; }
        public string DevicePath { get; set; }
        public string DisplayName { get; set; }
        public ushort VendorId { get; set; }
        public ushort ProductId { get; set; }
        public string HardwareId { get; set; }
        public string ConnectionType { get; set; }
        public string BatteryLevel { get; set; }
        public DateTime? LastInputUtc { get; set; }
        public string LastInputKind { get; set; }
        public bool? IsInputNeutral { get; set; }
        public DateTime? InputNeutralSinceUtc { get; set; }
    }
}
