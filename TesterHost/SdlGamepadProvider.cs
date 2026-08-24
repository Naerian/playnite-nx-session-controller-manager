using ControllerSessionManager.Tester.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ControllerSessionManager.Tester.Services
{
    public sealed class SdlGamepadProvider : IGamepadInputProvider
    {
        private const int StandardRawButtonCount = 15;
        private readonly uint initFlags = Sdl2Native.SdlInitGameController | Sdl2Native.SdlInitHaptic | Sdl2Native.SdlInitEvents;
        private readonly object syncRoot = new object();
        private IntPtr controller;
        private int? selectedInstanceId;
        private bool technicalDetailsInitialized;
        private string cachedSdlVersion = "Unknown";
        private string cachedSdlGuid = "Unavailable";
        private string cachedSdlMapping = "Unavailable";
        private int cachedAxisCount;
        private int cachedButtonCount;
        private int cachedHatCount;
        private readonly string mappingDatabasePath;

        public SdlGamepadProvider(string controllerMappingDatabasePath = null)
        {
            mappingDatabasePath = controllerMappingDatabasePath;
            Sdl2Native.SDL_Init(initFlags);
            LoadControllerMappings();
            OpenSelectedOrFirstController();
        }

        public GamepadState ReadState()
        {
            lock (syncRoot)
            {
                Sdl2Native.SDL_GameControllerUpdate();

                if (controller != IntPtr.Zero && Sdl2Native.SDL_GameControllerGetAttached(controller) == 0)
                {
                    CloseController();
                }

                if (controller == IntPtr.Zero)
                {
                    OpenSelectedOrFirstController();
                }

                if (controller == IntPtr.Zero)
                {
                    return new GamepadState();
                }

                var name = GetControllerName();
                ushort vendorId;
                ushort productId;
                GetDeviceIds(out vendorId, out productId);

                var layout = ControllerIdentificationService.DetectLayout(name, vendorId, productId);
                var eightBitDoModel = ControllerIdentificationService.DetectEightBitDoModel(name, vendorId, productId);

                var state = new GamepadState
                {
                    IsConnected = true,
                    ControllerName = name,
                    VendorId = vendorId,
                    ProductId = productId,
                    Layout = layout,
                    EightBitDoModel = eightBitDoModel,
                    LeftStick = new StickState
                    {
                        X = NormalizeAxis(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.LeftX)),
                        Y = -NormalizeAxis(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.LeftY))
                    },
                    RightStick = new StickState
                    {
                        X = NormalizeAxis(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.RightX)),
                        Y = -NormalizeAxis(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.RightY))
                    },
                    LeftTrigger = NormalizeTrigger(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.TriggerLeft)),
                    RightTrigger = NormalizeTrigger(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.TriggerRight)),
                    Buttons = new GamepadButtonState
                    {
                        South = IsPressed(SdlControllerButton.A),
                        East = IsPressed(SdlControllerButton.B),
                        West = IsPressed(SdlControllerButton.X),
                        North = IsPressed(SdlControllerButton.Y),
                        LeftShoulder = IsPressed(SdlControllerButton.LeftShoulder),
                        RightShoulder = IsPressed(SdlControllerButton.RightShoulder),
                        Back = IsPressed(SdlControllerButton.Back),
                        Start = IsPressed(SdlControllerButton.Start),
                        Guide = IsPressed(SdlControllerButton.Guide),
                        Touchpad = IsPressed(SdlControllerButton.Touchpad),
                        LeftStick = IsPressed(SdlControllerButton.LeftStick),
                        RightStick = IsPressed(SdlControllerButton.RightStick),
                        DpadUp = IsPressed(SdlControllerButton.DpadUp),
                        DpadDown = IsPressed(SdlControllerButton.DpadDown),
                        DpadLeft = IsPressed(SdlControllerButton.DpadLeft),
                        DpadRight = IsPressed(SdlControllerButton.DpadRight)
                    },
                    ExtraButtons = ReadExtraButtons()
                };
                PopulateTechnicalDetails(state);
                return state;
            }
        }

        public IReadOnlyList<GamepadControllerInfo> GetControllers()
        {
            lock (syncRoot)
            {
                var controllers = new List<GamepadControllerInfo>();
                var count = Sdl2Native.SDL_NumJoysticks();

                for (var index = 0; index < count; index++)
                {
                    if (Sdl2Native.SDL_IsGameController(index) != 1)
                    {
                        continue;
                    }

                    var name = GetControllerNameForIndex(index);
                    var vendorId = Sdl2Native.SDL_JoystickGetDeviceVendor(index);
                    var productId = Sdl2Native.SDL_JoystickGetDeviceProduct(index);

                    controllers.Add(new GamepadControllerInfo
                    {
                        JoystickIndex = index,
                        InstanceId = Sdl2Native.SDL_JoystickGetDeviceInstanceID(index),
                        Name = name,
                        VendorId = vendorId,
                        ProductId = productId,
                        Layout = ControllerIdentificationService.DetectLayout(name, vendorId, productId),
                        EightBitDoModel = ControllerIdentificationService.DetectEightBitDoModel(name, vendorId, productId)
                    });
                }

                return controllers;
            }
        }

        public void SelectController(int instanceId)
        {
            lock (syncRoot)
            {
                if (selectedInstanceId.HasValue && selectedInstanceId.Value == instanceId)
                {
                    return;
                }

                selectedInstanceId = instanceId;
                CloseController();
                OpenSelectedOrFirstController();
            }
        }

        public bool TryRumble(ushort lowFrequency, ushort highFrequency, uint durationMs)
        {
            lock (syncRoot)
            {
                if (controller == IntPtr.Zero)
                {
                    return false;
                }

                return Sdl2Native.SDL_GameControllerRumble(controller, lowFrequency, highFrequency, durationMs) == 0;
            }
        }

        private void LoadControllerMappings()
        {
            var candidates = new[]
            {
                mappingDatabasePath,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gamecontrollerdb.txt"),
                Path.Combine(Environment.CurrentDirectory, "gamecontrollerdb.txt")
            };

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                {
                    var rw = Sdl2Native.SDL_RWFromFile(candidate, "rb");
                    if (rw != IntPtr.Zero)
                    {
                        Sdl2Native.SDL_GameControllerAddMappingsFromRW(rw, 1);
                    }

                    return;
                }
            }
        }

        private void OpenSelectedOrFirstController()
        {
            var count = Sdl2Native.SDL_NumJoysticks();
            if (selectedInstanceId.HasValue)
            {
                for (var index = 0; index < count; index++)
                {
                    if (Sdl2Native.SDL_IsGameController(index) == 1 &&
                        Sdl2Native.SDL_JoystickGetDeviceInstanceID(index) == selectedInstanceId.Value)
                    {
                        controller = Sdl2Native.SDL_GameControllerOpen(index);
                        if (controller != IntPtr.Zero)
                        {
                            return;
                        }
                    }
                }
            }

            for (var index = 0; index < count; index++)
            {
                if (Sdl2Native.SDL_IsGameController(index) == 1)
                {
                    controller = Sdl2Native.SDL_GameControllerOpen(index);
                    if (controller != IntPtr.Zero)
                    {
                        selectedInstanceId = Sdl2Native.SDL_JoystickGetDeviceInstanceID(index);
                        return;
                    }
                }
            }
        }

        private string GetControllerName()
        {
            var namePointer = Sdl2Native.SDL_GameControllerName(controller);
            var controllerName = Marshal.PtrToStringAnsi(namePointer);
            return controllerName ?? "Game controller";
        }

        private static string GetControllerNameForIndex(int joystickIndex)
        {
            var namePointer = Sdl2Native.SDL_GameControllerNameForIndex(joystickIndex);
            var controllerName = Marshal.PtrToStringAnsi(namePointer);
            return controllerName ?? "Game controller";
        }

        private bool IsPressed(SdlControllerButton button)
        {
            return Sdl2Native.SDL_GameControllerGetButton(controller, button) == 1;
        }

        private List<ExtraButtonState> ReadExtraButtons()
        {
            var extraButtons = new List<ExtraButtonState>();
            try
            {
                var joystick = Sdl2Native.SDL_GameControllerGetJoystick(controller);
                if (joystick == IntPtr.Zero)
                {
                    return extraButtons;
                }

                var buttonCount = Sdl2Native.SDL_JoystickNumButtons(joystick);
                if (buttonCount <= StandardRawButtonCount)
                {
                    return extraButtons;
                }

                for (var rawIndex = StandardRawButtonCount; rawIndex < buttonCount; rawIndex++)
                {
                    extraButtons.Add(new ExtraButtonState
                    {
                        RawIndex = rawIndex,
                        Label = GetExtraButtonLabel(rawIndex, rawIndex - StandardRawButtonCount + 1),
                        IsPressed = Sdl2Native.SDL_JoystickGetButton(joystick, rawIndex) == 1
                    });
                }
            }
            catch (Exception)
            {
                // Raw joystick buttons are best-effort. Some SDL builds or virtual drivers may not expose them reliably.
                extraButtons.Clear();
            }

            return extraButtons;
        }

        private static string GetExtraButtonLabel(int rawIndex, int extraIndex)
        {
            return string.Format("Extra {0} (SDL {1})", extraIndex, rawIndex);
        }

        private void GetDeviceIds(out ushort vendorId, out ushort productId)
        {
            vendorId = 0;
            productId = 0;

            var joystick = Sdl2Native.SDL_GameControllerGetJoystick(controller);
            if (joystick == IntPtr.Zero)
            {
                return;
            }

            vendorId = Sdl2Native.SDL_JoystickGetVendor(joystick);
            productId = Sdl2Native.SDL_JoystickGetProduct(joystick);
        }

        private void PopulateTechnicalDetails(GamepadState state)
        {
            if (!technicalDetailsInitialized)
            {
                try
                {
                    SdlVersion version;
                    Sdl2Native.SDL_GetVersion(out version);
                    cachedSdlVersion = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Patch);

                    var joystick = Sdl2Native.SDL_GameControllerGetJoystick(controller);
                    if (joystick != IntPtr.Zero)
                    {
                        cachedAxisCount = Math.Max(0, Sdl2Native.SDL_JoystickNumAxes(joystick));
                        cachedButtonCount = Math.Max(0, Sdl2Native.SDL_JoystickNumButtons(joystick));
                        cachedHatCount = Math.Max(0, Sdl2Native.SDL_JoystickNumHats(joystick));
                        var guidText = new StringBuilder(64);
                        Sdl2Native.SDL_JoystickGetGUIDString(Sdl2Native.SDL_JoystickGetGUID(joystick), guidText, guidText.Capacity);
                        cachedSdlGuid = guidText.Length == 0 ? "Unavailable" : guidText.ToString();
                    }

                    var mappingPointer = Sdl2Native.SDL_GameControllerMapping(controller);
                    if (mappingPointer != IntPtr.Zero)
                    {
                        try
                        {
                            cachedSdlMapping = Marshal.PtrToStringAnsi(mappingPointer) ?? "Unavailable";
                        }
                        finally
                        {
                            Sdl2Native.SDL_free(mappingPointer);
                        }
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    cachedSdlVersion = "SDL2 (technical detail API unavailable)";
                }
                catch (Exception)
                {
                    // Technical metadata is best-effort and must never stop controller input.
                }
                finally
                {
                    technicalDetailsInitialized = true;
                }
            }

            state.SdlVersion = cachedSdlVersion;
            state.SdlGuid = cachedSdlGuid;
            state.SdlMapping = cachedSdlMapping;
            state.AxisCount = cachedAxisCount;
            state.ButtonCount = cachedButtonCount;
            state.HatCount = cachedHatCount;
        }

        private static float NormalizeAxis(short value)
        {
            var normalized = value < 0 ? value / 32768f : value / 32767f;
            return Clamp(normalized, -1f, 1f);
        }

        private static float NormalizeTrigger(short value)
        {
            return Clamp(value / 32767f, 0f, 1f);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            if (value > maximum)
            {
                return maximum;
            }

            return value;
        }

        private void CloseController()
        {
            if (controller != IntPtr.Zero)
            {
                Sdl2Native.SDL_GameControllerClose(controller);
                controller = IntPtr.Zero;
            }

            technicalDetailsInitialized = false;
            cachedSdlVersion = "Unknown";
            cachedSdlGuid = "Unavailable";
            cachedSdlMapping = "Unavailable";
            cachedAxisCount = 0;
            cachedButtonCount = 0;
            cachedHatCount = 0;
        }

        public void Dispose()
        {
            CloseController();
            Sdl2Native.SDL_QuitSubSystem(initFlags);
        }
    }
}
