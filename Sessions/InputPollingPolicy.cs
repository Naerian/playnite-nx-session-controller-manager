using System;

namespace ControllerSessionManager.Sessions
{
    internal static class InputPollingPolicy
    {
        internal static TimeSpan GetInterval(bool sessionRunning)
        {
            // Short stick movements can begin and return to neutral between slower samples.
            // XInput is designed for frequent polling, so use 20 Hz while tracking a game.
            return TimeSpan.FromMilliseconds(sessionRunning ? 50 : 250);
        }
    }
}
