using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using SerializeReferenceDropdown.Editor.Preferences;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;

namespace SerializeReferenceDropdown.Editor.SearchTool
{
    public static class SearchToolWindowIntegration
    {
        private static (Thread thread, TcpListener listener) instance;
        private static Thread serverThread;
        private static int? port;
        

        [InitializeOnLoadMethod]
        public static void Run()
        {
            EditorApplication.delayCall = TryFetchPort;
            StopServer();
            serverThread = new Thread(StartServer)
            {
                IsBackground = true
            };
            serverThread.Start();
        }


        private static void StopServer()
        {
            instance.listener?.Stop();
            instance.thread?.Join();
        }

        public static bool IsAvailablePort(int checkPort)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, checkPort);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e);
                return false;
            }
        }


        private static void StartServer()
        {
            try
            {
                while (port == null)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                var listener = new TcpListener(IPAddress.Loopback, port.Value);
                instance.listener = listener;
                listener.Start();
                Log.DevLog($"Start integration server on port - {port.Value}");

                while (listener.Pending())
                {
                    using var client = listener.AcceptTcpClient();
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream);
                    using var writer = new StreamWriter(stream) { AutoFlush = true };

                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        Log.DevLog($"Command received: {line}");
                        var values = line.Split('-');
                        if (values.Length >= 2)
                        {
                            var cmd = values[0];
                            var value = values[1];

                            if (cmd == "ShowSearchTypeWindow")
                            {
                                EditorApplication.delayCall += () => { ShowSearchWindow(value); };
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                StopServer();
            }
        }

        private static void TryFetchPort()
        {
            try
            {
                var settings = SerializeReferenceToolsUserPreferences.GetOrLoadSettings();
                port = settings.SearchToolIntegrationPort;
            }
            catch (Exception e)
            {
                Log.DevError(e);
            }
        }

        private static void ShowSearchWindow(string value)
        {
            var typeArray = value.Split(',');
            if (typeArray.Length >= 2)
            {
                var typeName = typeArray[0];
                var asmName = typeArray[1];
                var typeString = $"{asmName} {typeName}";
                var type = TypeUtils.ExtractTypeFromString(typeString);
                SearchToolWindow.ShowSearchTypeWindow(type);
            }
        }
    }
}