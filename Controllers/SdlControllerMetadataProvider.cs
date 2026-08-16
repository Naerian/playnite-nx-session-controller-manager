using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ControllerSessionManager.Controllers
{
    public sealed class SdlControllerMetadataProvider : IDisposable
    {
        private const uint SdlInitGameController = 0x00002000;
        private const uint SdlInitEvents = 0x00004000;
        private bool unavailable;
        private bool initialized;
        private readonly Dictionary<int, IntPtr> openJoysticks = new Dictionary<int, IntPtr>();
        private readonly Dictionary<int, IntPtr> openGameControllers = new Dictionary<int, IntPtr>();
        private readonly Dictionary<int, SdlInputState> inputStates = new Dictionary<int, SdlInputState>();

        public IReadOnlyList<ControllerMetadata> GetControllers()
        {
            return GetControllers(true);
        }

        public IReadOnlyList<ControllerMetadata> GetControllers(bool sampleInput)
        {
            var result = new List<ControllerMetadata>();
            if (unavailable || !EnsureInitialized())
            {
                return result;
            }

            try
            {
                NativeMethods.SDL_GameControllerUpdate();
                var duplicateCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var observedInstances = new HashSet<int>();
                var count = NativeMethods.SDL_NumJoysticks();
                for (var index = 0; index < count; index++)
                {
                    if (NativeMethods.SDL_IsGameController(index) != 1)
                    {
                        continue;
                    }

                    var instanceId = NativeMethods.SDL_JoystickGetDeviceInstanceID(index);
                    observedInstances.Add(instanceId);
                    var rawName = Marshal.PtrToStringAnsi(NativeMethods.SDL_GameControllerNameForIndex(index));
                    var devicePath = GetDevicePath(index);
                    var vendorId = NativeMethods.SDL_JoystickGetDeviceVendor(index);
                    var productId = NativeMethods.SDL_JoystickGetDeviceProduct(index);
                    var baseId = string.Format("hardware:{0:X4}:{1:X4}", vendorId, productId);
                    int ordinal;
                    duplicateCounts.TryGetValue(baseId, out ordinal);
                    ordinal++;
                    duplicateCounts[baseId] = ordinal;
                    var displayName = ControllerDeviceIdentity.GetDisplayName(rawName, vendorId, productId);
                    var playerIndex = NativeMethods.SDL_JoystickGetDevicePlayerIndex(index);
                    var isXInputBacked = playerIndex >= 0 ||
                        (!string.IsNullOrWhiteSpace(rawName) &&
                         rawName.IndexOf("XInput", StringComparison.OrdinalIgnoreCase) >= 0);
                    var shouldSampleDevice = sampleInput && !isXInputBacked;
                    var gameController = shouldSampleDevice
                        ? GetOrOpenGameController(index, instanceId)
                        : IntPtr.Zero;
                    var inputState = shouldSampleDevice
                        ? GetInputState(index, instanceId, gameController)
                        : null;
                    result.Add(new ControllerMetadata
                    {
                        Index = index,
                        InstanceId = instanceId,
                        PlayerIndex = playerIndex,
                        RawName = rawName,
                        DevicePath = devicePath,
                        DisplayName = displayName,
                        VendorId = vendorId,
                        ProductId = productId,
                        HardwareId = string.Format("{0}:{1}", baseId, ordinal),
                        ConnectionType = ControllerDeviceIdentity.GetConnectionType(
                            string.Format("{0} {1}", rawName, displayName), vendorId, productId, devicePath),
                        BatteryLevel = shouldSampleDevice
                            ? GetBatteryLevel(index, instanceId)
                            : "Unknown",
                        LastInputUtc = inputState == null ? null : inputState.LastInputUtc,
                        LastInputKind = inputState == null ? null : inputState.LastInputKind,
                        IsInputNeutral = inputState == null ? (bool?)null : inputState.IsInputNeutral,
                        InputNeutralSinceUtc = inputState == null ? null : inputState.InputNeutralSinceUtc
                    });
                }

                foreach (var staleInstance in openJoysticks.Keys.Where(a => !observedInstances.Contains(a)).ToList())
                {
                    // Do not close a native SDL handle from the hot-unplug callback path.
                    // Playnite owns the process-wide SDL event loop and some controller drivers
                    // invalidate these handles before SDL reports the removal to this plugin.
                    // Dropping our references lets the OS reclaim the small native allocation
                    // when the Fullscreen process exits and, crucially, cannot take Playnite down.
                    openGameControllers.Remove(staleInstance);
                    openJoysticks.Remove(staleInstance);
                    inputStates.Remove(staleInstance);
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
            catch (BadImageFormatException)
            {
                unavailable = true;
            }

            return result.OrderBy(a => a.Index).ToList();
        }

        public void Dispose()
        {
            foreach (var controller in openGameControllers.Values)
            {
                NativeMethods.SDL_GameControllerClose(controller);
            }
            openGameControllers.Clear();
            foreach (var joystick in openJoysticks.Values)
            {
                NativeMethods.SDL_JoystickClose(joystick);
            }
            openJoysticks.Clear();
            inputStates.Clear();
        }

        public bool TryRumble(int instanceId, ushort lowFrequency, ushort highFrequency, uint durationMs)
        {
            IntPtr controller;
            if (!openGameControllers.TryGetValue(instanceId, out controller) || controller == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                return NativeMethods.SDL_GameControllerRumble(
                    controller, lowFrequency, highFrequency, durationMs) == 0;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        private string GetBatteryLevel(int index, int instanceId)
        {
            var joystick = GetOrOpenJoystick(index, instanceId);
            if (joystick == IntPtr.Zero)
            {
                return "Unknown";
            }

            switch (NativeMethods.SDL_JoystickCurrentPowerLevel(joystick))
            {
                case 0: return "Empty";
                case 1: return "Low";
                case 2: return "Medium";
                case 3: return "Full";
                case 4: return "Unavailable";
                default: return "Unknown";
            }
        }

        private SdlInputState GetInputState(int index, int instanceId, IntPtr gameController)
        {
            var joystick = GetOrOpenJoystick(index, instanceId);
            if (joystick == IntPtr.Zero)
            {
                return null;
            }

            var sample = GetInputSample(joystick, gameController);
            SdlInputState state;
            if (!inputStates.TryGetValue(instanceId, out state))
            {
                state = new SdlInputState
                {
                    HasSample = true,
                    Sample = sample,
                    BaselineAxes = (short[])sample.Axes.Clone()
                };
                state.IsInputNeutral = IsNeutral(state, sample);
                state.InputNeutralSinceUtc = state.IsInputNeutral ? DateTime.UtcNow : (DateTime?)null;
                inputStates[instanceId] = state;
                return state;
            }

            if (state.HasSample)
            {
                var evidence = GetInputEvidence(state, state.Sample, sample);
                if (evidence != InputEvidenceKind.None)
                {
                    state.LastInputUtc = DateTime.UtcNow;
                    state.LastInputKind = evidence.ToString();
                }
            }

            state.HasSample = true;
            state.Sample = sample;
            state.IsInputNeutral = IsNeutral(state, sample);
            if (state.IsInputNeutral)
            {
                if (!state.InputNeutralSinceUtc.HasValue)
                {
                    state.InputNeutralSinceUtc = DateTime.UtcNow;
                }
            }
            else
            {
                state.InputNeutralSinceUtc = null;
            }
            return state;
        }

        private IntPtr GetOrOpenJoystick(int index, int instanceId)
        {
            IntPtr joystick;
            if (openJoysticks.TryGetValue(instanceId, out joystick))
            {
                return joystick;
            }

            joystick = NativeMethods.SDL_JoystickOpen(index);
            if (joystick != IntPtr.Zero)
            {
                openJoysticks[instanceId] = joystick;
            }

            return joystick;
        }

        private static string GetDevicePath(int index)
        {
            try
            {
                return Marshal.PtrToStringAnsi(NativeMethods.SDL_JoystickPathForIndex(index));
            }
            catch (EntryPointNotFoundException)
            {
                return string.Empty;
            }
        }

        private IntPtr GetOrOpenGameController(int index, int instanceId)
        {
            IntPtr controller;
            if (openGameControllers.TryGetValue(instanceId, out controller))
            {
                return controller;
            }

            controller = NativeMethods.SDL_GameControllerOpen(index);
            if (controller != IntPtr.Zero)
            {
                openGameControllers[instanceId] = controller;
            }

            return controller;
        }

        private static SdlInputSample GetInputSample(IntPtr joystick, IntPtr gameController)
        {
            unchecked
            {
                var axisCount = Math.Min(16, Math.Max(0, NativeMethods.SDL_JoystickNumAxes(joystick)));
                var axes = new short[axisCount];
                for (var axis = 0; axis < axisCount; axis++)
                {
                    axes[axis] = NativeMethods.SDL_JoystickGetAxis(joystick, axis);
                }

                ulong buttons = 0;
                if (gameController != IntPtr.Zero)
                {
                    // Use SDL's canonical game-controller mapping so button 5 is consistently
                    // Guide/PS/Home. System buttons can power a device off but do not represent
                    // participation in the game session.
                    const int controllerButtonCount = 21;
                    for (var button = 0; button < controllerButtonCount; button++)
                    {
                        if (IntentionalInputDetector.IsSdlGameplayButton(button) &&
                            NativeMethods.SDL_GameControllerGetButton(gameController, button) != 0)
                        {
                            buttons |= 1UL << button;
                        }
                    }
                }
                else
                {
                    var buttonCount = Math.Min(64, Math.Max(0, NativeMethods.SDL_JoystickNumButtons(joystick)));
                    for (var button = 0; button < buttonCount; button++)
                    {
                        if (NativeMethods.SDL_JoystickGetButton(joystick, button) != 0)
                        {
                            buttons |= 1UL << button;
                        }
                    }
                }

                var hatHash = 17;
                var hasHatDirection = false;
                var hatCount = Math.Min(8, Math.Max(0, NativeMethods.SDL_JoystickNumHats(joystick)));
                for (var hat = 0; hat < hatCount; hat++)
                {
                    var value = NativeMethods.SDL_JoystickGetHat(joystick, hat);
                    hasHatDirection |= value != 0;
                    hatHash = (hatHash * 31) + value;
                }

                return new SdlInputSample
                {
                    Axes = axes,
                    Buttons = buttons,
                    HatHash = hatHash,
                    HasHatDirection = hasHatDirection
                };
            }
        }

        private static InputEvidenceKind GetInputEvidence(SdlInputState state,
            SdlInputSample previous, SdlInputSample current)
        {
            return IntentionalInputDetector.GetSdlEvidence(
                previous.Buttons, current.Buttons, state.BaselineAxes, previous.Axes, current.Axes,
                previous.HatHash, current.HatHash, current.HasHatDirection);
        }

        private static bool IsNeutral(SdlInputState state, SdlInputSample current)
        {
            return IntentionalInputDetector.IsSdlNeutral(current.Buttons, state.BaselineAxes,
                current.Axes, current.HasHatDirection);
        }

        private struct SdlInputSample
        {
            public short[] Axes;
            public ulong Buttons;
            public int HatHash;
            public bool HasHatDirection;
        }

        private sealed class SdlInputState
        {
            public bool HasSample;
            public SdlInputSample Sample;
            public short[] BaselineAxes;
            public DateTime? LastInputUtc;
            public string LastInputKind;
            public bool IsInputNeutral;
            public DateTime? InputNeutralSinceUtc;
        }

        private bool EnsureInitialized()
        {
            if (initialized)
            {
                return true;
            }

            try
            {
                initialized = NativeMethods.SDL_InitSubSystem(SdlInitGameController | SdlInitEvents) == 0;
                return initialized;
            }
            catch (DllNotFoundException)
            {
                unavailable = true;
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                unavailable = true;
                return false;
            }
            catch (BadImageFormatException)
            {
                unavailable = true;
                return false;
            }
        }

        private static class NativeMethods
        {
            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SDL_InitSubSystem(uint flags);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SDL_NumJoysticks();

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SDL_IsGameController(int joystickIndex);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr SDL_GameControllerNameForIndex(int joystickIndex);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr SDL_JoystickPathForIndex(int joystickIndex);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr SDL_GameControllerOpen(int joystickIndex);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SDL_GameControllerClose(IntPtr gamecontroller);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SDL_GameControllerRumble(IntPtr gamecontroller, ushort lowFrequencyRumble,
                ushort highFrequencyRumble, uint durationMs);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern byte SDL_GameControllerGetButton(IntPtr gamecontroller, int button);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern ushort SDL_JoystickGetDeviceVendor(int joystickIndex);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern ushort SDL_JoystickGetDeviceProduct(int joystickIndex);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SDL_JoystickGetDeviceInstanceID(int joystickIndex);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SDL_JoystickGetDevicePlayerIndex(int joystickIndex);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SDL_GameControllerUpdate();

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr SDL_JoystickOpen(int joystickIndex);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SDL_JoystickClose(IntPtr joystick);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SDL_JoystickCurrentPowerLevel(IntPtr joystick);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SDL_JoystickNumAxes(IntPtr joystick);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern short SDL_JoystickGetAxis(IntPtr joystick, int axis);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SDL_JoystickNumButtons(IntPtr joystick);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern byte SDL_JoystickGetButton(IntPtr joystick, int button);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SDL_JoystickNumHats(IntPtr joystick);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern byte SDL_JoystickGetHat(IntPtr joystick, int hat);
        }
    }
}
