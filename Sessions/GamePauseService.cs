using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ControllerSessionManager.Sessions
{
    internal sealed class PauseAttemptGate
    {
        private bool attempted;

        public bool TryBegin()
        {
            if (attempted)
            {
                return false;
            }
            attempted = true;
            return true;
        }

        public void Reset()
        {
            attempted = false;
        }
    }

    internal enum PauseAttemptStatus
    {
        Sent,
        GameProcessUnavailable,
        ForegroundUnavailable,
        ForegroundNotGame,
        SendFailed
    }

    internal sealed class PauseReceipt
    {
        public PauseAttemptStatus Status { get; set; }
        public int TargetProcessId { get; set; }
        public IntPtr TargetWindow { get; set; }
        public DateTime AttemptedUtc { get; set; }

        public bool WasSent
        {
            get { return Status == PauseAttemptStatus.Sent; }
        }
    }

    internal sealed class GamePauseService
    {
        private const ushort VirtualKeyEscape = 0x1B;
        private const uint InputKeyboard = 1;
        private const uint KeyEventKeyUp = 0x0002;
        private const uint ProcessSnapshot = 0x00000002;
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        internal static int NativeInputSize
        {
            get { return Marshal.SizeOf(typeof(Input)); }
        }

        public PauseReceipt TrySendEscape(int gameProcessId, DateTime nowUtc)
        {
            return TrySendKey(gameProcessId, "Escape", nowUtc);
        }

        public PauseReceipt TrySendKey(int gameProcessId, string keyName, DateTime nowUtc)
        {
            var receipt = ResolveForegroundTarget(gameProcessId, nowUtc);
            ushort virtualKey;
            if (!TryGetVirtualKey(keyName, out virtualKey))
            {
                receipt.Status = PauseAttemptStatus.SendFailed;
                return receipt;
            }
            if (receipt.Status != PauseAttemptStatus.Sent)
            {
                return receipt;
            }

            var inputs = new[]
            {
                KeyboardInput(virtualKey, 0),
                KeyboardInput(virtualKey, KeyEventKeyUp)
            };
            receipt.Status = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input))) ==
                (uint)inputs.Length
                ? PauseAttemptStatus.Sent
                : PauseAttemptStatus.SendFailed;
            return receipt;
        }

        public PauseReceipt ResolveForegroundTarget(int gameProcessId, DateTime nowUtc)
        {
            var receipt = new PauseReceipt { AttemptedUtc = nowUtc };
            if (gameProcessId <= 0)
            {
                receipt.Status = PauseAttemptStatus.GameProcessUnavailable;
                return receipt;
            }

            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                receipt.Status = PauseAttemptStatus.ForegroundUnavailable;
                return receipt;
            }

            uint foregroundProcessId;
            GetWindowThreadProcessId(foreground, out foregroundProcessId);
            receipt.TargetWindow = foreground;
            receipt.TargetProcessId = unchecked((int)foregroundProcessId);
            if (!IsProcessInTree(receipt.TargetProcessId, gameProcessId, CaptureProcessParents()))
            {
                receipt.Status = PauseAttemptStatus.ForegroundNotGame;
                return receipt;
            }

            uint verifiedProcessId;
            if (foreground != GetForegroundWindow())
            {
                receipt.Status = PauseAttemptStatus.ForegroundNotGame;
                return receipt;
            }
            GetWindowThreadProcessId(foreground, out verifiedProcessId);
            receipt.Status = verifiedProcessId == foregroundProcessId
                ? PauseAttemptStatus.Sent
                : PauseAttemptStatus.ForegroundNotGame;
            return receipt;
        }

        public ISet<int> GetProcessTree(int rootProcessId)
        {
            var result = new HashSet<int>();
            if (rootProcessId <= 0)
            {
                return result;
            }

            var parents = CaptureProcessParents();
            foreach (var processId in parents.Keys)
            {
                if (IsProcessInTree(processId, rootProcessId, parents))
                {
                    result.Add(processId);
                }
            }
            result.Add(rootProcessId);
            return result;
        }

        internal static bool IsSupportedKey(string keyName)
        {
            ushort ignored;
            return TryGetVirtualKey(keyName, out ignored);
        }

        private static bool TryGetVirtualKey(string keyName, out ushort virtualKey)
        {
            virtualKey = 0;
            var value = (keyName ?? string.Empty).Trim().ToUpperInvariant();
            if ((value.Length == 1 && value[0] >= 'A' && value[0] <= 'Z') ||
                (value.Length == 1 && value[0] >= '0' && value[0] <= '9'))
            {
                virtualKey = value[0];
                return true;
            }
            if (value.StartsWith("F", StringComparison.Ordinal) && value.Length <= 3)
            {
                int functionNumber;
                if (int.TryParse(value.Substring(1), out functionNumber) &&
                    functionNumber >= 1 && functionNumber <= 12)
                {
                    virtualKey = (ushort)(0x70 + functionNumber - 1);
                    return true;
                }
            }

            switch (value)
            {
                case "ESC":
                case "ESCAPE": virtualKey = VirtualKeyEscape; return true;
                case "SPACE": virtualKey = 0x20; return true;
                case "ENTER":
                case "RETURN": virtualKey = 0x0D; return true;
                case "TAB": virtualKey = 0x09; return true;
                case "BACKSPACE": virtualKey = 0x08; return true;
                default: return false;
            }
        }

        internal static bool IsProcessInTree(int candidateProcessId, int rootProcessId,
            IDictionary<int, int> parentByProcess)
        {
            if (candidateProcessId <= 0 || rootProcessId <= 0)
            {
                return false;
            }
            if (candidateProcessId == rootProcessId)
            {
                return true;
            }

            var visited = new HashSet<int>();
            var current = candidateProcessId;
            while (current > 0 && visited.Add(current))
            {
                int parent;
                if (parentByProcess == null || !parentByProcess.TryGetValue(current, out parent))
                {
                    return false;
                }
                if (parent == rootProcessId)
                {
                    return true;
                }
                current = parent;
            }
            return false;
        }

        private static IDictionary<int, int> CaptureProcessParents()
        {
            var result = new Dictionary<int, int>();
            var snapshot = CreateToolhelp32Snapshot(ProcessSnapshot, 0);
            if (snapshot == InvalidHandleValue)
            {
                return result;
            }

            try
            {
                var entry = new ProcessEntry32 { Size = (uint)Marshal.SizeOf(typeof(ProcessEntry32)) };
                if (!Process32First(snapshot, ref entry))
                {
                    return result;
                }
                do
                {
                    result[unchecked((int)entry.ProcessId)] = unchecked((int)entry.ParentProcessId);
                    entry.Size = (uint)Marshal.SizeOf(typeof(ProcessEntry32));
                }
                while (Process32Next(snapshot, ref entry));
            }
            finally
            {
                CloseHandle(snapshot);
            }
            return result;
        }

        private static Input KeyboardInput(ushort virtualKey, uint flags)
        {
            return new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    Keyboard = new KeyboardInputData
                    {
                        VirtualKey = virtualKey,
                        Flags = flags
                    }
                }
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public uint Type;
            public InputUnion Union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public KeyboardInputData Keyboard;
            [FieldOffset(0)]
            public MouseInputData Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInputData
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInputData
        {
            public ushort VirtualKey;
            public ushort ScanCode;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ProcessEntry32
        {
            public uint Size;
            public uint Usage;
            public uint ProcessId;
            public IntPtr DefaultHeapId;
            public uint ModuleId;
            public uint Threads;
            public uint ParentProcessId;
            public int BasePriority;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string ExeFile;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry32 entry);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry32 entry);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
