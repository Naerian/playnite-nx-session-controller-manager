using System;
using System.Collections.Generic;
using System.Linq;
using ControllerSessionManager.Controllers;
using ControllerSessionManager.Tester.Models;

namespace ControllerSessionManager.Tester.Services
{
    /// <summary>
    /// Copies Mandos aliases onto Tester rows so the dropdown shows the same names.
    /// </summary>
    public static class TesterControllerAliases
    {
        public static IReadOnlyList<GamepadControllerInfo> Apply(
            IReadOnlyList<GamepadControllerInfo> controllers,
            IReadOnlyList<ControllerDeviceSnapshot> pads)
        {
            var result = (controllers ?? Enumerable.Empty<GamepadControllerInfo>())
                .Where(a => a != null)
                .Select(Clone)
                .ToList();
            var remaining = (pads ?? Enumerable.Empty<ControllerDeviceSnapshot>())
                .Where(a => a != null && a.IsConnected)
                .ToList();

            foreach (var controller in result)
            {
                var match = FindMatch(controller, remaining);
                if (match == null)
                {
                    continue;
                }

                remaining.Remove(match);
                if (!string.IsNullOrWhiteSpace(match.Name))
                {
                    controller.CustomName = match.Name.Trim();
                }
            }

            return result;
        }

        internal static ControllerDeviceSnapshot FindMatch(GamepadControllerInfo controller,
            IList<ControllerDeviceSnapshot> remaining)
        {
            if (controller == null || remaining == null || remaining.Count == 0)
            {
                return null;
            }

            var pathMatches = remaining.Where(a => IsPathMatch(controller, a)).ToList();
            if (pathMatches.Count == 1)
            {
                return pathMatches[0];
            }

            var hardwareMatches = remaining.Where(a => IsHardwareMatch(controller, a)).ToList();
            if (hardwareMatches.Count == 1)
            {
                return hardwareMatches[0];
            }

            if (controller.PlayerIndex >= 0 && controller.PlayerIndex <= 3)
            {
                var slotMatches = hardwareMatches.Where(a =>
                    string.Equals(a.ProviderId, "XInput", StringComparison.OrdinalIgnoreCase) &&
                    a.ProviderInstanceId == controller.PlayerIndex).ToList();
                if (slotMatches.Count == 0)
                {
                    slotMatches = remaining.Where(a =>
                        string.Equals(a.ProviderId, "XInput", StringComparison.OrdinalIgnoreCase) &&
                        a.ProviderInstanceId == controller.PlayerIndex &&
                        (controller.VendorId == 0 || a.VendorId == 0 ||
                         a.VendorId == controller.VendorId)).ToList();
                }

                if (slotMatches.Count == 1)
                {
                    return slotMatches[0];
                }
            }

            if (hardwareMatches.Count > 0)
            {
                return hardwareMatches[0];
            }

            if (controller.VendorId != 0)
            {
                var vendorMatches = remaining.Where(a => a.VendorId == controller.VendorId).ToList();
                if (vendorMatches.Count == 1)
                {
                    return vendorMatches[0];
                }
            }

            return null;
        }

        private static bool IsPathMatch(GamepadControllerInfo controller, ControllerDeviceSnapshot pad)
        {
            if (controller == null || pad == null ||
                string.IsNullOrWhiteSpace(controller.Path) ||
                string.IsNullOrWhiteSpace(pad.Path))
            {
                return false;
            }

            return ControllerBridgeIdentity.PathsReferToSameDevice(controller.Path, pad.Path) ||
                !ControllerBridgeIdentity.AreDistinctDeviceInstances(controller.Path, pad.Path);
        }

        private static bool IsHardwareMatch(GamepadControllerInfo controller, ControllerDeviceSnapshot pad)
        {
            return controller != null && pad != null &&
                controller.VendorId != 0 && pad.VendorId == controller.VendorId &&
                pad.ProductId == controller.ProductId;
        }

        private static GamepadControllerInfo Clone(GamepadControllerInfo source)
        {
            return new GamepadControllerInfo
            {
                JoystickIndex = source.JoystickIndex,
                InstanceId = source.InstanceId,
                PlayerIndex = source.PlayerIndex,
                Name = source.Name,
                CustomName = source.CustomName,
                VendorId = source.VendorId,
                ProductId = source.ProductId,
                Path = source.Path,
                Layout = source.Layout,
                EightBitDoModel = source.EightBitDoModel
            };
        }
    }
}
