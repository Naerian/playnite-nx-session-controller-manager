using System;
using System.Collections.Generic;
using System.Linq;

namespace ControllerSessionManager.Controllers
{
    /// <summary>
    /// Chooses the live Windows transport. XInput (`HID...&IG_` / XUSB) is the Xbox-mode
    /// gameplay path for dongle and cable. Bluetooth HID/DInput nodes often stay enumerated
    /// after an 8BitDo 2.4 GHz switch; they are leftovers unless that row has newer input
    /// than the XInput slot, or Windows already identified a second physical pad.
    /// </summary>
    public static class ControllerTransportPolicy
    {
        public static bool IsXInputObservation(ControllerDeviceSnapshot controller)
        {
            if (controller == null || !controller.IsConnected)
            {
                return false;
            }

            if (string.Equals(controller.ProviderId, "XInput", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return ControllerBridgeIdentity.IsXInputWrapperPath(controller.Path) &&
                !WindowsBluetoothBatteryProvider.IsBluetoothPath(controller.Path);
        }

        public static bool IsBluetoothObservation(ControllerDeviceSnapshot controller)
        {
            if (controller == null)
            {
                return false;
            }

            if (string.Equals(controller.ConnectionType, "Bluetooth",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return WindowsBluetoothBatteryProvider.IsBluetoothPath(controller.Path);
        }

        public static bool IsHidLeftover(ControllerDeviceSnapshot controller)
        {
            return controller != null &&
                string.Equals(controller.ProviderId, "HID", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(controller.LifecycleProviderId,
                    ControllerSnapshotMerger.PlayniteProviderId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// XInput 1.4 is four slots with no VID/PID. Windows exposes Xbox-mode pads as
        /// <c>HID\VID_xxxx&amp;PID_yyyy&amp;IG_00</c> (XUSB). 8BitDo USB/2.4 GHz uses that
        /// path; Bluetooth is a separate DInput/BLE HID node. Xbox-licensed pads (VID 045E)
        /// are the exception: they speak XInput over Bluetooth on the same wrapper.
        /// Binding those two 8BitDo nodes together by name copies Bluetooth onto the dongle.
        /// </summary>
        public static bool CanShareCapability(ControllerDeviceSnapshot authoritative,
            ControllerDeviceSnapshot capability)
        {
            if (authoritative == null || capability == null)
            {
                return false;
            }

            if (IsXboxLicensed(authoritative) || IsXboxLicensed(capability))
            {
                return true;
            }

            var bluetooth = IsBluetoothObservation(authoritative);
            var otherBluetooth = IsBluetoothObservation(capability);
            var dongleOrCable = IsNonBluetoothXInput(authoritative);
            var otherDongleOrCable = IsNonBluetoothXInput(capability);
            return !((bluetooth && otherDongleOrCable) || (dongleOrCable && otherBluetooth));
        }

        public static bool IsNonBluetoothXInput(ControllerDeviceSnapshot controller)
        {
            if (controller == null || WindowsBluetoothBatteryProvider.IsBluetoothPath(controller.Path))
            {
                return false;
            }

            return string.Equals(controller.ProviderId, "XInput", StringComparison.OrdinalIgnoreCase) ||
                ControllerBridgeIdentity.IsXInputWrapperPath(controller.Path);
        }

        public static bool IsXboxLicensed(ControllerDeviceSnapshot controller)
        {
            return controller != null && controller.VendorId == 0x045E;
        }

        public static bool BluetoothIsSupersededByXInput(ControllerDeviceSnapshot bluetooth,
            ControllerDeviceSnapshot xinput)
        {
            if (bluetooth == null || xinput == null || !bluetooth.IsConnected || !xinput.IsConnected)
            {
                return false;
            }

            if (!IsBluetoothObservation(bluetooth) || !IsXInputObservation(xinput))
            {
                return false;
            }

            if (WindowsBluetoothBatteryProvider.IsBluetoothPath(xinput.Path))
            {
                return false;
            }

            if (bluetooth.VendorId == 0 || bluetooth.VendorId != xinput.VendorId)
            {
                return false;
            }

            if (bluetooth.LastInputUtc.HasValue && xinput.LastInputUtc.HasValue &&
                bluetooth.LastInputUtc > xinput.LastInputUtc)
            {
                return false;
            }

            return true;
        }

        public static bool ShouldCollapse(ControllerDeviceSnapshot left, ControllerDeviceSnapshot right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (HaveDistinctPhysicalIdentities(left, right))
            {
                return false;
            }

            if (IsHidLeftover(left) || IsHidLeftover(right))
            {
                return left.VendorId != 0 && left.VendorId == right.VendorId &&
                    (IsXInputObservation(left) || IsXInputObservation(right) ||
                     IsBluetoothObservation(left) || IsBluetoothObservation(right));
            }

            return BluetoothIsSupersededByXInput(left, right) ||
                BluetoothIsSupersededByXInput(right, left);
        }

        /// <summary>
        /// Two pads of the same model stay independent when they share a transport and
        /// Windows already gave them distinct instance, serial or XInput-slot identities.
        /// Mixed 2.4 GHz / Bluetooth nodes of one 8BitDo still look different on paper,
        /// so that leftover case is not decided here.
        /// </summary>
        internal static bool HaveDistinctPhysicalIdentities(ControllerDeviceSnapshot left,
            ControllerDeviceSnapshot right)
        {
            if (left == null || right == null || !ShareTransportFamily(left, right))
            {
                return false;
            }

            if (string.Equals(left.ProviderId, "XInput", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(right.ProviderId, "XInput", StringComparison.OrdinalIgnoreCase) &&
                left.ProviderInstanceId >= 0 && right.ProviderInstanceId >= 0 &&
                left.ProviderInstanceId != right.ProviderInstanceId)
            {
                return true;
            }

            if (ControllerBridgeIdentity.AreDistinctDeviceInstances(left.Path, right.Path))
            {
                return true;
            }

            return HasDistinctStableHardwareId(left.HardwareId, right.HardwareId);
        }

        private static bool ShareTransportFamily(ControllerDeviceSnapshot left,
            ControllerDeviceSnapshot right)
        {
            var leftBluetooth = IsBluetoothObservation(left);
            var rightBluetooth = IsBluetoothObservation(right);
            if (leftBluetooth && rightBluetooth)
            {
                return true;
            }

            var leftDongleOrCable = IsNonBluetoothXInput(left) || IsXInputObservation(left);
            var rightDongleOrCable = IsNonBluetoothXInput(right) || IsXInputObservation(right);
            return leftDongleOrCable && rightDongleOrCable && !leftBluetooth && !rightBluetooth;
        }

        private static bool HasDistinctStableHardwareId(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right) ||
                string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var leftSerial = left.IndexOf(":id-", StringComparison.OrdinalIgnoreCase) >= 0;
            var rightSerial = right.IndexOf(":id-", StringComparison.OrdinalIgnoreCase) >= 0;
            if (leftSerial && rightSerial)
            {
                return true;
            }

            ushort leftVendor;
            ushort leftProduct;
            ushort rightVendor;
            ushort rightProduct;
            int leftOrdinal;
            int rightOrdinal;
            return TryReadHardwareOrdinal(left, out leftVendor, out leftProduct, out leftOrdinal) &&
                TryReadHardwareOrdinal(right, out rightVendor, out rightProduct, out rightOrdinal) &&
                leftVendor == rightVendor && leftProduct == rightProduct &&
                leftOrdinal > 0 && rightOrdinal > 0 && leftOrdinal != rightOrdinal;
        }

        private static bool TryReadHardwareOrdinal(string hardwareId, out ushort vendorId,
            out ushort productId, out int ordinal)
        {
            vendorId = 0;
            productId = 0;
            ordinal = 0;
            if (string.IsNullOrWhiteSpace(hardwareId))
            {
                return false;
            }

            var parts = hardwareId.Split(':');
            int value;
            if (parts.Length < 4 ||
                !string.Equals(parts[0], "hardware", StringComparison.OrdinalIgnoreCase) ||
                !ushort.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out vendorId) ||
                !ushort.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out productId) ||
                !int.TryParse(parts[3], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out value) ||
                vendorId == 0 || productId == 0 || value <= 0)
            {
                return false;
            }

            ordinal = value;
            return true;
        }

        public static ControllerDeviceSnapshot SelectCanonical(
            IList<ControllerDeviceSnapshot> members)
        {
            var source = (members ?? new ControllerDeviceSnapshot[0])
                .Where(a => a != null && a.IsConnected).ToList();
            if (source.Count == 0)
            {
                return (members ?? new ControllerDeviceSnapshot[0]).FirstOrDefault();
            }

            return source.FirstOrDefault(IsXInputObservation)
                ?? source.FirstOrDefault(IsBluetoothObservation)
                ?? source[0];
        }
    }
}
