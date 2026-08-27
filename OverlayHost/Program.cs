using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using ControllerSessionManager.Overlay;

namespace ControllerSessionManager.OverlayHost
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (HasFlag(args, "--focus-fullscreen"))
            {
                FullscreenFocusHelper.Run();
                return;
            }

            var pipeName = ReadArgument(args, "--pipe");
            var token = ReadArgument(args, "--token");
            int parentProcessId;
            if (string.IsNullOrWhiteSpace(pipeName) || string.IsNullOrWhiteSpace(token) ||
                !int.TryParse(ReadArgument(args, "--parent"), out parentProcessId))
            {
                return;
            }

            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var suspensionLease = new ProcessSuspensionLease();
            var window = new OverlayWindow(parentProcessId, suspensionLease);
            var toastWindow = new ToastWindow();
            application.Exit += delegate { suspensionLease.Dispose(); };
            var server = new Thread(new ThreadStart(delegate { RunServer(pipeName, token, window, toastWindow); }))
            {
                IsBackground = true,
                Name = "CSM Overlay Pipe Server"
            };
            server.Start();
            application.Run();
        }

        private static void RunServer(string pipeName, string token, OverlayWindow window, ToastWindow toastWindow)
        {
            while (true)
            {
                try
                {
                    using (var server = CreatePipe(pipeName))
                    {
                        server.WaitForConnection();
                        using (var reader = new StreamReader(server, Encoding.UTF8, false,
                            OverlayIpcLimits.PipeBufferBytes, true))
                        {
                            var line = reader.ReadLine();
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                continue;
                            }

                            if (line.Length > OverlayIpcLimits.MaxLineCharacters)
                            {
                                Debug.WriteLine("CSM overlay IPC line dropped: " + line.Length +
                                    " characters.");
                                continue;
                            }

                            Dispatch(line, token, window, toastWindow);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Thread.Sleep(250);
                }
            }
        }

        private static NamedPipeServerStream CreatePipe(string pipeName)
        {
            var identity = WindowsIdentity.GetCurrent();
            var security = new PipeSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new PipeAccessRule(identity.User, PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
            return new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte,
                PipeOptions.None, OverlayIpcLimits.PipeBufferBytes, OverlayIpcLimits.PipeBufferBytes,
                security);
        }

        private static void Dispatch(string line, string token, OverlayWindow window, ToastWindow toastWindow)
        {
            var parts = line.Split('|');
            if (parts.Length < 4 || parts[0] != "CSM3" || parts[1] != token)
            {
                return;
            }

            var sessionId = parts[2];
            var command = parts[3];
            window.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (command == "SHOW" && parts.Length >= 19)
                {
                    int processId;
                    int.TryParse(parts[5], out processId);
                    int pauseProcessId;
                    int.TryParse(parts[14], out pauseProcessId);
                    bool forcePause;
                    bool.TryParse(parts[13], out forcePause);
                    window.ShowIncident(sessionId, parts[4], processId, Decode(parts[6]),
                        Decode(parts[7]), Decode(parts[8]), Decode(parts[9]), Decode(parts[10]),
                        Decode(parts[11]), Decode(parts[12]), forcePause, pauseProcessId,
                        Decode(parts[15]), Decode(parts[16]), Decode(parts[17]), Decode(parts[18]),
                        parts.Length > 19 ? Decode(parts[19]) : string.Empty,
                        parts.Length > 20 ? Decode(parts[20]) : string.Empty,
                        parts.Length > 21 ? Decode(parts[21]) : string.Empty,
                        parts.Length > 22 ? Decode(parts[22]) : string.Empty,
                        parts.Length > 23 ? Decode(parts[23]) : string.Empty,
                        parts.Length > 24 ? Decode(parts[24]) : string.Empty);
                }
                else if ((command == "TOAST" || command == "TOASTPREVIEW") && parts.Length >= 12)
                {
                    int processId;
                    int duration;
                    int.TryParse(parts[5], out processId);
                    int.TryParse(parts[6], out duration);
                    var connectionIcon = parts.Length > 12 ? Decode(parts[12]) : string.Empty;
                    if (command == "TOASTPREVIEW")
                    {
                        toastWindow.ReplaceWith(parts[4], processId, duration, parts[7], Decode(parts[8]),
                            Decode(parts[9]), Decode(parts[10]), Decode(parts[11]), connectionIcon);
                    }
                    else
                    {
                        toastWindow.Enqueue(parts[4], processId, duration, parts[7], Decode(parts[8]),
                            Decode(parts[9]), Decode(parts[10]), Decode(parts[11]), connectionIcon);
                    }
                }
                else if (command == "HIDEALL")
                {
                    window.HideSession(sessionId);
                }
                else if (command == "HEARTBEAT")
                {
                    window.RecordHeartbeat(sessionId);
                }
                else if (command == "SHUTDOWN")
                {
                    window.HideSession(sessionId);
                    Application.Current.Shutdown();
                }
            }));
        }

        private static string Decode(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ReadArgument(string[] args, string name)
        {
            for (var index = 0; index + 1 < args.Length; index++)
            {
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }

            return null;
        }

        private static bool HasFlag(string[] args, string name)
        {
            if (args == null)
            {
                return false;
            }

            for (var index = 0; index < args.Length; index++)
            {
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
