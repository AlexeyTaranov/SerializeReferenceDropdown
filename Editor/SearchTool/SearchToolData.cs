using System;
using System.Collections.Generic;

namespace SerializeReferenceDropdown.Editor.SearchTool
{
    [Serializable]
    public class SearchToolData
    {
        public List<PrefabData> PrefabsData = new List<PrefabData>();
        public List<ScriptableObjectData> SOsData = new List<ScriptableObjectData>();

        [Serializable]
        public struct ReferenceData
        {
            public long referenceID;
            public string objectTypeName;
        }

        [Serializable]
        public class PrefabData
        {
            public string assetPath;
            public List<PrefabComponentData> componentsData;
        }

        [Serializable]
        public class PrefabComponentData
        {
            public int instanceId;
            public List<ReferenceData> serializeReferencesData;
        }

        [Serializable]
        public class ScriptableObjectData
        {
            public string assetPath;
            public List<ReferenceData> serializeReferencesData;
        }
    }
}