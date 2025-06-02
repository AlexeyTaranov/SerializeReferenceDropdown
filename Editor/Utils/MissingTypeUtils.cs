using System.Text;
using UnityEditor;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public static class MissingTypeUtils
    {
        public static string GetDetailData(this ManagedReferenceMissingType missingTypeData)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"ASM: {missingTypeData.assemblyName}");
            sb.AppendLine($"Namespace: {missingTypeData.namespaceName}");
            sb.AppendLine($"Class: {missingTypeData.className}");
            sb.AppendLine($"RefID: {missingTypeData.referenceId}");
            sb.AppendFormat("\n{0}", missingTypeData.serializedData);
            return sb.ToString();
        }
    }
}