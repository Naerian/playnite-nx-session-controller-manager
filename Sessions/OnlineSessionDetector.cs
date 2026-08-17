using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace ControllerSessionManager.Sessions
{
    internal enum OnlineEvidenceKind
    {
        None,
        Metadata,
        EstablishedTcpConnection
    }

    internal sealed class OnlineDetectionResult
    {
        public OnlineEvidenceKind Evidence { get; set; }
        public string Detail { get; set; }
        public bool IsOnlineLikely { get { return Evidence != OnlineEvidenceKind.None; } }
        public bool IsNotificationOnlySafe { get { return Evidence == OnlineEvidenceKind.Metadata; } }
    }

    internal sealed class OnlineSessionDetector
    {
        private const int AfInet = 2;
        private const int AfInet6 = 23;
        private const int TcpTableOwnerPidAll = 5;
        private const int ErrorInsufficientBuffer = 122;
        private const uint TcpStateEstablished = 5;

        private static readonly string[] OnlineMarkers =
        {
            "online only", "online-only", "always online", "always-online", "mmo", "mmorpg",
            "solo online", "toujours en ligne", "nur online", "sempre online", "sempre on-line",
            "только онлайн", "온라인 전용"
        };

        public OnlineDetectionResult Detect(IEnumerable<string> metadata, ISet<int> processIds)
        {
            string marker;
            if (HasOnlineMetadata(metadata, out marker))
            {
                return new OnlineDetectionResult { Evidence = OnlineEvidenceKind.Metadata, Detail = marker };
            }

            int ownerPid;
            if (HasEstablishedTcpConnection(processIds, out ownerPid))
            {
                return new OnlineDetectionResult
                {
                    Evidence = OnlineEvidenceKind.EstablishedTcpConnection,
                    Detail = ownerPid.ToString()
                };
            }

            return new OnlineDetectionResult { Evidence = OnlineEvidenceKind.None };
        }

        internal static bool HasOnlineMetadata(IEnumerable<string> metadata, out string matchedValue)
        {
            matchedValue = null;
            foreach (var value in metadata ?? Enumerable.Empty<string>())
            {
                var normalized = RemoveDiacritics((value ?? string.Empty).Trim().ToLowerInvariant());
                if (normalized.Length == 0)
                {
                    continue;
                }
                if (OnlineMarkers.Any(marker => normalized.Contains(marker)))
                {
                    matchedValue = value;
                    return true;
                }
            }
            return false;
        }

        private static bool HasEstablishedTcpConnection(ISet<int> processIds, out int ownerPid)
        {
            ownerPid = 0;
            if (processIds == null || processIds.Count == 0)
            {
                return false;
            }

            return HasEstablishedIpv4Connection(processIds, out ownerPid) ||
                HasEstablishedIpv6Connection(processIds, out ownerPid);
        }

        private static bool HasEstablishedIpv4Connection(ISet<int> processIds, out int ownerPid)
        {
            ownerPid = 0;
            var size = 0;
            var result = GetExtendedTcpTable(IntPtr.Zero, ref size, true, AfInet, TcpTableOwnerPidAll, 0);
            if (result != ErrorInsufficientBuffer || size <= 0)
            {
                return false;
            }

            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                result = GetExtendedTcpTable(buffer, ref size, true, AfInet, TcpTableOwnerPidAll, 0);
                if (result != 0)
                {
                    return false;
                }

                var count = Marshal.ReadInt32(buffer);
                var rowSize = Marshal.SizeOf(typeof(TcpRowOwnerPid));
                var rowPointer = IntPtr.Add(buffer, sizeof(int));
                for (var index = 0; index < count; index++)
                {
                    var row = (TcpRowOwnerPid)Marshal.PtrToStructure(
                        IntPtr.Add(rowPointer, index * rowSize), typeof(TcpRowOwnerPid));
                    var pid = unchecked((int)row.OwningPid);
                    if (row.State == TcpStateEstablished && processIds.Contains(pid) &&
                        IsRemoteAddress(row.RemoteAddress))
                    {
                        ownerPid = pid;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return false;
        }

        private static bool HasEstablishedIpv6Connection(ISet<int> processIds, out int ownerPid)
        {
            ownerPid = 0;
            var size = 0;
            var result = GetExtendedTcpTable(IntPtr.Zero, ref size, true, AfInet6, TcpTableOwnerPidAll, 0);
            if (result != ErrorInsufficientBuffer || size <= 0)
            {
                return false;
            }

            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                result = GetExtendedTcpTable(buffer, ref size, true, AfInet6, TcpTableOwnerPidAll, 0);
                if (result != 0)
                {
                    return false;
                }

                var count = Marshal.ReadInt32(buffer);
                var rowSize = Marshal.SizeOf(typeof(Tcp6RowOwnerPid));
                var rowPointer = IntPtr.Add(buffer, sizeof(int));
                for (var index = 0; index < count; index++)
                {
                    var row = (Tcp6RowOwnerPid)Marshal.PtrToStructure(
                        IntPtr.Add(rowPointer, index * rowSize), typeof(Tcp6RowOwnerPid));
                    var pid = unchecked((int)row.OwningPid);
                    if (row.State == TcpStateEstablished && processIds.Contains(pid) &&
                        IsRemoteAddress(row.RemoteAddress))
                    {
                        ownerPid = pid;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return false;
        }

        private static bool IsRemoteAddress(uint nativeAddress)
        {
            var bytes = BitConverter.GetBytes(nativeAddress);
            var address = new IPAddress(bytes);
            return !IPAddress.IsLoopback(address) && !address.Equals(IPAddress.Any) &&
                !address.Equals(IPAddress.None);
        }

        private static bool IsRemoteAddress(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 16)
            {
                return false;
            }
            var address = new IPAddress(bytes);
            return !IPAddress.IsLoopback(address) && !address.Equals(IPAddress.IPv6Any) &&
                !address.Equals(IPAddress.IPv6None);
        }

        private static string RemoveDiacritics(string value)
        {
            var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
            return new string(normalized.Where(c =>
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TcpRowOwnerPid
        {
            public uint State;
            public uint LocalAddress;
            public uint LocalPort;
            public uint RemoteAddress;
            public uint RemotePort;
            public uint OwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Tcp6RowOwnerPid
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] LocalAddress;
            public uint LocalScopeId;
            public uint LocalPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] RemoteAddress;
            public uint RemoteScopeId;
            public uint RemotePort;
            public uint State;
            public uint OwningPid;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool order,
            int ipVersion, int tableClass, uint reserved);
    }
}
