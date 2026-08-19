namespace ControllerSessionManager.Tester.Models
{
    public sealed class GamepadControllerInfo
    {
        public int JoystickIndex { get; set; }
        public int InstanceId { get; set; }
        public string Name { get; set; }
        public ushort VendorId { get; set; }
        public ushort ProductId { get; set; }
        public GamepadLayout Layout { get; set; }
        public EightBitDoModel EightBitDoModel { get; set; }

        public string DisplayName
        {
            get
            {
                return GamepadDeviceNames.GetDisplayName(Name, VendorId, ProductId, Layout, EightBitDoModel);
            }
        }

        public string DeviceLabel
        {
            get
            {
                if (VendorId == 0)
                {
                    return string.Format("{0}", Name ?? Layout.ToString());
                }

                if (Layout == GamepadLayout.EightBitDo)
                {
                    return string.Format("{0} - VID: {1:X4}  PID: {2:X4}", Name ?? "SDL GameController", VendorId, ProductId);
                }

                return string.Format("{0} - VID: {1:X4}  PID: {2:X4}", Name ?? Layout.ToString(), VendorId, ProductId);
            }
        }
    }
}
