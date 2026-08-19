using System.Collections.Generic;
using ControllerSessionManager.Tester.Models;
using Playnite.SDK;

namespace ControllerSessionManager.Tester.Services
{
    public sealed class HostedGamepadInputProvider : IGamepadInputProvider
    {
        private readonly TesterHostClient client;
        private bool disposed;

        public HostedGamepadInputProvider()
            : this(LogManager.GetLogger())
        {
        }

        public HostedGamepadInputProvider(ILogger logger)
        {
            client = TesterHostClient.Acquire(logger);
        }

        public GamepadState ReadState()
        {
            return client.ReadState();
        }

        public IReadOnlyList<GamepadControllerInfo> GetControllers()
        {
            return client.GetControllers();
        }

        public void SelectController(int instanceId)
        {
            client.SelectController(instanceId);
        }

        public bool TryRumble(ushort lowFrequency, ushort highFrequency, uint durationMs)
        {
            return client.TryRumble(lowFrequency, highFrequency, durationMs);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            TesterHostClient.ReleaseShared();
        }
    }
}
