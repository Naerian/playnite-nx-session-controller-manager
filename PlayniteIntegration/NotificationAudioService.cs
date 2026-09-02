using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Playnite.SDK;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class NotificationAudioService : IDisposable
    {
        private readonly ILogger logger;
        private readonly string pluginDirectory;
        private readonly IPlayniteAPI playniteApi;
        private readonly object gate = new object();
        private MediaPlayer player;
        private bool disposed;
        private int playbackGeneration;

        public NotificationAudioService(ILogger sourceLogger, string pluginDirectory, IPlayniteAPI api)
        {
            logger = sourceLogger;
            this.pluginDirectory = pluginDirectory ?? string.Empty;
            playniteApi = api;
        }

        public void Play(NotificationSoundKind kind, ControllerSessionManagerSettings settings)
        {
            Play(kind, settings, NotificationSoundScope.Fullscreen);
        }

        public void Play(NotificationSoundKind kind, ControllerSessionManagerSettings settings,
            NotificationSoundScope scope)
        {
            Play(kind, settings, scope, ignoreKindToggle: false, ignoreScopeToggle: false);
        }

        /// <summary>
        /// Preview playback respects the selected sound and volume, but ignores destination and
        /// per-event toggles so the explicit preview checkbox remains useful.
        /// </summary>
        public void PlayPreview(NotificationSoundKind kind, ControllerSessionManagerSettings settings)
        {
            Play(kind, settings, NotificationSoundScope.Fullscreen,
                ignoreKindToggle: true, ignoreScopeToggle: true);
        }

        private void Play(NotificationSoundKind kind, ControllerSessionManagerSettings settings,
            NotificationSoundScope scope, bool ignoreKindToggle, bool ignoreScopeToggle)
        {
            if (disposed || settings == null)
            {
                return;
            }

            if (!ignoreKindToggle && !IsKindEnabled(kind, settings))
            {
                return;
            }

            if (!ignoreScopeToggle && !IsScopeEnabled(scope, settings))
            {
                return;
            }

            var path = ResolvePath(kind, settings, scope);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                if (logger != null)
                {
                    logger.Warn("Notification sound missing for " + kind + " pack=" +
                        (settings.NotificationSoundPack ?? string.Empty));
                }
                return;
            }

            var volume = ClampVolume(settings.NotificationSoundVolume);
            var delayMs = GetPlaybackDelayMilliseconds(kind, ignoreKindToggle);
            var dispatcher = Application.Current == null ? null : Application.Current.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            var playbackToken = Interlocked.Increment(ref playbackGeneration);
            Action play = () =>
            {
                if (playbackToken == Volatile.Read(ref playbackGeneration))
                {
                    PlayCore(path, volume);
                }
            };
            if (delayMs > 0)
            {
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(delayMs)
                };
                timer.Tick += (sender, args) =>
                {
                    timer.Stop();
                    play();
                };
                if (dispatcher.CheckAccess())
                {
                    timer.Start();
                }
                else
                {
                    dispatcher.BeginInvoke(new Action(() => timer.Start()), DispatcherPriority.Normal);
                }
                return;
            }

            if (dispatcher.CheckAccess())
            {
                play();
            }
            else
            {
                dispatcher.BeginInvoke(play, DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// Soft delay so plugin SFX do not stack on top of Windows device connect/disconnect chimes.
        /// </summary>
        public const int ConnectionSoundDelayMilliseconds = 320;

        private static int GetPlaybackDelayMilliseconds(NotificationSoundKind kind, bool ignoreKindToggle)
        {
            if (ignoreKindToggle)
            {
                // Previews keep the same delay so users hear what real events will sound like.
            }

            if (kind == NotificationSoundKind.Connected || kind == NotificationSoundKind.Disconnected)
            {
                return ConnectionSoundDelayMilliseconds;
            }

            return 0;
        }

        public static bool IsKindEnabled(NotificationSoundKind kind, ControllerSessionManagerSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            switch (kind)
            {
                case NotificationSoundKind.Connected:
                    return settings.PlaySoundOnConnected;
                case NotificationSoundKind.Disconnected:
                    return settings.PlaySoundOnDisconnected;
                case NotificationSoundKind.LowBattery:
                    return settings.PlaySoundOnLowBattery;
                case NotificationSoundKind.Warning:
                    return settings.PlaySoundOnWarning;
                default:
                    return false;
            }
        }

        public static bool IsScopeEnabled(NotificationSoundScope scope,
            ControllerSessionManagerSettings settings)
        {
            return settings != null && (scope == NotificationSoundScope.Desktop
                ? settings.EnableDesktopNotificationSounds
                : settings.EnableFullscreenNotificationSounds);
        }

        public string ResolvePath(NotificationSoundKind kind,
            ControllerSessionManagerSettings settings)
        {
            return ResolvePath(kind, settings, NotificationSoundScope.Fullscreen);
        }

        public string ResolvePath(NotificationSoundKind kind,
            ControllerSessionManagerSettings settings, NotificationSoundScope scope)
        {
            var custom = CustomPath(kind, settings);
            if (!string.IsNullOrWhiteSpace(custom) && File.Exists(custom))
            {
                return custom;
            }
            var creatorSound = CreatorThemeCatalog.GetSoundPathForPack(
                settings == null ? string.Empty : settings.NotificationSoundPack, kind);
            if (!string.IsNullOrWhiteSpace(creatorSound)) return creatorSound;
            if (settings != null &&
                string.Equals(settings.NotificationSoundPack, NotificationSoundCatalog.ThemeEmbeddedPack,
                    StringComparison.OrdinalIgnoreCase) &&
                playniteApi != null)
            {
                var embedded = ThemeEmbeddedAppearanceCatalog.GetSoundPath(playniteApi, kind);
                if (!string.IsNullOrWhiteSpace(embedded)) return embedded;
            }
            return ResolvePath(kind, settings == null ? null : settings.NotificationSoundPack);
        }

        public string ResolvePath(NotificationSoundKind kind, string packId)
        {
            var pack = NotificationSoundCatalog.Normalize(packId);
            var file = NotificationSoundCatalog.FileName(kind);
            return Path.Combine(pluginDirectory, "Audio", pack, file);
        }

        private static string CustomPath(NotificationSoundKind kind,
            ControllerSessionManagerSettings settings)
        {
            if (settings == null) return string.Empty;
            switch (kind)
            {
                case NotificationSoundKind.Connected:
                    return settings.CustomConnectedSoundPath;
                case NotificationSoundKind.Disconnected:
                    return settings.CustomDisconnectedSoundPath;
                case NotificationSoundKind.LowBattery:
                    return settings.CustomLowBatterySoundPath;
                case NotificationSoundKind.Warning:
                    return settings.CustomWarningSoundPath;
                default:
                    return string.Empty;
            }
        }

        private void PlayCore(string path, double volume)
        {
            if (disposed)
            {
                return;
            }

            lock (gate)
            {
                try
                {
                    if (player == null)
                    {
                        player = new MediaPlayer();
                        player.MediaFailed += OnMediaFailed;
                    }

                    player.Stop();
                    player.Close();
                    player.Volume = volume;
                    player.Open(new Uri(path, UriKind.Absolute));
                    player.Play();
                }
                catch (Exception ex)
                {
                    if (logger != null)
                    {
                        logger.Warn(ex, "Failed to play notification sound: " + path);
                    }
                }
            }
        }

        private void OnMediaFailed(object sender, ExceptionEventArgs e)
        {
            if (logger != null)
            {
                logger.Warn(e != null ? e.ErrorException : null, "MediaPlayer failed for notification sound.");
            }
        }

        /// <summary>
        /// Releases the current media file immediately so it can be replaced or removed.
        /// </summary>
        public void Stop()
        {
            Interlocked.Increment(ref playbackGeneration);
            if (disposed)
            {
                return;
            }

            lock (gate)
            {
                if (player == null)
                {
                    return;
                }

                try
                {
                    player.Stop();
                    player.Close();
                }
                catch (Exception ex)
                {
                    if (logger != null)
                    {
                        logger.Warn(ex, "Failed to release the notification sound player.");
                    }
                }
            }
        }

        private static double ClampVolume(double volume)
        {
            if (double.IsNaN(volume) || double.IsInfinity(volume))
            {
                return 0.7;
            }

            if (volume < 0)
            {
                return 0;
            }

            if (volume > 1)
            {
                return 1;
            }

            return volume;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            lock (gate)
            {
                if (player != null)
                {
                    try
                    {
                        player.MediaFailed -= OnMediaFailed;
                        player.Stop();
                        player.Close();
                    }
                    catch
                    {
                        // ignore shutdown races
                    }

                    player = null;
                }
            }
        }
    }
}
