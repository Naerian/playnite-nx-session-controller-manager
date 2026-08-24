using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using ControllerSessionManager.Tester.Services;

namespace ControllerSessionManager.TesterHost
{
    internal static class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [STAThread]
        private static int Main(string[] args)
        {
            var pipeName = ReadArgument(args, "--pipe");
            var token = ReadArgument(args, "--token");
            var sdlDir = ReadArgument(args, "--sdl-dir");
            var mappingDatabase = ReadArgument(args, "--mapping-db");
            int parentProcessId;
            if (string.IsNullOrWhiteSpace(pipeName) || string.IsNullOrWhiteSpace(token) ||
                !int.TryParse(ReadArgument(args, "--parent"), out parentProcessId))
            {
                return 1;
            }

            if (!string.IsNullOrWhiteSpace(sdlDir) && Directory.Exists(sdlDir))
            {
                SetDllDirectory(sdlDir);
                try
                {
                    Directory.SetCurrentDirectory(sdlDir);
                }
                catch
                {
                }
            }

            SdlGamepadProvider provider = null;
            try
            {
                provider = new SdlGamepadProvider(mappingDatabase);
                RunServer(pipeName, token, parentProcessId, provider);
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return 2;
            }
            finally
            {
                if (provider != null)
                {
                    provider.Dispose();
                }
            }
        }

        private static void RunServer(string pipeName, string token, int parentProcessId, SdlGamepadProvider provider)
        {
            while (IsParentAlive(parentProcessId))
            {
                try
                {
                    using (var server = CreatePipe(pipeName))
                    {
                        var connected = WaitForConnection(server, parentProcessId);
                        if (!connected)
                        {
                            return;
                        }

                        using (var reader = new StreamReader(server, Encoding.UTF8, false, 65536, true))
                        using (var writer = new StreamWriter(server, new UTF8Encoding(false)) { AutoFlush = true })
                        {
                            ServeClient(reader, writer, token, parentProcessId, provider);
                        }
                    }
                }
                catch (IOException)
                {
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Thread.Sleep(250);
                }
            }
        }

        private static void ServeClient(StreamReader reader, StreamWriter writer, string token, int parentProcessId,
            SdlGamepadProvider provider)
        {
            var stop = false;
            var readerThread = new Thread(new ThreadStart(delegate
            {
                try
                {
                    while (!stop)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                        {
                            stop = true;
                            return;
                        }

                        Dispatch(line, token, provider);
                    }
                }
                catch (IOException)
                {
                    stop = true;
                }
                catch (ObjectDisposedException)
                {
                    stop = true;
                }
            }))
            {
                IsBackground = true,
                Name = "CSM Tester Pipe Reader"
            };
            readerThread.Start();

            while (!stop && IsParentAlive(parentProcessId))
            {
                try
                {
                    var snapshot = GamepadProtocol.EncodeSnapshot(token, provider.ReadState(), provider.GetControllers());
                    writer.WriteLine(snapshot);
                }
                catch (IOException)
                {
                    stop = true;
                    break;
                }

                Thread.Sleep(16);
            }

            stop = true;
        }

        private static void Dispatch(string line, string token, SdlGamepadProvider provider)
        {
            string command;
            string[] fields;
            if (!GamepadProtocol.TryParseCommand(line, token, out command, out fields))
            {
                return;
            }

            if (command == GamepadProtocol.SelectCommand && fields.Length >= 1)
            {
                int instanceId;
                if (int.TryParse(fields[0], out instanceId))
                {
                    provider.SelectController(instanceId);
                }
            }
            else if (command == GamepadProtocol.RumbleCommand && fields.Length >= 3)
            {
                ushort low;
                ushort high;
                uint duration;
                if (ushort.TryParse(fields[0], out low) &&
                    ushort.TryParse(fields[1], out high) &&
                    uint.TryParse(fields[2], out duration))
                {
                    provider.TryRumble(low, high, duration);
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
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous, 65536, 65536, security);
        }

        private static bool WaitForConnection(NamedPipeServerStream server, int parentProcessId)
        {
            var connected = false;
            var wait = server.BeginWaitForConnection(null, null);
            while (!wait.IsCompleted)
            {
                if (!IsParentAlive(parentProcessId))
                {
                    try
                    {
                        server.Dispose();
                    }
                    catch
                    {
                    }

                    return false;
                }

                wait.AsyncWaitHandle.WaitOne(250);
            }

            try
            {
                server.EndWaitForConnection(wait);
                connected = true;
            }
            catch (ObjectDisposedException)
            {
                connected = false;
            }

            return connected;
        }

        private static bool IsParentAlive(int parentProcessId)
        {
            try
            {
                var parent = Process.GetProcessById(parentProcessId);
                return parent != null && !parent.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private static string ReadArgument(string[] args, string name)
        {
            if (args == null)
            {
                return null;
            }

            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
