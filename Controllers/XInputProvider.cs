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
        public const string HidProviderId = "HID";
        private const uint ErrorSuccess = 0;
        private const byte BatteryDeviceTypeGamepad = 0;
        private const byte BatteryTypeWired = 1;
        private const byte BatteryTypeAlkaline = 2;
        private const byte BatteryTypeNimh = 3;
        private readonly SlotState[] slots = new SlotState[4];
        private readonly SdlControllerMetadataProvider metadataProvider = new SdlControllerMetadataProvider();
        private readonly IList<IControllerBatteryProvider> batteryProviders =
            new List<IControllerBatteryProvider>
            {
                new WindowsBluetoothBatteryProvider(),
                new PlayStationHidBatteryProvider()
            };
        private bool unavailable;
        public bool LastPollXInputTopologyChanged { get; private set; }

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
            LastPollXInputTopologyChanged = false;
            var result = new List<ControllerDeviceSnapshot>();
            if (unavailable)
            {
                return result;
            }

            try
            {
                var slotStates = new XInputState[4];
                var slotConnected = new bool[4];
                var topologyChanged = false;
                for (uint index = 0; index < 4; index++)
                {
                    slotConnected[index] = NativeMethods.XInputGetState(index, out slotStates[index]) ==
                        ErrorSuccess;
                    if (slotConnected[index] != slots[index].WasConnected)
                    {
                        topologyChanged = true;
                    }
                }

                LastPollXInputTopologyChanged = topologyChanged;
                IReadOnlyList<ControllerMetadata> metadata;
                if (topologyChanged)
                {
                    HidDiagnosticsService.InvalidatePresentControllerMetadata();
                    foreach (var provider in batteryProviders)
                    {
                        var windowsBattery = provider as WindowsBluetoothBatteryProvider;
                        if (windowsBattery != null)
                        {
                            windowsBattery.ClearCache();
                        }
                    }
                    metadata = new List<ControllerMetadata>();
                }
                else
                {
                    metadata = HidDiagnosticsService.GetPresentControllerMetadata();
                    EnrichKnownBatteryProtocols(metadata);
                }
                var usedMetadata = new HashSet<ControllerMetadata>();
                var connectedSlotCount = slotConnected.Count(a => a);
                for (uint index = 0; index < 4; index++)
                {
                    if (!slotConnected[index])
                    {
                        slots[index].WasConnected = false;
                        slots[index].HasGamepadState = false;
                        ClearSlotIdentity(slots[index]);
                        continue;
                    }

                    var state = slotStates[index];
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

                    var deviceMetadata = MatchHidMetadata(metadata, usedMetadata, connectedSlotCount);
                    if (deviceMetadata != null)
                    {
                        usedMetadata.Add(deviceMetadata);
                        RememberSlotIdentity(slot, deviceMetadata);
                    }

                    var hardwareId = deviceMetadata != null
                        ? deviceMetadata.HardwareId
                        : slot.HardwareId;
                    if (string.IsNullOrWhiteSpace(hardwareId))
                    {
                        hardwareId = string.Format("xinput:slot:{0}", index);
                    }
                    var vendorId = deviceMetadata != null ? deviceMetadata.VendorId : slot.VendorId;
                    var productId = deviceMetadata != null ? deviceMetadata.ProductId : slot.ProductId;
                    var path = deviceMetadata != null
                        ? deviceMetadata.DevicePath ?? string.Empty
                        : slot.DevicePath ?? string.Empty;
                    XInputBatteryInformation battery;
                    var batteryResult = NativeMethods.XInputGetBatteryInformation(
                        index, BatteryDeviceTypeGamepad, out battery);
                    var wired = batteryResult == ErrorSuccess && battery.BatteryType == BatteryTypeWired;
                    var wireless = batteryResult == ErrorSuccess &&
                        (battery.BatteryType == BatteryTypeAlkaline || battery.BatteryType == BatteryTypeNimh);
                    var detectedConnection = deviceMetadata != null
                        ? deviceMetadata.ConnectionType
                        : string.IsNullOrWhiteSpace(slot.ConnectionType) ? "Unknown" : slot.ConnectionType;
                    var detectedName = deviceMetadata != null
                        ? deviceMetadata.DisplayName
                        : !string.IsNullOrWhiteSpace(slot.DisplayName)
                            ? slot.DisplayName
                            : string.Format("XInput Controller (Player {0})", index + 1);
                    if (!IsReusableXInputTransport(vendorId, path, detectedConnection))
                    {
                        path = string.Empty;
                        detectedConnection = ControllerDeviceIdentity.ContainsWirelessHint(detectedName)
                            ? "Wireless" : "Unknown";
                    }
                    var connection = detectedConnection != "Unknown"
                        ? detectedConnection
                        : wireless ? "Wireless" : wired ? "Wired" : "Unknown";
                    var batteryLevel = XInputBatteryResolver.Resolve(deviceMetadata, batteryResult,
                        battery.BatteryType, battery.BatteryLevel,
                        battery.BatteryType == BatteryTypeWired && detectedConnection == "Unknown",
                        XInputBatteryResolver.IsWirelessBatteryType(battery.BatteryType));
                    // When XInput does not detect input (e.g. some BT stacks do not deliver XInput
                    // button events), use SDL raw-joystick data as a fallback LastInputUtc.
                    var lastInputUtc = slot.LastInputUtc;
                    var lastInputKind = slot.LastInputKind;
                    if (deviceMetadata != null && deviceMetadata.LastInputUtc.HasValue &&
                        (!lastInputUtc.HasValue || deviceMetadata.LastInputUtc > lastInputUtc))
                    {
                        lastInputUtc = deviceMetadata.LastInputUtc;
                        lastInputKind = deviceMetadata.LastInputKind;
                    }

                    result.Add(new ControllerDeviceSnapshot
                    {
                        ControllerId = string.Format("xinput:slot:{0}", index),
                        ProviderId = ProviderId,
                        ProviderInstanceId = (int)index,
                        Name = detectedName,
                        DetectedName = detectedName,
                        HardwareId = hardwareId,
                        VendorId = vendorId,
                        ProductId = productId,
                        Path = path,
                        IsConnected = true,
                        IsEnabled = true,
                        ConnectionType = connection,
                        BatteryLevel = batteryLevel,
                        BatteryProviderId = GetBatteryProviderId(deviceMetadata, batteryLevel),
                        LastSeenUtc = now,
                        LastInputUtc = lastInputUtc,
                        LastInputKind = lastInputKind,
                        IsInputNeutral = isNeutral,
                        InputNeutralSinceUtc = slot.InputNeutralSinceUtc
                    });
                }

                AppendUnusedHidObservations(result, metadata, usedMetadata);
            }
            catch (DllNotFoundException)
            {
                unavailable = true;
            }
            catch (EntryPointNotFoundException)
            {
                unavailable = true;
            }
            catch (Exception)
            {
            }

            return result;
        }

        public void AbandonSdlHandles()
        {
            metadataProvider.AbandonOpenHandles();
            HidDiagnosticsService.InvalidatePresentControllerMetadata();
        }

        private static void RememberSlotIdentity(SlotState slot, ControllerMetadata metadata)
        {
            if (slot == null || metadata == null)
            {
                return;
            }

            slot.HardwareId = metadata.HardwareId;
            slot.VendorId = metadata.VendorId;
            slot.ProductId = metadata.ProductId;
            slot.DisplayName = metadata.DisplayName;
            if (IsReusableXInputTransport(metadata.VendorId, metadata.DevicePath,
                metadata.ConnectionType))
            {
                slot.DevicePath = metadata.DevicePath ?? string.Empty;
                slot.ConnectionType = metadata.ConnectionType;
            }
            else
            {
                slot.DevicePath = string.Empty;
                slot.ConnectionType = ControllerDeviceIdentity.ContainsWirelessHint(metadata.DisplayName)
                    ? "Wireless" : "Unknown";
            }
        }

        private static void ClearSlotIdentity(SlotState slot)
        {
            if (slot == null)
            {
                return;
            }
            slot.HardwareId = null;
            slot.VendorId = 0;
            slot.ProductId = 0;
            slot.DevicePath = null;
            slot.DisplayName = null;
            slot.ConnectionType = null;
            slot.LastInputUtc = null;
            slot.LastInputKind = null;
            slot.InputNeutralSinceUtc = null;
        }

        private static bool IsReusableXInputTransport(ushort vendorId, string path, string connectionType)
        {
            if (WindowsBluetoothBatteryProvider.IsBluetoothPath(path) ||
                string.Equals(connectionType, "Bluetooth", StringComparison.OrdinalIgnoreCase))
            {
                return vendorId == 0x045E;
            }

            return true;
        }

        private static ControllerMetadata MatchHidMetadata(IReadOnlyList<ControllerMetadata> metadata,
            HashSet<ControllerMetadata> usedMetadata, int connectedSlotCount)
        {
            var unused = metadata.Where(a => a != null && !usedMetadata.Contains(a)).ToList();
            var xinputWrappers = unused.Where(a =>
                !string.IsNullOrWhiteSpace(a.DevicePath) &&
                a.DevicePath.IndexOf("&ig_", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (xinputWrappers.Count == 1 && (connectedSlotCount == 1 || unused.Count == 1))
            {
                return xinputWrappers[0];
            }

            if (xinputWrappers.Count > 0)
            {
                return xinputWrappers[0];
            }

            return null;
        }

        private static void AppendUnusedHidObservations(IList<ControllerDeviceSnapshot> result,
            IReadOnlyList<ControllerMetadata> metadata, HashSet<ControllerMetadata> usedMetadata)
        {
            if (result == null || metadata == null || usedMetadata == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var leftover in metadata)
            {
                if (leftover == null || usedMetadata.Contains(leftover) ||
                    IsXInputWrapperMetadata(leftover) ||
                    !ControllerDeviceIdentity.IsPublishableHidCapability(leftover.DisplayName,
                        leftover.DevicePath))
                {
                    continue;
                }

                var leftoverSnapshot = new ControllerDeviceSnapshot
                {
                    ProviderId = HidProviderId,
                    Name = leftover.DisplayName,
                    DetectedName = leftover.DisplayName,
                    HardwareId = leftover.HardwareId,
                    VendorId = leftover.VendorId,
                    ProductId = leftover.ProductId,
                    Path = leftover.DevicePath ?? string.Empty,
                    IsConnected = true,
                    ConnectionType = leftover.ConnectionType
                };
                if (result.Any(a => a != null && a.IsConnected &&
                    ControllerTransportPolicy.ShouldCollapse(a, leftoverSnapshot)))
                {
                    continue;
                }

                result.Add(new ControllerDeviceSnapshot
                {
                    ControllerId = string.IsNullOrWhiteSpace(leftover.HardwareId)
                        ? leftover.DevicePath : leftover.HardwareId,
                    ProviderId = HidProviderId,
                    ProviderInstanceId = 0,
                    Name = leftover.DisplayName,
                    DetectedName = leftover.DisplayName,
                    HardwareId = leftover.HardwareId,
                    VendorId = leftover.VendorId,
                    ProductId = leftover.ProductId,
                    Path = leftover.DevicePath ?? string.Empty,
                    IsConnected = true,
                    IsEnabled = true,
                    ConnectionType = leftover.ConnectionType,
                    BatteryLevel = leftover.BatteryLevel,
                    BatteryProviderId = leftover.BatteryProviderId,
                    LastSeenUtc = now,
                    LastInputUtc = leftover.LastInputUtc,
                    LastInputKind = leftover.LastInputKind
                });
            }
        }

        private static bool IsXInputWrapperMetadata(ControllerMetadata metadata)
        {
            return metadata != null &&
                WindowsBluetoothBatteryProvider.IsXInputWrapperPath(metadata.DevicePath);
        }

        private void EnrichKnownBatteryProtocols(IEnumerable<ControllerMetadata> controllers)
        {
            foreach (var controller in controllers.Where(a => a != null && a.IsSettled &&
                (string.IsNullOrWhiteSpace(a.BatteryLevel) || a.BatteryLevel == "Unknown" ||
                 a.BatteryLevel == "Unavailable")))
            {
                foreach (var provider in batteryProviders.Where(a => a.Supports(controller)))
                {
                    try
                    {
                        string level;
                        if (provider.TryGetBatteryLevel(controller, out level))
                        {
                            controller.BatteryLevel = level;
                            controller.BatteryProviderId = provider.Id;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        // HID/PnP reads during Bluetooth bring-up can throw; skip this provider.
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

        private int rumbleEpoch;

        public void Dispose()
        {
            Interlocked.Increment(ref rumbleEpoch);
            metadataProvider.Dispose();
        }

        public bool TryVibrate(string providerId, int providerInstanceId)
        {
            if (string.Equals(providerId, SdlProviderId, StringComparison.OrdinalIgnoreCase))
            {
                var started = metadataProvider.TryRumble(providerInstanceId, 36000, 48000, 450);
                if (!started)
                {
                    return false;
                }

                Thread.Sleep(450);
                metadataProvider.TryRumble(providerInstanceId, 0, 0, 1);
                return true;
            }

            if (!string.Equals(providerId, ProviderId, StringComparison.OrdinalIgnoreCase) ||
                providerInstanceId < 0 || providerInstanceId >= slots.Length)
            {
                return false;
            }

            var epoch = Interlocked.Increment(ref rumbleEpoch);
            var stop = new XInputVibration();
            NativeMethods.XInputSetState((uint)providerInstanceId, ref stop);
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
                if (Thread.VolatileRead(ref rumbleEpoch) != epoch)
                {
                    return;
                }

                NativeMethods.XInputSetState((uint)providerInstanceId, ref stop);
            });
            return true;
        }

        public void StopVibrate(string providerId, int providerInstanceId)
        {
            Interlocked.Increment(ref rumbleEpoch);
            if (string.Equals(providerId, SdlProviderId, StringComparison.OrdinalIgnoreCase))
            {
                metadataProvider.TryRumble(providerInstanceId, 0, 0, 1);
                return;
            }

            if (!string.Equals(providerId, ProviderId, StringComparison.OrdinalIgnoreCase) ||
                providerInstanceId < 0 || providerInstanceId >= slots.Length)
            {
                return;
            }

            var stop = new XInputVibration();
            NativeMethods.XInputSetState((uint)providerInstanceId, ref stop);
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
            public string HardwareId;
            public ushort VendorId;
            public ushort ProductId;
            public string DevicePath;
            public string DisplayName;
            public string ConnectionType;
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
