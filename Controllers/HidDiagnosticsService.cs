using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace ControllerSessionManager.Controllers
{
    public static class HidDiagnosticsService
    {
        private const uint DigcfPresent = 0x00000002;
        private const uint DigcfDeviceInterface = 0x00000010;
        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;
        private const uint OpenExisting = 3;
        private const uint FileFlagOverlapped = 0x40000000;
        private const int ErrorIoPending = 997;
        private const uint WaitObject0 = 0;

        public static string CreateReport(IEnumerable<ControllerDeviceSnapshot> controllers)
        {
            var devices = (controllers ?? Enumerable.Empty<ControllerDeviceSnapshot>())
                .Where(a => a.IsConnected && a.VendorId != 0)
                .GroupBy(a => string.Format("{0:X4}:{1:X4}", a.VendorId, a.ProductId))
                .Select(a => a.First())
                .ToList();
            var output = new StringBuilder();
            output.AppendLine("Controller Session Manager - HID diagnostic");
            output.AppendLine("Generated: " + DateTime.Now.ToString("O"));
            output.AppendLine("Mode: read-only inventory and current report capture");
            output.AppendLine();
            output.AppendLine("Connected controller identities:");
            foreach (var device in devices)
            {
                output.AppendLine(string.Format("- {0} | VID={1:X4} PID={2:X4} | {3}",
                    device.DetectedName ?? device.Name, device.VendorId, device.ProductId, device.HardwareId));
            }

            output.AppendLine();
            output.AppendLine("HID interfaces:");
            EnumerateInterfaces(devices, output);
            return output.ToString();
        }

        public static bool HasUsbInterface(ushort vendorId, ushort productId)
        {
            var marker = string.Format("VID_{0:X4}&PID_{1:X4}", vendorId, productId);
            using (var usbRoot = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB"))
            {
                if (usbRoot == null)
                {
                    return false;
                }

                foreach (var deviceName in usbRoot.GetSubKeyNames().Where(a =>
                    a.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    using (var deviceKey = usbRoot.OpenSubKey(deviceName))
                    {
                        if (deviceKey == null)
                        {
                            continue;
                        }

                        foreach (var instanceName in deviceKey.GetSubKeyNames())
                        {
                            uint deviceInstance;
                            var instanceId = string.Format("USB\\{0}\\{1}", deviceName, instanceName);
                            if (NativeMethods.CM_Locate_DevNode(out deviceInstance, instanceId, 0) == 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static bool TryReadInputReport(ushort vendorId, ushort productId,
            int timeoutMilliseconds, out byte[] report)
        {
            report = null;
            Guid hidGuid;
            NativeMethods.HidD_GetHidGuid(out hidGuid);
            var deviceInfoSet = NativeMethods.SetupDiGetClassDevs(
                ref hidGuid, IntPtr.Zero, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
            if (deviceInfoSet == new IntPtr(-1))
            {
                return false;
            }

            try
            {
                for (uint index = 0; ; index++)
                {
                    var interfaceData = new SpDeviceInterfaceData
                    {
                        Size = Marshal.SizeOf(typeof(SpDeviceInterfaceData))
                    };
                    if (!NativeMethods.SetupDiEnumDeviceInterfaces(
                        deviceInfoSet, IntPtr.Zero, ref hidGuid, index, ref interfaceData))
                    {
                        break;
                    }

                    uint requiredSize;
                    NativeMethods.SetupDiGetDeviceInterfaceDetail(
                        deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);
                    if (requiredSize == 0)
                    {
                        continue;
                    }

                    var detail = Marshal.AllocHGlobal((int)requiredSize);
                    try
                    {
                        Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                        if (!NativeMethods.SetupDiGetDeviceInterfaceDetail(
                            deviceInfoSet, ref interfaceData, detail, requiredSize, out requiredSize, IntPtr.Zero))
                        {
                            continue;
                        }

                        var path = Marshal.PtrToStringUni(IntPtr.Add(detail, IntPtr.Size == 8 ? 8 : 4));
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }

                        using (var handle = OpenHidDevice(path))
                        {
                            var attributes = new HiddAttributes { Size = Marshal.SizeOf(typeof(HiddAttributes)) };
                            if (handle == null || handle.IsInvalid ||
                                !NativeMethods.HidD_GetAttributes(handle, ref attributes) ||
                                attributes.VendorId != vendorId || attributes.ProductId != productId)
                            {
                                continue;
                            }

                            IntPtr preparsedData;
                            if (!NativeMethods.HidD_GetPreparsedData(handle, out preparsedData))
                            {
                                continue;
                            }
                            try
                            {
                                HidpCaps caps;
                                if (NativeMethods.HidP_GetCaps(preparsedData, out caps) < 0 ||
                                    caps.InputReportByteLength <= 0 || caps.InputReportByteLength > 4096)
                                {
                                    continue;
                                }

                                byte[] captured;
                                if (TryReadStreamReport(handle, caps.InputReportByteLength,
                                    Math.Max(20, Math.Min(500, timeoutMilliseconds)), out captured))
                                {
                                    report = captured;
                                    return true;
                                }
                            }
                            finally
                            {
                                NativeMethods.HidD_FreePreparsedData(preparsedData);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detail);
                    }
                }
            }
            finally
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return false;
        }

        private static void EnumerateInterfaces(IReadOnlyCollection<ControllerDeviceSnapshot> controllers, StringBuilder output)
        {
            Guid hidGuid;
            NativeMethods.HidD_GetHidGuid(out hidGuid);
            var deviceInfoSet = NativeMethods.SetupDiGetClassDevs(
                ref hidGuid, IntPtr.Zero, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
            if (deviceInfoSet == new IntPtr(-1))
            {
                output.AppendLine("SetupAPI could not enumerate HID interfaces.");
                return;
            }

            var matched = 0;
            try
            {
                for (uint index = 0; ; index++)
                {
                    var interfaceData = new SpDeviceInterfaceData
                    {
                        Size = Marshal.SizeOf(typeof(SpDeviceInterfaceData))
                    };
                    if (!NativeMethods.SetupDiEnumDeviceInterfaces(
                        deviceInfoSet, IntPtr.Zero, ref hidGuid, index, ref interfaceData))
                    {
                        break;
                    }

                    uint requiredSize;
                    NativeMethods.SetupDiGetDeviceInterfaceDetail(
                        deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);
                    if (requiredSize == 0)
                    {
                        continue;
                    }

                    var detail = Marshal.AllocHGlobal((int)requiredSize);
                    try
                    {
                        Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                        if (!NativeMethods.SetupDiGetDeviceInterfaceDetail(
                            deviceInfoSet, ref interfaceData, detail, requiredSize, out requiredSize, IntPtr.Zero))
                        {
                            continue;
                        }

                        var pathOffset = IntPtr.Size == 8 ? 8 : 4;
                        var path = Marshal.PtrToStringUni(IntPtr.Add(detail, pathOffset));
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }

                        using (var handle = OpenHidDevice(path))
                        {
                            if (handle == null || handle.IsInvalid)
                            {
                                continue;
                            }

                            var attributes = new HiddAttributes { Size = Marshal.SizeOf(typeof(HiddAttributes)) };
                            if (!NativeMethods.HidD_GetAttributes(handle, ref attributes) ||
                                !controllers.Any(a => a.VendorId == attributes.VendorId && a.ProductId == attributes.ProductId))
                            {
                                continue;
                            }

                            matched++;
                            WriteInterface(output, handle, path, attributes);
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detail);
                    }
                }
            }
            finally
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            if (matched == 0)
            {
                output.AppendLine("No matching HID interface could be opened.");
            }
        }

        private static SafeFileHandle OpenHidDevice(string path)
        {
            var handle = NativeMethods.CreateFile(path, GenericRead | GenericWrite,
                FileShareRead | FileShareWrite, IntPtr.Zero, OpenExisting, FileFlagOverlapped, IntPtr.Zero);
            if (!handle.IsInvalid)
            {
                return handle;
            }

            handle.Dispose();
            handle = NativeMethods.CreateFile(path, GenericRead,
                FileShareRead | FileShareWrite, IntPtr.Zero, OpenExisting, FileFlagOverlapped, IntPtr.Zero);
            if (!handle.IsInvalid)
            {
                return handle;
            }

            handle.Dispose();
            return NativeMethods.CreateFile(path, 0,
                FileShareRead | FileShareWrite, IntPtr.Zero, OpenExisting, FileFlagOverlapped, IntPtr.Zero);
        }

        private static void WriteInterface(StringBuilder output, SafeFileHandle handle, string path, HiddAttributes attributes)
        {
            output.AppendLine();
            output.AppendLine(string.Format("VID={0:X4} PID={1:X4} Version={2:X4}",
                attributes.VendorId, attributes.ProductId, attributes.VersionNumber));
            output.AppendLine("Path: " + path);
            output.AppendLine("Manufacturer: " + GetHidString(handle, NativeMethods.HidD_GetManufacturerString));
            output.AppendLine("Product: " + GetHidString(handle, NativeMethods.HidD_GetProductString));
            output.AppendLine("Serial: " + GetHidString(handle, NativeMethods.HidD_GetSerialNumberString));

            IntPtr preparsedData;
            if (!NativeMethods.HidD_GetPreparsedData(handle, out preparsedData))
            {
                output.AppendLine("Capabilities: unavailable");
                return;
            }

            try
            {
                HidpCaps caps;
                if (NativeMethods.HidP_GetCaps(preparsedData, out caps) < 0)
                {
                    output.AppendLine("Capabilities: HidP_GetCaps failed");
                    return;
                }

                output.AppendLine(string.Format(
                    "UsagePage=0x{0:X4} Usage=0x{1:X4} InputBytes={2} OutputBytes={3} FeatureBytes={4}",
                    caps.UsagePage, caps.Usage, caps.InputReportByteLength,
                    caps.OutputReportByteLength, caps.FeatureReportByteLength));
                output.AppendLine(string.Format(
                    "InputValues={0} InputButtons={1} FeatureValues={2} FeatureButtons={3}",
                    caps.NumberInputValueCaps, caps.NumberInputButtonCaps,
                    caps.NumberFeatureValueCaps, caps.NumberFeatureButtonCaps));

                WriteCurrentReport(output, "Input report ID 0", caps.InputReportByteLength,
                    buffer => NativeMethods.HidD_GetInputReport(handle, buffer, buffer.Length));
                WriteCurrentReport(output, "Feature report ID 0", caps.FeatureReportByteLength,
                    buffer => NativeMethods.HidD_GetFeature(handle, buffer, buffer.Length));
                WriteStreamReport(output, handle, caps.InputReportByteLength);
            }
            finally
            {
                NativeMethods.HidD_FreePreparsedData(preparsedData);
            }
        }

        private static void WriteStreamReport(StringBuilder output, SafeFileHandle handle, int length)
        {
            if (length <= 0 || length > 4096)
            {
                output.AppendLine("Input stream: unavailable");
                return;
            }

            byte[] buffer;
            output.AppendLine(TryReadStreamReport(handle, length, 350, out buffer)
                ? "Input stream: " + BitConverter.ToString(buffer, 0, Math.Min(buffer.Length, 96))
                : "Input stream: no report captured in 350 ms");
        }

        private static bool TryReadStreamReport(SafeFileHandle handle, int length,
            int timeoutMilliseconds, out byte[] report)
        {
            report = null;
            var buffer = new byte[length];
            var eventHandle = NativeMethods.CreateEvent(IntPtr.Zero, true, false, null);
            if (eventHandle == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                var overlapped = new NativeOverlappedData { EventHandle = eventHandle };
                uint bytesRead;
                var completed = NativeMethods.ReadFile(handle, buffer, (uint)buffer.Length, out bytesRead, ref overlapped);
                if (!completed && Marshal.GetLastWin32Error() == ErrorIoPending)
                {
                    if (NativeMethods.WaitForSingleObject(eventHandle, (uint)timeoutMilliseconds) == WaitObject0)
                    {
                        completed = NativeMethods.GetOverlappedResult(handle, ref overlapped, out bytesRead, false);
                    }
                    else
                    {
                        NativeMethods.CancelIoEx(handle, ref overlapped);
                    }
                }

                if (!completed || bytesRead == 0)
                {
                    return false;
                }

                report = bytesRead == buffer.Length
                    ? buffer
                    : buffer.Take((int)bytesRead).ToArray();
                return true;
            }
            finally
            {
                NativeMethods.CloseHandle(eventHandle);
            }
        }

        private static void WriteCurrentReport(StringBuilder output, string label, int length, Func<byte[], bool> reader)
        {
            if (length <= 0 || length > 4096)
            {
                output.AppendLine(label + ": unavailable");
                return;
            }

            var buffer = new byte[length];
            buffer[0] = 0;
            if (!reader(buffer))
            {
                output.AppendLine(label + ": not returned");
                return;
            }

            output.AppendLine(label + ": " + BitConverter.ToString(buffer, 0, Math.Min(buffer.Length, 96)));
        }

        private delegate bool HidStringReader(SafeFileHandle handle, byte[] buffer, int length);

        private static string GetHidString(SafeFileHandle handle, HidStringReader reader)
        {
            var buffer = new byte[512];
            return reader(handle, buffer, buffer.Length)
                ? Encoding.Unicode.GetString(buffer).TrimEnd('\0')
                : "unavailable";
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SpDeviceInterfaceData
        {
            public int Size;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SpDevinfoData
        {
            public int Size;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HiddAttributes
        {
            public int Size;
            public ushort VendorId;
            public ushort ProductId;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HidpCaps
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeOverlappedData
        {
            public IntPtr Internal;
            public IntPtr InternalHigh;
            public uint Offset;
            public uint OffsetHigh;
            public IntPtr EventHandle;
        }

        private static class NativeMethods
        {
            [DllImport("hid.dll")]
            public static extern void HidD_GetHidGuid(out Guid hidGuid);

            [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator,
                IntPtr parent, uint flags);

            [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true,
                EntryPoint = "SetupDiGetClassDevsW")]
            public static extern IntPtr SetupDiGetClassDevsForEnumerator(IntPtr classGuid, string enumerator,
                IntPtr parent, uint flags);

            [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
            public static extern uint CM_Locate_DevNode(out uint deviceInstance, string deviceId, uint flags);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, uint memberIndex,
                ref SpDevinfoData deviceInfoData);

            [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SetupDiGetDeviceInstanceId(IntPtr deviceInfoSet,
                ref SpDevinfoData deviceInfoData, StringBuilder deviceInstanceId,
                int deviceInstanceIdSize, out uint requiredSize);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData,
                ref Guid interfaceClassGuid, uint memberIndex, ref SpDeviceInterfaceData deviceInterfaceData);

            [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet,
                ref SpDeviceInterfaceData deviceInterfaceData, IntPtr detailData, uint detailDataSize,
                out uint requiredSize, IntPtr deviceInfoData);

            [DllImport("setupapi.dll")]
            public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode,
                IntPtr securityAttributes, uint creationDisposition, uint flags, IntPtr templateFile);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr CreateEvent(IntPtr eventAttributes, bool manualReset,
                bool initialState, string name);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool ReadFile(SafeFileHandle file, byte[] buffer, uint bytesToRead,
                out uint bytesRead, ref NativeOverlappedData overlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetOverlappedResult(SafeFileHandle file,
                ref NativeOverlappedData overlapped, out uint bytesTransferred, bool wait);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CancelIoEx(SafeFileHandle file, ref NativeOverlappedData overlapped);

            [DllImport("kernel32.dll")]
            public static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

            [DllImport("kernel32.dll")]
            public static extern bool CloseHandle(IntPtr handle);

            [DllImport("hid.dll")]
            public static extern bool HidD_GetAttributes(SafeFileHandle device, ref HiddAttributes attributes);

            [DllImport("hid.dll")]
            public static extern bool HidD_GetManufacturerString(SafeFileHandle device, byte[] buffer, int length);

            [DllImport("hid.dll")]
            public static extern bool HidD_GetProductString(SafeFileHandle device, byte[] buffer, int length);

            [DllImport("hid.dll")]
            public static extern bool HidD_GetSerialNumberString(SafeFileHandle device, byte[] buffer, int length);

            [DllImport("hid.dll")]
            public static extern bool HidD_GetPreparsedData(SafeFileHandle device, out IntPtr preparsedData);

            [DllImport("hid.dll")]
            public static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

            [DllImport("hid.dll")]
            public static extern int HidP_GetCaps(IntPtr preparsedData, out HidpCaps capabilities);

            [DllImport("hid.dll")]
            public static extern bool HidD_GetInputReport(SafeFileHandle device, byte[] report, int length);

            [DllImport("hid.dll")]
            public static extern bool HidD_GetFeature(SafeFileHandle device, byte[] report, int length);
        }
    }
}
