using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.SearchTool
{
    public static class SearchToolWindowIntegration
    {
        private static Thread serverThread;

        //TODO: expose to settings
        private const int portIndex = 11000;

        [InitializeOnLoadMethod]
        private static void Run()
        {
            serverThread = new Thread(StartServer)
            {
                IsBackground = true
            };
            serverThread.Start();
        }
        

        private static void StartServer()
        {
            var listener = new TcpListener(IPAddress.Loopback, portIndex);
            listener.Start();

            while (true)
            {
                using var client = listener.AcceptTcpClient();
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                string line = reader.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    Debug.Log($"[SRD] Command received: {line}");
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