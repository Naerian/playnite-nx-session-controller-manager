using ControllerSessionManager.Tester.Models;
using System;
using System.Collections.Generic;

namespace ControllerSessionManager.Tester.Services
{
    public interface IGamepadInputProvider : IDisposable
    {
        GamepadState ReadState();
        IReadOnlyList<GamepadControllerInfo> GetControllers();
        void SelectController(int instanceId);
        bool TryRumble(ushort lowFrequency, ushort highFrequency, uint durationMs);
    }
}
