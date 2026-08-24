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
        private static DevPropKey BluetoothDeviceAddressProperty = new DevPropKey(
            new Guid("2BD67D8B-8BEB-48D5-87E0-6CDA3428040A"), 1);
        private const string BluetoothBaseUuidTail = "00805F9B34FB";
        private readonly Dictionary<string, CachedReading> cache =
            new Dictionary<string, CachedReading>(StringComparer.OrdinalIgnoreCase);

        public string Id
        {
            get { return "Windows.BluetoothPnP"; }
        }

        public bool Supports(ControllerMetadata controller)
        {
            if (controller == null || IsXInputWrapperPath(controller.DevicePath) ||
                string.Equals(controller.ConnectionType, "Wired", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var bluetoothEvidence = IsBluetoothPath(controller.DevicePath) ||
                ControllerDeviceIdentity.HasBluetoothPresence(controller.VendorId,
                    controller.ProductId) ||
                string.Equals(controller.ConnectionType, "Bluetooth",
                    StringComparison.OrdinalIgnoreCase);

            // Dongle / receiver rows are often labelled Wireless. Skip them unless Windows
            // already proves a Bluetooth path or BLE presence (e.g. product name "Wireless"
            // on a real Bluetooth HID, which must still read BTHLE battery).
            if (!bluetoothEvidence &&
                (string.Equals(controller.ConnectionType, "Wireless",
                    StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(controller.ConnectionType, "WirelessReceiver",
                    StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return bluetoothEvidence;
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

            var percent = 0;
            var success = TryReadBatteryForController(controller, out percent);
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

        internal void ClearCache()
        {
            cache.Clear();
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
                path.IndexOf("00001124-0000-1000-8000-00805F9B34FB",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("00001812-0000-1000-8000-00805F9B34FB",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsXInputWrapperPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                (path.IndexOf("&ig_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 path.IndexOf("xusb", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        internal static bool TryExtractBluetoothAddress(string value, out string address)
        {
            address = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Trim().ToUpperInvariant();
            var searchFrom = 0;
            while (true)
            {
                var marker = normalized.IndexOf("DEV_", searchFrom, StringComparison.Ordinal);
                if (marker < 0)
                {
                    break;
                }

                string candidate;
                if (TryReadExactly12Hex(normalized, marker + 4, out candidate) &&
                    candidate != BluetoothBaseUuidTail)
                {
                    address = candidate;
                    return true;
                }

                searchFrom = marker + 4;
            }

            string colonAddress;
            if (TryExtractColonAddress(normalized, out colonAddress))
            {
                address = colonAddress;
                return true;
            }

            var tokens = new List<string>();
            var current = new StringBuilder();
            for (var index = 0; index <= normalized.Length; index++)
            {
                var character = index < normalized.Length ? normalized[index] : '\0';
                if (index < normalized.Length && IsHexDigit(character))
                {
                    current.Append(character);
                    continue;
                }

                if (current.Length == 12)
                {
                    var token = current.ToString();
                    if (token != BluetoothBaseUuidTail)
                    {
                        tokens.Add(token);
                    }
                }

                current.Length = 0;
            }

            if (tokens.Count == 0)
            {
                return false;
            }

            address = tokens[tokens.Count - 1];
            return true;
        }

        private bool TryReadBatteryForController(ControllerMetadata controller, out int percent)
        {
            percent = 0;
            if (controller == null || IsXInputWrapperPath(controller.DevicePath))
            {
                return false;
            }

            var records = EnumeratePresentDevices();
            Guid containerId;
            if (!IsXInputWrapperPath(controller.DevicePath) &&
                TryGetContainerFromHidPath(controller.DevicePath, out containerId) &&
                TryReadBatteryPercentFromRecords(records, containerId, out percent))
            {
                return true;
            }

            string address;
            if (TryExtractBluetoothAddress(controller.DevicePath, out address) &&
                TryReadBatteryPercentForAddress(records, address, out percent))
            {
                return true;
            }

            return TryReadBatteryPercentForVendor(records, controller.VendorId,
                controller.ProductId, out percent);
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

        private static bool TryReadBatteryPercentFromRecords(IList<PresentDeviceRecord> records,
            Guid containerId, out int percent)
        {
            percent = 0;
            if (records == null || containerId == Guid.Empty)
            {
                return false;
            }

            var values = new List<int>();
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                if (record.ContainerId == containerId && record.BatteryPercent.HasValue)
                {
                    values.Add(record.BatteryPercent.Value);
                }
            }

            return TryChooseUniquePercent(values, out percent);
        }

        private static bool TryReadBatteryPercentForAddress(IList<PresentDeviceRecord> records,
            string address, out int percent)
        {
            percent = 0;
            if (records == null || string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            var values = new List<int>();
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                if (string.Equals(record.BluetoothAddress, address, StringComparison.OrdinalIgnoreCase) &&
                    record.BatteryPercent.HasValue)
                {
                    values.Add(record.BatteryPercent.Value);
                }
            }

            return TryChooseUniquePercent(values, out percent);
        }

        private static bool TryReadBatteryPercentForVendor(IList<PresentDeviceRecord> records,
            ushort vendorId, ushort productId, out int percent)
        {
            percent = 0;
            if (records == null || vendorId == 0)
            {
                return false;
            }

            if (TryReadBatteryPercentForVendorGroups(records.GroupBy(a => a.ContainerId),
                vendorId, productId, true, out percent))
            {
                return true;
            }

            return TryReadBatteryPercentForVendorGroups(
                records.Where(a => !string.IsNullOrWhiteSpace(a.BluetoothAddress))
                    .GroupBy(a => a.BluetoothAddress, StringComparer.OrdinalIgnoreCase),
                vendorId, productId, false, out percent);
        }

        private static bool TryReadBatteryPercentForVendorGroups(
            IEnumerable<IGrouping<string, PresentDeviceRecord>> groups,
            ushort vendorId, ushort productId, bool skipEmptyKey, out int percent)
        {
            percent = 0;
            if (groups == null)
            {
                return false;
            }

            var vendorPercents = new List<int>();
            var productPercents = new List<int>();
            foreach (var group in groups)
            {
                if (skipEmptyKey && string.IsNullOrWhiteSpace(group.Key))
                {
                    continue;
                }

                var battery = group.Select(a => a.BatteryPercent).FirstOrDefault(a => a.HasValue);
                if (!battery.HasValue)
                {
                    continue;
                }

                var vendorMatch = false;
                var productMatch = false;
                foreach (var record in group)
                {
                    if (HidDiagnosticsService.HardwareIdContainsVid(record.InstanceId, vendorId))
                    {
                        vendorMatch = true;
                        if (productId != 0 &&
                            HidDiagnosticsService.HardwareIdContainsPid(record.InstanceId, productId))
                        {
                            productMatch = true;
                        }
                    }
                }

                if (!vendorMatch)
                {
                    continue;
                }

                vendorPercents.Add(battery.Value);
                if (productMatch)
                {
                    productPercents.Add(battery.Value);
                }
            }

            var chosen = productPercents.Count > 0 ? productPercents : vendorPercents;
            return TryChooseUniquePercent(chosen, out percent);
        }

        private static bool TryReadBatteryPercentForVendorGroups(
            IEnumerable<IGrouping<Guid, PresentDeviceRecord>> groups,
            ushort vendorId, ushort productId, bool skipEmptyKey, out int percent)
        {
            percent = 0;
            if (groups == null)
            {
                return false;
            }

            var vendorPercents = new List<int>();
            var productPercents = new List<int>();
            foreach (var group in groups)
            {
                if (skipEmptyKey && group.Key == Guid.Empty)
                {
                    continue;
                }

                var battery = group.Select(a => a.BatteryPercent).FirstOrDefault(a => a.HasValue);
                if (!battery.HasValue)
                {
                    continue;
                }

                var vendorMatch = false;
                var productMatch = false;
                foreach (var record in group)
                {
                    if (HidDiagnosticsService.HardwareIdContainsVid(record.InstanceId, vendorId))
                    {
                        vendorMatch = true;
                        if (productId != 0 &&
                            HidDiagnosticsService.HardwareIdContainsPid(record.InstanceId, productId))
                        {
                            productMatch = true;
                        }
                    }
                }

                if (!vendorMatch)
                {
                    continue;
                }

                vendorPercents.Add(battery.Value);
                if (productMatch)
                {
                    productPercents.Add(battery.Value);
                }
            }

            var chosen = productPercents.Count > 0 ? productPercents : vendorPercents;
            return TryChooseUniquePercent(chosen, out percent);
        }

        private static bool TryChooseUniquePercent(IList<int> values, out int percent)
        {
            percent = 0;
            if (values == null || values.Count == 0)
            {
                return false;
            }

            percent = values[0];
            for (var index = 1; index < values.Count; index++)
            {
                if (values[index] != percent)
                {
                    return false;
                }
            }

            return percent <= 100;
        }

        private static List<PresentDeviceRecord> EnumeratePresentDevices()
        {
            var records = new List<PresentDeviceRecord>();
            var infoSet = SetupDiGetClassDevsAll(IntPtr.Zero, null, IntPtr.Zero,
                DigcfPresent | DigcfAllClasses);
            if (infoSet == InvalidHandleValue)
            {
                return records;
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

                    Guid containerId;
                    TryGetGuidProperty(infoSet, ref deviceInfo, ref ContainerIdProperty,
                        out containerId);
                    int batteryPercent;
                    int? battery = null;
                    if (TryGetPercentProperty(infoSet, ref deviceInfo,
                            ref BluetoothBatteryLifeProperty, out batteryPercent) ||
                        TryGetPercentProperty(infoSet, ref deviceInfo,
                            ref DeviceBatteryLifeProperty, out batteryPercent))
                    {
                        battery = batteryPercent;
                    }

                    var instanceId = GetDeviceInstanceId(infoSet, ref deviceInfo);
                    string address;
                    if (!TryExtractBluetoothAddress(instanceId, out address))
                    {
                        TryGetBluetoothAddressProperty(infoSet, ref deviceInfo, out address);
                    }

                    records.Add(new PresentDeviceRecord
                    {
                        ContainerId = containerId,
                        InstanceId = instanceId,
                        BluetoothAddress = address,
                        BatteryPercent = battery
                    });
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(infoSet);
            }

            return records;
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

        private static bool TryReadExactly12Hex(string value, int start, out string address)
        {
            address = null;
            if (string.IsNullOrEmpty(value) || start < 0 || start + 12 > value.Length)
            {
                return false;
            }

            for (var index = 0; index < 12; index++)
            {
                if (!IsHexDigit(value[start + index]))
                {
                    return false;
                }
            }

            if (start + 12 < value.Length && IsHexDigit(value[start + 12]))
            {
                return false;
            }

            address = value.Substring(start, 12);
            return true;
        }

        private static bool TryExtractColonAddress(string value, out string address)
        {
            address = null;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var index = 0; index + 17 <= value.Length; index++)
            {
                var match = true;
                var digits = new StringBuilder(12);
                for (var offset = 0; offset < 17; offset++)
                {
                    var character = value[index + offset];
                    if (offset % 3 == 2)
                    {
                        if (character != ':')
                        {
                            match = false;
                            break;
                        }
                    }
                    else if (IsHexDigit(character))
                    {
                        digits.Append(character);
                    }
                    else
                    {
                        match = false;
                        break;
                    }
                }

                if (match && digits.Length == 12)
                {
                    address = digits.ToString();
                    return true;
                }
            }

            return false;
        }

        private static bool IsHexDigit(char value)
        {
            return (value >= '0' && value <= '9') ||
                (value >= 'A' && value <= 'F') ||
                (value >= 'a' && value <= 'f');
        }

        private static bool TryGetBluetoothAddressProperty(IntPtr infoSet, ref SpDevInfoData deviceInfo,
            out string address)
        {
            address = null;
            var buffer = new byte[32];
            uint requiredSize;
            uint propertyType;
            if (!SetupDiGetDeviceProperty(infoSet, ref deviceInfo, ref BluetoothDeviceAddressProperty,
                out propertyType, buffer, (uint)buffer.Length, out requiredSize, 0) ||
                requiredSize == 0)
            {
                return false;
            }

            if (requiredSize >= 6 && requiredSize <= 8)
            {
                address = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}",
                    buffer[0], buffer[1], buffer[2], buffer[3], buffer[4], buffer[5]);
                return address != BluetoothBaseUuidTail;
            }

            if (requiredSize >= 12)
            {
                var text = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize).Trim('\0');
                return TryExtractBluetoothAddress(text, out address);
            }

            return false;
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

        private static bool TryGetPercentProperty(IntPtr infoSet, ref SpDevInfoData deviceInfo,
            ref DevPropKey key, out int value)
        {
            value = 0;
            var buffer = new byte[8];
            uint requiredSize;
            uint propertyType;
            if (!SetupDiGetDeviceProperty(infoSet, ref deviceInfo, ref key, out propertyType,
                buffer, (uint)buffer.Length, out requiredSize, 0) || requiredSize < 1)
            {
                return false;
            }

            if (requiredSize == 1)
            {
                value = buffer[0];
                return true;
            }

            if (requiredSize >= 4)
            {
                value = BitConverter.ToInt32(buffer, 0);
                return value >= 0 && value <= 100;
            }

            return false;
        }

        private static string GetDeviceInstanceId(IntPtr infoSet, ref SpDevInfoData deviceInfo)
        {
            var buffer = new StringBuilder(512);
            uint requiredSize;
            return SetupDiGetDeviceInstanceId(infoSet, ref deviceInfo, buffer,
                (uint)buffer.Capacity, out requiredSize) ? buffer.ToString() : null;
        }

        private sealed class PresentDeviceRecord
        {
            public Guid ContainerId { get; set; }
            public string InstanceId { get; set; }
            public string BluetoothAddress { get; set; }
            public int? BatteryPercent { get; set; }
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
