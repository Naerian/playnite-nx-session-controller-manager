using System;
using System.Collections.Generic;
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
        /// <summary>Foreground window belongs to the game process tree and can be suspended.</summary>
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

    /// <summary>
    /// Resolves whether the foreground window belongs to the launched game process tree.
    /// Automatic pause uses OverlayHost process suspension; this service does not send keys.
    /// </summary>
    internal sealed class GamePauseService
    {
        private const uint ProcessSnapshot = 0x00000002;
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

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
