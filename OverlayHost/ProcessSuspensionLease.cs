using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ControllerSessionManager.OverlayHost
{
    internal sealed class ProcessSuspensionLease : IDisposable
    {
        private const uint ProcessSuspendResume = 0x0800;
        private const uint ProcessQueryLimitedInformation = 0x1000;
        private IntPtr processHandle;
        private int processId;
        private string incidentId;

        public bool IsActive { get { return processHandle != IntPtr.Zero; } }

        public bool TrySuspend(int targetProcessId, string targetIncidentId)
        {
            if (IsActive && processId == targetProcessId &&
                string.Equals(incidentId, targetIncidentId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Resume();
            if (targetProcessId <= 0 || targetProcessId == Process.GetCurrentProcess().Id)
            {
                return false;
            }

            try
            {
                using (var process = Process.GetProcessById(targetProcessId))
                {
                    if (process.HasExited)
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }

            var handle = OpenProcess(ProcessSuspendResume | ProcessQueryLimitedInformation,
                false, targetProcessId);
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            if (NtSuspendProcess(handle) != 0)
            {
                CloseHandle(handle);
                return false;
            }

            processHandle = handle;
            processId = targetProcessId;
            incidentId = targetIncidentId;
            return true;
        }

        public void Resume()
        {
            var handle = processHandle;
            processHandle = IntPtr.Zero;
            processId = 0;
            incidentId = null;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                NtResumeProcess(handle);
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        public void Dispose()
        {
            Resume();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("ntdll.dll")]
        private static extern int NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll")]
        private static extern int NtResumeProcess(IntPtr processHandle);
    }
}
