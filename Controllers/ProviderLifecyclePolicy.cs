namespace ControllerSessionManager.Controllers
{
    internal static class ProviderLifecyclePolicy
    {
        public const int ConsecutiveMissesToDisconnect = 3;
        public const int ConsecutivePresentsToRestore = 3;

        public static bool ShouldRestorePlayniteRow(bool playniteConnected, bool capabilityConnected,
            bool fallbackOwnedDisconnect, int consecutiveCapabilityPresent)
        {
            if (playniteConnected || !capabilityConnected)
            {
                return false;
            }

            return fallbackOwnedDisconnect ||
                consecutiveCapabilityPresent >= ConsecutivePresentsToRestore;
        }

        public static bool ShouldHonorSdkDisconnect(bool capabilityStillConnected)
        {
            return ShouldHonorSdkDisconnect(capabilityStillConnected, true, false);
        }

        public static bool ShouldHonorSdkDisconnect(bool capabilityStillConnected,
            bool isXInputWrapperPath, bool anotherSameVendorPlayniteConnected)
        {
            if (!capabilityStillConnected)
            {
                return true;
            }

            // A second Playnite row of the same VID is a Wireless/Bluetooth mode switch.
            // Honor the old path so Mandos does not keep a ghost dongle next to Bluetooth.
            if (anotherSameVendorPlayniteConnected)
            {
                return true;
            }

            // Bluetooth HID disconnects are real even if a leftover XInput slot is still up.
            // Ignore only the dongle/cable XInput-wrapper handshake that Galva overlay needs.
            return !isXInputWrapperPath;
        }

        public static bool ShouldMarkDisconnected(int consecutiveMisses)
        {
            return consecutiveMisses >= ConsecutiveMissesToDisconnect;
        }
    }
}
