using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using SerializeReferenceDropdown.Editor.Preferences;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;

namespace SerializeReferenceDropdown.Editor.SearchTool
{
    public static class SearchToolWindowIntegration
    {
        private const string pipeName = "SerializeReferenceDropdownIntegration";
        private static Thread serverThread;


        [InitializeOnLoadMethod]
        public static void Run()
        {
            var activeIntegration = SerializeReferenceToolsUserPreferences.GetOrLoadSettings().EnableRiderIntegration;
            if (activeIntegration)
            {
                serverThread = new Thread(StartServer)
                {
                    IsBackground = true
                };
                serverThread.Start();
            }
        }


        private static void StartServer()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut);
                    Log.DevLog($"Wait plugin message");
                    server.WaitForConnection();
                    int read = server.Read(buffer, 0, buffer.Length);
                    string message = Encoding.UTF8.GetString(buffer, 0, read);
                    Log.DevLog($"Command received: {message}");
                    var values = message.Split('-');
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
                catch (Exception e)
                {
                    Log.DevError(e);
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
                SearchToolWindow.SearchToolWindow.ShowSearchTypeWindow(type);
            }
        }
    }
}