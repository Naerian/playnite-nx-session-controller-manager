using ControllerSessionManager.Tester.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ControllerSessionManager.Tester.Services
{
    public sealed class GamepadPollingService : IDisposable
    {
        private readonly IGamepadInputProvider provider;
        private CancellationTokenSource cancellation;
        private Task pollingTask;

        public event EventHandler<GamepadState> StateUpdated;

        public GamepadPollingService(IGamepadInputProvider provider)
        {
            this.provider = provider;
        }

        public void Start()
        {
            if (pollingTask != null && !pollingTask.IsCompleted)
            {
                return;
            }

            cancellation = new CancellationTokenSource();
            var token = cancellation.Token;

            pollingTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var state = provider.ReadState();
                    var handler = StateUpdated;
                    if (handler != null)
                    {
                        handler(this, state);
                    }
                    await Task.Delay(16, token).ConfigureAwait(false);
                }
            }, token);
        }

        public bool TryRumble(ushort lowFrequency, ushort highFrequency, uint durationMs)
        {
            return provider.TryRumble(lowFrequency, highFrequency, durationMs);
        }

        public IReadOnlyList<GamepadControllerInfo> GetControllers()
        {
            return provider.GetControllers();
        }

        public void SelectController(int instanceId)
        {
            provider.SelectController(instanceId);
        }

        public void Dispose()
        {
            if (cancellation != null)
            {
                cancellation.Cancel();
                cancellation.Dispose();
            }

            provider.Dispose();
        }
    }
}
