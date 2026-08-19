namespace ControllerSessionManager.Tester.Models
{
    using System.Collections.Generic;

    public sealed class GamepadState
    {
        public bool IsConnected { get; set; }
        public string ControllerName { get; set; }
        public ushort VendorId { get; set; }
        public ushort ProductId { get; set; }
        public GamepadLayout Layout { get; set; }
        public EightBitDoModel EightBitDoModel { get; set; }
        public string SdlVersion { get; set; }
        public string SdlGuid { get; set; }
        public string SdlMapping { get; set; }
        public int AxisCount { get; set; }
        public int ButtonCount { get; set; }
        public int HatCount { get; set; }
        public StickState LeftStick { get; set; }
        public StickState RightStick { get; set; }
        public float LeftTrigger { get; set; }
        public float RightTrigger { get; set; }
        public GamepadButtonState Buttons { get; set; }
        public List<ExtraButtonState> ExtraButtons { get; set; }

        public GamepadState()
        {
            ControllerName = "No controller detected";
            SdlVersion = "Unknown";
            SdlGuid = "Unavailable";
            SdlMapping = "Unavailable";
            Layout = GamepadLayout.Unknown;
            LeftStick = new StickState();
            RightStick = new StickState();
            Buttons = new GamepadButtonState();
            ExtraButtons = new List<ExtraButtonState>();
        }
    }
}
