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
            public Type objectType;
        }
        
        public interface IAssetData
        {
            public string AssetPath { get; }
        }

        [Serializable]
        public class PrefabData : IAssetData
        {
            public string assetPath;
            public List<PrefabComponentData> componentsData = new List<PrefabComponentData>();
            public string AssetPath => assetPath;
        }

        [Serializable]
        public class PrefabComponentData
        {
            public int instanceId;
            public List<ReferenceData> serializeReferencesData;
        }

        [Serializable]
        public class ScriptableObjectData : IAssetData
        {
            public string assetPath;
            public List<ReferenceData> serializeReferencesData;
            public string AssetPath => assetPath;
        }
    }
}