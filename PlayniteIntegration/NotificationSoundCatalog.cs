using System;

namespace ControllerSessionManager.PlayniteIntegration
{
    public enum NotificationSoundKind
    {
        Connected,
        Disconnected,
        LowBattery,
        Warning
    }

    public enum NotificationSoundScope
    {
        Desktop,
        Fullscreen
    }

    /// <summary>
    /// Built-in WAV packs under Audio/{packId}/ (connected, disconnected, low_battery, error).
    /// </summary>
    public static class NotificationSoundCatalog
    {
        public const string CreatorPackPrefix = "creator:";
        public const string ModernCrystal = "1_Modern_Crystal";
        public const string ConsoleChime = "2_Console_Chime";
        public const string CyberGamer = "3_Cyber_Gamer";
        public const string RetroArcade = "4_Retro_Arcade";
        public const string MinimalSoft = "5_Minimal_Soft";
        public const string BassHeavy = "6_Bass_Heavy";
        public const string HandheldHaptic = "7_Handheld_Haptic";

        public static readonly string[] AllPacks =
        {
            ModernCrystal,
            ConsoleChime,
            CyberGamer,
            RetroArcade,
            MinimalSoft,
            BassHeavy,
            HandheldHaptic
        };

        public static string Normalize(string packId)
        {
            if (string.IsNullOrWhiteSpace(packId))
            {
                return ModernCrystal;
            }

            var trimmed = packId.Trim();
            if (trimmed.StartsWith(CreatorPackPrefix, StringComparison.OrdinalIgnoreCase) &&
                trimmed.Length > CreatorPackPrefix.Length)
            {
                return CreatorPackPrefix + trimmed.Substring(CreatorPackPrefix.Length);
            }

            foreach (var pack in AllPacks)
            {
                if (string.Equals(pack, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return pack;
                }
            }

            return ModernCrystal;
        }

        public static string FileName(NotificationSoundKind kind)
        {
            switch (kind)
            {
                case NotificationSoundKind.Connected:
                    return "connected.wav";
                case NotificationSoundKind.Disconnected:
                    return "disconnected.wav";
                case NotificationSoundKind.LowBattery:
                    return "low_battery.wav";
                case NotificationSoundKind.Warning:
                    return "error.wav";
                default:
                    return "connected.wav";
            }
        }

        public static string LocKey(string packId)
        {
            switch (Normalize(packId))
            {
                case ConsoleChime:
                    return "LOCCSM_SoundPackConsoleChime";
                case CyberGamer:
                    return "LOCCSM_SoundPackCyberGamer";
                case RetroArcade:
                    return "LOCCSM_SoundPackRetroArcade";
                case MinimalSoft:
                    return "LOCCSM_SoundPackMinimalSoft";
                case BassHeavy:
                    return "LOCCSM_SoundPackBassHeavy";
                case HandheldHaptic:
                    return "LOCCSM_SoundPackHandheldHaptic";
                default:
                    return "LOCCSM_SoundPackModernCrystal";
            }
        }

        public static string DisplayName(string packId)
        {
            switch (Normalize(packId))
            {
                case ConsoleChime:
                    return "Console Chime";
                case CyberGamer:
                    return "Cyber Gamer";
                case RetroArcade:
                    return "Retro Arcade";
                case MinimalSoft:
                    return "Minimal Soft";
                case BassHeavy:
                    return "Bass Heavy";
                case HandheldHaptic:
                    return "Handheld Haptic";
                default:
                    return "Modern Crystal";
            }
        }
    }
}
