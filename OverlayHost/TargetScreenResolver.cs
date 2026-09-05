using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace ControllerSessionManager.OverlayHost
{
    /// <summary>
    /// Picks the monitor for a process tree (and optional preferred HWND), so overlays/toasts
    /// follow the game, Playnite, or a settings dialog instead of always the primary display.
    /// </summary>
    internal static class TargetScreenResolver
    {
        public static Forms.Screen Resolve(int rootProcessId, IntPtr preferredWindow = default(IntPtr))
        {
            try
            {
                if (preferredWindow != IntPtr.Zero &&
                    IsWindow(preferredWindow) &&
                    IsWindowVisible(preferredWindow))
                {
                    return Forms.Screen.FromHandle(preferredWindow);
                }

                if (rootProcessId <= 0)
                {
                    return Forms.Screen.PrimaryScreen;
                }

                var processIds = GetProcessTree(rootProcessId);
                var foreground = GetForegroundWindow();
                if (foreground != IntPtr.Zero &&
                    IsWindowVisible(foreground) &&
                    BelongsToProcessTree(foreground, processIds))
                {
                    // Include owned dialogs (e.g. Playnite addon settings) — GW_OWNER filtered
                    // those out and forced the toast onto the main Playnite window's monitor.
                    return Forms.Screen.FromHandle(foreground);
                }

                var bestWindow = FindLargestVisibleWindow(processIds);
                if (bestWindow != IntPtr.Zero)
                {
                    return Forms.Screen.FromHandle(bestWindow);
                }

                using (var process = Process.GetProcessById(rootProcessId))
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return Forms.Screen.FromHandle(process.MainWindowHandle);
                    }
                }
            }
            catch
            {
            }

            return Forms.Screen.PrimaryScreen;
        }

        private static bool BelongsToProcessTree(IntPtr hwnd, ISet<int> processIds)
        {
            uint windowProcessId;
            GetWindowThreadProcessId(hwnd, out windowProcessId);
            return processIds.Contains(unchecked((int)windowProcessId));
        }

        private static HashSet<int> GetProcessTree(int rootProcessId)
        {
            var result = new HashSet<int> { rootProcessId };
            Dictionary<int, int> parents;
            try
            {
                parents = CaptureProcessParents();
            }
            catch
            {
                return result;
            }

            foreach (var processId in parents.Keys)
            {
                if (IsProcessInTree(processId, rootProcessId, parents))
                {
                    result.Add(processId);
                }
            }

            return result;
        }

        private static bool IsProcessInTree(int candidateProcessId, int rootProcessId,
            IReadOnlyDictionary<int, int> parents)
        {
            if (candidateProcessId <= 0 || rootProcessId <= 0)
            {
                return false;
            }

            if (candidateProcessId == rootProcessId)
            {
                return true;
            }

            var current = candidateProcessId;
            for (var depth = 0; depth < 32; depth++)
            {
                int parent;
                if (!parents.TryGetValue(current, out parent) || parent <= 0 || parent == current)
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

        private static Dictionary<int, int> CaptureProcessParents()
        {
            var result = new Dictionary<int, int>();
            var snapshot = CreateToolhelp32Snapshot(0x2, 0);
            if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
            {
                return result;
            }

            try
            {
                var entry = new ProcessEntry32
                {
                    DwSize = (uint)Marshal.SizeOf(typeof(ProcessEntry32))
                };
                if (!Process32First(snapshot, ref entry))
                {
                    return result;
                }

                do
                {
                    result[unchecked((int)entry.ProcessId)] = unchecked((int)entry.ParentProcessId);
                }
                while (Process32Next(snapshot, ref entry));
            }
            finally
            {
                CloseHandle(snapshot);
            }

            return result;
        }

        private static IntPtr FindLargestVisibleWindow(ISet<int> processIds)
        {
            var best = IntPtr.Zero;
            var bestArea = 0L;
            EnumWindows((hwnd, lParam) =>
            {
                if (!IsUsableTopLevelWindow(hwnd) || !BelongsToProcessTree(hwnd, processIds))
                {
                    return true;
                }

                RECT rect;
                if (!GetWindowRect(hwnd, out rect))
                {
                    return true;
                }

                var width = (long)rect.Right - rect.Left;
                var height = (long)rect.Bottom - rect.Top;
                if (width <= 0 || height <= 0)
                {
                    return true;
                }

                var area = width * height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = hwnd;
                }

                return true;
            }, IntPtr.Zero);

            return best;
        }

        private static bool IsUsableTopLevelWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindowVisible(hwnd))
            {
                return false;
            }

            // GW_OWNER: owned popups are usually dialogs; for "largest window" prefer the
            // true top-level surface (game / main Playnite). Preferred/foreground paths
            // handle settings dialogs explicitly.
            return GetWindow(hwnd, 4) == IntPtr.Zero;
        }

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry32 entry);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry32 entry);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ProcessEntry32
        {
            public uint DwSize;
            public uint CntUsage;
            public uint ProcessId;
            public IntPtr DefaultHeapId;
            public uint ModuleId;
            public uint CntThreads;
            public uint ParentProcessId;
            public int PcPriClassBase;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string SzExeFile;
        }
    }
}
