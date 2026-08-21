using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ControllerSessionManager.OverlayHost
{
    /// <summary>
    /// Survives DesktopApp shutdown after SwitchAppMode and activates Fullscreen so the
    /// Windows taskbar does not stay stuck on top until the user clicks.
    /// </summary>
    internal static class FullscreenFocusHelper
    {
        private const int PollMs = 100;
        private const int MaxWaitMs = 8000;
        private const int ActivateBurstMs = 2500;
        private const int SwShow = 5;

        public static void Run()
        {
            var deadline = Environment.TickCount + MaxWaitMs;
            IntPtr hwnd = IntPtr.Zero;
            while (Environment.TickCount < deadline)
            {
                hwnd = FindFullscreenMainWindow();
                if (hwnd != IntPtr.Zero)
                {
                    break;
                }

                Thread.Sleep(PollMs);
            }

            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var burstDeadline = Environment.TickCount + ActivateBurstMs;
            var clicked = false;
            while (Environment.TickCount < burstDeadline)
            {
                hwnd = FindFullscreenMainWindow();
                if (hwnd != IntPtr.Zero)
                {
                    ActivateWindow(hwnd, !clicked);
                    clicked = true;
                }

                Thread.Sleep(200);
            }
        }

        private static IntPtr FindFullscreenMainWindow()
        {
            foreach (var process in Process.GetProcessesByName("Playnite.FullscreenApp"))
            {
                try
                {
                    var handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        return handle;
                    }
                }
                catch
                {
                    // Process may exit while Desktop hands off.
                }
            }

            return IntPtr.Zero;
        }

        private static void ActivateWindow(IntPtr hwnd, bool synthesizeClick)
        {
            if (IsIconic(hwnd))
            {
                ShowWindow(hwnd, SwShow);
            }

            uint processId;
            GetWindowThreadProcessId(hwnd, out processId);
            if (processId != 0)
            {
                AllowSetForegroundWindow((int)processId);
            }

            var foreground = GetForegroundWindow();
            uint unusedPid;
            var foregroundThread = GetWindowThreadProcessId(foreground, out unusedPid);
            var currentThread = GetCurrentThreadId();
            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                AttachThreadInput(foregroundThread, currentThread, true);
                BringWindowToTop(hwnd);
                ShowWindow(hwnd, SwShow);
                SetForegroundWindow(hwnd);
                AttachThreadInput(foregroundThread, currentThread, false);
            }
            else
            {
                BringWindowToTop(hwnd);
                SetForegroundWindow(hwnd);
            }

            // A real click is what clears the stuck taskbar for users; synthesize one at the
            // top edge (usually chrome/empty) and restore the cursor afterwards.
            if (synthesizeClick)
            {
                TryClickTopCenter(hwnd);
                SetForegroundWindow(hwnd);
            }
        }

        private static void TryClickTopCenter(IntPtr hwnd)
        {
            RECT rect;
            if (!GetWindowRect(hwnd, out rect))
            {
                return;
            }

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width < 50 || height < 50)
            {
                return;
            }

            var targetX = rect.Left + (width / 2);
            var targetY = rect.Top + Math.Min(8, height / 20);

            POINT previous;
            var hadPrevious = GetCursorPos(out previous);
            try
            {
                SetCursorPos(targetX, targetY);
                mouse_event(MouseeventfLeftdown, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseeventfLeftup, 0, 0, 0, UIntPtr.Zero);
            }
            finally
            {
                if (hadPrevious)
                {
                    SetCursorPos(previous.X, previous.Y);
                }
            }
        }

        private const uint MouseeventfLeftdown = 0x0002;
        private const uint MouseeventfLeftup = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int processId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    }
}
