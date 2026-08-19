using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Playnite.SDK;

namespace ControllerSessionManager.Overlay
{
    internal sealed class OverlayClient : IDisposable
    {
        private readonly ILogger logger;
        private readonly string pipeName;
        private readonly string token;
        private readonly BlockingCollection<string> queue = new BlockingCollection<string>();
        private readonly Thread worker;
        private Process hostProcess;
        private bool disposed;
        private bool forcePauseActive;
        private Guid lastSessionId;
        private DateTime lastHeartbeatUtc = DateTime.MinValue;
        private readonly object hostSync = new object();

        public OverlayClient(ILogger sourceLogger)
        {
            logger = sourceLogger;
            pipeName = "CSM_" + Process.GetCurrentProcess().Id + "_" + Guid.NewGuid().ToString("N");
            token = Guid.NewGuid().ToString("N");
            worker = new Thread(ProcessQueue) { IsBackground = true, Name = "CSM Overlay IPC" };
            worker.Start();
        }

        public void Show(Guid sessionId, Guid incidentId, int gameProcessId, string title,
            string message, string instruction, string pauseStatus, string pauseStatusKind,
            string pauseStatusIconGeometry, string iconGeometry, bool forcePause, int pauseProcessId,
            string pauseFailureStatus, string pauseFailureKind, string pauseFailureIconGeometry,
            string presentationStyle)
        {
            EnsureHost();
            lastSessionId = sessionId;
            forcePauseActive = forcePause;
            Enqueue(string.Join("|", new[]
            {
                "CSM3", token, sessionId.ToString("N"), "SHOW", incidentId.ToString("N"),
                gameProcessId.ToString(), Encode(title), Encode(message), Encode(instruction),
                Encode(pauseStatus), Encode(pauseStatusKind), Encode(pauseStatusIconGeometry),
                Encode(iconGeometry), forcePause.ToString(), pauseProcessId.ToString(),
                Encode(pauseFailureStatus), Encode(pauseFailureKind), Encode(pauseFailureIconGeometry),
                Encode(presentationStyle)
            }));
        }

        public void Prepare(Guid sessionId)
        {
            EnsureHost();
            lastSessionId = sessionId;
            Heartbeat(sessionId);
        }

        public void ShowToast(Guid sessionId, int processId, string kind, string title,
            string message, string iconGeometry, int durationMilliseconds, string presentationStyle)
        {
            SendToast("TOAST", sessionId, processId, kind, title, message, iconGeometry,
                durationMilliseconds, presentationStyle);
        }

        public void ShowToastPreview(Guid sessionId, int processId, string kind, string title,
            string message, string iconGeometry, int durationMilliseconds, string presentationStyle)
        {
            SendToast("TOASTPREVIEW", sessionId, processId, kind, title, message, iconGeometry,
                durationMilliseconds, presentationStyle);
        }

        private void SendToast(string command, Guid sessionId, int processId, string kind, string title,
            string message, string iconGeometry, int durationMilliseconds, string presentationStyle)
        {
            EnsureHost();
            lastSessionId = sessionId;
            Enqueue(string.Join("|", new[]
            {
                "CSM3", token, sessionId.ToString("N"), command, Guid.NewGuid().ToString("N"),
                processId.ToString(), durationMilliseconds.ToString(), kind, Encode(title),
                Encode(message), Encode(iconGeometry), Encode(presentationStyle)
            }));
        }

        public void HideAll(Guid sessionId)
        {
            if (!IsHostRunning())
            {
                return;
            }
            forcePauseActive = false;
            lastSessionId = sessionId;
            Enqueue(string.Join("|", new[] { "CSM3", token, sessionId.ToString("N"), "HIDEALL" }));
        }

        public void Heartbeat(Guid sessionId)
        {
            if (!IsHostRunning() ||
                DateTime.UtcNow - lastHeartbeatUtc < TimeSpan.FromSeconds(2))
            {
                return;
            }

            lastHeartbeatUtc = DateTime.UtcNow;
            Enqueue(string.Join("|", new[] { "CSM3", token, sessionId.ToString("N"), "HEARTBEAT" }));
        }

        private void EnsureHost()
        {
            try
            {
                lock (hostSync)
                {
                    if (disposed || IsHostRunning())
                    {
                        return;
                    }

                    if (hostProcess != null)
                    {
                        hostProcess.Dispose();
                        hostProcess = null;
                    }
                    var directory = Path.GetDirectoryName(typeof(OverlayClient).Assembly.Location);
                    var executable = Path.Combine(directory, "ControllerSessionManager.OverlayHost.exe");
                    if (!File.Exists(executable))
                    {
                        logger.Error("Controller Manager overlay host was not found: " + executable);
                        return;
                    }

                    hostProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = executable,
                        Arguments = string.Format("--pipe {0} --token {1} --parent {2}", pipeName, token,
                            Process.GetCurrentProcess().Id),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    logger.Debug("Controller Manager overlay host started safely.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to start the overlay host; the notification was skipped.");
            }
        }

        private bool IsHostRunning()
        {
            try
            {
                return hostProcess != null && !hostProcess.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private void Enqueue(string message)
        {
            if (!disposed && !queue.IsAddingCompleted)
            {
                try
                {
                    queue.Add(message);
                }
                catch (InvalidOperationException)
                {
                    logger.Warn("An overlay command was ignored while the client was shutting down.");
                }
            }
        }

        private void ProcessQueue()
        {
            foreach (var message in queue.GetConsumingEnumerable())
            {
                for (var attempt = 0; attempt < 12 && !disposed; attempt++)
                {
                    try
                    {
                        using (var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
                        {
                            pipe.Connect(350);
                            using (var writer = new StreamWriter(pipe, new UTF8Encoding(false)))
                            {
                                writer.WriteLine(message);
                                writer.Flush();
                            }
                        }
                        break;
                    }
                    catch (TimeoutException)
                    {
                        Thread.Sleep(100);
                    }
                    catch (IOException ex)
                    {
                        if (attempt == 11)
                        {
                            logger.Warn(ex, "Failed to send a command to the overlay host.");
                        }
                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "Unexpected overlay IPC failure.");
                        break;
                    }
                }
            }
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (IsHostRunning())
            {
                Enqueue(string.Join("|", new[]
                {
                    "CSM3", token, (lastSessionId == Guid.Empty ? Guid.NewGuid() : lastSessionId).ToString("N"),
                    "SHUTDOWN"
                }));
            }
            queue.CompleteAdding();
            if (!worker.Join(2500))
            {
                worker.Interrupt();
            }
            disposed = true;
            queue.Dispose();
            if (hostProcess != null)
            {
                try
                {
                    if (!hostProcess.HasExited && !hostProcess.WaitForExit(2500) && !forcePauseActive)
                    {
                        hostProcess.Kill();
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "Failed to stop the overlay host owned by this plugin instance.");
                }
                hostProcess.Dispose();
            }
        }
    }
}
