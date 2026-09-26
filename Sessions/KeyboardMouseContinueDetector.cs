using System;
using System.Runtime.InteropServices;

namespace ControllerSessionManager.Sessions
{
    /// <summary>
    /// Detects intentional keyboard or mouse-button edges so a disconnect incident can be
    /// dismissed when the player continues without the missing controller.
    /// Mouse movement alone is ignored. Escape is tracked separately so it can dismiss even
    /// in local multiplayer and when the game is not in the foreground.
    /// </summary>
    internal sealed class KeyboardMouseContinueDetector
    {
        private const ushort VkEscape = 0x1B;

        private static readonly ushort[] WatchedVirtualKeys =
        {
            0x01, // VK_LBUTTON
            0x02, // VK_RBUTTON
            0x04, // VK_MBUTTON
            0x20, // VK_SPACE
            0x0D, // VK_RETURN
            0x25, // VK_LEFT
            0x26, // VK_UP
            0x27, // VK_RIGHT
            0x28, // VK_DOWN
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, // 0-9
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, // A-J
            0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, // K-T
            0x55, 0x56, 0x57, 0x58, 0x59, 0x5A // U-Z
        };

        private readonly bool[] previousDown = new bool[WatchedVirtualKeys.Length];
        private bool previousEscapeDown;
        private bool armed;
        private bool escapeArmed;

        public void Reset()
        {
            armed = false;
            escapeArmed = false;
            previousEscapeDown = false;
            Array.Clear(previousDown, 0, previousDown.Length);
        }

        /// <summary>
        /// Updates the baseline from the current physical key state without emitting a detection.
        /// Use while the game is not in the foreground so later play does not inherit stale edges.
        /// </summary>
        public void SyncBaseline()
        {
            SampleInto(previousDown);
            previousEscapeDown = IsDown(VkEscape);
            armed = true;
            escapeArmed = true;
        }

        public bool TryDetectEscape(out string evidence)
        {
            var current = IsDown(VkEscape);
            return TryDetectEscapeTransition(ref previousEscapeDown, current, ref escapeArmed,
                out evidence);
        }

        internal bool TryDetectEscape(bool previous, bool current, out string evidence)
        {
            var previousState = previous;
            var armedState = escapeArmed;
            var detected = TryDetectEscapeTransition(ref previousState, current, ref armedState,
                out evidence);
            previousEscapeDown = previousState;
            escapeArmed = armedState;
            return detected;
        }

        private static bool TryDetectEscapeTransition(ref bool previousState, bool current,
            ref bool armedState, out string evidence)
        {
            evidence = null;
            if (!armedState)
            {
                previousState = current;
                armedState = true;
                return false;
            }

            if (current && !previousState)
            {
                previousState = current;
                evidence = "Escape";
                return true;
            }

            previousState = current;
            return false;
        }

        public bool TryDetect(out string evidence)
        {
            var current = new bool[WatchedVirtualKeys.Length];
            SampleInto(current);
            return TryDetect(previousDown, current, out evidence);
        }

        internal bool TryDetect(bool[] previous, bool[] current, out string evidence)
        {
            evidence = null;
            if (previous == null || current == null ||
                previous.Length != WatchedVirtualKeys.Length ||
                current.Length != WatchedVirtualKeys.Length)
            {
                return false;
            }

            if (!armed)
            {
                Array.Copy(current, previous, current.Length);
                armed = true;
                return false;
            }

            for (var i = 0; i < WatchedVirtualKeys.Length; i++)
            {
                if (current[i] && !previous[i])
                {
                    evidence = Describe(WatchedVirtualKeys[i]);
                    Array.Copy(current, previous, current.Length);
                    return true;
                }
            }

            Array.Copy(current, previous, current.Length);
            return false;
        }

        internal static int WatchedKeyCount
        {
            get { return WatchedVirtualKeys.Length; }
        }

        internal static ushort GetWatchedVirtualKey(int index)
        {
            return WatchedVirtualKeys[index];
        }

        private static void SampleInto(bool[] target)
        {
            for (var i = 0; i < WatchedVirtualKeys.Length; i++)
            {
                target[i] = IsDown(WatchedVirtualKeys[i]);
            }
        }

        private static bool IsDown(ushort virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        private static string Describe(ushort virtualKey)
        {
            switch (virtualKey)
            {
                case 0x01: return "MouseLeft";
                case 0x02: return "MouseRight";
                case 0x04: return "MouseMiddle";
                case 0x20: return "Space";
                case 0x0D: return "Enter";
                case 0x25: return "ArrowLeft";
                case 0x26: return "ArrowUp";
                case 0x27: return "ArrowRight";
                case 0x28: return "ArrowDown";
                default:
                    if (virtualKey >= 0x30 && virtualKey <= 0x39)
                    {
                        return "Digit" + (char)virtualKey;
                    }
                    if (virtualKey >= 0x41 && virtualKey <= 0x5A)
                    {
                        return "Key" + (char)virtualKey;
                    }
                    return "Vk" + virtualKey.ToString("X2");
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);
    }
}
