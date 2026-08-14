using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ControllerSessionManager.Controllers
{
    public sealed class XInputProvider
    {
        public const string ProviderId = "XInput";
        private const uint ErrorSuccess = 0;
        private const byte BatteryDeviceTypeGamepad = 0;
        private const byte BatteryTypeWired = 1;
        private const byte BatteryTypeAlkaline = 2;
        private const byte BatteryTypeNimh = 3;
        private readonly SlotState[] slots = new SlotState[4];
        private bool unavailable;

        public XInputProvider()
        {
            for (var index = 0; index < slots.Length; index++)
            {
                slots[index] = new SlotState();
            }
        }

        public bool IsAvailable
        {
            get { return !unavailable; }
        }

        public IReadOnlyList<ControllerDeviceSnapshot> Poll()
        {
            var result = new List<ControllerDeviceSnapshot>();
            if (unavailable)
            {
                return result;
            }

            try
            {
                for (uint index = 0; index < 4; index++)
                {
                    XInputState state;
                    if (NativeMethods.XInputGetState(index, out state) != ErrorSuccess)
                    {
                        slots[index].WasConnected = false;
                        continue;
                    }

                    var now = DateTime.UtcNow;
                    var slot = slots[index];
                    if (slot.WasConnected && slot.PacketNumber != state.PacketNumber)
                    {
                        slot.LastInputUtc = now;
                    }

                    slot.WasConnected = true;
                    slot.PacketNumber = state.PacketNumber;

                    XInputBatteryInformation battery;
                    var batteryResult = NativeMethods.XInputGetBatteryInformation(
                        index, BatteryDeviceTypeGamepad, out battery);
                    var wired = batteryResult == ErrorSuccess && battery.BatteryType == BatteryTypeWired;
                    var wireless = batteryResult == ErrorSuccess &&
                        (battery.BatteryType == BatteryTypeAlkaline || battery.BatteryType == BatteryTypeNimh);
                    result.Add(new ControllerDeviceSnapshot
                    {
                        ControllerId = string.Format("xinput:slot:{0}", index),
                        ProviderId = ProviderId,
                        ProviderInstanceId = (int)index,
                        Name = string.Format("XInput Controller (Player {0})", index + 1),
                        Path = string.Empty,
                        IsConnected = true,
                        IsEnabled = true,
                        ConnectionType = wired ? "Wired" : wireless ? "Wireless" : "Unknown",
                        BatteryLevel = GetBatteryLevel(batteryResult, battery, wired, wireless),
                        LastSeenUtc = now,
                        LastInputUtc = slot.LastInputUtc
                    });
                }
            }
            catch (DllNotFoundException)
            {
                unavailable = true;
            }
            catch (EntryPointNotFoundException)
            {
                unavailable = true;
            }

            return result;
        }

        private static string GetBatteryLevel(uint result, XInputBatteryInformation battery, bool wired, bool wireless)
        {
            if (result != ErrorSuccess)
            {
                return "Unknown";
            }

            if (wired)
            {
                return "Wired";
            }

            if (!wireless)
            {
                return "Unknown";
            }

            switch (battery.BatteryLevel)
            {
                case 0:
                    return "Empty";
                case 1:
                    return "Low";
                case 2:
                    return "Medium";
                case 3:
                    return "Full";
                default:
                    return "Unknown";
            }
        }

        private sealed class SlotState
        {
            public bool WasConnected;
            public uint PacketNumber;
            public DateTime? LastInputUtc;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputGamepad
        {
            public ushort Buttons;
            public byte LeftTrigger;
            public byte RightTrigger;
            public short LeftThumbX;
            public short LeftThumbY;
            public short RightThumbX;
            public short RightThumbY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputState
        {
            public uint PacketNumber;
            public XInputGamepad Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputBatteryInformation
        {
            public byte BatteryType;
            public byte BatteryLevel;
        }

        private static class NativeMethods
        {
            [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
            public static extern uint XInputGetState(uint userIndex, out XInputState state);

            [DllImport("xinput1_4.dll", EntryPoint = "XInputGetBatteryInformation")]
            public static extern uint XInputGetBatteryInformation(
                uint userIndex,
                byte deviceType,
                out XInputBatteryInformation batteryInformation);
        }
    }
}
