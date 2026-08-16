using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ControllerSessionManager.Controllers
{
    public sealed class XInputProvider : IDisposable
    {
        public const string ProviderId = "XInput";
        public const string SdlProviderId = "SDL";
        private const uint ErrorSuccess = 0;
        private const byte BatteryDeviceTypeGamepad = 0;
        private const byte BatteryTypeWired = 1;
        private const byte BatteryTypeAlkaline = 2;
        private const byte BatteryTypeNimh = 3;
        private readonly SlotState[] slots = new SlotState[4];
        private readonly SdlControllerMetadataProvider metadataProvider = new SdlControllerMetadataProvider();
        private readonly IList<IControllerBatteryProvider> batteryProviders =
            new List<IControllerBatteryProvider> { new PlayStationHidBatteryProvider() };
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
            return Poll(true);
        }

        public IReadOnlyList<ControllerDeviceSnapshot> Poll(bool sampleSdlInput)
        {
            var result = new List<ControllerDeviceSnapshot>();
            if (unavailable)
            {
                return result;
            }

            try
            {
                // A false value is a hard process-safety boundary: do not initialize SDL or
                // call any SDL entry point. Fullscreen uses this mode because Playnite owns a
                // process-wide SDL event loop and some drivers terminate that process during
                // hot-unplug even when this plugin only performs device-level enumeration.
                var metadata = sampleSdlInput
                    ? metadataProvider.GetControllers(true)
                    : new List<ControllerMetadata>();
                if (sampleSdlInput)
                {
                    EnrichKnownBatteryProtocols(metadata);
                }
                var usedMetadata = new HashSet<ControllerMetadata>();
                for (uint index = 0; index < 4; index++)
                {
                    XInputState state;
                    if (NativeMethods.XInputGetState(index, out state) != ErrorSuccess)
                    {
                        slots[index].WasConnected = false;
                        slots[index].HasGamepadState = false;
                        continue;
                    }

                    var now = DateTime.UtcNow;
                    var slot = slots[index];
                    if (slot.WasConnected && slot.HasGamepadState)
                    {
                        var evidence = GetInputEvidence(slot.GamepadState, state.Gamepad);
                        if (evidence != InputEvidenceKind.None)
                        {
                            slot.LastInputUtc = now;
                            slot.LastInputKind = evidence.ToString();
                        }
                    }

                    slot.WasConnected = true;
                    slot.PacketNumber = state.PacketNumber;
                    slot.GamepadState = state.Gamepad;
                    slot.HasGamepadState = true;
                    var isNeutral = IsNeutral(state.Gamepad);
                    if (isNeutral)
                    {
                        if (!slot.InputNeutralSinceUtc.HasValue)
                        {
                            slot.InputNeutralSinceUtc = now;
                        }
                    }
                    else
                    {
                        slot.InputNeutralSinceUtc = null;
                    }

                    var deviceMetadata = metadata.FirstOrDefault(a => a.PlayerIndex == (int)index);
                    if (deviceMetadata == null)
                    {
                        deviceMetadata = metadata.FirstOrDefault(a => !usedMetadata.Contains(a) &&
                            !string.IsNullOrWhiteSpace(a.RawName) &&
                            a.RawName.IndexOf("XInput", StringComparison.OrdinalIgnoreCase) >= 0);
                    }

                    if (deviceMetadata != null)
                    {
                        usedMetadata.Add(deviceMetadata);
                    }

                    XInputBatteryInformation battery;
                    var batteryResult = NativeMethods.XInputGetBatteryInformation(
                        index, BatteryDeviceTypeGamepad, out battery);
                    var wired = batteryResult == ErrorSuccess && battery.BatteryType == BatteryTypeWired;
                    var wireless = batteryResult == ErrorSuccess &&
                        (battery.BatteryType == BatteryTypeAlkaline || battery.BatteryType == BatteryTypeNimh);
                    var detectedConnection = deviceMetadata == null ? "Unknown" : deviceMetadata.ConnectionType;
                    var connection = detectedConnection != "Unknown"
                        ? detectedConnection
                        : wireless ? "Wireless" : wired ? "Wired" : "Unknown";
                    var detectedName = deviceMetadata == null
                        ? string.Format("XInput Controller (Player {0})", index + 1)
                        : deviceMetadata.DisplayName;
                    var batteryLevel = GetBatteryLevel(deviceMetadata, batteryResult, battery,
                        wired && detectedConnection == "Unknown", wireless);
                    result.Add(new ControllerDeviceSnapshot
                    {
                        ControllerId = string.Format("xinput:slot:{0}", index),
                        ProviderId = ProviderId,
                        ProviderInstanceId = (int)index,
                        Name = detectedName,
                        DetectedName = detectedName,
                        HardwareId = deviceMetadata == null
                            ? string.Format("xinput:slot:{0}", index)
                            : deviceMetadata.HardwareId,
                        VendorId = deviceMetadata == null ? (ushort)0 : deviceMetadata.VendorId,
                        ProductId = deviceMetadata == null ? (ushort)0 : deviceMetadata.ProductId,
                        Path = string.Empty,
                        IsConnected = true,
                        IsEnabled = true,
                        ConnectionType = connection,
                        BatteryLevel = batteryLevel,
                        BatteryProviderId = GetBatteryProviderId(deviceMetadata, batteryLevel),
                        LastSeenUtc = now,
                        LastInputUtc = slot.LastInputUtc,
                        LastInputKind = slot.LastInputKind,
                        IsInputNeutral = isNeutral,
                        InputNeutralSinceUtc = slot.InputNeutralSinceUtc
                    });
                }

                foreach (var sdlDevice in metadata.Where(a => !usedMetadata.Contains(a)))
                {
                    result.Add(new ControllerDeviceSnapshot
                    {
                        ControllerId = string.Format("sdl:instance:{0}", sdlDevice.InstanceId),
                        ProviderId = SdlProviderId,
                        ProviderInstanceId = sdlDevice.InstanceId,
                        Name = sdlDevice.DisplayName,
                        DetectedName = sdlDevice.DisplayName,
                        HardwareId = sdlDevice.HardwareId,
                        VendorId = sdlDevice.VendorId,
                        ProductId = sdlDevice.ProductId,
                        Path = string.Empty,
                        IsConnected = true,
                        IsEnabled = true,
                        ConnectionType = sdlDevice.ConnectionType,
                        BatteryLevel = sdlDevice.BatteryLevel,
                        BatteryProviderId = string.IsNullOrWhiteSpace(sdlDevice.BatteryProviderId)
                            ? "None" : sdlDevice.BatteryProviderId,
                        LastSeenUtc = DateTime.UtcNow,
                        LastInputUtc = sdlDevice.LastInputUtc,
                        LastInputKind = sdlDevice.LastInputKind,
                        IsInputNeutral = sdlDevice.IsInputNeutral,
                        InputNeutralSinceUtc = sdlDevice.InputNeutralSinceUtc
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

        private void EnrichKnownBatteryProtocols(IEnumerable<ControllerMetadata> controllers)
        {
            foreach (var controller in controllers.Where(a => a != null &&
                (string.IsNullOrWhiteSpace(a.BatteryLevel) || a.BatteryLevel == "Unknown" ||
                 a.BatteryLevel == "Unavailable")))
            {
                foreach (var provider in batteryProviders.Where(a => a.Supports(controller)))
                {
                    string level;
                    if (provider.TryGetBatteryLevel(controller, out level))
                    {
                        controller.BatteryLevel = level;
                        controller.BatteryProviderId = provider.Id;
                        break;
                    }
                }
            }
        }

        private static string GetBatteryProviderId(ControllerMetadata metadata, string level)
        {
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.BatteryProviderId) &&
                level != "Unknown" && level != "Unavailable")
            {
                return metadata.BatteryProviderId;
            }
            return level == "Empty" || level == "Low" || level == "Medium" || level == "Full"
                ? ProviderId : "None";
        }

        public void Dispose()
        {
            metadataProvider.Dispose();
        }

        public bool TryVibrate(string providerId, int providerInstanceId)
        {
            if (string.Equals(providerId, SdlProviderId, StringComparison.OrdinalIgnoreCase))
            {
                return metadataProvider.TryRumble(providerInstanceId, 36000, 48000, 450);
            }

            if (!string.Equals(providerId, ProviderId, StringComparison.OrdinalIgnoreCase) ||
                providerInstanceId < 0 || providerInstanceId >= slots.Length)
            {
                return false;
            }

            var vibration = new XInputVibration
            {
                LeftMotorSpeed = 36000,
                RightMotorSpeed = 48000
            };
            if (NativeMethods.XInputSetState((uint)providerInstanceId, ref vibration) != ErrorSuccess)
            {
                return false;
            }

            Task.Run(delegate
            {
                Thread.Sleep(450);
                var stop = new XInputVibration();
                NativeMethods.XInputSetState((uint)providerInstanceId, ref stop);
            });
            return true;
        }

        private static string GetBatteryLevel(ControllerMetadata metadata, uint result,
            XInputBatteryInformation battery, bool wired, bool wireless)
        {
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.BatteryLevel) &&
                metadata.BatteryLevel != "Unknown" && metadata.BatteryLevel != "Unavailable")
            {
                return metadata.BatteryLevel;
            }

            if (result != ErrorSuccess)
            {
                return "Unknown";
            }

            if (wired)
            {
                return "Unavailable";
            }

            if (metadata != null && metadata.ConnectionType == "WirelessReceiver")
            {
                return "Unknown";
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

        private static InputEvidenceKind GetInputEvidence(XInputGamepad previous, XInputGamepad current)
        {
            return IntentionalInputDetector.GetXInputEvidence(
                previous.Buttons, current.Buttons,
                previous.LeftTrigger, current.LeftTrigger,
                previous.RightTrigger, current.RightTrigger,
                previous.LeftThumbX, current.LeftThumbX,
                previous.LeftThumbY, current.LeftThumbY,
                previous.RightThumbX, current.RightThumbX,
                previous.RightThumbY, current.RightThumbY);
        }

        private static bool IsNeutral(XInputGamepad value)
        {
            return IntentionalInputDetector.IsXInputNeutral(value.Buttons, value.LeftTrigger,
                value.RightTrigger, value.LeftThumbX, value.LeftThumbY, value.RightThumbX,
                value.RightThumbY);
        }

        private sealed class SlotState
        {
            public bool WasConnected;
            public uint PacketNumber;
            public DateTime? LastInputUtc;
            public string LastInputKind;
            public DateTime? InputNeutralSinceUtc;
            public bool HasGamepadState;
            public XInputGamepad GamepadState;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputVibration
        {
            public ushort LeftMotorSpeed;
            public ushort RightMotorSpeed;
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

            [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
            public static extern uint XInputSetState(uint userIndex, ref XInputVibration vibration);
        }
    }
}
