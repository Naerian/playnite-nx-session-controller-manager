using System;

namespace ControllerSessionManager.Tester.Models
{
    public sealed class RestDriftTracker
    {
        public const int MaximumSamples = DiagnosticConfidenceEvaluator.HighConfidenceRestSamples;
        private const double StabilityDelta = 0.015d;
        private const double ObservationMilliseconds = 450d;
        private const double MaximumCandidateMagnitude = 0.35d;
        private DateTime? candidateStartedAt;
        private double lastLeftX;
        private double lastLeftY;
        private double lastRightX;
        private double lastRightY;

        public double MaxLeftDrift { get; private set; }
        public double MaxRightDrift { get; private set; }
        public int SampleCount { get; private set; }

        public double MaxDrift
        {
            get { return Math.Max(MaxLeftDrift, MaxRightDrift); }
        }

        public void AddSample(GamepadState state, DateTime timestamp)
        {
            if (SampleCount >= MaximumSamples)
            {
                return;
            }

            if (!IsRestCandidate(state))
            {
                ResetCandidate();
                return;
            }

            if (!candidateStartedAt.HasValue)
            {
                StartCandidate(state, timestamp);
                return;
            }

            if (HasMoved(state))
            {
                StartCandidate(state, timestamp);
                return;
            }

            StorePosition(state);
            if ((timestamp - candidateStartedAt.Value).TotalMilliseconds < ObservationMilliseconds)
            {
                return;
            }

            MaxLeftDrift = Math.Max(MaxLeftDrift, state.LeftStick.Magnitude);
            MaxRightDrift = Math.Max(MaxRightDrift, state.RightStick.Magnitude);
            SampleCount++;
        }

        public void Reset()
        {
            MaxLeftDrift = 0d;
            MaxRightDrift = 0d;
            SampleCount = 0;
            ResetCandidate();
        }

        private static bool IsRestCandidate(GamepadState state)
        {
            return state != null &&
                   !HasPressedButton(state.Buttons) &&
                   !HasPressedExtraButton(state) &&
                   state.LeftTrigger < 0.02f &&
                   state.RightTrigger < 0.02f &&
                   state.LeftStick.Magnitude < MaximumCandidateMagnitude &&
                   state.RightStick.Magnitude < MaximumCandidateMagnitude;
        }

        private bool HasMoved(GamepadState state)
        {
            return Math.Abs(state.LeftStick.X - lastLeftX) > StabilityDelta ||
                   Math.Abs(state.LeftStick.Y - lastLeftY) > StabilityDelta ||
                   Math.Abs(state.RightStick.X - lastRightX) > StabilityDelta ||
                   Math.Abs(state.RightStick.Y - lastRightY) > StabilityDelta;
        }

        private void StartCandidate(GamepadState state, DateTime timestamp)
        {
            candidateStartedAt = timestamp;
            StorePosition(state);
        }

        private void StorePosition(GamepadState state)
        {
            lastLeftX = state.LeftStick.X;
            lastLeftY = state.LeftStick.Y;
            lastRightX = state.RightStick.X;
            lastRightY = state.RightStick.Y;
        }

        private void ResetCandidate()
        {
            candidateStartedAt = null;
            lastLeftX = 0d;
            lastLeftY = 0d;
            lastRightX = 0d;
            lastRightY = 0d;
        }

        private static bool HasPressedButton(GamepadButtonState buttons)
        {
            return buttons != null &&
                   (buttons.South || buttons.East || buttons.West || buttons.North ||
                    buttons.LeftShoulder || buttons.RightShoulder || buttons.Back ||
                    buttons.Start || buttons.Guide || buttons.Touchpad ||
                    buttons.LeftStick || buttons.RightStick || buttons.DpadUp ||
                    buttons.DpadDown || buttons.DpadLeft || buttons.DpadRight);
        }

        private static bool HasPressedExtraButton(GamepadState state)
        {
            if (state.ExtraButtons == null)
            {
                return false;
            }

            for (var index = 0; index < state.ExtraButtons.Count; index++)
            {
                if (state.ExtraButtons[index].IsPressed)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
