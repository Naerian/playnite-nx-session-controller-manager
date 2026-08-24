using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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
        private readonly HashSet<int> previouslyObservedInstances = new HashSet<int>();
        private int lastJoystickCount = -1;

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
                NativeMethods.SDL_PumpEvents();
                NativeMethods.SDL_GameControllerUpdate();
                var duplicateCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var observedInstances = new HashSet<int>();
                var count = NativeMethods.SDL_NumJoysticks();
                // Opening a native joystick on the same tick a pad appears (or disappears) has
                // been observed to terminate Playnite with no managed exception. Keep metadata
                // reads, but delay JoystickOpen/GameControllerOpen until the instance survived
                // at least one later poll and the SDL inventory is no longer changing.
                var inventoryChanged = lastJoystickCount >= 0 && count != lastJoystickCount;
                lastJoystickCount = count;
                for (var index = 0; index < count; index++)
                {
                    int instanceId;
                    try
                    {
                        instanceId = NativeMethods.SDL_JoystickGetDeviceInstanceID(index);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    observedInstances.Add(instanceId);
                    try
                    {
                        var isGameController = NativeMethods.SDL_IsGameController(index) == 1;
                        // For devices in the game controller database, use the canonical mapped name.
                        // For raw joysticks (e.g. BT DInput controllers not yet in gamecontrollerdb),
                        // fall back to the raw joystick name so they still appear and get input sampled.
                        var rawName = isGameController
                            ? Marshal.PtrToStringAnsi(NativeMethods.SDL_GameControllerNameForIndex(index))
                            : Marshal.PtrToStringAnsi(NativeMethods.SDL_JoystickNameForIndex(index));
                        var sdlGuid = GetDeviceGuidString(index);
                        var databaseName = ControllerMappingDatabase.ResolveName(sdlGuid, null);
                        if (!string.IsNullOrWhiteSpace(databaseName) &&
                            (!isGameController || ControllerDeviceIdentity.IsGenericDisplayName(rawName)))
                        {
                            rawName = databaseName;
                        }
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
                        var isSettled = previouslyObservedInstances.Contains(instanceId) && !inventoryChanged;
                        // Never open a native SDL handle for an XInput-backed pad. USB and 2.4 GHz
                        // dongle unplug of those devices shares Playnite's SDL loop and can abort
                        // the process. XInput already supplies input and battery for that path.
                        // Sample raw joysticks only after the instance has survived a later poll.
                        var shouldSampleDevice = sampleInput && isSettled && !isXInputBacked;
                        // Use the canonical SDL game-controller button mapping only for XInput-backed
                        // devices, where SDL's controller-db entry reliably maps to the physical layout.
                        // For DInput and BT-DInput devices, the same SDL entry often targets a different
                        // firmware mode (XInput) and maps to wrong physical button indices; raw joystick
                        // button reads reflect the actual HID report, so they work regardless of SDL db.
                        var gameController = (shouldSampleDevice && isGameController && isXInputBacked)
                            ? GetOrOpenGameController(index, instanceId)
                            : IntPtr.Zero;
                        var inputState = shouldSampleDevice
                            ? GetInputState(index, instanceId, gameController)
                            : null;
                        var batteryLevel = shouldSampleDevice
                            ? GetBatteryLevel(index, instanceId)
                            : "Unknown";
                        result.Add(new ControllerMetadata
                        {
                            Index = index,
                            InstanceId = instanceId,
                            PlayerIndex = playerIndex,
                            RawName = rawName,
                            SdlGuid = sdlGuid,
                            DevicePath = devicePath,
                            DisplayName = displayName,
                            VendorId = vendorId,
                            ProductId = productId,
                            HardwareId = string.Format("{0}:{1}", baseId, ordinal),
                            ConnectionType = ControllerDeviceIdentity.GetConnectionType(
                                string.Format("{0} {1}", rawName, displayName), vendorId, productId, devicePath),
                            BatteryLevel = batteryLevel,
                            BatteryProviderId = batteryLevel == "Unknown" || batteryLevel == "Unavailable"
                                ? null : "SDL",
                            LastInputUtc = inputState == null ? null : inputState.LastInputUtc,
                            LastInputKind = inputState == null ? null : inputState.LastInputKind,
                            IsInputNeutral = inputState == null ? (bool?)null : inputState.IsInputNeutral,
                            InputNeutralSinceUtc = inputState == null ? null : inputState.InputNeutralSinceUtc,
                            IsSettled = isSettled
                        });
                    }
                    catch (Exception)
                    {
                        // A half-initialized device must not abort the rest of Desktop polling.
                    }
                }

                previouslyObservedInstances.IntersectWith(observedInstances);
                previouslyObservedInstances.UnionWith(observedInstances);

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
            catch (Exception)
            {
                // Transient native enumeration failures during hot-plug must not disable SDL
                // for the rest of the Desktop session.
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
            previouslyObservedInstances.Clear();
            lastJoystickCount = -1;
        }

        public void AbandonOpenHandles()
        {
            // Drop native references without SDL_JoystickClose. Closing during hot-unplug has
            // been observed to terminate Playnite; the OS reclaims the handles later.
            openGameControllers.Clear();
            openJoysticks.Clear();
            inputStates.Clear();
            previouslyObservedInstances.Clear();
            lastJoystickCount = -1;
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

            try
            {
                joystick = NativeMethods.SDL_JoystickOpen(index);
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
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

        private static string GetDeviceGuidString(int index)
        {
            try
            {
                var guid = NativeMethods.SDL_JoystickGetDeviceGUID(index);
                var text = new StringBuilder(33);
                NativeMethods.SDL_JoystickGetGUIDString(guid, text, text.Capacity);
                return text.ToString();
            }
            catch (EntryPointNotFoundException)
            {
                return string.Empty;
            }
            catch
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

            try
            {
                controller = NativeMethods.SDL_GameControllerOpen(index);
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
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
            public static extern IntPtr SDL_JoystickNameForIndex(int joystickIndex);

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
            public static extern SdlJoystickGuid SDL_JoystickGetDeviceGUID(int joystickIndex);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SDL_JoystickGetGUIDString(SdlJoystickGuid guid,
                StringBuilder text, int textSize);

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SDL_GameControllerUpdate();

            [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SDL_PumpEvents();

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

        [StructLayout(LayoutKind.Sequential)]
        private struct SdlJoystickGuid
        {
            public ulong First;
            public ulong Second;
        }
    }
}
