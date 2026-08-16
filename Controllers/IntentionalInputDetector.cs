using System;

namespace ControllerSessionManager.Controllers
{
    internal enum InputEvidenceKind
    {
        None,
        DigitalButton,
        Trigger,
        Stick,
        DirectionalPad,
        PlayniteButton
    }

    internal static class IntentionalInputDetector
    {
        internal static bool IsSdlGameplayButton(int canonicalButton)
        {
            const int guideButton = 5;
            return canonicalButton != guideButton;
        }

        internal static bool IsXInputIntentional(ushort previousButtons, ushort currentButtons,
            byte previousLeftTrigger, byte currentLeftTrigger, byte previousRightTrigger,
            byte currentRightTrigger, short previousLeftX, short currentLeftX, short previousLeftY,
            short currentLeftY, short previousRightX, short currentRightX, short previousRightY,
            short currentRightY)
        {
            return GetXInputEvidence(previousButtons, currentButtons, previousLeftTrigger,
                currentLeftTrigger, previousRightTrigger, currentRightTrigger, previousLeftX,
                currentLeftX, previousLeftY, currentLeftY, previousRightX, currentRightX,
                previousRightY, currentRightY) != InputEvidenceKind.None;
        }

        internal static InputEvidenceKind GetXInputEvidence(ushort previousButtons, ushort currentButtons,
            byte previousLeftTrigger, byte currentLeftTrigger, byte previousRightTrigger,
            byte currentRightTrigger, short previousLeftX, short currentLeftX, short previousLeftY,
            short currentLeftY, short previousRightX, short currentRightX, short previousRightY,
            short currentRightY)
        {
            const int triggerThreshold = 64;
            const int stickThreshold = 8000;
            if ((currentButtons & ~previousButtons) != 0)
            {
                return InputEvidenceKind.DigitalButton;
            }
            if ((previousLeftTrigger < triggerThreshold && currentLeftTrigger >= triggerThreshold) ||
                (previousRightTrigger < triggerThreshold && currentRightTrigger >= triggerThreshold))
            {
                return InputEvidenceKind.Trigger;
            }

            return CrossedOrMovedStick(previousLeftX, currentLeftX, stickThreshold) ||
                CrossedOrMovedStick(previousLeftY, currentLeftY, stickThreshold) ||
                CrossedOrMovedStick(previousRightX, currentRightX, stickThreshold) ||
                CrossedOrMovedStick(previousRightY, currentRightY, stickThreshold)
                ? InputEvidenceKind.Stick : InputEvidenceKind.None;
        }

        internal static bool IsSdlIntentional(ulong previousButtons, ulong currentButtons,
            short[] baselineAxes, short[] previousAxes, short[] currentAxes, int previousHatHash,
            int currentHatHash, bool currentHasHatDirection)
        {
            return GetSdlEvidence(previousButtons, currentButtons, baselineAxes, previousAxes,
                currentAxes, previousHatHash, currentHatHash, currentHasHatDirection) !=
                InputEvidenceKind.None;
        }

        internal static InputEvidenceKind GetSdlEvidence(ulong previousButtons, ulong currentButtons,
            short[] baselineAxes, short[] previousAxes, short[] currentAxes, int previousHatHash,
            int currentHatHash, bool currentHasHatDirection)
        {
            if ((currentButtons & ~previousButtons) != 0)
            {
                return InputEvidenceKind.DigitalButton;
            }
            if (currentHasHatDirection && currentHatHash != previousHatHash)
            {
                return InputEvidenceKind.DirectionalPad;
            }

            var axisCount = Math.Min(baselineAxes == null ? 0 : baselineAxes.Length,
                Math.Min(previousAxes == null ? 0 : previousAxes.Length,
                    currentAxes == null ? 0 : currentAxes.Length));
            for (var axis = 0; axis < axisCount; axis++)
            {
                var previousOffset = (int)previousAxes[axis] - baselineAxes[axis];
                var currentOffset = (int)currentAxes[axis] - baselineAxes[axis];
                var previousActive = Math.Abs(previousOffset) >= 8000;
                var currentActive = Math.Abs(currentOffset) >= 8000;
                if (currentActive && (!previousActive ||
                    Math.Abs((int)currentAxes[axis] - previousAxes[axis]) >= 6000))
                {
                    return InputEvidenceKind.Stick;
                }
            }

            return InputEvidenceKind.None;
        }

        internal static bool IsXInputNeutral(ushort buttons, byte leftTrigger, byte rightTrigger,
            short leftX, short leftY, short rightX, short rightY)
        {
            return buttons == 0 && leftTrigger < 32 && rightTrigger < 32 &&
                Math.Abs((int)leftX) < 8000 && Math.Abs((int)leftY) < 8000 &&
                Math.Abs((int)rightX) < 8000 && Math.Abs((int)rightY) < 8000;
        }

        internal static bool IsSdlNeutral(ulong buttons, short[] baselineAxes, short[] currentAxes,
            bool hasHatDirection)
        {
            if (buttons != 0 || hasHatDirection)
            {
                return false;
            }

            var axisCount = Math.Min(baselineAxes == null ? 0 : baselineAxes.Length,
                currentAxes == null ? 0 : currentAxes.Length);
            for (var axis = 0; axis < axisCount; axis++)
            {
                if (Math.Abs((int)currentAxes[axis] - baselineAxes[axis]) >= 6000)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CrossedOrMovedStick(short previous, short current, int threshold)
        {
            var previousActive = Math.Abs((int)previous) >= threshold;
            var currentActive = Math.Abs((int)current) >= threshold;
            return currentActive && (!previousActive || Math.Abs((int)current - previous) >= 6000);
        }
    }
}
