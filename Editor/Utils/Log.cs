using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace SerializeReferenceDropdown.Editor.Utils
{
    internal static class Log
    {
        private const string LogPrefix = "[SRD]";

        public static void Error(object error)
        {
            Debug.LogError($"{LogPrefix} {error}");
        }

        [Conditional("SRD_DEV")]
        public static void DevError(object error)
        {
            Debug.LogError($"{LogPrefix} {error}");
        }

        [Conditional("SRD_DEV")]
        public static void DevWarning(object warning)
        {
            Debug.LogWarning($"{LogPrefix} {warning}");
        }

        [Conditional("SRD_DEV")]
        public static void DevLog(object log)
        {
            Debug.Log($"{LogPrefix} {log}");
        }
    }
}