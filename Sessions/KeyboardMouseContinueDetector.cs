using System;
using System.Runtime.InteropServices;

namespace ControllerSessionManager.Sessions
{
    /// <summary>
    /// Detects intentional keyboard or mouse-button edges so a single-player disconnect
    /// incident can be dismissed when the player continues with keyboard and mouse.
    /// Mouse movement alone is ignored. Escape is ignored to avoid colliding with common game pause shortcuts.
    /// </summary>
    internal sealed class KeyboardMouseContinueDetector
    {
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
        private bool armed;

        public void Reset()
        {
            armed = false;
            Array.Clear(previousDown, 0, previousDown.Length);
        }

        /// <summary>
        /// Updates the baseline from the current physical key state without emitting a detection.
        /// Use while the game is not in the foreground so later play does not inherit stale edges.
        /// </summary>
        public void SyncBaseline()
        {
            SampleInto(previousDown);
            armed = true;
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
                target[i] = (GetAsyncKeyState(WatchedVirtualKeys[i]) & 0x8000) != 0;
            }
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
