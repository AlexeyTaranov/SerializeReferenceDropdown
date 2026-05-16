using System.Collections.Generic;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    //TODO: need to find better solution
    public static class PropertyDrawerGlobalCaches
    {
        public static readonly Dictionary<Object, HashSet<string>> targetObjectAndSerializeReferencePaths =
            new Dictionary<Object, HashSet<string>>();

        public static readonly Dictionary<Object, IReadOnlyList<(string propertyPath, long refId)>>
            targetObjectAndMissingPaths = new Dictionary<Object, IReadOnlyList<(string propertyPath, long refId)>>();

        public static void DropCaches()
        {
            targetObjectAndSerializeReferencePaths.Clear();
            targetObjectAndMissingPaths.Clear();
        }
    }
}