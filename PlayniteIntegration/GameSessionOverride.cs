using System;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class GameSessionOverride
    {
        public GameSessionOverride()
        {
            EnableSessionTracking = true;
            DisconnectGracePeriodMilliseconds = 1500;
            ProtectAllActiveControllers = true;
        }

        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public bool EnableSessionTracking { get; set; }
        public int DisconnectGracePeriodMilliseconds { get; set; }
        public bool AllowControllerTakeover { get; set; }
        public bool ProtectAllActiveControllers { get; set; }
        public bool PauseGameOnDisconnect { get; set; }
        public bool ForcePauseOfflineGames { get; set; }
        public bool? OverrideSessionProtection { get; set; }
        public bool? OverridePauseProfile { get; set; }
    }

    internal sealed class SessionProtectionPolicy
    {
        public bool Enabled { get; set; }
        public int GracePeriodMilliseconds { get; set; }
        public bool AllowControllerTakeover { get; set; }
        public bool ProtectAllActiveControllers { get; set; }
        public bool PauseGameOnDisconnect { get; set; }
        public bool ForcePauseOfflineGames { get; set; }
        public bool IsGameOverride { get; set; }
        public bool HasSessionOverride { get; set; }
        public bool HasPauseOverride { get; set; }
    }
}
