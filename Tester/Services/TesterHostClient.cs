using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using ControllerSessionManager.Tester.Models;
using Playnite.SDK;

namespace ControllerSessionManager.Tester.Services
{
    internal sealed class TesterHostClient : IGamepadInputProvider
    {
        private static readonly object sharedSync = new object();
        private static TesterHostClient shared;
        private static int sharedReferences;

        private readonly ILogger logger;
        private readonly string pipeName;
        private readonly string token;
        private readonly object sync = new object();
        private readonly object stateSync = new object();
        private GamepadState latestState = new GamepadState();
        private IList<GamepadControllerInfo> latestControllers = new List<GamepadControllerInfo>();
        private Process hostProcess;
        private NamedPipeClientStream pipe;
        private StreamWriter writer;
        private StreamReader reader;
        private Thread readerThread;
        private bool disposed;
        private bool suspended;

        private TesterHostClient(ILogger sourceLogger)
        {
            logger = sourceLogger;
            pipeName = "CSMT_" + Process.GetCurrentProcess().Id + "_" + Guid.NewGuid().ToString("N");
            token = Guid.NewGuid().ToString("N");
        }

        public static TesterHostClient Acquire(ILogger logger)
        {
            lock (sharedSync)
            {
                if (shared == null || shared.disposed)
                {
                    shared = new TesterHostClient(logger);
                }

                sharedReferences++;
                return shared;
            }
        }

        public static void ReleaseShared()
        {
            lock (sharedSync)
            {
                if (shared == null)
                {
                    return;
                }

                sharedReferences--;
                if (sharedReferences <= 0)
                {
                    sharedReferences = 0;
                    shared.Dispose();
                    shared = null;
                }
            }
        }

        public static void SuspendShared()
        {
            lock (sharedSync)
            {
                if (shared != null)
                {
                    shared.Suspend();
                }
            }
        }

        public static void ResumeShared()
        {
            lock (sharedSync)
            {
                if (shared != null)
                {
                    shared.Resume();
                }
            }
        }

        public static void ForceStopShared()
        {
            lock (sharedSync)
            {
                if (shared == null)
                {
                    return;
                }

                shared.Dispose();
                shared = null;
                sharedReferences = 0;
            }
        }

        public void Resume()
        {
            lock (sync)
            {
                suspended = false;
            }
        }

        public void Suspend()
        {
            lock (sync)
            {
                suspended = true;
                CloseConnection();
                StopHost();
            }
        }

        public GamepadState ReadState()
        {
            EnsureConnected();
            lock (stateSync)
            {
                return latestState ?? new GamepadState();
            }
        }

        public IReadOnlyList<GamepadControllerInfo> GetControllers()
        {
            EnsureConnected();
            lock (stateSync)
            {
                return new List<GamepadControllerInfo>(latestControllers);
            }
        }

        public void SelectController(int instanceId)
        {
            Send(GamepadProtocol.EncodeCommand(token, GamepadProtocol.SelectCommand,
                instanceId.ToString()));
        }

        public bool TryRumble(ushort lowFrequency, ushort highFrequency, uint durationMs)
        {
            Send(GamepadProtocol.EncodeCommand(token, GamepadProtocol.RumbleCommand,
                lowFrequency.ToString(), highFrequency.ToString(), durationMs.ToString()));
            return ReadState().IsConnected;
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                CloseConnection();
                StopHost();
            }
        }

        private void EnsureConnected()
        {
            lock (sync)
            {
                if (disposed || suspended)
                {
                    return;
                }

                if (pipe != null && pipe.IsConnected && IsHostRunning())
                {
                    return;
                }

                CloseConnection();
                StartHost();
                ConnectPipe();
            }
        }

        private void StartHost()
        {
            if (IsHostRunning())
            {
                return;
            }

            if (hostProcess != null)
            {
                hostProcess.Dispose();
                hostProcess = null;
            }

            var directory = Path.GetDirectoryName(typeof(TesterHostClient).Assembly.Location);
            var executable = Path.Combine(directory, "ControllerSessionManager.TesterHost.exe");
            if (!File.Exists(executable))
            {
                if (logger != null)
                {
                    logger.Error("Controller Manager tester host was not found: " + executable);
                }
                return;
            }

            var sdlDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            hostProcess = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = string.Format("--pipe {0} --token {1} --parent {2} --sdl-dir \"{3}\"",
                    pipeName, token, Process.GetCurrentProcess().Id, sdlDir),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            if (logger != null)
            {
                logger.Debug("Controller Manager tester host started out of process.");
            }
        }

        private void ConnectPipe()
        {
            if (!IsHostRunning())
            {
                return;
            }

            pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            var connected = false;
            for (var attempt = 0; attempt < 20 && !disposed && !suspended; attempt++)
            {
                try
                {
                    pipe.Connect(250);
                    connected = true;
                    break;
                }
                catch (TimeoutException)
                {
                    Thread.Sleep(50);
                }
                catch (IOException)
                {
                    Thread.Sleep(50);
                }
            }

            if (!connected)
            {
                CloseConnection();
                return;
            }

            writer = new StreamWriter(pipe, new UTF8Encoding(false)) { AutoFlush = true };
            reader = new StreamReader(pipe, Encoding.UTF8, false, 65536, true);
            readerThread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "CSM Tester IPC"
            };
            readerThread.Start();
        }

        private void ReadLoop()
        {
            try
            {
                while (!disposed && pipe != null && pipe.IsConnected)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        return;
                    }

                    GamepadProtocol.SnapshotMessage snapshot;
                    if (!GamepadProtocol.TryParseSnapshot(line, token, out snapshot))
                    {
                        continue;
                    }

                    lock (stateSync)
                    {
                        latestState = snapshot.State;
                        latestControllers = snapshot.Controllers;
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void Send(string message)
        {
            EnsureConnected();
            lock (sync)
            {
                if (writer == null || disposed || suspended)
                {
                    return;
                }

                try
                {
                    writer.WriteLine(message);
                }
                catch (IOException ex)
                {
                    if (logger != null)
                    {
                        logger.Warn(ex, "Failed to send a command to the tester host.");
                    }
                }
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

        private void CloseConnection()
        {
            try
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }
            catch
            {
            }

            writer = null;
            reader = null;
            try
            {
                if (pipe != null)
                {
                    pipe.Dispose();
                }
            }
            catch
            {
            }

            pipe = null;
        }

        private void StopHost()
        {
            try
            {
                if (hostProcess != null && !hostProcess.HasExited)
                {
                    hostProcess.Kill();
                    hostProcess.WaitForExit(1000);
                }
            }
            catch
            {
            }

            if (hostProcess != null)
            {
                hostProcess.Dispose();
                hostProcess = null;
            }
        }
    }
}
