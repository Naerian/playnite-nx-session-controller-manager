using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ControllerSessionManager.Tester.Services
{
    internal static class Sdl2Native
    {
        public const uint SdlInitHaptic = 0x00001000;
        public const uint SdlInitGameController = 0x00002000;
        public const uint SdlInitEvents = 0x00004000;

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_Init(uint flags);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_QuitSubSystem(uint flags);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_RWFromFile(string file, string mode);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_GameControllerAddMappingsFromRW(IntPtr rw, int freerw);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_NumJoysticks();

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_IsGameController(int joystickIndex);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GameControllerNameForIndex(int joystickIndex);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort SDL_JoystickGetDeviceVendor(int joystickIndex);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort SDL_JoystickGetDeviceProduct(int joystickIndex);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_JoystickGetDeviceInstanceID(int joystickIndex);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GameControllerOpen(int joystickIndex);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_GameControllerClose(IntPtr gamecontroller);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_GameControllerGetAttached(IntPtr gamecontroller);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GameControllerName(IntPtr gamecontroller);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GameControllerGetJoystick(IntPtr gamecontroller);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort SDL_JoystickGetVendor(IntPtr joystick);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort SDL_JoystickGetProduct(IntPtr joystick);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_JoystickNumButtons(IntPtr joystick);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte SDL_JoystickGetButton(IntPtr joystick, int button);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_GameControllerUpdate();

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDL_GameControllerGetAxis(IntPtr gamecontroller, SdlControllerAxis axis);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte SDL_GameControllerGetButton(IntPtr gamecontroller, SdlControllerButton button);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_GameControllerRumble(IntPtr gamecontroller, ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_GetVersion(out SdlVersion version);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern SdlJoystickGuid SDL_JoystickGetGUID(IntPtr joystick);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_JoystickGetGUIDString(SdlJoystickGuid guid, StringBuilder text, int textSize);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_JoystickNumAxes(IntPtr joystick);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDL_JoystickNumHats(IntPtr joystick);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GameControllerMapping(IntPtr gamecontroller);

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_free(IntPtr memory);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SdlVersion
    {
        public byte Major;
        public byte Minor;
        public byte Patch;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SdlJoystickGuid
    {
        public ulong First;
        public ulong Second;
    }

    internal enum SdlControllerAxis
    {
        LeftX = 0,
        LeftY = 1,
        RightX = 2,
        RightY = 3,
        TriggerLeft = 4,
        TriggerRight = 5
    }

    internal enum SdlControllerButton
    {
        A = 0,
        B = 1,
        X = 2,
        Y = 3,
        Back = 4,
        Guide = 5,
        Start = 6,
        LeftStick = 7,
        RightStick = 8,
        LeftShoulder = 9,
        RightShoulder = 10,
        DpadUp = 11,
        DpadDown = 12,
        DpadLeft = 13,
        DpadRight = 14,
        Touchpad = 20
    }
}
