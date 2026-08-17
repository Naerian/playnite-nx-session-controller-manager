using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ControllerSessionManager.Controllers
{
    public sealed class WindowsBluetoothBatteryProvider : IControllerBatteryProvider
    {
        private const uint DigcfPresent = 0x00000002;
        private const uint DigcfAllClasses = 0x00000004;
        private const uint DigcfDeviceInterface = 0x00000010;
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);
        private static DevPropKey ContainerIdProperty = new DevPropKey(
            new Guid("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C"), 2);
        private static DevPropKey BluetoothBatteryLifeProperty = new DevPropKey(
            new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"), 2);
        private static DevPropKey DeviceBatteryLifeProperty = new DevPropKey(
            new Guid("49CD1F76-5626-4B17-A4E8-18B4AA1A2213"), 10);
        private readonly Dictionary<string, CachedReading> cache =
            new Dictionary<string, CachedReading>(StringComparer.OrdinalIgnoreCase);

        public string Id
        {
            get { return "Windows.BluetoothPnP"; }
        }

        public bool Supports(ControllerMetadata controller)
        {
            return controller != null &&
                (string.Equals(controller.ConnectionType, "Bluetooth", StringComparison.OrdinalIgnoreCase) ||
                 IsBluetoothPath(controller.DevicePath));
        }

        public bool TryGetBatteryLevel(ControllerMetadata controller, out string level)
        {
            level = null;
            if (!Supports(controller))
            {
                return false;
            }

            var key = string.Format("{0:X4}:{1:X4}:{2}", controller.VendorId,
                controller.ProductId, NormalizePath(controller.DevicePath));
            CachedReading cached;
            if (cache.TryGetValue(key, out cached) &&
                DateTime.UtcNow - cached.TimestampUtc < TimeSpan.FromSeconds(cached.Success ? 10 : 3))
            {
                level = cached.Level;
                return cached.Success;
            }

            Guid containerId;
            var percent = 0;
            var success = (TryGetContainerFromHidPath(controller.DevicePath, out containerId) ||
                TryGetUniqueBluetoothContainer(controller, out containerId)) &&
                TryReadBatteryPercent(containerId, out percent);
            if (success)
            {
                level = ToLevel(percent);
            }
            cache[key] = new CachedReading
            {
                TimestampUtc = DateTime.UtcNow,
                Success = success,
                Level = level
            };
            return success;
        }

        internal static string ToLevel(int percent)
        {
            if (percent <= 10) return "Empty";
            if (percent <= 30) return "Low";
            if (percent <= 70) return "Medium";
            return "Full";
        }

        internal static bool IsBluetoothPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            return path.IndexOf("BTHENUM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("BTHLE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("BLUETOOTH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("00001812-0000-1000-8000-00805F9B34FB",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryGetContainerFromHidPath(string devicePath, out Guid containerId)
        {
            containerId = Guid.Empty;
            if (string.IsNullOrWhiteSpace(devicePath))
            {
                return false;
            }

            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);
            var infoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero,
                DigcfPresent | DigcfDeviceInterface);
            if (infoSet == InvalidHandleValue)
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
                    if (!SetupDiEnumDeviceInterfaces(infoSet, IntPtr.Zero, ref hidGuid, index,
                        ref interfaceData))
                    {
                        break;
                    }

                    uint requiredSize;
                    SetupDiGetDeviceInterfaceDetail(infoSet, ref interfaceData, IntPtr.Zero, 0,
                        out requiredSize, IntPtr.Zero);
                    if (requiredSize == 0)
                    {
                        continue;
                    }

                    var detail = Marshal.AllocHGlobal((int)requiredSize);
                    try
                    {
                        Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                        var deviceInfo = new SpDevInfoData
                        {
                            Size = (uint)Marshal.SizeOf(typeof(SpDevInfoData))
                        };
                        if (!SetupDiGetDeviceInterfaceDetailWithDeviceInfo(infoSet, ref interfaceData,
                            detail, requiredSize, out requiredSize, ref deviceInfo))
                        {
                            continue;
                        }

                        var path = Marshal.PtrToStringUni(IntPtr.Add(detail, IntPtr.Size == 8 ? 8 : 4));
                        if (PathsReferToSameInterface(devicePath, path) &&
                            TryGetGuidProperty(infoSet, ref deviceInfo, ref ContainerIdProperty,
                                out containerId))
                        {
                            return true;
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
                SetupDiDestroyDeviceInfoList(infoSet);
            }
            return false;
        }

        private static bool TryGetUniqueBluetoothContainer(ControllerMetadata controller,
            out Guid containerId)
        {
            containerId = Guid.Empty;
            var matches = new HashSet<Guid>();
            var infoSet = SetupDiGetClassDevsAll(IntPtr.Zero, null, IntPtr.Zero,
                DigcfPresent | DigcfAllClasses);
            if (infoSet == InvalidHandleValue)
            {
                return false;
            }
            try
            {
                for (uint index = 0; ; index++)
                {
                    var deviceInfo = new SpDevInfoData
                    {
                        Size = (uint)Marshal.SizeOf(typeof(SpDevInfoData))
                    };
                    if (!SetupDiEnumDeviceInfo(infoSet, index, ref deviceInfo))
                    {
                        break;
                    }
                    var instanceId = GetDeviceInstanceId(infoSet, ref deviceInfo);
                    Guid candidate;
                    if (IsMatchingBluetoothInstance(instanceId, controller.VendorId,
                        controller.ProductId) && TryGetGuidProperty(infoSet, ref deviceInfo,
                            ref ContainerIdProperty, out candidate))
                    {
                        matches.Add(candidate);
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(infoSet);
            }

            if (matches.Count != 1)
            {
                return false;
            }
            containerId = matches.First();
            return true;
        }

        private static bool TryReadBatteryPercent(Guid containerId, out int percent)
        {
            percent = 0;
            var infoSet = SetupDiGetClassDevsAll(IntPtr.Zero, null, IntPtr.Zero,
                DigcfPresent | DigcfAllClasses);
            if (infoSet == InvalidHandleValue)
            {
                return false;
            }
            try
            {
                for (uint index = 0; ; index++)
                {
                    var deviceInfo = new SpDevInfoData
                    {
                        Size = (uint)Marshal.SizeOf(typeof(SpDevInfoData))
                    };
                    if (!SetupDiEnumDeviceInfo(infoSet, index, ref deviceInfo))
                    {
                        break;
                    }
                    Guid candidate;
                    if (!TryGetGuidProperty(infoSet, ref deviceInfo, ref ContainerIdProperty,
                        out candidate) || candidate != containerId)
                    {
                        continue;
                    }
                    if ((TryGetByteProperty(infoSet, ref deviceInfo,
                            ref BluetoothBatteryLifeProperty, out percent) ||
                         TryGetByteProperty(infoSet, ref deviceInfo,
                            ref DeviceBatteryLifeProperty, out percent)) && percent <= 100)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(infoSet);
            }
            return false;
        }

        private static bool IsMatchingBluetoothInstance(string instanceId, ushort vendorId,
            ushort productId)
        {
            if (string.IsNullOrWhiteSpace(instanceId) ||
                (instanceId.IndexOf("BTH", StringComparison.OrdinalIgnoreCase) < 0 &&
                 instanceId.IndexOf("00001812-0000-1000-8000-00805F9B34FB",
                    StringComparison.OrdinalIgnoreCase) < 0))
            {
                return false;
            }
            var vendor = vendorId.ToString("X4");
            var product = productId.ToString("X4");
            return (instanceId.IndexOf("VID_" + vendor, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    instanceId.IndexOf("VID&12" + vendor, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (instanceId.IndexOf("PID_" + product, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 instanceId.IndexOf("PID&" + product, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool PathsReferToSameInterface(string left, string right)
        {
            var first = NormalizePath(left);
            var second = NormalizePath(right);
            return first == second || first.StartsWith(second + "\\", StringComparison.Ordinal) ||
                second.StartsWith(first + "\\", StringComparison.Ordinal);
        }

        private static string NormalizePath(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty :
                value.Trim().Replace('/', '\\').ToUpperInvariant();
        }

        private static bool TryGetGuidProperty(IntPtr infoSet, ref SpDevInfoData deviceInfo,
            ref DevPropKey key, out Guid value)
        {
            value = Guid.Empty;
            var buffer = new byte[16];
            uint requiredSize;
            uint propertyType;
            if (!SetupDiGetDeviceProperty(infoSet, ref deviceInfo, ref key, out propertyType,
                buffer, (uint)buffer.Length, out requiredSize, 0) || requiredSize < 16)
            {
                return false;
            }
            value = new Guid(buffer);
            return value != Guid.Empty;
        }

        private static bool TryGetByteProperty(IntPtr infoSet, ref SpDevInfoData deviceInfo,
            ref DevPropKey key, out int value)
        {
            value = 0;
            var buffer = new byte[1];
            uint requiredSize;
            uint propertyType;
            if (!SetupDiGetDeviceProperty(infoSet, ref deviceInfo, ref key, out propertyType,
                buffer, 1, out requiredSize, 0) || requiredSize < 1)
            {
                return false;
            }
            value = buffer[0];
            return true;
        }

        private static string GetDeviceInstanceId(IntPtr infoSet, ref SpDevInfoData deviceInfo)
        {
            var buffer = new StringBuilder(512);
            uint requiredSize;
            return SetupDiGetDeviceInstanceId(infoSet, ref deviceInfo, buffer,
                (uint)buffer.Capacity, out requiredSize) ? buffer.ToString() : null;
        }

        private sealed class CachedReading
        {
            public DateTime TimestampUtc { get; set; }
            public bool Success { get; set; }
            public string Level { get; set; }
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
        private struct SpDevInfoData
        {
            public uint Size;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DevPropKey
        {
            public DevPropKey(Guid formatId, uint propertyId)
            {
                FormatId = formatId;
                PropertyId = propertyId;
            }
            public Guid FormatId;
            public uint PropertyId;
        }

        [DllImport("hid.dll")]
        private static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator,
            IntPtr parentWindow, uint flags);

        [DllImport("setupapi.dll", EntryPoint = "SetupDiGetClassDevsW", CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevsAll(IntPtr classGuid, string enumerator,
            IntPtr parentWindow, uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr infoSet, IntPtr deviceInfo,
            ref Guid interfaceClassGuid, uint memberIndex, ref SpDeviceInterfaceData interfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr infoSet,
            ref SpDeviceInterfaceData interfaceData, IntPtr detailData, uint detailSize,
            out uint requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceInterfaceDetailW",
            CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceInterfaceDetailWithDeviceInfo(IntPtr infoSet,
            ref SpDeviceInterfaceData interfaceData, IntPtr detailData, uint detailSize,
            out uint requiredSize, ref SpDevInfoData deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiEnumDeviceInfo(IntPtr infoSet, uint memberIndex,
            ref SpDevInfoData deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceProperty(IntPtr infoSet,
            ref SpDevInfoData deviceInfoData, ref DevPropKey propertyKey, out uint propertyType,
            [Out] byte[] propertyBuffer, uint propertyBufferSize, out uint requiredSize, uint flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceInstanceId(IntPtr infoSet,
            ref SpDevInfoData deviceInfoData, StringBuilder instanceId, uint instanceIdSize,
            out uint requiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr infoSet);
    }
}
