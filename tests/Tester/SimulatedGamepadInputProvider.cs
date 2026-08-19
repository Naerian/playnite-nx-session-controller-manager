using ControllerSessionManager.Tester.Models;
using ControllerSessionManager.Tester.Services;
using System.Collections.Generic;

namespace ControllerSessionManager.Tester.Tests
{
    internal sealed class SimulatedGamepadInputProvider : IGamepadInputProvider
    {
        private readonly Queue<GamepadState> states = new Queue<GamepadState>();
        private readonly List<GamepadControllerInfo> controllers = new List<GamepadControllerInfo>();

        public int SelectedInstanceId { get; private set; }
        public int RumbleCallCount { get; private set; }
        public GamepadState CurrentState { get; set; }

        public SimulatedGamepadInputProvider()
        {
            CurrentState = new GamepadState();
        }

        public void AddController(GamepadControllerInfo controller)
        {
            controllers.Add(controller);
        }

        public void Enqueue(GamepadState state)
        {
            states.Enqueue(state);
        }

        public GamepadState ReadState()
        {
            if (states.Count > 0)
            {
                CurrentState = states.Dequeue();
            }

            return CurrentState;
        }

        public IReadOnlyList<GamepadControllerInfo> GetControllers()
        {
            return controllers;
        }

        public void SelectController(int instanceId)
        {
            SelectedInstanceId = instanceId;
        }

        public bool TryRumble(ushort lowFrequency, ushort highFrequency, uint durationMs)
        {
            RumbleCallCount++;
            return CurrentState.IsConnected;
        }

        public void Dispose()
        {
        }
    }
}
